using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Operators;

/// <summary>Chooses one child by weight for each complete call and runs that call on it.</summary>
/// <remarks>
/// Each call samples its child with the call's random number generator and then runs on that child with the same
/// generator. Per-candidate routing, which splits one call's inputs among children, is a different composition. The
/// choice exposes no controls of its children.
/// </remarks>
public sealed record ChoosePerCall<TOperation, TCandidate> : MultiOperator<TOperation, TCandidate>
    where TOperation : IOperationDescriptor<TOperation>
{
    public ChoosePerCall(IReadOnlyList<IOperator<TOperation, TCandidate>> childOperators)
        : base(childOperators)
    {
    }

    /// <summary>Relative weight of each child, in child order. An empty collection chooses every child uniformly.</summary>
    /// <remarks>
    /// Weights are retained exactly as configured and follow <see cref="WeightedIndexSampler"/> semantics for zero,
    /// negative, infinite and NaN values.
    /// </remarks>
    public ValueArray<double> Weights { get; init; }

    protected override OperatorExecutorFactory CreateOperatorFactory()
    {
        if (ChildOperators.Count == 0)
            throw new InvalidOperationException("At least one child operator must be provided.");
        if (Weights.Count > 0 && Weights.Count != ChildOperators.Count)
            throw new InvalidOperationException("Weights must have the same length as the child operators.");
        return new Factory(ChildOperators, Weights);
    }

    private sealed class Factory(ValueArray<IOperator<TOperation, TCandidate>> childOperators, ValueArray<double> weights)
        : OperatorExecutorFactory<TOperation, TCandidate>
    {
        protected override IExecutor CreateExecutor<TSearchSpace, TProblem, TSearchState>(
            ResolutionScope<TCandidate, TSearchSpace, TProblem, TSearchState> scope)
        {
            var children = new IExecutor[childOperators.Count];
            for (var i = 0; i < children.Length; i++)
                children[i] = scope.ResolveOperator(childOperators[i]);
            return OperationExecutorAdapter.For<TOperation, TCandidate, TSearchSpace, TProblem, TSearchState>()
                .WrapInvocation(children[0], new ChoiceWrapper(new WeightedItemSampler<IExecutor>(children, weights)));
        }
    }
}

/// <summary>Runs each call on a child sampled with the call's random number generator; supplies no controls.</summary>
file readonly struct ChoiceWrapper(WeightedItemSampler<IExecutor> sampler) : IInvocationWrapper, IExecutorControlProvider
{
    public TResult Invoke<TInvocation, TResult>(TInvocation invocation) where TInvocation : struct, IInvocation<TResult> =>
        invocation.ProceedWith(sampler.Sample(invocation.Random));

    public TControl? Get<TControl>() where TControl : class => null;
}

public static class ChoosePerCall
{
    public static ChoosePerCall<TOperation, TCandidate> Create<TOperation, TCandidate>(params IReadOnlyList<IOperator<TOperation, TCandidate>> childOperators)
        where TOperation : IOperationDescriptor<TOperation> => new(childOperators);

    public static ChoosePerCall<TOperation, TCandidate> Create<TOperation, TCandidate>(IReadOnlyList<IOperator<TOperation, TCandidate>> childOperators, IReadOnlyList<double> weights)
        where TOperation : IOperationDescriptor<TOperation> => new(childOperators) { Weights = weights.ToValueArray() };
}
