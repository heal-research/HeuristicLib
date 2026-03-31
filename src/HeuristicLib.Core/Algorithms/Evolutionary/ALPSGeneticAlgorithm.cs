using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Operators.Mutators;
using HEAL.HeuristicLib.Operators.Replacers;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Algorithms.Evolutionary;

public record AlpsState<TGenotype> : AlgorithmState
{
  public required IReadOnlyList<Population<TGenotype>> Population { get; init; }
  public required IReadOnlyList<IReadOnlyList<int>> Ages { get; init; }
}

public record AlpsGeneticAlgorithm<TGenotype, TSearchSpace, TProblem>
  : IterativeAlgorithm<TGenotype, TSearchSpace, TProblem, AlpsState<TGenotype>>
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
{
  public required int PopulationSize { get; init; }
  public required ICreator<TGenotype, TSearchSpace, TProblem> Creator { get; init; }
  public required ICrossover<TGenotype, TSearchSpace, TProblem> Crossover { get; init; }
  public required IMutator<TGenotype, TSearchSpace, TProblem> Mutator { get; init; }
  public required double MutationRate { get; init; }
  public required ISelector<TGenotype, TSearchSpace, TProblem> Selector { get; init; }

  public int Elites { get; init; }
  // public IReplacer<TGenotype, TSearchSpace, TProblem> Replacer { get; }

  // ToDo: this is not yet correctly set.
  // private readonly MultiMutator<TGenotype, TSearchSpace, TProblem> internalMutator = new([]); 
  // private readonly ElitismReplacer<TGenotype> internalReplacer = new(1);

  public override AlpsGeneticAlgorithmInstance<TGenotype, TSearchSpace, TProblem> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry)
  {
    var internalMutator = new ChooseOneMutator<TGenotype, TSearchSpace, TProblem>([Mutator, new NoChangeMutator<TGenotype>()], [MutationRate, 1 - MutationRate]);
    var internalReplacer = new ElitismReplacer<TGenotype>(Elites);

    var interceptorInstance = Interceptor is not null ? instanceRegistry.Resolve(Interceptor) : null;
    var evaluatorInstance = instanceRegistry.Resolve(Evaluator);
    var creatorInstance = instanceRegistry.Resolve(Creator);
    var crossoverInstance = instanceRegistry.Resolve(Crossover);
    var mutatorInstance = instanceRegistry.Resolve(internalMutator);
    var selectorInstance = instanceRegistry.Resolve(Selector);
    var replacerInstance = instanceRegistry.Resolve(internalReplacer);

    return new AlpsGeneticAlgorithmInstance<TGenotype, TSearchSpace, TProblem>(
      interceptorInstance,
      evaluatorInstance,
      PopulationSize,
      creatorInstance,
      crossoverInstance,
      mutatorInstance,
      selectorInstance,
      replacerInstance
    );
  }
}

public class AlpsGeneticAlgorithmInstance<TGenotype, TSearchSpace, TProblem>
  : IterativeAlgorithmInstance<TGenotype, TSearchSpace, TProblem, AlpsState<TGenotype>>
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
{
  private readonly int PopulationSize;
  private readonly ICreatorInstance<TGenotype, TSearchSpace, TProblem> agedCreator;
  private readonly ICrossoverInstance<TGenotype, TSearchSpace, TProblem> agedCrossover;
  private readonly IMutatorInstance<TGenotype, TSearchSpace, TProblem> agedMutator;
  private readonly ISelectorInstance<TGenotype, TSearchSpace, TProblem> agedSelector;
  private readonly IReplacerInstance<TGenotype, TSearchSpace, TProblem> agedReplacer;

  public AlpsGeneticAlgorithmInstance(IInterceptorInstance<TGenotype, TSearchSpace, TProblem, AlpsState<TGenotype>>? interceptor, IEvaluatorInstance<TGenotype, TSearchSpace, TProblem> evaluator, int populationSize, ICreatorInstance<TGenotype, TSearchSpace, TProblem> agedCreator, ICrossoverInstance<TGenotype, TSearchSpace, TProblem> agedCrossover, IMutatorInstance<TGenotype, TSearchSpace, TProblem> agedMutator, ISelectorInstance<TGenotype, TSearchSpace, TProblem> agedSelector, IReplacerInstance<TGenotype, TSearchSpace, TProblem> agedReplacer)
    : base(interceptor, evaluator)
  {
    PopulationSize = populationSize;
    this.agedCreator = agedCreator;
    this.agedCrossover = agedCrossover;
    this.agedMutator = agedMutator;
    this.agedSelector = agedSelector;
    this.agedReplacer = agedReplacer;
  }

  public override AlpsState<TGenotype> ExecuteStep(AlpsState<TGenotype>? previousState, TProblem problem, IRandomNumberGenerator random)
  {
    var agedProblem = problem;
    var agedSearchSpace = problem.SearchSpace;

    if (previousState is null) {
      var initialLayerPopulation = agedCreator.Create(PopulationSize, random, agedSearchSpace, agedProblem);

      var initialFitnesses = Evaluator.Evaluate(initialLayerPopulation, random, agedSearchSpace, agedProblem);

      return new AlpsState<TGenotype> {
        Population = [Population.From(initialLayerPopulation, initialFitnesses)],
        Ages = [Enumerable.Repeat(0, PopulationSize).ToArray()]
      };
    }

    var offspringCount = PopulationSize;

    // ToDo: implement actual ALPS logic with layers

    var oldPopulation = previousState.Population[0].ToArray();

    var parents = agedSelector.Select(oldPopulation, problem.Objective, offspringCount * 2, random, agedSearchSpace, agedProblem);

    var parentPairs = new IParents<TGenotype>[offspringCount];
    var offspringAges = new int[offspringCount];
    for (int i = 0, j = 0; i < offspringCount; i++, j += 2) {
      parentPairs[i] = new Parents<TGenotype>(parents[j].Genotype, parents[j + 1].Genotype);
      offspringAges[i] = Math.Max(previousState.Ages[0][j], previousState.Ages[0][j + 1]) + 1;
    }

    var population = agedCrossover.Cross(parentPairs, random, agedSearchSpace, agedProblem);

    population = agedMutator.Mutate(population, random, agedSearchSpace, agedProblem);

    var fitnesses = Evaluator.Evaluate(population, random, agedSearchSpace, agedProblem);

    var evaluatedPopulation = Population.From(population, fitnesses);

    var newPopulation = agedReplacer.Replace(oldPopulation, evaluatedPopulation.ToList(), problem.Objective, offspringCount, random, agedSearchSpace, agedProblem);

    var result = new AlpsState<TGenotype> {
      Population = [Population.From(newPopulation)],
      Ages = [offspringAges] // ToDo: ERROR here, since the replacer might shuffled the population and keeps some of the old solutions, so we need to track the ages through the replacer as well
    };

    return result;
  }
}
