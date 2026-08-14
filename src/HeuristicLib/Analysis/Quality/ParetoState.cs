using HEAL.HeuristicLib.Optimization;

namespace HEAL.HeuristicLib.Analysis;

public class ParetoState<T>(ObjectiveVector referencePoint, ObjectiveDirections objective)
{
    public ObjectiveDirections Objective { get; } = objective;
    public ObjectiveVector ReferencePoint { get; } = referencePoint;

    protected List<EvaluatedCandidate<T>> Front { get; } = [];

    public virtual bool AddPoints(IEnumerable<EvaluatedCandidate<T>> evaluatedCandidates)
    {
        var t = false;
        foreach (var evaluatedCandidate in evaluatedCandidates)
            if (DominationCalculator.TryAddToParetoFrontInPlace(Front, evaluatedCandidate, Objective))
                t = true;
        return t;
    }

    public virtual void Clear() => Front.Clear();
}
