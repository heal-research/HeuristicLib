namespace HEAL.HeuristicLib.Problems.DataAnalysis.OnlineCalculators.DependencyCalculator;

public interface IDependencyCalculator {
  double Maximum { get; }
  double Minimum { get; }
  string Name { get; }

  double Calculate(IEnumerable<double> originalValues, IEnumerable<double> estimatedValues, out OnlineCalculatorError errorState);
  double Calculate(IEnumerable<Tuple<double, double>> values, out OnlineCalculatorError errorState);
}
