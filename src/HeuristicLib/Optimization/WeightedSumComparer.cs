using HEAL.HeuristicLib.Genotypes.Vectors;

namespace HEAL.HeuristicLib.Optimization;

public class WeightedSumComparer : IComparer<ObjectiveVector>
{
    private readonly ImmutableArray<ObjectiveDirection> objectives;
    private readonly RealVector weights;

    public WeightedSumComparer(IReadOnlyList<ObjectiveDirection> objectives, IReadOnlyList<double>? weights = null)
    {
        if (weights is not null && objectives.Count != weights.Count)
        {
            throw new ArgumentException("Objective and weights must have the same length");
        }

        this.objectives = objectives.ToImmutableArray();
        this.weights = weights is null
          ? RealVector.Repeat(1.0, this.objectives.Length)
          : RealVector.Create(weights);
    }

    public int Compare(ObjectiveVector? x, ObjectiveVector? y)
    {
        if ((x is not null && x.Count != objectives.Length) || (y is not null && y.Count != objectives.Length))
        {
            throw new ArgumentException("Objective vector must have the same length as the objective directions");
        }

        if (x is null && y is null)
        {
            return 0;
        }

        if (x is null)
        {
            return -1;
        }

        if (y is null)
        {
            return +1;
        }

        var xObjectiveVector = new RealVector(x);
        var yObjectiveVector = new RealVector(y);

        var directions = new RealVector(objectives.Select(d => d switch
        {
            ObjectiveDirection.Minimize => +1.0,
            ObjectiveDirection.Maximize => -1.0,
            _ => throw new NotImplementedException()
        }));
        var directedWeights = weights * directions;

        var xSum = (xObjectiveVector * directedWeights).Sum();
        var ySum = (yObjectiveVector * directedWeights).Sum();

        return xSum.CompareTo(ySum);
    }
}
