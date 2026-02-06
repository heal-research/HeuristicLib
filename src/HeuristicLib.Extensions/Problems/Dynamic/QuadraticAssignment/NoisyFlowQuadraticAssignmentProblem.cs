using HEAL.HeuristicLib.Genotypes.Vectors;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems.QuadraticAssignment;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.Random.Distributions;
using HEAL.HeuristicLib.SearchSpaces.Vectors;

namespace HEAL.HeuristicLib.Problems.Dynamic.QuadraticAssignment;

public sealed class NoisyFlowQuadraticAssignmentProblem
  : DynamicProblem<Permutation, PermutationSearchSpace>
{
  private readonly QuadraticAssignmentProblemData baseProblemData;

  private readonly double[,] noisyFlows; // current state
  private readonly double sigma;

  public NoisyFlowQuadraticAssignmentProblem(
    QuadraticAssignmentProblemData problemData,
    IRandomNumberGenerator environmentRandom,
    double sigma,
    UpdatePolicy updatePolicy = UpdatePolicy.AfterEvaluation,
    int epochLength = int.MaxValue
  ) : base(environmentRandom, updatePolicy, epochLength)
  {
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

  public override ObjectiveVector Evaluate(Permutation solution, IRandomNumberGenerator random, EvaluationTiming timing)
  {
    var n = baseProblemData.Size;
    var cost = 0.0;

    for (var i = 0; i < n; i++) {
      var li = solution[i];
      for (var j = 0; j < n; j++) {
        var lj = solution[j];
        cost += noisyFlows[i, j] * baseProblemData.Distances[li, lj];
      }
    }

    return cost;
  }

  protected override void Update()
  {
    // fresh noise each update (non-cumulative)
    var n = baseProblemData.Size;
    var f0 = baseProblemData.Flows;

    for (var i = 0; i < n; i++) {
      for (var j = 0; j < n; j++) {
        noisyFlows[i, j] = f0[i, j] + EnvironmentRandom.NextGaussian(0, sigma);
      }
    }
  }
}
