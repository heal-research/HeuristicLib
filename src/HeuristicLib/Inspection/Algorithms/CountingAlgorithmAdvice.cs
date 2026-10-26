using System.Runtime.CompilerServices;
using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Inspection;

/// <summary>Counts algorithm invocations that finish enumeration successfully.</summary>
public sealed class CountingAlgorithmAdvice<TCandidate, TSearchState> : AlgorithmAdvice<TCandidate, TSearchState>
    where TSearchState : class, ISearchState
{
    public CountAccumulator Counter { get; }

    public CountingAlgorithmAdvice(CountAccumulator counter)
    {
        Counter = counter;
    }

    public override IAlgorithmExecutor<TCandidate, TSearchSpace, TProblem, TSearchState> Wrap<TSearchSpace, TProblem>(
        IAlgorithmExecutor<TCandidate, TSearchSpace, TProblem, TSearchState> executor, IAlgorithm<TCandidate, TSearchState> source) =>
        new Executor<TSearchSpace, TProblem>(executor, Counter);

    private sealed class Executor<TSearchSpace, TProblem>(IAlgorithmExecutor<TCandidate, TSearchSpace, TProblem, TSearchState> childAlgorithm, CountAccumulator counter)
        : WrappingAlgorithmExecutor<TCandidate, TSearchSpace, TProblem, TSearchState>(childAlgorithm)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        public override async IAsyncEnumerable<TSearchState> RunStreamingAsync(TProblem problem, IRandomNumberGenerator random,
            TSearchState? initialState = null, [EnumeratorCancellation] CancellationToken ct = default)
        {
            await foreach (var state in ChildAlgorithm.RunStreamingAsync(problem, random, initialState, ct))
                yield return state;
            counter.IncrementBy(1);
        }
    }
}

public static class AlgorithmCounterExtensions
{
    extension(Aspect)
    {
        public static Aspect CountInvocations<TCandidate, TSearchState>(IAlgorithm<TCandidate, TSearchState> algorithm, CountAccumulator counter)
            where TSearchState : class, ISearchState =>
            Aspect.Create(Pointcut.Reference(algorithm), new CountingAlgorithmAdvice<TCandidate, TSearchState>(counter));
    }
}
