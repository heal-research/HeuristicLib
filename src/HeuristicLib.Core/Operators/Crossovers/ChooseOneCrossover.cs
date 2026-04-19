using Generator.Equals;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Crossovers;

[Equatable]
public partial record ChooseOneCrossover<TGenotype, TSearchSpace, TProblem>
  : MultiCrossover<TGenotype, TSearchSpace, TProblem>
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
{
  [IgnoreEquality] public ImmutableArray<ICrossover<TGenotype, TSearchSpace, TProblem>> Crossovers => InnerCrossovers;

  [OrderedEquality]
  public ImmutableArray<double> Weights { get; }

  [IgnoreEquality]
  private readonly WeightedBatchDispatch dispatcher;

  public ChooseOneCrossover(ImmutableArray<ICrossover<TGenotype, TSearchSpace, TProblem>> crossovers, ImmutableArray<double>? weights = null)
: base(crossovers)
  {
    if (crossovers.Length == 0) {
      throw new ArgumentException("At least one crossover must be provided.", nameof(crossovers));
    }

    var effectiveWeights = weights ?? [.. Enumerable.Repeat(1.0, crossovers.Length)];
    if (effectiveWeights.Length != crossovers.Length) {
      throw new ArgumentException("Weights must have the same length as crossovers.", nameof(weights));
    }

    dispatcher = new WeightedBatchDispatch(effectiveWeights);
    Weights = dispatcher.Weights;
  }

  protected override IReadOnlyList<TGenotype> Cross(
    IReadOnlyList<IParents<TGenotype>> parents,
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
  public static ChooseOneCrossover<TGenotype, TSearchSpace, TProblem> Create<TGenotype, TSearchSpace, TProblem>(
  params IEnumerable<ICrossover<TGenotype, TSearchSpace, TProblem>> crossovers)
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
  {
    var r = crossovers.ToImmutableArray();
    var weights = r.Select(_ => 1.0 / r.Length).ToImmutableArray();
    return new ChooseOneCrossover<TGenotype, TSearchSpace, TProblem>(r, weights);
  }

  public static ChooseOneCrossover<TGenotype, TSearchSpace, TProblem> Create<TGenotype, TSearchSpace, TProblem>(
  ImmutableArray<ICrossover<TGenotype, TSearchSpace, TProblem>> crossovers,
  ImmutableArray<double>? weights = null)
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
  {
    return new ChooseOneCrossover<TGenotype, TSearchSpace, TProblem>(crossovers, weights);
  }

  extension<TGenotype, TSearchSpace, TProblem>(ICrossover<TGenotype, TSearchSpace, TProblem> crossover)
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
  {
    public ChooseOneCrossover<TGenotype, TSearchSpace, TProblem> WithRate(double crossoverRate)
    {
      return Create([crossover, NoCrossover<TGenotype>.Instance], [crossoverRate, 1 - crossoverRate]);
    }
  }
}
