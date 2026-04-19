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
  public required ISelector<TGenotype, TSearchSpace, TProblem> Selector { get; init; }

  public int Elites { get; init; }

  public double MutationRate
  {
    get;
    init => field = value is >= 0.0 and <= 1.0
      ? value
      : throw new ArgumentOutOfRangeException(nameof(MutationRate), "MutationRate must be in [0, 1].");
  } = 0.1;

  protected override AlpsState<TGenotype> ExecuteStep(
    AlpsState<TGenotype>? previousState,
    IOperatorExecutor executor,
    TProblem problem,
    IRandomNumberGenerator random)
  {
    var searchSpace = problem.SearchSpace;

    if (previousState is null) {
      var initialLayerPopulation = executor.Create(Creator, PopulationSize, random, searchSpace, problem);
      var initialFitnesses = executor.Evaluate(Evaluator, initialLayerPopulation, random, searchSpace, problem);

      return new AlpsState<TGenotype> {
        Population = [Population.From(initialLayerPopulation, initialFitnesses)],
        Ages = [Enumerable.Repeat(0, PopulationSize).ToArray()]
      };
    }

    var offspringCount = PopulationSize;
    var oldPopulation = previousState.Population[0].ToArray();
    var selectedParents = executor.Select(Selector, oldPopulation, problem.Objective, offspringCount * 2, random, searchSpace, problem);

    var parentPairs = new IParents<TGenotype>[offspringCount];
    var offspringAges = new int[offspringCount];
    for (int i = 0, j = 0; i < offspringCount; i++, j += 2) {
      parentPairs[i] = new Parents<TGenotype>(selectedParents[j].Genotype, selectedParents[j + 1].Genotype);
      offspringAges[i] = Math.Max(previousState.Ages[0][j], previousState.Ages[0][j + 1]) + 1;
    }

    var offspring = executor.Cross(Crossover, parentPairs, random, searchSpace, problem);
    var effectiveMutator = MutationRate >= 1.0 ? Mutator : Mutator.WithRate(MutationRate);
    offspring = executor.Mutate(effectiveMutator, offspring, random, searchSpace, problem);

    var fitnesses = executor.Evaluate(Evaluator, offspring, random, searchSpace, problem);
    var offspringPopulation = Population.From(offspring, fitnesses).Solutions;
    var newPopulation = ElitismReplacer<TGenotype>.Replace(oldPopulation, offspringPopulation, problem.Objective, offspringCount, Elites);

    return new AlpsState<TGenotype> {
      Population = [Population.From(newPopulation)],
      Ages = [offspringAges] // ToDo: track ages through replacement once ALPS layer behavior is fully implemented.
    };
  }
}
