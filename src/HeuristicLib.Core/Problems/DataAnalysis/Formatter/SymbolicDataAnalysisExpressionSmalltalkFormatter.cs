using System.Globalization;
using System.Text;
using HEAL.HeuristicLib.Genotypes.Trees;
using HEAL.HeuristicLib.SearchSpaces.Trees.SymbolicExpressionTree.Formatters;
using HEAL.HeuristicLib.SearchSpaces.Trees.SymbolicExpressionTree.Symbols;
using HEAL.HeuristicLib.SearchSpaces.Trees.SymbolicExpressionTree.Symbols.Math;
using HEAL.HeuristicLib.SearchSpaces.Trees.SymbolicExpressionTree.Symbols.Math.Variables;

namespace HEAL.HeuristicLib.Problems.DataAnalysis.Formatter;

public class SymbolicDataAnalysisExpressionSmalltalkFormatter : ISymbolicExpressionTreeStringFormatter
{
  public string Format(SymbolicExpressionTree symbolicExpressionTree) => FormatRecursively(symbolicExpressionTree.Root);

  // returns the smalltalk expression corresponding to the node 
  // smalltalk expressions are always surrounded by parentheses "(<expr>)"
  private string FormatRecursively(SymbolicExpressionTreeNode node)
  {
    var symbol = node.Symbol;

    if (symbol is ProgramRootSymbol or StartSymbol) {
      return FormatRecursively(node.GetSubtree(0));
    }

    var stringBuilder = new StringBuilder(20);

    stringBuilder.Append("(");

    switch (symbol) {
      case Addition: {
        for (var i = 0; i < node.SubtreeCount; i++) {
          if (i > 0) {
            stringBuilder.Append("+");
          }
          stringBuilder.Append(FormatRecursively(node.GetSubtree(i)));
        }

        break;
      }
      case And: {
        stringBuilder.Append("(");
        for (var i = 0; i < node.SubtreeCount; i++) {
          if (i > 0) {
            stringBuilder.Append("&");
          }
          stringBuilder.Append("(");
          stringBuilder.Append(FormatRecursively(node.GetSubtree(i)));
          stringBuilder.Append(" > 0)");
        }

        stringBuilder.Append(") ifTrue:[1] ifFalse:[-1]");

        break;
      }
      case Absolute:
        stringBuilder.Append($"({FormatRecursively(node.GetSubtree(0))}) abs");

        break;
      case AnalyticQuotient:
        stringBuilder.Append($"({FormatRecursively(node.GetSubtree(0))}) / (1 + ({FormatPower(node.GetSubtree(1), "2")})) sqrt");

        break;
      case Average: {
        stringBuilder.Append("(1/");
        stringBuilder.Append(node.SubtreeCount);
        stringBuilder.Append(")*(");
        for (var i = 0; i < node.SubtreeCount; i++) {
          if (i > 0) {
            stringBuilder.Append("+");
          }
          stringBuilder.Append(FormatRecursively(node.GetSubtree(i)));
        }

        stringBuilder.Append(")");

        break;
      }
      default: {
        if (symbol is Number) {
          var numberTreeNode = (NumberTreeNode)node;
          stringBuilder.Append(numberTreeNode.Value.ToString(CultureInfo.InvariantCulture));
        } else if (symbol is Cosine) {
          stringBuilder.Append(FormatRecursively(node.GetSubtree(0)));
          stringBuilder.Append(" cos");
        } else if (symbol is Cube) {
          stringBuilder.Append(FormatPower(node.GetSubtree(0), "3"));
        } else if (symbol is CubeRoot) {
          stringBuilder.Append(FormatRecursively(node.GetSubtree(0)));
          stringBuilder.Append(" cbrt");
        } else if (symbol is Division) {
          if (node.SubtreeCount == 1) {
            stringBuilder.Append("1/");
            stringBuilder.Append(FormatRecursively(node.GetSubtree(0)));
          } else {
            stringBuilder.Append(FormatRecursively(node.GetSubtree(0)));
            stringBuilder.Append("/(");
            for (var i = 1; i < node.SubtreeCount; i++) {
              if (i > 1) {
                stringBuilder.Append('*');
              }
              stringBuilder.Append(FormatRecursively(node.GetSubtree(i)));
            }

            stringBuilder.Append(')');
          }
        } else if (symbol is Exponential) {
          stringBuilder.Append(FormatRecursively(node.GetSubtree(0)));
          stringBuilder.Append(" exp");
        } else if (symbol is GreaterThan) {
          stringBuilder.Append("(");
          stringBuilder.Append(FormatRecursively(node.GetSubtree(0)));
          stringBuilder.Append(" > ");
          stringBuilder.Append(FormatRecursively(node.GetSubtree(1)));
          stringBuilder.Append(") ifTrue: [1] ifFalse: [-1]");
        } else if (symbol is IfThenElse) {
          stringBuilder.Append("(");
          stringBuilder.Append(FormatRecursively(node.GetSubtree(0)));
          stringBuilder.Append(" > 0) ifTrue: [");
          stringBuilder.Append(FormatRecursively(node.GetSubtree(1)));
          stringBuilder.Append("] ifFalse: [");
          stringBuilder.Append(FormatRecursively(node.GetSubtree(2)));
          stringBuilder.Append("]");
        } else if (symbol is LaggedVariable) {
          stringBuilder.Append("lagged variables are not supported");
        } else if (symbol is LessThan) {
          stringBuilder.Append("(");
          stringBuilder.Append(FormatRecursively(node.GetSubtree(0)));
          stringBuilder.Append(" < ");
          stringBuilder.Append(FormatRecursively(node.GetSubtree(1)));
          stringBuilder.Append(") ifTrue: [1] ifFalse: [-1]");
        } else if (symbol is Logarithm) {
          stringBuilder.Append(FormatRecursively(node.GetSubtree(0)));
          stringBuilder.Append("ln");
        } else if (symbol is Multiplication) {
          for (var i = 0; i < node.SubtreeCount; i++) {
            if (i > 0) {
              stringBuilder.Append("*");
            }
            stringBuilder.Append(FormatRecursively(node.GetSubtree(i)));
          }
        } else if (symbol is Not) {
          stringBuilder.Append("(");
          stringBuilder.Append(FormatRecursively(node.GetSubtree(0)));
          stringBuilder.Append(">0) ifTrue: [-1] ifFalse: [1.0]");
        } else if (symbol is Or) {
          stringBuilder.Append("(");
          for (var i = 0; i < node.SubtreeCount; i++) {
            if (i > 0) {
              stringBuilder.Append("|");
            }
            stringBuilder.Append("(");
            stringBuilder.Append(FormatRecursively(node.GetSubtree(i)));
            stringBuilder.Append(">0)");
          }

          stringBuilder.Append(") ifTrue:[1] ifFalse:[-1]");
        } else if (symbol is Sine) {
          stringBuilder.Append(FormatRecursively(node.GetSubtree(0)));
          stringBuilder.Append(" sin");
        } else if (symbol is Square) {
          stringBuilder.Append(FormatPower(node.GetSubtree(0), "2"));
        } else if (symbol is SquareRoot) {
          stringBuilder.Append(FormatPower(node.GetSubtree(0), "(1/2)"));
        } else if (symbol is Subtraction) {
          if (node.SubtreeCount == 1) {
            stringBuilder.Append("-1*");
            stringBuilder.Append(FormatRecursively(node.GetSubtree(0)));
          } else {
            stringBuilder.Append(FormatRecursively(node.GetSubtree(0)));
            for (var i = 1; i < node.SubtreeCount; i++) {
              stringBuilder.Append(" - ");
              stringBuilder.Append(FormatRecursively(node.GetSubtree(i)));
            }
          }
        } else if (symbol is Tangent) {
          stringBuilder.Append(FormatRecursively(node.GetSubtree(0)));
          stringBuilder.Append(" tan");
        } else if (symbol is HyperbolicTangent) {
          stringBuilder.Append(FormatRecursively(node.GetSubtree(0)));
          stringBuilder.Append(" tanh");
        } else if (symbol is Variable) {
          var variableTreeNode = (VariableTreeNode)node;
          stringBuilder.Append(variableTreeNode.Weight.ToString(CultureInfo.InvariantCulture));
          stringBuilder.Append('*');
          stringBuilder.Append(variableTreeNode.VariableName);
        } else if (symbol is BinaryFactorVariable or FactorVariable) {
          stringBuilder.Append("factor variables are not supported");
        } else if (symbol is SubFunctionSymbol) {
          stringBuilder.Append(FormatRecursively(node.GetSubtree(0)));
        } else {
          stringBuilder.Append('(');
          for (var i = 0; i < node.SubtreeCount; i++) {
            if (i > 0) {
              stringBuilder.Append(", ");
            }
            stringBuilder.Append(FormatRecursively(node.GetSubtree(i)));
          }

          stringBuilder.Append($" {node.Symbol} [Not Supported] )");
        }

        break;
      }
    }

    stringBuilder.Append(")");

    return stringBuilder.ToString();
  }

  private string FormatPower(SymbolicExpressionTreeNode node, string exponent) => $"(({FormatRecursively(node)}) log * {exponent}) exp ";
}
