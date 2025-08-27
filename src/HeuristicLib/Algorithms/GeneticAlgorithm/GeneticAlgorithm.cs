using System.Diagnostics;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Algorithms.GeneticAlgorithm;

public record GeneticAlgorithmIterationResult<TGenotype> : IIterationResult<TGenotype>
{
  public required Population<TGenotype> Population { get; init; }
}

public record GeneticAlgorithmResult<TGenotype> : IAlgorithmResult<TGenotype>
{
  public required Population<TGenotype> Population { get; init; }
}

public class GeneticAlgorithm<TGenotype, TEncoding, TProblem>
  : IterativeAlgorithm<TGenotype, TEncoding, TProblem, GeneticAlgorithmResult<TGenotype>, GeneticAlgorithmIterationResult<TGenotype>>
   where TEncoding : class, IEncoding<TGenotype>
   where TProblem : class, IProblem<TGenotype, TEncoding>
{
  public int PopulationSize { get; }
  public ICreator<TGenotype, TEncoding, TProblem> Creator { get; }
  public ICrossover<TGenotype, TEncoding, TProblem> Crossover { get; }
  public IMutator<TGenotype, TEncoding, TProblem> Mutator { get; }
  public double MutationRate { get; }
  public ISelector<TGenotype, TEncoding, TProblem> Selector { get; }
  public int Elites { get; }
  // public IReplacer<TGenotype, TEncoding, TProblem> Replacer { get; }
  public int RandomSeed { get; }

  private readonly IRandomNumberGenerator algorithmRandom;
  private readonly MultiMutator<TGenotype, TEncoding, TProblem> internalMutator;
  private readonly IReplacer<TGenotype, TEncoding, TProblem> internalReplacer;

  public OperatorMetric CreatorMetric { get; protected set; } = OperatorMetric.Zero;
  public OperatorMetric CrossoverMetric { get; protected set; } = OperatorMetric.Zero;
  public OperatorMetric MutationMetric { get; protected set; } = OperatorMetric.Zero;
  public OperatorMetric SelectionMetric { get; protected set; } = OperatorMetric.Zero;
  public OperatorMetric ReplacementMetric { get; protected set; } = OperatorMetric.Zero;
  
  
  public GeneticAlgorithm(
    int populationSize,
    ICreator<TGenotype, TEncoding, TProblem> creator,
    ICrossover<TGenotype, TEncoding, TProblem> crossover,
    IMutator<TGenotype, TEncoding, TProblem> mutator, double mutationRate,
    ISelector<TGenotype, TEncoding, TProblem> selector,
    // IReplacer<TGenotype, TEncoding, TProblem> replacer,
    int elites,
    int? randomSeed,
    ITerminator<TGenotype, GeneticAlgorithmIterationResult<TGenotype>, TEncoding, TProblem> terminator,
    IInterceptor<TGenotype, GeneticAlgorithmIterationResult<TGenotype>, TEncoding, TProblem>? interceptor = null
  ) : base(terminator, interceptor) {
    PopulationSize = populationSize;
    Creator = creator;
    Crossover = crossover;
    Mutator = mutator;
    MutationRate = mutationRate;
    Selector = selector;
    //Replacer = replacer;
    Elites = elites;
    RandomSeed = randomSeed ?? SystemRandomNumberGenerator.RandomSeed();
    
    algorithmRandom = new SystemRandomNumberGenerator(RandomSeed);
    internalMutator = new MultiMutator<TGenotype, TEncoding, TProblem>([mutator, new NoChangeMutator<TGenotype>()], [mutationRate, 1 - mutationRate]);
    internalReplacer = new ElitismReplacer<TGenotype>(elites);
  }

  public override GeneticAlgorithmIterationResult<TGenotype> ExecuteStep(TProblem problem, GeneticAlgorithmIterationResult<TGenotype>? previousIterationResult = null, IRandomNumberGenerator? random = null) {
    var iterationRandom = (random ?? algorithmRandom).Fork(CurrentIteration);
    return previousIterationResult switch {
      null => ExecuteInitialization(problem, iterationRandom),
      _ => ExecuteGeneration(problem, previousIterationResult, iterationRandom)
    };
  }

  protected virtual GeneticAlgorithmIterationResult<TGenotype> ExecuteInitialization(TProblem problem, IRandomNumberGenerator iterationRandom) {
    var startCreating = Stopwatch.GetTimestamp();
    var population = Creator.Create(PopulationSize, iterationRandom, problem.Encoding, problem);
    var endCreating = Stopwatch.GetTimestamp();
    CreatorMetric += new OperatorMetric(PopulationSize, Stopwatch.GetElapsedTime(startCreating, endCreating));
    
    var startEvaluating = Stopwatch.GetTimestamp();
    var fitnesses = problem.Evaluate(population);
    var endEvaluating = Stopwatch.GetTimestamp();
    EvaluationsMetric += new OperatorMetric(PopulationSize, Stopwatch.GetElapsedTime(startEvaluating, endEvaluating));
    
    var result = new GeneticAlgorithmIterationResult<TGenotype>() {
      Population = Population.From(population, fitnesses)
    };

    return result;
  }
  
  protected virtual GeneticAlgorithmIterationResult<TGenotype> ExecuteGeneration(TProblem problem, GeneticAlgorithmIterationResult<TGenotype> previousGenerationResult, IRandomNumberGenerator iterationRandom) {
    int offspringCount = internalReplacer.GetOffspringCount(PopulationSize);

    var oldPopulation = previousGenerationResult.Population.ToArray();
    
    var startSelection = Stopwatch.GetTimestamp();
    var parents = Selector.Select(oldPopulation, problem.Objective, offspringCount * 2, iterationRandom, problem.Encoding, problem);
    var endSelection = Stopwatch.GetTimestamp();
    SelectionMetric += new OperatorMetric(parents.Count, Stopwatch.GetElapsedTime(startSelection, endSelection));
    
    var parentPairs = new ValueTuple<TGenotype, TGenotype>[offspringCount];
    for (int i = 0, j = 0; i < offspringCount; i++, j += 2) {
      parentPairs[i] = (parents[j].Genotype, parents[j + 1].Genotype);
    }

    var startCrossover = Stopwatch.GetTimestamp();
    int crossoverCount = 0;
    var population = Crossover.Cross(parentPairs, iterationRandom, problem.Encoding, problem);
    var endCrossover = Stopwatch.GetTimestamp();
    CrossoverMetric += new OperatorMetric(crossoverCount, Stopwatch.GetElapsedTime(startCrossover, endCrossover));
    
    var startMutation = Stopwatch.GetTimestamp();
    int mutationCount = 0;
    population = internalMutator.Mutate(population, iterationRandom, problem.Encoding, problem);
    var endMutation = Stopwatch.GetTimestamp();
    MutationMetric += new OperatorMetric(mutationCount, Stopwatch.GetElapsedTime(startMutation, endMutation));
    
    var startEvaluation = Stopwatch.GetTimestamp();
    var fitnesses = problem.Evaluate(population);
    var endEvaluation = Stopwatch.GetTimestamp();
    EvaluationsMetric += new OperatorMetric(fitnesses.Count, Stopwatch.GetElapsedTime(startEvaluation, endEvaluation));

    var evaluatedPopulation = Population.From(population, fitnesses);
    
    var startReplacement = Stopwatch.GetTimestamp();
    var newPopulation = internalReplacer.Replace(oldPopulation, evaluatedPopulation.ToList(), problem.Objective, iterationRandom, problem.Encoding, problem);
    var endReplacement = Stopwatch.GetTimestamp();
    ReplacementMetric += new OperatorMetric(1, Stopwatch.GetElapsedTime(startReplacement, endReplacement));
    
    var result = new GeneticAlgorithmIterationResult<TGenotype>() {
      Population = new Population<TGenotype>(new ImmutableList<Solution<TGenotype>>(newPopulation)),
    };

    return result;
  }

  protected override GeneticAlgorithmResult<TGenotype> FinalizeResult(GeneticAlgorithmIterationResult<TGenotype> iterationResult, TProblem problem) {
    return new GeneticAlgorithmResult<TGenotype>() {
      Population = iterationResult.Population
    };
  }
}

public class GeneticAlgorithm<TGenotype, TEncoding>
  : GeneticAlgorithm<TGenotype, TEncoding, IProblem<TGenotype, TEncoding>>
  where TEncoding : class, IEncoding<TGenotype>
{

  public GeneticAlgorithm(int populationSize, ICreator<TGenotype, TEncoding, IProblem<TGenotype, TEncoding>> creator, ICrossover<TGenotype, TEncoding, IProblem<TGenotype, TEncoding>> crossover, IMutator<TGenotype, TEncoding, IProblem<TGenotype, TEncoding>> mutator, double mutationRate, ISelector<TGenotype, TEncoding, IProblem<TGenotype, TEncoding>> selector, int elites, int randomSeed, ITerminator<TGenotype, GeneticAlgorithmIterationResult<TGenotype>, TEncoding, IProblem<TGenotype, TEncoding>> terminator, IInterceptor<TGenotype, GeneticAlgorithmIterationResult<TGenotype>, TEncoding, IProblem<TGenotype, TEncoding>>? interceptor = null)
    : base(populationSize, creator, crossover, mutator, mutationRate, selector, elites, randomSeed, terminator, interceptor) {
  }
}

public class GeneticAlgorithm<TGenotype>
  : GeneticAlgorithm<TGenotype, IEncoding<TGenotype>>
{

  public GeneticAlgorithm(int populationSize, ICreator<TGenotype, IEncoding<TGenotype>, IProblem<TGenotype, IEncoding<TGenotype>>> creator, ICrossover<TGenotype, IEncoding<TGenotype>, IProblem<TGenotype, IEncoding<TGenotype>>> crossover, IMutator<TGenotype, IEncoding<TGenotype>, IProblem<TGenotype, IEncoding<TGenotype>>> mutator, double mutationRate, ISelector<TGenotype, IEncoding<TGenotype>, IProblem<TGenotype, IEncoding<TGenotype>>> selector, int elites, int randomSeed, ITerminator<TGenotype, GeneticAlgorithmIterationResult<TGenotype>, IEncoding<TGenotype>, IProblem<TGenotype, IEncoding<TGenotype>>> terminator, IInterceptor<TGenotype, GeneticAlgorithmIterationResult<TGenotype>, IEncoding<TGenotype>, IProblem<TGenotype, IEncoding<TGenotype>>>? interceptor = null) 
    : base(populationSize, creator, crossover, mutator, mutationRate, selector, elites, randomSeed, terminator, interceptor) {
  }
}
