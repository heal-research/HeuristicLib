using HEAL.HeuristicLib.Genotypes.Vectors;
using HEAL.HeuristicLib.Problems.TestFunctions.MetaFunctions;
using HEAL.HeuristicLib.Problems.TestFunctions.SingleObjectives;
using HEAL.HeuristicLib.Tests.Problems.TestFunctions.SingleObjectives;

namespace HEAL.HeuristicLib.Tests.Problems.TestFunctions.MetaFunctions;

public class ScaledGradientTestFunctionTests
{
  [Fact]
  public void EvaluateGradient_ShouldApplyChainRule()
  {
    var inner = new SphereFunction(2);
    var function = new ScaledGradientTestFunction(
      inputScaling: [2.0, 3.0],
      outputScaling: 5.0,
      inner: inner);

    var x = new RealVector(1.0, 2.0);

    var gradient = function.EvaluateGradient(x);

    // scaled input = (2, 6)
    // grad inner there = (4, 12)
    // multiply by inputScaling and outputScaling:
    // (4,12) * (2,3) * 5 = (40, 180)
    Assert.Equal(40.0, gradient[0], 12);
    Assert.Equal(180.0, gradient[1], 12);
  }

  [Fact]
  public void EvaluateGradient_ShouldMatchFiniteDifferences()
  {
    var inner = new SphereFunction(2);
    var function = new ScaledGradientTestFunction(
      inputScaling: [2.0, 3.0],
      outputScaling: 5.0,
      inner: inner);

    var x = new RealVector(0.3, -0.4);

    SingleObjectiveTestFunctionHelper.AssertGradientMatchesFiniteDifferences(function, x);
  }
}
