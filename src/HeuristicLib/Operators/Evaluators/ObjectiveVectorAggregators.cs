using HEAL.HeuristicLib.Optimization;

namespace HEAL.HeuristicLib.Operators.Evaluators;

/// <summary>
/// Aggregates the objective vectors produced by repeated evaluations of one candidate.
/// </summary>
public interface IObjectiveVectorAggregator
{
    /// <summary>
    /// Aggregates repeated objective vectors into one objective vector.
    /// </summary>
    /// <param name="objectiveVectors">The objective vectors produced for one candidate. The collection is never empty.</param>
    /// <param name="objectiveDirections">The problem's objective directions and total objective order.</param>
    ObjectiveVector Aggregate(IReadOnlyList<ObjectiveVector> objectiveVectors, ObjectiveDirections objectiveDirections);
}

public static class ObjectiveVectorAggregation
{
    public static MeanObjectiveVectorAggregator Mean { get; } = new();
    public static MedianObjectiveVectorAggregator Median { get; } = new();
    public static BestObjectiveVectorAggregator Best { get; } = new();
    public static WorstObjectiveVectorAggregator Worst { get; } = new();
}

public sealed record MeanObjectiveVectorAggregator : IObjectiveVectorAggregator
{
    public ObjectiveVector Aggregate(IReadOnlyList<ObjectiveVector> objectiveVectors, ObjectiveDirections objectiveDirections) =>
        objectiveVectors.Mean();
}

public sealed record MedianObjectiveVectorAggregator : IObjectiveVectorAggregator
{
    public ObjectiveVector Aggregate(IReadOnlyList<ObjectiveVector> objectiveVectors, ObjectiveDirections objectiveDirections) =>
        objectiveVectors.Median(objectiveDirections);
}

public sealed record BestObjectiveVectorAggregator : IObjectiveVectorAggregator
{
    public ObjectiveVector Aggregate(IReadOnlyList<ObjectiveVector> objectiveVectors, ObjectiveDirections objectiveDirections) =>
        objectiveVectors.Best(objectiveDirections);
}

public sealed record WorstObjectiveVectorAggregator : IObjectiveVectorAggregator
{
    public ObjectiveVector Aggregate(IReadOnlyList<ObjectiveVector> objectiveVectors, ObjectiveDirections objectiveDirections) =>
        objectiveVectors.Worst(objectiveDirections);
}
