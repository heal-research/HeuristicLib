using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Mutators;

public abstract record MultiMutator<TCandidate, TSearchSpace, TProblem>
    : Mutator<TCandidate, TSearchSpace, TProblem>, IInvariantContract<TCandidate>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    protected MultiMutator(IReadOnlyList<IMutator<TCandidate, TSearchSpace, TProblem>> childMutators)
    {
        ChildMutators = childMutators.ToValueArray();
    }

    public ValueArray<IMutator<TCandidate, TSearchSpace, TProblem>> ChildMutators { get; init; }

    public sealed override IMutatorInstance<TCandidate, TSearchSpace, TProblem> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) =>
        CreateExecutionInstance([.. ChildMutators.Select(instanceRegistry.Resolve)]);

    protected abstract MultiMutatorInstance<TCandidate, TSearchSpace, TProblem> CreateExecutionInstance(ImmutableArray<IMutatorInstance<TCandidate, TSearchSpace, TProblem>> childMutators);

    /// <summary>
    /// Answers from the child mutators. Any child may run, so an invariant survives only when none of them breaks it.
    /// </summary>
    /// <remarks>
    /// Override in a multi mutator that changes candidates itself rather than only delegating.
    /// </remarks>
    public virtual bool? Ensures(ISearchInvariant<TCandidate> invariant) => InvariantContractComposition.Ensures(ChildMutators, invariant);

    /// <summary>
    /// Requires whatever the child mutators require, since any of them may be handed this operator's input.
    /// </summary>
    /// <remarks>
    /// Override in a multi mutator that changes candidates itself rather than only delegating.
    /// </remarks>
    public virtual IReadOnlyList<ISearchInvariant<TCandidate>> RequiredInputInvariants =>
        InvariantContractComposition.RequiredInputInvariants<TCandidate>(ChildMutators);
}

public abstract class MultiMutatorInstance<TCandidate, TSearchSpace, TProblem>(ImmutableArray<IMutatorInstance<TCandidate, TSearchSpace, TProblem>> childMutators)
    : MutatorInstance<TCandidate, TSearchSpace, TProblem>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    protected ImmutableArray<IMutatorInstance<TCandidate, TSearchSpace, TProblem>> ChildMutators { get; } = childMutators;
}
