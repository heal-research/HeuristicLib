using HEAL.HeuristicLib.MachineLearning;
using HEAL.HeuristicLib.Objectives;

namespace HEAL.HeuristicLib.Problems.MachineLearning.Legacy;

public class MeanLogErrorEvaluator : RegressionEvaluator
{
    public override ObjectiveDirection Direction => ObjectiveDirection.Minimize;

    public override double Evaluate(IEnumerable<double> predictedValues, IEnumerable<double> trueValues)
    {
        return EvaluateMetric(Metrics.MeanLogError, predictedValues, trueValues);
    }
}
