namespace HEAL.HeuristicLib.Optimization;

public static class MultiObjective
{
  public static Objective Minimize(int size) => Create(Enumerable.Repeat(ObjectiveDirection.Minimize, size).ToArray());
  public static Objective Maximize(int size) => Create(Enumerable.Repeat(ObjectiveDirection.Maximize, size).ToArray());
  public static Objective Create(params bool[] maximization) => Create(maximization.Select(x => x ? ObjectiveDirection.Maximize : ObjectiveDirection.Minimize).ToArray());
  public static Objective Create(params ObjectiveDirection[] directions) => new(directions, NoTotalOrderComparer.Instance);

  public static Objective WeightedSum(ObjectiveDirection[] directions, double[]? weights) => new(directions, new WeightedSumComparer(directions, weights));
  public static Objective Lexicographic(ObjectiveDirection[] directions, int[]? order) => new(directions, new LexicographicComparer(directions, order));

  extension(Objective objectives)
  {
    public Objective WithWithWeightedSum(double[] weights) => WeightedSum(objectives.Directions, weights);
    public Objective WithLexicographicOrder(int[] order) => Lexicographic(objectives.Directions, order);
  }
}
