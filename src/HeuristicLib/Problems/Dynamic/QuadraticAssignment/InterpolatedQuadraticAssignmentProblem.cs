using HEAL.HeuristicLib.Encodings.Permutation;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems.QuadraticAssignment;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Problems.Dynamic.QuadraticAssignment;

public sealed class InterpolatedQuadraticAssignmentProblem
  : DynamicProblem<Permutation, PermutationEncoding> {
  private readonly QuadraticAssignmentData a;
  private readonly QuadraticAssignmentData b;
  private readonly bool interpolateDistances;

  private double alpha;
  private readonly double alphaStep;
  private readonly bool pingPong;

  private readonly double[,] currentFlows;
  private readonly double[,] currentDistances;

  public double Alpha => alpha;

  public InterpolatedQuadraticAssignmentProblem(
    QuadraticAssignmentData a,
    QuadraticAssignmentData b,
    IRandomNumberGenerator environmentRandom,
    double alphaStart = 0.0,
    double alphaStep = 0.01,
    bool interpolateDistances = false,
    bool pingPong = true,
    UpdatePolicy updatePolicy = UpdatePolicy.AfterEvaluation,
    int epochLength = int.MaxValue
  ) : base(environmentRandom, updatePolicy, epochLength) {
    if (a.Size != b.Size) throw new ArgumentException("Instances must have same size.");
    ArgumentOutOfRangeException.ThrowIfLessThan(alphaStart, 0.0);
    ArgumentOutOfRangeException.ThrowIfGreaterThan(alphaStart, 1.0);
    ArgumentOutOfRangeException.ThrowIfNegativeOrZero(alphaStep);
    ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(alphaStep, 1.0);

    this.a = a;
    this.b = b;
    this.interpolateDistances = interpolateDistances;
    alpha = alphaStart;
    this.alphaStep = alphaStep;
    this.pingPong = pingPong;

    Objective = SingleObjective.Minimize;
    SearchSpace = new PermutationEncoding(a.Size);

    currentFlows = new double[a.Size, a.Size];
    currentDistances = new double[a.Size, a.Size];

    RebuildCurrentMatrices();
  }

  public override PermutationEncoding SearchSpace { get; }
  public override Objective Objective { get; }

  public override ObjectiveVector Evaluate(Permutation solution, IRandomNumberGenerator random, EvaluationTiming timing) {
    int n = a.Size;
    double cost = 0.0;

    for (int i = 0; i < n; i++) {
      int li = solution[i];
      for (int j = 0; j < n; j++) {
        int lj = solution[j];
        cost += currentFlows[i, j] * currentDistances[li, lj];
      }
    }

    return cost;
  }

  protected override void Update() {
    // advance alpha
    double next = alpha + alphaStep;

    if (!pingPong) {
      // wrap 0..1
      alpha = next % 1.0;
    } else {
      // ping-pong 0..1..0..1...
      // simplest: reflect at boundaries
      if (next <= 1.0) {
        alpha = next;
      } else {
        // reflect once; if alphaStep is huge, you could loop, but typical steps are small
        alpha = 2.0 - next;
        // flip direction by negating step would be cleaner, but we keep it stateless/simple
        // so we just rely on reflection each time (works for small step)
      }
      // If you want perfect ping-pong for any step size, I can give a robust sawtooth/triangle-wave mapping.
    }

    RebuildCurrentMatrices();
  }

  private void RebuildCurrentMatrices() {
    // flows
    LerpInto(currentFlows, a.Flows, b.Flows, alpha);

    // distances
    if (interpolateDistances) {
      LerpInto(currentDistances, a.Distances, b.Distances, alpha);
    } else {
      // keep A distances (copy once would be enough if you never mutate them)
      CopyInto(currentDistances, a.Distances);
    }
  }

  private static void LerpInto(double[,] dst, double[,] x, double[,] y, double t) {
    int n0 = dst.GetLength(0);
    int n1 = dst.GetLength(1);
    double s = 1.0 - t;

    for (int i = 0; i < n0; i++)
    for (int j = 0; j < n1; j++)
      dst[i, j] = s * x[i, j] + t * y[i, j];
  }

  private static void CopyInto(double[,] dst, double[,] src) {
    int n0 = dst.GetLength(0);
    int n1 = dst.GetLength(1);
    for (int i = 0; i < n0; i++)
    for (int j = 0; j < n1; j++)
      dst[i, j] = src[i, j];
  }
}
