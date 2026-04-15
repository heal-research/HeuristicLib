using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Operators.Selectors;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Algorithms.LocalSearch;

public record HillClimber<TGenotype, TSearchSpace, TProblem>
  : IterativeAlgorithm<TGenotype, TSearchSpace, TProblem, SingleSolutionState<TGenotype>, HillClimber<TGenotype, TSearchSpace, TProblem>.State>
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
{
  public required ICreator<TGenotype, TSearchSpace, TProblem> Creator { get; init; }
  public required IMutator<TGenotype, TSearchSpace, TProblem> Mutator { get; init; }
  public required LocalSearchDirection Direction { get; init; }
  public required int MaxNeighbors { get; init; }
  public required int BatchSize { get; init; }

  protected override State CreateInitialState(ExecutionInstanceRegistry instanceRegistry) => new(instanceRegistry, this);

  protected override SingleSolutionState<TGenotype> ExecuteStep(State state, SingleSolutionState<TGenotype>? previousState, TProblem problem, IRandomNumberGenerator random)
  {
    if (previousState is null) {
      var initialSolution = state.Creator.Create(1, random, problem.SearchSpace, problem)[0];
      var initialFitness = state.Evaluator.Evaluate([initialSolution], random, problem.SearchSpace, problem)[0];
      return new SingleSolutionState<TGenotype> {
        Population = Population.From([initialSolution], [initialFitness])
      };
    }

    var sol = previousState.Solution;
    var newISolution = sol;

    for (var i = 0; i < MaxNeighbors; i += BatchSize) {
      var child = state.Mutator.Mutate(Enumerable.Repeat(sol.Genotype, BatchSize).ToArray(), random, problem.SearchSpace, problem);
      var res = state.Evaluator.Evaluate(child, random, problem.SearchSpace, problem);
      var best = BestSelector.Select(res.Append(sol.ObjectiveVector).ToArray(), problem.Objective, 1, random)[0];
      if (best == BatchSize) {
        continue;
      }

      newISolution = new Solution<TGenotype>(child[best], res[best]);
      if (Direction == LocalSearchDirection.FirstImprovement) {
        return new SingleSolutionState<TGenotype> {
          Population = Population.From([newISolution.Genotype], [newISolution.ObjectiveVector])
        };
      }
    }

    return new SingleSolutionState<TGenotype> {
      Population = Population.From([newISolution.Genotype], [newISolution.ObjectiveVector])
    };
  }

  public class State : IterativeAlgorithmState
  {
    public ICreatorInstance<TGenotype, TSearchSpace, TProblem> Creator { get; }
    public IMutatorInstance<TGenotype, TSearchSpace, TProblem> Mutator { get; }

    public State(ExecutionInstanceRegistry instanceRegistry, HillClimber<TGenotype, TSearchSpace, TProblem> algorithm) : base(instanceRegistry, algorithm)
    {
      Creator = instanceRegistry.Resolve(algorithm.Creator);
      Mutator = instanceRegistry.Resolve(algorithm.Mutator);
    }
  }
}

public static class HillClimber
{
  public static HillClimberBuilder<TGenotype, TSearchSpace, TProblem> GetBuilder<TGenotype, TSearchSpace, TProblem>(
    ICreator<TGenotype, TSearchSpace, TProblem> creator, IMutator<TGenotype, TSearchSpace, TProblem> mutator)
    where TSearchSpace : class, ISearchSpace<TGenotype>
    where TProblem : class, IProblem<TGenotype, TSearchSpace>
  {
    return new HillClimberBuilder<TGenotype, TSearchSpace, TProblem> {
      Mutator = mutator,
      Creator = creator
    };
  }
}
