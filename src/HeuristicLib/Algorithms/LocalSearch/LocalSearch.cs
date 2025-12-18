using HEAL.HeuristicLib.Operators.Creator;
using HEAL.HeuristicLib.Operators.Evaluator;
using HEAL.HeuristicLib.Operators.Mutator;
using HEAL.HeuristicLib.Operators.Prototypes;
using HEAL.HeuristicLib.Operators.Selector;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Algorithms.LocalSearch;

public class LocalSearch<TGenotype, TEncoding, TProblem>
  : IterativeAlgorithm<TGenotype, TEncoding, TProblem, SingleISolutionIterationResult<TGenotype>>
  where TEncoding : class, IEncoding<TGenotype>
  where TProblem : class, IProblem<TGenotype, TEncoding>
  where TGenotype : class {
  public required IMutator<TGenotype, TEncoding, TProblem> Mutator { get; init; }
  public required LocalSearchDirection Direction { get; init; }
  public required int MaxNeighbors { get; init; }
  public required int BatchSize { get; init; }

  public override SingleISolutionIterationResult<TGenotype> ExecuteStep(
    TProblem problem,
    TEncoding searchSpace,
    SingleISolutionIterationResult<TGenotype>? previousIterationResult,
    IRandomNumberGenerator random) {
    if (previousIterationResult == null)
      return new SingleISolutionIterationResult<TGenotype>(CreateInitialPopulation(problem, searchSpace, random, 1).Solutions[0]);

    var sol = previousIterationResult.Solution;
    var newISolution = sol;

    for (var i = 0; i < MaxNeighbors; i += BatchSize) {
      var child = Mutator.Mutate(Enumerable.Repeat(sol.Genotype, BatchSize).ToArray(), random, searchSpace, problem);
      var res = Evaluator.Evaluate(child, random, searchSpace, problem);
      var best = BestSelector.Select(res.Append(sol.ObjectiveVector).ToArray(), problem.Objective, 1, random)[0];
      if (best == BatchSize)
        continue;
      newISolution = new Solution<TGenotype>(child[best], res[best]);
      if (Direction == LocalSearchDirection.FirstImprovement)
        return new SingleISolutionIterationResult<TGenotype>(newISolution);
    }

    return new SingleISolutionIterationResult<TGenotype>(newISolution);
  }

  public class Builder : AlgorithmBuilder<TGenotype, TEncoding, TProblem, SingleISolutionIterationResult<TGenotype>, LocalSearch<TGenotype, TEncoding, TProblem>>, IMutatorPrototype<TGenotype, TEncoding, TProblem> {
    public required IMutator<TGenotype, TEncoding, TProblem> Mutator { get; set; }
    public int MaxNeighbors { get; set; } = 100;
    public int BatchSize { get; set; } = 100;
    public LocalSearchDirection Direction { get; set; } = LocalSearchDirection.FirstImprovement;

    public LocalSearch<TGenotype, TEncoding, TProblem> Create() =>
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

    public override LocalSearch<TGenotype, TEncoding, TProblem> BuildAlgorithm() => Create();
  }
}

public static class LocalSearch {
  public static LocalSearch<TGenotype, TEncoding, TProblem>.Builder GetBuilder<TGenotype, TEncoding, TProblem>(
    ICreator<TGenotype, TEncoding, TProblem> creator, IMutator<TGenotype, TEncoding, TProblem> mutator)
    where TEncoding : class, IEncoding<TGenotype>
    where TProblem : class, IProblem<TGenotype, TEncoding>
    where TGenotype : class {
    return new LocalSearch<TGenotype, TEncoding, TProblem>.Builder {
      Mutator = mutator,
      Creator = creator
    };
  }
}
