using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Mutators;

/// <remarks>
/// A multi mutator owns children, so it stays agnostic in the search space and problem and passes the run's triple
/// through unchanged. See <see cref="WrappingMutator{TCandidate}"/> for why binding is a leaf concept.
/// </remarks>
public abstract record MultiMutator<TCandidate>
    : IMutator<TCandidate>, IInvariantContract<TCandidate>
{
    protected MultiMutator(IReadOnlyList<IMutator<TCandidate>> childMutators)
    {
        ChildMutators = childMutators.ToValueArray();
    }

    public ValueArray<IMutator<TCandidate>> ChildMutators { get; init; }

    /// <summary>
    /// Resolves each child over the run's search space and problem and hands them to
    /// <see cref="CombineExecutionInstances{TRunSearchSpace, TRunProblem}"/>. Left visible, because unlike a leaf
    /// mutator this base offers no other creation member and hiding it would leave an author with no view of the
    /// mechanism their override plugs into.
    /// </summary>
    public IMutatorInstance<TCandidate, TRunSearchSpace, TRunProblem> CreateExecutionInstance<TRunSearchSpace, TRunProblem>(ExecutionInstanceRegistry instanceRegistry)
        where TRunSearchSpace : class, ISearchSpace<TCandidate>
        where TRunProblem : class, IProblem<TCandidate, TRunSearchSpace>
    {
        var resolver = instanceRegistry.For<TCandidate, TRunSearchSpace, TRunProblem>();
        return CombineExecutionInstances([.. ChildMutators.Select(child => resolver.Resolve(child))]);
    }

    /// <summary>Combines the children's execution instances into this operator's own.</summary>
    protected abstract IMutatorInstance<TCandidate, TRunSearchSpace, TRunProblem> CombineExecutionInstances<TRunSearchSpace, TRunProblem>(ImmutableArray<IMutatorInstance<TCandidate, TRunSearchSpace, TRunProblem>> childMutators)
        where TRunSearchSpace : class, ISearchSpace<TCandidate>
        where TRunProblem : class, IProblem<TCandidate, TRunSearchSpace>;

    /// <summary>
    /// Answers from the child mutators. Any child may run, so an invariant survives only when none of them breaks it.
    /// </summary>
    /// <remarks>Override in a multi mutator that changes candidates itself rather than only delegating.</remarks>
    public virtual bool? Ensures(ISearchInvariant<TCandidate> invariant) => InvariantContractComposition.Ensures(ChildMutators, invariant);

    /// <summary>Requires whatever the child mutators require, since any of them may be handed this operator's input.</summary>
    /// <remarks>Override in a multi mutator that changes candidates itself rather than only delegating.</remarks>
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
