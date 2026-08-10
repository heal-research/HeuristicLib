using Generator.Equals;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Replacers;

/// <summary>
/// Selects one child replacer by weight for each complete replacement call.
/// </summary>
[Equatable]
public partial record ChooseOneReplacer<TCandidate, TSearchSpace, TProblem>
    : MultiReplacer<TCandidate, TSearchSpace, TProblem>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    [IgnoreEquality]
    public ImmutableArray<IReplacer<TCandidate, TSearchSpace, TProblem>> Replacers => InnerReplacers;

    [OrderedEquality]
    public ImmutableArray<double> Weights { get; }

    [IgnoreEquality]
    private readonly WeightedBatchDispatch dispatcher;

    public ChooseOneReplacer(IReadOnlyList<IReplacer<TCandidate, TSearchSpace, TProblem>> replacers, IReadOnlyList<double>? weights = null)
        : base(replacers)
    {
        if (replacers.Count == 0)
            throw new ArgumentException("At least one replacer must be provided.", nameof(replacers));

        IReadOnlyList<double> effectiveWeights = weights ?? [.. Enumerable.Repeat(1.0 / replacers.Count, replacers.Count)];
        if (effectiveWeights.Count != replacers.Count)
            throw new ArgumentException("Weights must have the same length as replacers.", nameof(weights));

        dispatcher = new WeightedBatchDispatch(effectiveWeights);
        Weights = dispatcher.Weights;
    }

    protected override MultiReplacerInstance<TCandidate, TSearchSpace, TProblem> CreateReplacerInstance(ImmutableArray<IReplacerInstance<TCandidate, TSearchSpace, TProblem>> innerReplacers) =>
        new Instance(innerReplacers, dispatcher);

    private sealed class Instance(ImmutableArray<IReplacerInstance<TCandidate, TSearchSpace, TProblem>> innerReplacers, WeightedBatchDispatch dispatcher)
        : MultiReplacerInstance<TCandidate, TSearchSpace, TProblem>(innerReplacers)
    {
        public override IReadOnlyList<EvaluatedCandidate<TCandidate>> Replace(IReadOnlyList<EvaluatedCandidate<TCandidate>> previousPopulation, IReadOnlyList<EvaluatedCandidate<TCandidate>> offspringPopulation, ObjectiveDirections objective, int count, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem) =>
            InnerReplacers[dispatcher.ChooseOperator(random, InnerReplacers.Length)].Replace(previousPopulation, offspringPopulation, objective, count, random, searchSpace, problem);
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
        new(replacers, weights);
}
