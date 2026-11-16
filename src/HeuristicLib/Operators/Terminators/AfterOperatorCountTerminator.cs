using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Inspection;
using HEAL.HeuristicLib.Operators.Terminators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators;

/// <summary>Terminates once the run's result of a counter reaches a maximum count.</summary>
/// <remarks>
/// The counter counts wherever it is attached or placed in the run, such as <c>evaluator.Invocations.Count()</c> attached
/// with <c>AnalyzedBy</c> or placed with <c>Counted</c>, and this terminator reads the same result, whichever is built
/// first. The count accumulates over the whole run, but the terminator stops only the stream it is placed on; place it
/// on the root algorithm to stop the run.
/// </remarks>
public sealed record AfterOperatorCountTerminator<TCandidate> : Terminator<TCandidate>
{
    public AfterOperatorCountTerminator(IConfigurationNode<CountAccumulator> counter, int maximumCount)
    {
        Counter = counter;
        MaximumCount = maximumCount;
    }

    public IConfigurationNode<CountAccumulator> Counter { get; init; }

    public int MaximumCount { get; init; }

    public static Parameter<AfterOperatorCountTerminator<TCandidate>, IConfigurationNode<CountAccumulator>> CounterParameter { get; } =
        new(nameof(Counter), static terminator => terminator.Counter, static (terminator, value) => terminator with { Counter = value });

    public static Parameter<AfterOperatorCountTerminator<TCandidate>, int> MaximumCountParameter { get; } =
        new(nameof(MaximumCount), static terminator => terminator.MaximumCount, static (terminator, value) => terminator with { MaximumCount = value });

    public static IReadOnlyList<IParameter> Parameters { get; } = [CounterParameter, MaximumCountParameter];

    public override IReadOnlyList<IParameter> GetParameters() => [.. base.GetParameters(), .. Parameters];

    protected override TerminationExecutor CreateExecutor(ResolutionScope<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>, ISearchState> scope) =>
        new Executor(scope.Resolve(Counter), MaximumCount);

    private sealed class Executor(CountAccumulator counter, int maximumCount) : TerminationExecutor
    {
        public override bool IsTerminalState() => counter.CurrentCount >= maximumCount;
    }
}

public static class AfterOperatorCountTerminator
{
    public static AfterOperatorCountTerminator<TCandidate> For<TCandidate>(IProblem<TCandidate, ISearchSpace<TCandidate>> problem, IConfigurationNode<CountAccumulator> counter, int maximumCount) => new(counter, maximumCount);
}
