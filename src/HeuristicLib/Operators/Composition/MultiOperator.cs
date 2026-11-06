using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators;

/// <summary>An operator configuration with an ordered collection of children supporting the same operation.</summary>
public abstract record MultiOperator<TOperation, TCandidate> : Operator<TOperation, TCandidate>, IOperatorContract<TCandidate>
    where TOperation : IOperation<TOperation>
{
    protected MultiOperator(IReadOnlyList<IOperator<TOperation, TCandidate>> childOperators)
    {
        ChildOperators = childOperators.ToValueArray();
    }

    public ValueArray<IOperator<TOperation, TCandidate>> ChildOperators { get; init; }

    public static ListParameter<MultiOperator<TOperation, TCandidate>, IOperator<TOperation, TCandidate>> ChildOperatorsParameter { get; } =
        new(nameof(ChildOperators), static multi => multi.ChildOperators, static (multi, value) => multi with { ChildOperators = value.ToValueArray() });

    public static IReadOnlyList<IParameter> Parameters { get; } = [ChildOperatorsParameter];

    public override IReadOnlyList<IParameter> GetParameters() => [.. base.GetParameters(), .. Parameters];

    public virtual bool? Ensures(ICandidateInvariant<TCandidate> invariant) => OperatorContractComposition.Ensures(ChildOperators, invariant);

    public virtual IReadOnlyList<ICandidateInvariant<TCandidate>> Requires => OperatorContractComposition.Requires<TCandidate>(ChildOperators);
}
