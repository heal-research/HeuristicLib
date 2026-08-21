using HEAL.HeuristicLib.Objectives;

namespace HEAL.HeuristicLib.Problems.MachineLearning.Legacy;

public interface IRegressionEvaluator<in TModel>
{
    ObjectiveDirection Direction { get; }

    double Evaluate(TModel model, IEnumerable<double> predictedValues, IEnumerable<double> trueValues);
}
