using System.Globalization;
using System.Text;
using HEAL.HeuristicLib.Encodings.SymbolicExpressionTree;
using HEAL.HeuristicLib.Encodings.SymbolicExpressionTree.Formatters;
using HEAL.HeuristicLib.Encodings.SymbolicExpressionTree.Symbols.Math;
using HEAL.HeuristicLib.Encodings.SymbolicExpressionTree.Symbols.Math.Variables;

namespace HEAL.HeuristicLib.Problems.DataAnalysis.Formatter;

public sealed class SymbolicDataAnalysisExpressionMathematicaFormatter : ISymbolicExpressionTreeStringFormatter {
  public string Format(SymbolicExpressionTree symbolicExpressionTree) {
    // skip root and start symbols
    var strBuilder = new StringBuilder();
    FormatRecursively(symbolicExpressionTree.Root.GetSubtree(0).GetSubtree(0), strBuilder);
    return strBuilder.ToString();
  }

  private void FormatRecursively(SymbolicExpressionTreeNode node, StringBuilder strBuilder) {
    if (node.Subtrees.Any()) {
      switch (node.Symbol) {
        case Addition:
          FormatFunction(node, "Plus", strBuilder);
          break;
        case Absolute:
          FormatFunction(node, "Abs", strBuilder);
          break;
        case AnalyticQuotient:
          strBuilder.Append('[');
          FormatRecursively(node.GetSubtree(0), strBuilder);
          strBuilder.Append("]/Sqrt[ 1 + Power[");
          FormatRecursively(node.GetSubtree(1), strBuilder);
          strBuilder.Append(", 2]]");
          break;
        case Average:
          FormatAverage(node, strBuilder);
          break;
        case Multiplication:
          FormatFunction(node, "Times", strBuilder);
          break;
        case Subtraction:
          FormatSubtraction(node, strBuilder);
          break;
        case Division:
          FormatDivision(node, strBuilder);
          break;
        case Sine:
          FormatFunction(node, "Sin", strBuilder);
          break;
        case Cosine:
          FormatFunction(node, "Cos", strBuilder);
          break;
        case Tangent:
          FormatFunction(node, "Tan", strBuilder);
          break;
        case HyperbolicTangent:
          FormatFunction(node, "Tanh", strBuilder);
          break;
        case Exponential:
          FormatFunction(node, "Exp", strBuilder);
          break;
        case Logarithm:
          FormatFunction(node, "Log", strBuilder);
          break;
        case IfThenElse:
          FormatIf(node, strBuilder);
          break;
        case GreaterThan:
          strBuilder.Append("If[Greater[");
          FormatRecursively(node.GetSubtree(0), strBuilder);
          strBuilder.Append(',');
          FormatRecursively(node.GetSubtree(1), strBuilder);
          strBuilder.Append("], 1, -1]");
          break;
        case LessThan:
          strBuilder.Append("If[Less[");
          FormatRecursively(node.GetSubtree(0), strBuilder);
          strBuilder.Append(',');
          FormatRecursively(node.GetSubtree(1), strBuilder);
          strBuilder.Append("], 1, -1]");
          break;
        case And:
          FormatAnd(node, strBuilder);
          break;
        case Not:
          strBuilder.Append("If[Greater[");
          FormatRecursively(node.GetSubtree(0), strBuilder);
          strBuilder.Append(", 0], -1, 1]");
          break;
        case Or:
          FormatOr(node, strBuilder);
          break;
        case Xor:
          FormatXor(node, strBuilder);
          break;
        case Square:
          FormatSquare(node, strBuilder);
          break;
        case SquareRoot:
          FormatFunction(node, "Sqrt", strBuilder);
          break;
        case Cube:
          FormatPower(node, strBuilder, "3");
          break;
        case CubeRoot:
          strBuilder.Append("CubeRoot[");
          FormatRecursively(node.GetSubtree(0), strBuilder);
          strBuilder.Append(']');
          break;
        case Power:
          FormatFunction(node, "Power", strBuilder);
          break;
        case Root:
          FormatRoot(node, strBuilder);
          break;
        case SubFunctionSymbol:
          FormatRecursively(node.GetSubtree(0), strBuilder);
          break;
        default:
          throw new NotSupportedException("Formatting of symbol: " + node.Symbol + " is not supported.");
      }
    } else {
      switch (node.Symbol) {
        // terminals
        case Variable: {
          var varNode = (VariableTreeNode)node;
          strBuilder.Append($"Times[{varNode.VariableName}, {varNode.Weight.ToString("G17", CultureInfo.InvariantCulture)}]");
          break;
        }
        case Number: {
          var numNode = (NumberTreeNode)node;
          strBuilder.Append(numNode.Value.ToString("G17", CultureInfo.InvariantCulture));
          break;
        }
        case FactorVariable: {
          var factorNode = (FactorVariableTreeNode)node;
          strBuilder.Append($"Switch[{factorNode.VariableName},");
          var varValues = factorNode.Symbol.GetVariableValues(factorNode.VariableName).ToArray();
          var weights = varValues.Select(factorNode.GetValue).ToArray();

          var weightStr = string.Join(", ",
            varValues.Zip(weights, (s, d) => string.Format(CultureInfo.InvariantCulture, "\"{0}\", {1:G17}", s, d)));
          strBuilder.Append(weightStr);
          strBuilder.Append(']');
          break;
        }
        case BinaryFactorVariable: {
          var factorNode = (BinaryFactorVariableTreeNode)node;
          strBuilder.Append(CultureInfo.InvariantCulture, $"If[{factorNode.VariableName}==\"{factorNode.VariableValue}\",{factorNode.Weight:G17},0.0]");
          break;
        }
        default:
          throw new NotSupportedException("Formatting of symbol: " + node.Symbol + " is not supported.");
      }
    }
  }

  private void FormatXor(SymbolicExpressionTreeNode node, StringBuilder strBuilder) {
    strBuilder.Append("If[Xor[");
    foreach (var t in node.Subtrees) {
      strBuilder.Append("Greater[");
      FormatRecursively(t, strBuilder);
      strBuilder.Append(", 0]");
      if (t != node.Subtrees.Last()) strBuilder.Append(",");
    }

    strBuilder.Append("], 1, -1]");
  }

  private void FormatOr(SymbolicExpressionTreeNode node, StringBuilder strBuilder) {
    strBuilder.Append("If[Or[");
    foreach (var t in node.Subtrees) {
      strBuilder.Append("Greater[");
      FormatRecursively(t, strBuilder);
      strBuilder.Append(", 0]");
      if (t != node.Subtrees.Last()) strBuilder.Append(",");
    }

    strBuilder.Append("], 1, -1]");
  }

  private void FormatAnd(SymbolicExpressionTreeNode node, StringBuilder strBuilder) {
    strBuilder.Append("If[And[");
    foreach (var t in node.Subtrees) {
      strBuilder.Append("Greater[");
      FormatRecursively(t, strBuilder);
      strBuilder.Append(", 0]");
      if (t != node.Subtrees.Last()) strBuilder.Append(",");
    }

    strBuilder.Append("], 1, -1]");
  }

  private void FormatIf(SymbolicExpressionTreeNode node, StringBuilder strBuilder) {
    strBuilder.Append("If[Greater[");
    FormatRecursively(node.GetSubtree(0), strBuilder);
    strBuilder.Append(", 0], ");
    FormatRecursively(node.GetSubtree(1), strBuilder);
    strBuilder.Append(", ");
    FormatRecursively(node.GetSubtree(2), strBuilder);
    strBuilder.Append("]");
  }

  private void FormatAverage(SymbolicExpressionTreeNode node, StringBuilder strBuilder) {
    // mean function needs a list of values
    strBuilder.Append("Mean[{");
    FormatRecursively(node.GetSubtree(0), strBuilder);
    for (var i = 1; i < node.SubtreeCount; i++) {
      strBuilder.Append(",");
      FormatRecursively(node.GetSubtree(i), strBuilder);
    }

    strBuilder.Append("}]");
  }

  private void FormatSubtraction(SymbolicExpressionTreeNode node, StringBuilder strBuilder) {
    strBuilder.Append("Subtract[");
    FormatRecursively(node.GetSubtree(0), strBuilder);
    strBuilder.Append(", Times[-1");
    foreach (var t in node.Subtrees) {
      strBuilder.Append(",");
      FormatRecursively(t, strBuilder);
    }

    strBuilder.Append("]]");
  }

  private void FormatSquare(SymbolicExpressionTreeNode node, StringBuilder strBuilder) {
    FormatPower(node, strBuilder, "2");
  }

  private void FormatPower(SymbolicExpressionTreeNode node, StringBuilder strBuilder, string exponent) {
    strBuilder.Append("Power[");
    FormatRecursively(node.GetSubtree(0), strBuilder);
    strBuilder.Append($", {exponent}]");
  }

  private void FormatRoot(SymbolicExpressionTreeNode node, StringBuilder strBuilder) {
    strBuilder.Append("Power[");
    FormatRecursively(node.GetSubtree(0), strBuilder);
    strBuilder.Append(", Divide[1,");
    FormatRecursively(node.GetSubtree(1), strBuilder);
    strBuilder.Append("]]");
  }

  private void FormatDivision(SymbolicExpressionTreeNode node, StringBuilder strBuilder) {
    if (node.SubtreeCount == 1) {
      strBuilder.Append("Divide[1, ");
      FormatRecursively(node.GetSubtree(0), strBuilder);
      strBuilder.Append("]");
    } else {
      strBuilder.Append("Divide[");
      FormatRecursively(node.GetSubtree(0), strBuilder);
      strBuilder.Append(", Times[");
      FormatRecursively(node.GetSubtree(1), strBuilder);
      for (var i = 2; i < node.SubtreeCount; i++) {
        strBuilder.Append(",");
        FormatRecursively(node.GetSubtree(i), strBuilder);
      }

      strBuilder.Append("]]");
    }
  }

  private void FormatFunction(SymbolicExpressionTreeNode node, string function, StringBuilder strBuilder) {
    strBuilder.Append(function + "[");
    foreach (var child in node.Subtrees) {
      FormatRecursively(child, strBuilder);
      if (child != node.Subtrees.Last())
        strBuilder.Append(", ");
    }

    strBuilder.Append("]");
  }
}
