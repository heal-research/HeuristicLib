using HEAL.HeuristicLib.Genotypes.Vectors;
using HEAL.HeuristicLib.Problems.TestFunctions;

namespace HEAL.HeuristicLib.Tests.Problems.TestFunctions.SingleObjectives;

internal static class SingleObjectiveTestFunctionHelper
{
  public static RealVector NumericalGradient(
    Func<RealVector, double> f,
    RealVector x,
    double epsilon = 1e-7)
  {
    var grad = new double[x.Count];

    for (var i = 0; i < x.Count; i++) {
      var plus = x.ToArray();
      var minus = x.ToArray();

      plus[i] += epsilon;
      minus[i] -= epsilon;

      grad[i] = (f(RealVector.FromOwnedArray(plus)) - f(RealVector.FromOwnedArray(minus))) / (2.0 * epsilon);
    }

    return RealVector.FromOwnedArray(grad);
  }

  public static void AssertVectorApproximatelyEqual(
    RealVector expected,
    RealVector actual,
    double tolerance = 1e-5)
  {
    actual.Count.ShouldBe(expected.Count);

    for (var i = 0; i < expected.Count; i++) {
      (Math.Abs(expected[i] - actual[i]) <= tolerance).ShouldBeTrue($"Vectors differ at index {i}: expected {expected[i]}, actual {actual[i]}");
    }
  }

  public static void AssertGradientMatchesFiniteDifferences(
    IGradientTestFunction function,
    RealVector point,
    double tolerance = 1e-5,
    double epsilon = 1e-7)
  {
    var analytical = function.EvaluateGradient(point);
    var numerical = NumericalGradient(function.Evaluate, point, epsilon);

    AssertVectorApproximatelyEqual(numerical, analytical, tolerance);
  }
}
