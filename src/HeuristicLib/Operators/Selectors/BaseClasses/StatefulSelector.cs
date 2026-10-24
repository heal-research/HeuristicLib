using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Selectors;

/// <remarks>
/// <typeparamref name="TState"/> may contain mutable execution data and helper data structures.
/// It must not contain operator or algorithm configurations, executors or executor resolution facilities.
/// <see cref="CreateExecutionState"/> must return a fresh state object. Resolution requests it once per owning execution state, and every executor built there shares it.
/// Calls are not inherently thread safe.
/// </remarks>
public abstract record StatefulSelector<TCandidate, TSearchSpace, TProblem, TState>
    : Selector<TCandidate, TSearchSpace, TProblem>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
    where TState : class
{
    protected abstract TState CreateExecutionState();

    protected abstract IReadOnlyList<EvaluatedCandidate<TCandidate>> Select(IReadOnlyList<EvaluatedCandidate<TCandidate>> population, ObjectiveDirections objective, int count, TState state, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem);

    protected sealed override ISelectionExecutor<TCandidate, TSearchSpace, TProblem> CreateExecutor(ResolutionScope scope) =>
        new Executor(this, scope.GetExecutionState(this, CreateExecutionState));

    private sealed class Executor(StatefulSelector<TCandidate, TSearchSpace, TProblem, TState> selector, TState state)
        : SelectionExecutor<TCandidate, TSearchSpace, TProblem>
    {
        public override IReadOnlyList<EvaluatedCandidate<TCandidate>> Select(IReadOnlyList<EvaluatedCandidate<TCandidate>> population, ObjectiveDirections objective, int count, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem) =>
            selector.Select(population, objective, count, state, random, searchSpace, problem);
    }
}

public abstract record StatefulSelector<TCandidate, TSearchSpace, TState>
    : Selector<TCandidate, TSearchSpace>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TState : class
{
    protected abstract TState CreateExecutionState();

    protected abstract IReadOnlyList<EvaluatedCandidate<TCandidate>> Select(IReadOnlyList<EvaluatedCandidate<TCandidate>> population, ObjectiveDirections objective, int count, TState state, IRandomNumberGenerator random, TSearchSpace searchSpace);

    protected sealed override ISelectionExecutor<TCandidate, TSearchSpace, IProblem<TCandidate, TSearchSpace>> CreateExecutor(ResolutionScope scope) =>
        new Executor(this, scope.GetExecutionState(this, CreateExecutionState));

    private sealed class Executor(StatefulSelector<TCandidate, TSearchSpace, TState> selector, TState state)
        : SelectionExecutor<TCandidate, TSearchSpace>
    {
        public override IReadOnlyList<EvaluatedCandidate<TCandidate>> Select(IReadOnlyList<EvaluatedCandidate<TCandidate>> population, ObjectiveDirections objective, int count, IRandomNumberGenerator random, TSearchSpace searchSpace) =>
            selector.Select(population, objective, count, state, random, searchSpace);
    }
}

public abstract record StatefulSelector<TCandidate, TState>
    : Selector<TCandidate>
    where TState : class
{
    protected abstract TState CreateExecutionState();

    protected abstract IReadOnlyList<EvaluatedCandidate<TCandidate>> Select(IReadOnlyList<EvaluatedCandidate<TCandidate>> population, ObjectiveDirections objective, int count, TState state, IRandomNumberGenerator random);

    protected sealed override ISelectionExecutor<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>> CreateExecutor(ResolutionScope scope) =>
        new Executor(this, scope.GetExecutionState(this, CreateExecutionState));

    private sealed class Executor(StatefulSelector<TCandidate, TState> selector, TState state)
        : SelectionExecutor<TCandidate>
    {
        public override IReadOnlyList<EvaluatedCandidate<TCandidate>> Select(IReadOnlyList<EvaluatedCandidate<TCandidate>> population, ObjectiveDirections objective, int count, IRandomNumberGenerator random) =>
            selector.Select(population, objective, count, state, random);
    }
}
