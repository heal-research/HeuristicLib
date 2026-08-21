using HEAL.HeuristicLib.MachineLearning;
using HEAL.HeuristicLib.Objectives;

namespace HEAL.HeuristicLib.Problems.MachineLearning.Legacy;

public class NormalizedMeanSquaredErrorEvaluator : RegressionEvaluator
{
    public override ObjectiveDirection Direction => ObjectiveDirection.Minimize;

    public override double Evaluate(IEnumerable<double> predictedValues, IEnumerable<double> trueValues)
    {
        return EvaluateMetric(Metrics.NMSE, predictedValues, trueValues);
    }
}
