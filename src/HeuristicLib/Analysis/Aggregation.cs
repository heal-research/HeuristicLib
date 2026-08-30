using HEAL.HeuristicLib.Objectives;

namespace HEAL.HeuristicLib.Analysis;

/// <summary>
/// Reduces the readings taken at one firing to the single value recorded as that firing's sample.
/// </summary>
public interface IAggregation<in TValue, out TResult>
{
    TResult Aggregate(IReadOnlyList<TValue> readings, ObjectiveDirections objective);
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
public sealed record BestMedianWorstAggregation : IAggregation<ObjectiveVector, BestMedianWorst>
{
    public BestMedianWorst Aggregate(IReadOnlyList<ObjectiveVector> readings, ObjectiveDirections objective)
    {
        var ordered = readings.Order(objective.ToTotalOrderComparer()).ToArray();
        if (ordered.Length == 0)
            throw new InvalidOperationException("There are no readings, cannot determine best, median and worst.");

        return new BestMedianWorst(ordered[0], ordered[ordered.Length / 2], ordered[^1]);
    }
}

/// <summary>
/// Orders evaluated candidates by the observed run's objective and keeps the best, median and worst of them.
/// </summary>
public sealed record BestMedianWorstCandidateAggregation<TCandidate> : IAggregation<EvaluatedCandidate<TCandidate>, BestMedianWorstEntry<TCandidate>>
{
    public BestMedianWorstEntry<TCandidate> Aggregate(IReadOnlyList<EvaluatedCandidate<TCandidate>> readings, ObjectiveDirections objective)
    {
        var ordered = readings.OrderBy(evaluated => evaluated.ObjectiveVector, objective.ToTotalOrderComparer()).ToArray();
        if (ordered.Length == 0)
            throw new InvalidOperationException("There are no readings, cannot determine best, median and worst.");

        return BestMedianWorstEntry.From(ordered[0], ordered[ordered.Length / 2], ordered[^1]);
    }
}

/// <summary>
/// Keeps the smallest, the arithmetic mean and the largest of the readings.
/// </summary>
public sealed record MinMeanMaxAggregation : IAggregation<double, MinMeanMax>
{
    public MinMeanMax Aggregate(IReadOnlyList<double> readings, ObjectiveDirections objective)
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
public sealed record ReadingsAggregation<TValue> : IAggregation<TValue, IReadOnlyList<TValue>>
{
    public IReadOnlyList<TValue> Aggregate(IReadOnlyList<TValue> readings, ObjectiveDirections objective) => [.. readings];
}

/// <summary>
/// Keeps the best objective vector of the readings, by the observed run's objective.
/// </summary>
public sealed record BestAggregation : IAggregation<ObjectiveVector, ObjectiveVector>
{
    public ObjectiveVector Aggregate(IReadOnlyList<ObjectiveVector> readings, ObjectiveDirections objective)
    {
        if (readings.Count == 0)
            throw new InvalidOperationException("There are no readings, cannot determine the best.");

        return readings.Min(objective.ToTotalOrderComparer())!;
    }
}

/// <summary>
/// Keeps the best evaluated candidate of the readings, by the observed run's objective.
/// </summary>
public sealed record BestCandidateAggregation<TCandidate> : IAggregation<EvaluatedCandidate<TCandidate>, EvaluatedCandidate<TCandidate>>
{
    public EvaluatedCandidate<TCandidate> Aggregate(IReadOnlyList<EvaluatedCandidate<TCandidate>> readings, ObjectiveDirections objective)
    {
        if (readings.Count == 0)
            throw new InvalidOperationException("There are no readings, cannot determine the best.");

        var comparer = objective.ToTotalOrderComparer();
        return readings.MinBy(evaluated => evaluated.ObjectiveVector, comparer)!;
    }
}

public static class Aggregate
{
    /// <summary>
    /// Keeps the readings themselves rather than reducing them.
    /// </summary>
    public static ReadingsAggregation<TValue> Readings<TValue>() => new();

    public static BestAggregation Best() => new();

    public static BestCandidateAggregation<TCandidate> Best<TCandidate>() => new();

    public static BestMedianWorstAggregation BestMedianWorst() => new();

    public static BestMedianWorstCandidateAggregation<TCandidate> BestMedianWorst<TCandidate>() => new();

    public static MinMeanMaxAggregation MinMeanMax() => new();
}

public static class ObjectiveOrderExtensions
{
    extension(ObjectiveDirections objective)
    {
        /// <summary>
        /// Gets the objective's total order comparer, falling back to a lexicographic order when it defines none.
        /// </summary>
        /// <remarks>
        /// The fallback is a stopgap, not a design. For a genuinely multi-objective problem a lexicographic order is a
        /// wrong answer rather than a missing one, because it silently privileges the order the dimensions happen to be
        /// declared in. Ordering that an analysis needs should become an explicit choice in the objective system, at
        /// which point this method goes away.
        /// </remarks>
        [Obsolete("The lexicographic fallback silently privileges dimension order. Ordering will become an explicit choice in the objective system.")]
        public IComparer<ObjectiveVector> ToTotalOrderComparer() =>
            objective.TotalOrderComparer is NoTotalOrderComparer
                ? new LexicographicComparer(objective.Directions)
                : objective.TotalOrderComparer;
    }
}
