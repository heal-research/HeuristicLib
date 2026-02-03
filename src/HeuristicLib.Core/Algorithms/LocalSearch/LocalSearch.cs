using HEAL.HeuristicLib.Operators.Creators;
using HEAL.HeuristicLib.Operators.Mutators;
using HEAL.HeuristicLib.Operators.Prototypes;
using HEAL.HeuristicLib.Operators.Selectors;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Algorithms.LocalSearch;

public class LocalSearch<TGenotype, TSearchSpace, TProblem>
  : IterativeAlgorithm<TGenotype, TSearchSpace, TProblem, SingleISolutionAlgorithmState<TGenotype>>
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
  where TGenotype : class
{
  public required IMutator<TGenotype, TSearchSpace, TProblem> Mutator { get; init; }
  public required LocalSearchDirection Direction { get; init; }
  public required int MaxNeighbors { get; init; }
  public required int BatchSize { get; init; }

  public override SingleISolutionAlgorithmState<TGenotype> ExecuteStep(
    TProblem problem,
    TSearchSpace searchSpace,
    SingleISolutionAlgorithmState<TGenotype>? previousIterationResult,
    IRandomNumberGenerator random)
  {
    if (previousIterationResult == null) {
      return new SingleISolutionAlgorithmState<TGenotype>(CreateInitialPopulation(problem, searchSpace, random, 1).Solutions[0]);
    }

    var sol = previousIterationResult.Solution;
    var newISolution = sol;

    for (var i = 0; i < MaxNeighbors; i += BatchSize) {
      var child = Mutator.Mutate(Enumerable.Repeat(sol.Genotype, BatchSize).ToArray(), random, searchSpace, problem);
      var res = Evaluator.Evaluate(child, random, searchSpace, problem);
      var best = BestSelector.Select(res.Append(sol.ObjectiveVector).ToArray(), problem.Objective, 1, random)[0];
      if (best == BatchSize) {
        continue;
      }
      newISolution = new Solution<TGenotype>(child[best], res[best]);
      if (Direction == LocalSearchDirection.FirstImprovement) {
        return new SingleISolutionAlgorithmState<TGenotype>(newISolution);
      }
    }

    return new SingleISolutionAlgorithmState<TGenotype>(newISolution);
  }

  public class Builder : AlgorithmBuilder<TGenotype, TSearchSpace, TProblem, SingleISolutionAlgorithmState<TGenotype>, LocalSearch<TGenotype, TSearchSpace, TProblem>>, IMutatorPrototype<TGenotype, TSearchSpace, TProblem>
  {
    public int MaxNeighbors { get; set; } = 100;
    public int BatchSize { get; set; } = 100;
    public LocalSearchDirection Direction { get; set; } = LocalSearchDirection.FirstImprovement;
    public required IMutator<TGenotype, TSearchSpace, TProblem> Mutator { get; set; }

    public LocalSearch<TGenotype, TSearchSpace, TProblem> Create() =>
      new() {
        Terminator = Terminator,
        Interceptor = Interceptor,
        Creator = Creator,
        Mutator = Mutator,
        Evaluator = Evaluator,
        AlgorithmRandom = SystemRandomNumberGenerator.Default(RandomSeed),
        MaxNeighbors = MaxNeighbors,
        BatchSize = BatchSize,
        Direction = Direction
      };

    public override LocalSearch<TGenotype, TSearchSpace, TProblem> BuildAlgorithm() => Create();
  }
}

public static class LocalSearch
{
  public static LocalSearch<TGenotype, TSearchSpace, TProblem>.Builder GetBuilder<TGenotype, TSearchSpace, TProblem>(
    ICreator<TGenotype, TSearchSpace, TProblem> creator, IMutator<TGenotype, TSearchSpace, TProblem> mutator)
    where TSearchSpace : class, ISearchSpace<TGenotype>
    where TProblem : class, IProblem<TGenotype, TSearchSpace>
    where TGenotype : class
  {
    return new LocalSearch<TGenotype, TSearchSpace, TProblem>.Builder {
      Mutator = mutator,
      Creator = creator
    };
  }
}
