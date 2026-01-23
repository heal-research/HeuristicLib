namespace HEAL.HeuristicLib.Problems.DataAnalysis.OnlineCalculators.DependencyCalculator;

public class PearsonsRSquaredDependenceCalculator : IDependencyCalculator
{
  public double Maximum => 1.0;

  public double Minimum => 0.0;

  public string Name => "Pearsons R Squared";

  public double Calculate(IEnumerable<double> originalValues, IEnumerable<double> estimatedValues, out OnlineCalculatorError errorState)
  {
    var r = OnlinePearsonsRCalculator.Calculate(originalValues, estimatedValues, out errorState);
    return r * r;
  }

  public double Calculate(IEnumerable<Tuple<double, double>> values, out OnlineCalculatorError errorState)
  {
    var calculator = new OnlinePearsonsRCalculator();
    foreach (var tuple in values) {
      calculator.Add(tuple.Item1, tuple.Item2);
      if (calculator.ErrorState != OnlineCalculatorError.None) {
        break;
      }
    }

    errorState = calculator.ErrorState;
    var r = calculator.R;
    return r * r;
  }
}
