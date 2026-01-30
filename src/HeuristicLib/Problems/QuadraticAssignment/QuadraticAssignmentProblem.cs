using HEAL.HeuristicLib.Encodings.Permutation;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Problems.QuadraticAssignment;

public class QuadraticAssignmentProblem(IQuadraticAssignmentProblemData problemData)
  : PermutationProblem(SingleObjective.Minimize, new PermutationEncoding(problemData.Size)) {
  public IQuadraticAssignmentProblemData ProblemData { get; } = problemData;

  public override ObjectiveVector Evaluate(Permutation solution, IRandomNumberGenerator random) {
    // solution[facility] = location
    int n = ProblemData.Size;
    double cost = 0.0;
    for (int i = 0; i < n; i++) {
      int li = solution[i];
      for (int j = 0; j < n; j++) {
        int lj = solution[j];
        cost += ProblemData.GetFlow(i, j) * ProblemData.GetDistance(li, lj);
      }
    }

    return cost;
  }

  #region Default Instance
  public static QuadraticAssignmentProblem CreateDefault() {
    var flows = new double[,] { { 0, 2, 0, 1 }, { 2, 0, 3, 0 }, { 0, 3, 0, 4 }, { 1, 0, 4, 0 } };
    var distances = new double[,] { { 0, 1, 2, 3 }, { 1, 0, 1, 2 }, { 2, 1, 0, 1 }, { 3, 2, 1, 0 } };
    return new QuadraticAssignmentProblem(new QuadraticAssignmentData(flows, distances));
  }
  #endregion
}
