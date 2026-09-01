using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Mutators;

public abstract record WrappingMutator<TCandidate, TSearchSpace, TProblem>
    : Mutator<TCandidate, TSearchSpace, TProblem>, IInvariantContract<TCandidate>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    protected WrappingMutator(IMutator<TCandidate, TSearchSpace, TProblem> childMutator)
    {
        ChildMutator = childMutator;
    }

    public IMutator<TCandidate, TSearchSpace, TProblem> ChildMutator { get; init; }

    public sealed override IMutatorInstance<TCandidate, TSearchSpace, TProblem> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) =>
        CreateExecutionInstance(instanceRegistry.Resolve(ChildMutator));

    protected abstract WrappingMutatorInstance<TCandidate, TSearchSpace, TProblem> CreateExecutionInstance(IMutatorInstance<TCandidate, TSearchSpace, TProblem> childMutator);

    /// <summary>
    /// Answers from the wrapped mutator. A wrapper that only delegates, counts or measures cannot weaken what its child
    /// ensures.
    /// </summary>
    /// <remarks>
    /// Override in a wrapper that changes candidates itself rather than only delegating.
    /// </remarks>
    public virtual bool? Ensures(ISearchInvariant<TCandidate> invariant) => InvariantContractComposition.Ensures([ChildMutator], invariant);

    /// <summary>
    /// Requires whatever the wrapped mutator require, since any of them may be handed this operator's input.
    /// </summary>
    /// <remarks>
    /// Override in a wrapper that changes candidates itself rather than only delegating.
    /// </remarks>
    public virtual IReadOnlyList<ISearchInvariant<TCandidate>> RequiredInputInvariants =>
        InvariantContractComposition.RequiredInputInvariants<TCandidate>([ChildMutator]);
}

public abstract class WrappingMutatorInstance<TCandidate, TSearchSpace, TProblem>(IMutatorInstance<TCandidate, TSearchSpace, TProblem> childMutator)
    : MutatorInstance<TCandidate, TSearchSpace, TProblem>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    protected IMutatorInstance<TCandidate, TSearchSpace, TProblem> ChildMutator { get; } = childMutator;
}
