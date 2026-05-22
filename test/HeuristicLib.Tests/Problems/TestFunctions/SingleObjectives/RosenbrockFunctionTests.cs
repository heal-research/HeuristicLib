using HEAL.HeuristicLib.Genotypes.Vectors;
using HEAL.HeuristicLib.Problems.TestFunctions.SingleObjectives;

namespace HEAL.HeuristicLib.Tests.Problems.TestFunctions.SingleObjectives;

public class RosenbrockFunctionTests
{
    [Fact]
    public void Evaluate_ShouldReturnZero_AtGlobalMinimum()
    {
        var f = new RosenbrockFunction(3);
        RealVector x = [1.0, 1.0, 1.0];

        f.Evaluate(x).ShouldBe(0.0, 1e-12);
    }

    [Fact]
    public void Evaluate_ShouldReturnExpectedValue_ForKnownPoint()
    {
        var f = new RosenbrockFunction(2);
        RealVector x = [0.0, 0.0];

        // 100*(0 - 0^2)^2 + (0 - 1)^2 = 1
        f.Evaluate(x).ShouldBe(1.0, 1e-12);
    }

    [Fact]
    public void EvaluateGradient_ShouldBeZero_AtGlobalMinimum()
    {
        var f = new RosenbrockFunction(3);
        RealVector x = [1.0, 1.0, 1.0];

        var grad = f.EvaluateGradient(x);

        grad[0].ShouldBe(0.0, 1e-12);
        grad[1].ShouldBe(0.0, 1e-12);
        grad[2].ShouldBe(0.0, 1e-12);
    }

    [Fact]
    public void EvaluateGradient_ShouldMatchFiniteDifferences()
    {
        var f = new RosenbrockFunction(3);
        RealVector x = [0.8, 1.2, 0.9];

        SingleObjectiveTestFunctionHelper.AssertGradientMatchesFiniteDifferences(f, x, tolerance: 1e-4);
    }
}
