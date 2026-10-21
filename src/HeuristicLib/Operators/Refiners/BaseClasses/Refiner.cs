using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Refiners;

/// <remarks>
/// Derive directly from this base when the refiner needs direct control over its execution structure.
/// Use <see cref="StatelessRefiner{TCandidate,TSearchSpace,TProblem}"/> when no mutable execution data is needed.
/// Use <see cref="StatefulRefiner{TCandidate,TSearchSpace,TProblem,TState}"/> when only ordinary execution data is needed.
/// <para>
/// The type arguments are the search space and problem this refiner is written for. The base bridges to whatever a
/// run requests, and a request the refiner was not written for is reported when the execution graph is built. A
/// refiner that owns children stays agnostic and derives from <see cref="WrappingOperator{Refinement, TCandidate}"/> or
/// <see cref="MultiOperator{Refinement, TCandidate}"/> instead.
/// </para>
/// </remarks>
public abstract record Refiner<TCandidate, TSearchSpace, TProblem>
    : Operator<Refinement, TCandidate, TSearchSpace, TProblem, IRefinementExecutor<TCandidate, TSearchSpace, TProblem>>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>;

public abstract record Refiner<TCandidate, TSearchSpace>
    : Refiner<TCandidate, TSearchSpace, IProblem<TCandidate, TSearchSpace>>
    where TSearchSpace : class, ISearchSpace<TCandidate>;

public abstract record Refiner<TCandidate>
    : Refiner<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>>;

public abstract class RefinementExecutor<TCandidate, TSearchSpace, TProblem>
    : IRefinementExecutor<TCandidate, TSearchSpace, TProblem>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    public abstract IReadOnlyList<TCandidate> Refine(IReadOnlyList<TCandidate> candidates, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem);
}

public abstract class RefinementExecutor<TCandidate, TSearchSpace>
    : IRefinementExecutor<TCandidate, TSearchSpace, IProblem<TCandidate, TSearchSpace>>
    where TSearchSpace : class, ISearchSpace<TCandidate>
{
    public abstract IReadOnlyList<TCandidate> Refine(IReadOnlyList<TCandidate> candidates, IRandomNumberGenerator random, TSearchSpace searchSpace);

    IReadOnlyList<TCandidate> IRefinementExecutor<TCandidate, TSearchSpace, IProblem<TCandidate, TSearchSpace>>.Refine(IReadOnlyList<TCandidate> candidates, IRandomNumberGenerator random, TSearchSpace searchSpace, IProblem<TCandidate, TSearchSpace> problem) =>
        Refine(candidates, random, searchSpace);
}

public abstract class RefinementExecutor<TCandidate>
    : IRefinementExecutor<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>>
{
    public abstract IReadOnlyList<TCandidate> Refine(IReadOnlyList<TCandidate> candidates, IRandomNumberGenerator random);

    IReadOnlyList<TCandidate> IRefinementExecutor<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>>.Refine(IReadOnlyList<TCandidate> candidates, IRandomNumberGenerator random, ISearchSpace<TCandidate> searchSpace, IProblem<TCandidate, ISearchSpace<TCandidate>> problem) =>
        Refine(candidates, random);
}
