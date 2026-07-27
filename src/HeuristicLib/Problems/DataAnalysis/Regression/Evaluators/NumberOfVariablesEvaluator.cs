using HEAL.HeuristicLib.Genotypes.Trees;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.SearchSpaces.Trees.SymbolicExpressionTree.Symbols.Math;

namespace HEAL.HeuristicLib.Problems.DataAnalysis.Regression.Legacy.Evaluators;

public class NumberOfVariablesEvaluator : IRegressionEvaluator<SymbolicExpressionTree>
{
    public ObjectiveDirection Direction => ObjectiveDirection.Minimize;

    public double Evaluate(SymbolicExpressionTree solution, IEnumerable<double> predictedValues, IEnumerable<double> trueValues) => solution.IterateNodesPostfix().OfType<VariableTreeNode>().Count();
}
