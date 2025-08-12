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
   where TEncoding : IEncoding<TGenotype>
   where TProblem : IProblem<TGenotype, TEncoding>
{
  public int PopulationSize { get; }
  public ICreator<TGenotype, TEncoding, TProblem> Creator { get; }
  public ICrossover<TGenotype, TEncoding, TProblem> Crossover { get; }
  public IMutator<TGenotype, TEncoding, TProblem> Mutator { get; }
  public double MutationRate { get; }
  public ISelector<TGenotype, TEncoding, TProblem> Selector { get; }
  public IReplacer<TGenotype, TEncoding, TProblem> Replacer { get; }
  public int RandomSeed { get; }

  private readonly SystemRandomNumberGenerator random;

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
    IReplacer<TGenotype, TEncoding, TProblem> replacer,
    int randomSeed,
    ITerminator<TGenotype, GeneticAlgorithmIterationResult<TGenotype>, TEncoding, TProblem> terminator,
    IInterceptor<TGenotype, GeneticAlgorithmIterationResult<TGenotype>, TEncoding, TProblem>? interceptor = null
  ) : base(terminator, interceptor) {
    PopulationSize = populationSize;
    Creator = creator;
    Crossover = crossover;
    Mutator = mutator;
    MutationRate = mutationRate;
    Selector = selector;
    Replacer = replacer;
    RandomSeed = randomSeed;
    
    random = new SystemRandomNumberGenerator(randomSeed);
  }

  public override GeneticAlgorithmIterationResult<TGenotype> ExecuteStep(TProblem problem, GeneticAlgorithmIterationResult<TGenotype>? previousIterationResult = null) {
    var context = new ExecutionContext<TEncoding, TProblem>(random, problem.Encoding, problem);
    return previousIterationResult switch {
      null => ExecuteInitialization(problem, context),
      _ => ExecuteGeneration(problem, previousIterationResult, context)
    };
  }

  protected virtual GeneticAlgorithmIterationResult<TGenotype> ExecuteInitialization(TProblem problem, ExecutionContext<TEncoding, TProblem> context) {
    var startCreating = Stopwatch.GetTimestamp();
    var population = Creator.Create(PopulationSize, context);
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
  
  protected virtual GeneticAlgorithmIterationResult<TGenotype> ExecuteGeneration(TProblem problem, GeneticAlgorithmIterationResult<TGenotype> previousGenerationResult, ExecutionContext<TEncoding, TProblem> context) {
    int offspringCount = Replacer.GetOffspringCount(PopulationSize);

    var oldPopulation = previousGenerationResult.Population.ToArray();
    
    var startSelection = Stopwatch.GetTimestamp();
    var parents = Selector.Select(oldPopulation, problem.Objective, offspringCount * 2, random, problem.Encoding, problem);
    var endSelection = Stopwatch.GetTimestamp();
    SelectionMetric += new OperatorMetric(parents.Count, Stopwatch.GetElapsedTime(startSelection, endSelection));
    
    var parentPairs = new ValueTuple<TGenotype, TGenotype>[offspringCount];
    for (int i = 0, j = 0; i < offspringCount; i++, j += 2) {
      parentPairs[i] = (parents[j].Genotype, parents[j + 1].Genotype);
    }

    var startCrossover = Stopwatch.GetTimestamp();
    int crossoverCount = 0;
    var population = Crossover.Cross(parentPairs, random, problem.Encoding, problem);
    var endCrossover = Stopwatch.GetTimestamp();
    CrossoverMetric += new OperatorMetric(crossoverCount, Stopwatch.GetElapsedTime(startCrossover, endCrossover));
    
    var startMutation = Stopwatch.GetTimestamp();
    var mutatedGenotypes = new TGenotype[offspringCount];
    int mutationCount = 0;
    // ToDo: use a MultiMutator with the "actual" mutator, biased by the mutation rate
    population = Mutator.Mutate(population, random, problem.Encoding, problem);
    //
    // for (int i = 0; i < population.Count; i++) {
    //   if (random.Random() < MutationRate) {
    //     mutatedGenotypes[i] = Mutator.Mutate([population[i]], random, problem.Encoding, problem)[0];
    //     mutationCount++;
    //   }
    // }
    var endMutation = Stopwatch.GetTimestamp();
    MutationMetric += new OperatorMetric(mutationCount, Stopwatch.GetElapsedTime(startMutation, endMutation));
    
    var startEvaluation = Stopwatch.GetTimestamp();
    var fitnesses = problem.Evaluate(mutatedGenotypes);
    var endEvaluation = Stopwatch.GetTimestamp();
    EvaluationsMetric += new OperatorMetric(fitnesses.Count, Stopwatch.GetElapsedTime(startEvaluation, endEvaluation));

    var evaluatedPopulation = Population.From(population, fitnesses);
    
    var startReplacement = Stopwatch.GetTimestamp();
    var newPopulation = Replacer.Replace(oldPopulation, evaluatedPopulation.ToList(), problem.Objective, random, problem.Encoding, problem);
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
  where TEncoding : IEncoding<TGenotype>
{

  public GeneticAlgorithm(int populationSize, ICreator<TGenotype, TEncoding, IProblem<TGenotype, TEncoding>> creator, ICrossover<TGenotype, TEncoding, IProblem<TGenotype, TEncoding>> crossover, IMutator<TGenotype, TEncoding, IProblem<TGenotype, TEncoding>> mutator, double mutationRate, ISelector<TGenotype, TEncoding, IProblem<TGenotype, TEncoding>> selector, IReplacer<TGenotype, TEncoding, IProblem<TGenotype, TEncoding>> replacer, int randomSeed, ITerminator<TGenotype, GeneticAlgorithmIterationResult<TGenotype>, TEncoding, IProblem<TGenotype, TEncoding>> terminator, IInterceptor<TGenotype, GeneticAlgorithmIterationResult<TGenotype>, TEncoding, IProblem<TGenotype, TEncoding>>? interceptor = null)
    : base(populationSize, creator, crossover, mutator, mutationRate, selector, replacer, randomSeed, terminator, interceptor) {
  }
}

public class GeneticAlgorithm<TGenotype>
  : GeneticAlgorithm<TGenotype, IEncoding<TGenotype>>
{

  public GeneticAlgorithm(int populationSize, ICreator<TGenotype, IEncoding<TGenotype>, IProblem<TGenotype, IEncoding<TGenotype>>> creator, ICrossover<TGenotype, IEncoding<TGenotype>, IProblem<TGenotype, IEncoding<TGenotype>>> crossover, IMutator<TGenotype, IEncoding<TGenotype>, IProblem<TGenotype, IEncoding<TGenotype>>> mutator, double mutationRate, ISelector<TGenotype, IEncoding<TGenotype>, IProblem<TGenotype, IEncoding<TGenotype>>> selector, IReplacer<TGenotype, IEncoding<TGenotype>, IProblem<TGenotype, IEncoding<TGenotype>>> replacer, int randomSeed, ITerminator<TGenotype, GeneticAlgorithmIterationResult<TGenotype>, IEncoding<TGenotype>, IProblem<TGenotype, IEncoding<TGenotype>>> terminator, IInterceptor<TGenotype, GeneticAlgorithmIterationResult<TGenotype>, IEncoding<TGenotype>, IProblem<TGenotype, IEncoding<TGenotype>>>? interceptor = null) 
    : base(populationSize, creator, crossover, mutator, mutationRate, selector, replacer, randomSeed, terminator, interceptor) {
  }
}
