using HEAL.HeuristicLib.Optimization;

namespace HEAL.HeuristicLib.Analysis.Scoring;

public class ParetoState<T>
{
    protected Lazy<ObjectiveVector>? HyperVolume;
    private List<ISolution<T>> Front { get; } = [];

    public void AddPoints(IEnumerable<ISolution<T>> solutions, Objective objective, ObjectiveVector referencePoint)
    {
        var t = false;
        foreach (var solution in solutions)
        {
            if (DominationCalculator.TryAddToParetoFrontInPlace(Front, solution, objective))
                t = true;
        }

        if (!t)
            return;
        HyperVolume = new Lazy<ObjectiveVector>(() => HyperVolumeCalculator.Calculate(Front.Select(x => x.ObjectiveVector), referencePoint, objective));
    }

    public void Clear()
    {
        Front.Clear();
        HyperVolume = null;
    }
}
