using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Terminators;

public abstract record StatelessTerminator<TCandidate, TSearchSpace, TProblem, TSearchState> : Terminator<TCandidate, TSearchSpace, TProblem, TSearchState>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
    where TSearchState : class, ISearchState
{
    protected sealed override ITerminationExecutor<TSearchState> CreateExecutor(ResolutionScope<TCandidate, TSearchSpace, TProblem, TSearchState> scope) => new Executor(this, scope.Problem);

    public abstract bool IsTerminalState(TSearchState state, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem);

    private sealed class Executor(StatelessTerminator<TCandidate, TSearchSpace, TProblem, TSearchState> configuration, TProblem problem) : TerminationExecutor<TSearchState>
    {
        public override bool IsTerminalState(TSearchState state, IRandomNumberGenerator random) => configuration.IsTerminalState(state, random, problem.SearchSpace, problem);
    }
}

public abstract record StatelessTerminator<TCandidate, TSearchSpace, TSearchState> : Terminator<TCandidate, TSearchSpace, TSearchState>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TSearchState : class, ISearchState
{
    protected sealed override ITerminationExecutor<TSearchState> CreateExecutor(ResolutionScope<TCandidate, TSearchSpace, IProblem<TCandidate, TSearchSpace>, TSearchState> scope) => new Executor(this, scope.SearchSpace);

    public abstract bool IsTerminalState(TSearchState state, IRandomNumberGenerator random, TSearchSpace searchSpace);

    private sealed class Executor(StatelessTerminator<TCandidate, TSearchSpace, TSearchState> configuration, TSearchSpace searchSpace) : TerminationExecutor<TSearchState>
    {
        public override bool IsTerminalState(TSearchState state, IRandomNumberGenerator random) => configuration.IsTerminalState(state, random, searchSpace);
    }
}

public abstract record StatelessTerminator<TCandidate, TSearchState> : Terminator<TCandidate, TSearchState>, ITerminationExecutor<TSearchState>
    where TSearchState : class, ISearchState
{
    protected sealed override ITerminationExecutor<TSearchState> CreateExecutor(ResolutionScope<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>, TSearchState> scope) => this;

    public abstract bool IsTerminalState(TSearchState state, IRandomNumberGenerator random);
}

public abstract record StatelessTerminator<TCandidate> : Terminator<TCandidate>, ITerminationExecutor<ISearchState>
{
    protected sealed override ITerminationExecutor<ISearchState> CreateExecutor(ResolutionScope<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>, ISearchState> scope) => this;

    public abstract bool IsTerminalState();
    bool ITerminationExecutor<ISearchState>.IsTerminalState(ISearchState state, IRandomNumberGenerator random) => IsTerminalState();
}
