using HEAL.HeuristicLib.MachineLearning;
using HEAL.HeuristicLib.Objectives;

namespace HEAL.HeuristicLib.Problems.MachineLearning.Legacy;

public class PearsonR2Evaluator : RegressionEvaluator
{
    public override ObjectiveDirection Direction => ObjectiveDirection.Maximize;

    public override double Evaluate(IEnumerable<double> predictedValues, IEnumerable<double> trueValues)
    {
        return EvaluateMetric(Metrics.PearsonR2, predictedValues, trueValues);
    }
}
