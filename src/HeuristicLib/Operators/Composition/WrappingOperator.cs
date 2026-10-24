using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators;

/// <summary>An operator configuration with one child supporting the same operation.</summary>
public abstract record WrappingOperator<TOperation, TCandidate> : Operator<TOperation, TCandidate>, IOperatorContract<TCandidate>
    where TOperation : IOperation<TOperation>
{
    protected WrappingOperator(IOperator<TOperation, TCandidate> childOperator)
    {
        ChildOperator = childOperator;
    }

    public IOperator<TOperation, TCandidate> ChildOperator { get; init; }

    public override bool Fits(ExecutionSignature execution) => execution.Fits(ChildOperator);

    public virtual bool? Ensures(ICandidateInvariant<TCandidate> invariant) => OperatorContractComposition.Ensures([ChildOperator], invariant);

    public virtual IReadOnlyList<ICandidateInvariant<TCandidate>> Requires => OperatorContractComposition.Requires<TCandidate>([ChildOperator]);
}
