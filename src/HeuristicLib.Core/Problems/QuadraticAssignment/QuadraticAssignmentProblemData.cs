namespace HEAL.HeuristicLib.Problems.QuadraticAssignment;

public sealed class QuadraticAssignmentProblemData(double[,] flows, double[,] distances) : IQuadraticAssignmentProblemData
{
  public readonly double[,] Distances = ValidateSquare(distances, nameof(distances));

  public readonly double[,] Flows = ValidateSquare(flows, nameof(flows));
  public int Size { get; } = flows.GetLength(0);

  public double GetFlow(int facilityA, int facilityB) => Flows[facilityA, facilityB];
  public double GetDistance(int locationA, int locationB) => Distances[locationA, locationB];

  private static double[,] ValidateSquare(double[,] m, string name)
  {
    var n0 = m.GetLength(0);
    var n1 = m.GetLength(1);

    return n0 != n1 ? throw new ArgumentException($"{name} must be square.", name) : m;
  }
}
