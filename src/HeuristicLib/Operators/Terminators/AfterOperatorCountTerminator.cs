using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Inspection;
using HEAL.HeuristicLib.Operators.Terminators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators;

public sealed record AfterOperatorCountTerminator<TCandidate> : Terminator<TCandidate>
{
    public AfterOperatorCountTerminator(ResourceHandle<CountAccumulator> counter, int maximumCount)
    {
        Counter = counter;
        MaximumCount = maximumCount;
    }

    public ResourceHandle<CountAccumulator> Counter { get; init; }

    public int MaximumCount { get; init; }

    protected override TerminationExecutor<TCandidate> CreateExecutor(ResolutionScope scope) =>
        new Executor(scope.Resource(Counter), MaximumCount);

    private sealed class Executor(CountAccumulator counter, int maximumCount) : TerminationExecutor<TCandidate>
    {
        public override bool IsTerminalState() => counter.CurrentCount >= maximumCount;
    }
}

public static class AfterOperatorCountTerminator
{
    public static AfterOperatorCountTerminator<TCandidate> For<TCandidate>(IProblem<TCandidate, ISearchSpace<TCandidate>> problem, ResourceHandle<CountAccumulator> counter, int maximumCount) => new(counter, maximumCount);
}
