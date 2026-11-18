using HEAL.HeuristicLib.Execution;

namespace HEAL.HeuristicLib.Inspection;

/// <summary>A count that wrappers add to and terminators read, such as the calls of a mutator; its executor is a <see cref="CountAccumulator"/>.</summary>
/// <remarks>
/// It stays in the configuration graph, so the wrapper, the terminator and a <c>Sharing</c> entry that hold it show who
/// uses which counter. It is a link rather than a value: two counters are different counters, and copies of a configuration
/// keep the link to the same one. The count itself belongs to the run, as its executor, so runs of such copies count
/// separately. Ordinary resolution uses the root's accumulator unless a scope narrows it with
/// <see cref="ResolutionScope.Share"/>, as a budget does to restart it each time it is built.
/// <see cref="AlgorithmRun.Result{TResult}"/> and clocks from <see cref="Clock"/> always read the root's accumulator, which can differ
/// from a scope-local shared accumulator. Accumulator reads and additions are thread-safe.
/// <para>
/// A counter places nothing. A wrapper counts into it, such as <c>Counted</c>, <c>CountedAlgorithm</c> or the
/// substitution a source's <c>Counting</c> returns. The wrapper says what is counted. A counter placed nowhere reads
/// zero, also when a trace reads it.
/// </para>
/// </remarks>
public sealed class Counter : IConfigurationNode<CountAccumulator>
{
    /// <summary>Creates a counter whose count each run accumulates from zero.</summary>
    public Counter() { }

    /// <summary>A clock reading this node, equal on every read of this property.</summary>
    public CounterClock Clock => new(this);

    /// <summary>Creates a fresh accumulator starting at zero.</summary>
    public CountAccumulator CreateExecutionNode(ResolutionScope scope) => new();
}
