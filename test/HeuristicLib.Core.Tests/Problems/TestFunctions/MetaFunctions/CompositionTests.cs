using HEAL.HeuristicLib.Genotypes.Vectors;
using HEAL.HeuristicLib.Problems.TestFunctions;
using HEAL.HeuristicLib.Problems.TestFunctions.MetaFunctions;
using HEAL.HeuristicLib.Problems.TestFunctions.SingleObjectives;
using HEAL.HeuristicLib.Tests.Problems.TestFunctions.SingleObjectives;

namespace HEAL.HeuristicLib.Tests.Problems.TestFunctions.MetaFunctions;

public class CompositionTests
{
  [Fact]
  public void Decorators_ShouldComposeCorrectly()
  {
    IGradientTestFunction inner = new SphereFunction(2);
    inner = new ShiftedGradientTestFunction(new RealVector(1.0, 2.0), inner);
    inner = new ScaledGradientTestFunction([2.0, 3.0], 5.0, inner);

    var x = new RealVector(1.0, 1.0);

    var value = inner.Evaluate(x);
    var gradient = inner.EvaluateGradient(x);

    Assert.True(value > 0);
    SingleObjectiveTestFunctionHelper.AssertGradientMatchesFiniteDifferences(inner, x);
  }
}
