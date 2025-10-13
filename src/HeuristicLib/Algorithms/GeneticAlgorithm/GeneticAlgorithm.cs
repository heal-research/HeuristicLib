using System.Diagnostics;
using HEAL.HeuristicLib.Core;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Algorithms.GeneticAlgorithm;

public class GeneticAlgorithm<TGenotype, TEncoding, TProblem>
  : IterativeAlgorithm<TGenotype, TEncoding, TProblem, GeneticAlgorithmResult<TGenotype>, GeneticAlgorithmIterationResult<TGenotype>>
  where TEncoding : class, IEncoding<TGenotype>
  where TProblem : class, IProblem<TGenotype, TEncoding> {
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

  public override GeneticAlgorithmIterationResult<TGenotype> ExecuteStep(TProblem problem, TEncoding? searchSpace = null, GeneticAlgorithmIterationResult<TGenotype>? previousIterationResult = null, IRandomNumberGenerator? random = null) {
    if (searchSpace is ISubencodingComparable<TEncoding> s && !s.IsSubspaceOf(problem.SearchSpace))
      throw new ArgumentException("The provided search space is not a subspace of the problem's search space.");

    var iterationRandom = (random ?? algorithmRandom).Fork(CurrentIteration);
    return previousIterationResult switch {
      null => ExecuteInitialization(problem, searchSpace ?? problem.SearchSpace, iterationRandom),
      _ => ExecuteGeneration(problem, searchSpace ?? problem.SearchSpace, previousIterationResult, iterationRandom)
    };
  }

  protected virtual GeneticAlgorithmIterationResult<TGenotype> ExecuteInitialization(TProblem problem, TEncoding searchSpace, IRandomNumberGenerator iterationRandom) {
    var population = Creator.Create(PopulationSize, iterationRandom, searchSpace, problem);
    var fitnesses = problem.Evaluate(population);
    return new GeneticAlgorithmIterationResult<TGenotype>() { Population = Population.From(population, fitnesses) };
  }

  protected virtual GeneticAlgorithmIterationResult<TGenotype> ExecuteGeneration(TProblem problem, TEncoding searchSpace, GeneticAlgorithmIterationResult<TGenotype> previousGenerationResult, IRandomNumberGenerator iterationRandom) {
    int offspringCount = internalReplacer.GetOffspringCount(PopulationSize);
    var oldPopulation = previousGenerationResult.Population.ToArray();
    var parents = Selector.Select(oldPopulation, problem.Objective, offspringCount * 2, iterationRandom, searchSpace, problem);

    var parentPairs = new ValueTuple<TGenotype, TGenotype>[offspringCount];
    for (int i = 0, j = 0; i < offspringCount; i++, j += 2) {
      parentPairs[i] = (parents[j].Genotype, parents[j + 1].Genotype);
    }

    var population = Crossover.Cross(parentPairs, iterationRandom, searchSpace, problem);
    population = internalMutator.Mutate(population, iterationRandom, searchSpace, problem);
    var fitnesses = problem.Evaluate(population);
    var evaluatedPopulation = Population.From(population, fitnesses);
    var newPopulation = internalReplacer.Replace(oldPopulation, evaluatedPopulation.ToList(), problem.Objective, iterationRandom, searchSpace, problem);

    var result = new GeneticAlgorithmIterationResult<TGenotype> {
      Population = new Population<TGenotype>(new ImmutableList<Solution<TGenotype>>(newPopulation)),
    };

    return result;
  }

  protected override GeneticAlgorithmResult<TGenotype> FinalizeResult(GeneticAlgorithmIterationResult<TGenotype> iterationResult, TProblem problem) {
    return new GeneticAlgorithmResult<TGenotype>() { Population = iterationResult.Population };
  }
}

public class GeneticAlgorithm<TGenotype, TEncoding>(int populationSize, ICreator<TGenotype, TEncoding, IProblem<TGenotype, TEncoding>> creator, ICrossover<TGenotype, TEncoding, IProblem<TGenotype, TEncoding>> crossover, IMutator<TGenotype, TEncoding, IProblem<TGenotype, TEncoding>> mutator, double mutationRate, ISelector<TGenotype, TEncoding, IProblem<TGenotype, TEncoding>> selector, int elites, int randomSeed, ITerminator<TGenotype, GeneticAlgorithmIterationResult<TGenotype>, TEncoding, IProblem<TGenotype, TEncoding>> terminator, IInterceptor<TGenotype, GeneticAlgorithmIterationResult<TGenotype>, TEncoding, IProblem<TGenotype, TEncoding>>? interceptor = null)
  : GeneticAlgorithm<TGenotype, TEncoding, IProblem<TGenotype, TEncoding>>(populationSize, creator, crossover, mutator, mutationRate, selector, elites, randomSeed, terminator, interceptor)
  where TEncoding : class, IEncoding<TGenotype>;

public class GeneticAlgorithm<TGenotype>(int populationSize, ICreator<TGenotype, IEncoding<TGenotype>, IProblem<TGenotype, IEncoding<TGenotype>>> creator, ICrossover<TGenotype, IEncoding<TGenotype>, IProblem<TGenotype, IEncoding<TGenotype>>> crossover, IMutator<TGenotype, IEncoding<TGenotype>, IProblem<TGenotype, IEncoding<TGenotype>>> mutator, double mutationRate, ISelector<TGenotype, IEncoding<TGenotype>, IProblem<TGenotype, IEncoding<TGenotype>>> selector, int elites, int randomSeed, ITerminator<TGenotype, GeneticAlgorithmIterationResult<TGenotype>, IEncoding<TGenotype>, IProblem<TGenotype, IEncoding<TGenotype>>> terminator, IInterceptor<TGenotype, GeneticAlgorithmIterationResult<TGenotype>, IEncoding<TGenotype>, IProblem<TGenotype, IEncoding<TGenotype>>>? interceptor = null)
  : GeneticAlgorithm<TGenotype, IEncoding<TGenotype>>(populationSize, creator, crossover, mutator, mutationRate, selector, elites, randomSeed, terminator, interceptor);
