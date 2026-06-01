using System.Diagnostics.CodeAnalysis;
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
  : IterativeAlgorithm<TGenotype, TSearchSpace, TProblem, SingleSolutionState<TGenotype>, HillClimber<TGenotype, TSearchSpace, TProblem>.ExecutionState>
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
{
    public new sealed class ExecutionState
      : IterativeAlgorithm<TGenotype, TSearchSpace, TProblem, SingleSolutionState<TGenotype>, ExecutionState>.ExecutionState
    {
        public required ICreatorInstance<TGenotype, TSearchSpace, TProblem> Creator { get; init; }
        public required IMutatorInstance<TGenotype, TSearchSpace, TProblem> Mutator { get; init; }
    }

    public required ICreator<TGenotype, TSearchSpace, TProblem> Creator { get; init; }
    public required IMutator<TGenotype, TSearchSpace, TProblem> Mutator { get; init; }
    public required LocalSearchDirection Direction { get; init; }
    public required int MaxNeighbors { get; init; }
    public required int BatchSize { get; init; }

    protected override ExecutionState CreateInitialExecutionState(IExecutionInstanceResolver resolver)
    {
        return new ExecutionState
        {
            Evaluator = resolver.Resolve(Evaluator),
            Interceptor = Interceptor is not null ? resolver.Resolve(Interceptor) : null,
            Creator = resolver.Resolve(Creator),
            Mutator = resolver.Resolve(Mutator)
        };
    }

    protected override SingleSolutionState<TGenotype> ExecuteStep(
      SingleSolutionState<TGenotype>? previousState,
      ExecutionState executionState,
      TProblem problem,
      IRandomNumberGenerator random)
    {
        return TryExecuteStep(previousState, executionState, problem, random, out var nextState)
          ? nextState!
          : throw new InvalidOperationException("HillClimber has structurally completed and cannot produce another step.");
    }

    protected override bool TryExecuteStep(
      SingleSolutionState<TGenotype>? previousState,
      ExecutionState executionState,
      TProblem problem,
      IRandomNumberGenerator random,
      [NotNullWhen(true)] out SingleSolutionState<TGenotype>? nextState)
    {
        if (previousState is null)
        {
            nextState = CreateInitialState(executionState, problem, random);
            return true;
        }

        if (!TryFindImprovement(previousState.Solution, executionState, problem, random, out var improvement))
        {
            nextState = null;
            return false;
        }

        nextState = ToState(improvement);
        return true;
    }

    private static SingleSolutionState<TGenotype> CreateInitialState(
      ExecutionState executionState,
      TProblem problem,
      IRandomNumberGenerator random)
    {
        var initialSolution = executionState.Creator.Create(1, random, problem.SearchSpace, problem)[0];
        var initialFitness = executionState.Evaluator.Evaluate([initialSolution], random, problem.SearchSpace, problem)[0];
        return ToState(new Solution<TGenotype>(initialSolution, initialFitness));
    }

    private bool TryFindImprovement(
      ISolution<TGenotype> current,
      ExecutionState executionState,
      TProblem problem,
      IRandomNumberGenerator random,
      [NotNullWhen(true)] out ISolution<TGenotype>? improvement)
    {
        improvement = null;

        for (var i = 0; i < MaxNeighbors; i += BatchSize)
        {
            var candidates = executionState.Mutator.Mutate(Enumerable.Repeat(current.Genotype, BatchSize).ToArray(), random, problem.SearchSpace, problem);
            var objectiveVectors = executionState.Evaluator.Evaluate(candidates, random, problem.SearchSpace, problem);
            var bestIndex = BestSelector.Select(objectiveVectors, problem.Objective, count: 1)[0];

            if (problem.Objective.TotalOrderComparer.Compare(objectiveVectors[bestIndex], current.ObjectiveVector) >= 0)
            {
                continue;
            }

            improvement = new Solution<TGenotype>(candidates[bestIndex], objectiveVectors[bestIndex]);
            if (Direction == LocalSearchDirection.FirstImprovement)
            {
                return true;
            }
        }

        return improvement is not null;
    }

    private static SingleSolutionState<TGenotype> ToState(ISolution<TGenotype> solution)
    {
        return new SingleSolutionState<TGenotype>
        {
            Population = Population.From([solution])
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
        return new HillClimberBuilder<TGenotype, TSearchSpace, TProblem>
        {
            Mutator = mutator,
            Creator = creator
        };
    }
}
