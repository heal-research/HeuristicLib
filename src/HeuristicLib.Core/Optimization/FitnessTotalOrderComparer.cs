namespace HEAL.HeuristicLib.Optimization;

public static class FitnessTotalOrderComparer
{
  public static SingleObjectiveComparer CreateSingleObjectiveComparer(ObjectiveDirection objectiveDirection) => new(objectiveDirection);

  public static WeightedSumComparer CreateWeightedSumComparer(ObjectiveDirection[] objectives, double[]? weights = null) => new(objectives, weights);

  public static LexicographicComparer CreateLexicographicComparer(ObjectiveDirection[] objectives, int[]? order = null) => new(objectives, order);

  public static NoTotalOrderComparer CreateNoTotalOrderComparer(ObjectiveDirection[] objectives) => new();
}
