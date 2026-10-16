using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Operators.Selectors;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators;

/// <summary>
/// Selects one child selector by weight for each complete selection call.
/// </summary>
public sealed record ChooseOneSelector<TCandidate>
    : MultiSelector<TCandidate>
{
    /// <summary>
    /// Relative selection weight of each child selector, in child order. An empty collection selects every child
    /// uniformly.
    /// </summary>
    /// <remarks>
    /// Weights are retained exactly as configured rather than normalized, so omitting them stays distinguishable from
    /// passing equal weights.
    /// </remarks>
    public ValueArray<double> Weights { get; init; }

    public ChooseOneSelector(IReadOnlyList<ISelector<TCandidate>> childSelectors)
        : base(childSelectors)
    {
    }

    protected override CompositeExecutorFactory<ISelectionExecutor<TCandidate, TRunSearchSpace, TRunProblem>> CreateCompositeFactory<TRunSearchSpace, TRunProblem>()
    {
        if (ChildSelectors.Count == 0)
            throw new InvalidOperationException("At least one selector must be provided.");
        if (Weights.Count > 0 && Weights.Count != ChildSelectors.Count)
            throw new InvalidOperationException("Weights must have the same length as selectors.");

        return childSelectors => new Executor<TRunSearchSpace, TRunProblem>(childSelectors, new(childSelectors, Weights));
    }

    private sealed class Executor<TSearchSpace, TProblem>(ImmutableArray<ISelectionExecutor<TCandidate, TSearchSpace, TProblem>> childSelectors, WeightedItemSampler<ISelectionExecutor<TCandidate, TSearchSpace, TProblem>> sampler)
        : MultiSelectionExecutor<TCandidate, TSearchSpace, TProblem>(childSelectors)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        public override IReadOnlyList<EvaluatedCandidate<TCandidate>> Select(IReadOnlyList<EvaluatedCandidate<TCandidate>> population, ObjectiveDirections objective, int count, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem) =>
            sampler.Sample(random).Select(population, objective, count, random, searchSpace, problem);
    }
}

public static class ChooseOneSelector
{
    public static ChooseOneSelector<TCandidate> Create<TCandidate>(params IReadOnlyList<ISelector<TCandidate>> childSelectors) => new(childSelectors);

    public static ChooseOneSelector<TCandidate> Create<TCandidate>(IReadOnlyList<ISelector<TCandidate>> childSelectors, IReadOnlyList<double> weights) => new(childSelectors) { Weights = weights.ToValueArray() };
}
