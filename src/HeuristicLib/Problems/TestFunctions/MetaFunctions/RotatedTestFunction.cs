using HEAL.HeuristicLib.Encodings.RealVector;

#pragma warning disable S2368

namespace HEAL.HeuristicLib.Problems.TestFunctions.MetaFunctions;

public class RotatedTestFunction(double[,] rotation, ITestFunction inner) : MetaTestFunction(inner) {
  protected readonly double[,] Rotation = rotation;
  public override int Dimension => Rotation.GetLength(0);

  public override double Evaluate(RealVector solution) => Inner.Evaluate(Rotate(Rotation, solution));

  public static double[] Rotate(double[,] r, IReadOnlyList<double> v) {
    var result = new double[r.GetLength(0)];
    var nRows = r.GetLength(0);
    var nCols = r.GetLength(1);

    ArgumentOutOfRangeException.ThrowIfNotEqual(v.Count, nCols);
    ArgumentOutOfRangeException.ThrowIfNotEqual(result.Length, nRows);

    for (var i = 0; i < nRows; i++) {
      var sum = 0.0;
      for (var j = 0; j < nCols; j++) {
        sum += r[i, j] * v[j];
      }

      result[i] = sum;
    }

    return result;
  }
}

public class RotatedGradientTestFunction(double[,] rotation, IGradientTestFunction inner) : RotatedTestFunction(rotation, inner), IGradientTestFunction {
  protected readonly IGradientTestFunction GradientInner = inner;

  public RealVector EvaluateGradient(RealVector solution) {
    var rotatedSolution = Rotate(Rotation, solution);
    var gradInner = GradientInner.EvaluateGradient(rotatedSolution);
    var nCols = Rotation.GetLength(1);
    var rotatedGrad = new double[nCols];
    var nRows = Rotation.GetLength(0);
    for (var j = 0; j < nCols; j++) {
      var sum = 0.0;
      for (var i = 0; i < nRows; i++)
        sum += Rotation[i, j] * gradInner[i];
      rotatedGrad[j] = sum;
    }

    return new RealVector(rotatedGrad);
  }
}
