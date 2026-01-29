using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using HEAL.HeuristicLib.Encodings.SymbolicExpressionTree;
using HEAL.HeuristicLib.Encodings.SymbolicExpressionTree.Formatters;
using HEAL.HeuristicLib.Encodings.SymbolicExpressionTree.Symbols.Math;
using HEAL.HeuristicLib.Encodings.SymbolicExpressionTree.Symbols.Math.Variables;

namespace HEAL.HeuristicLib.Problems.DataAnalysis.Formatter;

public sealed partial class CSharpSymbolicExpressionTreeStringFormatter : SymbolicExpressionTreeStringFormatter {
  public string Format(SymbolicExpressionTree symbolicExpressionTree) {
    // skip root and start symbols
    var strBuilder = new StringBuilder();
    GenerateHeader(strBuilder, symbolicExpressionTree);
    FormatRecursively(symbolicExpressionTree.Root.GetSubtree(0).GetSubtree(0), strBuilder);
    GenerateFooter(strBuilder);
    return strBuilder.ToString();
  }

  private static string VariableName2Identifier(string name) {
    /*
     * identifier-start-character:
     *    letter-character
     *    _ (the underscore character U+005F)
     *  identifier-part-characters:
     *    identifier-part-character
     *    identifier-part-characters   identifier-part-character
     *  identifier-part-character:
     *    letter-character
     *    decimal-digit-character
     *    connecting-character
     *    combining-character
     *    formatting-character
     *  letter-character:
     *    A Unicode character of classes Lu, Ll, Lt, Lm, Lo, or Nl
     *    A unicode-escape-sequence representing a character of classes Lu, Ll, Lt, Lm, Lo, or Nl
     *  combining-character:
     *    A Unicode character of classes Mn or Mc
     *    A unicode-escape-sequence representing a character of classes Mn or Mc
     *  decimal-digit-character:
     *    A Unicode character of the class Nd
     *    A unicode-escape-sequence representing a character of the class Nd
     *  connecting-character:
     *    A Unicode character of the class Pc
     *    A unicode-escape-sequence representing a character of the class Pc
     *  formatting-character:
     *    A Unicode character of the class Cf
     *    A unicode-escape-sequence representing a character of the class Cf
     */

    var invalidIdentifierStarts = InvalidIdentifierStartsRegex();
    var invalidIdentifierParts = new Regex(@"[^\p{Lu}\p{Ll}\p{Lt}\p{Lm}\p{Lo}\p{Nl}\p{Mn}\p{Mc}\p{Nd}\p{Pc}\p{Cf}]");
    return "@" +
           (invalidIdentifierStarts.IsMatch(name.AsSpan(0, 1)) ? "_" : "") + // prepend '_' if necessary
           invalidIdentifierParts.Replace(name, "_");
  }

  private void FormatRecursively(SymbolicExpressionTreeNode node, StringBuilder strBuilder) {
    // TODO: adapt to interpreter semantics. The HL interpreter also allows Boolean operations on reals
    if (node.Subtrees.Any()) {
      switch (node.Symbol) {
        case Addition:
          FormatOperator(node, "+", strBuilder);
          break;
        case And:
          FormatOperator(node, "&&", strBuilder);
          break;
        case Average:
          FormatFunction(node, "Average", strBuilder);
          break;
        case Cosine:
          FormatFunction(node, "Math.Cos", strBuilder);
          break;
        case Division:
          FormatDivision(node, strBuilder);
          break;
        case Exponential:
          FormatFunction(node, "Math.Exp", strBuilder);
          break;
        case GreaterThan:
          FormatOperator(node, ">", strBuilder);
          break;
        case IfThenElse:
          FormatFunction(node, "EvaluateIf", strBuilder);
          break;
        case LessThan:
          FormatOperator(node, "<", strBuilder);
          break;
        case Logarithm:
          FormatFunction(node, "Math.Log", strBuilder);
          break;
        case Multiplication:
          FormatOperator(node, "*", strBuilder);
          break;
        case Not:
          FormatOperator(node, "!", strBuilder);
          break;
        case Or:
          FormatOperator(node, "||", strBuilder);
          break;
        case Xor:
          FormatOperator(node, "^", strBuilder);
          break;
        case Sine:
          FormatFunction(node, "Math.Sin", strBuilder);
          break;
        case Subtraction:
          FormatSubtraction(node, strBuilder);
          break;
        case Tangent:
          FormatFunction(node, "Math.Tan", strBuilder);
          break;
        case HyperbolicTangent:
          FormatFunction(node, "Math.Tanh", strBuilder);
          break;
        case Square:
          FormatSquare(node, strBuilder);
          break;
        case SquareRoot:
          FormatFunction(node, "Math.Sqrt", strBuilder);
          break;
        case Cube:
          FormatPower(node, strBuilder, "3");
          break;
        case CubeRoot:
          strBuilder.Append("Cbrt(");
          FormatRecursively(node.GetSubtree(0), strBuilder);
          strBuilder.Append(")");
          break;
        case Power:
          FormatFunction(node, "Math.Pow", strBuilder);
          break;
        case Root:
          FormatRoot(node, strBuilder);
          break;
        case Absolute:
          FormatFunction(node, "Math.Abs", strBuilder);
          break;
        case AnalyticQuotient:
          strBuilder.Append("(");
          FormatRecursively(node.GetSubtree(0), strBuilder);
          strBuilder.Append(" / Math.Sqrt(1 + Math.Pow(");
          FormatRecursively(node.GetSubtree(1), strBuilder);
          strBuilder.Append(" , 2) ) )");
          break;
        case SubFunctionSymbol:
          FormatRecursively(node.GetSubtree(0), strBuilder);
          break;
        default:
          throw new NotSupportedException("Formatting of symbol: " + node.Symbol + " not supported for C# symbolic expression tree formatter.");
      }
    } else {
      if (node is VariableTreeNode) {
        var varNode = node as VariableTreeNode;
        strBuilder.AppendFormat("{0} * {1}", VariableName2Identifier(varNode.VariableName), varNode.Weight.ToString("g17", CultureInfo.InvariantCulture));
      } else if (node is NumberTreeNode numNode) {
        strBuilder.Append(numNode.Value.ToString("g17", CultureInfo.InvariantCulture));
      } else if (node.Symbol is FactorVariable) {
        var factorNode = node as FactorVariableTreeNode;
        FormatFactor(factorNode, strBuilder);
      } else if (node.Symbol is BinaryFactorVariable) {
        var binFactorNode = node as BinaryFactorVariableTreeNode;
        FormatBinaryFactor(binFactorNode, strBuilder);
      } else {
        throw new NotSupportedException("Formatting of symbol: " + node.Symbol + " not supported for C# symbolic expression tree formatter.");
      }
    }
  }

  private void FormatFactor(FactorVariableTreeNode node, StringBuilder strBuilder) {
    strBuilder.AppendFormat("EvaluateFactor({0}, new [] {{ {1} }}, new [] {{ {2} }})", VariableName2Identifier(node.VariableName),
      string.Join(",", node.Symbol.GetVariableValues(node.VariableName).Select(name => "\"" + name + "\"")), string.Join(",", node.Weights.Select(v => v.ToString(CultureInfo.InvariantCulture))));
  }

  private void FormatBinaryFactor(BinaryFactorVariableTreeNode node, StringBuilder strBuilder) {
    strBuilder.AppendFormat(CultureInfo.InvariantCulture, "EvaluateBinaryFactor({0}, \"{1}\", {2})", VariableName2Identifier(node.VariableName), node.VariableValue, node.Weight);
  }

  private void FormatSquare(SymbolicExpressionTreeNode node, StringBuilder strBuilder) {
    FormatPower(node, strBuilder, "2");
  }

  private void FormatPower(SymbolicExpressionTreeNode node, StringBuilder strBuilder, string exponent) {
    strBuilder.Append("Math.Pow(");
    FormatRecursively(node.GetSubtree(0), strBuilder);
    strBuilder.Append($", {exponent})");
  }

  private void FormatRoot(SymbolicExpressionTreeNode node, StringBuilder strBuilder) {
    strBuilder.Append("Math.Pow(");
    FormatRecursively(node.GetSubtree(0), strBuilder);
    strBuilder.Append(", 1.0 / (");
    FormatRecursively(node.GetSubtree(1), strBuilder);
    strBuilder.Append("))");
  }

  private void FormatDivision(SymbolicExpressionTreeNode node, StringBuilder strBuilder) {
    if (node.SubtreeCount == 1) {
      strBuilder.Append("1.0 / ");
      FormatRecursively(node.GetSubtree(0), strBuilder);
    } else {
      FormatRecursively(node.GetSubtree(0), strBuilder);
      strBuilder.Append("/ (");
      for (var i = 1; i < node.SubtreeCount; i++) {
        if (i > 1) strBuilder.Append(" * ");
        FormatRecursively(node.GetSubtree(i), strBuilder);
      }

      strBuilder.Append(")");
    }
  }

  private void FormatSubtraction(SymbolicExpressionTreeNode node, StringBuilder strBuilder) {
    if (node.SubtreeCount == 1) {
      strBuilder.Append("-");
      FormatRecursively(node.GetSubtree(0), strBuilder);
      return;
    }

    //Default case: more than 1 child
    FormatOperator(node, "-", strBuilder);
  }

  private void FormatOperator(SymbolicExpressionTreeNode node, string symbol, StringBuilder strBuilder) {
    strBuilder.Append("(");
    foreach (var child in node.Subtrees) {
      FormatRecursively(child, strBuilder);
      if (child != node.Subtrees.Last())
        strBuilder.Append(" " + symbol + " ");
    }

    strBuilder.Append(")");
  }

  private void FormatFunction(SymbolicExpressionTreeNode node, string function, StringBuilder strBuilder) {
    strBuilder.Append(function + "(");
    foreach (var child in node.Subtrees) {
      FormatRecursively(child, strBuilder);
      if (child != node.Subtrees.Last())
        strBuilder.Append(", ");
    }

    strBuilder.Append(")");
  }

  private void GenerateHeader(StringBuilder strBuilder, SymbolicExpressionTree symbolicExpressionTree) {
    strBuilder.AppendLine("using System;");
    strBuilder.AppendLine("using System.Linq;" + Environment.NewLine);
    strBuilder.AppendLine("namespace HeuristicLab.Models {");
    strBuilder.AppendLine("public static class Model {");
    GenerateAverageSource(strBuilder);
    GenerateCbrtSource(strBuilder);
    GenerateIfThenElseSource(strBuilder);
    GenerateFactorSource(strBuilder);
    GenerateBinaryFactorSource(strBuilder);
    strBuilder.Append(Environment.NewLine + "public static double PredictAndTrain (");

    // here we don't have access to problemData to determine the type for each variable (double/string) therefore we must distinguish based on the symbol type
    var doubleVarNames = new HashSet<string>();
    doubleVarNames.UnionWith(symbolicExpressionTree.IterateNodesPostfix().OfType<VariableTreeNode>().Select(n => n.VariableName));
    doubleVarNames.UnionWith(symbolicExpressionTree.IterateNodesPostfix().OfType<VariableConditionTreeNode>().Select(n => n.VariableName));

    var stringVarNames = new HashSet<string>();

    stringVarNames.UnionWith(symbolicExpressionTree.IterateNodesPostfix().OfType<BinaryFactorVariableTreeNode>().Select(n => n.VariableName));
    stringVarNames.UnionWith(symbolicExpressionTree.IterateNodesPostfix().OfType<FactorVariableTreeNode>().Select(n => n.VariableName));

    var orderedNames = stringVarNames.OrderBy(n => n, new NaturalStringComparer()).Select(n => "string " + VariableName2Identifier(n) + " /* " + n + " */");
    strBuilder.Append(string.Join(", ", orderedNames));

    if (stringVarNames.Any() && doubleVarNames.Any())
      strBuilder.AppendLine(",");
    orderedNames = doubleVarNames.OrderBy(n => n, new NaturalStringComparer()).Select(n => "double " + VariableName2Identifier(n) + " /* " + n + " */");
    strBuilder.Append(string.Join(", ", orderedNames));

    strBuilder.AppendLine(") {");
    strBuilder.Append("double result = ");
  }

  private void GenerateFooter(StringBuilder strBuilder) {
    strBuilder.AppendLine(";");

    strBuilder.AppendLine("return result;");
    strBuilder.AppendLine("}");
    strBuilder.AppendLine("}");
    strBuilder.AppendLine("}");
  }

  private void GenerateAverageSource(StringBuilder strBuilder) {
    strBuilder.AppendLine("private static double Average(params double[] values) {");
    strBuilder.AppendLine("  return values.Average();");
    strBuilder.AppendLine("}");
  }

  private void GenerateCbrtSource(StringBuilder strBuilder) {
    strBuilder.AppendLine("private static double Cbrt(double x) {");
    strBuilder.AppendLine("  return x < 0 ? -Math.Pow(-x, 1.0 / 3.0) : Math.Pow(x, 1.0 / 3.0);");
    strBuilder.AppendLine("}");
  }

  private void GenerateIfThenElseSource(StringBuilder strBuilder) {
    strBuilder.AppendLine("private static double EvaluateIf(bool condition, double then, double @else) {");
    strBuilder.AppendLine("   return condition ? then : @else;");
    strBuilder.AppendLine("}");
  }

  private void GenerateFactorSource(StringBuilder strBuilder) {
    strBuilder.AppendLine("private static double EvaluateFactor(string factorValue, string[] factorValues, double[] parameters) {");
    strBuilder.AppendLine("   for(int i=0;i<factorValues.Length;i++) " +
                          "      if(factorValues[i] == factorValue) return parameters[i];" +
                          "   throw new ArgumentException();");
    strBuilder.AppendLine("}");
  }

  private void GenerateBinaryFactorSource(StringBuilder strBuilder) {
    strBuilder.AppendLine("private static double EvaluateBinaryFactor(string factorValue, string targetValue, double weight) {");
    strBuilder.AppendLine("  return factorValue == targetValue ? weight : 0.0;");
    strBuilder.AppendLine("}");
  }

  [GeneratedRegex(@"[^_\p{Lu}\p{Ll}\p{Lt}\p{Lm}\p{Lo}\p{Nl}]")]
  private static partial Regex InvalidIdentifierStartsRegex();
}
