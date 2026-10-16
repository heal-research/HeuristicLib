using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Terminators;

public abstract record StatelessTerminator<TCandidate, TSearchSpace, TProblem, TSearchState>
    : Terminator<TCandidate, TSearchSpace, TProblem, TSearchState>, ITerminationExecutor<TCandidate, TSearchSpace, TProblem, TSearchState>
    where TSearchState : class, ISearchState
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    public sealed override ExecutorFactory<ITerminationExecutor<TCandidate, TSearchSpace, TProblem, TSearchState>> CreateExecutorFactory() => _ => this;

    public abstract bool IsTerminalState(TSearchState state, TSearchSpace searchSpace, TProblem problem);
}

public abstract record StatelessTerminator<TCandidate, TSearchSpace, TSearchState>
    : Terminator<TCandidate, TSearchSpace, TSearchState>, ITerminationExecutor<TCandidate, TSearchSpace, IProblem<TCandidate, TSearchSpace>, TSearchState>
    where TSearchState : class, ISearchState
    where TSearchSpace : class, ISearchSpace<TCandidate>
{
    public sealed override ExecutorFactory<ITerminationExecutor<TCandidate, TSearchSpace, IProblem<TCandidate, TSearchSpace>, TSearchState>> CreateExecutorFactory() => _ => this;

    public abstract bool IsTerminalState(TSearchState state, TSearchSpace searchSpace);

    bool ITerminationExecutor<TCandidate, TSearchSpace, IProblem<TCandidate, TSearchSpace>, TSearchState>.IsTerminalState(TSearchState state, TSearchSpace searchSpace, IProblem<TCandidate, TSearchSpace> problem) =>
        IsTerminalState(state, searchSpace);
}

public abstract record StatelessTerminator<TCandidate, TSearchState>
    : Terminator<TCandidate, TSearchState>, ITerminationExecutor<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>, TSearchState>
    where TSearchState : class, ISearchState
{
    public sealed override ExecutorFactory<ITerminationExecutor<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>, TSearchState>> CreateExecutorFactory() => _ => this;

    public abstract bool IsTerminalState(TSearchState state);

    bool ITerminationExecutor<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>, TSearchState>.IsTerminalState(TSearchState state, ISearchSpace<TCandidate> searchSpace, IProblem<TCandidate, ISearchSpace<TCandidate>> problem) =>
        IsTerminalState(state);
}

public abstract record StatelessTerminator<TCandidate>
    : Terminator<TCandidate>, ITerminationExecutor<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>, ISearchState>
{
    public sealed override ExecutorFactory<ITerminationExecutor<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>, ISearchState>> CreateExecutorFactory() => _ => this;

    public abstract bool IsTerminalState();

    bool ITerminationExecutor<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>, ISearchState>.IsTerminalState(ISearchState state, ISearchSpace<TCandidate> searchSpace, IProblem<TCandidate, ISearchSpace<TCandidate>> problem) =>
        IsTerminalState();
}
