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
public sealed partial record ChooseOneSelector<TCandidate, TSearchSpace, TProblem>
    : MultiSelector<TCandidate, TSearchSpace, TProblem>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    /// <summary>
    /// Gets the configured weights, or an empty array when all child selectors are selected uniformly.
    /// </summary>
    [OrderedEquality]
    public ImmutableArray<double> Weights { get; }

    public ChooseOneSelector(IReadOnlyList<ISelector<TCandidate, TSearchSpace, TProblem>> childSelectors, IReadOnlyList<double>? weights = null)
        : base(childSelectors)
    {
        if (ChildSelectors.Length == 0)
            throw new ArgumentException("At least one selector must be provided.", nameof(childSelectors));

        var immutableWeights = weights?.ToImmutableArray() ?? [];
        Weights = immutableWeights.IsDefault ? [] : immutableWeights;

        if (Weights.Length > 0 && Weights.Length != ChildSelectors.Length)
            throw new ArgumentException("Weights must have the same length as selectors.", nameof(weights));
    }

    protected override MultiSelectorInstance<TCandidate, TSearchSpace, TProblem> CreateExecutionInstance(ImmutableArray<ISelectorInstance<TCandidate, TSearchSpace, TProblem>> childSelectors) =>
        new Instance(childSelectors, new WeightedBatchDispatch(Weights));

    private sealed class Instance(ImmutableArray<ISelectorInstance<TCandidate, TSearchSpace, TProblem>> childSelectors, WeightedBatchDispatch dispatcher)
        : MultiSelectorInstance<TCandidate, TSearchSpace, TProblem>(childSelectors)
    {
        public override IReadOnlyList<EvaluatedCandidate<TCandidate>> Select(IReadOnlyList<EvaluatedCandidate<TCandidate>> population, ObjectiveDirections objective, int count, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem) =>
            ChildSelectors[dispatcher.ChooseOperator(random, ChildSelectors.Length)].Select(population, objective, count, random, searchSpace, problem);
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
        where TProblem : class, IProblem<TCandidate, TSearchSpace> => new(childSelectors, weights);
}
