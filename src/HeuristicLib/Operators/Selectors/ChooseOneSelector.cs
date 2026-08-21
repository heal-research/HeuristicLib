using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Operators.Selectors;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators;

/// <summary>
/// Selects one child selector by weight for each complete selection call.
/// </summary>
public sealed record ChooseOneSelector<TCandidate, TSearchSpace, TProblem>
    : MultiSelector<TCandidate, TSearchSpace, TProblem>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
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

    public ChooseOneSelector(IReadOnlyList<ISelector<TCandidate, TSearchSpace, TProblem>> childSelectors)
        : base(childSelectors)
    {
    }

    protected override MultiSelectorInstance<TCandidate, TSearchSpace, TProblem> CreateExecutionInstance(ImmutableArray<ISelectorInstance<TCandidate, TSearchSpace, TProblem>> childSelectors)
    {
        if (ChildSelectors.Count == 0)
            throw new InvalidOperationException("At least one selector must be provided.");
        if (Weights.Count > 0 && Weights.Count != ChildSelectors.Count)
            throw new InvalidOperationException("Weights must have the same length as selectors.");

        return new Instance(
            childSelectors,
            WeightedDispatcher.Create(childSelectors, Weights));
    }

    private sealed class Instance(ImmutableArray<ISelectorInstance<TCandidate, TSearchSpace, TProblem>> childSelectors, WeightedDispatcher<ISelectorInstance<TCandidate, TSearchSpace, TProblem>> dispatcher)
        : MultiSelectorInstance<TCandidate, TSearchSpace, TProblem>(childSelectors)
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
    public static ChooseOneSelector<TCandidate, TSearchSpace, TProblem> Create<TCandidate, TSearchSpace, TProblem>(params IReadOnlyList<ISelector<TCandidate, TSearchSpace, TProblem>> childSelectors)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        => new(childSelectors);

    public static ChooseOneSelector<TCandidate, TSearchSpace, TProblem> Create<TCandidate, TSearchSpace, TProblem>(IReadOnlyList<ISelector<TCandidate, TSearchSpace, TProblem>> childSelectors, IReadOnlyList<double> weights)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace> => new(childSelectors) { Weights = weights.ToValueArray() };
}
