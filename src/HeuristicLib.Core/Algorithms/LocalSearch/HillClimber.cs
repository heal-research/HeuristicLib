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
  where TGenotype : class
{
  public required ICreator<TGenotype, TSearchSpace, TProblem> Creator { get; init; }
  public required IMutator<TGenotype, TSearchSpace, TProblem> Mutator { get; init; }
  public required LocalSearchDirection Direction { get; init; }
  public required int MaxNeighbors { get; init; }
  public required int BatchSize { get; init; }

  public override SingleSolutionIterationState<TGenotype> ExecuteStep(TProblem problem,
    SingleSolutionIterationState<TGenotype>? previousState,
    IRandomNumberGenerator random)
  {
    if (previousState == null) {
      var initialSolution = Creator.Create(random, problem.SearchSpace, problem);
      var initialFitness = Evaluator.Evaluate([initialSolution], random, problem.SearchSpace, problem)[0];
      return new SingleSolutionIterationState<TGenotype> {
        Population = Population.From([initialSolution], [initialFitness]),
        CurrentIteration = 0
      };
    }

    var sol = previousState.Solution;
    var newISolution = sol;

    for (var i = 0; i < MaxNeighbors; i += BatchSize) {
      var child = Mutator.Mutate(Enumerable.Repeat(sol.Genotype, BatchSize).ToArray(), random, problem.SearchSpace, problem);
      var res = Evaluator.Evaluate(child, random, problem.SearchSpace, problem);
      var best = BestSelector.Select(res.Append(sol.ObjectiveVector).ToArray(), problem.Objective, 1, random)[0];
      if (best == BatchSize) {
        continue;
      }

      newISolution = new Solution<TGenotype>(child[best], res[best]);
      if (Direction == LocalSearchDirection.FirstImprovement) {
        return new SingleSolutionIterationState<TGenotype> {
          Population = Population.From([newISolution.Genotype], [newISolution.ObjectiveVector]),
          CurrentIteration = previousState.CurrentIteration + 1
        };
      }
    }

    return new SingleSolutionIterationState<TGenotype> {
      Population = Population.From([newISolution.Genotype], [newISolution.ObjectiveVector]),
      CurrentIteration = previousState.CurrentIteration + 1
    };
  }
}

public static class HillClimber
{
  public static HillClimberBuilder<TGenotype, TSearchSpace, TProblem> GetBuilder<TGenotype, TSearchSpace, TProblem>(
    ICreator<TGenotype, TSearchSpace, TProblem> creator, IMutator<TGenotype, TSearchSpace, TProblem> mutator)
    where TSearchSpace : class, ISearchSpace<TGenotype>
    where TProblem : class, IProblem<TGenotype, TSearchSpace>
    where TGenotype : class
  {
    return new HillClimberBuilder<TGenotype, TSearchSpace, TProblem> {
      Mutator = mutator,
      Creator = creator
    };
  }
}
