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
    StringBuilder strBuilder = new StringBuilder();
    FormatRecursively(symbolicExpressionTree.Root.GetSubtree(0).GetSubtree(0), strBuilder);
    return strBuilder.ToString();
  }

  private void FormatRecursively(SymbolicExpressionTreeNode node, StringBuilder strBuilder) {
    if (node.Subtrees.Any()) {
      if (node.Symbol is Addition) {
        FormatFunction(node, "Plus", strBuilder);
      } else if (node.Symbol is Absolute) {
        FormatFunction(node, "Abs", strBuilder);
      } else if (node.Symbol is AnalyticQuotient) {
        strBuilder.Append("[");
        FormatRecursively(node.GetSubtree(0), strBuilder);
        strBuilder.Append("]/Sqrt[ 1 + Power[");
        FormatRecursively(node.GetSubtree(1), strBuilder);
        strBuilder.Append(", 2]]");
      } else if (node.Symbol is Average) {
        FormatAverage(node, strBuilder);
      } else if (node.Symbol is Multiplication) {
        FormatFunction(node, "Times", strBuilder);
      } else if (node.Symbol is Subtraction) {
        FormatSubtraction(node, strBuilder);
      } else if (node.Symbol is Division) {
        FormatDivision(node, strBuilder);
      } else if (node.Symbol is Sine) {
        FormatFunction(node, "Sin", strBuilder);
      } else if (node.Symbol is Cosine) {
        FormatFunction(node, "Cos", strBuilder);
      } else if (node.Symbol is Tangent) {
        FormatFunction(node, "Tan", strBuilder);
      } else if (node.Symbol is HyperbolicTangent) {
        FormatFunction(node, "Tanh", strBuilder);
      } else if (node.Symbol is Exponential) {
        FormatFunction(node, "Exp", strBuilder);
      } else if (node.Symbol is Logarithm) {
        FormatFunction(node, "Log", strBuilder);
      } else if (node.Symbol is IfThenElse) {
        FormatIf(node, strBuilder);
      } else if (node.Symbol is GreaterThan) {
        strBuilder.Append("If[Greater[");
        FormatRecursively(node.GetSubtree(0), strBuilder);
        strBuilder.Append(",");
        FormatRecursively(node.GetSubtree(1), strBuilder);
        strBuilder.Append("], 1, -1]");
      } else if (node.Symbol is LessThan) {
        strBuilder.Append("If[Less[");
        FormatRecursively(node.GetSubtree(0), strBuilder);
        strBuilder.Append(",");
        FormatRecursively(node.GetSubtree(1), strBuilder);
        strBuilder.Append("], 1, -1]");
      } else if (node.Symbol is And) {
        FormatAnd(node, strBuilder);
      } else if (node.Symbol is Not) {
        strBuilder.Append("If[Greater[");
        FormatRecursively(node.GetSubtree(0), strBuilder);
        strBuilder.Append(", 0], -1, 1]");
      } else if (node.Symbol is Or) {
        FormatOr(node, strBuilder);
      } else if (node.Symbol is Xor) {
        FormatXor(node, strBuilder);
      } else if (node.Symbol is Square) {
        FormatSquare(node, strBuilder);
      } else if (node.Symbol is SquareRoot) {
        FormatFunction(node, "Sqrt", strBuilder);
      } else if (node.Symbol is Cube) {
        FormatPower(node, strBuilder, "3");
      } else if (node.Symbol is CubeRoot) {
        strBuilder.Append("CubeRoot[");
        FormatRecursively(node.GetSubtree(0), strBuilder);
        strBuilder.Append("]");
      } else if (node.Symbol is Power) {
        FormatFunction(node, "Power", strBuilder);
      } else if (node.Symbol is Root) {
        FormatRoot(node, strBuilder);
      } else if (node.Symbol is SubFunctionSymbol) {
        FormatRecursively(node.GetSubtree(0), strBuilder);
      } else {
        throw new NotSupportedException("Formatting of symbol: " + node.Symbol + " is not supported.");
      }
    } else {
      switch (node.Symbol) {
        // terminals
        case Variable: {
          var varNode = node as VariableTreeNode;
          strBuilder.Append($"Times[{varNode.VariableName}, {varNode.Weight.ToString("G17", CultureInfo.InvariantCulture)}]");
          break;
        }
        case Number: {
          var numNode = node as NumberTreeNode;
          strBuilder.Append(numNode.Value.ToString("G17", CultureInfo.InvariantCulture));
          break;
        }
        case FactorVariable: {
          var factorNode = node as FactorVariableTreeNode;
          strBuilder.Append($"Switch[{factorNode.VariableName},");
          var varValues = factorNode.Symbol.GetVariableValues(factorNode.VariableName).ToArray();
          var weights = varValues.Select(factorNode.GetValue).ToArray();

          var weightStr = string.Join(", ",
            varValues.Zip(weights, (s, d) => string.Format(CultureInfo.InvariantCulture, "\"{0}\", {1:G17}", s, d)));
          strBuilder.Append(weightStr);
          strBuilder.Append("]");
          break;
        }
        case BinaryFactorVariable: {
          var factorNode = node as BinaryFactorVariableTreeNode;
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
    for (int i = 1; i < node.SubtreeCount; i++) {
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
      for (int i = 2; i < node.SubtreeCount; i++) {
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
