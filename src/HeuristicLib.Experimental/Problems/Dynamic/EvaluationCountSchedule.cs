namespace HEAL.HeuristicLib.Problems.Dynamic;

/// <summary>
/// Ends an epoch every fixed number of evaluations.
/// </summary>
public sealed class EvaluationCountSchedule : IEpochSchedule
{
    private long inEpoch;

    public EvaluationCountSchedule(int evaluationsPerEpoch)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(evaluationsPerEpoch);
        EvaluationsPerEpoch = evaluationsPerEpoch;
    }

    public int EvaluationsPerEpoch { get; }

    /// <summary>
    /// How many evaluations the epoch in progress has seen.
    /// </summary>
    public long EvaluationsInEpoch => inEpoch;

    public void RecordProgress() => inEpoch++;

    public int TakeDueEpochs()
    {
        var due = (int)(inEpoch / EvaluationsPerEpoch);
        inEpoch -= (long)due * EvaluationsPerEpoch;
        return due;
    }

    public void Restart() => inEpoch = 0;
}
