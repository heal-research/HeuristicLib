using HEAL.HeuristicLib.Encodings.RealVectors;
using HEAL.HeuristicLib.Objectives;

namespace HEAL.HeuristicLib.Problems.TestFunctions;

public interface ITestFunction
{
    int Dimension { get; }
    double Min { get; }
    double Max { get; }
    ObjectiveDirection Objective { get; }
    double Evaluate(RealVector solution);
}

public static class TestFunctionExtensions
{
    extension(ITestFunction inner)
    {
        public ITestFunction Shifted(RealVector shiftVector) => new ShiftedTestFunction(shiftVector, inner);

        public ITestFunction Rotated(double[,] rotationMatrix) => new RotatedTestFunction(rotationMatrix, inner);

        public ITestFunction Scaled(double[] inputScaling, double outputScaling) => new ScaledTestFunction(inputScaling, outputScaling, inner);
    }
}
