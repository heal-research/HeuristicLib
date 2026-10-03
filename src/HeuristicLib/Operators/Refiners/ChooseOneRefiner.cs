using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Operators.Refiners;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators;

/// <summary>
/// Chooses one child refiner independently for each candidate and restores the results to input order.
/// </summary>
/// <remarks>
/// Each selected refiner must return exactly one result for every candidate assigned to it.
/// </remarks>
public sealed record ChooseOneRefiner<TCandidate>
    : MultiRefiner<TCandidate>
{
    /// <summary>
    /// Relative selection weight of each child refiner, in child order. An empty collection selects every child
    /// uniformly.
    /// </summary>
    /// <remarks>
    /// Weights are retained exactly as configured rather than normalized, so omitting them stays distinguishable from
    /// passing equal weights.
    /// </remarks>
    public ValueArray<double> Weights { get; init; }

    public ChooseOneRefiner(IReadOnlyList<IRefiner<TCandidate>> childRefiners)
        : base(childRefiners)
    {
    }

    protected override CompositeExecutionFactory<IRefinerExecution<TCandidate, TRunSearchSpace, TRunProblem>> CreateCompositeFactory<TRunSearchSpace, TRunProblem>()
    {
        if (ChildRefiners.Count == 0)
            throw new InvalidOperationException("At least one refiner must be provided.");
        if (Weights.Count > 0 && Weights.Count != ChildRefiners.Count)
            throw new InvalidOperationException("Weights must have the same length as refiners.");

        var dispatcher = new WeightedBatchDispatcher(ChildRefiners.Count, Weights);
        return childRefiners => new Execution<TRunSearchSpace, TRunProblem>(childRefiners, dispatcher);
    }

    private sealed class Execution<TSearchSpace, TProblem>(ImmutableArray<IRefinerExecution<TCandidate, TSearchSpace, TProblem>> childRefiners, WeightedBatchDispatcher dispatcher)
        : MultiRefinerExecution<TCandidate, TSearchSpace, TProblem>(childRefiners)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        public override IReadOnlyList<TCandidate> Refine(IReadOnlyList<TCandidate> candidates, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem) =>
            dispatcher.Dispatch(
                candidates,
                ChildRefiners,
                random,
                (random, searchSpace, problem),
                static (refiner, batchCandidates, state) => refiner.Refine(batchCandidates, state.random, state.searchSpace, state.problem));
    }
}

public static class ChooseOneRefiner
{
    public static ChooseOneRefiner<TCandidate> Create<TCandidate>(params IReadOnlyList<IRefiner<TCandidate>> childRefiners) =>
        new(childRefiners);

    public static ChooseOneRefiner<TCandidate> Create<TCandidate>(IReadOnlyList<IRefiner<TCandidate>> childRefiners, IReadOnlyList<double> weights) =>
        new(childRefiners) { Weights = weights.ToValueArray() };
}

public static class ChooseOneRefinerExtensions
{
    extension<TCandidate>(IRefiner<TCandidate> refiner)
    {
        public ChooseOneRefiner<TCandidate> AppliedAtRate(double refinementRate) =>
            ChooseOneRefiner.Create(
                [refiner, NoChangeRefiner<TCandidate>.Instance],
                [refinementRate, double.IsNaN(refinementRate) ? double.PositiveInfinity : 1 - refinementRate]);
    }
}
