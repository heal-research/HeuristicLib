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
    return previousIterationResult switch {
      null => ExecuteInitialization(problem),
      _ => ExecuteGeneration(problem, previousIterationResult)
    };
  }

  protected virtual GeneticAlgorithmIterationResult<TGenotype> ExecuteInitialization(TProblem problem) {
    var startCreating = Stopwatch.GetTimestamp();
    var genotypes = Enumerable.Range(0, PopulationSize).Select(i => Creator.Create(random, problem.Encoding, problem)).ToArray();
    var endCreating = Stopwatch.GetTimestamp();
    CreatorMetric += new OperatorMetric(PopulationSize, Stopwatch.GetElapsedTime(startCreating, endCreating));
    
    var startEvaluating = Stopwatch.GetTimestamp();
    var fitnesses = genotypes.Select(genotype => problem.Evaluate(genotype)).ToArray();
    var endEvaluating = Stopwatch.GetTimestamp();
    EvaluationsMetric += new OperatorMetric(genotypes.Length, Stopwatch.GetElapsedTime(startEvaluating, endEvaluating));

    var population = Population.From(genotypes, fitnesses);
    
    var result = new GeneticAlgorithmIterationResult<TGenotype>() {
      Population = population
    };

    return result;
  }
  
  protected virtual GeneticAlgorithmIterationResult<TGenotype> ExecuteGeneration(TProblem problem, GeneticAlgorithmIterationResult<TGenotype> previousGenerationResult) {
    int offspringCount = Replacer.GetOffspringCount(PopulationSize);

    var oldPopulation = previousGenerationResult.Population.ToList();
    
    var startSelection = Stopwatch.GetTimestamp();
    var parents = Selector.Select(oldPopulation, problem.Objective, offspringCount * 2, random, problem.Encoding, problem).ToList();
    var endSelection = Stopwatch.GetTimestamp();
    SelectionMetric += new OperatorMetric(parents.Count, Stopwatch.GetElapsedTime(startSelection, endSelection));
     
    var genotypes = new TGenotype[offspringCount];
    var startCrossover = Stopwatch.GetTimestamp();
    int crossoverCount = 0;
    for (int i = 0; i < parents.Count; i += 2) {
      var parent1 = parents[i];
      var parent2 = parents[i + 1];
      var child = Crossover.Cross(parent1.Genotype, parent2.Genotype, random, problem.Encoding, problem);
      genotypes[i / 2] = child;
      crossoverCount++;
    }
    var endCrossover = Stopwatch.GetTimestamp();
    CrossoverMetric += new OperatorMetric(crossoverCount, Stopwatch.GetElapsedTime(startCrossover, endCrossover));
    
    var startMutation = Stopwatch.GetTimestamp();
    int mutationCount = 0;
    for (int i = 0; i < genotypes.Length; i++) {
      if (random.Random() < MutationRate) {
        genotypes[i] = Mutator.Mutate(genotypes[i], random, problem.Encoding, problem);
        mutationCount++;
      }
    }
    var endMutation = Stopwatch.GetTimestamp();
    MutationMetric += new OperatorMetric(mutationCount, Stopwatch.GetElapsedTime(startMutation, endMutation));
    
    var startEvaluation = Stopwatch.GetTimestamp();
    var fitnesses = genotypes.Select(genotype => problem.Evaluate(genotype)).ToArray();
    var endEvaluation = Stopwatch.GetTimestamp();
    EvaluationsMetric += new OperatorMetric(fitnesses.Length, Stopwatch.GetElapsedTime(startEvaluation, endEvaluation));
    
    var population = Population.From(genotypes, fitnesses).ToList();
    
    var startReplacement = Stopwatch.GetTimestamp();
    var newPopulation = Replacer.Replace(oldPopulation, population, problem.Objective, random, problem.Encoding, problem);
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
