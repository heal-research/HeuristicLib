using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Operators;

/// <summary>
/// Chooses one child by weight for each candidate result of a call, such as each mutated parent, crossed parent group or
/// requested candidate, and returns the results in order.
/// </summary>
/// <remarks>
/// Each call assigns all its items with the call's random number generator before any child runs. The items assigned to
/// one child run as one call on it, in item order and with the same generator; a child assigned every item receives the
/// original call. Each child must return one result per item it receives. Only operations marked with
/// <see cref="IItemOperation{TSelf}"/> support this: creation, crossover, mutation, evaluation and refinement. Whole-call
/// choice is <see cref="ChoosePerCall{TOperation, TCandidate}"/>. The choice exposes no controls of its children.
/// </remarks>
public sealed record ChoosePerCandidate<TOperation, TCandidate> : MultiOperator<TOperation, TCandidate>
    where TOperation : IItemOperation<TOperation>
{
    public ChoosePerCandidate(IReadOnlyList<IOperator<TOperation, TCandidate>> childOperators)
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
        return new Factory(this);
    }

    private sealed class Factory(ChoosePerCandidate<TOperation, TCandidate> source) : OperatorExecutorFactory<TOperation, TCandidate>
    {
        private readonly WeightedIndexSampler sampler = new(source.ChildOperators.Count, source.Weights);

        protected override IExecutor CreateExecutor<TSearchSpace, TProblem, TSearchState>(
            ResolutionScope<TCandidate, TSearchSpace, TProblem, TSearchState> scope)
        {
            var operation = OperationExecutorAdapter.For<TOperation, TCandidate, TSearchSpace, TProblem, TSearchState>();
            var children = new IExecutor[source.ChildOperators.Count];
            for (var i = 0; i < children.Length; i++)
                children[i] = scope.ResolveOperator(source.ChildOperators[i]);
            return operation.WrapInvocation(children[0], new CandidateChoiceWrapper(new WeightedRouter<IExecutor>(children, sampler)));
        }
    }
}

/// <summary>Runs each item of a call on the child the router assigns it; supplies no controls.</summary>
file readonly struct CandidateChoiceWrapper(WeightedRouter<IExecutor> router) : IInvocationWrapper, IExecutorControlProvider
{
    public TResult Invoke<TInvocation, TResult>(TInvocation invocation) where TInvocation : struct, IInvocation<TResult> =>
        invocation.ProceedPerItem(router);

    public TControl? Get<TControl>() where TControl : class => null;
}

public static class ChoosePerCandidate
{
    public static ChoosePerCandidate<TOperation, TCandidate> Create<TOperation, TCandidate>(params IReadOnlyList<IOperator<TOperation, TCandidate>> childOperators)
        where TOperation : IItemOperation<TOperation> => new(childOperators);

    public static ChoosePerCandidate<TOperation, TCandidate> Create<TOperation, TCandidate>(IReadOnlyList<IOperator<TOperation, TCandidate>> childOperators, IReadOnlyList<double> weights)
        where TOperation : IItemOperation<TOperation> => new(childOperators) { Weights = weights.ToValueArray() };
}

public static class ChoosePerCandidateExtensions
{
    extension<TCandidate>(IOperator<Crossover, TCandidate> crossover)
    {
        /// <summary>Crosses each parent group with the given probability and otherwise keeps its first parent.</summary>
        public ChoosePerCandidate<Crossover, TCandidate> AppliedAtRate(double crossoverRate) =>
            ChoosePerCandidate.Create([crossover, SelectFirstParentCrossover<TCandidate>.Instance], WeightedRouter.RateWeights(crossoverRate));
    }

    extension<TCandidate>(IOperator<Mutation, TCandidate> mutator)
    {
        /// <summary>Mutates each parent with the given probability and otherwise keeps it unchanged.</summary>
        public ChoosePerCandidate<Mutation, TCandidate> AppliedAtRate(double mutationRate) =>
            ChoosePerCandidate.Create([mutator, new NoChangeMutator<TCandidate>()], WeightedRouter.RateWeights(mutationRate));
    }

    extension<TCandidate>(IOperator<Refinement, TCandidate> refiner)
    {
        /// <summary>Refines each candidate with the given probability and otherwise keeps it unchanged.</summary>
        public ChoosePerCandidate<Refinement, TCandidate> AppliedAtRate(double refinementRate) =>
            ChoosePerCandidate.Create([refiner, NoChangeRefiner<TCandidate>.Instance], WeightedRouter.RateWeights(refinementRate));
    }
}
