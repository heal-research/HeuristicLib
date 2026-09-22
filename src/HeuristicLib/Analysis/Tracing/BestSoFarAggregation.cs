using HEAL.HeuristicLib.Objectives;

namespace HEAL.HeuristicLib.Analysis;

/// <summary>
/// Keeps the best objective vector seen so far, across observations.
/// </summary>
/// <remarks>The running best is this object's own state, so give each trace its own.</remarks>
public sealed class BestSoFarAggregation : IAggregation<ObjectiveVector, ObjectiveVector>
{
    private readonly BestAggregation best = new();
    private ObjectiveVector? bestSoFar;

    public ObjectiveVector Aggregate(IReadOnlyList<ObjectiveVector> readings, ObjectiveDirections objective, IComparer<ObjectiveVector>? objectiveComparer = null)
    {
        var current = best.Aggregate(readings, objective, objectiveComparer);
        if (bestSoFar is null || objective.RequireTotalOrder(objectiveComparer).Compare(current, bestSoFar) < 0)
            bestSoFar = current;
        return bestSoFar;
    }
}

/// <summary>
/// Keeps the best evaluated candidate seen so far, across observations.
/// </summary>
/// <remarks>The running best is this object's own state, so give each trace its own.</remarks>
public sealed class BestCandidateSoFarAggregation<TCandidate> : IAggregation<EvaluatedCandidate<TCandidate>, EvaluatedCandidate<TCandidate>>
{
    private readonly BestCandidateAggregation<TCandidate> best = new();
    private EvaluatedCandidate<TCandidate>? bestSoFar;

    public EvaluatedCandidate<TCandidate> Aggregate(IReadOnlyList<EvaluatedCandidate<TCandidate>> readings, ObjectiveDirections objective, IComparer<ObjectiveVector>? objectiveComparer = null)
    {
        var current = best.Aggregate(readings, objective, objectiveComparer);
        if (bestSoFar is null || objective.RequireTotalOrder(objectiveComparer).Compare(current.ObjectiveVector, bestSoFar.ObjectiveVector) < 0)
            bestSoFar = current;
        return bestSoFar;
    }
}
