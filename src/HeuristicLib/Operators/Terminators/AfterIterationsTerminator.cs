namespace HEAL.HeuristicLib.Operators.Terminators;

public record AfterIterationsTerminator<TGenotype>
  : Terminator<TGenotype, AfterIterationsTerminator<TGenotype>.ExecutionState>
{
    public sealed class ExecutionState
    {
        public int CurrentCount { get; set; }
    }

    public AfterIterationsTerminator(int maximumIterations)
    {
        MaximumIterations = maximumIterations;
    }

    public int MaximumIterations
    {
        get;
        init => field = value > 0
            ? value
            : throw new ArgumentOutOfRangeException(nameof(MaximumIterations), "MaximumIterations must be positive.");
    }

    protected override ExecutionState CreateInitialState() => new();

    protected override bool IsTerminalState(ExecutionState executionState)
    {
        executionState.CurrentCount += 1;
        return executionState.CurrentCount >= MaximumIterations;
    }
}
