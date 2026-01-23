using HEAL.HeuristicLib.Genotypes.Vectors;

namespace HEAL.HeuristicLib.Optimization;

public class WeightedSumComparer : IComparer<ObjectiveVector>
{
  private readonly ObjectiveDirection[] objectives;
  private readonly RealVector weights;

  public WeightedSumComparer(ObjectiveDirection[] objectives, double[]? weights = null)
  {
    if (weights is not null && objectives.Length != weights.Length) {
      throw new ArgumentException("Objective and weights must have the same length");
    }

    this.objectives = objectives;
    this.weights = weights ?? RealVector.Repeat(1.0, this.objectives.Length);
  }

  public int Compare(ObjectiveVector? x, ObjectiveVector? y)
  {
    if ((x is not null && x.Count != objectives.Length) || (y is not null && y.Count != objectives.Length)) {
      throw new ArgumentException("Fitness must have the same length as the objective");
    }

    if (x is null && y is null) {
      return 0;
    }

    if (x is null) {
      return -1;
    }

    if (y is null) {
      return +1;
    }

    var xFitness = new RealVector(x);
    var yFitness = new RealVector(y);

    var directions = new RealVector(objectives.Select(d => d switch {
      ObjectiveDirection.Minimize => +1.0,
      ObjectiveDirection.Maximize => -1.0,
      _ => throw new NotImplementedException()
    }));
    var directedWeights = weights * directions;

    var xSum = (xFitness * directedWeights).Sum();
    var ySum = (yFitness * directedWeights).Sum();

    return xSum.CompareTo(ySum);
  }
}
