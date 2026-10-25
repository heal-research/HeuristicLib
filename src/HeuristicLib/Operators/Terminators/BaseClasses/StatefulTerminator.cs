using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Terminators;

/// <remarks>
/// <typeparamref name="TState"/> may contain mutable execution data and helper data structures.
/// It must not contain operator or algorithm configurations, executors or executor resolution facilities.
/// <see cref="CreateScopedState"/> must return a fresh state object. Resolution requests it once per owning scope, and every executor built there shares it.
/// Calls are not inherently thread safe.
/// </remarks>
public abstract record StatefulTerminator<TCandidate, TSearchSpace, TProblem, TSearchState, TState>
    : Terminator<TCandidate, TSearchSpace, TProblem, TSearchState>
    where TSearchState : class, ISearchState
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
    where TState : class
{
    protected abstract TState CreateScopedState();

    protected abstract bool IsTerminalState(TSearchState searchState, TState operatorState, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem);

    protected sealed override ITerminationExecutor<TCandidate, TSearchSpace, TProblem, TSearchState> CreateExecutor(ResolutionScope scope) =>
        new Executor(this, scope.GetScopedState(this, CreateScopedState));

    private sealed class Executor(StatefulTerminator<TCandidate, TSearchSpace, TProblem, TSearchState, TState> terminator, TState operatorState)
        : TerminationExecutor<TCandidate, TSearchSpace, TProblem, TSearchState>
    {
        public override bool IsTerminalState(TSearchState state, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem) =>
            terminator.IsTerminalState(state, operatorState, random, searchSpace, problem);
    }
}

public abstract record StatefulTerminator<TCandidate, TSearchSpace, TSearchState, TState>
    : Terminator<TCandidate, TSearchSpace, TSearchState>
    where TSearchState : class, ISearchState
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TState : class
{
    protected abstract TState CreateScopedState();

    protected abstract bool IsTerminalState(TSearchState searchState, TState operatorState, IRandomNumberGenerator random, TSearchSpace searchSpace);

    protected sealed override ITerminationExecutor<TCandidate, TSearchSpace, IProblem<TCandidate, TSearchSpace>, TSearchState> CreateExecutor(ResolutionScope scope) =>
        new Executor(this, scope.GetScopedState(this, CreateScopedState));

    private sealed class Executor(StatefulTerminator<TCandidate, TSearchSpace, TSearchState, TState> terminator, TState operatorState)
        : TerminationExecutor<TCandidate, TSearchSpace, TSearchState>
    {
        public override bool IsTerminalState(TSearchState state, IRandomNumberGenerator random, TSearchSpace searchSpace) =>
            terminator.IsTerminalState(state, operatorState, random, searchSpace);
    }
}

public abstract record StatefulTerminator<TCandidate, TSearchState, TState>
    : Terminator<TCandidate, TSearchState>
    where TSearchState : class, ISearchState
    where TState : class
{
    protected abstract TState CreateScopedState();

    protected abstract bool IsTerminalState(TSearchState searchState, TState operatorState, IRandomNumberGenerator random);

    protected sealed override ITerminationExecutor<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>, TSearchState> CreateExecutor(ResolutionScope scope) =>
        new Executor(this, scope.GetScopedState(this, CreateScopedState));

    private sealed class Executor(StatefulTerminator<TCandidate, TSearchState, TState> terminator, TState operatorState)
        : TerminationExecutor<TCandidate, TSearchState>
    {
        public override bool IsTerminalState(TSearchState state, IRandomNumberGenerator random) => terminator.IsTerminalState(state, operatorState, random);
    }
}

public abstract record StatefulTerminator<TCandidate, TState>
    : Terminator<TCandidate>
    where TState : class
{
    protected abstract TState CreateScopedState();

    protected abstract bool IsTerminalState(TState state);

    protected sealed override ITerminationExecutor<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>, ISearchState> CreateExecutor(ResolutionScope scope) =>
        new Executor(this, scope.GetScopedState(this, CreateScopedState));

    private sealed class Executor(StatefulTerminator<TCandidate, TState> terminator, TState state)
        : TerminationExecutor<TCandidate>
    {
        public override bool IsTerminalState() => terminator.IsTerminalState(state);
    }
}
