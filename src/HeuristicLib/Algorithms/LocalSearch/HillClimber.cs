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

public record HillClimber<TCandidate, TSearchSpace, TProblem>
    : IterativeAlgorithm<TCandidate, TSearchSpace, TProblem, SingleSolutionState<TCandidate>, HillClimber<TCandidate, TSearchSpace, TProblem>.ExecutionState>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    public new sealed class ExecutionState
        : IterativeAlgorithm<TCandidate, TSearchSpace, TProblem, SingleSolutionState<TCandidate>, ExecutionState>.ExecutionState
    {
        public required ICreatorInstance<TCandidate, TSearchSpace, TProblem> Creator { get; init; }
        public required IMutatorInstance<TCandidate, TSearchSpace, TProblem> Mutator { get; init; }
    }

    public required ICreator<TCandidate, TSearchSpace, TProblem> Creator { get; init; }
    public required IMutator<TCandidate, TSearchSpace, TProblem> Mutator { get; init; }
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

    protected override SingleSolutionState<TCandidate> ExecuteStep(
        SingleSolutionState<TCandidate>? previousState,
        ExecutionState executionState,
        TProblem problem,
        IRandomNumberGenerator random)
    {
        return TryExecuteStep(previousState, executionState, problem, random, out var nextState)
          ? nextState!
          : throw new InvalidOperationException("HillClimber has structurally completed and cannot produce another step.");
    }

    protected override bool TryExecuteStep(
        SingleSolutionState<TCandidate>? previousState,
        ExecutionState executionState,
        TProblem problem,
        IRandomNumberGenerator random,
        [NotNullWhen(true)] out SingleSolutionState<TCandidate>? nextState)
    {
        if (previousState is null)
        {
            nextState = CreateInitialState(executionState, problem, random);
            return true;
        }

        if (!TryFindImprovement(previousState.EvaluatedCandidate, executionState, problem, random, out var improvement))
        {
            nextState = null;
            return false;
        }

        nextState = ToState(improvement);
        return true;
    }

    private static SingleSolutionState<TCandidate> CreateInitialState(
        ExecutionState executionState,
        TProblem problem,
        IRandomNumberGenerator random)
    {
        var initialSolution = executionState.Creator.Create(1, random, problem.SearchSpace, problem)[0];
        var initialFitness = executionState.Evaluator.Evaluate([initialSolution], random, problem.SearchSpace, problem)[0];
        return ToState(new EvaluatedCandidate<TCandidate>(initialSolution, initialFitness));
    }

    private bool TryFindImprovement(
        EvaluatedCandidate<TCandidate> current,
        ExecutionState executionState,
        TProblem problem,
        IRandomNumberGenerator random,
        [NotNullWhen(true)] out EvaluatedCandidate<TCandidate>? improvement)
    {
        improvement = null;

        for (var i = 0; i < MaxNeighbors; i += BatchSize)
        {
            var candidates = executionState.Mutator.Mutate(Enumerable.Repeat(current.Candidate, BatchSize).ToArray(), random, problem.SearchSpace, problem);
            var objectiveVectors = executionState.Evaluator.Evaluate(candidates, random, problem.SearchSpace, problem);
            var bestIndex = BestSelector.Select(objectiveVectors, problem.Objective, count: 1)[0];

            if (problem.Objective.TotalOrderComparer.Compare(objectiveVectors[bestIndex], current.ObjectiveVector) >= 0)
            {
                continue;
            }

            improvement = new EvaluatedCandidate<TCandidate>(candidates[bestIndex], objectiveVectors[bestIndex]);
            if (Direction == LocalSearchDirection.FirstImprovement)
            {
                return true;
            }
        }

        return improvement is not null;
    }

    private static SingleSolutionState<TCandidate> ToState(EvaluatedCandidate<TCandidate> solution)
    {
        return new SingleSolutionState<TCandidate>
        {
            Population = Population.From([solution])
        };
    }
}

public static class HillClimber
{
    public static HillClimberBuilder<TCandidate, TSearchSpace, TProblem> GetBuilder<TCandidate, TSearchSpace, TProblem>(
        ICreator<TCandidate, TSearchSpace, TProblem> creator, IMutator<TCandidate, TSearchSpace, TProblem> mutator)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        return new HillClimberBuilder<TCandidate, TSearchSpace, TProblem>
        {
            Mutator = mutator,
            Creator = creator
        };
    }
}
