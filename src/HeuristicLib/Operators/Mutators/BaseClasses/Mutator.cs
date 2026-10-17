using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Mutators;

/// <remarks>
/// Derive directly from this base when the mutator needs direct control over its execution structure.
/// Use <see cref="StatelessMutator{TCandidate,TSearchSpace,TProblem}"/> when no mutable execution data is needed.
/// Use <see cref="StatefulMutator{TCandidate,TSearchSpace,TProblem,TState}"/> when only ordinary execution data is needed.
/// <para>
/// The type arguments are the search space and problem this mutator is written for. The base bridges to whatever a
/// run requests, and a request the mutator was not written for is reported when the execution graph is built. A
/// mutator that owns children stays agnostic and derives from <see cref="WrappingOperator{Mutation, TCandidate}"/> or
/// <see cref="MultiOperator{Mutation, TCandidate}"/> instead.
/// </para>
/// </remarks>
public abstract record Mutator<TCandidate, TSearchSpace, TProblem>
    : Operator<Mutation, TCandidate, TSearchSpace, TProblem, IMutationExecutor<TCandidate, TSearchSpace, TProblem>>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>;

public abstract record Mutator<TCandidate, TSearchSpace>
    : Mutator<TCandidate, TSearchSpace, IProblem<TCandidate, TSearchSpace>>
    where TSearchSpace : class, ISearchSpace<TCandidate>;

public abstract record Mutator<TCandidate>
    : Mutator<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>>;

public abstract class MutationExecutor<TCandidate, TSearchSpace, TProblem>
    : IMutationExecutor<TCandidate, TSearchSpace, TProblem>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    public abstract IReadOnlyList<TCandidate> Mutate(IReadOnlyList<TCandidate> parents, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem);
}

public abstract class MutationExecutor<TCandidate, TSearchSpace>
    : IMutationExecutor<TCandidate, TSearchSpace, IProblem<TCandidate, TSearchSpace>>
    where TSearchSpace : class, ISearchSpace<TCandidate>
{
    public abstract IReadOnlyList<TCandidate> Mutate(IReadOnlyList<TCandidate> parents, IRandomNumberGenerator random, TSearchSpace searchSpace);

    IReadOnlyList<TCandidate> IMutationExecutor<TCandidate, TSearchSpace, IProblem<TCandidate, TSearchSpace>>.Mutate(IReadOnlyList<TCandidate> parents, IRandomNumberGenerator random, TSearchSpace searchSpace, IProblem<TCandidate, TSearchSpace> problem) =>
        Mutate(parents, random, searchSpace);
}

public abstract class MutationExecutor<TCandidate>
    : IMutationExecutor<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>>
{
    public abstract IReadOnlyList<TCandidate> Mutate(IReadOnlyList<TCandidate> parents, IRandomNumberGenerator random);

    IReadOnlyList<TCandidate> IMutationExecutor<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>>.Mutate(IReadOnlyList<TCandidate> parents, IRandomNumberGenerator random, ISearchSpace<TCandidate> searchSpace, IProblem<TCandidate, ISearchSpace<TCandidate>> problem) =>
        Mutate(parents, random);
}
