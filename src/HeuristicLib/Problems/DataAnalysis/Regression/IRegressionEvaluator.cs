using HEAL.HeuristicLib.Optimization;

namespace HEAL.HeuristicLib.Problems.DataAnalysis.Regression;

public interface IRegressionEvaluator<in TCandidate>
{
    ObjectiveDirection Direction { get; }

    double Evaluate(TCandidate solution, IEnumerable<double> predictedValues, IEnumerable<double> trueValues);
}
