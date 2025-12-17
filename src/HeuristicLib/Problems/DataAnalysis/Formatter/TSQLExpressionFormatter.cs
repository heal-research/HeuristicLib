using System.Globalization;
using System.Text;
using HEAL.HeuristicLib.Encodings.SymbolicExpressionTree;
using HEAL.HeuristicLib.Encodings.SymbolicExpressionTree.Formatters;
using HEAL.HeuristicLib.Encodings.SymbolicExpressionTree.Symbols.Math;
using HEAL.HeuristicLib.Encodings.SymbolicExpressionTree.Symbols.Math.Variables;

namespace HEAL.HeuristicLib.Problems.DataAnalysis.Formatter;

internal static class StringBuilderExtensions {
  internal static void AppendIndented(this StringBuilder strBuilder, int level, string text) {
    strBuilder.Append(new string(' ', level * 2));
    strBuilder.Append(text);
  }

  internal static void AppendLineIndented(this StringBuilder strBuilder, int level, string text) {
    strBuilder.Append(new string(' ', level * 2));
    strBuilder.AppendLine(text);
  }
}

public sealed class TsqlExpressionFormatter : ISymbolicExpressionTreeStringFormatter {
  public string Format(SymbolicExpressionTree symbolicExpressionTree) {
    // skip root and start symbols
    var strBuilder = new StringBuilder();
    GenerateHeader(strBuilder, symbolicExpressionTree);

    //generate function body
    FormatRecursively(1, symbolicExpressionTree.Root.GetSubtree(0).GetSubtree(0), strBuilder);

    GenerateFooter(strBuilder);
    return strBuilder.ToString();
  }

  private void GenerateHeader(StringBuilder strBuilder, SymbolicExpressionTree symbolicExpressionTree) {
    var floatVarNames = new HashSet<string>();
    foreach (var node in symbolicExpressionTree.IterateNodesPostfix().Where(x => x is VariableTreeNode || x is VariableConditionTreeNode)) {
      floatVarNames.Add(((VariableTreeNode)node).VariableName);
    }

    var sortedFloatIdentifiers = floatVarNames.OrderBy(n => n, new NaturalStringComparer()).Select(n => VariableName2Identifier(n)).ToList();

    var varcharVarNames = new HashSet<string>();
    foreach (var node in symbolicExpressionTree.IterateNodesPostfix().Where(x => x is BinaryFactorVariableTreeNode || x is FactorVariableTreeNode)) {
      varcharVarNames.Add(((VariableTreeNode)node).VariableName);
    }

    var sortedVarcharIdentifiers = varcharVarNames.OrderBy(n => n, new NaturalStringComparer()).Select(n => VariableName2Identifier(n)).ToList();

    //Generate comment and instructions
    strBuilder.Append("-- generated. created function can be used like 'SELECT dbo.REGRESSIONMODEL(");
    strBuilder.Append(string.Join(", ", sortedVarcharIdentifiers));
    if (varcharVarNames.Any() && floatVarNames.Any())
      strBuilder.Append(",");
    strBuilder.Append(string.Join(", ", sortedFloatIdentifiers));
    strBuilder.AppendLine(")'");
    strBuilder.AppendLine("-- use the expression after the RETURN statement if you want to incorporate the model in a query without creating a function.");

    //Generate function header
    strBuilder.Append("CREATE FUNCTION dbo.REGRESSIONMODEL(");
    strBuilder.Append(string.Join(", ", sortedVarcharIdentifiers.Select(n => $"{n} NVARCHAR(max)")));
    if (varcharVarNames.Any() && floatVarNames.Any())
      strBuilder.Append(",");
    strBuilder.Append(string.Join(", ", sortedFloatIdentifiers.Select(n => $"{n} FLOAT")));
    strBuilder.AppendLine(")");

    //start function body
    strBuilder.AppendLine("RETURNS FLOAT");
    strBuilder.AppendLine("BEGIN");

    //add variable declaration for convenience
    strBuilder.AppendLineIndented(1, "-- added variable declaration for convenience");
    foreach (var name in sortedVarcharIdentifiers)
      strBuilder.AppendLineIndented(1, $"-- DECLARE {name} NVARCHAR(max) = ''");
    foreach (var name in sortedFloatIdentifiers)
      strBuilder.AppendLineIndented(1, $"-- DECLARE {name} FLOAT = 0.0");
    strBuilder.AppendLineIndented(1, "-- SELECT");
    strBuilder.AppendLine("RETURN ");
  }

  private void FormatRecursively(int level, SymbolicExpressionTreeNode node, StringBuilder strBuilder) {
    if (node.Subtrees.Any()) {
      switch (node.Symbol) {
        case Addition:
          FormatOperator(level, node, "+", strBuilder);
          break;
        case And:
          FormatOperator(level, node, "AND", strBuilder);
          break;
        case Average:
          FormatAverage(level, node, strBuilder);
          break;
        case Cosine:
          FormatFunction(level, node, "COS", strBuilder);
          break;
        case Division:
          FormatDivision(level, node, strBuilder);
          break;
        case Exponential:
          FormatFunction(level, node, "EXP", strBuilder);
          break;
        case GreaterThan:
          FormatOperator(level, node, ">", strBuilder);
          break;
        case IfThenElse:
          FormatIfThenElse(level, node, strBuilder);
          break;
        case LessThan:
          FormatOperator(level, node, "<", strBuilder);
          break;
        case Logarithm:
          FormatFunction(level, node, "LOG", strBuilder);
          break;
        case Multiplication:
          FormatOperator(level, node, "*", strBuilder);
          break;
        case Not:
          FormatOperator(level, node, "NOT LIKE", strBuilder);
          break;
        case Or:
          FormatOperator(level, node, "OR", strBuilder);
          break;
        case Xor:
          throw new NotSupportedException($"Symbol {node.Symbol.GetType().Name} not yet supported.");
        case Sine:
          FormatFunction(level, node, "SIN", strBuilder);
          break;
        case Subtraction:
          FormatSubtraction(level, node, strBuilder);
          break;
        case Tangent:
          FormatFunction(level, node, "TAN", strBuilder);
          break;
        case Square:
          FormatFunction(level, node, "SQUARE", strBuilder);
          break;
        case SquareRoot:
          FormatFunction(level, node, "SQRT", strBuilder);
          break;
        case Power:
          FormatFunction(level, node, "POWER", strBuilder);
          break;
        case Root:
          FormatRoot(level, node, strBuilder);
          break;
        case SubFunctionSymbol:
          FormatRecursively(level, node.GetSubtree(0), strBuilder);
          break;
        default:
          throw new NotSupportedException("Formatting of symbol: " + node.Symbol + " not supported for TSQL symbolic expression tree formatter.");
      }
    } else {
      switch (node) {
        case VariableTreeNode treeNode: {
          strBuilder.AppendFormat("{0} * {1}", VariableName2Identifier(treeNode.VariableName), treeNode.Weight.ToString("g17", CultureInfo.InvariantCulture));
          break;
        }
        case NumberTreeNode numNode:
          strBuilder.Append(numNode.Value.ToString("g17", CultureInfo.InvariantCulture));
          break;
        default: {
          switch (node.Symbol) {
            case FactorVariable: {
              var factorNode = node as FactorVariableTreeNode;
              FormatFactor(level, factorNode, strBuilder);
              break;
            }
            case BinaryFactorVariable: {
              throw new NotSupportedException($"Symbol {node.Symbol.GetType().Name} not yet supported.");
            }
            default:
              throw new NotSupportedException("Formatting of symbol: " + node.Symbol + " not supported for TSQL symbolic expression tree formatter.");
          }

          break;
        }
      }
    }
  }

  private void FormatIfThenElse(int level, SymbolicExpressionTreeNode node, StringBuilder strBuilder) {
    strBuilder.Append("CASE ISNULL((SELECT 1 WHERE");
    FormatRecursively(level, node.GetSubtree(0), strBuilder);
    strBuilder.AppendLine("),0)");
    strBuilder.AppendIndented(level, "WHEN 1 THEN ");
    FormatRecursively(level, node.GetSubtree(1), strBuilder);
    strBuilder.AppendLine();
    strBuilder.AppendIndented(level, "WHEN 0 THEN ");
    FormatRecursively(level, node.GetSubtree(2), strBuilder);
    strBuilder.AppendLine();
    strBuilder.AppendIndented(level, "END");
  }

  private void FormatAverage(int level, SymbolicExpressionTreeNode node, StringBuilder strBuilder) {
    strBuilder.Append("(");
    foreach (var child in node.Subtrees) {
      FormatRecursively(level, child, strBuilder);
      if (child != node.Subtrees.Last())
        strBuilder.Append(" + ");
    }

    strBuilder.AppendFormat(") / {0}", node.Subtrees.Count());
  }

  private string VariableName2Identifier(string variableName) {
    return "@" + variableName.Replace(' ', '_');
  }

  private void GenerateFooter(StringBuilder strBuilder) {
    strBuilder.Append(Environment.NewLine);
    strBuilder.AppendLine("END");
  }

  private void FormatOperator(int level, SymbolicExpressionTreeNode node, string symbol, StringBuilder strBuilder) {
    strBuilder.Append("(");
    foreach (var child in node.Subtrees) {
      FormatRecursively(level, child, strBuilder);
      if (child != node.Subtrees.Last())
        strBuilder.Append(" " + symbol + " ");
    }

    strBuilder.Append(")");
  }

  private void FormatFunction(int level, SymbolicExpressionTreeNode node, string function, StringBuilder strBuilder) {
    strBuilder.Append(function + "(");
    foreach (var child in node.Subtrees) {
      FormatRecursively(level++, child, strBuilder);
      if (child != node.Subtrees.Last())
        strBuilder.Append(", ");
    }

    strBuilder.Append(")");
  }

  private void FormatDivision(int level, SymbolicExpressionTreeNode node, StringBuilder strBuilder) {
    if (node.SubtreeCount == 1) {
      strBuilder.Append("1.0 / ");
      FormatRecursively(level, node.GetSubtree(0), strBuilder);
    } else {
      FormatRecursively(level, node.GetSubtree(0), strBuilder);
      strBuilder.Append("/ (");
      for (var i = 1; i < node.SubtreeCount; i++) {
        if (i > 1) strBuilder.Append(" * ");
        FormatRecursively(level, node.GetSubtree(i), strBuilder);
      }

      strBuilder.Append(")");
    }
  }

  private void FormatSubtraction(int level, SymbolicExpressionTreeNode node, StringBuilder strBuilder) {
    if (node.SubtreeCount == 1) {
      strBuilder.Append("-");
      FormatRecursively(level, node.GetSubtree(0), strBuilder);
      return;
    }

    //Default case: more than 1 child
    FormatOperator(level, node, "-", strBuilder);
  }

  private void FormatRoot(int level, SymbolicExpressionTreeNode node, StringBuilder strBuilder) {
    strBuilder.AppendLine("POWER(");
    FormatRecursively(level, node.GetSubtree(0), strBuilder);
    strBuilder.AppendLineIndented(level, " , 1.0 / (");
    FormatRecursively(level, node.GetSubtree(1), strBuilder);
    strBuilder.AppendLineIndented(level, "))");
  }

  private void FormatFactor(int level, FactorVariableTreeNode node, StringBuilder strBuilder) {
    strBuilder.AppendLine("( ");
    strBuilder.AppendLineIndented(level + 1, $"CASE {VariableName2Identifier(node.VariableName)}");
    foreach (var name in node.Symbol.GetVariableValues(node.VariableName)) {
      strBuilder.AppendLineIndented(level + 2, $"WHEN '{name}' THEN {node.GetValue(name).ToString(CultureInfo.InvariantCulture)}");
    }

    strBuilder.AppendLineIndented(level + 1, "ELSE NULL END");
    strBuilder.AppendIndented(level, ")");
  }
}
