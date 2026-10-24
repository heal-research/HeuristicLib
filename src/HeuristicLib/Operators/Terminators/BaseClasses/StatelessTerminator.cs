using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Terminators;

public abstract record StatelessTerminator<TCandidate, TSearchSpace, TProblem, TSearchState>
    : Terminator<TCandidate, TSearchSpace, TProblem, TSearchState>, ITerminationExecutor<TCandidate, TSearchSpace, TProblem, TSearchState>
    where TSearchState : class, ISearchState
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    protected sealed override ITerminationExecutor<TCandidate, TSearchSpace, TProblem, TSearchState> CreateExecutor(ResolutionScope scope) => this;

    public abstract bool IsTerminalState(TSearchState state, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem);
}

public abstract record StatelessTerminator<TCandidate, TSearchSpace, TSearchState>
    : Terminator<TCandidate, TSearchSpace, TSearchState>, ITerminationExecutor<TCandidate, TSearchSpace, IProblem<TCandidate, TSearchSpace>, TSearchState>
    where TSearchState : class, ISearchState
    where TSearchSpace : class, ISearchSpace<TCandidate>
{
    protected sealed override ITerminationExecutor<TCandidate, TSearchSpace, IProblem<TCandidate, TSearchSpace>, TSearchState> CreateExecutor(ResolutionScope scope) => this;

    public abstract bool IsTerminalState(TSearchState state, IRandomNumberGenerator random, TSearchSpace searchSpace);

    bool ITerminationExecutor<TCandidate, TSearchSpace, IProblem<TCandidate, TSearchSpace>, TSearchState>.IsTerminalState(TSearchState state, IRandomNumberGenerator random, TSearchSpace searchSpace, IProblem<TCandidate, TSearchSpace> problem) =>
        IsTerminalState(state, random, searchSpace);
}

public abstract record StatelessTerminator<TCandidate, TSearchState>
    : Terminator<TCandidate, TSearchState>, ITerminationExecutor<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>, TSearchState>
    where TSearchState : class, ISearchState
{
    protected sealed override ITerminationExecutor<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>, TSearchState> CreateExecutor(ResolutionScope scope) => this;

    public abstract bool IsTerminalState(TSearchState state, IRandomNumberGenerator random);

    bool ITerminationExecutor<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>, TSearchState>.IsTerminalState(TSearchState state, IRandomNumberGenerator random, ISearchSpace<TCandidate> searchSpace, IProblem<TCandidate, ISearchSpace<TCandidate>> problem) =>
        IsTerminalState(state, random);
}

public abstract record StatelessTerminator<TCandidate>
    : Terminator<TCandidate>, ITerminationExecutor<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>, ISearchState>
{
    protected sealed override ITerminationExecutor<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>, ISearchState> CreateExecutor(ResolutionScope scope) => this;

    public abstract bool IsTerminalState();

    bool ITerminationExecutor<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>, ISearchState>.IsTerminalState(ISearchState state, IRandomNumberGenerator random, ISearchSpace<TCandidate> searchSpace, IProblem<TCandidate, ISearchSpace<TCandidate>> problem) =>
        IsTerminalState();
}
