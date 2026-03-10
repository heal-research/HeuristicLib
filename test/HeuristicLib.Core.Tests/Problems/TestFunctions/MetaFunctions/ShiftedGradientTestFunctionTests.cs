using HEAL.HeuristicLib.Genotypes.Vectors;
using HEAL.HeuristicLib.Problems.TestFunctions.MetaFunctions;
using HEAL.HeuristicLib.Problems.TestFunctions.SingleObjectives;
using HEAL.HeuristicLib.Tests.Problems.TestFunctions.SingleObjectives;

namespace HEAL.HeuristicLib.Tests.Problems.TestFunctions.MetaFunctions;

public class ShiftedGradientTestFunctionTests
{
  [Fact]
  public void EvaluateGradient_ShouldEvaluateInnerGradientAtShiftedPoint()
  {
    var inner = new SphereFunction(2);
    var function = new ShiftedGradientTestFunction(
      shiftVector: new RealVector(1.0, 2.0),
      inner: inner);

    var x = new RealVector(3.0, 4.0);

    var gradient = function.EvaluateGradient(x);

    // shifted input = (4, 6), grad sphere = (8, 12)
    Assert.Equal(8.0, gradient[0], 12);
    Assert.Equal(12.0, gradient[1], 12);
  }

  [Fact]
  public void EvaluateGradient_ShouldMatchFiniteDifferences()
  {
    var inner = new SphereFunction(2);
    var function = new ShiftedGradientTestFunction(
      shiftVector: new RealVector(1.0, -2.0),
      inner: inner);

    var x = new RealVector(0.3, -0.4);

    SingleObjectiveTestFunctionHelper.AssertGradientMatchesFiniteDifferences(function, x);
  }
}
