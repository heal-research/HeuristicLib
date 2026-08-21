namespace HEAL.HeuristicLib.Problems.QuadraticAssignment;

public sealed class QuadraticAssignmentProblemData : IQuadraticAssignmentProblemData
{
    private readonly double[,] distances;
    private readonly double[,] flows;

    public QuadraticAssignmentProblemData(double[,] flows, double[,] distances)
    {
        var flowSize = ValidateSquare(flows, nameof(flows));
        var distanceSize = ValidateSquare(distances, nameof(distances));
        if (flowSize != distanceSize)
            throw new ArgumentException("Flows and distances must have the same size.", nameof(distances));

        this.flows = (double[,])flows.Clone();
        this.distances = (double[,])distances.Clone();
        Size = flowSize;
    }

    public int Size { get; }

    public double GetFlow(int facilityA, int facilityB) => flows[facilityA, facilityB];
    public double GetDistance(int locationA, int locationB) => distances[locationA, locationB];

    private static int ValidateSquare(double[,] matrix, string parameterName)
    {
        var rowCount = matrix.GetLength(0);
        var columnCount = matrix.GetLength(1);
        if (rowCount != columnCount)
            throw new ArgumentException("The matrix must be square.", parameterName);

        return rowCount;
    }
}
