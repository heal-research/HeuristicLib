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
/// interceptor transformed it. Sub-iterations an algorithm does not yield are not observed. The search space and problem
/// are read at the candidate's interfaces, so an observation fits every run of the algorithm.
/// </remarks>
public sealed record AlgorithmObservation<TCandidate, TSearchState>(
    IAlgorithm<TCandidate, TSearchState> Algorithm,
    long Iteration,
    TSearchState State,
    TSearchState? PreviousState,
    ISearchSpace<TCandidate> SearchSpace,
    IProblem<TCandidate, ISearchSpace<TCandidate>> Problem)
    where TSearchState : class, ISearchState;

// The iteration count lives on the advice, so it continues across every execution the observed algorithm builds,
// including fresh child scopes of a cycle and runs over other search space and problem types.
file sealed class AlgorithmObservationAdvice<TCandidate, TSearchState>(
    Action<AlgorithmObservation<TCandidate, TSearchState>> observe) : AlgorithmAdvice<TCandidate, TSearchState>
    where TSearchState : class, ISearchState
{
    private long iteration;

    public override IAlgorithmExecutor<TCandidate, TSearchSpace, TProblem, TSearchState> Wrap<TSearchSpace, TProblem>(
        IAlgorithmExecutor<TCandidate, TSearchSpace, TProblem, TSearchState> executor, IAlgorithm<TCandidate, TSearchState> source) =>
        new Executor<TSearchSpace, TProblem>(executor, source, this);

    private void Observe(IAlgorithm<TCandidate, TSearchState> algorithm, TSearchState state, TSearchState? previousState,
        ISearchSpace<TCandidate> searchSpace, IProblem<TCandidate, ISearchSpace<TCandidate>> problem) =>
        observe(new AlgorithmObservation<TCandidate, TSearchState>(algorithm, Interlocked.Increment(ref iteration), state, previousState, searchSpace, problem));

    private sealed class Executor<TSearchSpace, TProblem>(
        IAlgorithmExecutor<TCandidate, TSearchSpace, TProblem, TSearchState> childAlgorithm,
        IAlgorithm<TCandidate, TSearchState> observedAlgorithm,
        AlgorithmObservationAdvice<TCandidate, TSearchState> advice)
        : WrappingAlgorithmExecutor<TCandidate, TSearchSpace, TProblem, TSearchState>(childAlgorithm)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        public override async IAsyncEnumerable<TSearchState> RunStreamingAsync(TProblem problem, IRandomNumberGenerator random,
            TSearchState? initialState = null, [EnumeratorCancellation] CancellationToken ct = default)
        {
            var previousState = initialState;
            await foreach (var searchState in ChildAlgorithm.RunStreamingAsync(problem, random, initialState, ct))
            {
                advice.Observe(observedAlgorithm, searchState, previousState, problem.SearchSpace, problem);
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
        public ObservationSource<AlgorithmObservation<TCandidate, TSearchState>> Iterations =>
            new(algorithm, observe => Aspect.Create(Pointcut.Reference(algorithm), new AlgorithmObservationAdvice<TCandidate, TSearchState>(observe)));
    }
}
