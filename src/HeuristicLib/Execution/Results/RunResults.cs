namespace HEAL.HeuristicLib.Execution;

/// <summary>The results of one run: one per result-producing configuration, and the time the run has spent advancing its algorithm.</summary>
/// <remarks>
/// <para>
/// The root scope of a resolution tree owns the run's results, so direct resolution without an <c>AlgorithmRun</c> has
/// them too. A successfully created result stays for the rest of the run, also if later construction or execution fails.
/// Lookup is synchronized; each result defines how it may be read while the run writes to it.
/// </para>
/// <para>
/// <c>AlgorithmRun</c> measures each call advancing its retained iterator. <see cref="Elapsed"/> includes time inside an
/// active call, also one that fails or is canceled. Executor construction before the first advance, pauses and
/// consumer time between states are excluded. Deferred child construction during an advance is included.
/// Direct scope execution leaves it at zero; use <c>algorithm.Timed(duration)</c> for explicit measurement there.
/// </para>
/// </remarks>
public sealed class RunResults
{
    private readonly Lock elapsedSync = new();
    private bool active;
    private long activeSince;
    private TimeSpan elapsed;

    internal RunResults(TimeProvider timeProvider)
    {
        TimeProvider = timeProvider;
    }

    /// <summary>The run's one time source, used by timing wrappers, elapsed-time terminators and duration budgets.</summary>
    /// <remarks>
    /// Wrappers of your own measure with this provider too, reading <c>scope.Results.TimeProvider</c> when built.
    /// <see cref="ResolutionScope.Create()"/>, the public <c>AlgorithmRun</c> constructor and <c>CreateRun</c> use the
    /// system clock. Only the library's internal creation paths choose another provider.
    /// </remarks>
    public TimeProvider TimeProvider { get; }

    // Guards the scope tree and its result table. The reentrant lock lets result construction read other results.
    internal Lock Sync { get; } = new();

    // The scope tree's root, which holds the run results of configuration nodes.
    internal ResolutionScope? Root { get; set; }

    /// <summary>Returns the run result of the configuration, such as the accumulator of a counter, building it at the first lookup.</summary>
    /// <remarks>
    /// Always reads the root scope, even when a child scope shares the configuration. Unlike
    /// <see cref="AlgorithmRun.Result{TResult}"/>, this does not check whether the algorithm holds the configuration.
    /// </remarks>
    /// <param name="configuration">The result-producing configuration, identified by reference.</param>
    /// <exception cref="InvalidOperationException">Result construction recursively requests the same configuration.</exception>
    public TResult Result<TResult>(IConfigurationNode<TResult> configuration) where TResult : class, IRunResult =>
        Root!.RunResult(configuration);

    /// <summary>The time the run has spent advancing its algorithm, including the current call.</summary>
    /// <remarks>Zero for direct scope execution without an <c>AlgorithmRun</c>.</remarks>
    public TimeSpan Elapsed
    {
        get
        {
            lock (elapsedSync)
                return active ? elapsed + TimeProvider.GetElapsedTime(activeSince) : elapsed;
        }
    }

    internal void EnterCall()
    {
        lock (elapsedSync)
        {
            activeSince = TimeProvider.GetTimestamp();
            active = true;
        }
    }

    internal void ExitCall()
    {
        lock (elapsedSync)
        {
            elapsed += TimeProvider.GetElapsedTime(activeSince);
            active = false;
        }
    }
}
