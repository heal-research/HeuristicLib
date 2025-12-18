using HEAL.HeuristicLib.Encodings.RealVector;

namespace HEAL.HeuristicLib.Problems.TestFunctions.MetaFunctions;

public class RotatedTestFunction : MetaTestFunction {
  private readonly double[,] rotation;
  public override int Dimension => rotation.GetLength(0);

  public RotatedTestFunction(double[,] rotation, ITestFunction inner) : base(inner) => this.rotation = rotation;

  public override double Evaluate(RealVector solution) => Inner.Evaluate(Rotate(rotation, solution));

  private static void Rotate(double[,] r, IReadOnlyList<double> v, double[] result) {
    var nRows = r.GetLength(0);
    var nCols = r.GetLength(1);

    if (v.Count != nCols)
      throw new ArgumentException("Vector length must match matrix column count.", nameof(v));
    if (result.Length != nRows)
      throw new ArgumentException("Result length must match matrix row count.", nameof(result));

    for (var i = 0; i < nRows; i++) {
      var sum = 0.0;
      for (var j = 0; j < nCols; j++) {
        sum += r[i, j] * v[j];
      }

      result[i] = sum;
    }
  }

  private static double[] Rotate(double[,] r, IReadOnlyList<double> v) {
    var result = new double[r.GetLength(0)];
    Rotate(r, v, result);
    return result;
  }
}
