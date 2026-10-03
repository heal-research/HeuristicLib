using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Terminators;

/// <remarks>
/// <typeparamref name="TState"/> may contain mutable execution data and helper data structures.
/// It must not contain operator or algorithm configurations, execution nodes or execution node resolution facilities.
/// <see cref="CreateInitialState"/> must return a fresh state object for each preparation. Bindings of that execution share the state.
/// Calls are not inherently thread safe.
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

    public sealed override ExecutionFactory<ITerminatorExecution<TCandidate, TSearchSpace, TProblem, TSearchState>> CreateExecutionFactory()
    {
        var execution = new Execution(this, CreateInitialState());
        return _ => execution;
    }

    private sealed class Execution(StatefulTerminator<TCandidate, TSearchSpace, TProblem, TSearchState, TState> terminator, TState executionState)
        : TerminatorExecution<TCandidate, TSearchSpace, TProblem, TSearchState>
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

    public sealed override ExecutionFactory<ITerminatorExecution<TCandidate, TSearchSpace, IProblem<TCandidate, TSearchSpace>, TSearchState>> CreateExecutionFactory()
    {
        var execution = new Execution(this, CreateInitialState());
        return _ => execution;
    }

    private sealed class Execution(StatefulTerminator<TCandidate, TSearchSpace, TSearchState, TState> terminator, TState executionState)
        : TerminatorExecution<TCandidate, TSearchSpace, TSearchState>
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

    public sealed override ExecutionFactory<ITerminatorExecution<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>, TSearchState>> CreateExecutionFactory()
    {
        var execution = new Execution(this, CreateInitialState());
        return _ => execution;
    }

    private sealed class Execution(StatefulTerminator<TCandidate, TSearchState, TState> terminator, TState executionState)
        : TerminatorExecution<TCandidate, TSearchState>
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

    public sealed override ExecutionFactory<ITerminatorExecution<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>, ISearchState>> CreateExecutionFactory()
    {
        var execution = new Execution(this, CreateInitialState());
        return _ => execution;
    }

    private sealed class Execution(StatefulTerminator<TCandidate, TState> terminator, TState executionState)
        : TerminatorExecution<TCandidate>
    {
        public override bool IsTerminalState() => terminator.IsTerminalState(executionState);
    }
}
