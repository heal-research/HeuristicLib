using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Mutators;

/// <summary>
/// Chooses one child mutator independently for each parent and restores the results to input order.
/// </summary>
/// <remarks>
/// Each selected mutator must return exactly one result for every parent assigned to it.
/// </remarks>
public sealed record ChooseOneMutator<TCandidate, TSearchSpace, TProblem>
    : MultiMutator<TCandidate, TSearchSpace, TProblem>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
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

    public ChooseOneMutator(IReadOnlyList<IMutator<TCandidate, TSearchSpace, TProblem>> childMutators)
        : base(childMutators)
    {
    }

    protected override MultiMutatorInstance<TCandidate, TSearchSpace, TProblem> CreateExecutionInstance(ImmutableArray<IMutatorInstance<TCandidate, TSearchSpace, TProblem>> childMutators)
    {
        if (ChildMutators.Count == 0)
            throw new InvalidOperationException("At least one mutator must be provided.");
        if (Weights.Count > 0 && Weights.Count != ChildMutators.Count)
            throw new InvalidOperationException("Weights must have the same length as mutators.");

        return new Instance(childMutators, new WeightedBatchDispatcher(childMutators.Length, Weights));
    }

    private sealed class Instance(ImmutableArray<IMutatorInstance<TCandidate, TSearchSpace, TProblem>> childMutators, WeightedBatchDispatcher dispatcher)
        : MultiMutatorInstance<TCandidate, TSearchSpace, TProblem>(childMutators)
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
    public static ChooseOneMutator<TCandidate, TSearchSpace, TProblem> Create<TCandidate, TSearchSpace, TProblem>(params IReadOnlyList<IMutator<TCandidate, TSearchSpace, TProblem>> childMutators)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace> =>
        new(childMutators);

    public static ChooseOneMutator<TCandidate, TSearchSpace, TProblem> Create<TCandidate, TSearchSpace, TProblem>(IReadOnlyList<IMutator<TCandidate, TSearchSpace, TProblem>> childMutators, IReadOnlyList<double> weights)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace> =>
        new(childMutators) { Weights = weights.ToValueArray() };
}

public static class ChooseOneMutatorExtensions
{
    extension<TCandidate, TSearchSpace, TProblem>(IMutator<TCandidate, TSearchSpace, TProblem> mutator)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        public ChooseOneMutator<TCandidate, TSearchSpace, TProblem> WithRate(double mutationRate) =>
            ChooseOneMutator.Create(
                [mutator, NoChangeMutator<TCandidate>.Instance],
                [mutationRate, double.IsNaN(mutationRate) ? double.PositiveInfinity : 1 - mutationRate]);
    }
}
