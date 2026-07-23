using System.Runtime.CompilerServices;
using HEAL.HeuristicLib.Analysis;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Algorithms.MetaAlgorithms;

public record OperatorBudgetAlgorithm<TCandidate, TSearchSpace, TProblem, TSearchState, TOperator, TObservedInstance>
    : Algorithm<TCandidate, TSearchSpace, TProblem, TSearchState>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
    where TSearchState : class, ISearchState
    where TOperator : IOperator<TObservedInstance>
    where TObservedInstance : class, IOperatorInstance
{
    public required IAlgorithm<TCandidate, TSearchSpace, TProblem, TSearchState> Algorithm { get; init; }
    public required TOperator ObservedOperator { get; init; }
    public required Func<TOperator, ObservationCounter, IOperator<TObservedInstance>> CountedOperatorFactory { get; init; }

    public int MaximumCount
    {
        get;
        init => field = value > 0
            ? value
            : throw new ArgumentOutOfRangeException(nameof(MaximumCount), "MaximumCount must be positive.");
    }

    protected override OperatorBudgetAlgorithmInstance<TCandidate, TSearchSpace, TProblem, TSearchState> CreateAlgorithmInstance(ExecutionInstanceRegistry registry)
    {
        var counter = new ObservationCounter();
        var countedOperator = CountedOperatorFactory(ObservedOperator, counter);
        var childRegistry = registry.CreateChildRegistry();
        childRegistry.RegisterReplacement(ObservedOperator, countedOperator);

        return new OperatorBudgetAlgorithmInstance<TCandidate, TSearchSpace, TProblem, TSearchState>(childRegistry.Resolve(Algorithm), counter, MaximumCount);
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
