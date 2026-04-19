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
  : IterativeAlgorithm<TGenotype, TSearchSpace, TProblem, SingleSolutionState<TGenotype>>
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
{
  public required ICreator<TGenotype, TSearchSpace, TProblem> Creator { get; init; }
  public required IMutator<TGenotype, TSearchSpace, TProblem> Mutator { get; init; }
  public required LocalSearchDirection Direction { get; init; }
  public required int MaxNeighbors { get; init; }
  public required int BatchSize { get; init; }

  protected override SingleSolutionState<TGenotype> ExecuteStep(
    SingleSolutionState<TGenotype>? previousState,
    IOperatorExecutor executor,
    TProblem problem,
    IRandomNumberGenerator random)
  {
    if (previousState is null) {
      var initialSolution = executor.Create(Creator, 1, random, problem.SearchSpace, problem)[0];
      var initialFitness = executor.Evaluate(Evaluator, [initialSolution], random, problem.SearchSpace, problem)[0];
      return new SingleSolutionState<TGenotype> {
        Population = Population.From([initialSolution], [initialFitness])
      };
    }

    var sol = previousState.Solution;
    var newISolution = sol;

    for (var i = 0; i < MaxNeighbors; i += BatchSize) {
      var child = executor.Mutate(Mutator, Enumerable.Repeat(sol.Genotype, BatchSize).ToArray(), random, problem.SearchSpace, problem);
      var res = executor.Evaluate(Evaluator, child, random, problem.SearchSpace, problem);
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
