using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators;

/// <summary>Chooses one child by weight for each complete call and runs that call on it.</summary>
/// <remarks>
/// Each call samples its child with the call's random number generator and then runs on that child with the same
/// generator. Per-item routing, which splits one call's items among children, is a different composition. The choice
/// exposes no controls of its children.
/// </remarks>
public sealed record ChoosePerCall<TOperation, TCandidate> : MultiOperator<TOperation, TCandidate>
    where TOperation : IOperation<TOperation>
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

    protected override IExecutor CreateExecutor<TSearchSpace, TProblem, TSearchState>(ResolutionScope<TCandidate, TSearchSpace, TProblem, TSearchState> scope)
    {
        var children = WeightedChoice.ResolveChildren(ChildOperators, Weights, scope);
        return TOperation.Wrap<TCandidate, TSearchSpace, TProblem, TSearchState, ChoiceWrapper>(children[0], new ChoiceWrapper(new WeightedItemSampler<IExecutor>(children, Weights)));
    }
}

/// <summary>Runs each call on a child sampled with the call's random number generator; supplies no controls.</summary>
file readonly struct ChoiceWrapper(WeightedItemSampler<IExecutor> sampler) : IInvocationWrapper, IExecutorControlProvider
{
    public TResult Invoke<TInvocation, TResult>(TInvocation invocation) where TInvocation : struct, IInvocation<TResult> =>
        invocation.Proceed(sampler.Sample(invocation.Random));

    public TControl? Get<TControl>() where TControl : class => null;
}

/// <summary>What <see cref="ChoosePerCall{TOperation, TCandidate}"/> and <see cref="ChoosePerItem{TOperation, TCandidate}"/> share.</summary>
internal static class WeightedChoice
{
    /// <summary>Checks the children and their weights, then resolves every child in child order.</summary>
    internal static IExecutor[] ResolveChildren<TOperation, TCandidate, TSearchSpace, TProblem, TSearchState>(
        ValueArray<IOperator<TOperation, TCandidate>> childOperators, ValueArray<double> weights,
        ResolutionScope<TCandidate, TSearchSpace, TProblem, TSearchState> scope)
        where TOperation : IOperation<TOperation>
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TSearchState : class, ISearchState
    {
        if (childOperators.Count == 0)
            throw new InvalidOperationException("At least one child operator must be provided.");
        if (weights.Count > 0 && weights.Count != childOperators.Count)
            throw new InvalidOperationException("Weights must have the same length as the child operators.");
        var children = new IExecutor[childOperators.Count];
        for (var i = 0; i < children.Length; i++)
            children[i] = scope.ResolveOperator(childOperators[i]);
        return children;
    }
}

public static class ChoosePerCall
{
    public static ChoosePerCall<TOperation, TCandidate> Create<TOperation, TCandidate>(params IReadOnlyList<IOperator<TOperation, TCandidate>> childOperators)
        where TOperation : IOperation<TOperation> => new(childOperators);

    public static ChoosePerCall<TOperation, TCandidate> Create<TOperation, TCandidate>(IReadOnlyList<IOperator<TOperation, TCandidate>> childOperators, IReadOnlyList<double> weights)
        where TOperation : IOperation<TOperation> => new(childOperators) { Weights = weights.ToValueArray() };
}
