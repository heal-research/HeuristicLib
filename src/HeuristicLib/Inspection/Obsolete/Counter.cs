using HEAL.HeuristicLib.Execution;

namespace HEAL.HeuristicLib.Inspection.Obsolete;

/// <summary>A clock that counts, such as the evaluations of a run; its result in each run is a <see cref="CountAccumulator"/>.</summary>
/// <remarks>
/// Create counters from the source they count, such as <c>evaluator.Invocations.Count()</c>. Each call creates a new
/// counter with its own result, so keep the variable: <c>run.Result(counter).CurrentCount</c> reads it, and a terminator
/// such as <c>AfterOperatorCountTerminator</c> checks it. A counter of your own derives from this type and names the
/// wrapper that keeps it current in its substitutions.
/// <para>
/// This type is temporary and goes with the old counters. As a configuration node it hands out the run's
/// result, so a wrapper or terminator reaches old and new counters alike.
/// </para>
/// </remarks>
public abstract class Counter : Clock<long, CountAccumulator>, IConfigurationNode<CountAccumulator>
{
    protected Counter() { }

    protected sealed override CountAccumulator CreateResult(RunResults results) => new();

    protected sealed override long Read(CountAccumulator result) => result.CurrentCount;

    public CountAccumulator CreateExecutor(ResolutionScope scope) => scope.Results.Result(this);
}
