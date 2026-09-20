using System.Runtime.CompilerServices;
using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Analysis;

public static class AlgorithmObservations
{
    extension(ExecutionInstanceResolverBuilder builder)
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
            builder.Install(new AlgorithmObservationHook<TCandidate, TSearchSpace, TProblem, TSearchState>(algorithm, observe));

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

    extension<TCandidate, TSearchSpace, TProblem>(TypedObservationBuilder<TCandidate, TSearchSpace, TProblem> builder)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        /// <remarks>The search state is inferred from the algorithm.</remarks>
        public void Observe<TSearchState>(
            IAlgorithm<TCandidate, TSearchState> algorithm,
            Action<AlgorithmObservation<TCandidate, TSearchSpace, TProblem, TSearchState>> observe)
            where TSearchState : class, ISearchState =>
            builder.Builder.Observe<TCandidate, TSearchSpace, TProblem, TSearchState>(algorithm, observe);
    }
}

internal sealed class AlgorithmObservationHook<TCandidate, TSearchSpace, TProblem, TSearchState>(
    IAlgorithm<TCandidate, TSearchState> algorithm,
    Action<AlgorithmObservation<TCandidate, TSearchSpace, TProblem, TSearchState>> observe) : IExecutionHook
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
    where TSearchState : class, ISearchState
{
    public void Install(ExecutionInstanceResolverBuilder builder) =>
        builder.Decorate(algorithm, current => new ObservingAlgorithm<TCandidate, TSearchSpace, TProblem, TSearchState>(algorithm, current, observe));
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

    public override IAlgorithmInstance<TCandidate, TRunSearchSpace, TRunProblem, TSearchState> CreateExecutionInstance<TRunSearchSpace, TRunProblem>(ExecutionInstanceResolver resolver)
    {
        ObservationSignature.Require<TSearchSpace, TProblem, TRunSearchSpace, TRunProblem>(this);
        return new Instance<TRunSearchSpace, TRunProblem>(
            ObservedAlgorithm, resolver.Resolve<TCandidate, TRunSearchSpace, TRunProblem, TSearchState>(ChildAlgorithm), Observe);
    }

    private sealed class Instance<TRunSearchSpace, TRunProblem>(
        IAlgorithm<TCandidate, TSearchState> observedAlgorithm,
        IAlgorithmInstance<TCandidate, TRunSearchSpace, TRunProblem, TSearchState> childAlgorithm,
        Action<AlgorithmObservation<TCandidate, TSearchSpace, TProblem, TSearchState>> observe)
        : IAlgorithmInstance<TCandidate, TRunSearchSpace, TRunProblem, TSearchState>
        where TRunSearchSpace : class, ISearchSpace<TCandidate>
        where TRunProblem : class, IProblem<TCandidate, TRunSearchSpace>
    {
        private long iteration;

        public async IAsyncEnumerable<TSearchState> RunStreamingAsync(TRunProblem problem, IRandomNumberGenerator random,
            TSearchState? initialState = null, [EnumeratorCancellation] CancellationToken ct = default)
        {
            var previousState = initialState;
            await foreach (var state in childAlgorithm.RunStreamingAsync(problem, random, initialState, ct))
            {
                observe(new AlgorithmObservation<TCandidate, TSearchSpace, TProblem, TSearchState>(
                    observedAlgorithm, ++iteration, state, previousState, (TSearchSpace)(object)problem.SearchSpace, (TProblem)(object)problem));
                previousState = state;
                yield return state;
            }
        }
    }
}
