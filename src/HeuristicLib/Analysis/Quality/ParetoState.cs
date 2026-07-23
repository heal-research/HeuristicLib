using HEAL.HeuristicLib.Optimization;

namespace HEAL.HeuristicLib.Analysis;

public class ParetoState<T>(ObjectiveVector referencePoint, ObjectiveDirections objective)
{
    public ObjectiveDirections Objective { get; } = objective;
    public ObjectiveVector ReferencePoint { get; } = referencePoint;

    protected List<EvaluatedCandidate<T>> Front { get; } = [];

    public virtual bool AddPoints(IEnumerable<EvaluatedCandidate<T>> solutions)
    {
        var t = false;
        foreach (var solution in solutions)
            if (DominationCalculator.TryAddToParetoFrontInPlace(Front, solution, Objective))
                t = true;
        return t;
    }

    public virtual void Clear() => Front.Clear();
}
