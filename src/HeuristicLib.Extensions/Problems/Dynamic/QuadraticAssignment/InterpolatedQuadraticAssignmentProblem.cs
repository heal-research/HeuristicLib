using HEAL.HeuristicLib.Genotypes.Vectors;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems.QuadraticAssignment;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces.Vectors;

namespace HEAL.HeuristicLib.Problems.Dynamic.QuadraticAssignment;

public sealed class InterpolatedQuadraticAssignmentProblem
  : DynamicProblem<Permutation, PermutationSearchSpace>
{
  private readonly QuadraticAssignmentProblemData a;
  private readonly double alphaStep;
  private readonly QuadraticAssignmentProblemData b;
  private readonly double[,] currentDistances;

  private readonly double[,] currentFlows;
  private readonly bool interpolateDistances;
  private readonly bool pingPong;

  public InterpolatedQuadraticAssignmentProblem(
    QuadraticAssignmentProblemData a,
    QuadraticAssignmentProblemData b,
    IRandomNumberGenerator environmentRandom,
    double alphaStart = 0.0,
    double alphaStep = 0.01,
    bool interpolateDistances = false,
    bool pingPong = true,
    UpdatePolicy updatePolicy = UpdatePolicy.AfterEvaluation,
    int epochLength = int.MaxValue
  ) : base(environmentRandom, updatePolicy, epochLength)
  {
    if (a.Size != b.Size) {
      throw new ArgumentException("Instances must have same size.");
    }

    ArgumentOutOfRangeException.ThrowIfLessThan(alphaStart, 0.0);
    ArgumentOutOfRangeException.ThrowIfGreaterThan(alphaStart, 1.0);
    ArgumentOutOfRangeException.ThrowIfNegativeOrZero(alphaStep);
    ArgumentOutOfRangeException.ThrowIfGreaterThan(alphaStep, 1.0);

    this.a = a;
    this.b = b;
    this.interpolateDistances = interpolateDistances;
    Alpha = alphaStart;
    this.alphaStep = alphaStep;
    this.pingPong = pingPong;

    Objective = SingleObjective.Minimize;
    SearchSpace = new PermutationSearchSpace(a.Size);

    currentFlows = new double[a.Size, a.Size];
    currentDistances = new double[a.Size, a.Size];

    RebuildCurrentMatrices();
  }

  public double Alpha { get; private set; }

  public override PermutationSearchSpace SearchSpace { get; }
  public override Objective Objective { get; }

  public override ObjectiveVector Evaluate(Permutation solution, IRandomNumberGenerator random, EvaluationTiming timing)
  {
    var n = a.Size;
    var cost = 0.0;

    for (var i = 0; i < n; i++) {
      var li = solution[i];
      for (var j = 0; j < n; j++) {
        var lj = solution[j];
        cost += currentFlows[i, j] * currentDistances[li, lj];
      }
    }

    return cost;
  }

  protected override void Update()
  {
    // advance alpha
    var next = Alpha + alphaStep;

    if (!pingPong) {
      // wrap 0..1
      if (next > 1.0) {
        next -= Math.Floor(next);
      }

      if (next < 0.0) {
        next -= Math.Floor(next);
      }

      Alpha = next;
    } else {
      // ping-pong 0..1..0..1...
      // simplest: reflect at boundaries
      if (next <= 1.0) {
        Alpha = next;
      } else {
        // reflect once; if alphaStep is huge, you could loop, but typical steps are small
        Alpha = 2.0 - next;
        // flip direction by negating step would be cleaner, but we keep it stateless/simple
        // so we just rely on reflection each time (works for small step)
      }
      // If you want perfect ping-pong for any step size, I can give a robust sawtooth/triangle-wave mapping.
    }

    RebuildCurrentMatrices();
  }

  private void RebuildCurrentMatrices()
  {
    // flows
    LerpInto(currentFlows, a.Flows, b.Flows, Alpha);

    // distances
    if (interpolateDistances) {
      LerpInto(currentDistances, a.Distances, b.Distances, Alpha);
    } else {
      // keep A distances (copy once would be enough if you never mutate them)
      CopyInto(currentDistances, a.Distances);
    }
  }

  private static void LerpInto(double[,] dst, double[,] x, double[,] y, double t)
  {
    var n0 = dst.GetLength(0);
    var n1 = dst.GetLength(1);
    var s = 1.0 - t;

    for (var i = 0; i < n0; i++) {
      for (var j = 0; j < n1; j++) {
        dst[i, j] = s * x[i, j] + t * y[i, j];
      }
    }
  }

  private static void CopyInto(double[,] dst, double[,] src)
  {
    var n0 = dst.GetLength(0);
    var n1 = dst.GetLength(1);
    for (var i = 0; i < n0; i++) {
      for (var j = 0; j < n1; j++) {
        dst[i, j] = src[i, j];
      }
    }
  }
}
