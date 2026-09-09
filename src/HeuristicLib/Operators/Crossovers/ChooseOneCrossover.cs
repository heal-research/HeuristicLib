using HEAL.HeuristicLib.Operators.Crossovers;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators;

/// <summary>
/// Chooses one child crossover independently for each parent group and restores the results to input order.
/// </summary>
/// <remarks>
/// Each selected crossover must return exactly one result for every parent group assigned to it.
/// </remarks>
public sealed record ChooseOneCrossover<TCandidate>
    : MultiCrossover<TCandidate>
{
    /// <summary>
    /// Relative selection weight of each child crossover, in child order. An empty collection selects every child
    /// uniformly.
    /// </summary>
    /// <remarks>
    /// Weights are retained exactly as configured rather than normalized, so omitting them stays distinguishable from
    /// passing equal weights.
    /// </remarks>
    public ValueArray<double> Weights { get; init; }

    public ChooseOneCrossover(IReadOnlyList<ICrossover<TCandidate>> childCrossovers)
        : base(childCrossovers)
    {
    }

    protected override ICrossoverInstance<TCandidate, TRunSearchSpace, TRunProblem> CombineExecutionInstances<TRunSearchSpace, TRunProblem>(ImmutableArray<ICrossoverInstance<TCandidate, TRunSearchSpace, TRunProblem>> childCrossovers)
    {
        if (ChildCrossovers.Count == 0)
            throw new InvalidOperationException("At least one crossover must be provided.");
        if (Weights.Count > 0 && Weights.Count != ChildCrossovers.Count)
            throw new InvalidOperationException("Weights must have the same length as crossovers.");

        return new Instance<TRunSearchSpace, TRunProblem>(childCrossovers, new WeightedBatchDispatcher(childCrossovers.Length, Weights));
    }

    private sealed class Instance<TSearchSpace, TProblem>(ImmutableArray<ICrossoverInstance<TCandidate, TSearchSpace, TProblem>> childCrossovers, WeightedBatchDispatcher dispatcher)
        : MultiCrossoverInstance<TCandidate, TSearchSpace, TProblem>(childCrossovers)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        public override IReadOnlyList<TCandidate> Cross(IReadOnlyList<Parents<TCandidate>> parents, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem) =>
            dispatcher.Dispatch(
                parents,
                ChildCrossovers,
                random,
                (random, searchSpace, problem),
                static (crossover, batchParents, state) => crossover.Cross(batchParents, state.random, state.searchSpace, state.problem));
    }
}

public static class ChooseOneCrossover
{
    public static ChooseOneCrossover<TCandidate> Create<TCandidate>(params IReadOnlyList<ICrossover<TCandidate>> childCrossovers) =>
        new(childCrossovers);

    public static ChooseOneCrossover<TCandidate> Create<TCandidate>(IReadOnlyList<ICrossover<TCandidate>> childCrossovers, IReadOnlyList<double> weights) =>
        new(childCrossovers) { Weights = weights.ToValueArray() };
}

public static class ChooseOneCrossoverExtensions
{
    extension<TCandidate>(ICrossover<TCandidate> crossover)
    {
        public ChooseOneCrossover<TCandidate> WithRate(double crossoverRate) =>
            ChooseOneCrossover.Create(
                [crossover, SelectFirstParentCrossover<TCandidate>.Instance],
                [crossoverRate, double.IsNaN(crossoverRate) ? double.PositiveInfinity : 1 - crossoverRate]);
    }
}
