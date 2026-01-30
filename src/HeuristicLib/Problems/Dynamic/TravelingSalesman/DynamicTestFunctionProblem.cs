using HEAL.HeuristicLib.Encodings.RealVector;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems.TestFunctions.MetaFunctions;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.Random.Distributions;

namespace HEAL.HeuristicLib.Problems.Dynamic.TravelingSalesman;

public class DynamicTestFunctionProblem : DynamicProblem<RealVector, RealVectorEncoding> {
  private readonly IProblem<RealVector, RealVectorEncoding> problem;

  public record State(double[] Shift, double[,] Rotation, double[] InputScaling, double OutputScaling);

  public record DeviationSigmas(double ShiftStrength, double RotationStrength, double InputScalingStrength, double OutputScalingStrength);

  public DynamicTestFunctionProblem(IRandomNumberGenerator environmentRandom,
                                    IProblem<RealVector, RealVectorEncoding> problem,
                                    UpdatePolicy updatePolicy = UpdatePolicy.AfterEvaluation,
                                    int epochLength = int.MaxValue) : base(environmentRandom, updatePolicy: updatePolicy, epochLength: epochLength) {
    this.problem = problem;
    var rot = new double[problem.SearchSpace.Length, problem.SearchSpace.Length];
    var shift = new double[problem.SearchSpace.Length];
    var inputScaling = new double[problem.SearchSpace.Length];
    for (int i = 0; i < problem.SearchSpace.Length; i++) {
      rot[i, i] = 1.0;
      inputScaling[i] = 1.0;
    }

    CurrentState = new State(shift, rot, inputScaling, 1);
  }

  public State CurrentState { get; private set; }
  public required DeviationSigmas DeviationSigma { get; init; }

  public override RealVectorEncoding SearchSpace => problem.SearchSpace;
  public override Objective Objective => problem.Objective;

  public override ObjectiveVector Evaluate(RealVector solution, IRandomNumberGenerator random, EvaluationTiming timing) {
    solution = RotatedTestFunction.Rotate(CurrentState.Rotation, solution);
    solution *= CurrentState.InputScaling;
    solution += CurrentState.Shift;
    var res = problem.Evaluate(solution, random);
    var currentStateOutputScaling = CurrentState.OutputScaling;
    return new ObjectiveVector(res.Select(x => x * currentStateOutputScaling));
  }

  protected override void Update() {
    var shift = CurrentState.Shift.Select(x => x + EnvironmentRandom.NextGaussian(sigma: DeviationSigma.ShiftStrength)).ToArray();
    var rot = RandomRotationMatrix(CurrentState.Rotation, EnvironmentRandom, EnvironmentRandom.NextGaussian(sigma: DeviationSigma.RotationStrength));
    var inScale = CurrentState.InputScaling.Select(x => x + EnvironmentRandom.NextGaussian(sigma: DeviationSigma.InputScalingStrength)).ToArray();
    var outScale = CurrentState.OutputScaling + EnvironmentRandom.NextGaussian(sigma: DeviationSigma.OutputScalingStrength);
    CurrentState = new State(shift, rot, inScale, outScale);

    //Note: the scaling factors could become zero or negative, which may lead to degenerate situations.
    //This is intentional to increase the dynamics of the problem.
  }

  static double[] RandomUnitVector(int n, IRandomNumberGenerator rng) {
    double[] v = new double[n];
    double norm = 0.0;

    for (int i = 0; i < n; i++) {
      v[i] = rng.NextGaussian();
      norm += v[i] * v[i];
    }

    norm = Math.Sqrt(norm);
    for (int i = 0; i < n; i++)
      v[i] /= norm;

    return v;
  }

  static double[] Orthogonalize(double[] v, double[] against) {
    double dot = 0.0;
    for (int i = 0; i < v.Length; i++)
      dot += v[i] * against[i];

    for (int i = 0; i < v.Length; i++)
      v[i] -= dot * against[i];

    return v;
  }

  static double[,] Multiply(double[,] a, double[,] b) {
    int n = a.GetLength(0);
    double[,] c = new double[n, n];
    for (int i = 0; i < n; i++) {
      for (int j = 0; j < n; j++) {
        for (int k = 0; k < n; k++)
          c[i, j] += a[i, k] * b[k, j];
      }
    }

    return c;
  }

  static double[,] RandomRotationMatrix(double[,] rot, IRandomNumberGenerator rng, double theta) {
    var n = rot.GetLength(0);

    double[] u, v;
    double norm;
    do {
      norm = 0;
      u = RandomUnitVector(n, rng);
      v = RandomUnitVector(n, rng);
      v = Orthogonalize(v, u);
      for (int i = 0; i < n; i++)
        norm += v[i] * v[i];
      norm = Math.Sqrt(norm);
    } while (norm < 1e-8);

    for (int i = 0; i < n; i++)
      v[i] /= norm;

    double cos = Math.Cos(theta);
    double sin = Math.Sin(theta);

    double[,] p = new double[n, n];

    // Start with identity
    for (int i = 0; i < n; i++)
      p[i, i] = 1.0;

    // Apply plane rotation update
    for (int i = 0; i < n; i++) {
      for (int j = 0; j < n; j++) {
        p[i, j] += (cos - 1) * (u[i] * u[j] + v[i] * v[j])
                   + sin * (v[i] * u[j] - u[i] * v[j]);
      }
    }

    return Multiply(p, rot);
  }
}
