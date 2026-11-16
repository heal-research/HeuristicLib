using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Execution;

namespace HEAL.HeuristicLib.Inspection;

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

    protected override IAlgorithmExecutor<TSearchState> CreateExecutor<TSearchSpace, TProblem>(
        ResolutionScope<TCandidate, TSearchSpace, TProblem, TSearchState> scope)
    {
        var counter = scope.Resolve(Counter);
        return TObservation.Wrap<TSearchSpace, TProblem>(scope.Resolve(ChildAlgorithm), ChildAlgorithm, observation =>
        {
            if (observation.Counts)
                counter.IncrementBy(1);
        }, scope.Problem);
    }
}

public static class CountedAlgorithmExtensions
{
    extension<TCandidate, TSearchState>(IAlgorithm<TCandidate, TSearchState> algorithm)
        where TSearchState : class, ISearchState
    {
        /// <summary>Counts a boundary of an algorithm wherever this configuration holds it, such as <c>ga.Counting(ga.Iterations, out var iterations)</c>.</summary>
        /// <param name="target">The boundary to count. Its algorithm is matched by reference, and it is an error when this configuration does not hold it.</param>
        /// <param name="counter">Receives the new counter, which <c>run.Result(counter).CurrentCount</c> reads and a terminator can check.</param>
        /// <returns>The interface, because the target may be this algorithm itself, which the wrapper then replaces.</returns>
        public IAlgorithm<TCandidate, TSearchState> Counting<TObservation>(AlgorithmSource<TCandidate, TSearchState, TObservation> target, out Counter counter)
            where TObservation : IAlgorithmObservation<TCandidate, TSearchState, TObservation> =>
            algorithm.Substitute(Substitution.Counting(target, out counter));

        /// <summary>Counts a boundary of an algorithm into an existing counter wherever this configuration holds it.</summary>
        /// <param name="target">The boundary to count. Its algorithm is matched by reference, and it is an error when this configuration does not hold it.</param>
        /// <param name="counter">The counter to add to, such as the one <c>LimitedToCount</c> hands to its placement.</param>
        /// <returns>The interface, because the target may be this algorithm itself, which the wrapper then replaces.</returns>
        public IAlgorithm<TCandidate, TSearchState> Counting<TObservation>(AlgorithmSource<TCandidate, TSearchState, TObservation> target, Counter counter)
            where TObservation : IAlgorithmObservation<TCandidate, TSearchState, TObservation> =>
            algorithm.Substitute(Substitution.Counting(target, counter));
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
            return Substitution.Counting(target, created);
        }

        /// <summary>The substitution behind <c>Counting</c> into an existing counter, for one call with others.</summary>
        /// <param name="target">The boundary to count. Its algorithm is matched by reference, and it is an error when the configuration does not hold it.</param>
        /// <param name="counter">The counter to add to.</param>
        public static Substitution Counting<TCandidate, TSearchState, TObservation>(AlgorithmSource<TCandidate, TSearchState, TObservation> target, Counter counter)
            where TSearchState : class, ISearchState
            where TObservation : IAlgorithmObservation<TCandidate, TSearchState, TObservation> =>
            Substitution.Of<IAlgorithm<TCandidate, TSearchState>>(target.Algorithm,
                node => new CountedAlgorithm<TCandidate, TSearchState, TObservation>(node, counter));
    }
}
