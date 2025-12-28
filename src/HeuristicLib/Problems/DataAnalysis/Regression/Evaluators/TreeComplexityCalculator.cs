using HEAL.HeuristicLib.Encodings.Trees.SymbolicExpressionTree.Symbols;
using HEAL.HeuristicLib.Encodings.Trees.SymbolicExpressionTree.Symbols.Math;
using HEAL.HeuristicLib.Encodings.Trees.SymbolicExpressionTree.Symbols.Math.Variables;
using HEAL.HeuristicLib.Genotypes.Trees;

namespace HEAL.HeuristicLib.Problems.DataAnalysis.Regression.Evaluators;

public static class TreeComplexityCalculator {
  public static double CalculateComplexity(SymbolicExpressionTree tree) => CalculateComplexity(tree.Root);

  public static double CalculateComplexity(SymbolicExpressionTreeNode treeNode) {
    double complexity;

    switch (treeNode.Symbol) {
      case ProgramRootSymbol:
      case StartSymbol:
        return CalculateComplexity(treeNode[0]);
      case Number: // fall through
      case Constant:
        return 1;
      case Variable:
      case BinaryFactorVariable:
      case FactorVariable:
        return 2;
      case Addition:
      case Subtraction:
        return treeNode.Subtrees.Sum(CalculateComplexity);
      case Multiplication:
      case Division:
        return treeNode.Subtrees.Aggregate(1.0, (p, x) => p * (1 + CalculateComplexity(x)));
      case Sine:
      case Cosine:
      case Tangent:
      case Exponential:
      case Logarithm:
        return Math.Pow(2.0, CalculateComplexity(treeNode[0]));
      case Square:
        complexity = CalculateComplexity(treeNode[0]);
        return complexity * complexity;
      case SquareRoot:
        complexity = CalculateComplexity(treeNode[0]);
        return complexity * complexity * complexity;
      case Power:
      case Root:
        complexity = CalculateComplexity(treeNode[0]);
        if (treeNode[1] is not NumberTreeNode exponent)
          return Math.Pow(complexity, 2 * CalculateComplexity(treeNode[1]));

        var expVal = exponent.Value;
        if (expVal < 0)
          expVal = Math.Abs(expVal);
        if (expVal < 1)
          expVal = 1 / expVal;
        return Math.Pow(complexity, Math.Round(expVal));

      default:
        throw new NotSupportedException();
    }
  }
}
