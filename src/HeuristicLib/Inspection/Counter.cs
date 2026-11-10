using HEAL.HeuristicLib.Execution;

namespace HEAL.HeuristicLib.Inspection;

/// <summary>A count that wrappers add to and terminators read, such as the calls of a mutator; its executor is a <see cref="CountAccumulator"/>.</summary>
/// <remarks>
/// It stays in the configuration graph, so the wrapper, the terminator and a <c>Sharing</c> entry that hold it show who
/// uses which counter. It is a link rather than a value: two counters are different counters, and copies of a configuration
/// keep the link to the same one. The count itself belongs to the run, as its executor, so runs of such copies count
/// separately. The count spans the run, unless a scope narrows it with <see cref="ResolutionScope.Share"/>, as a budget does
/// to restart it each time it is built. Keep the variable, and read it with <c>run.Result(counter).CurrentCount</c>.
/// <para>
/// A counter created from a source, such as <c>evaluator.Invocations.Count()</c>, holds the substitution that places its
/// wrapper, so <c>AnalyzedBy</c> attaches it and a trace listing it as a clock places it. A counter created with
/// <c>new Counter()</c> holds none and counts wherever it is placed explicitly, such as with <c>Counted</c>. The wrapper
/// decides what is counted, so placing a counter explicitly states the metric.
/// </para>
/// </remarks>
public sealed class Counter : IConfigurationNode<CountAccumulator>, IClock<long>
{
    private readonly IReadOnlyList<Substitution> substitutions;

    public Counter()
    {
        substitutions = [];
    }

    // A counter of a source, which holds its placement outside its declared parameters.
    internal Counter(Func<Counter, Substitution> placement)
    {
        substitutions = [placement(this)];
    }

    /// <summary>The substitution that places this counter's wrapper, or none for a counter created with <c>new Counter()</c>.</summary>
    public IReadOnlyList<Substitution> Substitutions => substitutions;

    public CountAccumulator CreateExecutor(ResolutionScope scope) => new();

    Func<object> IClock.Bind(RunResults results)
    {
        var accumulator = results.Result(this);
        return () => accumulator.CurrentCount;
    }
}
