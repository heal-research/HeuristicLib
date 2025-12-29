using HEAL.HeuristicLib.OperatorPrototypes;
using HEAL.HeuristicLib.Operators.Creators;
using HEAL.HeuristicLib.Operators.Mutators;
using HEAL.HeuristicLib.Operators.Selectors;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Algorithms.LocalSearch;

public class HillClimber<TGenotype, TSearchSpace, TProblem>
  : IterativeAlgorithm<TGenotype, TSearchSpace, TProblem, SingleSolutionIterationState<TGenotype>>
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
  where TGenotype : class {
  public required IMutator<TGenotype, TSearchSpace, TProblem> Mutator { get; init; }
  public required LocalSearchDirection Direction { get; init; }
  public required int MaxNeighbors { get; init; }
  public required int BatchSize { get; init; }

  public override SingleSolutionIterationState<TGenotype> ExecuteStep(
    TProblem problem,
    TSearchSpace searchSpace,
    SingleSolutionIterationState<TGenotype>? previousState,
    IRandomNumberGenerator random) {
    if (previousState == null) {
      var initialSolution = Creator.Create(random, searchSpace, problem);
      var initialFitness = Evaluator.Evaluate([initialSolution], random, searchSpace, problem)[0];
      return new SingleSolutionIterationState<TGenotype> {
        Population = Population.From([initialSolution], [initialFitness]),
        CurrentIteration = 0
      };
    }

    var sol = previousState.Solution;
    var newISolution = sol;

    for (int i = 0; i < MaxNeighbors; i += BatchSize) {
      var child = Mutator.Mutate(Enumerable.Repeat(sol.Genotype, BatchSize).ToArray(), random, searchSpace, problem);
      var res = Evaluator.Evaluate(child, random, searchSpace, problem);
      var best = BestSelector.Select(res.Append(sol.ObjectiveVector).ToArray(), problem.Objective, 1, random)[0];
      if (best == BatchSize)
        continue;
      newISolution = new Solution<TGenotype>(child[best], res[best]);
      if (Direction == LocalSearchDirection.FirstImprovement)
        return new SingleSolutionIterationState<TGenotype> {
          Population = Population.From([newISolution.Genotype], [newISolution.ObjectiveVector]),
          CurrentIteration = previousState.CurrentIteration + 1
        };
    }

    return new SingleSolutionIterationState<TGenotype> {
      Population = Population.From([newISolution.Genotype], [newISolution.ObjectiveVector]),
      CurrentIteration = previousState.CurrentIteration + 1
    };
  }

  public class Builder : AlgorithmBuilder<TGenotype, TSearchSpace, TProblem, SingleSolutionIterationState<TGenotype>, HillClimber<TGenotype, TSearchSpace, TProblem>>, IMutatorPrototype<TGenotype, TSearchSpace, TProblem> {
    public required IMutator<TGenotype, TSearchSpace, TProblem> Mutator { get; set; }
    public int MaxNeighbors { get; set; } = 100;
    public int BatchSize { get; set; } = 100;
    public LocalSearchDirection Direction { get; set; } = LocalSearchDirection.FirstImprovement;

    public HillClimber<TGenotype, TSearchSpace, TProblem> Create() =>
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

    public override HillClimber<TGenotype, TSearchSpace, TProblem> BuildAlgorithm() => Create();
  }
}

public static class HillClimber {
  public static HillClimber<TGenotype, TSearchSpace, TProblem>.Builder GetBuilder<TGenotype, TSearchSpace, TProblem>(
    ICreator<TGenotype, TSearchSpace, TProblem> creator, IMutator<TGenotype, TSearchSpace, TProblem> mutator)
    where TSearchSpace : class, ISearchSpace<TGenotype>
    where TProblem : class, IProblem<TGenotype, TSearchSpace>
    where TGenotype : class {
    return new HillClimber<TGenotype, TSearchSpace, TProblem>.Builder {
      Mutator = mutator,
      Creator = creator
    };
  }
}
