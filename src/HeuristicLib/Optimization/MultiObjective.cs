namespace HEAL.HeuristicLib.Optimization;

public static class MultiObjective {
  public static Objective Minimize(int size) => new(Enumerable.Repeat(ObjectiveDirection.Minimize, size).ToArray(), NoTotalOrderComparer.Instance);
  public static Objective Maximize(int size) => new(Enumerable.Repeat(ObjectiveDirection.Maximize, size).ToArray(), NoTotalOrderComparer.Instance);

  public static Objective Create(params ObjectiveDirection[] directions) => new(directions, NoTotalOrderComparer.Instance);
  public static Objective Create(params bool[] maximization) => new(maximization.Select(x => x ? ObjectiveDirection.Maximize : ObjectiveDirection.Minimize).ToArray(), NoTotalOrderComparer.Instance);

  public static Objective WeightedSum(ObjectiveDirection[] directions, double[]? weights) => new(directions, new WeightedSumComparer(directions, weights));
  public static Objective Lexicographic(ObjectiveDirection[] directions, int[]? order) => new(directions, new LexicographicComparer(directions, order));

  public static Objective WithWithWeightedSum(this Objective objectives, double[] weights) => WeightedSum(objectives.Directions, weights);
  public static Objective WithLexicographicOrder(this Objective objectives, int[] order) => Lexicographic(objectives.Directions, order);
}
