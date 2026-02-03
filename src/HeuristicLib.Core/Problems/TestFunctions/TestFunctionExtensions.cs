using HEAL.HeuristicLib.Genotypes.Vectors;
using HEAL.HeuristicLib.Problems.TestFunctions.MetaFunctions;

#pragma warning disable S2368

namespace HEAL.HeuristicLib.Problems.TestFunctions;

public static class TestFunctionExtensions
{
  public static ITestFunction Shifted(this ITestFunction inner, RealVector shiftVector) => new ShiftedTestFunction(shiftVector, inner);
  public static IGradientTestFunction Shifted(this IGradientTestFunction inner, RealVector shiftVector) => new ShiftedGradientTestFunction(shiftVector, inner);

  public static ITestFunction Rotated(this ITestFunction inner, double[,] rotationMatrix) => new RotatedTestFunction(rotationMatrix, inner);
  public static IGradientTestFunction Rotated(this IGradientTestFunction inner, double[,] rotationMatrix) => new RotatedGradientTestFunction(rotationMatrix, inner);

  public static ITestFunction Scaled(this ITestFunction inner, double[] inputScaling, double outputScaling) => new ScaledTestFunction(inputScaling, outputScaling, inner);
  public static IGradientTestFunction Scaled(this IGradientTestFunction inner, double[] inputScaling, double outputScaling) => new ScaledGradientTestFunction(inputScaling, outputScaling, inner);//TODO: Gradient version
}
