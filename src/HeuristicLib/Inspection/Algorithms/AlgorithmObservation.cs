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
/// interceptor transformed it. Sub-iterations an algorithm does not yield are not observed.
/// </remarks>
public sealed record AlgorithmObservation<TCandidate, TSearchSpace, TProblem, TSearchState>(
    IAlgorithm<TCandidate, TSearchState> Algorithm,
    long Iteration,
    TSearchState State,
    TSearchState? PreviousState,
    TSearchSpace SearchSpace,
    TProblem Problem) : Observation<TProblem>(Problem)
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
    where TSearchState : class, ISearchState;

public static class AlgorithmObservations
{
    extension(Aspect)
    {
        /// <summary>Observes every search state yielded by one chosen algorithm.</summary>
        /// <remarks>
        /// The algorithm names only its candidate and search state, so the observation names the search space and
        /// problem it reads. A run over types the observation was not written for is reported when the execution graph
        /// is built.
        /// </remarks>
        public static Aspect Observe<TCandidate, TSearchSpace, TProblem, TSearchState>(
            IAlgorithm<TCandidate, TSearchState> algorithm,
            Action<AlgorithmObservation<TCandidate, TSearchSpace, TProblem, TSearchState>> observe)
            where TSearchSpace : class, ISearchSpace<TCandidate>
            where TProblem : class, IProblem<TCandidate, TSearchSpace>
            where TSearchState : class, ISearchState =>
            Aspect.Create(Pointcut.Reference(algorithm), new AlgorithmObservationAdvice<TCandidate, TSearchSpace, TProblem, TSearchState>(observe));

        /// <summary>
        /// Observes every search state yielded by one chosen algorithm, for an observer that reads no particular search
        /// space or problem.
        /// </summary>
        /// <remarks>
        /// An implicitly typed lambda binds here, because the overload naming the search space and problem cannot infer
        /// them from it. The observation fits every run of the algorithm.
        /// </remarks>
        public static Aspect Observe<TCandidate, TSearchState>(
            IAlgorithm<TCandidate, TSearchState> algorithm,
            Action<AlgorithmObservation<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>, TSearchState>> observe)
            where TSearchState : class, ISearchState =>
            Aspect.Observe<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>, TSearchState>(algorithm, observe);
    }
}

public sealed class AlgorithmObservationAdvice<TCandidate, TSearchSpace, TProblem, TSearchState>(
    Action<AlgorithmObservation<TCandidate, TSearchSpace, TProblem, TSearchState>> observe) : AlgorithmAdvice<TCandidate, TSearchState>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
    where TSearchState : class, ISearchState
{
    public override WrapperExecutionFactory<IAlgorithmExecution<TCandidate, TRunSearchSpace, TRunProblem, TSearchState>> CreateWrapperFactory<TRunSearchSpace, TRunProblem>(IAlgorithm<TCandidate, TSearchState> algorithm)
    {
        ObservationSignature.Require<TSearchSpace, TProblem, TRunSearchSpace, TRunProblem>(this);
        var state = new ExecutionState();
        return inner => new Execution<TRunSearchSpace, TRunProblem>(
            algorithm, inner, observe, state);
    }

    private sealed class ExecutionState
    {
        public long Iteration { get; set; }
    }

    private sealed class Execution<TRunSearchSpace, TRunProblem>(
        IAlgorithm<TCandidate, TSearchState> observedAlgorithm,
        IAlgorithmExecution<TCandidate, TRunSearchSpace, TRunProblem, TSearchState> childAlgorithm,
        Action<AlgorithmObservation<TCandidate, TSearchSpace, TProblem, TSearchState>> observe,
        ExecutionState state)
        : WrappingAlgorithmExecution<TCandidate, TRunSearchSpace, TRunProblem, TSearchState>(childAlgorithm)
        where TRunSearchSpace : class, ISearchSpace<TCandidate>
        where TRunProblem : class, IProblem<TCandidate, TRunSearchSpace>
    {
        public override async IAsyncEnumerable<TSearchState> RunStreamingAsync(TRunProblem problem, IRandomNumberGenerator random,
            TSearchState? initialState = null, [EnumeratorCancellation] CancellationToken ct = default)
        {
            var previousState = initialState;
            await foreach (var searchState in ChildAlgorithm.RunStreamingAsync(problem, random, initialState, ct))
            {
                observe(new AlgorithmObservation<TCandidate, TSearchSpace, TProblem, TSearchState>(
                    observedAlgorithm, ++state.Iteration, searchState, previousState, (TSearchSpace)(object)problem.SearchSpace, (TProblem)(object)problem));
                previousState = searchState;
                yield return searchState;
            }
        }
    }
}
