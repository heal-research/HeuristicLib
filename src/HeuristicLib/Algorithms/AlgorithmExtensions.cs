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
        /// <summary>Creates a ready run, placing the supplied substitutions before validating the resulting configuration.</summary>
        /// <param name="problem">The problem the run is created for; its search space is what the check runs against.</param>
        /// <param name="random">The run's random source.</param>
        /// <param name="substitutions">
        /// Optional analyzers, counter and duration placements (<c>Counting</c>, <c>Timing</c>), clocks that observe, or other
        /// substitutions, expanded and placed together in one call. A later entry sits closer to its target. Shared single
        /// substitutions apply once at their last position. Named targets must come from this configuration, after any
        /// budgets, such as <c>limited.Algorithm.Iterations</c>.
        /// </param>
        /// <param name="validate">
        /// Checks every configuration reachable from the algorithm before the run is created, and throws when one
        /// declares an invariant this search space contradicts or was not written for this run at all. Pass
        /// <see langword="false"/> to skip the check when deliberately running a configuration whose declared
        /// contracts do not hold.
        /// </param>
        /// <returns>A run in <see cref="RunLifecycleState.Ready"/>; execution starts when a stream is requested.</returns>
        /// <remarks>
        /// The caller's configuration is unchanged. Placements exist only in the run's configuration; substitutions
        /// themselves are never persisted. After validation, every listed configuration node that places something, such as
        /// an analyzer or a clock that observes, is checked for placement before execution. A reachable declared delegate permits unknown placement.
        /// </remarks>
        /// <exception cref="InvalidOperationException">
        /// A substitution cannot be applied, validation fails, or a supplied result-producing definition is provably absent.
        /// </exception>
        public AlgorithmRun<TCandidate, TSearchSpace, TProblem, TSearchState> CreateRun<TProblem, TSearchSpace>(
            Problem<TProblem, TCandidate, TSearchSpace> problem, IRandomNumberGenerator random, IReadOnlyList<ISubstitution>? substitutions = null, bool validate = true)
            where TProblem : Problem<TProblem, TCandidate, TSearchSpace>
            where TSearchSpace : class, ISearchSpace<TCandidate>
        {
            if (substitutions is { Count: > 0 })
                algorithm = algorithm.Substitute(substitutions);
            if (validate)
            {
                SearchConfigurationValidation
                    .Validate(algorithm, problem.SearchSpace, ExecutionSignature.For<TSearchSpace, TProblem, TSearchState>())
                    .ThrowIfInvalid();
            }

            var run = new AlgorithmRun<TCandidate, TSearchSpace, TProblem, TSearchState>(algorithm, (TProblem)problem, random);
            foreach (var substitution in substitutions ?? [])
                if (substitution is IConfigurationNode node)
                    run.RequireHeld(node);
            return run;
        }

        /// <summary>Creates a fresh run and starts a single-consumer stream; stopping consumption pauses that run.</summary>
        /// <param name="problem">The problem whose search space and contracts are used for validation.</param>
        /// <param name="random">The new run's random source.</param>
        /// <param name="initialState">An optional starting state.</param>
        /// <param name="ct">Cooperatively cancels consumption at root yield boundaries.</param>
        /// <param name="validate">Whether to validate the configuration before starting the run.</param>
        /// <remarks>Keep an explicit run to resume execution or read analysis results.</remarks>
        public ExecutionStream<TSearchState> Stream<TProblem, TSearchSpace>(
            Problem<TProblem, TCandidate, TSearchSpace> problem, IRandomNumberGenerator random, TSearchState? initialState = null, CancellationToken ct = default, bool validate = true)
            where TProblem : Problem<TProblem, TCandidate, TSearchSpace>
            where TSearchSpace : class, ISearchSpace<TCandidate> =>
            algorithm.CreateRun(problem, random, validate: validate).Stream(initialState, ct);

        /// <summary>Creates a fresh run and asynchronously consumes it to its last yielded state.</summary>
        /// <param name="problem">The problem whose search space and contracts are used for validation.</param>
        /// <param name="random">The new run's random source.</param>
        /// <param name="initialState">An optional starting state.</param>
        /// <param name="ct">Cooperatively cancels consumption at root yield boundaries.</param>
        /// <param name="validate">Whether to validate the configuration before starting the run.</param>
        /// <remarks>Keep an explicit run to read analysis results. Execution yielding no state has no last state.</remarks>
        public async Task<TSearchState> CompleteAsync<TProblem, TSearchSpace>(
            Problem<TProblem, TCandidate, TSearchSpace> problem, IRandomNumberGenerator random, TSearchState? initialState = null, CancellationToken ct = default, bool validate = true)
            where TProblem : Problem<TProblem, TCandidate, TSearchSpace>
            where TSearchSpace : class, ISearchSpace<TCandidate> =>
            await algorithm.CreateRun(problem, random, validate: validate).CompleteAsync(initialState, ct);

        /// <summary>Creates a fresh run and blocks until execution ends, returning its last yielded state.</summary>
        /// <param name="problem">The problem whose search space and contracts are used for validation.</param>
        /// <param name="random">The new run's random source.</param>
        /// <param name="initialState">An optional starting state.</param>
        /// <param name="ct">Cooperatively cancels consumption at root yield boundaries.</param>
        /// <param name="validate">Whether to validate the configuration before starting the run.</param>
        /// <remarks>Keep an explicit run to read analysis results. Execution yielding no state has no last state.</remarks>
        public TSearchState Complete<TProblem, TSearchSpace>(
            Problem<TProblem, TCandidate, TSearchSpace> problem, IRandomNumberGenerator random, TSearchState? initialState = null, CancellationToken ct = default, bool validate = true)
            where TProblem : Problem<TProblem, TCandidate, TSearchSpace>
            where TSearchSpace : class, ISearchSpace<TCandidate> =>
            algorithm.CreateRun(problem, random, validate: validate).Complete(initialState, ct);
    }

    /// <remarks>
    /// For a problem held behind its interface. The run is then typed at the interface, so an operator written for one
    /// specific problem will not bind. Passing a problem by its own type selects the overload above instead.
    /// </remarks>
    extension<TCandidate, TSearchState>(IAlgorithm<TCandidate, TSearchState> algorithm)
        where TSearchState : class, ISearchState
    {
        /// <summary>Creates a ready run, placing the supplied substitutions before validating the resulting configuration.</summary>
        /// <param name="problem">The problem the run is created for; its search space is what the check runs against.</param>
        /// <param name="random">The run's random source.</param>
        /// <param name="substitutions">
        /// Optional analyzers, counter and duration placements (<c>Counting</c>, <c>Timing</c>), clocks that observe, or other
        /// substitutions, expanded and placed together in one call. A later entry sits closer to its target. Shared single
        /// substitutions apply once at their last position. Named targets must come from this configuration, after any
        /// budgets, such as <c>limited.Algorithm.Iterations</c>.
        /// </param>
        /// <param name="validate">
        /// Checks every configuration reachable from the algorithm before the run is created, and throws when one
        /// declares an invariant this search space contradicts or was not written for this run at all. Pass
        /// <see langword="false"/> to skip the check when deliberately running a configuration whose declared
        /// contracts do not hold.
        /// </param>
        /// <returns>A run in <see cref="RunLifecycleState.Ready"/>; execution starts when a stream is requested.</returns>
        /// <remarks>
        /// The caller's configuration is unchanged. Placements exist only in the run's configuration; substitutions
        /// themselves are never persisted. After validation, every listed configuration node that places something, such as
        /// an analyzer or a clock that observes, is checked for placement before execution. A reachable declared delegate permits unknown placement.
        /// </remarks>
        /// <exception cref="InvalidOperationException">
        /// A substitution cannot be applied, validation fails, or a supplied result-producing definition is provably absent.
        /// </exception>
        public AlgorithmRun<TCandidate, TSearchSpace, IProblem<TCandidate, TSearchSpace>, TSearchState> CreateRun<TSearchSpace>(
            IProblem<TCandidate, TSearchSpace> problem, IRandomNumberGenerator random, IReadOnlyList<ISubstitution>? substitutions = null, bool validate = true)
            where TSearchSpace : class, ISearchSpace<TCandidate>
        {
            if (substitutions is { Count: > 0 })
                algorithm = algorithm.Substitute(substitutions);
            if (validate)
            {
                SearchConfigurationValidation
                    .Validate(algorithm, problem.SearchSpace, ExecutionSignature.For<TSearchSpace, IProblem<TCandidate, TSearchSpace>, TSearchState>())
                    .ThrowIfInvalid();
            }

            var run = new AlgorithmRun<TCandidate, TSearchSpace, IProblem<TCandidate, TSearchSpace>, TSearchState>(algorithm, problem, random);
            foreach (var substitution in substitutions ?? [])
                if (substitution is IConfigurationNode node)
                    run.RequireHeld(node);
            return run;
        }

        /// <summary>Creates a fresh run and starts a single-consumer stream; stopping consumption pauses that run.</summary>
        /// <param name="problem">The problem whose search space and contracts are used for validation.</param>
        /// <param name="random">The new run's random source.</param>
        /// <param name="initialState">An optional starting state.</param>
        /// <param name="ct">Cooperatively cancels consumption at root yield boundaries.</param>
        /// <param name="validate">Whether to validate the configuration before starting the run.</param>
        /// <remarks>Keep an explicit run to resume execution or read analysis results.</remarks>
        public ExecutionStream<TSearchState> Stream<TSearchSpace>(
            IProblem<TCandidate, TSearchSpace> problem, IRandomNumberGenerator random, TSearchState? initialState = null, CancellationToken ct = default, bool validate = true)
            where TSearchSpace : class, ISearchSpace<TCandidate> =>
            algorithm.CreateRun(problem, random, validate: validate).Stream(initialState, ct);

        /// <summary>Creates a fresh run and asynchronously consumes it to its last yielded state.</summary>
        /// <param name="problem">The problem whose search space and contracts are used for validation.</param>
        /// <param name="random">The new run's random source.</param>
        /// <param name="initialState">An optional starting state.</param>
        /// <param name="ct">Cooperatively cancels consumption at root yield boundaries.</param>
        /// <param name="validate">Whether to validate the configuration before starting the run.</param>
        /// <remarks>Keep an explicit run to read analysis results. Execution yielding no state has no last state.</remarks>
        public async Task<TSearchState> CompleteAsync<TSearchSpace>(
            IProblem<TCandidate, TSearchSpace> problem, IRandomNumberGenerator random, TSearchState? initialState = null, CancellationToken ct = default, bool validate = true)
            where TSearchSpace : class, ISearchSpace<TCandidate> =>
            await algorithm.CreateRun(problem, random, validate: validate).CompleteAsync(initialState, ct);

        /// <summary>Creates a fresh run and blocks until execution ends, returning its last yielded state.</summary>
        /// <param name="problem">The problem whose search space and contracts are used for validation.</param>
        /// <param name="random">The new run's random source.</param>
        /// <param name="initialState">An optional starting state.</param>
        /// <param name="ct">Cooperatively cancels consumption at root yield boundaries.</param>
        /// <param name="validate">Whether to validate the configuration before starting the run.</param>
        /// <remarks>Keep an explicit run to read analysis results. Execution yielding no state has no last state.</remarks>
        public TSearchState Complete<TSearchSpace>(
            IProblem<TCandidate, TSearchSpace> problem, IRandomNumberGenerator random, TSearchState? initialState = null, CancellationToken ct = default, bool validate = true)
            where TSearchSpace : class, ISearchSpace<TCandidate> =>
            algorithm.CreateRun(problem, random, validate: validate).Complete(initialState, ct);
    }

    extension<TSearchState>(
        IAlgorithmExecutor<TSearchState> algorithmExecutor)
        where TSearchState : class, ISearchState
    {
        public async Task<TSearchState> CompleteAsync(IRandomNumberGenerator random, TSearchState? initialState = null, CancellationToken ct = default) =>
            await algorithmExecutor.RunStreamingAsync(random, initialState, ct).LastAsync(ct);

        public IEnumerable<TSearchState> Stream(IRandomNumberGenerator random, TSearchState? initialState = null, CancellationToken ct = default) =>
            algorithmExecutor.RunStreamingAsync(random, initialState, ct).ToBlockingEnumerable(ct);

        public TSearchState Complete(IRandomNumberGenerator random, TSearchState? initialState = null, CancellationToken ct = default) =>
            algorithmExecutor.CompleteAsync(random, initialState, ct).GetAwaiter().GetResult();
    }
}
