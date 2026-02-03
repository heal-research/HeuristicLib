using HEAL.HeuristicLib.Encodings.Permutation;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems.QuadraticAssignment;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Problems.Dynamic.QuadraticAssignment;

public sealed class NoisyFlowQuadraticAssignmentProblem
  : DynamicProblem<Permutation, PermutationSearchSpace> {
  private readonly QuadraticAssignmentProblemData baseProblemData;
  private readonly double sigma;

  private readonly double[,] noisyFlows; // current state

  public NoisyFlowQuadraticAssignmentProblem(
    QuadraticAssignmentProblemData problemData,
    IRandomNumberGenerator environmentRandom,
    double sigma,
    UpdatePolicy updatePolicy = UpdatePolicy.AfterEvaluation,
    int epochLength = int.MaxValue
  ) : base(environmentRandom, updatePolicy, epochLength) {
    ArgumentOutOfRangeException.ThrowIfNegative(sigma);

    baseProblemData = problemData;
    this.sigma = sigma;

    Objective = SingleObjective.Minimize;
    SearchSpace = new PermutationSearchSpace(problemData.Size);

    noisyFlows = (double[,])baseProblemData.Flows.Clone();
    Update(); // initialize state (or call RebuildNoisyFlows() directly)
  }

  public override PermutationSearchSpace SearchSpace { get; }
  public override Objective Objective { get; }

  public override ObjectiveVector Evaluate(Permutation solution, IRandomNumberGenerator random, EvaluationTiming timing) {
    int n = baseProblemData.Size;
    double cost = 0.0;

    for (int i = 0; i < n; i++) {
      int li = solution[i];
      for (int j = 0; j < n; j++) {
        int lj = solution[j];
        cost += noisyFlows[i, j] * baseProblemData.Distances[li, lj];
      }
    }

    return cost;
  }

  protected override void Update() {
    // fresh noise each update (non-cumulative)
    int n = baseProblemData.Size;
    var f0 = baseProblemData.Flows;

    for (int i = 0; i < n; i++) {
      for (int j = 0; j < n; j++) {
        noisyFlows[i, j] = f0[i, j] + EnvironmentRandom.NextGaussian(0, sigma);
      }
    }
  }
}
