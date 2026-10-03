using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Selectors;

/// <remarks>
/// <typeparamref name="TState"/> may contain mutable execution data and helper data structures.
/// It must not contain operator or algorithm configurations, execution nodes or execution node resolution facilities.
/// <see cref="CreateInitialState"/> must return a fresh state object for each preparation. Bindings of that execution share the state.
/// Calls are not inherently thread safe.
/// </remarks>
public abstract record StatefulSelector<TCandidate, TSearchSpace, TProblem, TState>
    : Selector<TCandidate, TSearchSpace, TProblem>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
    where TState : class
{
    protected abstract TState CreateInitialState();

    protected abstract IReadOnlyList<EvaluatedCandidate<TCandidate>> Select(IReadOnlyList<EvaluatedCandidate<TCandidate>> population, ObjectiveDirections objective, int count, TState state, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem);

    public sealed override ExecutionFactory<ISelectorExecution<TCandidate, TSearchSpace, TProblem>> CreateExecutionFactory()
    {
        var execution = new Execution(this, CreateInitialState());
        return _ => execution;
    }

    private sealed class Execution(StatefulSelector<TCandidate, TSearchSpace, TProblem, TState> selector, TState state)
        : SelectorExecution<TCandidate, TSearchSpace, TProblem>
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
    protected abstract TState CreateInitialState();

    protected abstract IReadOnlyList<EvaluatedCandidate<TCandidate>> Select(IReadOnlyList<EvaluatedCandidate<TCandidate>> population, ObjectiveDirections objective, int count, TState state, IRandomNumberGenerator random, TSearchSpace searchSpace);

    public sealed override ExecutionFactory<ISelectorExecution<TCandidate, TSearchSpace, IProblem<TCandidate, TSearchSpace>>> CreateExecutionFactory()
    {
        var execution = new Execution(this, CreateInitialState());
        return _ => execution;
    }

    private sealed class Execution(StatefulSelector<TCandidate, TSearchSpace, TState> selector, TState state)
        : SelectorExecution<TCandidate, TSearchSpace>
    {
        public override IReadOnlyList<EvaluatedCandidate<TCandidate>> Select(IReadOnlyList<EvaluatedCandidate<TCandidate>> population, ObjectiveDirections objective, int count, IRandomNumberGenerator random, TSearchSpace searchSpace) =>
            selector.Select(population, objective, count, state, random, searchSpace);
    }
}

public abstract record StatefulSelector<TCandidate, TState>
    : Selector<TCandidate>
    where TState : class
{
    protected abstract TState CreateInitialState();

    protected abstract IReadOnlyList<EvaluatedCandidate<TCandidate>> Select(IReadOnlyList<EvaluatedCandidate<TCandidate>> population, ObjectiveDirections objective, int count, TState state, IRandomNumberGenerator random);

    public sealed override ExecutionFactory<ISelectorExecution<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>>> CreateExecutionFactory()
    {
        var execution = new Execution(this, CreateInitialState());
        return _ => execution;
    }

    private sealed class Execution(StatefulSelector<TCandidate, TState> selector, TState state)
        : SelectorExecution<TCandidate>
    {
        public override IReadOnlyList<EvaluatedCandidate<TCandidate>> Select(IReadOnlyList<EvaluatedCandidate<TCandidate>> population, ObjectiveDirections objective, int count, IRandomNumberGenerator random) =>
            selector.Select(population, objective, count, state, random);
    }
}
