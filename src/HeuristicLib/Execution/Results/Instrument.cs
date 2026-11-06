using System.Runtime.CompilerServices;

namespace HEAL.HeuristicLib.Execution;

/// <summary>Defines one result per run, such as a counter or a trace, and where that result is collected.</summary>
/// <remarks>
/// An instrument holds immutable settings only. Each run creates its result at the first lookup and keeps it for the
/// rest of the run, also after a failure, so every placement of the instrument in that run writes into one result.
/// Instruments match by reference: two instruments with equal settings have separate results. Derive from
/// <see cref="Instrument{TResult}"/>.
/// </remarks>
public abstract class Instrument
{
    private IReadOnlyList<Substitution>? substitutions;

    private protected Instrument() { }

    /// <summary>The substitutions that place this instrument in a configuration graph, such as <c>ga.Substitute(trace.Substitutions)</c>.</summary>
    /// <remarks>
    /// Each read returns the same instances, also under concurrent first reads, so that listing the substitutions of two
    /// instruments that build on a third applies the third's once, closest to the node.
    /// </remarks>
    public IReadOnlyList<Substitution> Substitutions => LazyInitializer.EnsureInitialized(ref substitutions, CreateSubstitutions);

    /// <summary>Creates the substitutions that place this instrument. An instrument that needs no placement, such as a clock that only reads something, returns an empty list.</summary>
    protected abstract IReadOnlyList<Substitution> CreateSubstitutions();

    // Creates this run's result and the aspect that places the instrument at its source, which the run publishes together.
    internal abstract (object Result, Aspect? Aspect) Create(RunResults results);

    public sealed override bool Equals(object? obj) => ReferenceEquals(this, obj);

    public sealed override int GetHashCode() => RuntimeHelpers.GetHashCode(this);
}

/// <summary>An instrument whose result in each run has the type <typeparamref name="TResult"/>.</summary>
/// <remarks>
/// <see cref="CreateResult"/> creates the result, and <see cref="CreateAspect"/> the aspect that collects into it
/// wherever the instrument is attached with <c>Advised</c>. Explicit placement, such as <c>Counted</c> or
/// <c>Observed</c>, looks the result up instead. Both hooks run once per run, at the first lookup, and must not keep run
/// data in the instrument.
/// </remarks>
public abstract class Instrument<TResult> : Instrument where TResult : class
{
    protected Instrument() { }

    /// <summary>Creates this instrument's result for one run.</summary>
    /// <param name="results">The run's results, for the results this one reads, such as the clocks of a trace.</param>
    protected abstract TResult CreateResult(RunResults results);

    /// <summary>Creates the aspect that collects into <paramref name="result"/> at the instrument's source, or null when it has none.</summary>
    /// <remarks>
    /// The default places the instrument nowhere. The aspect collects into <paramref name="result"/>: the run publishes
    /// the result and its aspect together, so looking up this instrument itself from either hook fails. An aspect that
    /// builds on other instruments lists their run aspects, taken from <paramref name="results"/>, so that their advice
    /// is shared.
    /// </remarks>
    protected virtual Aspect? CreateAspect(TResult result, RunResults results) => null;

    internal sealed override (object Result, Aspect? Aspect) Create(RunResults results)
    {
        var result = CreateResult(results);
        return (result, CreateAspect(result, results));
    }
}
