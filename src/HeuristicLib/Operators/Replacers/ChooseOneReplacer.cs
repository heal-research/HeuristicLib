using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Replacers;

/// <summary>
/// Selects one child replacer by weight for each complete replacement call.
/// </summary>
public sealed record ChooseOneReplacer<TCandidate, TSearchSpace, TProblem>
    : MultiReplacer<TCandidate, TSearchSpace, TProblem>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    /// <summary>
    /// Relative selection weight of each child replacer, in child order. An empty collection selects every child
    /// uniformly.
    /// </summary>
    /// <remarks>
    /// Weights are retained exactly as configured rather than normalized, so omitting them stays distinguishable from
    /// passing equal weights.
    /// </remarks>
    public ValueArray<double> Weights { get; init; }

    public ChooseOneReplacer(IReadOnlyList<IReplacer<TCandidate, TSearchSpace, TProblem>> childReplacers)
        : base(childReplacers)
    {
    }

    protected override MultiReplacerInstance<TCandidate, TSearchSpace, TProblem> CreateExecutionInstance(ImmutableArray<IReplacerInstance<TCandidate, TSearchSpace, TProblem>> childReplacers)
    {
        if (ChildReplacers.Count == 0)
            throw new InvalidOperationException("At least one replacer must be provided.");
        if (Weights.Count > 0 && Weights.Count != ChildReplacers.Count)
            throw new InvalidOperationException("Weights must have the same length as replacers.");

        return new Instance(
            childReplacers,
            WeightedDispatcher.Create(childReplacers, Weights));
    }

    private sealed class Instance(ImmutableArray<IReplacerInstance<TCandidate, TSearchSpace, TProblem>> childReplacers, WeightedDispatcher<IReplacerInstance<TCandidate, TSearchSpace, TProblem>> dispatcher)
        : MultiReplacerInstance<TCandidate, TSearchSpace, TProblem>(childReplacers)
    {
        public override IReadOnlyList<EvaluatedCandidate<TCandidate>> Replace(IReadOnlyList<EvaluatedCandidate<TCandidate>> previousPopulation, IReadOnlyList<EvaluatedCandidate<TCandidate>> offspringPopulation, ObjectiveDirections objective, int count, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem) =>
            dispatcher.Dispatch(
                random,
                (previousPopulation, offspringPopulation, objective, count, random, searchSpace, problem),
                static (replacer, state) => replacer.Replace(state.previousPopulation, state.offspringPopulation, state.objective, state.count, state.random, state.searchSpace, state.problem));
    }
}

public static class ChooseOneReplacer
{
    public static ChooseOneReplacer<TCandidate, TSearchSpace, TProblem> Create<TCandidate, TSearchSpace, TProblem>(params IReadOnlyList<IReplacer<TCandidate, TSearchSpace, TProblem>> childReplacers)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        => new(childReplacers);

    public static ChooseOneReplacer<TCandidate, TSearchSpace, TProblem> Create<TCandidate, TSearchSpace, TProblem>(IReadOnlyList<IReplacer<TCandidate, TSearchSpace, TProblem>> childReplacers, IReadOnlyList<double> weights)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace> =>
        new(childReplacers) { Weights = weights.ToValueArray() };
}
