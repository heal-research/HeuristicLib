using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Mutators;

/// <remarks>
/// Derive directly from this base when the mutator needs direct control over its execution structure.
/// Use <see cref="StatelessMutator{TCandidate,TSearchSpace,TProblem}"/> when no mutable execution data is needed.
/// <para>
/// The type arguments are the search space and problem this mutator is written for. The base bridges to whatever a
/// run requests, and a request the mutator was not written for is reported when the execution graph is built. A
/// mutator that owns children stays agnostic and derives from <see cref="WrappingOperator{Mutation, TCandidate}"/> or
/// <see cref="MultiOperator{Mutation, TCandidate}"/> instead.
/// </para>
/// </remarks>
public abstract record Mutator<TCandidate, TSearchSpace, TProblem>
    : Operator<Mutation, TCandidate, TSearchSpace, TProblem, IMutationExecutor<TCandidate>>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>;

public abstract record Mutator<TCandidate, TSearchSpace>
    : Mutator<TCandidate, TSearchSpace, IProblem<TCandidate, TSearchSpace>>
    where TSearchSpace : class, ISearchSpace<TCandidate>;

public abstract record Mutator<TCandidate>
    : Mutator<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>>;

public abstract class MutationExecutor<TCandidate>
    : IMutationExecutor<TCandidate>
{
    public abstract IReadOnlyList<TCandidate> Mutate(IReadOnlyList<TCandidate> parents, IRandomNumberGenerator random);
}
