using HEAL.HeuristicLib.Optimization;

namespace HEAL.HeuristicLib.Problems.DataAnalysis.Regression;

public interface IRegressionMetric
{
    ObjectiveDirection Direction { get; }

    double Evaluate(ReadOnlySpan<double> predictedValues, ReadOnlySpan<double> targetValues);
}
