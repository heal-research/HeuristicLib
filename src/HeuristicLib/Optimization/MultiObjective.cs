namespace HEAL.HeuristicLib.Optimization;

public static class MultiObjective {
  public static Objective Create(ObjectiveDirection[] directions) => new Objective(directions, new NoTotalOrderComparer());

  public static Objective WeightedSum(ObjectiveDirection[] directions, double[]? weights) => new(directions, new WeightedSumComparer(directions, weights));
  public static Objective Lexicographic(ObjectiveDirection[] directions, int[]? order) => new(directions, new LexicographicComparer(directions, order));

  public static Objective WithWithWeightedSum(this Objective objectives, double[] weights) => WeightedSum(objectives.Directions, weights);
  public static Objective WithLexicographicOrder(this Objective objectives, int[] order) => Lexicographic(objectives.Directions, order);
}
