using HEAL.HeuristicLib.Genotypes.Vectors;
using HEAL.HeuristicLib.Problems.TestFunctions.SingleObjectives;

namespace HEAL.HeuristicLib.Tests.Problems.TestFunctions.SingleObjectives;

public class AckleyFunctionTests
{
  [Fact]
  public void Evaluate_ShouldReturnZero_AtOrigin()
  {
    var f = new AckleyFunction(3);
    var x = new RealVector(0.0, 0.0, 0.0);

    Assert.Equal(0.0, f.Evaluate(x), 12);
  }

  [Fact]
  public void EvaluateGradient_ShouldReturnZero_AtOrigin()
  {
    var f = new AckleyFunction(3);
    var x = new RealVector(0.0, 0.0, 0.0);

    var grad = f.EvaluateGradient(x);

    Assert.Equal(0.0, grad[0], 12);
    Assert.Equal(0.0, grad[1], 12);
    Assert.Equal(0.0, grad[2], 12);
  }

  [Fact]
  public void Evaluate_ShouldReturnExpectedValue_ForKnownPoint()
  {
    var f = new AckleyFunction(2);
    var x = new RealVector(1.0, 1.0);

    var sumSquares = (1.0 * 1.0 + 1.0 * 1.0) / 2.0;
    var rootMeanSquare = Math.Sqrt(sumSquares);
    var meanCos = (Math.Cos(2.0 * Math.PI * 1.0) + Math.Cos(2.0 * Math.PI * 1.0)) / 2.0;

    var expected = -20.0 * Math.Exp(-0.2 * rootMeanSquare) - Math.Exp(meanCos) + 20.0 + Math.E;

    Assert.Equal(expected, f.Evaluate(x), 12);
  }

  [Fact]
  public void EvaluateGradient_ShouldMatchFiniteDifferences_AwayFromOrigin()
  {
    var f = new AckleyFunction(3);
    var x = new RealVector(0.3, -0.4, 0.5);

    SingleObjectiveTestFunctionHelper.AssertGradientMatchesFiniteDifferences(f, x, tolerance: 1e-4);
  }
}
