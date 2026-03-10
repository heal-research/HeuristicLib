using System;
using HEAL.HeuristicLib.Genotypes.Vectors;
using HEAL.HeuristicLib.Problems.TestFunctions.MetaFunctions;
using HEAL.HeuristicLib.Problems.TestFunctions.SingleObjectives;
using HEAL.HeuristicLib.Tests.Problems.TestFunctions.SingleObjectives;
using Xunit;

namespace HEAL.HeuristicLib.Tests.Problems.TestFunctions.MetaFunctions;

public class RotatedGradientTestFunctionTests
{
  [Fact]
  public void EvaluateGradient_ShouldReturnExpectedGradient_ForOrthogonalRotationAndSphere()
  {
    var rotation = new double[,] {
      { 0, -1 },
      { 1, 0 }
    };
    var inner = new SphereFunction(2);
    var function = new RotatedGradientTestFunction(rotation, inner);
    var x = new RealVector(3.0, 4.0);

    var gradient = function.EvaluateGradient(x);

    // Rx = (-4, 3)
    // grad inner at Rx = 2 * (-4, 3) = (-8, 6)
    // result = R^T * grad = [0 1; -1 0] * (-8, 6) = (6, 8)
    Assert.Equal(6.0, gradient[0], 12);
    Assert.Equal(8.0, gradient[1], 12);
  }

  [Fact]
  public void EvaluateGradient_ShouldMatchFiniteDifferences()
  {
    var rotation = new double[,] {
      { 0.8, -0.6 },
      { 0.6, 0.8 }
    };
    var inner = new SphereFunction(2);
    var function = new RotatedGradientTestFunction(rotation, inner);
    var x = new RealVector(0.3, -0.4);

    SingleObjectiveTestFunctionHelper.AssertGradientMatchesFiniteDifferences(function, x);
  }
}
