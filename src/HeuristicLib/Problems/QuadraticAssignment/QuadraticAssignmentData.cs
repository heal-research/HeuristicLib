namespace HEAL.HeuristicLib.Problems.QuadraticAssignment;

public sealed class QuadraticAssignmentData(double[,] flows, double[,] distances) : IQuadraticAssignmentProblemData {
  public int Size { get; } = flows.GetLength(0);

  public readonly double[,] Flows = ValidateSquare(flows, nameof(flows));
  public readonly double[,] Distances = ValidateSquare(distances, nameof(distances));

  public double GetFlow(int facilityA, int facilityB) => Flows[facilityA, facilityB];
  public double GetDistance(int locationA, int locationB) => Distances[locationA, locationB];

  private static double[,] ValidateSquare(double[,] m, string name) {
    int n0 = m.GetLength(0);
    int n1 = m.GetLength(1);
    return n0 != n1 ? throw new ArgumentException($"{name} must be square.", name) : m;
  }
}
