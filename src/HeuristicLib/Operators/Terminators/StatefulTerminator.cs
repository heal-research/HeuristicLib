using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Operators.Terminators;

/// <remarks>
/// <typeparamref name="TState"/> may contain mutable execution data and helper data structures.
/// It must not contain operator or algorithm configurations, execution instances or execution instance resolution facilities.
/// <see cref="CreateInitialState"/> must return a fresh state object for every execution instance. Calls are not inherently thread safe.
/// </remarks>
public abstract record StatefulTerminator<TCandidate, TSearchSpace, TProblem, TSearchState, TState>
    : Terminator<TCandidate, TSearchSpace, TProblem, TSearchState>
    where TSearchState : class, ISearchState
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
    where TState : class
{
    protected abstract TState CreateInitialState();

    protected abstract bool IsTerminalState(TSearchState searchState, TState state, TSearchSpace searchSpace, TProblem problem);

    public sealed override ITerminatorInstance<TCandidate, TSearchSpace, TProblem, TSearchState> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) => new Instance(this, CreateInitialState());

    private sealed class Instance(StatefulTerminator<TCandidate, TSearchSpace, TProblem, TSearchState, TState> terminator, TState executionState)
        : TerminatorInstance<TCandidate, TSearchSpace, TProblem, TSearchState>
    {
        public override bool IsTerminalState(TSearchState state, TSearchSpace searchSpace, TProblem problem) =>
            terminator.IsTerminalState(state, executionState, searchSpace, problem);
    }
}

public abstract record StatefulTerminator<TCandidate, TSearchSpace, TSearchState, TState>
    : Terminator<TCandidate, TSearchSpace, TSearchState>
    where TSearchState : class, ISearchState
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TState : class
{
    protected abstract TState CreateInitialState();

    protected abstract bool IsTerminalState(TSearchState searchState, TState state, TSearchSpace searchSpace);

    public sealed override ITerminatorInstance<TCandidate, TSearchSpace, IProblem<TCandidate, TSearchSpace>, TSearchState> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) => new Instance(this, CreateInitialState());

    private sealed class Instance(StatefulTerminator<TCandidate, TSearchSpace, TSearchState, TState> terminator, TState executionState)
        : TerminatorInstance<TCandidate, TSearchSpace, TSearchState>
    {
        public override bool IsTerminalState(TSearchState state, TSearchSpace searchSpace) =>
            terminator.IsTerminalState(state, executionState, searchSpace);
    }
}

public abstract record StatefulTerminator<TCandidate, TSearchState, TState>
    : Terminator<TCandidate, TSearchState>
    where TSearchState : class, ISearchState
    where TState : class
{
    protected abstract TState CreateInitialState();

    protected abstract bool IsTerminalState(TSearchState searchState, TState state);

    public sealed override ITerminatorInstance<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>, TSearchState> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) => new Instance(this, CreateInitialState());

    private sealed class Instance(StatefulTerminator<TCandidate, TSearchState, TState> terminator, TState executionState)
        : TerminatorInstance<TCandidate, TSearchState>
    {
        public override bool IsTerminalState(TSearchState state) => terminator.IsTerminalState(state, executionState);
    }
}

public abstract record StatefulTerminator<TCandidate, TState>
    : Terminator<TCandidate>
    where TState : class
{
    protected abstract TState CreateInitialState();

    protected abstract bool IsTerminalState(TState state);

    public sealed override ITerminatorInstance<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>, ISearchState> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) => new Instance(this, CreateInitialState());

    private sealed class Instance(StatefulTerminator<TCandidate, TState> terminator, TState executionState)
        : TerminatorInstance<TCandidate>
    {
        public override bool IsTerminalState() => terminator.IsTerminalState(executionState);
    }
}
