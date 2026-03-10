using HEAL.HeuristicLib.Genotypes.Vectors;
using HEAL.HeuristicLib.Problems.TestFunctions.MetaFunctions;
using HEAL.HeuristicLib.Problems.TestFunctions.SingleObjectives;

namespace HEAL.HeuristicLib.Tests.Problems.TestFunctions.MetaFunctions;

public class ShiftedTestFunctionTests
{
  [Fact]
  public void Evaluate_ShouldApplyShiftBeforeInnerEvaluation()
  {
    var inner = new SphereFunction(2);
    var function = new ShiftedTestFunction(
      shiftVector: new RealVector(1.0, 2.0),
      inner: inner);

    var x = new RealVector(3.0, 4.0);

    var result = function.Evaluate(x);

    // shifted input = (4, 6), sphere = 16 + 36 = 52
    Assert.Equal(52.0, result, 12);
  }
}
