using Generator.Equals;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Mutators;

[Equatable]
public partial record ChooseOneMutator<TGenotype, TSearchSpace, TProblem>
  : CompositeMutator<TGenotype, TSearchSpace, TProblem>
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
{
  [IgnoreEquality]
  public ImmutableArray<IMutator<TGenotype, TSearchSpace, TProblem>> Mutators => InnerMutators;

  [OrderedEquality]
  public ImmutableArray<double> Weights { get; }

  [IgnoreEquality]
  private readonly WeightedBatchDispatch dispatcher;

  public ChooseOneMutator(ImmutableArray<IMutator<TGenotype, TSearchSpace, TProblem>> mutators, ImmutableArray<double>? weights = null)
    : base(mutators)
  {
    if (mutators.Length == 0) {
      throw new ArgumentException("At least one mutator must be provided.", nameof(mutators));
    }

    var effectiveWeights = weights ?? [.. Enumerable.Repeat(1.0, mutators.Length)];
    if (effectiveWeights.Length != mutators.Length) {
      throw new ArgumentException("Weights must have the same length as mutators.", nameof(weights));
    }

    dispatcher = new WeightedBatchDispatch(effectiveWeights);
    Weights = dispatcher.Weights;
  }

  protected override IReadOnlyList<TGenotype> Mutate(IReadOnlyList<TGenotype> parents,
                                                     IReadOnlyList<IMutatorInstance<TGenotype, TSearchSpace, TProblem>> innerMutators,
                                                     IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem)
  {
    return dispatcher.Dispatch(parents, innerMutators, random, (mutator, batchParents) => mutator.Mutate(batchParents, random, searchSpace, problem));
  }
}

public static class ChooseOneMutator
{
  public static ChooseOneMutator<TGenotype, TSearchSpace, TProblem> Create<TGenotype, TSearchSpace, TProblem>(params IEnumerable<IMutator<TGenotype, TSearchSpace, TProblem>> mutators)
    where TSearchSpace : class, ISearchSpace<TGenotype>
    where TProblem : class, IProblem<TGenotype, TSearchSpace>
  {
    var r = mutators.ToImmutableArray();
    var weights = r.Select(_ => 1.0 / r.Length).ToImmutableArray();
    return new ChooseOneMutator<TGenotype, TSearchSpace, TProblem>(r, weights);
  }

  public static ChooseOneMutator<TGenotype, TSearchSpace, TProblem> Create<TGenotype, TSearchSpace, TProblem>(ImmutableArray<IMutator<TGenotype, TSearchSpace, TProblem>> mutators, ImmutableArray<double>? weights = null)
    where TSearchSpace : class, ISearchSpace<TGenotype>
    where TProblem : class, IProblem<TGenotype, TSearchSpace>
  {
    return new ChooseOneMutator<TGenotype, TSearchSpace, TProblem>(mutators, weights);
  }

  extension<TGenotype, TSearchSpace, TProblem>(IMutator<TGenotype, TSearchSpace, TProblem> mutator)
    where TSearchSpace : class, ISearchSpace<TGenotype>
    where TProblem : class, IProblem<TGenotype, TSearchSpace>
  {
    public IMutator<TGenotype, TSearchSpace, TProblem> WithRate(double mutationRate)
    {
      return mutationRate switch {
        < 0 => NoChangeMutator<TGenotype>.Instance,
        > 1 => mutator,
        _ => Create([mutator, NoChangeMutator<TGenotype>.Instance], [mutationRate, 1 - mutationRate])
      };
    }
  }
}
