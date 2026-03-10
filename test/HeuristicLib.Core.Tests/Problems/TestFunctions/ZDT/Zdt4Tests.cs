using HEAL.HeuristicLib.Genotypes.Vectors;
using HEAL.HeuristicLib.Problems.TestFunctions.ZDT;

namespace HEAL.HeuristicLib.Tests.Problems.TestFunctions.ZDT;

public class Zdt4Tests
{
  [Fact]
  public void Constructor_ShouldThrow_WhenDimensionIsLessThanTwo()
  {
    Assert.Throws<ArgumentOutOfRangeException>(() => new Zdt4(1));
  }

  [Fact]
  public void Evaluate_ShouldReturnExpectedValue_ForAllZeroVector()
  {
    var problem = new Zdt4(4);
    var x = new RealVector(0.0, 0.0, 0.0, 0.0);

    var result = problem.Evaluate(x);

    Assert.Equal(0.0, result[0], 12);
    Assert.Equal(1.0, result[1], 12);
  }

  [Fact]
  public void Evaluate_ShouldReturnExpectedValue_ForKnownVector()
  {
    var problem = new Zdt4(4);
    var x = new RealVector(0.25, 0.5, 0.5, 0.5);

    var result = problem.Evaluate(x);

    var f1 = 0.25;
    var gTail =
      (0.5 * 0.5 - 10.0 * Math.Cos(4.0 * Math.PI * 0.5)) +
      (0.5 * 0.5 - 10.0 * Math.Cos(4.0 * Math.PI * 0.5)) +
      (0.5 * 0.5 - 10.0 * Math.Cos(4.0 * Math.PI * 0.5));
    var g = 1.0 + 10.0 * 3.0 + gTail;
    var f2 = g * (1.0 - Math.Sqrt(f1 / g));

    Assert.Equal(f1, result[0], 12);
    Assert.Equal(f2, result[1], 12);
  }

  [Fact]
  public void EvaluateGradient_ShouldMatchFiniteDifferences()
  {
    var problem = new Zdt4(4);
    var x = new RealVector(0.3, 0.4, 0.5, 0.6);

    ZdtTestHelper.AssertGradientMatchesFiniteDifferences(problem, x, tolerance: 1e-4);
  }
}
