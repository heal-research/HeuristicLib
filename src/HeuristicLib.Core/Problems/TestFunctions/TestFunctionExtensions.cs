using HEAL.HeuristicLib.Genotypes.Vectors;
using HEAL.HeuristicLib.Problems.TestFunctions.MetaFunctions;

#pragma warning disable S2368
#pragma warning disable S2325

namespace HEAL.HeuristicLib.Problems.TestFunctions;

public static class TestFunctionExtensions {
  extension(ITestFunction inner) {
    public ITestFunction Shifted(RealVector shiftVector) => new ShiftedTestFunction(shiftVector, inner);
    public ITestFunction Rotated(double[,] rotationMatrix) => new RotatedTestFunction(rotationMatrix, inner);
  }
}
