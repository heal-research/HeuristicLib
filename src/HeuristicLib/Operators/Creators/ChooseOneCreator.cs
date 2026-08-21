using HEAL.HeuristicLib.Operators.Creators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators;

/// <summary>
/// Chooses one child creator independently for each requested candidate and restores the results to assignment order.
/// </summary>
/// <remarks>
/// Candidates assigned to the same creator are requested as one batch, so each selected creator must return exactly
/// the count assigned to it.
/// </remarks>
public sealed record ChooseOneCreator<TCandidate, TSearchSpace, TProblem>
    : MultiCreator<TCandidate, TSearchSpace, TProblem>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    /// <summary>
    /// Relative selection weight of each child creator, in child order. An empty collection selects every child
    /// uniformly.
    /// </summary>
    /// <remarks>
    /// Weights are retained exactly as configured rather than normalized, so omitting them stays distinguishable from
    /// passing equal weights.
    /// </remarks>
    public ValueArray<double> Weights { get; init; }

    public ChooseOneCreator(IReadOnlyList<ICreator<TCandidate, TSearchSpace, TProblem>> childCreators)
        : base(childCreators)
    {
    }

    protected override MultiCreatorInstance<TCandidate, TSearchSpace, TProblem> CreateExecutionInstance(ImmutableArray<ICreatorInstance<TCandidate, TSearchSpace, TProblem>> childCreators)
    {
        if (ChildCreators.Count == 0)
            throw new InvalidOperationException("At least one creator must be provided.");
        if (Weights.Count > 0 && Weights.Count != ChildCreators.Count)
            throw new InvalidOperationException("Weights must have the same length as creators.");

        return new Instance(childCreators, new WeightedBatchDispatcher(childCreators.Length, Weights));
    }

    private sealed class Instance(ImmutableArray<ICreatorInstance<TCandidate, TSearchSpace, TProblem>> childCreators, WeightedBatchDispatcher dispatcher)
        : MultiCreatorInstance<TCandidate, TSearchSpace, TProblem>(childCreators)
    {
        public override IReadOnlyList<TCandidate> Create(int count, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem) =>
            dispatcher.Dispatch(
                count,
                ChildCreators,
                random,
                (random, searchSpace, problem),
                static (creator, batchCount, state) => creator.Create(batchCount, state.random, state.searchSpace, state.problem));
    }
}

public static class ChooseOneCreator
{
    public static ChooseOneCreator<TCandidate, TSearchSpace, TProblem> Create<TCandidate, TSearchSpace, TProblem>(params IReadOnlyList<ICreator<TCandidate, TSearchSpace, TProblem>> childCreators)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace> =>
        new(childCreators);

    public static ChooseOneCreator<TCandidate, TSearchSpace, TProblem> Create<TCandidate, TSearchSpace, TProblem>(IReadOnlyList<ICreator<TCandidate, TSearchSpace, TProblem>> childCreators, IReadOnlyList<double> weights)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace> =>
        new(childCreators) { Weights = weights.ToValueArray() };
}
