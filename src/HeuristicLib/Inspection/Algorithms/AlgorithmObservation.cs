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
    IProblem<TCandidate, ISearchSpace<TCandidate>> Problem) : Observation<IProblem<TCandidate, ISearchSpace<TCandidate>>>(Problem), IStateObservation<TSearchState>
    where TSearchState : class, ISearchState;

/// <summary>An observation of a search state, read the same way whether an algorithm yielded it or an interceptor returned it.</summary>
/// <remarks>Measurements of a search state, such as its population's objective vectors, read observations through this.</remarks>
public interface IStateObservation<out TSearchState>
    where TSearchState : class, ISearchState
{
    TSearchState State { get; }
}

public static class AlgorithmObservations
{
    extension(Aspect)
    {
        /// <summary>Observes every search state yielded by one chosen algorithm.</summary>
        public static Aspect Observe<TCandidate, TSearchState>(
            IAlgorithm<TCandidate, TSearchState> algorithm,
            Action<AlgorithmObservation<TCandidate, TSearchState>> observe)
            where TSearchState : class, ISearchState =>
            Aspect.Create(Pointcut.Reference(algorithm), new AlgorithmObservationAdvice<TCandidate, TSearchState>(observe));
    }
}

/// <summary>Reports every search state yielded by each algorithm its pointcut selects.</summary>
/// <param name="observe">Receives each observation on the thread that consumes the run.</param>
public sealed class AlgorithmObservationAdvice<TCandidate, TSearchState>(
    Action<AlgorithmObservation<TCandidate, TSearchState>> observe) : AlgorithmAdvice<TCandidate, TSearchState>
    where TSearchState : class, ISearchState
{
    public override WrapperExecutorFactory<IAlgorithmExecutor<TCandidate, TRunSearchSpace, TRunProblem, TSearchState>> CreateWrapperFactory<TRunSearchSpace, TRunProblem>(IAlgorithm<TCandidate, TSearchState> algorithm)
    {
        var state = new ExecutionState();
        return inner => new Executor<TRunSearchSpace, TRunProblem>(
            algorithm, inner, observe, state);
    }

    private sealed class ExecutionState
    {
        public long Iteration { get; set; }
    }

    private sealed class Executor<TRunSearchSpace, TRunProblem>(
        IAlgorithm<TCandidate, TSearchState> observedAlgorithm,
        IAlgorithmExecutor<TCandidate, TRunSearchSpace, TRunProblem, TSearchState> childAlgorithm,
        Action<AlgorithmObservation<TCandidate, TSearchState>> observe,
        ExecutionState state)
        : WrappingAlgorithmExecutor<TCandidate, TRunSearchSpace, TRunProblem, TSearchState>(childAlgorithm)
        where TRunSearchSpace : class, ISearchSpace<TCandidate>
        where TRunProblem : class, IProblem<TCandidate, TRunSearchSpace>
    {
        public override async IAsyncEnumerable<TSearchState> RunStreamingAsync(TRunProblem problem, IRandomNumberGenerator random,
            TSearchState? initialState = null, [EnumeratorCancellation] CancellationToken ct = default)
        {
            var previousState = initialState;
            await foreach (var searchState in ChildAlgorithm.RunStreamingAsync(problem, random, initialState, ct))
            {
                observe(new AlgorithmObservation<TCandidate, TSearchState>(
                    observedAlgorithm, ++state.Iteration, searchState, previousState, problem.SearchSpace, problem));
                previousState = searchState;
                yield return searchState;
            }
        }
    }
}
