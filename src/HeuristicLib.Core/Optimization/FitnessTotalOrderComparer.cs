namespace HEAL.HeuristicLib.Optimization;

public static class FitnessTotalOrderComparer {
  public static SingleObjectiveComparer CreateSingleObjectiveComparer(ObjectiveDirection objectiveDirection) {
    return new SingleObjectiveComparer(objectiveDirection);
  }

  public static WeightedSumComparer CreateWeightedSumComparer(ObjectiveDirection[] objectives, double[]? weights = null) {
    return new WeightedSumComparer(objectives, weights);
  }

  public static LexicographicComparer CreateLexicographicComparer(ObjectiveDirection[] objectives, int[]? order = null) {
    return new LexicographicComparer(objectives, order);
  }

  public static NoTotalOrderComparer CreateNoTotalOrderComparer(ObjectiveDirection[] objectives) {
    return NoTotalOrderComparer.Instance;
  }
}
