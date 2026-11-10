using System.Runtime.CompilerServices;
using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Inspection;

/// <summary>
/// The search state an algorithm yielded at the end of one iteration, with the state it followed.
/// </summary>
/// <remarks>
/// The observation is taken outside the algorithm execution, so it reports what the run streams: the state after any
/// interceptor transformed it. Sub-iterations an algorithm does not yield are not observed. <see cref="Iteration"/>
/// counts the states this call of the algorithm has yielded, from 1, as the algorithm sees them: it restarts at each
/// call, such as each turn of a cycle, whatever the cycle's scope policy. A counter of the iterations, from
/// <c>algorithm.Iterations.Count()</c>, keeps counting across calls. The search space and problem are read at the
/// candidate's interfaces, so an observation fits every run of the algorithm.
/// </remarks>
public sealed record AlgorithmObservation<TCandidate, TSearchState>(
    IAlgorithm<TCandidate, TSearchState> Algorithm,
    long Iteration,
    TSearchState State,
    TSearchState? PreviousState,
    ISearchSpace<TCandidate> SearchSpace,
    IProblem<TCandidate, ISearchSpace<TCandidate>> Problem)
    : IAlgorithmObservation<TCandidate, TSearchState, AlgorithmObservation<TCandidate, TSearchState>>
    where TSearchState : class, ISearchState
{
    static IAlgorithmExecutor<TCandidate, TSearchSpace, TProblem, TSearchState> IAlgorithmObservation<TCandidate, TSearchState, AlgorithmObservation<TCandidate, TSearchState>>.Wrap<TSearchSpace, TProblem>(
        IAlgorithmExecutor<TCandidate, TSearchSpace, TProblem, TSearchState> executor, IAlgorithm<TCandidate, TSearchState> source, Action<AlgorithmObservation<TCandidate, TSearchState>> record) =>
        new Executor<TSearchSpace, TProblem>(executor, source, record);

    private sealed class Executor<TSearchSpace, TProblem>(
        IAlgorithmExecutor<TCandidate, TSearchSpace, TProblem, TSearchState> childAlgorithm,
        IAlgorithm<TCandidate, TSearchState> observedAlgorithm,
        Action<AlgorithmObservation<TCandidate, TSearchState>> observe)
        : WrappingAlgorithmExecutor<TCandidate, TSearchSpace, TProblem, TSearchState>(childAlgorithm)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        // The count belongs to the call, as the algorithm's own iteration count does.
        public override async IAsyncEnumerable<TSearchState> RunStreamingAsync(TProblem problem, IRandomNumberGenerator random,
            TSearchState? initialState = null, [EnumeratorCancellation] CancellationToken ct = default)
        {
            var iteration = 0L;
            var previousState = initialState;
            await foreach (var searchState in ChildAlgorithm.RunStreamingAsync(problem, random, initialState, ct))
            {
                observe(new AlgorithmObservation<TCandidate, TSearchState>(observedAlgorithm, ++iteration, searchState, previousState, problem.SearchSpace, problem));
                previousState = searchState;
                yield return searchState;
            }
        }
    }
}

public static class AlgorithmObservations
{
    extension<TCandidate, TSearchState>(IAlgorithm<TCandidate, TSearchState> algorithm)
        where TSearchState : class, ISearchState
    {
        /// <summary>The observations of the search states this algorithm yields.</summary>
        public AlgorithmSource<TCandidate, TSearchState, AlgorithmObservation<TCandidate, TSearchState>> Iterations =>
            new(algorithm);

        /// <summary>Records the search states this algorithm yields into the instrument, wherever this configuration is placed.</summary>
        public ObservedAlgorithm<TCandidate, TSearchState, AlgorithmObservation<TCandidate, TSearchState>> Observed(
            IObservationInstrument<AlgorithmObservation<TCandidate, TSearchState>> instrument) =>
            new(algorithm, instrument);
    }

    extension<TCandidate, TSearchState>(AlgorithmSource<TCandidate, TSearchState, AlgorithmObservation<TCandidate, TSearchState>> iterations)
        where TSearchState : class, ISearchState
    {
        /// <summary>Creates a counter of the states the algorithm yields, which keeps counting across its calls.</summary>
        public Counter Count() =>
            new(counter => Substitution.Of(iterations.Algorithm,
                node => new CountedAlgorithm<TCandidate, TSearchState, AlgorithmObservation<TCandidate, TSearchState>>(node, counter)));

        /// <summary>Creates a duration adding up the time the algorithm spends advancing to each state, including failures.</summary>
        /// <param name="timeProvider">Measures each advance. When omitted, the system clock does.</param>
        public Duration Duration(TimeProvider? timeProvider = null) =>
            new(timeProvider, duration => Substitution.Of(iterations.Algorithm, node => node.Timed(duration)));
    }
}
