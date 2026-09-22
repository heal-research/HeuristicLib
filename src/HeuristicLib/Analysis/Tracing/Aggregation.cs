using HEAL.HeuristicLib.Objectives;

namespace HEAL.HeuristicLib.Analysis;

/// <summary>
/// Turns the readings of one observation into the value a trace stores, and may keep state across observations.
/// </summary>
/// <remarks>
/// An aggregation is an object rather than a setting. One that keeps state, such as a best-so-far, owns that state,
/// so two traces sharing one object share its history. The factories on <see cref="Aggregate"/> return a fresh
/// aggregation per call, which is what makes the ordinary inline use independent.
/// </remarks>
public interface IAggregation<in TValue, out TResult>
{
    TResult Aggregate(IReadOnlyList<TValue> readings, ObjectiveDirections objective, IComparer<ObjectiveVector>? objectiveComparer = null);
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
public sealed class BestMedianWorstAggregation : IAggregation<ObjectiveVector, BestMedianWorst>
{
    public BestMedianWorst Aggregate(IReadOnlyList<ObjectiveVector> readings, ObjectiveDirections objective, IComparer<ObjectiveVector>? objectiveComparer = null)
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
public sealed class BestMedianWorstCandidateAggregation<TCandidate> : IAggregation<EvaluatedCandidate<TCandidate>, BestMedianWorstEntry<TCandidate>>
{
    public BestMedianWorstEntry<TCandidate> Aggregate(IReadOnlyList<EvaluatedCandidate<TCandidate>> readings, ObjectiveDirections objective, IComparer<ObjectiveVector>? objectiveComparer = null)
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
public sealed class MinMeanMaxAggregation : IAggregation<double, MinMeanMax>
{
    public MinMeanMax Aggregate(IReadOnlyList<double> readings, ObjectiveDirections objective, IComparer<ObjectiveVector>? objectiveComparer = null)
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
public sealed class ReadingsAggregation<TValue> : IAggregation<TValue, IReadOnlyList<TValue>>
{
    public IReadOnlyList<TValue> Aggregate(IReadOnlyList<TValue> readings, ObjectiveDirections objective, IComparer<ObjectiveVector>? objectiveComparer = null) => readings.ToImmutableArray();
}

/// <summary>
/// Keeps the best objective vector of the readings, by the observed run's objective.
/// </summary>
public sealed class BestAggregation : IAggregation<ObjectiveVector, ObjectiveVector>
{
    public ObjectiveVector Aggregate(IReadOnlyList<ObjectiveVector> readings, ObjectiveDirections objective, IComparer<ObjectiveVector>? objectiveComparer = null)
    {
        if (readings.Count == 0)
            throw new InvalidOperationException("There are no readings, cannot determine the best.");

        return readings.Min(objective.RequireTotalOrder(objectiveComparer))!;
    }
}

/// <summary>
/// Keeps the best evaluated candidate of the readings, by the observed run's objective.
/// </summary>
public sealed class BestCandidateAggregation<TCandidate> : IAggregation<EvaluatedCandidate<TCandidate>, EvaluatedCandidate<TCandidate>>
{
    public EvaluatedCandidate<TCandidate> Aggregate(IReadOnlyList<EvaluatedCandidate<TCandidate>> readings, ObjectiveDirections objective, IComparer<ObjectiveVector>? objectiveComparer = null)
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
public sealed class CountAggregation<TValue> : IAggregation<TValue, int>
{
    public int Aggregate(IReadOnlyList<TValue> readings, ObjectiveDirections objective, IComparer<ObjectiveVector>? objectiveComparer = null) => readings.Count;
}

/// <summary>
/// Takes the one reading of a firing, for a measurement that reads a single value rather than a set of them.
/// </summary>
public sealed class SingleAggregation<TValue> : IAggregation<TValue, TValue>
{
    public TValue Aggregate(IReadOnlyList<TValue> readings, ObjectiveDirections objective, IComparer<ObjectiveVector>? objectiveComparer = null) =>
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
