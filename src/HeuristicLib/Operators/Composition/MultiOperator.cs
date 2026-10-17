using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators;

/// <summary>An operator configuration with an ordered collection of children supporting the same operation.</summary>
public abstract record MultiOperator<TOperation, TCandidate> : Operator<TOperation, TCandidate>, IOperatorContract<TCandidate>
    where TOperation : IOperationDescriptor<TOperation>
{
    protected MultiOperator(IReadOnlyList<IOperator<TOperation, TCandidate>> childOperators)
    {
        ChildOperators = childOperators.ToValueArray();
    }

    public ValueArray<IOperator<TOperation, TCandidate>> ChildOperators { get; init; }

    public override bool Fits(ExecutionSignature execution) => execution.Fits([.. ChildOperators]);

    public virtual bool? Ensures(ICandidateInvariant<TCandidate> invariant) => OperatorContractComposition.Ensures(ChildOperators, invariant);

    public virtual IReadOnlyList<ICandidateInvariant<TCandidate>> Requires => OperatorContractComposition.Requires<TCandidate>(ChildOperators);
}
