namespace HEAL.HeuristicLib.Experiments;

public sealed record ExperimentExecutionPolicy
{
    internal ExperimentExecutionKind Kind { get; }

    internal int MaximumConcurrency { get; }

    private ExperimentExecutionPolicy(ExperimentExecutionKind kind, int maximumConcurrency)
    {
        Kind = kind;
        MaximumConcurrency = maximumConcurrency;
    }

    public static ExperimentExecutionPolicy Sequential() => new(ExperimentExecutionKind.Sequential, 1);

    public static ExperimentExecutionPolicy Concurrent() => new(ExperimentExecutionKind.Concurrent, int.MaxValue);

    public static ExperimentExecutionPolicy Concurrent(int maximumConcurrency)
    {
        if (maximumConcurrency <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumConcurrency), maximumConcurrency, "Maximum concurrency must be positive.");
        }

        return new ExperimentExecutionPolicy(ExperimentExecutionKind.Concurrent, maximumConcurrency);
    }
}

internal enum ExperimentExecutionKind
{
    Sequential,
    Concurrent
}
