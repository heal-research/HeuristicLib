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

    public ChooseOneReplacer(ImmutableArray<IReplacer<TCandidate, TSearchSpace, TProblem>> replacers, ImmutableArray<double>? weights = null)
        : base(replacers)
    {
        if (replacers.Length == 0)
        {
            throw new ArgumentException("At least one replacer must be provided.", nameof(replacers));
        }

        var effectiveWeights = weights ?? [.. Enumerable.Repeat(1.0 / replacers.Length, replacers.Length)];
        if (effectiveWeights.Length != replacers.Length)
        {
            throw new ArgumentException("Weights must have the same length as replacers.", nameof(weights));
        }

        dispatcher = new WeightedBatchDispatch(effectiveWeights);
        Weights = dispatcher.Weights;
    }

    protected override MultiReplacerInstance<TCandidate, TSearchSpace, TProblem> CreateReplacerInstance(ImmutableArray<IReplacerInstance<TCandidate, TSearchSpace, TProblem>> innerReplacers) =>
        new Instance(innerReplacers, dispatcher);

    private sealed class Instance(ImmutableArray<IReplacerInstance<TCandidate, TSearchSpace, TProblem>> innerReplacers, WeightedBatchDispatch dispatcher)
        : MultiReplacerInstance<TCandidate, TSearchSpace, TProblem>(innerReplacers)
    {
        public override IReadOnlyList<EvaluatedCandidate<TCandidate>> Replace(IReadOnlyList<EvaluatedCandidate<TCandidate>> previousPopulation, IReadOnlyList<EvaluatedCandidate<TCandidate>> offspringPopulation, ObjectiveDirections objective, int count, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem) =>
            InnerReplacers[dispatcher.ChooseOperator(random)].Replace(previousPopulation, offspringPopulation, objective, count, random, searchSpace, problem);
    }
}

public static class ChooseOneReplacer
{
    public static ChooseOneReplacer<TCandidate, TSearchSpace, TProblem> Create<TCandidate, TSearchSpace, TProblem>(params IEnumerable<IReplacer<TCandidate, TSearchSpace, TProblem>> replacers)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        var replacerArray = replacers.ToImmutableArray();
        return new ChooseOneReplacer<TCandidate, TSearchSpace, TProblem>(replacerArray);
    }

    public static ChooseOneReplacer<TCandidate, TSearchSpace, TProblem> Create<TCandidate, TSearchSpace, TProblem>(ImmutableArray<IReplacer<TCandidate, TSearchSpace, TProblem>> replacers, ImmutableArray<double>? weights = null)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace> =>
        new(replacers, weights);
}
