using System.Runtime.CompilerServices;
using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Analysis;

/// <summary>
/// Delivers an observation for every search state an algorithm yields.
/// </summary>
/// <remarks>
/// The wrapper sits outside the algorithm instance, so it observes exactly what the run streams: the state at the end of
/// an iteration, after any interceptor has transformed it. Sub-iterations an algorithm does not yield are not observed.
/// </remarks>
internal sealed record ObservingAlgorithm<TCandidate, TSearchSpace, TProblem, TSearchState>
    : IAlgorithm<TCandidate, TSearchSpace, TProblem, TSearchState>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
    where TSearchState : class, ISearchState
{
    private readonly IAlgorithm<TCandidate, TSearchSpace, TProblem, TSearchState> anchor;
    private readonly ValueArray<IObservationRecorder<AlgorithmObservation<TCandidate, TSearchSpace, TProblem, TSearchState>>> recorders;

    public ObservingAlgorithm(
        IAlgorithm<TCandidate, TSearchSpace, TProblem, TSearchState> anchor,
        IAlgorithm<TCandidate, TSearchSpace, TProblem, TSearchState> childAlgorithm,
        IReadOnlyList<IObservationRecorder<AlgorithmObservation<TCandidate, TSearchSpace, TProblem, TSearchState>>> recorders)
    {
        this.anchor = anchor;
        ChildAlgorithm = childAlgorithm;
        this.recorders = recorders.ToValueArray();
    }

    public IAlgorithm<TCandidate, TSearchSpace, TProblem, TSearchState> ChildAlgorithm { get; init; }

    public IAlgorithmInstance<TCandidate, TSearchSpace, TProblem, TSearchState> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) =>
        new Instance(anchor, instanceRegistry.Resolve(ChildAlgorithm), recorders);

    private sealed class Instance(
        IAlgorithm<TCandidate, TSearchSpace, TProblem, TSearchState> anchor,
        IAlgorithmInstance<TCandidate, TSearchSpace, TProblem, TSearchState> childAlgorithm,
        ValueArray<IObservationRecorder<AlgorithmObservation<TCandidate, TSearchSpace, TProblem, TSearchState>>> recorders)
        : IAlgorithmInstance<TCandidate, TSearchSpace, TProblem, TSearchState>
    {
        public async IAsyncEnumerable<TSearchState> RunStreamingAsync(TProblem problem, IRandomNumberGenerator random, TSearchState? initialState = null, [EnumeratorCancellation] CancellationToken ct = default)
        {
            var previousState = initialState;
            long iteration = 0;
            await foreach (var state in childAlgorithm.RunStreamingAsync(problem, random, initialState, ct))
            {
                var observation = new AlgorithmObservation<TCandidate, TSearchSpace, TProblem, TSearchState>(
                    anchor,
                    ++iteration,
                    state,
                    previousState,
                    problem.SearchSpace,
                    problem);
                foreach (var recorder in recorders)
                    recorder.Record(observation);

                previousState = state;
                yield return state;
            }
        }
    }
}
