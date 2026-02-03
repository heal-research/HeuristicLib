namespace HEAL.HeuristicLib.Problems.QuadraticAssignment;

public interface IQuadraticAssignmentProblemData
{
  int Size { get; }
  double GetFlow(int facilityA, int facilityB);
  double GetDistance(int locationA, int locationB);
}
