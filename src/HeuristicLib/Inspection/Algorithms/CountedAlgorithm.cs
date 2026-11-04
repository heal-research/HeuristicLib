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

    protected override IAlgorithmExecutor<TCandidate, TSearchSpace, TProblem, TSearchState> CreateExecutor<TSearchSpace, TProblem>(
        ResolutionScope<TCandidate, TSearchSpace, TProblem, TSearchState> scope)
    {
        var advice = Counter.CreateAdvice<TCandidate, TSearchState>(scope.Results.Result(Counter));
        return advice.Wrap(scope.Resolve(ChildAlgorithm), ChildAlgorithm);
    }
}

public static class CountedAlgorithmExtensions
{
    extension<TCandidate, TSearchState>(IAlgorithm<TCandidate, TSearchState> algorithm)
        where TSearchState : class, ISearchState
    {
        /// <summary>Counts what each call of this algorithm does into the run's result of the counter.</summary>
        public CountedAlgorithm<TCandidate, TSearchState> Counted(AlgorithmCounter counter) => new(algorithm, counter);
    }
}
