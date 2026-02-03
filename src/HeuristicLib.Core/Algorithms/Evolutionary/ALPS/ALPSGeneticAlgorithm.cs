using System.Diagnostics;
using HEAL.HeuristicLib.Collections;
using HEAL.HeuristicLib.OperatorExtensions.MeasuredOperators;
using HEAL.HeuristicLib.Operators.Creators;
using HEAL.HeuristicLib.Operators.Crossovers;
using HEAL.HeuristicLib.Operators.Evaluators;
using HEAL.HeuristicLib.Operators.Interceptors;
using HEAL.HeuristicLib.Operators.Mutators;
using HEAL.HeuristicLib.Operators.Replacers;
using HEAL.HeuristicLib.Operators.Selectors;
using HEAL.HeuristicLib.Operators.Terminators;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Algorithms.Evolutionary.ALPS;

public class AlpsGeneticAlgorithm<TGenotype, TSearchSpace, TProblem>
  : IterativeAlgorithm<TGenotype, TSearchSpace, TProblem, AlpsAlgorithmState<TGenotype>>
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
{
  // public IReplacer<TGenotype, TSearchSpace, TProblem> Replacer { get; }

  private readonly AgedGenotypeCreator<TGenotype, TSearchSpace, TProblem> agedCreator;
  private readonly AgedCrossover<TGenotype, TSearchSpace, TProblem> agedCrossover;
  private readonly AgedMutator<TGenotype, TSearchSpace, TProblem> agedMutator;
  private readonly AgedReplacer<TGenotype, TSearchSpace, TProblem> agedReplacer;
  private readonly AgedSelector<TGenotype, TSearchSpace, TProblem> agedSelector;

  private readonly IReplacer<TGenotype, TSearchSpace, TProblem> internalReplacer;

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
    ITerminator<TGenotype, AlpsAlgorithmState<TGenotype>, TSearchSpace, TProblem> terminator,
    IInterceptor<TGenotype, AlpsAlgorithmState<TGenotype>, TSearchSpace, TProblem>? interceptor = null
  )
  {
    PopulationSize = populationSize;
    Creator = creator;
    Crossover = crossover;
    Mutator = mutator;
    MutationRate = mutationRate;
    Selector = selector;
    Evaluator = evaluator;
    //Replacer = replacer;
    Elites = elites;

    var internalMutator1 = new MultiMutator<TGenotype, TSearchSpace, TProblem>([mutator, new NoChangeMutator<TGenotype>()], [mutationRate, 1 - mutationRate]);
    internalReplacer = new ElitismReplacer<TGenotype>(elites);

    agedCreator = new AgedGenotypeCreator<TGenotype, TSearchSpace, TProblem>(Creator);
    agedCrossover = new AgedCrossover<TGenotype, TSearchSpace, TProblem>(Crossover);
    agedMutator = new AgedMutator<TGenotype, TSearchSpace, TProblem>(internalMutator1);
    agedSelector = new AgedSelector<TGenotype, TSearchSpace, TProblem>(Selector);
    agedReplacer = new AgedReplacer<TGenotype, TSearchSpace, TProblem>(internalReplacer);
  }
  public int PopulationSize { get; }
  public ICrossover<TGenotype, TSearchSpace, TProblem> Crossover { get; }
  public IMutator<TGenotype, TSearchSpace, TProblem> Mutator { get; }
  public double MutationRate { get; }
  public ISelector<TGenotype, TSearchSpace, TProblem> Selector { get; }
  public int Elites { get; }

  public OperatorMetric CreatorMetric { get; protected set; } = OperatorMetric.Zero;
  public OperatorMetric CrossoverMetric { get; protected set; } = OperatorMetric.Zero;
  public OperatorMetric MutationMetric { get; protected set; } = OperatorMetric.Zero;
  public OperatorMetric SelectionMetric { get; protected set; } = OperatorMetric.Zero;
  public OperatorMetric ReplacementMetric { get; protected set; } = OperatorMetric.Zero;

  public override AlpsAlgorithmState<TGenotype> ExecuteStep(TProblem problem, TSearchSpace searchSpace, AlpsAlgorithmState<TGenotype>? previousIterationResult, IRandomNumberGenerator random)
  {
    var agedProblem = new AgedProblem<TGenotype, TSearchSpace, TProblem>(problem);
    var agedEncoding = new AgedSearchSpace<TGenotype, TSearchSpace>(searchSpace);
    var iterationRandom = random.Spawn();

    return previousIterationResult switch {
      null => ExecuteInitialization(agedProblem, agedEncoding, iterationRandom),
      _ => ExecuteGeneration(agedProblem, agedEncoding, previousIterationResult, iterationRandom)
    };
  }

  protected virtual AlpsAlgorithmState<TGenotype> ExecuteInitialization(AgedProblem<TGenotype, TSearchSpace, TProblem> problem, AgedSearchSpace<TGenotype, TSearchSpace> searchSpace, IRandomNumberGenerator iterationRandom)
  {
    var startCreating = Stopwatch.GetTimestamp();
    var initialLayerPopulation = agedCreator.Create(PopulationSize, iterationRandom, searchSpace, problem);
    var endCreating = Stopwatch.GetTimestamp();
    CreatorMetric += new OperatorMetric(PopulationSize, Stopwatch.GetElapsedTime(startCreating, endCreating));
    var fitnesses = Evaluator.Evaluate(initialLayerPopulation.Select(x => x.InnerGenotype).ToArray(), iterationRandom, searchSpace.InnerEncoding, problem.InnerProblem);
    var result = new AlpsAlgorithmState<TGenotype> { Population = [Population.From(initialLayerPopulation, fitnesses)] };

    return result;
  }

  protected virtual AlpsAlgorithmState<TGenotype> ExecuteGeneration(AgedProblem<TGenotype, TSearchSpace, TProblem> problem, AgedSearchSpace<TGenotype, TSearchSpace> searchSpace, AlpsAlgorithmState<TGenotype> previousGenerationResult, IRandomNumberGenerator iterationRandom)
  {
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

    var result = new AlpsAlgorithmState<TGenotype> {
      Population = [new Population<AgedGenotype<TGenotype>>(new ImmutableList<ISolution<AgedGenotype<TGenotype>>>(newPopulation))]
    };

    return result;
  }
}
