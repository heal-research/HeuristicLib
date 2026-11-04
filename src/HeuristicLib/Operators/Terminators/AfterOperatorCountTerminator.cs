using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Inspection;
using HEAL.HeuristicLib.Operators.Terminators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators;

/// <summary>Terminates once the run's result of a counter reaches a maximum count.</summary>
/// <remarks>
/// The counter counts wherever it is attached or placed in the run, such as <c>evaluator.Invocations.Count()</c> attached
/// with <c>Advised</c> or placed with <c>Counted</c>, and this terminator reads the same result, whichever is built
/// first. The count accumulates over the whole run, but the terminator stops only the stream it is placed on; place it
/// on the root algorithm to stop the run.
/// </remarks>
public sealed record AfterOperatorCountTerminator<TCandidate> : Terminator<TCandidate>
{
    public AfterOperatorCountTerminator(Counter counter, int maximumCount)
    {
        Counter = counter;
        MaximumCount = maximumCount;
    }

    public Counter Counter { get; init; }

    public int MaximumCount { get; init; }

    protected override TerminationExecutor<TCandidate> CreateExecutor(ResolutionScope scope) =>
        new Executor(scope.Results.Result(Counter), MaximumCount);

    private sealed class Executor(CountAccumulator counter, int maximumCount) : TerminationExecutor<TCandidate>
    {
        public override bool IsTerminalState() => counter.CurrentCount >= maximumCount;
    }
}

public static class AfterOperatorCountTerminator
{
    public static AfterOperatorCountTerminator<TCandidate> For<TCandidate>(IProblem<TCandidate, ISearchSpace<TCandidate>> problem, Counter counter, int maximumCount) => new(counter, maximumCount);
}
