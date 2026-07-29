using Generator.Equals;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Selectors;

/// <summary>
/// Selects one child selector by weight for each complete selection call.
/// </summary>
[Equatable]
public partial record ChooseOneSelector<TCandidate, TSearchSpace, TProblem>
    : MultiSelector<TCandidate, TSearchSpace, TProblem>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    [IgnoreEquality]
    public ImmutableArray<ISelector<TCandidate, TSearchSpace, TProblem>> Selectors => InnerSelectors;

    [OrderedEquality]
    public ImmutableArray<double> Weights { get; }

    [IgnoreEquality]
    private readonly WeightedBatchDispatch dispatcher;

    public ChooseOneSelector(IReadOnlyList<ISelector<TCandidate, TSearchSpace, TProblem>> selectors, IReadOnlyList<double>? weights = null)
        : base(selectors)
    {
        if (selectors.Count == 0)
            throw new ArgumentException("At least one selector must be provided.", nameof(selectors));

        IReadOnlyList<double> effectiveWeights = weights ?? [.. Enumerable.Repeat(1.0 / selectors.Count, selectors.Count)];
        if (effectiveWeights.Count != selectors.Count)
            throw new ArgumentException("Weights must have the same length as selectors.", nameof(weights));

        dispatcher = new WeightedBatchDispatch(effectiveWeights);
        Weights = dispatcher.Weights;
    }

    protected override MultiSelectorInstance<TCandidate, TSearchSpace, TProblem> CreateSelectorInstance(ImmutableArray<ISelectorInstance<TCandidate, TSearchSpace, TProblem>> innerSelectors) =>
        new Instance(innerSelectors, dispatcher);

    private sealed class Instance(ImmutableArray<ISelectorInstance<TCandidate, TSearchSpace, TProblem>> innerSelectors, WeightedBatchDispatch dispatcher)
        : MultiSelectorInstance<TCandidate, TSearchSpace, TProblem>(innerSelectors)
    {
        public override IReadOnlyList<EvaluatedCandidate<TCandidate>> Select(IReadOnlyList<EvaluatedCandidate<TCandidate>> population, ObjectiveDirections objective, int count, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem) =>
            InnerSelectors[dispatcher.ChooseOperator(random)].Select(population, objective, count, random, searchSpace, problem);
    }
}

public static class ChooseOneSelector
{
    public static ChooseOneSelector<TCandidate, TSearchSpace, TProblem> Create<TCandidate, TSearchSpace, TProblem>(params IReadOnlyList<ISelector<TCandidate, TSearchSpace, TProblem>> selectors)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        => new(selectors);

    public static ChooseOneSelector<TCandidate, TSearchSpace, TProblem> Create<TCandidate, TSearchSpace, TProblem>(IReadOnlyList<ISelector<TCandidate, TSearchSpace, TProblem>> selectors, IReadOnlyList<double>? weights = null)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace> => new(selectors, weights);
}
