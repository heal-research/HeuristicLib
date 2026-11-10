using System.Runtime.CompilerServices;

namespace HEAL.HeuristicLib.Execution;

/// <summary>The results of one run: one per instrument, and the time the run's root algorithm has spent executing.</summary>
/// <remarks>
/// <para>
/// The root scope of a resolution tree owns the run's results, so direct resolution without an <c>AlgorithmRun</c> has
/// them too. A result is created at its instrument's first lookup and stays for the rest of the run, also after a
/// construction failure. Lookup is synchronized; each result defines how it may be read while the run writes to it.
/// </para>
/// <para>
/// <see cref="Elapsed"/> counts the time spent inside the calls that advance the algorithm resolved at the root, outside
/// any construction: pauses and the time a consumer spends between streamed states are excluded, and overlapping calls
/// count once.
/// </para>
/// </remarks>
public sealed class RunResults
{
    private readonly Dictionary<Instrument, object> table = new(ReferenceEqualityComparer.Instance);
    private readonly HashSet<Instrument> creating = new(ReferenceEqualityComparer.Instance);
    private readonly TimeProvider timeProvider;
    private readonly Lock elapsedSync = new();
    private int activeCalls;
    private long activeSince;
    private TimeSpan elapsed;

    internal RunResults(TimeProvider timeProvider)
    {
        this.timeProvider = timeProvider;
    }

    // Guards this table and the construction of the scope tree. One lock, because creating a result can read the result of a
    // configuration, while building a configuration can read a result.
    internal Lock Sync { get; } = new();

    // The scope tree's root, which holds the run results of configuration nodes.
    internal ResolutionScope? Root { get; set; }

    /// <summary>Returns this run's result of the instrument, creating it at the first lookup.</summary>
    [OverloadResolutionPriority(1)]
    public TResult Result<TResult>(Instrument<TResult> instrument) where TResult : class =>
        (TResult)Lookup(instrument);

    /// <summary>Returns the run result of the configuration, such as the accumulator of a counter, building it at the first lookup.</summary>
    public TResult Result<TResult>(IConfigurationNode<TResult> configuration) where TResult : class, IRunResult =>
        Root!.RunResult(configuration);

    /// <summary>The time the root algorithm has spent executing so far.</summary>
    public TimeSpan Elapsed
    {
        get
        {
            lock (elapsedSync)
                return activeCalls == 0 ? elapsed : elapsed + timeProvider.GetElapsedTime(activeSince);
        }
    }

    internal void EnterCall()
    {
        lock (elapsedSync)
        {
            if (activeCalls++ == 0)
                activeSince = timeProvider.GetTimestamp();
        }
    }

    internal void ExitCall()
    {
        lock (elapsedSync)
        {
            if (--activeCalls == 0)
                elapsed += timeProvider.GetElapsedTime(activeSince);
        }
    }

    // The lock is reentrant, so an instrument may look up the instruments it builds on while its own result is created.
    private object Lookup(Instrument instrument)
    {
        lock (Sync)
        {
            if (table.TryGetValue(instrument, out var existing))
                return existing;
            if (!creating.Add(instrument))
                throw new InvalidOperationException($"Creating the result of {instrument.GetType().Name} requires that result itself. " +
                    "An instrument may build on other instruments, but not on itself.");
            try
            {
                var result = instrument.Create(this);
                table.Add(instrument, result);
                return result;
            }
            finally
            {
                creating.Remove(instrument);
            }
        }
    }
}
