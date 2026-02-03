using System.Globalization;
using System.Text;
using HEAL.HeuristicLib.Genotypes.Trees;
using HEAL.HeuristicLib.SearchSpaces.Trees.SymbolicExpressionTree.Formatters;
using HEAL.HeuristicLib.SearchSpaces.Trees.SymbolicExpressionTree.Symbols;
using HEAL.HeuristicLib.SearchSpaces.Trees.SymbolicExpressionTree.Symbols.Math;
using HEAL.HeuristicLib.SearchSpaces.Trees.SymbolicExpressionTree.Symbols.Math.Variables;

namespace HEAL.HeuristicLib.Problems.DataAnalysis.Formatter;

public sealed class SymbolicDataAnalysisExpressionExcelFormatter : ISymbolicExpressionTreeStringFormatter
{

  private readonly Dictionary<string, string> variableNameMapping = new();
  private int currentVariableIndex;

  public string Format(SymbolicExpressionTree symbolicExpressionTree) => Format(symbolicExpressionTree, null);
  private static string GetExcelColumnName(int columnNumber)
  {
    var dividend = columnNumber;
    var columnName = string.Empty;

    while (dividend > 0) {
      var modulo = (dividend - 1) % 26;
      columnName = $"{Convert.ToChar(65 + modulo)}{columnName}";
      dividend = (dividend - modulo) / 26;
    }

    return columnName;
  }

  private string GetColumnToVariableName(string varName)
  {
    if (variableNameMapping.TryGetValue(varName, out var value)) {
      return $"${value}1";
    }

    currentVariableIndex++;
    variableNameMapping.Add(varName, GetExcelColumnName(currentVariableIndex));

    return $"${variableNameMapping[varName]}1";
  }

  public string Format(SymbolicExpressionTree symbolicExpressionTree, Dataset? dataset) => FormatWithMapping(symbolicExpressionTree, dataset != null ? CalculateVariableMapping(symbolicExpressionTree, dataset) : new Dictionary<string, string>());

  public string FormatWithMapping(SymbolicExpressionTree symbolicExpressionTree, Dictionary<string, string> customVariableNameMapping)
  {
    foreach (var kvp in customVariableNameMapping) {
      variableNameMapping.Add(kvp.Key, kvp.Value);
    }
    var stringBuilder = new StringBuilder();

    stringBuilder.Append('=');
    stringBuilder.Append(FormatRecursively(symbolicExpressionTree.Root));

    foreach (var variable in variableNameMapping) {
      stringBuilder.AppendLine();
      stringBuilder.Append(variable.Key + " = " + variable.Value);
    }

    return stringBuilder.ToString();
  }

  private static Dictionary<string, string> CalculateVariableMapping(SymbolicExpressionTree tree, Dataset dataset)
  {
    var mapping = new Dictionary<string, string>();
    var inputIndex = 0;
    var usedVariables = tree.IterateNodesPrefix().OfType<VariableTreeNode>().Select(v => v.VariableName).Distinct().ToArray();
    foreach (var variable in dataset.GetVariableNames()) {
      if (!usedVariables.Contains(variable)) {
        continue;
      }
      inputIndex++;
      mapping[variable] = GetExcelColumnName(inputIndex);
    }

    return mapping;
  }

  private string FormatRecursively(SymbolicExpressionTreeNode node)
  {
    var symbol = node.Symbol;
    var stringBuilder = new StringBuilder();

    switch (symbol) {
      case ProgramRootSymbol:
        stringBuilder.AppendLine(FormatRecursively(node.GetSubtree(0)));

        break;
      case StartSymbol:
        return FormatRecursively(node.GetSubtree(0));
      case Addition: {
        stringBuilder.Append('(');
        for (var i = 0; i < node.SubtreeCount; i++) {
          if (i > 0) {
            stringBuilder.Append('+');
          }
          stringBuilder.Append(FormatRecursively(node.GetSubtree(i)));
        }

        stringBuilder.Append(')');

        break;
      }
      case Absolute:
        stringBuilder.Append($"ABS({FormatRecursively(node.GetSubtree(0))})");

        break;
      case AnalyticQuotient:
        stringBuilder.Append($"({FormatRecursively(node.GetSubtree(0))}) / SQRT(1 + POWER({FormatRecursively(node.GetSubtree(1))}, 2))");

        break;
      case Average: {
        stringBuilder.Append("(1/(");
        stringBuilder.Append(node.SubtreeCount);
        stringBuilder.Append(")*(");
        for (var i = 0; i < node.SubtreeCount; i++) {
          if (i > 0) {
            stringBuilder.Append('+');
          }
          stringBuilder.Append('(');
          stringBuilder.Append(FormatRecursively(node.GetSubtree(i)));
          stringBuilder.Append(')');
        }

        stringBuilder.Append("))");

        break;
      }
      case Number: {
        var numTreeNode = (NumberTreeNode)node;
        stringBuilder.Append(numTreeNode.Value.ToString(CultureInfo.InvariantCulture));

        break;
      }
      case Cosine:
        stringBuilder.Append("COS(");
        stringBuilder.Append(FormatRecursively(node.GetSubtree(0)));
        stringBuilder.Append(')');

        break;
      case Cube:
        stringBuilder.Append($"POWER({FormatRecursively(node.GetSubtree(0))}, 3)");

        break;
      case CubeRoot: {
        var argExpr = FormatRecursively(node.GetSubtree(0));
        stringBuilder.Append($"IF({argExpr} < 0, -POWER(-{argExpr}, 1/3), POWER({argExpr}, 1/3))");

        break;
      }
      case Division: {
        if (node.SubtreeCount == 1) {
          stringBuilder.Append("1/(");
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
        }

        stringBuilder.Append(')');

        break;
      }
      case Exponential:
        stringBuilder.Append("EXP(");
        stringBuilder.Append(FormatRecursively(node.GetSubtree(0)));
        stringBuilder.Append(')');

        break;
      case Square:
        stringBuilder.Append("POWER(");
        stringBuilder.Append(FormatRecursively(node.GetSubtree(0)));
        stringBuilder.Append(",2)");

        break;
      case SquareRoot:
        stringBuilder.Append("SQRT(");
        stringBuilder.Append(FormatRecursively(node.GetSubtree(0)));
        stringBuilder.Append(')');

        break;
      case Logarithm:
        stringBuilder.Append("LN(");
        stringBuilder.Append(FormatRecursively(node.GetSubtree(0)));
        stringBuilder.Append(')');

        break;
      case Multiplication: {
        for (var i = 0; i < node.SubtreeCount; i++) {
          if (i > 0) {
            stringBuilder.Append('*');
          }
          stringBuilder.Append(FormatRecursively(node.GetSubtree(i)));
        }

        break;
      }
      case Sine:
        stringBuilder.Append("SIN(");
        stringBuilder.Append(FormatRecursively(node.GetSubtree(0)));
        stringBuilder.Append(')');

        break;
      case Subtraction: {
        stringBuilder.Append('(');
        if (node.SubtreeCount == 1) {
          stringBuilder.Append('-');
          stringBuilder.Append(FormatRecursively(node.GetSubtree(0)));
        } else {
          stringBuilder.Append(FormatRecursively(node.GetSubtree(0)));
          for (var i = 1; i < node.SubtreeCount; i++) {
            stringBuilder.Append('-');
            stringBuilder.Append(FormatRecursively(node.GetSubtree(i)));
          }
        }

        stringBuilder.Append(')');

        break;
      }
      case Tangent:
        stringBuilder.Append("TAN(");
        stringBuilder.Append(FormatRecursively(node.GetSubtree(0)));
        stringBuilder.Append(')');

        break;
      case HyperbolicTangent:
        stringBuilder.Append("TANH(");
        stringBuilder.Append(FormatRecursively(node.GetSubtree(0)));
        stringBuilder.Append(')');

        break;
      case Variable: {
        var variableTreeNode = (VariableTreeNode)node;
        stringBuilder.Append(variableTreeNode.Weight.ToString(CultureInfo.InvariantCulture));
        stringBuilder.Append('*');
        stringBuilder.Append(GetColumnToVariableName(variableTreeNode.VariableName));

        break;
      }
      case BinaryFactorVariable: {
        var binFactorNode = (BinaryFactorVariableTreeNode)node;
        stringBuilder.AppendFormat("IF({0}=\"{1}\", {2}, 0)",
        GetColumnToVariableName(binFactorNode.VariableName),
        binFactorNode.VariableValue,
        binFactorNode.Weight.ToString(CultureInfo.InvariantCulture)
        );

        break;
      }
      case FactorVariable: {
        var factorNode = (FactorVariableTreeNode)node;
        var values = factorNode.Symbol.GetVariableValues(factorNode.VariableName).ToArray();
        var w = factorNode.Weights;
        // create nested if
        for (var i = 0; i < values.Length; i++) {
          stringBuilder.Append($"IF({GetColumnToVariableName(factorNode.VariableName)}=\"{values[i]}\", {w![i].ToString(CultureInfo.InvariantCulture)}, ");
        }

        stringBuilder.Append("\"\"");// return empty string on unknown value
        stringBuilder.Append(')', values.Length);// add closing parenthesis

        break;
      }
      case Power:
        stringBuilder.Append("POWER(");
        stringBuilder.Append(FormatRecursively(node.GetSubtree(0)));
        stringBuilder.Append(",ROUND(");
        stringBuilder.Append(FormatRecursively(node.GetSubtree(1)));
        stringBuilder.Append(",0))");

        break;
      case Root:
        stringBuilder.Append('(');
        stringBuilder.Append(FormatRecursively(node.GetSubtree(0)));
        stringBuilder.Append(")^(1 / ROUND(");
        stringBuilder.Append(FormatRecursively(node.GetSubtree(1)));
        stringBuilder.Append(",0))");

        break;
      case IfThenElse:
        stringBuilder.Append("IF(");
        stringBuilder.Append("(" + FormatRecursively(node.GetSubtree(0)) + " ) > 0");
        stringBuilder.Append(',');
        stringBuilder.Append(FormatRecursively(node.GetSubtree(1)));
        stringBuilder.Append(',');
        stringBuilder.Append(FormatRecursively(node.GetSubtree(2)));
        stringBuilder.Append(')');

        break;
      case VariableCondition: {
        var variableConditionTreeNode = (VariableConditionTreeNode)node;
        if (!variableConditionTreeNode.Symbol.IgnoreSlope) {
          var threshold = variableConditionTreeNode.Threshold;
          var slope = variableConditionTreeNode.Slope;
          var p = "(1 / (1 + EXP(-" + slope.ToString(CultureInfo.InvariantCulture) + " * (" +
                  GetColumnToVariableName(variableConditionTreeNode.VariableName) + "-" +
                  threshold.ToString(CultureInfo.InvariantCulture) + "))))";
          stringBuilder.Append("((");
          stringBuilder.Append(FormatRecursively(node.GetSubtree(0)));
          stringBuilder.Append('*');
          stringBuilder.Append(p);
          stringBuilder.Append(") + (");
          stringBuilder.Append(FormatRecursively(node.GetSubtree(1)));
          stringBuilder.Append("*(");
          stringBuilder.Append("1 - " + p + ")");
          stringBuilder.Append("))");
        } else {
          stringBuilder.Append(CultureInfo.InvariantCulture, $"(IF({GetColumnToVariableName(variableConditionTreeNode.VariableName)} <= {variableConditionTreeNode.Threshold}, {FormatRecursively(node.GetSubtree(0))}, {FormatRecursively(node.GetSubtree(1))}))");
        }

        break;
      }
      case Xor:
        stringBuilder.Append("IF(");
        stringBuilder.Append("XOR(");
        stringBuilder.Append("(" + FormatRecursively(node.GetSubtree(0)) + ") > 0,");
        stringBuilder.Append("(" + FormatRecursively(node.GetSubtree(1)) + ") > 0");
        stringBuilder.Append("), 1.0, -1.0)");

        break;
      case Or:
        stringBuilder.Append("IF(");
        stringBuilder.Append("OR(");
        stringBuilder.Append("(" + FormatRecursively(node.GetSubtree(0)) + ") > 0,");
        stringBuilder.Append("(" + FormatRecursively(node.GetSubtree(1)) + ") > 0");
        stringBuilder.Append("), 1.0, -1.0)");

        break;
      case And:
        stringBuilder.Append("IF(");
        stringBuilder.Append("AND(");
        stringBuilder.Append("(" + FormatRecursively(node.GetSubtree(0)) + ") > 0,");
        stringBuilder.Append("(" + FormatRecursively(node.GetSubtree(1)) + ") > 0");
        stringBuilder.Append("), 1.0, -1.0)");

        break;
      case Not:
        stringBuilder.Append("IF(");
        stringBuilder.Append(FormatRecursively(node.GetSubtree(0)));
        stringBuilder.Append(" > 0, -1.0, 1.0)");

        break;
      case GreaterThan:
        stringBuilder.Append("IF((");
        stringBuilder.Append(FormatRecursively(node.GetSubtree(0)));
        stringBuilder.Append(") > (");
        stringBuilder.Append(FormatRecursively(node.GetSubtree(1)));
        stringBuilder.Append("), 1.0, -1.0)");

        break;
      case LessThan:
        stringBuilder.Append("IF((");
        stringBuilder.Append(FormatRecursively(node.GetSubtree(0)));
        stringBuilder.Append(") < (");
        stringBuilder.Append(FormatRecursively(node.GetSubtree(1)));
        stringBuilder.Append("), 1.0, -1.0)");

        break;
      case SubFunctionSymbol:
        stringBuilder.Append(FormatRecursively(node.GetSubtree(0)));

        break;
      default:
        throw new NotImplementedException("Excel export of " + node.Symbol + " is not implemented.");
    }

    return stringBuilder.ToString();
  }
}
