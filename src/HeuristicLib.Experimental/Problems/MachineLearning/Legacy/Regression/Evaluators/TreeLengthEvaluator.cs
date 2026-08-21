using HEAL.HeuristicLib.Encodings.LegacySymbolicExpressions;
using HEAL.HeuristicLib.Objectives;

namespace HEAL.HeuristicLib.Problems.MachineLearning.Legacy;

public class TreeLengthEvaluator : IRegressionEvaluator<SymbolicExpressionTree>
{
    public ObjectiveDirection Direction => ObjectiveDirection.Minimize;

    public double Evaluate(SymbolicExpressionTree model, IEnumerable<double> predictedValues, IEnumerable<double> trueValues) => model.Length;
}
