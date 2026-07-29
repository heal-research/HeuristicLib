using HEAL.HeuristicLib.Optimization;

namespace HEAL.HeuristicLib.Problems.DataAnalysis.Regression;

public interface IRegressionEvaluator<in TModel>
{
    ObjectiveDirection Direction { get; }

    double Evaluate(TModel model, IEnumerable<double> predictedValues, IEnumerable<double> trueValues);
}
