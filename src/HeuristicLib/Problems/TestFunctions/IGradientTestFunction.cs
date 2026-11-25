using HEAL.HeuristicLib.Encodings.RealVectors;

namespace HEAL.HeuristicLib.Problems.TestFunctions;

public interface IGradientTestFunction : ITestFunction
{
    RealVector EvaluateGradient(RealVector solution);
}

public static class GradientTestFunctionExtensions
{
    extension(IGradientTestFunction inner)
    {
        public IGradientTestFunction Shifted(RealVector shiftVector) => new ShiftedGradientTestFunction(shiftVector, inner);

        public IGradientTestFunction Rotated(double[,] rotationMatrix) => new RotatedGradientTestFunction(rotationMatrix, inner);

        public IGradientTestFunction Scaled(double[] inputScaling, double outputScaling) => new ScaledGradientTestFunction(inputScaling, outputScaling, inner);
    }
}
