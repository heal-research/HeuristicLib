using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Operators.Replacers;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators;

/// <summary>
/// Selects one child replacer by weight for each complete replacement call.
/// </summary>
public sealed record ChooseOneReplacer<TCandidate>
    : MultiReplacer<TCandidate>
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

    public ChooseOneReplacer(IReadOnlyList<IReplacer<TCandidate>> childReplacers)
        : base(childReplacers)
    {
    }

    protected override IReplacerInstance<TCandidate, TRunSearchSpace, TRunProblem> CombineExecutionInstances<TRunSearchSpace, TRunProblem>(ImmutableArray<IReplacerInstance<TCandidate, TRunSearchSpace, TRunProblem>> childReplacers)
    {
        if (ChildReplacers.Count == 0)
            throw new InvalidOperationException("At least one replacer must be provided.");
        if (Weights.Count > 0 && Weights.Count != ChildReplacers.Count)
            throw new InvalidOperationException("Weights must have the same length as replacers.");

        return new Instance<TRunSearchSpace, TRunProblem>(
            childReplacers,
            WeightedDispatcher.Create(childReplacers, Weights));
    }

    private sealed class Instance<TSearchSpace, TProblem>(ImmutableArray<IReplacerInstance<TCandidate, TSearchSpace, TProblem>> childReplacers, WeightedDispatcher<IReplacerInstance<TCandidate, TSearchSpace, TProblem>> dispatcher)
        : MultiReplacerInstance<TCandidate, TSearchSpace, TProblem>(childReplacers)
          where TSearchSpace : class, ISearchSpace<TCandidate>
          where TProblem : class, IProblem<TCandidate, TSearchSpace>
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
    public static ChooseOneReplacer<TCandidate> Create<TCandidate>(params IReadOnlyList<IReplacer<TCandidate>> childReplacers) => new(childReplacers);

    public static ChooseOneReplacer<TCandidate> Create<TCandidate>(IReadOnlyList<IReplacer<TCandidate>> childReplacers, IReadOnlyList<double> weights) =>
        new(childReplacers) { Weights = weights.ToValueArray() };
}
