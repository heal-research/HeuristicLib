using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Creators;

/// <remarks>
/// Derive directly from this base when the creator needs direct control over its execution structure.
/// Use <see cref="StatelessCreator{TCandidate,TSearchSpace,TProblem}"/> when no mutable execution data is needed.
/// Use <see cref="StatefulCreator{TCandidate,TSearchSpace,TProblem,TState}"/> when only ordinary execution data is needed.
/// <para>
/// The type arguments are the search space and problem this creator is written for. The base bridges to whatever a
/// run requests, and a request the creator was not written for is reported when the execution graph is built. A
/// creator that owns children stays agnostic and derives from <see cref="WrappingOperator{Creation, TCandidate}"/> or
/// <see cref="MultiOperator{Creation, TCandidate}"/> instead.
/// </para>
/// </remarks>
public abstract record Creator<TCandidate, TSearchSpace, TProblem>
    : Operator<Creation, TCandidate, TSearchSpace, TProblem, ICreationExecutor<TCandidate, TSearchSpace, TProblem>>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>;

public abstract record Creator<TCandidate, TSearchSpace>
    : Creator<TCandidate, TSearchSpace, IProblem<TCandidate, TSearchSpace>>
    where TSearchSpace : class, ISearchSpace<TCandidate>;

public abstract record Creator<TCandidate>
    : Creator<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>>;

public abstract class CreationExecutor<TCandidate, TSearchSpace, TProblem>
    : ICreationExecutor<TCandidate, TSearchSpace, TProblem>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    public abstract IReadOnlyList<TCandidate> Create(int count, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem);
}

public abstract class CreationExecutor<TCandidate, TSearchSpace>
    : ICreationExecutor<TCandidate, TSearchSpace, IProblem<TCandidate, TSearchSpace>>
    where TSearchSpace : class, ISearchSpace<TCandidate>
{
    public abstract IReadOnlyList<TCandidate> Create(int count, IRandomNumberGenerator random, TSearchSpace searchSpace);

    IReadOnlyList<TCandidate> ICreationExecutor<TCandidate, TSearchSpace, IProblem<TCandidate, TSearchSpace>>.Create(int count, IRandomNumberGenerator random, TSearchSpace searchSpace, IProblem<TCandidate, TSearchSpace> problem) =>
        Create(count, random, searchSpace);
}

public abstract class CreationExecutor<TCandidate>
    : ICreationExecutor<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>>
{
    public abstract IReadOnlyList<TCandidate> Create(int count, IRandomNumberGenerator random);

    IReadOnlyList<TCandidate> ICreationExecutor<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>>.Create(int count, IRandomNumberGenerator random, ISearchSpace<TCandidate> searchSpace, IProblem<TCandidate, ISearchSpace<TCandidate>> problem) =>
        Create(count, random);
}
