using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Mutators;

/// <remarks>
/// A wrapping mutator owns a child, so it stays agnostic in the search space and problem and passes the run's triple
/// through unchanged.
/// </remarks>
public abstract record WrappingMutator<TCandidate>
    : IMutator<TCandidate>, IOperatorContract<TCandidate>
{
    protected WrappingMutator(IMutator<TCandidate> childMutator)
    {
        ChildMutator = childMutator;
    }

    public IMutator<TCandidate> ChildMutator { get; init; }

    /// <summary>
    /// Resolves the child over the run's search space and problem and hands it to
    /// <see cref="WrapExecutionInstance{TRunSearchSpace, TRunProblem}"/>.
    /// </summary>
    public IMutatorInstance<TCandidate, TRunSearchSpace, TRunProblem> CreateExecutionInstance<TRunSearchSpace, TRunProblem>(ExecutionInstanceRegistry instanceRegistry)
        where TRunSearchSpace : class, ISearchSpace<TCandidate>
        where TRunProblem : class, IProblem<TCandidate, TRunSearchSpace> =>
        WrapExecutionInstance(instanceRegistry.Resolve<TCandidate, TRunSearchSpace, TRunProblem>(ChildMutator));

    /// <summary>Wraps the child's execution instance in this operator's own.</summary>
    protected abstract IMutatorInstance<TCandidate, TRunSearchSpace, TRunProblem> WrapExecutionInstance<TRunSearchSpace, TRunProblem>(IMutatorInstance<TCandidate, TRunSearchSpace, TRunProblem> childMutator)
        where TRunSearchSpace : class, ISearchSpace<TCandidate>
        where TRunProblem : class, IProblem<TCandidate, TRunSearchSpace>;

    /// <summary>
    /// Answers from the wrapped mutator. A wrapper that only delegates, counts or measures cannot weaken what its
    /// child ensures.
    /// </summary>
    /// <remarks>Override in a wrapper that changes candidates itself rather than only delegating.</remarks>
    public virtual bool? Ensures(ICandidateInvariant<TCandidate> invariant) => OperatorContractComposition.Ensures([ChildMutator], invariant);

    /// <summary>Requires whatever the wrapped mutator requires, since it is handed this operator's input.</summary>
    /// <remarks>Override in a wrapper that changes candidates itself rather than only delegating.</remarks>
    public virtual IReadOnlyList<ICandidateInvariant<TCandidate>> Requires =>
        OperatorContractComposition.Requires<TCandidate>([ChildMutator]);
}

public abstract class WrappingMutatorInstance<TCandidate, TSearchSpace, TProblem>(IMutatorInstance<TCandidate, TSearchSpace, TProblem> childMutator)
    : MutatorInstance<TCandidate, TSearchSpace, TProblem>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    protected IMutatorInstance<TCandidate, TSearchSpace, TProblem> ChildMutator { get; } = childMutator;
}
