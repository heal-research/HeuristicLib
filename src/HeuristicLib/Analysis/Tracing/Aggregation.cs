using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Objectives;

namespace HEAL.HeuristicLib.Analysis;

/// <summary>Reusable configuration for aggregating observations, optionally accumulating across them.</summary>
public interface IAggregation<TValue, TResult>
    : IExecutionInstanceResolvable<IAggregationInstance<TValue, TResult>>;

/// <summary>Aggregates readings into an immutable result. Mutable history belongs to this instance.</summary>
public interface IAggregationInstance<in TValue, out TResult> : IExecutionInstance
{
    TResult Aggregate(IReadOnlyList<TValue> readings, ObjectiveDirections objective, IComparer<ObjectiveVector>? objectiveComparer = null);
}

/// <summary>A value strategy without mutable execution state. Resolution returns the strategy itself.</summary>
public abstract record StatelessAggregation<TValue, TResult> : IAggregation<TValue, TResult>, IAggregationInstance<TValue, TResult>
{
    public IAggregationInstance<TValue, TResult> CreateExecutionInstance(ExecutionInstanceResolver resolver) => this;
    public abstract TResult Aggregate(IReadOnlyList<TValue> readings, ObjectiveDirections objective, IComparer<ObjectiveVector>? objectiveComparer = null);
}

public readonly record struct BestMedianWorst(ObjectiveVector Best, ObjectiveVector Median, ObjectiveVector Worst);

public readonly record struct MinMeanMax(double Min, double Mean, double Max);

/// <summary>
/// The best, median and worst evaluated candidate of one set of readings.
/// </summary>
public record BestMedianWorstEntry<TCandidate>(EvaluatedCandidate<TCandidate> Best, EvaluatedCandidate<TCandidate> Median, EvaluatedCandidate<TCandidate> Worst);

public static class BestMedianWorstEntry
{
    public static BestMedianWorstEntry<TCandidate> From<TCandidate>(EvaluatedCandidate<TCandidate> best, EvaluatedCandidate<TCandidate> median, EvaluatedCandidate<TCandidate> worst) => new(best, median, worst);
}

/// <summary>
/// Orders objective vectors by the observed run's objective and keeps the best, median and worst of them.
/// </summary>
public sealed record BestMedianWorstAggregation : StatelessAggregation<ObjectiveVector, BestMedianWorst>
{
    public override BestMedianWorst Aggregate(IReadOnlyList<ObjectiveVector> readings, ObjectiveDirections objective, IComparer<ObjectiveVector>? objectiveComparer = null)
    {
        var ordered = readings.Order(objective.RequireTotalOrder(objectiveComparer)).ToArray();
        if (ordered.Length == 0)
            throw new InvalidOperationException("There are no readings, cannot determine best, median and worst.");

        return new BestMedianWorst(ordered[0], ordered[ordered.Length / 2], ordered[^1]);
    }
}

/// <summary>
/// Orders evaluated candidates by the observed run's objective and keeps the best, median and worst of them.
/// </summary>
public sealed record BestMedianWorstCandidateAggregation<TCandidate> : StatelessAggregation<EvaluatedCandidate<TCandidate>, BestMedianWorstEntry<TCandidate>>
{
    public override BestMedianWorstEntry<TCandidate> Aggregate(IReadOnlyList<EvaluatedCandidate<TCandidate>> readings, ObjectiveDirections objective, IComparer<ObjectiveVector>? objectiveComparer = null)
    {
        var ordered = readings.OrderBy(evaluated => evaluated.ObjectiveVector, objective.RequireTotalOrder(objectiveComparer)).ToArray();
        if (ordered.Length == 0)
            throw new InvalidOperationException("There are no readings, cannot determine best, median and worst.");

        return BestMedianWorstEntry.From(ordered[0], ordered[ordered.Length / 2], ordered[^1]);
    }
}

/// <summary>
/// Keeps the smallest, the arithmetic mean and the largest of the readings.
/// </summary>
public sealed record MinMeanMaxAggregation : StatelessAggregation<double, MinMeanMax>
{
    public override MinMeanMax Aggregate(IReadOnlyList<double> readings, ObjectiveDirections objective, IComparer<ObjectiveVector>? objectiveComparer = null)
    {
        if (readings.Count == 0)
            throw new InvalidOperationException("There are no readings, cannot determine minimum, mean and maximum.");

        return new MinMeanMax(readings.Min(), readings.Average(), readings.Max());
    }
}

/// <summary>
/// Keeps the readings themselves, so that a firing's whole set of values becomes the recorded entry.
/// </summary>
/// <remarks>
/// This is the absence of aggregation. It retains one value per reading rather than one per firing, so a trace using it
/// grows with the observed population and is not a default recommendation for long runs.
/// </remarks>
public sealed record ReadingsAggregation<TValue> : StatelessAggregation<TValue, IReadOnlyList<TValue>>
{
    public override IReadOnlyList<TValue> Aggregate(IReadOnlyList<TValue> readings, ObjectiveDirections objective, IComparer<ObjectiveVector>? objectiveComparer = null) => readings.ToImmutableArray();
}

/// <summary>
/// Keeps the best objective vector of the readings, by the observed run's objective.
/// </summary>
public sealed record BestAggregation : StatelessAggregation<ObjectiveVector, ObjectiveVector>
{
    public override ObjectiveVector Aggregate(IReadOnlyList<ObjectiveVector> readings, ObjectiveDirections objective, IComparer<ObjectiveVector>? objectiveComparer = null)
    {
        if (readings.Count == 0)
            throw new InvalidOperationException("There are no readings, cannot determine the best.");

        return readings.Min(objective.RequireTotalOrder(objectiveComparer))!;
    }
}

/// <summary>
/// Keeps the best evaluated candidate of the readings, by the observed run's objective.
/// </summary>
public sealed record BestCandidateAggregation<TCandidate> : StatelessAggregation<EvaluatedCandidate<TCandidate>, EvaluatedCandidate<TCandidate>>
{
    public override EvaluatedCandidate<TCandidate> Aggregate(IReadOnlyList<EvaluatedCandidate<TCandidate>> readings, ObjectiveDirections objective, IComparer<ObjectiveVector>? objectiveComparer = null)
    {
        if (readings.Count == 0)
            throw new InvalidOperationException("There are no readings, cannot determine the best.");

        var totalOrder = objective.RequireTotalOrder(objectiveComparer);
        return readings.MinBy(evaluated => evaluated.ObjectiveVector, totalOrder)!;
    }
}

/// <summary>
/// Counts the readings of one firing, so that a firing's sample is how many things it produced.
/// </summary>
public sealed record CountAggregation<TValue> : StatelessAggregation<TValue, int>
{
    public override int Aggregate(IReadOnlyList<TValue> readings, ObjectiveDirections objective, IComparer<ObjectiveVector>? objectiveComparer = null) => readings.Count;
}

/// <summary>
/// Takes the one reading of a firing, for a measurement that reads a single value rather than a set of them.
/// </summary>
public sealed record SingleAggregation<TValue> : StatelessAggregation<TValue, TValue>
{
    public override TValue Aggregate(IReadOnlyList<TValue> readings, ObjectiveDirections objective, IComparer<ObjectiveVector>? objectiveComparer = null) =>
        readings.Count == 1
            ? readings[0]
            : throw new InvalidOperationException($"A single aggregation needs exactly one reading per firing but received {readings.Count}.");
}

public static class Aggregate
{
    public static BestSoFarAggregation BestSoFar() => new();
    public static BestCandidateSoFarAggregation<TCandidate> BestCandidateSoFar<TCandidate>() => new();

    /// <summary>
    /// Keeps the readings themselves rather than reducing them.
    /// </summary>
    public static ReadingsAggregation<TValue> Readings<TValue>() => new();

    public static CountAggregation<TValue> Count<TValue>() => new();

    /// <summary>
    /// Records the one value a scalar measurement reads.
    /// </summary>
    public static SingleAggregation<TValue> Single<TValue>() => new();

    public static BestAggregation Best() => new();

    public static BestCandidateAggregation<TCandidate> Best<TCandidate>() => new();

    public static BestMedianWorstAggregation BestMedianWorst() => new();

    public static BestMedianWorstCandidateAggregation<TCandidate> BestMedianWorst<TCandidate>() => new();

    public static MinMeanMaxAggregation MinMeanMax() => new();
}
