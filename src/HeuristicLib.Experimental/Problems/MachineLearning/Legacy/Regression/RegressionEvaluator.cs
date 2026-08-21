using HEAL.HeuristicLib.MachineLearning;
using HEAL.HeuristicLib.Objectives;

namespace HEAL.HeuristicLib.Problems.MachineLearning.Legacy;

public abstract class RegressionEvaluator : IRegressionEvaluator<object>
{
    public abstract ObjectiveDirection Direction { get; }
    public double Evaluate(object model, IEnumerable<double> predictedValues, IEnumerable<double> trueValues) => Evaluate(predictedValues, trueValues);
    public abstract double Evaluate(IEnumerable<double> predictedValues, IEnumerable<double> trueValues);

    protected static double EvaluateMetric(IRegressionMetric metric, IEnumerable<double> predictedValues, IEnumerable<double> trueValues)
    {
        var predictions = predictedValues as double[] ?? predictedValues.ToArray();
        var targets = trueValues as double[] ?? trueValues.ToArray();
        return metric.Evaluate(predictions, targets);
    }
}
