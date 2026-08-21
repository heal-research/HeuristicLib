using HEAL.HeuristicLib.Encodings.RealVectors;
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
          shiftVector: [1.0, 2.0],
          inner: inner);

        RealVector x = [3.0, 4.0];

        var gradient = function.EvaluateGradient(x);

        // shifted input = (4, 6), grad sphere = (8, 12)
        gradient[0].ShouldBe(8.0, 1e-12);
        gradient[1].ShouldBe(12.0, 1e-12);
    }

    [Fact]
    public void EvaluateGradient_ShouldMatchFiniteDifferences()
    {
        var inner = new SphereFunction(2);
        var function = new ShiftedGradientTestFunction(
          shiftVector: [1.0, -2.0],
          inner: inner);

        RealVector x = [0.3, -0.4];

        SingleObjectiveTestFunctionHelper.AssertGradientMatchesFiniteDifferences(function, x);
    }
}
