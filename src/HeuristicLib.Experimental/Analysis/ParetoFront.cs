using HEAL.HeuristicLib.Objectives;

namespace HEAL.HeuristicLib.Analysis;

/// <summary>
/// The non-dominated candidates seen so far, kept as one growing set.
/// </summary>
/// <remarks>
/// This is the accumulator behind <see cref="ParetoFrontAnalyzer{T, TS, TP}"/> and the hypervolume aggregation. It is
/// mutable and belongs to whatever accumulates it, and <see cref="Points"/> is a live view of it, so read it once the
/// run has finished rather than during it.
/// </remarks>
public sealed class ParetoFront<T>(ObjectiveVector referencePoint, ObjectiveDirections objective)
{
    private readonly Lock sync = new();
    private readonly List<EvaluatedCandidate<T>> front = [];

    public ObjectiveDirections Objective { get; } = objective;
    public ObjectiveVector ReferencePoint { get; } = referencePoint;

    /// <summary>How many points are on the front.</summary>
    public int Count { get { lock (sync) return front.Count; } }

    /// <summary>The points on the front.</summary>
    public IReadOnlyList<EvaluatedCandidate<T>> Points => front;

    /// <summary>Adds the candidates that are not dominated. Returns whether the front changed.</summary>
    public bool AddPoints(IEnumerable<EvaluatedCandidate<T>> evaluatedCandidates)
    {
        lock (sync)
        {
            var changed = false;
            foreach (var evaluatedCandidate in evaluatedCandidates)
                if (DominationCalculator.TryAddToParetoFrontInPlace(front, evaluatedCandidate, Objective))
                    changed = true;
            return changed;
        }
    }

    public void Clear()
    {
        lock (sync)
            front.Clear();
    }
}
