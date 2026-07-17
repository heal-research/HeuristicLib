using Generator.Equals;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Creators;

/// <remarks>
/// Selection is performed independently for each candidate. Candidates assigned to the same creator are created as one batch and returned in their original assignment order.
/// </remarks>
[Equatable]
public partial record ChooseOneCreator<TCandidate, TSearchSpace, TProblem>
    : MultiCreator<TCandidate, TSearchSpace, TProblem>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    [IgnoreEquality]
    public ImmutableArray<ICreator<TCandidate, TSearchSpace, TProblem>> Creators => InnerCreators;

    [OrderedEquality]
    public ImmutableArray<double> Weights { get; }

    [IgnoreEquality]
    private readonly WeightedBatchDispatch dispatcher;

    public ChooseOneCreator(ImmutableArray<ICreator<TCandidate, TSearchSpace, TProblem>> creators, ImmutableArray<double>? weights = null)
        : base(creators)
    {
        if (creators.Length == 0)
        {
            throw new ArgumentException("At least one creator must be provided.", nameof(creators));
        }

        var effectiveWeights = weights ?? [.. Enumerable.Repeat(1.0 / creators.Length, creators.Length)];
        if (effectiveWeights.Length != creators.Length)
        {
            throw new ArgumentException("Weights must have the same length as creators.", nameof(weights));
        }

        dispatcher = new WeightedBatchDispatch(effectiveWeights);
        Weights = dispatcher.Weights;
    }

    protected override MultiCreatorInstance<TCandidate, TSearchSpace, TProblem> CreateCreatorInstance(ImmutableArray<ICreatorInstance<TCandidate, TSearchSpace, TProblem>> innerCreators) =>
        new Instance(innerCreators, dispatcher);

    private sealed class Instance(ImmutableArray<ICreatorInstance<TCandidate, TSearchSpace, TProblem>> innerCreators, WeightedBatchDispatch dispatcher)
        : MultiCreatorInstance<TCandidate, TSearchSpace, TProblem>(innerCreators)
    {
        public override IReadOnlyList<TCandidate> Create(int count, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem) =>
            dispatcher.Dispatch(Enumerable.Range(0, count).ToArray(), InnerCreators, random, (creator, positions) => creator.Create(positions.Count, random, searchSpace, problem));
    }
}

public static class ChooseOneCreator
{
    public static ChooseOneCreator<TCandidate, TSearchSpace, TProblem> Create<TCandidate, TSearchSpace, TProblem>(params IEnumerable<ICreator<TCandidate, TSearchSpace, TProblem>> creators)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        var creatorArray = creators.ToImmutableArray();
        return new ChooseOneCreator<TCandidate, TSearchSpace, TProblem>(creatorArray);
    }

    public static ChooseOneCreator<TCandidate, TSearchSpace, TProblem> Create<TCandidate, TSearchSpace, TProblem>(ImmutableArray<ICreator<TCandidate, TSearchSpace, TProblem>> creators, ImmutableArray<double>? weights = null)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace> => new(creators, weights);
}
