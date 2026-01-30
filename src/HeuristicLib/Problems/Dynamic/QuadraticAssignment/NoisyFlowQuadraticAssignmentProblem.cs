using HEAL.HeuristicLib.Encodings.Permutation;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems.QuadraticAssignment;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Problems.Dynamic.QuadraticAssignment;

public sealed class NoisyFlowQuadraticAssignmentProblem
  : DynamicProblem<Permutation, PermutationEncoding> {
  private readonly QuadraticAssignmentData baseData;
  private readonly double sigma;

  private double[,] noisyFlows; // current state

  public NoisyFlowQuadraticAssignmentProblem(
    QuadraticAssignmentData problemData,
    IRandomNumberGenerator environmentRandom,
    double sigma,
    UpdatePolicy updatePolicy = UpdatePolicy.AfterEvaluation,
    int epochLength = int.MaxValue
  ) : base(environmentRandom, updatePolicy, epochLength) {
    ArgumentOutOfRangeException.ThrowIfNegative(sigma);

    baseData = problemData;
    this.sigma = sigma;

    Objective = SingleObjective.Minimize;
    SearchSpace = new PermutationEncoding(problemData.Size);

    noisyFlows = (double[,])baseData.Flows.Clone();
    Update(); // initialize state (or call RebuildNoisyFlows() directly)
  }

  public override PermutationEncoding SearchSpace { get; }
  public override Objective Objective { get; }

  public override ObjectiveVector Evaluate(Permutation solution, IRandomNumberGenerator random, EvaluationTiming timing) {
    int n = baseData.Size;
    double cost = 0.0;

    for (int i = 0; i < n; i++) {
      int li = solution[i];
      for (int j = 0; j < n; j++) {
        int lj = solution[j];
        cost += noisyFlows[i, j] * baseData.Distances[li, lj];
      }
    }

    return cost;
  }

  protected override void Update() {
    // fresh noise each update (non-cumulative)
    int n = baseData.Size;
    var f0 = baseData.Flows;

    for (int i = 0; i < n; i++) {
      for (int j = 0; j < n; j++) {
        noisyFlows[i, j] = f0[i, j] + sigma * NextGaussian(EnvironmentRandom);
      }
    }
  }

  // Box–Muller; uses only Uniform(0,1) from your RNG
  private static double NextGaussian(IRandomNumberGenerator rng) {
    // protect log(0)
    double u1 = Math.Max(double.Epsilon, rng.NextDouble());
    double u2 = rng.Double();
    return Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2);
  }
}
