namespace HEAL.HeuristicLib.Optimization;

public static class MultiObjective
{
  public static Objective Minimize(int size) => Create(Enumerable.Repeat(ObjectiveDirection.Minimize, size).ToArray());
  public static Objective Maximize(int size) => Create(Enumerable.Repeat(ObjectiveDirection.Maximize, size).ToArray());
  public static Objective Create(params bool[] maximization) => Create(maximization.Select(x => x ? ObjectiveDirection.Maximize : ObjectiveDirection.Minimize).ToArray());
  public static Objective Create(params ObjectiveDirection[] directions) => new(directions, NoTotalOrderComparer.Instance);

  public static Objective WeightedSum(ObjectiveDirection[] directions, double[]? weights) => new(directions, new WeightedSumComparer(directions, weights));
  public static Objective Lexicographic(ObjectiveDirection[] directions, int[]? order) => new(directions, new LexicographicComparer(directions, order));

  public static Objective WithWeightedSum(this Objective objectives, double[]? weights = null) => WeightedSum(objectives.Directions, weights);
  public static Objective WithLexicographicOrder(this Objective objectives, int[]? order = null) => Lexicographic(objectives.Directions, order);
}
