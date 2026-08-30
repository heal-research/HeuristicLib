using HEAL.HeuristicLib.Objectives;

namespace HEAL.HeuristicLib.Analysis;

/// <summary>
/// Decides whether the value aggregated at one firing becomes a trace entry.
/// </summary>
/// <remarks>
/// Retention is what separates a trace that records everything from one that records only what is worth keeping.
/// Recording only on improvement, for instance, produces a far shorter series than recording at every firing while
/// describing the same run. A retention keeps whatever it needs to decide, so it belongs to one trace and one run, the
/// same way an analyzer does.
/// </remarks>
public interface IRetention<in TResult>
{
    bool ShouldRecord(TResult value, ObjectiveDirections objective);
}

/// <summary>
/// Records every firing.
/// </summary>
public sealed class AlwaysRetention<TResult> : IRetention<TResult>
{
    public bool ShouldRecord(TResult value, ObjectiveDirections objective) => true;
}

/// <summary>
/// Records every n-th firing, counting firings rather than recorded entries.
/// </summary>
public sealed class EveryNthRetention<TResult> : IRetention<TResult>
{
    private long observed;

    public EveryNthRetention(int interval)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(interval);
        Interval = interval;
    }

    public int Interval { get; }

    public bool ShouldRecord(TResult value, ObjectiveDirections objective) => ++observed % Interval == 0;
}

/// <summary>
/// Records a value only when it differs from the one recorded before it.
/// </summary>
public sealed class OnChangeRetention<TResult>(IEqualityComparer<TResult> comparer) : IRetention<TResult>
{
    private TResult? lastRecorded;
    private bool hasLastRecorded;

    public bool ShouldRecord(TResult value, ObjectiveDirections objective)
    {
        if (hasLastRecorded && comparer.Equals(value, lastRecorded!))
            return false;

        lastRecorded = value;
        hasLastRecorded = true;
        return true;
    }
}

/// <summary>
/// Records a value only when it is better, by the observed run's objective, than the one recorded before it.
/// </summary>
/// <remarks>
/// The objective vector to compare is selected from the value, so this works for a trace of candidates as well as for a
/// trace of objective vectors.
/// </remarks>
public sealed class OnImprovementRetention<TResult>(Func<TResult, ObjectiveVector> objectiveVectorSelector) : IRetention<TResult>
{
    private ObjectiveVector? bestRecorded;

    public bool ShouldRecord(TResult value, ObjectiveDirections objective)
    {
        var objectiveVector = objectiveVectorSelector(value);
        if (bestRecorded is not null && objective.ToTotalOrderComparer().Compare(objectiveVector, bestRecorded) >= 0)
            return false;

        bestRecorded = objectiveVector;
        return true;
    }
}

public static class Retain
{
    /// <summary>
    /// Records every firing. This is what a trace does when no retention is given.
    /// </summary>
    public static AlwaysRetention<TResult> Always<TResult>() => new();

    public static EveryNthRetention<TResult> EveryNth<TResult>(int interval) => new(interval);

    public static OnChangeRetention<TResult> OnChange<TResult>(IEqualityComparer<TResult>? comparer = null) =>
        new(comparer ?? EqualityComparer<TResult>.Default);

    public static OnImprovementRetention<ObjectiveVector> OnImprovement() => new(static objectiveVector => objectiveVector);

    public static OnImprovementRetention<EvaluatedCandidate<TCandidate>> OnImprovement<TCandidate>() =>
        new(static evaluated => evaluated.ObjectiveVector);

    public static OnImprovementRetention<TResult> OnImprovement<TResult>(Func<TResult, ObjectiveVector> objectiveVectorSelector) =>
        new(objectiveVectorSelector);
}
