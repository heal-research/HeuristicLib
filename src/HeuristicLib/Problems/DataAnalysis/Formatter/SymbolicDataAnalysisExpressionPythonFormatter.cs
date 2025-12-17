using System.Globalization;
using System.Text;
using HEAL.HeuristicLib.Encodings.SymbolicExpressionTree;
using HEAL.HeuristicLib.Encodings.SymbolicExpressionTree.Formatters;
using HEAL.HeuristicLib.Encodings.SymbolicExpressionTree.Symbols;
using HEAL.HeuristicLib.Encodings.SymbolicExpressionTree.Symbols.Math;

namespace HEAL.HeuristicLib.Problems.DataAnalysis.Formatter;

public sealed class SymbolicDataAnalysisExpressionPythonFormatter : ISymbolicExpressionTreeStringFormatter {
  public string Format(SymbolicExpressionTree symbolicExpressionTree) {
    var strBuilderModel = new StringBuilder();
    var header = GenerateHeader(symbolicExpressionTree);
    FormatRecursively(symbolicExpressionTree.Root, strBuilderModel);
    return $"{header}{strBuilderModel}";
  }

  public static string FormatTree(SymbolicExpressionTree symbolicExpressionTree) {
    var formatter = new SymbolicDataAnalysisExpressionPythonFormatter();
    return formatter.Format(symbolicExpressionTree);
  }

  private static string GenerateHeader(SymbolicExpressionTree symbolicExpressionTree) {
    var strBuilder = new StringBuilder();

    var variables = new HashSet<string>();
    var mathLibCounter = 0;
    var statisticLibCounter = 0;
    var evaluateIfCounter = 0;

    // iterate tree and search for necessary imports and variable names
    foreach (var node in symbolicExpressionTree.IterateNodesPostfix()) {
      var symbol = node.Symbol;
      switch (symbol) {
        case Average:
          statisticLibCounter++;
          break;
        case IfThenElse:
          evaluateIfCounter++;
          break;
        case Cosine:
        case Exponential:
        case Logarithm:
        case Sine:
        case Tangent:
        case HyperbolicTangent:
        case SquareRoot:
        case Power:
        case AnalyticQuotient:
          mathLibCounter++;
          break;
        default: {
          if (node is VariableTreeNode varNode) {
            var formattedVariable = VariableName2Identifier(varNode.VariableName);
            variables.Add(formattedVariable);
          }

          break;
        }
      }
    }

    // generate import section (if necessary)
    var importSection = GenerateNecessaryImports(mathLibCounter, statisticLibCounter);
    strBuilder.Append(importSection);

    // generate if-then-else helper construct (if necessary)
    var ifThenElseSourceSection = GenerateIfThenElseSource(evaluateIfCounter);
    strBuilder.Append(ifThenElseSourceSection);

    // generate model evaluation function
    var modelEvaluationFunctionSection = GenerateModelEvaluationFunction(variables);
    strBuilder.Append(modelEvaluationFunctionSection);

    return strBuilder.ToString();
  }

  private static string GenerateNecessaryImports(int mathLibCounter, int statisticLibCounter) {
    var strBuilder = new StringBuilder();
    if (mathLibCounter > 0 || statisticLibCounter > 0) {
      strBuilder.AppendLine("# imports");
      if (mathLibCounter > 0)
        strBuilder.AppendLine("import math");
      if (statisticLibCounter > 0)
        strBuilder.AppendLine("import statistics");
      strBuilder.AppendLine();
    }

    return strBuilder.ToString();
  }

  private static string GenerateIfThenElseSource(int evaluateIfCounter) {
    var strBuilder = new StringBuilder();
    if (evaluateIfCounter > 0) {
      strBuilder.AppendLine("# condition helper function");
      strBuilder.AppendLine("def evaluate_if(condition, then_path, else_path): ");
      strBuilder.AppendLine("\tif condition:");
      strBuilder.AppendLine("\t\treturn then_path");
      strBuilder.AppendLine("\telse:");
      strBuilder.AppendLine("\t\treturn else_path");
    }

    return strBuilder.ToString();
  }

  private static string GenerateModelEvaluationFunction(ISet<string> variables) {
    var strBuilder = new StringBuilder();
    strBuilder.Append("def evaluate(");
    var orderedVariables = variables.OrderBy(n => n, new NaturalStringComparer()).ToArray();
    foreach (var variable in orderedVariables) {
      strBuilder.Append($"{variable}");
      if (variable != orderedVariables[^1])
        strBuilder.Append(", ");
    }

    strBuilder.AppendLine("):");
    strBuilder.Append("\treturn ");
    return strBuilder.ToString();
  }

  private static void FormatRecursively(SymbolicExpressionTreeNode node, StringBuilder strBuilder) {
    var symbol = node.Symbol;
    if (symbol is ProgramRootSymbol)
      FormatRecursively(node.GetSubtree(0), strBuilder);
    else if (symbol is StartSymbol)
      FormatRecursively(node.GetSubtree(0), strBuilder);
    else if (symbol is Absolute)
      FormatNode(node, strBuilder, "abs");
    else if (symbol is Addition)
      FormatNode(node, strBuilder, infixSymbol: " + ");
    else if (symbol is Subtraction)
      FormatSubtraction(node, strBuilder);
    else if (symbol is Multiplication)
      FormatNode(node, strBuilder, infixSymbol: " * ");
    else if (symbol is Division)
      FormatDivision(node, strBuilder);
    else if (symbol is Average)
      FormatNode(node, strBuilder, prefixSymbol: "statistics.mean", openingSymbol: "([", closingSymbol: "])");
    else if (symbol is Sine)
      FormatNode(node, strBuilder, "math.sin");
    else if (symbol is Cosine)
      FormatNode(node, strBuilder, "math.cos");
    else if (symbol is Tangent)
      FormatNode(node, strBuilder, "math.tan");
    else if (symbol is HyperbolicTangent)
      FormatNode(node, strBuilder, "math.tanh");
    else if (symbol is Exponential)
      FormatNode(node, strBuilder, "math.exp");
    else if (symbol is Logarithm)
      FormatNode(node, strBuilder, "math.log");
    else if (symbol is Power)
      FormatNode(node, strBuilder, "math.pow");
    else if (symbol is Root)
      FormatRoot(node, strBuilder);
    else if (symbol is Square)
      FormatPower(node, strBuilder, "2");
    else if (symbol is SquareRoot)
      FormatNode(node, strBuilder, "math.sqrt");
    else if (symbol is Cube)
      FormatPower(node, strBuilder, "3");
    else if (symbol is CubeRoot)
      FormatNode(node, strBuilder, closingSymbol: " ** (1. / 3))");
    else if (symbol is AnalyticQuotient)
      FormatAnalyticQuotient(node, strBuilder);
    else if (symbol is And)
      FormatNode(node, strBuilder, infixSymbol: " and ");
    else if (symbol is Or)
      FormatNode(node, strBuilder, infixSymbol: " or ");
    else if (symbol is Xor)
      FormatNode(node, strBuilder, infixSymbol: " ^ ");
    else if (symbol is Not)
      FormatNode(node, strBuilder, "not");
    else if (symbol is IfThenElse)
      FormatNode(node, strBuilder, "evaluate_if");
    else if (symbol is GreaterThan)
      FormatNode(node, strBuilder, infixSymbol: " > ");
    else if (symbol is LessThan)
      FormatNode(node, strBuilder, infixSymbol: " < ");
    else if (node is VariableTreeNode)
      FormatVariableTreeNode(node, strBuilder);
    else if (node is NumberTreeNode)
      FormatNumericTreeNode(node, strBuilder);
    else if (symbol is SubFunctionSymbol)
      FormatRecursively(node.GetSubtree(0), strBuilder);
    else
      throw new NotSupportedException("Formatting of symbol: " + symbol + " not supported for Python symbolic expression tree formatter.");
  }

  private static string VariableName2Identifier(string variableName) => variableName.Replace(" ", "_");

  private static void FormatNode(SymbolicExpressionTreeNode node, StringBuilder strBuilder, string prefixSymbol = "", string openingSymbol = "(", string closingSymbol = ")", string infixSymbol = ",") {
    strBuilder.Append($"{prefixSymbol}{openingSymbol}");
    foreach (var child in node.Subtrees) {
      FormatRecursively(child, strBuilder);
      if (child != node.Subtrees.Last())
        strBuilder.Append(infixSymbol);
    }

    strBuilder.Append(closingSymbol);
  }

  private static void FormatVariableTreeNode(SymbolicExpressionTreeNode node, StringBuilder strBuilder) {
    var varNode = (VariableTreeNode)node;
    var formattedVariable = VariableName2Identifier(varNode.VariableName);
    var variableWeight = varNode.Weight.ToString("g17", CultureInfo.InvariantCulture);
    strBuilder.Append($"{formattedVariable} * {variableWeight}");
  }

  private static void FormatNumericTreeNode(SymbolicExpressionTreeNode node, StringBuilder strBuilder) {
    var numNode = (NumberTreeNode)node;
    strBuilder.Append(numNode.Value.ToString("g17", CultureInfo.InvariantCulture));
  }

  private static void FormatPower(SymbolicExpressionTreeNode node, StringBuilder strBuilder, string exponent) {
    strBuilder.Append("math.pow(");
    FormatRecursively(node.GetSubtree(0), strBuilder);
    strBuilder.Append($", {exponent})");
  }

  private static void FormatRoot(SymbolicExpressionTreeNode node, StringBuilder strBuilder) {
    strBuilder.Append("math.pow(");
    FormatRecursively(node.GetSubtree(0), strBuilder);
    strBuilder.Append(", 1.0 / (");
    FormatRecursively(node.GetSubtree(1), strBuilder);
    strBuilder.Append("))");
  }

  private static void FormatSubtraction(SymbolicExpressionTreeNode node, StringBuilder strBuilder) {
    if (node.SubtreeCount == 1) {
      strBuilder.Append("-");
      FormatRecursively(node.GetSubtree(0), strBuilder);
      return;
    }

    //Default case: more than 1 child
    FormatNode(node, strBuilder, infixSymbol: " - ");
  }

  private static void FormatDivision(SymbolicExpressionTreeNode node, StringBuilder strBuilder) {
    strBuilder.Append('(');
    if (node.SubtreeCount == 1) {
      strBuilder.Append("1.0 / ");
      FormatRecursively(node.GetSubtree(0), strBuilder);
    } else {
      FormatRecursively(node.GetSubtree(0), strBuilder);
      strBuilder.Append(" / (");
      for (var i = 1; i < node.SubtreeCount; i++) {
        if (i > 1) strBuilder.Append(" * ");
        FormatRecursively(node.GetSubtree(i), strBuilder);
      }

      strBuilder.Append(')');
    }

    strBuilder.Append(')');
  }

  private static void FormatAnalyticQuotient(SymbolicExpressionTreeNode node, StringBuilder strBuilder) {
    strBuilder.Append('(');
    FormatRecursively(node.GetSubtree(0), strBuilder);
    strBuilder.Append(" / math.sqrt(1 + math.pow(");
    FormatRecursively(node.GetSubtree(1), strBuilder);
    strBuilder.Append(" , 2) ) )");
  }
}
