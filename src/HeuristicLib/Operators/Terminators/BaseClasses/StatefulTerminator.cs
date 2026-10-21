using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Terminators;

/// <remarks>
/// <typeparamref name="TState"/> may contain mutable execution data and helper data structures.
/// It must not contain operator or algorithm configurations, executors or executor resolution facilities.
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

    protected abstract bool IsTerminalState(TSearchState searchState, TState state, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem);

    protected sealed override ExecutorFactory<ITerminationExecutor<TCandidate, TSearchSpace, TProblem, TSearchState>> CreateFactory()
    {
        var executor = new Executor(this, CreateInitialState());
        return _ => executor;
    }

    private sealed class Executor(StatefulTerminator<TCandidate, TSearchSpace, TProblem, TSearchState, TState> terminator, TState executionState)
        : TerminationExecutor<TCandidate, TSearchSpace, TProblem, TSearchState>
    {
        public override bool IsTerminalState(TSearchState state, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem) =>
            terminator.IsTerminalState(state, executionState, random, searchSpace, problem);
    }
}

public abstract record StatefulTerminator<TCandidate, TSearchSpace, TSearchState, TState>
    : Terminator<TCandidate, TSearchSpace, TSearchState>
    where TSearchState : class, ISearchState
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TState : class
{
    protected abstract TState CreateInitialState();

    protected abstract bool IsTerminalState(TSearchState searchState, TState state, IRandomNumberGenerator random, TSearchSpace searchSpace);

    protected sealed override ExecutorFactory<ITerminationExecutor<TCandidate, TSearchSpace, IProblem<TCandidate, TSearchSpace>, TSearchState>> CreateFactory()
    {
        var executor = new Executor(this, CreateInitialState());
        return _ => executor;
    }

    private sealed class Executor(StatefulTerminator<TCandidate, TSearchSpace, TSearchState, TState> terminator, TState executionState)
        : TerminationExecutor<TCandidate, TSearchSpace, TSearchState>
    {
        public override bool IsTerminalState(TSearchState state, IRandomNumberGenerator random, TSearchSpace searchSpace) =>
            terminator.IsTerminalState(state, executionState, random, searchSpace);
    }
}

public abstract record StatefulTerminator<TCandidate, TSearchState, TState>
    : Terminator<TCandidate, TSearchState>
    where TSearchState : class, ISearchState
    where TState : class
{
    protected abstract TState CreateInitialState();

    protected abstract bool IsTerminalState(TSearchState searchState, TState state, IRandomNumberGenerator random);

    protected sealed override ExecutorFactory<ITerminationExecutor<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>, TSearchState>> CreateFactory()
    {
        var executor = new Executor(this, CreateInitialState());
        return _ => executor;
    }

    private sealed class Executor(StatefulTerminator<TCandidate, TSearchState, TState> terminator, TState executionState)
        : TerminationExecutor<TCandidate, TSearchState>
    {
        public override bool IsTerminalState(TSearchState state, IRandomNumberGenerator random) => terminator.IsTerminalState(state, executionState, random);
    }
}

public abstract record StatefulTerminator<TCandidate, TState>
    : Terminator<TCandidate>
    where TState : class
{
    protected abstract TState CreateInitialState();

    protected abstract bool IsTerminalState(TState state);

    protected sealed override ExecutorFactory<ITerminationExecutor<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>, ISearchState>> CreateFactory()
    {
        var executor = new Executor(this, CreateInitialState());
        return _ => executor;
    }

    private sealed class Executor(StatefulTerminator<TCandidate, TState> terminator, TState executionState)
        : TerminationExecutor<TCandidate>
    {
        public override bool IsTerminalState() => terminator.IsTerminalState(executionState);
    }
}
