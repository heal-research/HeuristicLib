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

    public virtual bool Fits(ExecutionSignature execution) => execution.Fits(ChildMutator);

    /// <summary>Prepares this operator once, then binds its child in each construction scope.</summary>
    public ExecutionFactory<IMutatorExecution<TCandidate, TRunSearchSpace, TRunProblem>> CreateExecutionFactory<TRunSearchSpace, TRunProblem>()
        where TRunSearchSpace : class, ISearchSpace<TCandidate>
        where TRunProblem : class, IProblem<TCandidate, TRunSearchSpace>
    {
        var create = CreateWrapperFactory<TRunSearchSpace, TRunProblem>();
        return scope => create(scope.Resolve<TCandidate, TRunSearchSpace, TRunProblem>(ChildMutator));
    }

    /// <summary>Prepares persistent execution data and returns a constructor accepting the resolved child.</summary>
    /// <remarks>Allocate shared state here; construct nodes with their contextual children in the returned delegate.</remarks>
    protected abstract WrapperExecutionFactory<IMutatorExecution<TCandidate, TRunSearchSpace, TRunProblem>> CreateWrapperFactory<TRunSearchSpace, TRunProblem>()
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

public abstract class WrappingMutatorExecution<TCandidate, TSearchSpace, TProblem>(IMutatorExecution<TCandidate, TSearchSpace, TProblem> childMutator)
    : MutatorExecution<TCandidate, TSearchSpace, TProblem>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    protected IMutatorExecution<TCandidate, TSearchSpace, TProblem> ChildMutator { get; } = childMutator;
}
