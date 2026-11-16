using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Terminators;

/// <remarks>
/// Derive directly from this base when the terminator needs direct control over its execution structure.
/// Use <see cref="StatelessTerminator{TCandidate,TSearchSpace,TProblem,TSearchState}"/> when no mutable execution data is needed.
/// <para>
/// The type arguments are the search space, problem and search state this terminator is written for. The base bridges
/// to whatever a run requests, and a request the terminator was not written for is reported when the execution graph
/// is built. A terminator that owns children stays agnostic and derives from
/// <see cref="WrappingOperator{Termination, TCandidate}"/> or <see cref="MultiOperator{Termination, TCandidate}"/> instead.
/// </para>
/// </remarks>
public abstract record Terminator<TCandidate, TSearchSpace, TProblem, TSearchState>
    : Operator<Termination, TCandidate, TSearchSpace, TProblem, TSearchState, ITerminationExecutor<TSearchState>>
    where TSearchState : class, ISearchState
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>;

public abstract record Terminator<TCandidate, TSearchSpace, TSearchState>
    : Terminator<TCandidate, TSearchSpace, IProblem<TCandidate, TSearchSpace>, TSearchState>
    where TSearchState : class, ISearchState
    where TSearchSpace : class, ISearchSpace<TCandidate>;

public abstract record Terminator<TCandidate, TSearchState>
    : Terminator<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>, TSearchState>
    where TSearchState : class, ISearchState;

public abstract record Terminator<TCandidate>
    : Terminator<TCandidate, ISearchState>;

public abstract class TerminationExecutor<TCandidate, TSearchSpace, TProblem, TSearchState>
    : ITerminationExecutor<TSearchState>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
    where TSearchState : class, ISearchState
{
    protected TProblem Problem { get; }
    protected TSearchSpace SearchSpace { get; }

    protected TerminationExecutor(TProblem problem)
    {
        Problem = problem;
        SearchSpace = problem.SearchSpace;
    }

    public abstract bool IsTerminalState(TSearchState state, IRandomNumberGenerator random);
}

public abstract class TerminationExecutor<TCandidate, TSearchSpace, TSearchState>
    : ITerminationExecutor<TSearchState>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TSearchState : class, ISearchState
{
    protected TSearchSpace SearchSpace { get; }

    protected TerminationExecutor(TSearchSpace searchSpace)
    {
        SearchSpace = searchSpace;
    }

    public abstract bool IsTerminalState(TSearchState state, IRandomNumberGenerator random);
}

public abstract class TerminationExecutor<TSearchState>
    : ITerminationExecutor<TSearchState>
    where TSearchState : class, ISearchState
{
    public abstract bool IsTerminalState(TSearchState state, IRandomNumberGenerator random);
}

public abstract class TerminationExecutor : ITerminationExecutor<ISearchState>
{
    public abstract bool IsTerminalState();
    bool ITerminationExecutor<ISearchState>.IsTerminalState(ISearchState state, IRandomNumberGenerator random) => IsTerminalState();
}
