using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Operators;

/// <summary>
/// Chooses one child by weight for each item of a call, such as each mutated parent, crossed parent group, evaluated
/// candidate or requested candidate, and returns the item results in order.
/// </summary>
/// <remarks>
/// Each call assigns all its items with the call's random number generator before any child runs. The items assigned to
/// one child run as one call on it, in item order and with the same generator; a child assigned every item receives the
/// original call. Each child must return one result per item it receives. Only operations marked with
/// <see cref="IItemOperation{TSelf}"/> support this.
/// Whole-call choice is <see cref="ChoosePerCall{TOperation, TCandidate}"/>. The choice exposes no controls of its
/// children.
/// </remarks>
public sealed record ChoosePerItem<TOperation, TCandidate> : MultiOperator<TOperation, TCandidate>
    where TOperation : IItemOperation<TOperation>
{
    public ChoosePerItem(IReadOnlyList<IOperator<TOperation, TCandidate>> childOperators)
        : base(childOperators)
    {
    }

    /// <summary>Relative weight of each child, in child order. An empty collection chooses every child uniformly.</summary>
    /// <remarks>
    /// Weights are retained exactly as configured and follow <see cref="WeightedIndexSampler"/> semantics for zero,
    /// negative, infinite and NaN values.
    /// </remarks>
    public ValueArray<double> Weights { get; init; }

    protected override IExecutor CreateExecutor<TSearchSpace, TProblem, TSearchState>(ResolutionScope<TCandidate, TSearchSpace, TProblem, TSearchState> scope) =>
        TOperation.Route<TCandidate, TSearchSpace, TProblem, TSearchState>(
            new WeightedRouter<IExecutor>(WeightedChoice.ResolveChildren(ChildOperators, Weights, scope), Weights));
}

public static class ChoosePerItem
{
    public static ChoosePerItem<TOperation, TCandidate> Create<TOperation, TCandidate>(params IReadOnlyList<IOperator<TOperation, TCandidate>> childOperators)
        where TOperation : IItemOperation<TOperation> => new(childOperators);

    public static ChoosePerItem<TOperation, TCandidate> Create<TOperation, TCandidate>(IReadOnlyList<IOperator<TOperation, TCandidate>> childOperators, IReadOnlyList<double> weights)
        where TOperation : IItemOperation<TOperation> => new(childOperators) { Weights = weights.ToValueArray() };
}

public static class ChoosePerItemExtensions
{
    extension<TCandidate>(IOperator<Crossover, TCandidate> crossover)
    {
        /// <summary>Crosses each parent group with the given probability and otherwise keeps its first parent.</summary>
        public ChoosePerItem<Crossover, TCandidate> AppliedAtRate(double crossoverRate) =>
            ChoosePerItem.Create([crossover, SelectFirstParentCrossover<TCandidate>.Instance], WeightedRouter.RateWeights(crossoverRate));
    }

    extension<TCandidate>(IOperator<Mutation, TCandidate> mutator)
    {
        /// <summary>Mutates each parent with the given probability and otherwise keeps it unchanged.</summary>
        public ChoosePerItem<Mutation, TCandidate> AppliedAtRate(double mutationRate) =>
            ChoosePerItem.Create([mutator, new NoChangeMutator<TCandidate>()], WeightedRouter.RateWeights(mutationRate));
    }

    extension<TCandidate>(IOperator<Refinement, TCandidate> refiner)
    {
        /// <summary>Refines each candidate with the given probability and otherwise keeps it unchanged.</summary>
        public ChoosePerItem<Refinement, TCandidate> AppliedAtRate(double refinementRate) =>
            ChoosePerItem.Create([refiner, NoChangeRefiner<TCandidate>.Instance], WeightedRouter.RateWeights(refinementRate));
    }
}
