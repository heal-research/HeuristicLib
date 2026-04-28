using HEAL.HeuristicLib.Genotypes.Vectors;
using HEAL.HeuristicLib.Problems.TestFunctions.ZDT;

namespace HEAL.HeuristicLib.Tests.Problems.TestFunctions.ZDT;

internal static class ZdtTestHelper
{
  public static void AssertVectorApproximatelyEqual(RealVector expected, RealVector actual, double tolerance = 1e-6)
  {
    Assert.Equal(expected.Count, actual.Count);
    for (var i = 0; i < expected.Count; i++) {
      Assert.True(
        Math.Abs(expected[i] - actual[i]) <= tolerance,
        $"Vectors differ at index {i}: expected {expected[i]}, actual {actual[i]}");
    }
  }

  public static RealVector NumericalGradient(Func<RealVector, double> f, RealVector x, double epsilon = 1e-7)
  {
    var grad = new double[x.Count];

    for (var i = 0; i < x.Count; i++) {
      var plus = x.ToArray();
      var minus = x.ToArray();

      plus[i] += epsilon;
      minus[i] -= epsilon;

      grad[i] = (f(plus) - f(minus)) / (2.0 * epsilon);
    }

    return grad;
  }

  public static void AssertGradientMatchesFiniteDifferences(
    Zdt problem,
    RealVector point,
    double tolerance = 1e-5,
    double epsilon = 1e-7)
  {
    var gradients = problem.EvaluateGradient(point);

    Assert.Equal(2, gradients.Length);

    var numericalF1 = NumericalGradient(x => problem.Evaluate(x)[0], point, epsilon);
    var numericalF2 = NumericalGradient(x => problem.Evaluate(x)[1], point, epsilon);

    AssertVectorApproximatelyEqual(numericalF1, gradients[0], tolerance);
    AssertVectorApproximatelyEqual(numericalF2, gradients[1], tolerance);
  }
}
