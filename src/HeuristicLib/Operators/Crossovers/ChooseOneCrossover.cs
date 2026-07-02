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

    public ChooseOneCrossover(ImmutableArray<ICrossover<TCandidate, TSearchSpace, TProblem>> crossovers, ImmutableArray<double>? weights = null)
  : base(crossovers)
    {
        if (crossovers.Length == 0)
        {
            throw new ArgumentException("At least one crossover must be provided.", nameof(crossovers));
        }

        var effectiveWeights = weights ?? [.. Enumerable.Repeat(1.0, crossovers.Length)];
        if (effectiveWeights.Length != crossovers.Length)
        {
            throw new ArgumentException("Weights must have the same length as crossovers.", nameof(weights));
        }

        dispatcher = new WeightedBatchDispatch(effectiveWeights);
        Weights = dispatcher.Weights;
    }

    protected override IReadOnlyList<TCandidate> Cross(
      IReadOnlyList<IParents<TCandidate>> parents,
      IReadOnlyList<InnerCross> innerCrossovers,
      IRandomNumberGenerator random,
      TSearchSpace searchSpace,
      TProblem problem)
    {
        return dispatcher.Dispatch(
          parents,
          innerCrossovers,
          random,
          (crossover, batchParents) => crossover(batchParents, random, searchSpace, problem));
    }
}

public static class ChooseOneCrossover
{
    public static ChooseOneCrossover<TCandidate, TSearchSpace, TProblem> Create<TCandidate, TSearchSpace, TProblem>(
    params IEnumerable<ICrossover<TCandidate, TSearchSpace, TProblem>> crossovers)
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        var r = crossovers.ToImmutableArray();
        var weights = r.Select(_ => 1.0 / r.Length).ToImmutableArray();
        return new ChooseOneCrossover<TCandidate, TSearchSpace, TProblem>(r, weights);
    }

    public static ChooseOneCrossover<TCandidate, TSearchSpace, TProblem> Create<TCandidate, TSearchSpace, TProblem>(
    ImmutableArray<ICrossover<TCandidate, TSearchSpace, TProblem>> crossovers,
    ImmutableArray<double>? weights = null)
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        return new ChooseOneCrossover<TCandidate, TSearchSpace, TProblem>(crossovers, weights);
    }

    extension<TCandidate, TSearchSpace, TProblem>(ICrossover<TCandidate, TSearchSpace, TProblem> crossover)
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        public ChooseOneCrossover<TCandidate, TSearchSpace, TProblem> WithRate(double crossoverRate)
        {
            return Create([crossover, SelectFirstParentCrossover<TCandidate>.Instance], [crossoverRate, 1 - crossoverRate]);
        }
    }
}
