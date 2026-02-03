using HEAL.HeuristicLib.Genotypes.Vectors;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces.Vectors;

namespace HEAL.HeuristicLib.Problems.Dynamic.MovingPeaks;

public sealed class MovingPeaksProblem
  : DynamicProblem<RealVector, RealVectorSearchSpace>
{
  private double[] peakHeights = null!;

  private double[][] peakPositions = null!;
  private double[] peakWidths = null!;

  public MovingPeaksProblem(MovingPeaksParameters parameters,
    IRandomNumberGenerator environmentRandom,
    UpdatePolicy updatePolicy = UpdatePolicy.AfterEvaluation,
    int epochLength = int.MaxValue)
    : base(environmentRandom, updatePolicy, epochLength)
  {
    Validate(parameters);
    Parameters = parameters;
    Objective = SingleObjective.Maximize;
    SearchSpace = new RealVectorSearchSpace(
    parameters.Dimension,
    parameters.LowerBound,
    parameters.UpperBound
    );

    InitializePeaks();
  }

  public MovingPeaksProblem(MovingPeaksParameters parameters,
    IRandomNumberGenerator environmentRandom,
    (double[] center, double height, double width)[] peaks,
    UpdatePolicy updatePolicy = UpdatePolicy.AfterEvaluation,
    int epochLength = int.MaxValue)
    : base(environmentRandom, updatePolicy, epochLength)
  {
    Validate(parameters);
    Parameters = parameters;
    Objective = SingleObjective.Maximize;
    SearchSpace = new RealVectorSearchSpace(
    parameters.Dimension,
    parameters.LowerBound,
    parameters.UpperBound
    );
    ArgumentOutOfRangeException.ThrowIfNotEqual(peaks.Length, parameters.NumberOfPeaks);
    peakPositions = new double[parameters.NumberOfPeaks][];
    peakHeights = new double[parameters.NumberOfPeaks];
    peakWidths = new double[parameters.NumberOfPeaks];
    for (var i = 0; i < parameters.NumberOfPeaks; i++) {
      var center = peaks[i].center;
      ArgumentOutOfRangeException.ThrowIfNotEqual(center.Length, parameters.Dimension);
      peakPositions[i] = center.ToArray();
      peakHeights[i] = peaks[i].height;
      peakWidths[i] = peaks[i].width;
    }
  }
  public MovingPeaksParameters Parameters { get; }

  public override RealVectorSearchSpace SearchSpace { get; }
  public override Objective Objective { get; }

  private static void Validate(MovingPeaksParameters p)
  {
    ArgumentOutOfRangeException.ThrowIfNegativeOrZero(p.Dimension);
    ArgumentOutOfRangeException.ThrowIfNegativeOrZero(p.NumberOfPeaks);
    ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(p.LowerBound, p.UpperBound);
    ArgumentOutOfRangeException.ThrowIfGreaterThan(p.MinHeight, p.MaxHeight);
    ArgumentOutOfRangeException.ThrowIfGreaterThan(p.MinWidth, p.MaxWidth);
    ArgumentOutOfRangeException.ThrowIfNegative(p.ShiftSeverity);
    ArgumentOutOfRangeException.ThrowIfNegative(p.HeightSeverity);
    ArgumentOutOfRangeException.ThrowIfNegative(p.WidthSeverity);
  }

  public IEnumerable<(double[] Center, double Height, double Width)> Peaks()
  {
    for (var i = 0; i < Parameters.NumberOfPeaks; i++) {
      yield return (peakPositions[i], peakHeights[i], peakWidths[i]);
    }
  }

  public override ObjectiveVector Evaluate(RealVector solution, IRandomNumberGenerator random, EvaluationTiming timing)
  {
    var best = double.NegativeInfinity;

    for (var i = 0; i < Parameters.NumberOfPeaks; i++) {
      var dist = EuclideanDistance(solution, peakPositions[i]);
      var value = peakHeights[i] - peakWidths[i] * dist;
      if (value > best) {
        best = value;
      }
    }

    return best;
  }

  protected override void Update()
  {
    // Move peaks
    for (var i = 0; i < Parameters.NumberOfPeaks; i++) {
      var dir = RandomUnitVector(Parameters.Dimension);
      for (var d = 0; d < Parameters.Dimension; d++) {
        peakPositions[i][d] = ReflectIntoBounds(
        peakPositions[i][d] + Parameters.ShiftSeverity * dir[d],
        Parameters.LowerBound,
        Parameters.UpperBound
        );
      }
    }

    // Change heights
    for (var i = 0; i < Parameters.NumberOfPeaks; i++) {
      var delta = RandomSigned(EnvironmentRandom) * Parameters.HeightSeverity;
      peakHeights[i] = Clamp(peakHeights[i] + delta, Parameters.MinHeight, Parameters.MaxHeight);
    }

    // Change widths
    for (var i = 0; i < Parameters.NumberOfPeaks; i++) {
      var delta = RandomSigned(EnvironmentRandom) * Parameters.WidthSeverity;
      peakWidths[i] = Clamp(peakWidths[i] + delta, Parameters.MinWidth, Parameters.MaxWidth);
    }
  }

  private void InitializePeaks()
  {
    peakPositions = new double[Parameters.NumberOfPeaks][];
    peakHeights = new double[Parameters.NumberOfPeaks];
    peakWidths = new double[Parameters.NumberOfPeaks];

    for (var i = 0; i < Parameters.NumberOfPeaks; i++) {
      peakPositions[i] = new double[Parameters.Dimension];
      for (var d = 0; d < Parameters.Dimension; d++) {
        peakPositions[i][d] = EnvironmentRandom.Double(Parameters.LowerBound, Parameters.UpperBound);
      }

      peakHeights[i] = EnvironmentRandom.Double(Parameters.MinHeight, Parameters.MaxHeight);
      peakWidths[i] = EnvironmentRandom.Double(Parameters.MinWidth, Parameters.MaxWidth);
    }
  }

  private static double EuclideanDistance(RealVector x, double[] p)
  {
    var sumSq = 0.0;
    for (var d = 0; d < p.Length; d++) {
      var diff = x[d] - p[d];
      sumSq += diff * diff;
    }

    return Math.Sqrt(sumSq);
  }

  private double[] RandomUnitVector(int dim)
  {
    var v = new double[dim];
    var normSq = 0.0;
    for (var i = 0; i < dim; i++) {
      var a = EnvironmentRandom.Double(-1.0, 1.0);
      v[i] = a;
      normSq += a * a;
    }

    var norm = Math.Sqrt(normSq);
    if (norm.IsAlmost(0.0, 1e-100)) {
      v[0] = 1;

      return v;
    }

    for (var i = 0; i < dim; i++) {
      v[i] /= norm;
    }

    return v;
  }

  private static double RandomSigned(IRandomNumberGenerator rng) =>
    rng.Boolean() ? 1.0 : -1.0;

  private static double Clamp(double x, double lo, double hi)
  {
    if (x < lo) {
      return lo;
    }

    if (x > hi) {
      return hi;
    }

    return x;
  }

  private static double ReflectIntoBounds(double x, double lo, double hi)
  {
    if (x < lo) {
      return lo + (lo - x);
    }

    if (x > hi) {
      return hi - (x - hi);
    }

    return x;
  }
}
