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
    /// Gets the configured weights, or an empty array when all child replacers are selected uniformly.
    /// </summary>
    public ValueArray<double> Weights { get; init; }

    public ChooseOneReplacer(IReadOnlyList<IReplacer<TCandidate, TSearchSpace, TProblem>> childReplacers)
        : base(childReplacers)
    {
        if (ChildReplacers.Count == 0)
            throw new ArgumentException("At least one replacer must be provided.", nameof(childReplacers));
    }

    protected override MultiReplacerInstance<TCandidate, TSearchSpace, TProblem> CreateExecutionInstance(ImmutableArray<IReplacerInstance<TCandidate, TSearchSpace, TProblem>> childReplacers)
    {
        if (Weights.Count > 0 && Weights.Count != ChildReplacers.Count)
            throw new InvalidOperationException("Weights must have the same length as replacers.");

        return new Instance(childReplacers, new WeightedBatchDispatch(Weights));
    }

    private sealed class Instance(ImmutableArray<IReplacerInstance<TCandidate, TSearchSpace, TProblem>> childReplacers, WeightedBatchDispatch dispatcher)
        : MultiReplacerInstance<TCandidate, TSearchSpace, TProblem>(childReplacers)
    {
        public override IReadOnlyList<EvaluatedCandidate<TCandidate>> Replace(IReadOnlyList<EvaluatedCandidate<TCandidate>> previousPopulation, IReadOnlyList<EvaluatedCandidate<TCandidate>> offspringPopulation, ObjectiveDirections objective, int count, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem) =>
            ChildReplacers[dispatcher.ChooseOperator(random, ChildReplacers.Length)].Replace(previousPopulation, offspringPopulation, objective, count, random, searchSpace, problem);
    }
}

public static class ChooseOneReplacer
{
    public static ChooseOneReplacer<TCandidate, TSearchSpace, TProblem> Create<TCandidate, TSearchSpace, TProblem>(params IReadOnlyList<IReplacer<TCandidate, TSearchSpace, TProblem>> replacers)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        => new(replacers);

    public static ChooseOneReplacer<TCandidate, TSearchSpace, TProblem> Create<TCandidate, TSearchSpace, TProblem>(IReadOnlyList<IReplacer<TCandidate, TSearchSpace, TProblem>> replacers, IReadOnlyList<double> weights)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace> =>
        new(replacers) { Weights = weights.ToValueArray() };
}
