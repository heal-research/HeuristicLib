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
        public void Observe<TCandidate, TSearchSpace, TProblem, TSearchState>(
            IAlgorithm<TCandidate, TSearchSpace, TProblem, TSearchState> algorithm,
            Action<AlgorithmObservation<TCandidate, TSearchSpace, TProblem, TSearchState>> observe)
            where TSearchSpace : class, ISearchSpace<TCandidate>
            where TProblem : class, IProblem<TCandidate, TSearchSpace>
            where TSearchState : class, ISearchState =>
            builder.Install(new AlgorithmObservationHook<TCandidate, TSearchSpace, TProblem, TSearchState>(algorithm, observe));
    }
}

internal sealed class AlgorithmObservationHook<TCandidate, TSearchSpace, TProblem, TSearchState>(
    IAlgorithm<TCandidate, TSearchSpace, TProblem, TSearchState> algorithm,
    Action<AlgorithmObservation<TCandidate, TSearchSpace, TProblem, TSearchState>> observe) : IExecutionHook
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
    where TSearchState : class, ISearchState
{
    public void Install(ExecutionInstanceResolverBuilder builder) =>
        builder.Decorate(algorithm, current => new ObservingAlgorithm<TCandidate, TSearchSpace, TProblem, TSearchState>(algorithm, current, observe));
}

internal sealed class ObservingAlgorithm<TCandidate, TSearchSpace, TProblem, TSearchState>(
    IAlgorithm<TCandidate, TSearchSpace, TProblem, TSearchState> observedAlgorithm,
    IAlgorithm<TCandidate, TSearchSpace, TProblem, TSearchState> childAlgorithm,
    Action<AlgorithmObservation<TCandidate, TSearchSpace, TProblem, TSearchState>> observe)
    : IAlgorithm<TCandidate, TSearchSpace, TProblem, TSearchState>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
    where TSearchState : class, ISearchState
{
    public IAlgorithmInstance<TCandidate, TSearchSpace, TProblem, TSearchState> CreateExecutionInstance(ExecutionInstanceResolver resolver) =>
        new Instance(observedAlgorithm, resolver.Resolve(childAlgorithm), observe);

    private sealed class Instance(
        IAlgorithm<TCandidate, TSearchSpace, TProblem, TSearchState> observedAlgorithm,
        IAlgorithmInstance<TCandidate, TSearchSpace, TProblem, TSearchState> childAlgorithm,
        Action<AlgorithmObservation<TCandidate, TSearchSpace, TProblem, TSearchState>> observe)
        : IAlgorithmInstance<TCandidate, TSearchSpace, TProblem, TSearchState>
    {
        private long iteration;

        public async IAsyncEnumerable<TSearchState> RunStreamingAsync(TProblem problem, IRandomNumberGenerator random,
            TSearchState? initialState = null, [EnumeratorCancellation] CancellationToken ct = default)
        {
            var previousState = initialState;
            await foreach (var state in childAlgorithm.RunStreamingAsync(problem, random, initialState, ct))
            {
                observe(new AlgorithmObservation<TCandidate, TSearchSpace, TProblem, TSearchState>(
                    observedAlgorithm, ++iteration, state, previousState, problem.SearchSpace, problem));
                previousState = state;
                yield return state;
            }
        }
    }
}
