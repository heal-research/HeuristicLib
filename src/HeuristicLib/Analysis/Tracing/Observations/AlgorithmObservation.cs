using System.Runtime.CompilerServices;
using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Analysis;

public static class AlgorithmObservations
{
    extension(ResolutionScopeBuilder builder)
    {
        /// <summary>Observes every search state yielded by one chosen algorithm.</summary>
        /// <remarks>
        /// The algorithm names only its candidate and search state, so the observation names the search space and
        /// problem it reads. A run over types the observation was not written for is reported when the execution graph
        /// is built.
        /// </remarks>
        public void Observe<TCandidate, TSearchSpace, TProblem, TSearchState>(
            IAlgorithm<TCandidate, TSearchState> algorithm,
            Action<AlgorithmObservation<TCandidate, TSearchSpace, TProblem, TSearchState>> observe)
            where TSearchSpace : class, ISearchSpace<TCandidate>
            where TProblem : class, IProblem<TCandidate, TSearchSpace>
            where TSearchState : class, ISearchState =>
            builder.Install(new AlgorithmObservationModule<TCandidate, TSearchSpace, TProblem, TSearchState>(algorithm, observe));

        /// <summary>
        /// Observes every search state yielded by one chosen algorithm, for an observer that reads no particular search
        /// space or problem.
        /// </summary>
        /// <remarks>
        /// An implicitly typed lambda binds here, because the overload naming the search space and problem cannot infer
        /// them from it. The observation fits every run of the algorithm.
        /// </remarks>
        public void Observe<TCandidate, TSearchState>(
            IAlgorithm<TCandidate, TSearchState> algorithm,
            Action<AlgorithmObservation<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>, TSearchState>> observe)
            where TSearchState : class, ISearchState =>
            builder.Observe<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>, TSearchState>(algorithm, observe);
    }
}

internal sealed class AlgorithmObservationModule<TCandidate, TSearchSpace, TProblem, TSearchState>(
    IAlgorithm<TCandidate, TSearchState> algorithm,
    Action<AlgorithmObservation<TCandidate, TSearchSpace, TProblem, TSearchState>> observe) : IExecutionModule
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
    where TSearchState : class, ISearchState
{
    public void Install(ResolutionScopeBuilder builder) =>
        builder.Wrap(algorithm, current => new ObservingAlgorithm<TCandidate, TSearchSpace, TProblem, TSearchState>(algorithm, current, observe));
}

internal sealed record ObservingAlgorithm<TCandidate, TSearchSpace, TProblem, TSearchState>(
    IAlgorithm<TCandidate, TSearchState> ObservedAlgorithm,
    IAlgorithm<TCandidate, TSearchState> ChildAlgorithm,
    Action<AlgorithmObservation<TCandidate, TSearchSpace, TProblem, TSearchState>> Observe)
    : Algorithm<ObservingAlgorithm<TCandidate, TSearchSpace, TProblem, TSearchState>, TCandidate, TSearchState>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
    where TSearchState : class, ISearchState
{
    public override bool Fits(ExecutionSignature execution) =>
        base.Fits(execution) && ObservationSignature.Fits<TSearchSpace, TProblem>(execution) && execution.Fits(ChildAlgorithm);

    public override IAlgorithmExecution<TCandidate, TRunSearchSpace, TRunProblem, TSearchState> CreateExecutionInstance<TRunSearchSpace, TRunProblem>(ResolutionScope scope)
    {
        ObservationSignature.Require<TSearchSpace, TProblem, TRunSearchSpace, TRunProblem>(this);
        return new Execution<TRunSearchSpace, TRunProblem>(
            ObservedAlgorithm, scope.Resolve<TCandidate, TRunSearchSpace, TRunProblem, TSearchState>(ChildAlgorithm), Observe, new ExecutionState());
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
        : IAlgorithmExecution<TCandidate, TRunSearchSpace, TRunProblem, TSearchState>
        where TRunSearchSpace : class, ISearchSpace<TCandidate>
        where TRunProblem : class, IProblem<TCandidate, TRunSearchSpace>
    {
        public async IAsyncEnumerable<TSearchState> RunStreamingAsync(TRunProblem problem, IRandomNumberGenerator random,
            TSearchState? initialState = null, [EnumeratorCancellation] CancellationToken ct = default)
        {
            var previousState = initialState;
            await foreach (var searchState in childAlgorithm.RunStreamingAsync(problem, random, initialState, ct))
            {
                observe(new AlgorithmObservation<TCandidate, TSearchSpace, TProblem, TSearchState>(
                    observedAlgorithm, ++state.Iteration, searchState, previousState, (TSearchSpace)(object)problem.SearchSpace, (TProblem)(object)problem));
                previousState = searchState;
                yield return searchState;
            }
        }
    }
}
