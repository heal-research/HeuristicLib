using HEAL.HeuristicLib.Encodings.RealVector;
using HEAL.HeuristicLib.Optimization;

namespace HEAL.HeuristicLib.Problems.TestFunctions.MetaFunctions;

public class ShiftedTestFunction : MetaTestFunction {
  private readonly RealVector shiftVector;
  public ShiftedTestFunction(RealVector shiftVector, ITestFunction inner) : base(inner) => this.shiftVector = shiftVector;

  public override double Evaluate(RealVector solution) {
    var shiftedISolution = solution + shiftVector;
    return Inner.Evaluate(shiftedISolution);
  }
}

public abstract class MetaTestFunction : ITestFunction {
  protected readonly ITestFunction Inner;
  protected MetaTestFunction(ITestFunction inner) => Inner = inner;
  public virtual int Dimension => Inner.Dimension;
  public double Min => Inner.Min;
  public double Max => Inner.Max;
  public ObjectiveDirection Objective => Inner.Objective;

  public abstract double Evaluate(RealVector solution);
}

public class RotatedTestFunction : MetaTestFunction {
  private readonly double[,] rotation;
  public override int Dimension => rotation.GetLength(0);

  public RotatedTestFunction(double[,] rotation, ITestFunction inner) : base(inner) => this.rotation = rotation;

  public override double Evaluate(RealVector solution) => Inner.Evaluate(Rotate(rotation, solution));

  private static void Rotate(double[,] r, IReadOnlyList<double> v, double[] result) {
    int nRows = r.GetLength(0);
    int nCols = r.GetLength(1);

    if (v.Count != nCols)
      throw new ArgumentException("Vector length must match matrix column count.", nameof(v));
    if (result.Length != nRows)
      throw new ArgumentException("Result length must match matrix row count.", nameof(result));

    for (int i = 0; i < nRows; i++) {
      double sum = 0.0;
      for (int j = 0; j < nCols; j++) {
        sum += r[i, j] * v[j];
      }

      result[i] = sum;
    }
  }

  private static double[] Rotate(double[,] r, IReadOnlyList<double> v) {
    double[] result = new double[r.GetLength(0)];
    Rotate(r, v, result);
    return result;
  }
}
