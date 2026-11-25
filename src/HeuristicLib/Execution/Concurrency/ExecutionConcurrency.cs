namespace HEAL.HeuristicLib.Execution;

public sealed record ExecutionConcurrency
{
    public ExecutionConcurrencyKind Kind { get; }

    public int MaximumConcurrency { get; }

    private ExecutionConcurrency(ExecutionConcurrencyKind kind, int maximumConcurrency)
    {
        Kind = kind;
        MaximumConcurrency = maximumConcurrency;
    }

    public static ExecutionConcurrency Sequential() => new(ExecutionConcurrencyKind.Sequential, 1);

    public static ExecutionConcurrency Concurrent() => new(ExecutionConcurrencyKind.Concurrent, int.MaxValue);

    public static ExecutionConcurrency Concurrent(int maximumConcurrency)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maximumConcurrency);

        return new ExecutionConcurrency(ExecutionConcurrencyKind.Concurrent, maximumConcurrency);
    }
}

public enum ExecutionConcurrencyKind
{
    Sequential,
    Concurrent
}
