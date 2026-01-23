namespace HEAL.HeuristicLib.Optimization;

public static class MultiObjective {
  public static Objective Create(ObjectiveDirection[] directions) => new Objective(directions, new NoTotalOrderComparer());

  public static Objective WeightedSum(ObjectiveDirection[] directions, double[]? weights) => new(directions, new WeightedSumComparer(directions, weights));
  public static Objective Lexicographic(ObjectiveDirection[] directions, int[]? order) => new(directions, new LexicographicComparer(directions, order));

  extension(Objective objectives) {
    public Objective WithWithWeightedSum(double[] weights) => WeightedSum(objectives.Directions, weights);
    public Objective WithLexicographicOrder(int[] order) => Lexicographic(objectives.Directions, order);
  }
}
