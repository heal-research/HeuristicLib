using System.Runtime.CompilerServices;
using HEAL.HeuristicLib.Analysis;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Algorithms;

public record OperatorBudgetAlgorithm<TCandidate, TSearchSpace, TProblem, TSearchState, TOperator>
    : Algorithm<OperatorBudgetAlgorithm<TCandidate, TSearchSpace, TProblem, TSearchState, TOperator>, TCandidate, TSearchSpace, TProblem, TSearchState>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
    where TSearchState : class, ISearchState
    where TOperator : class, IOperator
{
    public required IAlgorithm<TCandidate, TSearchSpace, TProblem, TSearchState> Algorithm { get; init; }
    public required TOperator ObservedOperator { get; init; }
    public required Func<TOperator, ObservationCounter, TOperator> CountedOperatorFactory { get; init; }

    /// <summary>
    /// Gets the counted-operator budget. The expected value is positive.
    /// </summary>
    /// <remarks>The budget is checked after each produced state, so a nonpositive budget stops after the first state.</remarks>
    public int MaximumCount { get; init; }

    public override OperatorBudgetAlgorithmInstance<TCandidate, TSearchSpace, TProblem, TSearchState> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry)
    {
        var counter = new ObservationCounter();
        var countedOperator = CountedOperatorFactory(ObservedOperator, counter);
        var childRegistry = instanceRegistry.CreateChildRegistry();
        childRegistry.RegisterReplacement(ObservedOperator, countedOperator);

        return new(childRegistry.Resolve(Algorithm), counter, MaximumCount);
    }
}

public sealed class OperatorBudgetAlgorithmInstance<TCandidate, TSearchSpace, TProblem, TSearchState>
    : AlgorithmInstance<TCandidate, TSearchSpace, TProblem, TSearchState>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
    where TSearchState : class, ISearchState
{
    private readonly IAlgorithmInstance<TCandidate, TSearchSpace, TProblem, TSearchState> algorithm;
    private readonly ObservationCounter counter;
    private readonly int maximumCount;

    public OperatorBudgetAlgorithmInstance(IAlgorithmInstance<TCandidate, TSearchSpace, TProblem, TSearchState> algorithm, ObservationCounter counter, int maximumCount)
    {
        this.algorithm = algorithm;
        this.counter = counter;
        this.maximumCount = maximumCount;
    }

    public override async IAsyncEnumerable<TSearchState> RunStreamingAsync(TProblem problem, IRandomNumberGenerator random, TSearchState? initialState = null, [EnumeratorCancellation] CancellationToken ct = default)
    {
        await foreach (var state in algorithm.RunStreamingAsync(problem, random, initialState, ct))
        {
            yield return state;

            if (counter.CurrentCount >= maximumCount)
            {
                yield break;
            }
        }
    }
}
