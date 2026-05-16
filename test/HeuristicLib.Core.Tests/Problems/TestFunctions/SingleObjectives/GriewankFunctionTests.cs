using HEAL.HeuristicLib.Genotypes.Vectors;
using HEAL.HeuristicLib.Problems.TestFunctions.SingleObjectives;

namespace HEAL.HeuristicLib.Tests.Problems.TestFunctions.SingleObjectives;

public class GriewankFunctionTests
{
  [Fact]
  public void Evaluate_ShouldReturnZero_AtOrigin()
  {
    var f = new GriewankFunction(3);
    RealVector x = [0.0, 0.0, 0.0];

    f.Evaluate(x).ShouldBe(0.0, 1e-12);
  }

  [Fact]
  public void Evaluate_ShouldReturnExpectedValue_ForKnownPoint()
  {
    var f = new GriewankFunction(1);
    RealVector x = [2.0];

    var expected = (4.0 / 4000.0) - Math.Cos(2.0) + 1.0;
    f.Evaluate(x).ShouldBe(expected, 1e-12);
  }

  [Fact]
  public void EvaluateGradient_ShouldBeZero_AtOrigin()
  {
    var f = new GriewankFunction(3);
    RealVector x = [0.0, 0.0, 0.0];

    var grad = f.EvaluateGradient(x);

    grad[0].ShouldBe(0.0, 1e-12);
    grad[1].ShouldBe(0.0, 1e-12);
    grad[2].ShouldBe(0.0, 1e-12);
  }

  [Fact]
  public void EvaluateGradient_ShouldMatchFiniteDifferences()
  {
    var f = new GriewankFunction(3);
    RealVector x = [0.3, -0.4, 0.5];

    SingleObjectiveTestFunctionHelper.AssertGradientMatchesFiniteDifferences(f, x, tolerance: 1e-5);
  }
}
