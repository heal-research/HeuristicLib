using System.Diagnostics;
using HEAL.HeuristicLib.Collections;
using HEAL.HeuristicLib.OperatorExtensions.MeasuredOperators;
using HEAL.HeuristicLib.Operators.Creator;
using HEAL.HeuristicLib.Operators.Crossover;
using HEAL.HeuristicLib.Operators.Evaluator;
using HEAL.HeuristicLib.Operators.Interceptor;
using HEAL.HeuristicLib.Operators.Mutator;
using HEAL.HeuristicLib.Operators.Replacer;
using HEAL.HeuristicLib.Operators.Selector;
using HEAL.HeuristicLib.Operators.Terminator;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Algorithms.ALPS;

public class AlpsGeneticAlgorithm<TGenotype, TSearchSpace, TProblem>
  : IterativeAlgorithm<TGenotype, TSearchSpace, TProblem, AlpsIterationResult<TGenotype>>
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace> {
  public int PopulationSize { get; }
  public ICrossover<TGenotype, TSearchSpace, TProblem> Crossover { get; }
  public IMutator<TGenotype, TSearchSpace, TProblem> Mutator { get; }
  public double MutationRate { get; }
  public ISelector<TGenotype, TSearchSpace, TProblem> Selector { get; }
  public int Elites { get; }
  // public IReplacer<TGenotype, TSearchSpace, TProblem> Replacer { get; }

  private readonly AgedGenotypeCreator<TGenotype, TSearchSpace, TProblem> agedCreator;
  private readonly AgedCrossover<TGenotype, TSearchSpace, TProblem> agedCrossover;
  private readonly AgedMutator<TGenotype, TSearchSpace, TProblem> agedMutator;
  private readonly AgedSelector<TGenotype, TSearchSpace, TProblem> agedSelector;
  private readonly AgedReplacer<TGenotype, TSearchSpace, TProblem> agedReplacer;

  private readonly MultiMutator<TGenotype, TSearchSpace, TProblem> internalMutator;
  private readonly IReplacer<TGenotype, TSearchSpace, TProblem> internalReplacer;

  public OperatorMetric CreatorMetric { get; protected set; } = OperatorMetric.Zero;
  public OperatorMetric CrossoverMetric { get; protected set; } = OperatorMetric.Zero;
  public OperatorMetric MutationMetric { get; protected set; } = OperatorMetric.Zero;
  public OperatorMetric SelectionMetric { get; protected set; } = OperatorMetric.Zero;
  public OperatorMetric ReplacementMetric { get; protected set; } = OperatorMetric.Zero;

  public AlpsGeneticAlgorithm(
    int populationSize,
    ICreator<TGenotype, TSearchSpace, TProblem> creator,
    ICrossover<TGenotype, TSearchSpace, TProblem> crossover,
    IMutator<TGenotype, TSearchSpace, TProblem> mutator, double mutationRate,
    ISelector<TGenotype, TSearchSpace, TProblem> selector,
    IEvaluator<TGenotype, TSearchSpace, TProblem> evaluator,

    // IReplacer<TGenotype, TSearchSpace, TProblem> replacer,
    int elites,
    int? randomSeed,
    ITerminator<TGenotype, AlpsIterationResult<TGenotype>, TSearchSpace, TProblem> terminator,
    IInterceptor<TGenotype, AlpsIterationResult<TGenotype>, TSearchSpace, TProblem>? interceptor = null
  ) : base() {
    PopulationSize = populationSize;
    Creator = creator;
    Crossover = crossover;
    Mutator = mutator;
    MutationRate = mutationRate;
    Selector = selector;
    Evaluator = evaluator;
    //Replacer = replacer;
    Elites = elites;

    internalMutator = new MultiMutator<TGenotype, TSearchSpace, TProblem>([mutator, new NoChangeMutator<TGenotype>()], [mutationRate, 1 - mutationRate]);
    internalReplacer = new ElitismReplacer<TGenotype>(elites);

    agedCreator = new AgedGenotypeCreator<TGenotype, TSearchSpace, TProblem>(Creator);
    agedCrossover = new AgedCrossover<TGenotype, TSearchSpace, TProblem>(Crossover);
    agedMutator = new AgedMutator<TGenotype, TSearchSpace, TProblem>(internalMutator);
    agedSelector = new AgedSelector<TGenotype, TSearchSpace, TProblem>(Selector);
    agedReplacer = new AgedReplacer<TGenotype, TSearchSpace, TProblem>(internalReplacer);
  }

  public override AlpsIterationResult<TGenotype> ExecuteStep(TProblem problem, TSearchSpace searchSpace, AlpsIterationResult<TGenotype>? previousIterationResult, IRandomNumberGenerator random) {
    var agedProblem = new AgedProblem<TGenotype, TSearchSpace, TProblem>(problem);
    var agedSearchSpace = new AgedSearchSpace<TGenotype, TSearchSpace>(searchSpace);
    var iterationRandom = random.Spawn();
    return previousIterationResult switch {
      null => ExecuteInitialization(agedProblem, agedSearchSpace, iterationRandom),
      _ => ExecuteGeneration(agedProblem, agedSearchSpace, previousIterationResult, iterationRandom)
    };
  }

  protected virtual AlpsIterationResult<TGenotype> ExecuteInitialization(AgedProblem<TGenotype, TSearchSpace, TProblem> problem, AgedSearchSpace<TGenotype, TSearchSpace> searchSpace, IRandomNumberGenerator iterationRandom) {
    var startCreating = Stopwatch.GetTimestamp();
    var initialLayerPopulation = agedCreator.Create(PopulationSize, iterationRandom, searchSpace, problem);
    var endCreating = Stopwatch.GetTimestamp();
    CreatorMetric += new OperatorMetric(PopulationSize, Stopwatch.GetElapsedTime(startCreating, endCreating));

    var startEvaluating = Stopwatch.GetTimestamp();
    var fitnesses = Evaluator.Evaluate(initialLayerPopulation.Select(x => x.InnerGenotype).ToArray(), iterationRandom, searchSpace.InnerSearchSpace, problem.InnerProblem);
    var endEvaluating = Stopwatch.GetTimestamp();

    var result = new AlpsIterationResult<TGenotype>() { Population = [Population.From(initialLayerPopulation, fitnesses)] };

    return result;
  }

  protected virtual AlpsIterationResult<TGenotype> ExecuteGeneration(AgedProblem<TGenotype, TSearchSpace, TProblem> problem, AgedSearchSpace<TGenotype, TSearchSpace> searchSpace, AlpsIterationResult<TGenotype> previousGenerationResult, IRandomNumberGenerator iterationRandom) {
    int offspringCount = internalReplacer.GetOffspringCount(PopulationSize);

    var oldPopulation = previousGenerationResult.Population[0].ToArray();

    var startSelection = Stopwatch.GetTimestamp();
    var parents = agedSelector.Select(oldPopulation, problem.Objective, offspringCount * 2, iterationRandom, searchSpace, problem);
    var endSelection = Stopwatch.GetTimestamp();
    SelectionMetric += new OperatorMetric(parents.Count, Stopwatch.GetElapsedTime(startSelection, endSelection));

    var parentPairs = new IParents<AgedGenotype<TGenotype>>[offspringCount];
    for (int i = 0, j = 0; i < offspringCount; i++, j += 2) {
      parentPairs[i] = new Parents<AgedGenotype<TGenotype>>(parents[j].Genotype, parents[j + 1].Genotype);
    }

    var startCrossover = Stopwatch.GetTimestamp();
    int crossoverCount = 0;
    var population = agedCrossover.Cross(parentPairs, iterationRandom, searchSpace, problem);
    var endCrossover = Stopwatch.GetTimestamp();
    CrossoverMetric += new OperatorMetric(crossoverCount, Stopwatch.GetElapsedTime(startCrossover, endCrossover));

    var startMutation = Stopwatch.GetTimestamp();
    int mutationCount = 0;
    population = agedMutator.Mutate(population, iterationRandom, searchSpace, problem);
    var endMutation = Stopwatch.GetTimestamp();
    MutationMetric += new OperatorMetric(mutationCount, Stopwatch.GetElapsedTime(startMutation, endMutation));

    var fitnesses = Evaluator.Evaluate(population.Select(x => x.InnerGenotype).ToArray(), iterationRandom, searchSpace.InnerSearchSpace, problem.InnerProblem);

    var evaluatedPopulation = Population.From(population, fitnesses);

    var startReplacement = Stopwatch.GetTimestamp();
    var newPopulation = agedReplacer.Replace(oldPopulation, evaluatedPopulation.ToList(), problem.Objective, iterationRandom, searchSpace, problem);
    var endReplacement = Stopwatch.GetTimestamp();
    ReplacementMetric += new OperatorMetric(1, Stopwatch.GetElapsedTime(startReplacement, endReplacement));

    var result = new AlpsIterationResult<TGenotype>() {
      Population = [new Population<AgedGenotype<TGenotype>>(new ImmutableList<ISolution<AgedGenotype<TGenotype>>>(newPopulation))],
    };

    return result;
  }
}
