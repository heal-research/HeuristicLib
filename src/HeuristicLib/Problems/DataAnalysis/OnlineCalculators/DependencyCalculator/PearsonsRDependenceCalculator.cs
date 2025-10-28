namespace HEAL.HeuristicLib.Problems.DataAnalysis.OnlineCalculators.DependencyCalculator;

public class PearsonsRDependenceCalculator : IDependencyCalculator {
  public double Maximum => 1.0;

  public double Minimum => -1.0;

  public string Name => "Pearsons R";

  public double Calculate(IEnumerable<double> originalValues, IEnumerable<double> estimatedValues, out OnlineCalculatorError errorState) {
    return OnlinePearsonsRCalculator.Calculate(originalValues, estimatedValues, out errorState);
  }

  public double Calculate(IEnumerable<Tuple<double, double>> values, out OnlineCalculatorError errorState) {
    var calculator = new OnlinePearsonsRCalculator();
    foreach (var tuple in values) {
      calculator.Add(tuple.Item1, tuple.Item2);
      if (calculator.ErrorState != OnlineCalculatorError.None) break;
    }

    errorState = calculator.ErrorState;
    return calculator.R;
  }
}
