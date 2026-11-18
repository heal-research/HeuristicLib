using System.Runtime.CompilerServices;
using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Inspection;

/// <summary>What happened to one call of an algorithm's executor.</summary>
public enum AlgorithmCallEvent
{
    /// <summary>The call started: its caller asked for its first state.</summary>
    Started,

    /// <summary>The call yielded all its states and ended.</summary>
    Completed,

    /// <summary>The call ended with an exception other than cancellation.</summary>
    Failed,

    /// <summary>The call ended before completion: it was canceled, or its caller stopped asking for states.</summary>
    Stopped,
}

/// <summary>The start or the end of one call of an algorithm's executor, such as one turn of a cycle.</summary>
/// <remarks>
/// Every call that starts reports <see cref="AlgorithmCallEvent.Started"/> when its caller asks for its first state, and
/// one end event, also when it yields no state. A run paused by disposing its stream reports no end, because the run
/// keeps the call to resume it. <see cref="Iterations"/> counts the states the call has yielded so far and
/// <see cref="State"/> is the last of them, or null while it has yielded none. The search space and problem are read at
/// the candidate's interfaces, so an observation fits every run of the algorithm.
/// </remarks>
public sealed record AlgorithmCallObservation<TCandidate, TSearchState>(
    IAlgorithm<TCandidate, TSearchState> Algorithm,
    AlgorithmCallEvent Event,
    TSearchState? InitialState,
    TSearchState? State,
    long Iterations,
    ISearchSpace<TCandidate> SearchSpace,
    IProblem<TCandidate, ISearchSpace<TCandidate>> Problem)
    : IAlgorithmObservation<TCandidate, TSearchState, AlgorithmCallObservation<TCandidate, TSearchState>>
    where TSearchState : class, ISearchState
{
    // A call reports its start and its end, and counts once.
    bool IAlgorithmObservation<TCandidate, TSearchState, AlgorithmCallObservation<TCandidate, TSearchState>>.Counts => Event == AlgorithmCallEvent.Started;

    static IAlgorithmExecutor<TSearchState> IAlgorithmObservation<TCandidate, TSearchState, AlgorithmCallObservation<TCandidate, TSearchState>>.Wrap<TSearchSpace, TProblem>(
        IAlgorithmExecutor<TSearchState> executor, IAlgorithm<TCandidate, TSearchState> source, Action<AlgorithmCallObservation<TCandidate, TSearchState>> record, TProblem problem) =>
        new Executor<TSearchSpace, TProblem>(executor, source, record, problem);

    private sealed class Executor<TSearchSpace, TProblem>(
        IAlgorithmExecutor<TSearchState> childAlgorithm,
        IAlgorithm<TCandidate, TSearchState> observedAlgorithm,
        Action<AlgorithmCallObservation<TCandidate, TSearchState>> observe, TProblem problem)
        : WrappingAlgorithmExecutor<TCandidate, TSearchSpace, TProblem, TSearchState>(childAlgorithm, problem)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        public override async IAsyncEnumerable<TSearchState> RunStreamingAsync(IRandomNumberGenerator random, TSearchState? initialState = null, [EnumeratorCancellation] CancellationToken ct = default)
        {
            var iterations = 0L;
            TSearchState? state = null;
            var ended = false;
            void Report(AlgorithmCallEvent callEvent) =>
                observe(new AlgorithmCallObservation<TCandidate, TSearchState>(observedAlgorithm, callEvent, initialState, state, iterations,
                    Problem.SearchSpace, Problem));
            // Every call that starts reports exactly one end.
            void End(AlgorithmCallEvent callEvent)
            {
                ended = true;
                Report(callEvent);
            }

            Report(AlgorithmCallEvent.Started);
            IAsyncEnumerator<TSearchState>? iterator = null;
            try
            {
                while (true)
                {
                    bool moved;
                    try
                    {
                        iterator ??= ChildAlgorithm.RunStreamingAsync(random, initialState, ct).GetAsyncEnumerator(ct);
                        moved = await iterator.MoveNextAsync();
                    }
                    catch (OperationCanceledException)
                    {
                        End(AlgorithmCallEvent.Stopped);
                        throw;
                    }
                    catch
                    {
                        End(AlgorithmCallEvent.Failed);
                        throw;
                    }
                    if (!moved)
                    {
                        End(AlgorithmCallEvent.Completed);
                        yield break;
                    }
                    state = iterator.Current;
                    iterations++;
                    yield return state;
                }
            }
            finally
            {
                // The end is reported before the call is disposed, as for completion and failure.
                try
                {
                    // The caller stopped asking for states before the call ended.
                    if (!ended)
                        End(AlgorithmCallEvent.Stopped);
                }
                finally
                {
                    if (iterator is not null)
                        await iterator.DisposeAsync();
                }
            }
        }
    }
}

public static class AlgorithmCalls
{
    extension<TCandidate, TSearchState>(IAlgorithm<TCandidate, TSearchState> algorithm)
        where TSearchState : class, ISearchState
    {
        /// <summary>The start and end of each call of this algorithm's executor, such as each turn of a cycle.</summary>
        public AlgorithmSource<TCandidate, TSearchState, AlgorithmCallObservation<TCandidate, TSearchState>> Calls =>
            new(algorithm);

        /// <summary>Records the start and end of each call of this algorithm into the receiver, wherever this configuration is placed.</summary>
        public ObservedAlgorithm<TCandidate, TSearchState, AlgorithmCallObservation<TCandidate, TSearchState>> Observed(
            IObservationReceiver<AlgorithmCallObservation<TCandidate, TSearchState>> receiver) =>
            new(algorithm, receiver);
    }
}
