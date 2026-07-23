namespace HEAL.HeuristicLib.Optimization;

public static class MultiObjective
{
    public static ObjectiveDirections Minimize(int size) => Create(Enumerable.Repeat(ObjectiveDirection.Minimize, size).ToArray());
    public static ObjectiveDirections Maximize(int size) => Create(Enumerable.Repeat(ObjectiveDirection.Maximize, size).ToArray());
    public static ObjectiveDirections Create(params bool[] maximization) => Create(maximization.Select(x => x ? ObjectiveDirection.Maximize : ObjectiveDirection.Minimize).ToArray());
    public static ObjectiveDirections Create(params ObjectiveDirection[] directions) => new(directions, NoTotalOrderComparer.Instance);

    public static ObjectiveDirections WeightedSum(ObjectiveDirection[] directions, double[]? weights) => new(directions, new WeightedSumComparer(directions, weights));
    public static ObjectiveDirections Lexicographic(ObjectiveDirection[] directions, int[]? order) => new(directions, new LexicographicComparer(directions, order));

    extension(ObjectiveDirections objectives)
    {
        public ObjectiveDirections WithWithWeightedSum(double[] weights) => WeightedSum(objectives.Directions, weights);
        public ObjectiveDirections WithLexicographicOrder(int[] order) => Lexicographic(objectives.Directions, order);
    }
}
