using HEAL.HeuristicLib.Genotypes.Vectors;
using HEAL.HeuristicLib.Problems.TestFunctions.SingleObjectives;

namespace HEAL.HeuristicLib.Tests.Problems.TestFunctions.SingleObjectives;

public class SphereFunctionTests
{
  [Fact]
  public void Evaluate_ShouldReturnZero_AtOrigin()
  {
    var f = new SphereFunction(3);
    var x = new RealVector(0.0, 0.0, 0.0);

    Assert.Equal(0.0, f.Evaluate(x), 12);
  }

  [Fact]
  public void Evaluate_ShouldReturnExpectedValue_ForKnownPoint()
  {
    var f = new SphereFunction(3);
    var x = new RealVector(1.0, -2.0, 3.0);

    Assert.Equal(14.0, f.Evaluate(x), 12);
  }

  [Fact]
  public void EvaluateGradient_ShouldReturnExpectedValue_ForKnownPoint()
  {
    var f = new SphereFunction(3);
    var x = new RealVector(1.0, -2.0, 3.0);

    var grad = f.EvaluateGradient(x);

    Assert.Equal(2.0, grad[0], 12);
    Assert.Equal(-4.0, grad[1], 12);
    Assert.Equal(6.0, grad[2], 12);
  }

  [Fact]
  public void EvaluateGradient_ShouldMatchFiniteDifferences()
  {
    var f = new SphereFunction(3);
    var x = new RealVector(0.3, -0.4, 0.5);

    SingleObjectiveTestFunctionHelper.AssertGradientMatchesFiniteDifferences(f, x);
  }
}
