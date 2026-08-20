using HEAL.HeuristicLib.Genotypes.Trees;
using HEAL.HeuristicLib.Optimization;

namespace HEAL.HeuristicLib.Problems.DataAnalysis.Regression.Legacy.Evaluators;

public class TreeComplexityEvaluator : IRegressionEvaluator<SymbolicExpressionTree>
{
    public ObjectiveDirection Direction => ObjectiveDirection.Minimize;

    public double Evaluate(SymbolicExpressionTree model, IEnumerable<double> predictedValues, IEnumerable<double> trueValues) => TreeComplexityCalculator.CalculateComplexity(model);
}
