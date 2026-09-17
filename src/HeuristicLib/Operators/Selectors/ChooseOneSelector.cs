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

    protected override ISelectorInstance<TCandidate, TRunSearchSpace, TRunProblem> CombineExecutionInstances<TRunSearchSpace, TRunProblem>(ImmutableArray<ISelectorInstance<TCandidate, TRunSearchSpace, TRunProblem>> childSelectors)
    {
        if (ChildSelectors.Count == 0)
            throw new InvalidOperationException("At least one selector must be provided.");
        if (Weights.Count > 0 && Weights.Count != ChildSelectors.Count)
            throw new InvalidOperationException("Weights must have the same length as selectors.");

        return new Instance<TRunSearchSpace, TRunProblem>(
            childSelectors,
            WeightedDispatcher.Create(childSelectors, Weights));
    }

    private sealed class Instance<TSearchSpace, TProblem>(ImmutableArray<ISelectorInstance<TCandidate, TSearchSpace, TProblem>> childSelectors, WeightedDispatcher<ISelectorInstance<TCandidate, TSearchSpace, TProblem>> dispatcher)
        : MultiSelectorInstance<TCandidate, TSearchSpace, TProblem>(childSelectors)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        public override IReadOnlyList<EvaluatedCandidate<TCandidate>> Select(IReadOnlyList<EvaluatedCandidate<TCandidate>> population, ObjectiveDirections objective, int count, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem) =>
            dispatcher.Dispatch(
                random,
                (population, objective, count, random, searchSpace, problem),
                static (selector, state) => selector.Select(state.population, state.objective, state.count, state.random, state.searchSpace, state.problem));
    }
}

public static class ChooseOneSelector
{
    public static ChooseOneSelector<TCandidate> Create<TCandidate>(params IReadOnlyList<ISelector<TCandidate>> childSelectors) => new(childSelectors);

    public static ChooseOneSelector<TCandidate> Create<TCandidate>(IReadOnlyList<ISelector<TCandidate>> childSelectors, IReadOnlyList<double> weights) => new(childSelectors) { Weights = weights.ToValueArray() };
}
