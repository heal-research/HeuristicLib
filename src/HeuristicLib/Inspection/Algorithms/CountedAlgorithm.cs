using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Execution;

namespace HEAL.HeuristicLib.Inspection;

/// <summary>Counts what each call of its child algorithm does, such as the states it yields, into the run's result of a counter.</summary>
/// <remarks>
/// This is the explicit placement of an algorithm counter: it counts what the counter's advice counts, through the same
/// wrapper, but at the algorithm it wraps rather than at the counter's source.
/// </remarks>
public sealed record CountedAlgorithm<TCandidate, TSearchState> : WrappingAlgorithm<TCandidate, TSearchState>
    where TSearchState : class, ISearchState
{
    public CountedAlgorithm(IAlgorithm<TCandidate, TSearchState> childAlgorithm, AlgorithmCounter counter)
        : base(childAlgorithm)
    {
        Counter = counter;
    }

    public AlgorithmCounter Counter { get; init; }

    public static Parameter<CountedAlgorithm<TCandidate, TSearchState>, AlgorithmCounter> CounterParameter { get; } =
        new(nameof(Counter), static algorithm => algorithm.Counter, static (algorithm, value) => algorithm with { Counter = value });

    public new static IReadOnlyList<IParameter> Parameters { get; } = [CounterParameter];

    public override IReadOnlyList<IParameter> GetParameters() => [.. base.GetParameters(), .. Parameters];

    protected override IAlgorithmExecutor<TCandidate, TSearchSpace, TProblem, TSearchState> CreateExecutor<TSearchSpace, TProblem>(
        ResolutionScope<TCandidate, TSearchSpace, TProblem, TSearchState> scope)
    {
        var advice = Counter.CreateAdvice<TCandidate, TSearchState>(scope.Results.Result(Counter));
        return advice.Wrap(scope.Resolve(ChildAlgorithm), ChildAlgorithm);
    }
}

/// <summary>Counts one boundary of its child algorithm, such as the states it yields or the calls that start, into a counter.</summary>
/// <remarks><typeparamref name="TObservation"/> says which boundary is counted. A terminator reading the same counter sees these counts wherever the wrapper is placed.</remarks>
public sealed record CountedAlgorithm<TCandidate, TSearchState, TObservation> : WrappingAlgorithm<TCandidate, TSearchState>
    where TSearchState : class, ISearchState
    where TObservation : IAlgorithmObservation<TCandidate, TSearchState, TObservation>
{
    public CountedAlgorithm(IAlgorithm<TCandidate, TSearchState> childAlgorithm, IConfigurationNode<CountAccumulator> counter)
        : base(childAlgorithm)
    {
        Counter = counter;
    }

    public IConfigurationNode<CountAccumulator> Counter { get; init; }

    public static Parameter<CountedAlgorithm<TCandidate, TSearchState, TObservation>, IConfigurationNode<CountAccumulator>> CounterParameter { get; } =
        new(nameof(Counter), static counted => counted.Counter, static (counted, value) => counted with { Counter = value });

    public new static IReadOnlyList<IParameter> Parameters { get; } = [CounterParameter];

    public override IReadOnlyList<IParameter> GetParameters() => [.. base.GetParameters(), .. Parameters];

    protected override IAlgorithmExecutor<TCandidate, TSearchSpace, TProblem, TSearchState> CreateExecutor<TSearchSpace, TProblem>(
        ResolutionScope<TCandidate, TSearchSpace, TProblem, TSearchState> scope)
    {
        var counter = scope.Resolve(Counter);
        return TObservation.Wrap(scope.Resolve(ChildAlgorithm), ChildAlgorithm, observation =>
        {
            if (observation.Counts)
                counter.IncrementBy(1);
        });
    }
}

public static class CountedAlgorithmExtensions
{
    extension<TCandidate, TSearchState>(IAlgorithm<TCandidate, TSearchState> algorithm)
        where TSearchState : class, ISearchState
    {
        /// <summary>Counts what each call of this algorithm does into the run's result of the counter.</summary>
        public CountedAlgorithm<TCandidate, TSearchState> Counted(AlgorithmCounter counter) => new(algorithm, counter);

        /// <summary>Counts a boundary of an algorithm wherever this configuration holds it, such as <c>ga.Counting(ga.Iterations, out var iterations)</c>.</summary>
        /// <param name="target">The boundary to count. Its algorithm is matched by reference, and it is an error when this configuration does not hold it.</param>
        /// <param name="counter">Receives the new counter, which <c>run.Result(counter).CurrentCount</c> reads and a terminator can check.</param>
        /// <returns>The interface, because the target may be this algorithm itself, which the wrapper then replaces.</returns>
        public IAlgorithm<TCandidate, TSearchState> Counting<TObservation>(AlgorithmSource<TCandidate, TSearchState, TObservation> target, out Counter counter)
            where TObservation : IAlgorithmObservation<TCandidate, TSearchState, TObservation> =>
            algorithm.Substitute(Substitution.Counting(target, out counter));
    }

    extension(Substitution)
    {
        /// <summary>The substitution behind <c>Counting</c>, for one call with others: <c>ga.Substitute(Substitution.Counting(ga.Mutator, out var mutations), Substitution.Counting(ga.Iterations, out var iterations))</c>.</summary>
        /// <param name="target">The boundary to count. Its algorithm is matched by reference, and it is an error when the configuration does not hold it.</param>
        /// <param name="counter">Receives the new counter.</param>
        public static Substitution Counting<TCandidate, TSearchState, TObservation>(AlgorithmSource<TCandidate, TSearchState, TObservation> target, out Counter counter)
            where TSearchState : class, ISearchState
            where TObservation : IAlgorithmObservation<TCandidate, TSearchState, TObservation>
        {
            var created = new Counter();
            counter = created;
            return Substitution.Of<IAlgorithm<TCandidate, TSearchState>>(target.Algorithm,
                node => new CountedAlgorithm<TCandidate, TSearchState, TObservation>(node, created));
        }
    }
}
