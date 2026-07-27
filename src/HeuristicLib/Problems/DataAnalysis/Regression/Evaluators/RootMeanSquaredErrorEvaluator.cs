using HEAL.HeuristicLib.DataAnalysis.Regression;
using HEAL.HeuristicLib.Optimization;

namespace HEAL.HeuristicLib.Problems.DataAnalysis.Regression.Legacy.Evaluators;

public class RootMeanSquaredErrorEvaluator : RegressionEvaluator
{
    public override ObjectiveDirection Direction => ObjectiveDirection.Minimize;

    public override double Evaluate(IEnumerable<double> predictedValues, IEnumerable<double> trueValues)
    {
        return EvaluateMetric(Metrics.RMSE, predictedValues, trueValues);
    }
}
