using Generator.Equals;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Crossovers;

[Equatable]
public partial record ChooseOneCrossover<TCandidate, TSearchSpace, TProblem>
    : MultiCrossover<TCandidate, TSearchSpace, TProblem>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    [IgnoreEquality] public ImmutableArray<ICrossover<TCandidate, TSearchSpace, TProblem>> Crossovers => InnerCrossovers;

    [OrderedEquality]
    public ImmutableArray<double> Weights { get; }

    [IgnoreEquality]
    private readonly WeightedBatchDispatch dispatcher;

    public ChooseOneCrossover(IReadOnlyList<ICrossover<TCandidate, TSearchSpace, TProblem>> crossovers, IReadOnlyList<double>? weights = null)
        : base(crossovers)
    {
        if (crossovers.Count == 0)
            throw new ArgumentException("At least one crossover must be provided.", nameof(crossovers));

        IReadOnlyList<double> effectiveWeights = weights ?? [.. Enumerable.Repeat(1.0 / crossovers.Count, crossovers.Count)];
        if (effectiveWeights.Count != crossovers.Count)
            throw new ArgumentException("Weights must have the same length as crossovers.", nameof(weights));

        dispatcher = new WeightedBatchDispatch(effectiveWeights);
        Weights = dispatcher.Weights;
    }

    protected override MultiCrossoverInstance<TCandidate, TSearchSpace, TProblem> CreateCrossoverInstance(ImmutableArray<ICrossoverInstance<TCandidate, TSearchSpace, TProblem>> innerCrossovers) =>
        new Instance(innerCrossovers, dispatcher);

    private sealed class Instance(ImmutableArray<ICrossoverInstance<TCandidate, TSearchSpace, TProblem>> innerCrossovers, WeightedBatchDispatch dispatcher)
        : MultiCrossoverInstance<TCandidate, TSearchSpace, TProblem>(innerCrossovers)
    {
        public override IReadOnlyList<TCandidate> Cross(IReadOnlyList<Parents<TCandidate>> parents, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem) =>
            dispatcher.Dispatch(parents, InnerCrossovers, random, (crossover, batchParents) => crossover.Cross(batchParents, random, searchSpace, problem));
    }
}

public static class ChooseOneCrossover
{
    public static ChooseOneCrossover<TCandidate, TSearchSpace, TProblem> Create<TCandidate, TSearchSpace, TProblem>(params IReadOnlyList<ICrossover<TCandidate, TSearchSpace, TProblem>> crossovers)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        => new(crossovers);

    public static ChooseOneCrossover<TCandidate, TSearchSpace, TProblem> Create<TCandidate, TSearchSpace, TProblem>(IReadOnlyList<ICrossover<TCandidate, TSearchSpace, TProblem>> crossovers, IReadOnlyList<double> weights)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace> => new(crossovers, weights);

    extension<TCandidate, TSearchSpace, TProblem>(ICrossover<TCandidate, TSearchSpace, TProblem> crossover)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        public ChooseOneCrossover<TCandidate, TSearchSpace, TProblem> WithRate(double crossoverRate) =>
            Create([crossover, SelectFirstParentCrossover<TCandidate>.Instance], [crossoverRate, 1 - crossoverRate]);
    }
}
