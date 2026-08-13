using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Selectors;

/// <remarks>
/// <typeparamref name="TState"/> may contain mutable execution data and helper data structures.
/// It must not contain operator or algorithm configurations, execution instances or execution instance resolution facilities.
/// <see cref="CreateInitialState"/> must return a fresh state object for every execution instance. Calls are not inherently thread safe.
/// </remarks>
public abstract record StatefulSelector<TCandidate, TSearchSpace, TProblem, TState>
    : Selector<TCandidate, TSearchSpace, TProblem>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
    where TState : class
{
    protected abstract TState CreateInitialState();

    protected abstract IReadOnlyList<EvaluatedCandidate<TCandidate>> Select(IReadOnlyList<EvaluatedCandidate<TCandidate>> population, ObjectiveDirections objective, int count, TState state, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem);

    public sealed override ISelectorInstance<TCandidate, TSearchSpace, TProblem> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) => new Instance(this, CreateInitialState());

    private sealed class Instance(StatefulSelector<TCandidate, TSearchSpace, TProblem, TState> selector, TState state)
        : SelectorInstance<TCandidate, TSearchSpace, TProblem>
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

    public sealed override ISelectorInstance<TCandidate, TSearchSpace, IProblem<TCandidate, TSearchSpace>> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) => new Instance(this, CreateInitialState());

    private sealed class Instance(StatefulSelector<TCandidate, TSearchSpace, TState> selector, TState state)
        : SelectorInstance<TCandidate, TSearchSpace>
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

    public sealed override ISelectorInstance<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) => new Instance(this, CreateInitialState());

    private sealed class Instance(StatefulSelector<TCandidate, TState> selector, TState state)
        : SelectorInstance<TCandidate>
    {
        public override IReadOnlyList<EvaluatedCandidate<TCandidate>> Select(IReadOnlyList<EvaluatedCandidate<TCandidate>> population, ObjectiveDirections objective, int count, IRandomNumberGenerator random) =>
            selector.Select(population, objective, count, state, random);
    }
}
