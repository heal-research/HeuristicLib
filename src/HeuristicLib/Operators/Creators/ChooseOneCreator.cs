using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Creators;

/// <remarks>
/// Selection is performed independently for each candidate. Candidates assigned to the same creator are created as one batch and returned in their original assignment order.
/// </remarks>
public record ChooseOneCreator<TCandidate, TSearchSpace, TProblem>
    : MultiCreator<TCandidate, TSearchSpace, TProblem>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    public ValueArray<ICreator<TCandidate, TSearchSpace, TProblem>> Creators => InnerCreators;

    public ValueArray<double> Weights { get; }

    public ChooseOneCreator(IReadOnlyList<ICreator<TCandidate, TSearchSpace, TProblem>> creators, IReadOnlyList<double>? weights = null)
        : base(creators)
    {
        if (creators.Count == 0)
            throw new ArgumentException("At least one creator must be provided.", nameof(creators));

        IReadOnlyList<double> effectiveWeights = weights ?? [.. Enumerable.Repeat(1.0 / creators.Count, creators.Count)];
        if (effectiveWeights.Count != creators.Count)
            throw new ArgumentException("Weights must have the same length as creators.", nameof(weights));

        Weights = effectiveWeights.ToValueArray();
    }

    protected override MultiCreatorInstance<TCandidate, TSearchSpace, TProblem> CreateCreatorInstance(ImmutableArray<ICreatorInstance<TCandidate, TSearchSpace, TProblem>> innerCreators) =>
        new Instance(innerCreators, new WeightedBatchDispatch(Weights));

    private sealed class Instance(ImmutableArray<ICreatorInstance<TCandidate, TSearchSpace, TProblem>> innerCreators, WeightedBatchDispatch dispatcher)
        : MultiCreatorInstance<TCandidate, TSearchSpace, TProblem>(innerCreators)
    {
        public override IReadOnlyList<TCandidate> Create(int count, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem) =>
            dispatcher.Dispatch(Enumerable.Range(0, count).ToArray(), InnerCreators, random, (creator, positions) => creator.Create(positions.Count, random, searchSpace, problem));
    }
}

public static class ChooseOneCreator
{
    public static ChooseOneCreator<TCandidate, TSearchSpace, TProblem> Create<TCandidate, TSearchSpace, TProblem>(params IReadOnlyList<ICreator<TCandidate, TSearchSpace, TProblem>> creators)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        => new(creators);

    public static ChooseOneCreator<TCandidate, TSearchSpace, TProblem> Create<TCandidate, TSearchSpace, TProblem>(IReadOnlyList<ICreator<TCandidate, TSearchSpace, TProblem>> creators, IReadOnlyList<double> weights)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace> => new(creators, weights);
}
