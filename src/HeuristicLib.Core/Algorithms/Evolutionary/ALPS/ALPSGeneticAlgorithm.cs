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

namespace HEAL.HeuristicLib.Algorithms.ALPS;

public class AlpsGeneticAlgorithm<TGenotype, TEncoding, TProblem>
  : IterativeAlgorithm<TGenotype, TEncoding, TProblem, AlpsAlgorithmState<TGenotype>>
  where TEncoding : class, IEncoding<TGenotype>
  where TProblem : class, IProblem<TGenotype, TEncoding> {
  public int PopulationSize { get; }
  public ICrossover<TGenotype, TEncoding, TProblem> Crossover { get; }
  public IMutator<TGenotype, TEncoding, TProblem> Mutator { get; }
  public double MutationRate { get; }
  public ISelector<TGenotype, TEncoding, TProblem> Selector { get; }
  public int Elites { get; }
  // public IReplacer<TGenotype, TEncoding, TProblem> Replacer { get; }

  private readonly AgedGenotypeCreator<TGenotype, TEncoding, TProblem> agedCreator;
  private readonly AgedCrossover<TGenotype, TEncoding, TProblem> agedCrossover;
  private readonly AgedMutator<TGenotype, TEncoding, TProblem> agedMutator;
  private readonly AgedSelector<TGenotype, TEncoding, TProblem> agedSelector;
  private readonly AgedReplacer<TGenotype, TEncoding, TProblem> agedReplacer;

  private readonly IReplacer<TGenotype, TEncoding, TProblem> internalReplacer;

  public OperatorMetric CreatorMetric { get; protected set; } = OperatorMetric.Zero;
  public OperatorMetric CrossoverMetric { get; protected set; } = OperatorMetric.Zero;
  public OperatorMetric MutationMetric { get; protected set; } = OperatorMetric.Zero;
  public OperatorMetric SelectionMetric { get; protected set; } = OperatorMetric.Zero;
  public OperatorMetric ReplacementMetric { get; protected set; } = OperatorMetric.Zero;

  public AlpsGeneticAlgorithm(
    int populationSize,
    ICreator<TGenotype, TEncoding, TProblem> creator,
    ICrossover<TGenotype, TEncoding, TProblem> crossover,
    IMutator<TGenotype, TEncoding, TProblem> mutator, double mutationRate,
    ISelector<TGenotype, TEncoding, TProblem> selector,
    IEvaluator<TGenotype, TEncoding, TProblem> evaluator,

    // IReplacer<TGenotype, TEncoding, TProblem> replacer,
    int elites,
    int? randomSeed,
    ITerminator<TGenotype, AlpsAlgorithmState<TGenotype>, TEncoding, TProblem> terminator,
    IInterceptor<TGenotype, AlpsAlgorithmState<TGenotype>, TEncoding, TProblem>? interceptor = null
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

    var internalMutator1 = new MultiMutator<TGenotype, TEncoding, TProblem>([mutator, new NoChangeMutator<TGenotype>()], [mutationRate, 1 - mutationRate]);
    internalReplacer = new ElitismReplacer<TGenotype>(elites);

    agedCreator = new AgedGenotypeCreator<TGenotype, TEncoding, TProblem>(Creator);
    agedCrossover = new AgedCrossover<TGenotype, TEncoding, TProblem>(Crossover);
    agedMutator = new AgedMutator<TGenotype, TEncoding, TProblem>(internalMutator1);
    agedSelector = new AgedSelector<TGenotype, TEncoding, TProblem>(Selector);
    agedReplacer = new AgedReplacer<TGenotype, TEncoding, TProblem>(internalReplacer);
  }

  public override AlpsAlgorithmState<TGenotype> ExecuteStep(TProblem problem, TEncoding searchSpace, AlpsAlgorithmState<TGenotype>? previousIterationResult, IRandomNumberGenerator random) {
    var agedProblem = new AgedProblem<TGenotype, TEncoding, TProblem>(problem);
    var agedEncoding = new AgedSearchSpace<TGenotype, TEncoding>(searchSpace);
    var iterationRandom = random.Spawn();
    return previousIterationResult switch {
      null => ExecuteInitialization(agedProblem, agedEncoding, iterationRandom),
      _ => ExecuteGeneration(agedProblem, agedEncoding, previousIterationResult, iterationRandom)
    };
  }

  protected virtual AlpsAlgorithmState<TGenotype> ExecuteInitialization(AgedProblem<TGenotype, TEncoding, TProblem> problem, AgedSearchSpace<TGenotype, TEncoding> searchSpace, IRandomNumberGenerator iterationRandom) {
    var startCreating = Stopwatch.GetTimestamp();
    var initialLayerPopulation = agedCreator.Create(PopulationSize, iterationRandom, searchSpace, problem);
    var endCreating = Stopwatch.GetTimestamp();
    CreatorMetric += new OperatorMetric(PopulationSize, Stopwatch.GetElapsedTime(startCreating, endCreating));
    var fitnesses = Evaluator.Evaluate(initialLayerPopulation.Select(x => x.InnerGenotype).ToArray(), iterationRandom, searchSpace.InnerEncoding, problem.InnerProblem);
    var result = new AlpsAlgorithmState<TGenotype>() { Population = [Population.From(initialLayerPopulation, fitnesses)] };

    return result;
  }

  protected virtual AlpsAlgorithmState<TGenotype> ExecuteGeneration(AgedProblem<TGenotype, TEncoding, TProblem> problem, AgedSearchSpace<TGenotype, TEncoding> searchSpace, AlpsAlgorithmState<TGenotype> previousGenerationResult, IRandomNumberGenerator iterationRandom) {
    var offspringCount = internalReplacer.GetOffspringCount(PopulationSize);

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
    var crossoverCount = 0;
    var population = agedCrossover.Cross(parentPairs, iterationRandom, searchSpace, problem);
    var endCrossover = Stopwatch.GetTimestamp();
    CrossoverMetric += new OperatorMetric(crossoverCount, Stopwatch.GetElapsedTime(startCrossover, endCrossover));

    var startMutation = Stopwatch.GetTimestamp();
    var mutationCount = 0;
    population = agedMutator.Mutate(population, iterationRandom, searchSpace, problem);
    var endMutation = Stopwatch.GetTimestamp();
    MutationMetric += new OperatorMetric(mutationCount, Stopwatch.GetElapsedTime(startMutation, endMutation));

    var fitnesses = Evaluator.Evaluate(population.Select(x => x.InnerGenotype).ToArray(), iterationRandom, searchSpace.InnerEncoding, problem.InnerProblem);

    var evaluatedPopulation = Population.From(population, fitnesses);

    var startReplacement = Stopwatch.GetTimestamp();
    var newPopulation = agedReplacer.Replace(oldPopulation, evaluatedPopulation.ToList(), problem.Objective, iterationRandom, searchSpace, problem);
    var endReplacement = Stopwatch.GetTimestamp();
    ReplacementMetric += new OperatorMetric(1, Stopwatch.GetElapsedTime(startReplacement, endReplacement));

    var result = new AlpsAlgorithmState<TGenotype>() {
      Population = [new Population<AgedGenotype<TGenotype>>(new ImmutableList<ISolution<AgedGenotype<TGenotype>>>(newPopulation))],
    };

    return result;
  }
}
