using System.Diagnostics;
using HEAL.HeuristicLib.Collections;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Operators.Creator;
using HEAL.HeuristicLib.Operators.Crossover;
using HEAL.HeuristicLib.Operators.Evaluator;
using HEAL.HeuristicLib.Operators.Interceptor;
using HEAL.HeuristicLib.Operators.Mutator;
using HEAL.HeuristicLib.Operators.Replacer;
using HEAL.HeuristicLib.Operators.Selector;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Algorithms.ALPS;

public class ALPSGeneticAlgorithm<TGenotype, TEncoding, TProblem>
  : IterativeAlgorithm<TGenotype, TEncoding, TProblem, ALPSResult<TGenotype>, ALPSIterationResult<TGenotype>>
  where TEncoding : class, IEncoding<TGenotype>
  where TProblem : class, IProblem<TGenotype, TEncoding> {
  public int PopulationSize { get; }
  public ICreator<TGenotype, TEncoding, TProblem> Creator { get; }
  public ICrossover<TGenotype, TEncoding, TProblem> Crossover { get; }
  public IMutator<TGenotype, TEncoding, TProblem> Mutator { get; }
  public double MutationRate { get; }
  public ISelector<TGenotype, TEncoding, TProblem> Selector { get; }
  public IEvaluator<TGenotype, TEncoding, TProblem> Evaluator { get; }
  public int Elites { get; }
  // public IReplacer<TGenotype, TEncoding, TProblem> Replacer { get; }

  private readonly AgedGenotypeCreator<TGenotype, TEncoding, TProblem> agedCreator;
  private readonly AgedCrossover<TGenotype, TEncoding, TProblem> agedCrossover;
  private readonly AgedMutator<TGenotype, TEncoding, TProblem> agedMutator;
  private readonly AgedSelector<TGenotype, TEncoding, TProblem> agedSelector;
  private readonly AgedReplacer<TGenotype, TEncoding, TProblem> agedReplacer;

  private readonly MultiMutator<TGenotype, TEncoding, TProblem> internalMutator;
  private readonly IReplacer<TGenotype, TEncoding, TProblem> internalReplacer;

  public OperatorMetric CreatorMetric { get; protected set; } = OperatorMetric.Zero;
  public OperatorMetric CrossoverMetric { get; protected set; } = OperatorMetric.Zero;
  public OperatorMetric MutationMetric { get; protected set; } = OperatorMetric.Zero;
  public OperatorMetric SelectionMetric { get; protected set; } = OperatorMetric.Zero;
  public OperatorMetric ReplacementMetric { get; protected set; } = OperatorMetric.Zero;

  public ALPSGeneticAlgorithm(
    int populationSize,
    ICreator<TGenotype, TEncoding, TProblem> creator,
    ICrossover<TGenotype, TEncoding, TProblem> crossover,
    IMutator<TGenotype, TEncoding, TProblem> mutator, double mutationRate,
    ISelector<TGenotype, TEncoding, TProblem> selector,
    IEvaluator<TGenotype, TEncoding, TProblem> evaluator,

    // IReplacer<TGenotype, TEncoding, TProblem> replacer,
    int elites,
    int? randomSeed,
    ITerminator<TGenotype, ALPSIterationResult<TGenotype>, TEncoding, TProblem> terminator,
    IInterceptor<TGenotype, ALPSIterationResult<TGenotype>, TEncoding, TProblem>? interceptor = null
  ) : base(terminator, randomSeed, interceptor) {
    PopulationSize = populationSize;
    Creator = creator;
    Crossover = crossover;
    Mutator = mutator;
    MutationRate = mutationRate;
    Selector = selector;
    Evaluator = evaluator;
    //Replacer = replacer;
    Elites = elites;

    internalMutator = new MultiMutator<TGenotype, TEncoding, TProblem>([mutator, new NoChangeMutator<TGenotype>()], [mutationRate, 1 - mutationRate]);
    internalReplacer = new ElitismReplacer<TGenotype>(elites);

    agedCreator = new AgedGenotypeCreator<TGenotype, TEncoding, TProblem>(Creator);
    agedCrossover = new AgedCrossover<TGenotype, TEncoding, TProblem>(Crossover);
    agedMutator = new AgedMutator<TGenotype, TEncoding, TProblem>(internalMutator);
    agedSelector = new AgedSelector<TGenotype, TEncoding, TProblem>(Selector);
    agedReplacer = new AgedReplacer<TGenotype, TEncoding, TProblem>(internalReplacer);
  }

  public override ALPSIterationResult<TGenotype> ExecuteStep(TProblem problem, TEncoding searchSpace, ALPSIterationResult<TGenotype>? previousIterationResult, IRandomNumberGenerator random) {
    var agedProblem = new AgedProblem<TGenotype, TEncoding, TProblem>(problem);
    var agedEncoding = new AgedEncoding<TGenotype, TEncoding>(searchSpace);
    var iterationRandom = random.Spawn();
    return previousIterationResult switch {
      null => ExecuteInitialization(agedProblem, agedEncoding, iterationRandom),
      _ => ExecuteGeneration(agedProblem, agedEncoding, previousIterationResult, iterationRandom)
    };
  }

  protected virtual ALPSIterationResult<TGenotype> ExecuteInitialization(AgedProblem<TGenotype, TEncoding, TProblem> problem, AgedEncoding<TGenotype, TEncoding> searchSpace, IRandomNumberGenerator iterationRandom) {
    var startCreating = Stopwatch.GetTimestamp();
    var initialLayerPopulation = agedCreator.Create(PopulationSize, iterationRandom, searchSpace, problem);
    var endCreating = Stopwatch.GetTimestamp();
    CreatorMetric += new OperatorMetric(PopulationSize, Stopwatch.GetElapsedTime(startCreating, endCreating));

    var startEvaluating = Stopwatch.GetTimestamp();
    var fitnesses = Evaluator.Evaluate(initialLayerPopulation.Select(x => x.InnerGenotype).ToArray(), iterationRandom, searchSpace.InnerEncoding, problem.InnerProblem);
    var endEvaluating = Stopwatch.GetTimestamp();
    EvaluationsMetric += new OperatorMetric(PopulationSize, Stopwatch.GetElapsedTime(startEvaluating, endEvaluating));

    var result = new ALPSIterationResult<TGenotype>() { Population = [Population.From(initialLayerPopulation, fitnesses)] };

    return result;
  }

  protected virtual ALPSIterationResult<TGenotype> ExecuteGeneration(AgedProblem<TGenotype, TEncoding, TProblem> problem, AgedEncoding<TGenotype, TEncoding> searchSpace, ALPSIterationResult<TGenotype> previousGenerationResult, IRandomNumberGenerator iterationRandom) {
    int offspringCount = internalReplacer.GetOffspringCount(PopulationSize);

    var oldPopulation = previousGenerationResult.Population[0].ToArray();

    var startSelection = Stopwatch.GetTimestamp();
    var parents = agedSelector.Select(oldPopulation, problem.Objective, offspringCount * 2, iterationRandom, searchSpace, problem);
    var endSelection = Stopwatch.GetTimestamp();
    SelectionMetric += new OperatorMetric(parents.Count, Stopwatch.GetElapsedTime(startSelection, endSelection));

    var parentPairs = new ValueTuple<AgedGenotype<TGenotype>, AgedGenotype<TGenotype>>[offspringCount];
    for (int i = 0, j = 0; i < offspringCount; i++, j += 2) {
      parentPairs[i] = (parents[j].Genotype, parents[j + 1].Genotype);
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

    var startEvaluation = Stopwatch.GetTimestamp();
    var fitnesses = Evaluator.Evaluate(population.Select(x => x.InnerGenotype).ToArray(), iterationRandom, searchSpace.InnerEncoding, problem.InnerProblem);
    var endEvaluation = Stopwatch.GetTimestamp();
    EvaluationsMetric += new OperatorMetric(fitnesses.Count, Stopwatch.GetElapsedTime(startEvaluation, endEvaluation));

    var evaluatedPopulation = Population.From(population, fitnesses);

    var startReplacement = Stopwatch.GetTimestamp();
    var newPopulation = agedReplacer.Replace(oldPopulation, evaluatedPopulation.ToList(), problem.Objective, iterationRandom, searchSpace, problem);
    var endReplacement = Stopwatch.GetTimestamp();
    ReplacementMetric += new OperatorMetric(1, Stopwatch.GetElapsedTime(startReplacement, endReplacement));

    var result = new ALPSIterationResult<TGenotype>() {
      Population = [new Population<AgedGenotype<TGenotype>>(new ImmutableList<Solution<AgedGenotype<TGenotype>>>(newPopulation))],
    };

    return result;
  }

  protected override ALPSResult<TGenotype> FinalizeResult(ALPSIterationResult<TGenotype> iterationResult, TProblem problem) {
    return new ALPSResult<TGenotype>() { Population = iterationResult.Population };
  }
}
