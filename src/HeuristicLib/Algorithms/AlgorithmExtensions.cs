using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Algorithms;

public static class AlgorithmExtensions
{
    extension<TCandidate, TSearchState>(IAlgorithm<TCandidate, TSearchState> algorithm)
        where TSearchState : class, ISearchState
    {
        /// <param name="problem">The problem the run is created for; its search space is what the check runs against.</param>
        /// <param name="random">The run's random source.</param>
        /// <param name="validate">
        /// Checks every operator reachable from the algorithm against the problem's search space before the run is
        /// created, and throws when one declares that it cannot be used there. Pass <see langword="false"/> to skip
        /// the check when deliberately running a configuration whose declared contracts do not hold.
        /// </param>
        public AlgorithmRun<TCandidate, TSearchSpace, TProblem, TSearchState> CreateRun<TProblem, TSearchSpace>(
            Problem<TProblem, TCandidate, TSearchSpace> problem, IRandomNumberGenerator random, bool validate = true)
            where TProblem : Problem<TProblem, TCandidate, TSearchSpace>
            where TSearchSpace : class, ISearchSpace<TCandidate>
        {
            if (validate)
            {
                SearchConfigurationValidation.Validate(algorithm, problem.SearchSpace).ThrowIfInvalid();
            }

            return new(algorithm, (TProblem)problem, random);
        }

        public ExecutionStream<TSearchState> Stream<TProblem, TSearchSpace>(
            Problem<TProblem, TCandidate, TSearchSpace> problem, IRandomNumberGenerator random, TSearchState? initialState = null, CancellationToken ct = default, bool validate = true)
            where TProblem : Problem<TProblem, TCandidate, TSearchSpace>
            where TSearchSpace : class, ISearchSpace<TCandidate> =>
            algorithm.CreateRun(problem, random, validate).Stream(initialState, ct);

        public async Task<TSearchState> CompleteAsync<TProblem, TSearchSpace>(
            Problem<TProblem, TCandidate, TSearchSpace> problem, IRandomNumberGenerator random, TSearchState? initialState = null, CancellationToken ct = default, bool validate = true)
            where TProblem : Problem<TProblem, TCandidate, TSearchSpace>
            where TSearchSpace : class, ISearchSpace<TCandidate> =>
            await algorithm.CreateRun(problem, random, validate).CompleteAsync(initialState, ct);

        public TSearchState Complete<TProblem, TSearchSpace>(
            Problem<TProblem, TCandidate, TSearchSpace> problem, IRandomNumberGenerator random, TSearchState? initialState = null, CancellationToken ct = default, bool validate = true)
            where TProblem : Problem<TProblem, TCandidate, TSearchSpace>
            where TSearchSpace : class, ISearchSpace<TCandidate> =>
            algorithm.CreateRun(problem, random, validate).Complete(initialState, ct);
    }

    /// <remarks>
    /// For a problem held behind its interface. The run is then typed at the interface, so an operator written for one
    /// specific problem will not bind. Passing a problem by its own type selects the overload above instead.
    /// </remarks>
    extension<TCandidate, TSearchState>(IAlgorithm<TCandidate, TSearchState> algorithm)
        where TSearchState : class, ISearchState
    {
        /// <param name="problem">The problem the run is created for; its search space is what the check runs against.</param>
        /// <param name="random">The run's random source.</param>
        /// <param name="validate">
        /// Checks every operator reachable from the algorithm against the problem's search space before the run is
        /// created, and throws when one declares that it cannot be used there. Pass <see langword="false"/> to skip
        /// the check when deliberately running a configuration whose declared contracts do not hold.
        /// </param>
        public AlgorithmRun<TCandidate, TSearchSpace, IProblem<TCandidate, TSearchSpace>, TSearchState> CreateRun<TSearchSpace>(
            IProblem<TCandidate, TSearchSpace> problem, IRandomNumberGenerator random, bool validate = true)
            where TSearchSpace : class, ISearchSpace<TCandidate>
        {
            if (validate)
            {
                SearchConfigurationValidation.Validate(algorithm, problem.SearchSpace).ThrowIfInvalid();
            }

            return new(algorithm, problem, random);
        }

        public ExecutionStream<TSearchState> Stream<TSearchSpace>(
            IProblem<TCandidate, TSearchSpace> problem, IRandomNumberGenerator random, TSearchState? initialState = null, CancellationToken ct = default, bool validate = true)
            where TSearchSpace : class, ISearchSpace<TCandidate> =>
            algorithm.CreateRun(problem, random, validate).Stream(initialState, ct);

        public async Task<TSearchState> CompleteAsync<TSearchSpace>(
            IProblem<TCandidate, TSearchSpace> problem, IRandomNumberGenerator random, TSearchState? initialState = null, CancellationToken ct = default, bool validate = true)
            where TSearchSpace : class, ISearchSpace<TCandidate> =>
            await algorithm.CreateRun(problem, random, validate).CompleteAsync(initialState, ct);

        public TSearchState Complete<TSearchSpace>(
            IProblem<TCandidate, TSearchSpace> problem, IRandomNumberGenerator random, TSearchState? initialState = null, CancellationToken ct = default, bool validate = true)
            where TSearchSpace : class, ISearchSpace<TCandidate> =>
            algorithm.CreateRun(problem, random, validate).Complete(initialState, ct);
    }

    extension<TCandidate, TSearchSpace, TProblem, TSearchState>(
        IAlgorithmInstance<TCandidate, TSearchSpace, TProblem, TSearchState> algorithmInstance)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TSearchState : class, ISearchState
    {
        public async Task<TSearchState> CompleteAsync(TProblem problem, IRandomNumberGenerator random, TSearchState? initialState = null, CancellationToken ct = default) =>
            await algorithmInstance.RunStreamingAsync(problem, random, initialState, ct).LastAsync(ct);

        public IEnumerable<TSearchState> Stream(TProblem problem, IRandomNumberGenerator random, TSearchState? initialState = null, CancellationToken ct = default) =>
            algorithmInstance.RunStreamingAsync(problem, random, initialState, ct).ToBlockingEnumerable(ct);

        public TSearchState Complete(TProblem problem, IRandomNumberGenerator random, TSearchState? initialState = null, CancellationToken ct = default) =>
            algorithmInstance.CompleteAsync(problem, random, initialState, ct).GetAwaiter().GetResult();
    }
}
