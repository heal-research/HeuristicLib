namespace HEAL.HeuristicLib.Problems.QuadraticAssignment;

public sealed class QapData
{
    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public int Dimension { get; set; }

    public double[,] Distances { get; set; } = null!;

    public double[,] Flows { get; set; } = null!;

    public int[]? BestKnownAssignment { get; set; }

    public double? BestKnownQuality { get; set; }

    public QuadraticAssignmentProblemData ToProblemData()
    {
        return new QuadraticAssignmentProblemData(Flows, Distances);
    }
}
