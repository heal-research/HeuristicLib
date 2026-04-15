using HEAL.HeuristicLib.Algorithms.Evolutionary;
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

public record AlpsState<TGenotype> : IAlgorithmState
{
  public required IReadOnlyList<Population<TGenotype>> Population { get; init; }
  public required IReadOnlyList<IReadOnlyList<int>> Ages { get; init; }
}

public record AlpsGeneticAlgorithm<TGenotype, TSearchSpace, TProblem>
  : IterativeAlgorithm<TGenotype, TSearchSpace, TProblem, AlpsState<TGenotype>, AlpsGeneticAlgorithm<TGenotype, TSearchSpace, TProblem>.State>
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

  protected override State CreateInitialState(ExecutionInstanceRegistry instanceRegistry) => new(instanceRegistry, this);

  protected override AlpsState<TGenotype> ExecuteStep(State state, AlpsState<TGenotype>? previousState, TProblem problem, IRandomNumberGenerator random)
  {
    var agedProblem = problem;
    var agedSearchSpace = problem.SearchSpace;

    if (previousState is null) {
      var initialLayerPopulation = state.Creator.Create(PopulationSize, random, agedSearchSpace, agedProblem);

      var initialFitnesses = state.Evaluator.Evaluate(initialLayerPopulation, random, agedSearchSpace, agedProblem);

      return new AlpsState<TGenotype> {
        Population = [Population.From(initialLayerPopulation, initialFitnesses)],
        Ages = [Enumerable.Repeat(0, PopulationSize).ToArray()]
      };
    }

    var offspringCount = PopulationSize;

    // ToDo: implement actual ALPS logic with layers

    var oldPopulation = previousState.Population[0].ToArray();

    var parents = state.Selector.Select(oldPopulation, problem.Objective, offspringCount * 2, random, agedSearchSpace, agedProblem);

    var parentPairs = new IParents<TGenotype>[offspringCount];
    var offspringAges = new int[offspringCount];
    for (int i = 0, j = 0; i < offspringCount; i++, j += 2) {
      parentPairs[i] = new Parents<TGenotype>(parents[j].Genotype, parents[j + 1].Genotype);
      offspringAges[i] = Math.Max(previousState.Ages[0][j], previousState.Ages[0][j + 1]) + 1;
    }

    var population = state.Crossover.Cross(parentPairs, random, agedSearchSpace, agedProblem);

    population = state.Mutator.Mutate(population, random, agedSearchSpace, agedProblem);

    var fitnesses = state.Evaluator.Evaluate(population, random, agedSearchSpace, agedProblem);

    var evaluatedPopulation = Population.From(population, fitnesses);

    var newPopulation = state.Replacer.Replace(oldPopulation, evaluatedPopulation.ToList(), problem.Objective, offspringCount, random, agedSearchSpace, agedProblem);

    var result = new AlpsState<TGenotype> {
      Population = [Population.From(newPopulation)],
      Ages = [offspringAges] // ToDo: ERROR here, since the replacer might shuffled the population and keeps some of the old solutions, so we need to track the ages through the replacer as well
    };

    return result;
  }

  public class State : IterativeAlgorithmState
  {
    public ICreatorInstance<TGenotype, TSearchSpace, TProblem> Creator { get; }
    public ICrossoverInstance<TGenotype, TSearchSpace, TProblem> Crossover { get; }
    public IMutatorInstance<TGenotype, TSearchSpace, TProblem> Mutator { get; }
    public ISelectorInstance<TGenotype, TSearchSpace, TProblem> Selector { get; }
    public IReplacerInstance<TGenotype, TSearchSpace, TProblem> Replacer { get; }

    public State(ExecutionInstanceRegistry instanceRegistry, AlpsGeneticAlgorithm<TGenotype, TSearchSpace, TProblem> algorithm) : base(instanceRegistry, algorithm)
    {
      Creator = instanceRegistry.Resolve(algorithm.Creator);
      Crossover = instanceRegistry.Resolve(algorithm.Crossover);
      Mutator = instanceRegistry.Resolve(algorithm.Mutator.WithRate(algorithm.MutationRate));
      Selector = instanceRegistry.Resolve(algorithm.Selector);
      Replacer = instanceRegistry.Resolve(new ElitismReplacer<TGenotype>(algorithm.Elites));
    }
  }
}
