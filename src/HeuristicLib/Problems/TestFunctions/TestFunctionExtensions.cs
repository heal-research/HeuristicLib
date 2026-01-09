using HEAL.HeuristicLib.Encodings.RealVector;
using HEAL.HeuristicLib.Problems.TestFunctions.MetaFunctions;

#pragma warning disable S2368
#pragma warning disable S2325

namespace HEAL.HeuristicLib.Problems.TestFunctions;

public static class TestFunctionExtensions {
  extension(ITestFunction inner) {
    public ITestFunction Shifted(RealVector shiftVector) => new ShiftedTestFunction(shiftVector, inner);
    public ITestFunction Rotated(double[,] rotationMatrix) => new RotatedTestFunction(rotationMatrix, inner);
    public ITestFunction Scaled(double[] inputScaling, double outputScaling) => new ScaledTestFunction(inputScaling, outputScaling, inner);
  }

  extension(IGradientTestFunction inner) {
    public IGradientTestFunction Shifted(RealVector shiftVector) => new ShiftedGradientTestFunction(shiftVector, inner);
    public ITestFunction Rotated(double[,] rotationMatrix) => new RotatedTestFunction(rotationMatrix, inner); //TODO: Gradient version
    public ITestFunction Scaled(double[] inputScaling, double outputScaling) => new ScaledTestFunction(inputScaling, outputScaling, inner); //TODO: Gradient version
  }
}
