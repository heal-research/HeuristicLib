using HEAL.HeuristicLib.DataAnalysis.Regression;
using HEAL.HeuristicLib.Optimization;

namespace HEAL.HeuristicLib.Problems.DataAnalysis.Regression.Legacy.Evaluators;

public class PearsonR2Evaluator : RegressionEvaluator
{
    public override ObjectiveDirection Direction => ObjectiveDirection.Maximize;

    public override double Evaluate(IEnumerable<double> predictedValues, IEnumerable<double> trueValues)
    {
        return EvaluateMetric(Metrics.PearsonR2, predictedValues, trueValues);
    }
}
