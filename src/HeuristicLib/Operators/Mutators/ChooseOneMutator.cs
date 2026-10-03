using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Operators.Mutators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators;

/// <summary>
/// Chooses one child mutator independently for each parent and restores the results to input order.
/// </summary>
/// <remarks>
/// Each selected mutator must return exactly one result for every parent assigned to it.
/// </remarks>
public sealed record ChooseOneMutator<TCandidate> : MultiMutator<TCandidate>
{
    /// <summary>
    /// Relative selection weight of each child mutator, in child order. An empty collection selects every child
    /// uniformly.
    /// </summary>
    /// <remarks>
    /// Weights are retained exactly as configured rather than normalized, so omitting them stays distinguishable from
    /// passing equal weights.
    /// </remarks>
    public ValueArray<double> Weights { get; init; }

    public ChooseOneMutator(IReadOnlyList<IMutator<TCandidate>> childMutators)
        : base(childMutators)
    {
    }

    protected override CompositeExecutionFactory<IMutatorExecution<TCandidate, TRunSearchSpace, TRunProblem>> CreateCompositeFactory<TRunSearchSpace, TRunProblem>()
    {
        if (ChildMutators.Count == 0)
            throw new InvalidOperationException("At least one mutator must be provided.");
        if (Weights.Count > 0 && Weights.Count != ChildMutators.Count)
            throw new InvalidOperationException("Weights must have the same length as mutators.");

        var dispatcher = new WeightedBatchDispatcher(ChildMutators.Count, Weights);
        return childMutators => new Execution<TRunSearchSpace, TRunProblem>(childMutators, dispatcher);
    }

    private sealed class Execution<TSearchSpace, TProblem>(ImmutableArray<IMutatorExecution<TCandidate, TSearchSpace, TProblem>> childMutators, WeightedBatchDispatcher dispatcher)
        : MultiMutatorExecution<TCandidate, TSearchSpace, TProblem>(childMutators)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        public override IReadOnlyList<TCandidate> Mutate(IReadOnlyList<TCandidate> parents, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem) =>
            dispatcher.Dispatch(
                parents,
                ChildMutators,
                random,
                (random, searchSpace, problem),
                static (mutator, batchParents, state) => mutator.Mutate(batchParents, state.random, state.searchSpace, state.problem));
    }
}

public static class ChooseOneMutator
{
    public static ChooseOneMutator<TCandidate> Create<TCandidate>(params IReadOnlyList<IMutator<TCandidate>> childMutators) =>
        new(childMutators);

    public static ChooseOneMutator<TCandidate> Create<TCandidate>(IReadOnlyList<IMutator<TCandidate>> childMutators, IReadOnlyList<double> weights) =>
        new(childMutators) { Weights = weights.ToValueArray() };
}

public static class ChooseOneMutatorExtensions
{
    extension<TCandidate>(IMutator<TCandidate> mutator)
    {
        public ChooseOneMutator<TCandidate> AppliedAtRate(double mutationRate) =>
            ChooseOneMutator.Create(
                [mutator, NoChangeMutator<TCandidate>.Instance],
                [mutationRate, double.IsNaN(mutationRate) ? double.PositiveInfinity : 1 - mutationRate]);
    }
}
