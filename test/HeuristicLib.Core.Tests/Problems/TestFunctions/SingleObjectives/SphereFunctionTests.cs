using HEAL.HeuristicLib.Genotypes.Vectors;
using HEAL.HeuristicLib.Problems.TestFunctions.SingleObjectives;

namespace HEAL.HeuristicLib.Tests.Problems.TestFunctions.SingleObjectives;

public class SphereFunctionTests
{
  [Fact]
  public void Evaluate_ShouldReturnZero_AtOrigin()
  {
    var f = new SphereFunction(3);
    RealVector x = [0.0, 0.0, 0.0];

    f.Evaluate(x).ShouldBe(0.0, 1e-12);
  }

  [Fact]
  public void Evaluate_ShouldReturnExpectedValue_ForKnownPoint()
  {
    var f = new SphereFunction(3);
    RealVector x = [1.0, -2.0, 3.0];

    f.Evaluate(x).ShouldBe(14.0, 1e-12);
  }

  [Fact]
  public void EvaluateGradient_ShouldReturnExpectedValue_ForKnownPoint()
  {
    var f = new SphereFunction(3);
    RealVector x = [1.0, -2.0, 3.0];

    var grad = f.EvaluateGradient(x);

    grad[0].ShouldBe(2.0, 1e-12);
    grad[1].ShouldBe(-4.0, 1e-12);
    grad[2].ShouldBe(6.0, 1e-12);
  }

  [Fact]
  public void EvaluateGradient_ShouldMatchFiniteDifferences()
  {
    var f = new SphereFunction(3);
    RealVector x = [0.3, -0.4, 0.5];

    SingleObjectiveTestFunctionHelper.AssertGradientMatchesFiniteDifferences(f, x);
  }
}
