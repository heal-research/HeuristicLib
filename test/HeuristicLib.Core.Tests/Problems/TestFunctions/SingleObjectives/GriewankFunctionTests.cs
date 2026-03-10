using HEAL.HeuristicLib.Genotypes.Vectors;
using HEAL.HeuristicLib.Problems.TestFunctions.SingleObjectives;

namespace HEAL.HeuristicLib.Tests.Problems.TestFunctions.SingleObjectives;

public class GriewankFunctionTests
{
  [Fact]
  public void Evaluate_ShouldReturnZero_AtOrigin()
  {
    var f = new GriewankFunction(3);
    var x = new RealVector(0.0, 0.0, 0.0);

    Assert.Equal(0.0, f.Evaluate(x), 12);
  }

  [Fact]
  public void Evaluate_ShouldReturnExpectedValue_ForKnownPoint()
  {
    var f = new GriewankFunction(1);
    var x = new RealVector(2.0);

    var expected = (4.0 / 4000.0) - Math.Cos(2.0) + 1.0;
    Assert.Equal(expected, f.Evaluate(x), 12);
  }

  [Fact]
  public void EvaluateGradient_ShouldBeZero_AtOrigin()
  {
    var f = new GriewankFunction(3);
    var x = new RealVector(0.0, 0.0, 0.0);

    var grad = f.EvaluateGradient(x);

    Assert.Equal(0.0, grad[0], 12);
    Assert.Equal(0.0, grad[1], 12);
    Assert.Equal(0.0, grad[2], 12);
  }

  [Fact]
  public void EvaluateGradient_ShouldMatchFiniteDifferences()
  {
    var f = new GriewankFunction(3);
    var x = new RealVector(0.3, -0.4, 0.5);

    SingleObjectiveTestFunctionHelper.AssertGradientMatchesFiniteDifferences(f, x, tolerance: 1e-5);
  }
}
