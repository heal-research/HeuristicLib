using Generator.Equals;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Mutators;

[Equatable]
public partial record ChooseOneMutator<TCandidate, TSearchSpace, TProblem>
    : MultiMutator<TCandidate, TSearchSpace, TProblem>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    [IgnoreEquality] public ImmutableArray<IMutator<TCandidate, TSearchSpace, TProblem>> Mutators => InnerMutators;

    [OrderedEquality]
    public ImmutableArray<double> Weights { get; }

    [IgnoreEquality]
    private readonly WeightedBatchDispatch dispatcher;

    public ChooseOneMutator(ImmutableArray<IMutator<TCandidate, TSearchSpace, TProblem>> mutators, ImmutableArray<double>? weights = null)
        : base(mutators)
    {
        if (mutators.Length == 0)
        {
            throw new ArgumentException("At least one mutator must be provided.", nameof(mutators));
        }

        var effectiveWeights = weights ?? [.. Enumerable.Repeat(1.0 / mutators.Length, mutators.Length)];
        if (effectiveWeights.Length != mutators.Length)
        {
            throw new ArgumentException("Weights must have the same length as mutators.", nameof(weights));
        }

        dispatcher = new WeightedBatchDispatch(effectiveWeights);
        Weights = dispatcher.Weights;
    }

    protected override MultiMutatorInstance<TCandidate, TSearchSpace, TProblem> CreateMutatorInstance(ImmutableArray<IMutatorInstance<TCandidate, TSearchSpace, TProblem>> innerMutators)
        => new Instance(innerMutators, dispatcher);

    private sealed class Instance(ImmutableArray<IMutatorInstance<TCandidate, TSearchSpace, TProblem>> innerMutators, WeightedBatchDispatch dispatcher)
        : MultiMutatorInstance<TCandidate, TSearchSpace, TProblem>(innerMutators)
    {
        public override IReadOnlyList<TCandidate> Mutate(IReadOnlyList<TCandidate> parents, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem) =>
            dispatcher.Dispatch(parents, InnerMutators, random, (mutator, batchParents) => mutator.Mutate(batchParents, random, searchSpace, problem));
    }
}

public static class ChooseOneMutator
{
    public static ChooseOneMutator<TCandidate, TSearchSpace, TProblem> Create<TCandidate, TSearchSpace, TProblem>(params IEnumerable<IMutator<TCandidate, TSearchSpace, TProblem>> mutators)
      where TSearchSpace : class, ISearchSpace<TCandidate>
      where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        var mutatorArray = mutators.ToImmutableArray();
        return new(mutatorArray);
    }

    public static ChooseOneMutator<TCandidate, TSearchSpace, TProblem> Create<TCandidate, TSearchSpace, TProblem>(ImmutableArray<IMutator<TCandidate, TSearchSpace, TProblem>> mutators, ImmutableArray<double>? weights = null)
      where TSearchSpace : class, ISearchSpace<TCandidate>
      where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        return new(mutators, weights);
    }

    extension<TCandidate, TSearchSpace, TProblem>(IMutator<TCandidate, TSearchSpace, TProblem> mutator)
     where TSearchSpace : class, ISearchSpace<TCandidate>
     where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        public ChooseOneMutator<TCandidate, TSearchSpace, TProblem> WithRate(double mutationRate)
        {
            return Create([mutator, NoChangeMutator<TCandidate>.Instance], [mutationRate, 1 - mutationRate]);
        }
    }
}
