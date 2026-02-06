using HEAL.HeuristicLib.Genotypes.Vectors;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems.Dynamic.MovingPeaks;

namespace HEAL.HeuristicLib.Extensions.Tests.Dynamic;

public class MovingPeaksTests
{
  public static readonly MovingPeaksParameters Parameters = new(
  2,
  2,
  0,
  100,
  0,
  100,
  0,
  10,
  10,
  5,
  1
  );

  private static readonly (double[] center, double height, double width)[] Peaks = [
    (center: [10, 10], height: 50.0, width: 1.0),
    (center: [90, 90], height: 30.0, width: 1.0)
  ];

  [Fact]
  public void Evaluate_AtPeakCenter_EqualsPeakHeight()
  {
    var rng = new SystemRandomNumberGenerator(0);
    var p = new MovingPeaksProblem(Parameters, rng, Peaks);

    var x = new RealVector(10.0, 10.0);
    var fx = p.Evaluate(x, NoRandomGenerator.Instance)[0];

    Assert.Equal(50.0, fx, 10);
  }

  [Fact]
  public void Evaluate_UsesMaximumOverPeaks()
  {
    var rng = new SystemRandomNumberGenerator(0);
    var p = new MovingPeaksProblem(Parameters, rng, Peaks);

    // At (10,10) peak1 gives 50, peak2 gives 30 - 1*sqrt(80^2+80^2) which is negative
    var x = new RealVector(10.0, 10.0);
    var fx = p.Evaluate(x, NoRandomGenerator.Instance)[0];

    Assert.Equal(50.0, fx, 10);
  }

  [Fact]
  public void Evaluate_FarAway_DecreasesWithDistance()
  {
    var rng = new SystemRandomNumberGenerator(0);
    var p = new MovingPeaksProblem(Parameters, rng, Peaks);

    // Near the 50-peak
    var near = p.Evaluate(new RealVector(10.0, 10.0), NoRandomGenerator.Instance)[0];
    // Far from both peaks (roughly center-ish but far from 10,10 and 90,90)
    var far = p.Evaluate(new RealVector(50.0, 50.0), NoRandomGenerator.Instance)[0];

    Assert.True(far < near);
  }

  [Fact]
  public void Evaluate_DoesNotUseRandomGenerator()
  {
    var rng = new SystemRandomNumberGenerator(0);
    var p = new MovingPeaksProblem(Parameters, rng, Peaks);

    // If Evaluate touches RNG, this test should throw
    var x = new RealVector(12.0, 12.0);
    _ = p.Evaluate(x, NoRandomGenerator.Instance)[0];
  }

  [Fact]
  public void Update_WithZeroSeverities_DoesNotChangeFitness()
  {
    var rng = new SystemRandomNumberGenerator(0);
    var staticParams = Parameters with {
      ShiftSeverity = 0,
      HeightSeverity = 0,
      WidthSeverity = 0
    };
    var p = new MovingPeaksProblem(staticParams, rng, Peaks);

    var x = new RealVector(33.0, 33.0);
    var before = p.Evaluate(x, NoRandomGenerator.Instance)[0];

    for (var i = 0; i < 5; i++) {
      p.UpdateOnce();
    }

    var after = p.Evaluate(x, NoRandomGenerator.Instance)[0];
    Assert.Equal(before, after, 12);
  }

  [Fact]
  public void Update_WithNonzeroSeverities_ChangesFitnessEventually()
  {
    var rng = new SystemRandomNumberGenerator(0);
    var p = new MovingPeaksProblem(Parameters, rng, Peaks);

    var x = new RealVector(33.0, 33.0);
    var before = p.Evaluate(x, NoRandomGenerator.Instance)[0];

    var changed = false;
    for (var i = 0; i < 50; i++) {
      p.UpdateOnce();
      var now = p.Evaluate(x, NoRandomGenerator.Instance)[0];
      if (now.IsAlmost(before, 1e-12)) {
        continue;
      }

      changed = true;

      break;
    }

    Assert.True(changed);
  }

  [Fact]
  public void Update_ClampsHeightsAndWidths_ToConfiguredRanges()
  {
    // This test assumes your implementation clamps heights/widths after update.
    // If you don't clamp, either remove this test or change it to assert "can exceed".
    var rng = new SystemRandomNumberGenerator(0);
    var p = new MovingPeaksProblem(Parameters, rng, Peaks);

    for (var i = 0; i < 200; i++) {
      p.UpdateOnce();

      // This assumes you expose current peaks as a read-only list.
      // If you don't, consider exposing them (super useful for debugging + tests).
      foreach (var pk in p.Peaks()) {
        Assert.InRange(pk.Height, Parameters.MinHeight, Parameters.MaxHeight);
        Assert.InRange(pk.Width, Parameters.MinWidth, Parameters.MaxWidth);
      }
    }
  }

  [Fact]
  public void Update_ShiftsCenters_ByAtMostShiftSeverity()
  {
    // Assumes: center movement step length is limited by ShiftSeverity,
    // and boundary handling can't increase it.
    var rng = new SystemRandomNumberGenerator(0);
    var p = new MovingPeaksProblem(Parameters, rng, Peaks);

    var before = p.Peaks().Select(pk => pk.Center.ToArray()).ToArray();

    p.UpdateOnce();

    var peaks = p.Peaks().ToArr();
    for (var i = 0; i < peaks.Length; i++) {
      var dist = Euclidean(before[i], peaks[i].Center);
      Assert.True(dist <= Parameters.ShiftSeverity + 1e-9);
    }
  }

  private static double Euclidean(double[] a, IReadOnlyList<double> b)
  {
    var s = a.Select((t, i) => t - b[i]).Sum(d => d * d);

    return Math.Sqrt(s);
  }
}
