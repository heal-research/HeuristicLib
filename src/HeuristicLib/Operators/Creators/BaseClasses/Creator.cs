using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Creators;

/// <remarks>
/// Derive directly from this base when the creator needs direct control over its execution structure.
/// Use <see cref="StatelessCreator{TCandidate,TSearchSpace,TProblem}"/> when no mutable execution data is needed.
/// <para>
/// The type arguments are the search space and problem this creator is written for. The base bridges to whatever a
/// run requests, and a request the creator was not written for is reported when the execution graph is built. A
/// creator that owns children stays agnostic and derives from <see cref="WrappingOperator{Creation, TCandidate}"/> or
/// <see cref="MultiOperator{Creation, TCandidate}"/> instead.
/// </para>
/// </remarks>
public abstract record Creator<TCandidate, TSearchSpace, TProblem>
    : Operator<Creation, TCandidate, TSearchSpace, TProblem, ICreationExecutor<TCandidate>>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>;

public abstract record Creator<TCandidate, TSearchSpace>
    : Creator<TCandidate, TSearchSpace, IProblem<TCandidate, TSearchSpace>>
    where TSearchSpace : class, ISearchSpace<TCandidate>;

public abstract record Creator<TCandidate>
    : Creator<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>>;

public abstract class CreationExecutor<TCandidate, TSearchSpace, TProblem>
    : ICreationExecutor<TCandidate>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    protected TProblem Problem { get; }
    protected TSearchSpace SearchSpace { get; }

    protected CreationExecutor(TProblem problem)
    {
        Problem = problem;
        SearchSpace = problem.SearchSpace;
    }

    public abstract IReadOnlyList<TCandidate> Create(int count, IRandomNumberGenerator random);
}

public abstract class CreationExecutor<TCandidate, TSearchSpace>
    : ICreationExecutor<TCandidate>
    where TSearchSpace : class, ISearchSpace<TCandidate>
{
    protected TSearchSpace SearchSpace { get; }

    protected CreationExecutor(TSearchSpace searchSpace)
    {
        SearchSpace = searchSpace;
    }

    public abstract IReadOnlyList<TCandidate> Create(int count, IRandomNumberGenerator random);
}

public abstract class CreationExecutor<TCandidate>
    : ICreationExecutor<TCandidate>
{
    public abstract IReadOnlyList<TCandidate> Create(int count, IRandomNumberGenerator random);
}
