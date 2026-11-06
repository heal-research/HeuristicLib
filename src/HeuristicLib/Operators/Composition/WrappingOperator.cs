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

    public static Parameter<WrappingOperator<TOperation, TCandidate>, IOperator<TOperation, TCandidate>> ChildOperatorParameter { get; } =
        new(nameof(ChildOperator), static wrapper => wrapper.ChildOperator, static (wrapper, value) => wrapper with { ChildOperator = value });

    public static IReadOnlyList<IParameter> Parameters { get; } = [ChildOperatorParameter];

    public override IReadOnlyList<IParameter> GetParameters() => [.. base.GetParameters(), .. Parameters];

    public override bool Fits(ExecutionSignature execution) => execution.Fits(ChildOperator);

    public virtual bool? Ensures(ICandidateInvariant<TCandidate> invariant) => OperatorContractComposition.Ensures([ChildOperator], invariant);

    public virtual IReadOnlyList<ICandidateInvariant<TCandidate>> Requires => OperatorContractComposition.Requires<TCandidate>([ChildOperator]);
}
