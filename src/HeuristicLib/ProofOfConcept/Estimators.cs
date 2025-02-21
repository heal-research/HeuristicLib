namespace HEAL.HeuristicLib.ProofOfConcept;

public interface IRegressor
{
  IFittedRegressor Fit(double[][] x, double[] y);
}

public interface IFittedRegressor
{
  double[] Predict(double[][] x);
}

public abstract class RegressorBase
  : IRegressor
{
  public abstract IFittedRegressor Fit(double[][] x, double[] y);
}

public class LinearRegressor
  : RegressorBase
{
  public LinearRegressor(bool fitIntercept = true) {
    FitIntercept = fitIntercept;
  }
  
  public bool FitIntercept { get; }

  public override FittedLinearRegressor Fit(double[][] x, double[] y) {
    return FitIntercept switch {
      true => new FittedLinearRegressor([1.0, 2.0]),
      false => new FittedLinearRegressor([2.0])
    };
  }
}

public class FittedLinearRegressor : IFittedRegressor
{
  internal FittedLinearRegressor(double[] coefficients) {
    Coefficients = coefficients;
  }

  public double[] Coefficients { get; }

  public double[] Predict(double[][] x) {
    return new double[x.Length];
  }
}


public class EstimatorsExample
{
  void Execute() {
    double[][] x = [[1, 2], [2, 3], [3, 4]];
    double[] y = [3, 5, 7];

    var regressor = new LinearRegressor();
    
    var fittedRegressor = regressor.Fit(x, y);

    Console.WriteLine("Coefficients: " + string.Join(", ", fittedRegressor.Coefficients));
  }
}
