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
  private string GetExcelColumnName(int columnNumber)
  {
    var dividend = columnNumber;
    var columnName = string.Empty;

    while (dividend > 0) {
      var modulo = (dividend - 1) % 26;
      columnName = Convert.ToChar(65 + modulo) + columnName;
      dividend = (dividend - modulo) / 26;
    }

    return columnName;
  }

  private readonly Dictionary<string, string> variableNameMapping = new();
  private int currentVariableIndex;

  private string GetColumnToVariableName(string varName)
  {
    if (!variableNameMapping.ContainsKey(varName)) {
      currentVariableIndex++;
      variableNameMapping.Add(varName, GetExcelColumnName(currentVariableIndex));
    }

    return string.Format("${0}1", variableNameMapping[varName]);
  }

  public string Format(SymbolicExpressionTree symbolicExpressionTree) => Format(symbolicExpressionTree, null);

  public string Format(SymbolicExpressionTree symbolicExpressionTree, Dataset? dataset)
  {
    if (dataset != null) {
      return FormatWithMapping(symbolicExpressionTree, CalculateVariableMapping(symbolicExpressionTree, dataset));
    }

    return FormatWithMapping(symbolicExpressionTree, new Dictionary<string, string>());
  }

  public string FormatWithMapping(SymbolicExpressionTree symbolicExpressionTree, Dictionary<string, string> variableNameMapping)
  {
    foreach (var kvp in variableNameMapping) {
      this.variableNameMapping.Add(kvp.Key, kvp.Value);
    }

    var stringBuilder = new StringBuilder();

    stringBuilder.Append("=");
    stringBuilder.Append(FormatRecursively(symbolicExpressionTree.Root));

    foreach (var variable in this.variableNameMapping) {
      stringBuilder.AppendLine();
      stringBuilder.Append(variable.Key + " = " + variable.Value);
    }

    return stringBuilder.ToString();
  }

  private Dictionary<string, string> CalculateVariableMapping(SymbolicExpressionTree tree, Dataset dataset)
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

    if (symbol is ProgramRootSymbol) {
      stringBuilder.AppendLine(FormatRecursively(node.GetSubtree(0)));
    } else if (symbol is StartSymbol) {
      return FormatRecursively(node.GetSubtree(0));
    } else if (symbol is Addition) {
      stringBuilder.Append("(");
      for (var i = 0; i < node.SubtreeCount; i++) {
        if (i > 0) {
          stringBuilder.Append("+");
        }

        stringBuilder.Append(FormatRecursively(node.GetSubtree(i)));
      }

      stringBuilder.Append(")");
    } else if (symbol is Absolute) {
      stringBuilder.Append($"ABS({FormatRecursively(node.GetSubtree(0))})");
    } else if (symbol is AnalyticQuotient) {
      stringBuilder.Append($"({FormatRecursively(node.GetSubtree(0))}) / SQRT(1 + POWER({FormatRecursively(node.GetSubtree(1))}, 2))");
    } else if (symbol is Average) {
      stringBuilder.Append("(1/(");
      stringBuilder.Append(node.SubtreeCount);
      stringBuilder.Append(")*(");
      for (var i = 0; i < node.SubtreeCount; i++) {
        if (i > 0) {
          stringBuilder.Append("+");
        }

        stringBuilder.Append("(");
        stringBuilder.Append(FormatRecursively(node.GetSubtree(i)));
        stringBuilder.Append(")");
      }

      stringBuilder.Append("))");
    } else if (symbol is Number numSy) {
      var numTreeNode = (NumberTreeNode)node;
      stringBuilder.Append(numTreeNode.Value.ToString(CultureInfo.InvariantCulture));
    } else if (symbol is Cosine) {
      stringBuilder.Append("COS(");
      stringBuilder.Append(FormatRecursively(node.GetSubtree(0)));
      stringBuilder.Append(")");
    } else if (symbol is Cube) {
      stringBuilder.Append($"POWER({FormatRecursively(node.GetSubtree(0))}, 3)");
    } else if (symbol is CubeRoot) {
      var argExpr = FormatRecursively(node.GetSubtree(0));
      stringBuilder.Append($"IF({argExpr} < 0, -POWER(-{argExpr}, 1/3), POWER({argExpr}, 1/3))");
    } else if (symbol is Division) {
      if (node.SubtreeCount == 1) {
        stringBuilder.Append("1/(");
        stringBuilder.Append(FormatRecursively(node.GetSubtree(0)));
        stringBuilder.Append(")");
      } else {
        stringBuilder.Append(FormatRecursively(node.GetSubtree(0)));
        stringBuilder.Append("/(");
        for (var i = 1; i < node.SubtreeCount; i++) {
          if (i > 1) {
            stringBuilder.Append("*");
          }

          stringBuilder.Append(FormatRecursively(node.GetSubtree(i)));
        }

        stringBuilder.Append(")");
      }
    } else if (symbol is Exponential) {
      stringBuilder.Append("EXP(");
      stringBuilder.Append(FormatRecursively(node.GetSubtree(0)));
      stringBuilder.Append(")");
    } else if (symbol is Square) {
      stringBuilder.Append("POWER(");
      stringBuilder.Append(FormatRecursively(node.GetSubtree(0)));
      stringBuilder.Append(",2)");
    } else if (symbol is SquareRoot) {
      stringBuilder.Append("SQRT(");
      stringBuilder.Append(FormatRecursively(node.GetSubtree(0)));
      stringBuilder.Append(")");
    } else if (symbol is Logarithm) {
      stringBuilder.Append("LN(");
      stringBuilder.Append(FormatRecursively(node.GetSubtree(0)));
      stringBuilder.Append(")");
    } else if (symbol is Multiplication) {
      for (var i = 0; i < node.SubtreeCount; i++) {
        if (i > 0) {
          stringBuilder.Append("*");
        }

        stringBuilder.Append(FormatRecursively(node.GetSubtree(i)));
      }
    } else if (symbol is Sine) {
      stringBuilder.Append("SIN(");
      stringBuilder.Append(FormatRecursively(node.GetSubtree(0)));
      stringBuilder.Append(")");
    } else if (symbol is Subtraction) {
      stringBuilder.Append("(");
      if (node.SubtreeCount == 1) {
        stringBuilder.Append("-");
        stringBuilder.Append(FormatRecursively(node.GetSubtree(0)));
      } else {
        stringBuilder.Append(FormatRecursively(node.GetSubtree(0)));
        for (var i = 1; i < node.SubtreeCount; i++) {
          stringBuilder.Append("-");
          stringBuilder.Append(FormatRecursively(node.GetSubtree(i)));
        }
      }

      stringBuilder.Append(")");
    } else if (symbol is Tangent) {
      stringBuilder.Append("TAN(");
      stringBuilder.Append(FormatRecursively(node.GetSubtree(0)));
      stringBuilder.Append(")");
    } else if (symbol is HyperbolicTangent) {
      stringBuilder.Append("TANH(");
      stringBuilder.Append(FormatRecursively(node.GetSubtree(0)));
      stringBuilder.Append(")");
    } else if (symbol is Variable) {
      var variableTreeNode = (VariableTreeNode)node;
      stringBuilder.Append(variableTreeNode.Weight.ToString(CultureInfo.InvariantCulture));
      stringBuilder.Append("*");
      stringBuilder.Append(GetColumnToVariableName(variableTreeNode.VariableName));
    } else if (symbol is BinaryFactorVariable) {
      var binFactorNode = (BinaryFactorVariableTreeNode)node;
      stringBuilder.AppendFormat("IF({0}=\"{1}\", {2}, 0)",
        GetColumnToVariableName(binFactorNode.VariableName),
        binFactorNode.VariableValue,
        binFactorNode.Weight.ToString(CultureInfo.InvariantCulture)
      );
    } else if (symbol is FactorVariable) {
      var factorNode = (FactorVariableTreeNode)node;
      var values = factorNode.Symbol.GetVariableValues(factorNode.VariableName).ToArray();
      var w = factorNode.Weights ?? [];
      // create nested if
      for (var i = 0; i < values.Length; i++) {
        stringBuilder.AppendFormat("IF({0}=\"{1}\", {2}, ",
          GetColumnToVariableName(factorNode.VariableName),
          values[i],
          w[i].ToString(CultureInfo.InvariantCulture));
      }

      stringBuilder.Append("\"\""); // return empty string on unknown value
      stringBuilder.Append(')', values.Length); // add closing parenthesis
    } else if (symbol is Power) {
      stringBuilder.Append("POWER(");
      stringBuilder.Append(FormatRecursively(node.GetSubtree(0)));
      stringBuilder.Append(",ROUND(");
      stringBuilder.Append(FormatRecursively(node.GetSubtree(1)));
      stringBuilder.Append(",0))");
    } else if (symbol is Root) {
      stringBuilder.Append("(");
      stringBuilder.Append(FormatRecursively(node.GetSubtree(0)));
      stringBuilder.Append(")^(1 / ROUND(");
      stringBuilder.Append(FormatRecursively(node.GetSubtree(1)));
      stringBuilder.Append(",0))");
    } else if (symbol is IfThenElse) {
      stringBuilder.Append("IF(");
      stringBuilder.Append("(" + FormatRecursively(node.GetSubtree(0)) + " ) > 0");
      stringBuilder.Append(",");
      stringBuilder.Append(FormatRecursively(node.GetSubtree(1)));
      stringBuilder.Append(",");
      stringBuilder.Append(FormatRecursively(node.GetSubtree(2)));
      stringBuilder.Append(")");
    } else if (symbol is VariableCondition) {
      var variableConditionTreeNode = (VariableConditionTreeNode)node;
      if (!variableConditionTreeNode.Symbol.IgnoreSlope) {
        var threshold = variableConditionTreeNode.Threshold;
        var slope = variableConditionTreeNode.Slope;
        var p = "(1 / (1 + EXP(-" + slope.ToString(CultureInfo.InvariantCulture) + " * (" +
                GetColumnToVariableName(variableConditionTreeNode.VariableName) + "-" +
                threshold.ToString(CultureInfo.InvariantCulture) + "))))";
        stringBuilder.Append("((");
        stringBuilder.Append(FormatRecursively(node.GetSubtree(0)));
        stringBuilder.Append("*");
        stringBuilder.Append(p);
        stringBuilder.Append(") + (");
        stringBuilder.Append(FormatRecursively(node.GetSubtree(1)));
        stringBuilder.Append("*(");
        stringBuilder.Append("1 - " + p + ")");
        stringBuilder.Append("))");
      } else {
        stringBuilder.AppendFormat(CultureInfo.InvariantCulture, "(IF({0} <= {1}, {2}, {3}))",
          GetColumnToVariableName(variableConditionTreeNode.VariableName),
          variableConditionTreeNode.Threshold,
          FormatRecursively(node.GetSubtree(0)),
          FormatRecursively(node.GetSubtree(1))
        );
      }
    } else if (symbol is Xor) {
      stringBuilder.Append("IF(");
      stringBuilder.Append("XOR(");
      stringBuilder.Append("(" + FormatRecursively(node.GetSubtree(0)) + ") > 0,");
      stringBuilder.Append("(" + FormatRecursively(node.GetSubtree(1)) + ") > 0");
      stringBuilder.Append("), 1.0, -1.0)");
    } else if (symbol is Or) {
      stringBuilder.Append("IF(");
      stringBuilder.Append("OR(");
      stringBuilder.Append("(" + FormatRecursively(node.GetSubtree(0)) + ") > 0,");
      stringBuilder.Append("(" + FormatRecursively(node.GetSubtree(1)) + ") > 0");
      stringBuilder.Append("), 1.0, -1.0)");
    } else if (symbol is And) {
      stringBuilder.Append("IF(");
      stringBuilder.Append("AND(");
      stringBuilder.Append("(" + FormatRecursively(node.GetSubtree(0)) + ") > 0,");
      stringBuilder.Append("(" + FormatRecursively(node.GetSubtree(1)) + ") > 0");
      stringBuilder.Append("), 1.0, -1.0)");
    } else if (symbol is Not) {
      stringBuilder.Append("IF(");
      stringBuilder.Append(FormatRecursively(node.GetSubtree(0)));
      stringBuilder.Append(" > 0, -1.0, 1.0)");
    } else if (symbol is GreaterThan) {
      stringBuilder.Append("IF((");
      stringBuilder.Append(FormatRecursively(node.GetSubtree(0)));
      stringBuilder.Append(") > (");
      stringBuilder.Append(FormatRecursively(node.GetSubtree(1)));
      stringBuilder.Append("), 1.0, -1.0)");
    } else if (symbol is LessThan) {
      stringBuilder.Append("IF((");
      stringBuilder.Append(FormatRecursively(node.GetSubtree(0)));
      stringBuilder.Append(") < (");
      stringBuilder.Append(FormatRecursively(node.GetSubtree(1)));
      stringBuilder.Append("), 1.0, -1.0)");
    } else if (symbol is SubFunctionSymbol) {
      stringBuilder.Append(FormatRecursively(node.GetSubtree(0)));
    } else {
      throw new NotImplementedException("Excel export of " + node.Symbol + " is not implemented.");
    }

    return stringBuilder.ToString();
  }
}
