using HEAL.HeuristicLib.Genotypes.Vectors;
using HEAL.HeuristicLib.Problems.TestFunctions.MetaFunctions;
using HEAL.HeuristicLib.Problems.TestFunctions.SingleObjectives;
using Xunit;

namespace HEAL.HeuristicLib.Tests.Problems.TestFunctions.MetaFunctions;

public class ScaledTestFunctionTests
{
  [Fact]
  public void Evaluate_ShouldApplyInputAndOutputScaling()
  {
    var inner = new SphereFunction(2);
    var function = new ScaledTestFunction(
      inputScaling: [2.0, 3.0],
      outputScaling: 5.0,
      inner: inner);

    var x = new RealVector(1.0, 2.0);

    var result = function.Evaluate(x);

    // inner on (2, 6): 4 + 36 = 40
    // output scaling: 40 * 5 = 200
    Assert.Equal(200.0, result, 12);
  }
}
