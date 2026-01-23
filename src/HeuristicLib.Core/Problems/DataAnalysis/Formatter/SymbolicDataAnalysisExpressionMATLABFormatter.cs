using System.Globalization;
using System.Text;
using HEAL.HeuristicLib.Genotypes.Trees;
using HEAL.HeuristicLib.SearchSpaces.Trees.SymbolicExpressionTree.Formatters;
using HEAL.HeuristicLib.SearchSpaces.Trees.SymbolicExpressionTree.Symbols;
using HEAL.HeuristicLib.SearchSpaces.Trees.SymbolicExpressionTree.Symbols.Math;
using HEAL.HeuristicLib.SearchSpaces.Trees.SymbolicExpressionTree.Symbols.Math.Variables;

namespace HEAL.HeuristicLib.Problems.DataAnalysis.Formatter;

public sealed class SymbolicDataAnalysisExpressionMatlabFormatter : ISymbolicExpressionTreeStringFormatter
{
  private int currentLag;

  private int currentIndexNumber;
  public string CurrentIndexVariable => "i" + currentIndexNumber;

  private void ReleaseIndexVariable() => currentIndexNumber--;

  private string AllocateIndexVariable()
  {
    currentIndexNumber++;
    return CurrentIndexVariable;
  }

  public string Format(SymbolicExpressionTree symbolicExpressionTree)
  {
    currentLag = 0;
    currentIndexNumber = 0;

    var stringBuilder = new StringBuilder();
    stringBuilder.AppendLine("rows = ???");
    stringBuilder.AppendLine(FormatOnlyExpression(symbolicExpressionTree.Root) + ";");
    stringBuilder.AppendLine();
    stringBuilder.AppendLine("function y = log_(x)");
    stringBuilder.AppendLine("  if(x<=0) y = NaN;");
    stringBuilder.AppendLine("  else     y = log(x);");
    stringBuilder.AppendLine("  end");
    stringBuilder.AppendLine("end");
    stringBuilder.AppendLine();
    stringBuilder.AppendLine("function y = fivePoint(f0, f1, f3, f4)");
    stringBuilder.AppendLine("  y = (f0 + 2*f1 - 2*f3 - f4) / 8;");
    stringBuilder.AppendLine("end");

    var factorVariableNames =
      symbolicExpressionTree.IterateNodesPostfix()
        .OfType<FactorVariableTreeNode>()
        .Select(n => n.VariableName)
        .Distinct();

    foreach (var factorVarName in factorVariableNames) {
      var factorSymb = symbolicExpressionTree.IterateNodesPostfix()
        .OfType<FactorVariableTreeNode>()
        .First(n => n.VariableName == factorVarName)
        .Symbol;
      stringBuilder.Append($"function y = switch_{factorVarName}(val, v)").AppendLine();
      var values = factorSymb.GetVariableValues(factorVarName).ToArray();
      stringBuilder.AppendLine("switch val");
      for (var i = 0; i < values.Length; i++) {
        stringBuilder.Append(CultureInfo.InvariantCulture, $"  case \"{values[i]}\" y = v({i})").AppendLine();
      }

      stringBuilder.AppendLine("end");
      stringBuilder.AppendLine();
    }

    return stringBuilder.ToString();
  }

  public string FormatOnlyExpression(SymbolicExpressionTreeNode expressionNode)
  {
    var stringBuilder = new StringBuilder();
    stringBuilder.AppendLine("  for " + CurrentIndexVariable + " = 1:1:rows");
    stringBuilder.AppendLine("    estimated(" + CurrentIndexVariable + ") = " + FormatRecursively(expressionNode.GetSubtree(0)) + ";");
    stringBuilder.AppendLine("  end;");
    return stringBuilder.ToString();
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
        stringBuilder.Append($"abs({FormatRecursively(node.GetSubtree(0))})");
        break;
      case AnalyticQuotient:
        stringBuilder.Append($"({FormatRecursively(node.GetSubtree(0))}) / sqrt(1 + ({FormatRecursively(node.GetSubtree(1))}).^2)");
        break;
      case And: {
        stringBuilder.Append("((");
        for (var i = 0; i < node.SubtreeCount; i++) {
          if (i > 0) {
            stringBuilder.Append('&');
          }

          stringBuilder.Append("((");
          stringBuilder.Append(FormatRecursively(node.GetSubtree(i)));
          stringBuilder.Append(")>0)");
        }

        stringBuilder.Append(")-0.5)*2");
        // MATLAB maps false and true to 0 and 1, resp., we map this result to -1.0 and +1.0, resp.
        break;
      }
      case Average: {
        stringBuilder.Append("(1/");
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

        stringBuilder.Append(")");
        break;
      }
      case Number: {
        var numberTreeNode = (NumberTreeNode)node;
        stringBuilder.Append(numberTreeNode.Value.ToString(CultureInfo.InvariantCulture));
        break;
      }
      case Cosine:
        stringBuilder.Append("cos(");
        stringBuilder.Append(FormatRecursively(node.GetSubtree(0)));
        stringBuilder.Append(")");
        break;
      case Division when node.SubtreeCount == 1:
        stringBuilder.Append("1/");
        stringBuilder.Append(FormatRecursively(node.GetSubtree(0)));
        break;
      case Division: {
        stringBuilder.Append(FormatRecursively(node.GetSubtree(0)));
        stringBuilder.Append("/(");
        for (var i = 1; i < node.SubtreeCount; i++) {
          if (i > 1) {
            stringBuilder.Append("*");
          }

          stringBuilder.Append(FormatRecursively(node.GetSubtree(i)));
        }

        stringBuilder.Append(")");
        break;
      }
      case Exponential:
        stringBuilder.Append("exp(");
        stringBuilder.Append(FormatRecursively(node.GetSubtree(0)));
        stringBuilder.Append(")");
        break;
      case Square:
        stringBuilder.Append("(");
        stringBuilder.Append(FormatRecursively(node.GetSubtree(0)));
        stringBuilder.Append(").^2");
        break;
      case SquareRoot:
        stringBuilder.Append("sqrt(");
        stringBuilder.Append(FormatRecursively(node.GetSubtree(0)));
        stringBuilder.Append(")");
        break;
      case Cube:
        stringBuilder.Append("(");
        stringBuilder.Append(FormatRecursively(node.GetSubtree(0)));
        stringBuilder.Append(").^3");
        break;
      case CubeRoot:
        stringBuilder.Append("NTHROOT(");
        stringBuilder.Append(FormatRecursively(node.GetSubtree(0)));
        stringBuilder.Append(", 3)");
        break;
      case GreaterThan:
        stringBuilder.Append("((");
        stringBuilder.Append(FormatRecursively(node.GetSubtree(0)));
        stringBuilder.Append(">");
        stringBuilder.Append(FormatRecursively(node.GetSubtree(1)));
        stringBuilder.Append(")-0.5)*2");
        // MATLAB maps false and true to 0 and 1, resp., we map this result to -1.0 and +1.0, resp.
        break;
      case IfThenElse:
        stringBuilder.Append("(");
        stringBuilder.Append(FormatRecursively(node.GetSubtree(0)));
        stringBuilder.Append(">0)*");
        stringBuilder.Append(FormatRecursively(node.GetSubtree(1)));
        stringBuilder.Append("+");
        stringBuilder.Append("(");
        stringBuilder.Append(FormatRecursively(node.GetSubtree(0)));
        stringBuilder.Append("<=0)*");
        stringBuilder.Append(FormatRecursively(node.GetSubtree(2)));
        break;
      case LaggedVariable: {
        // this if must be checked before if(symbol is LaggedVariable)
        var laggedVariableTreeNode = (LaggedVariableTreeNode)node;
        stringBuilder.Append(laggedVariableTreeNode.Weight.ToString(CultureInfo.InvariantCulture));
        stringBuilder.Append('*');
        stringBuilder.Append(laggedVariableTreeNode.VariableName +
                             LagToString(currentLag + laggedVariableTreeNode.Lag));
        break;
      }
      case LessThan:
        stringBuilder.Append("((");
        stringBuilder.Append(FormatRecursively(node.GetSubtree(0)));
        stringBuilder.Append("<");
        stringBuilder.Append(FormatRecursively(node.GetSubtree(1)));
        stringBuilder.Append(")-0.5)*2");
        // MATLAB maps false and true to 0 and 1, resp., we map this result to -1.0 and +1.0, resp.
        break;
      case Logarithm:
        stringBuilder.Append("log_(");
        stringBuilder.Append(FormatRecursively(node.GetSubtree(0)));
        stringBuilder.Append(")");
        break;
      case Multiplication: {
        for (var i = 0; i < node.SubtreeCount; i++) {
          if (i > 0) {
            stringBuilder.Append("*");
          }

          stringBuilder.Append(FormatRecursively(node.GetSubtree(i)));
        }

        break;
      }
      case Not:
        stringBuilder.Append("~(");
        stringBuilder.Append(FormatRecursively(node.GetSubtree(0)));
        stringBuilder.Append(" > 0 )");
        break;
      case Or: {
        stringBuilder.Append("((");
        for (var i = 0; i < node.SubtreeCount; i++) {
          if (i > 0) {
            stringBuilder.Append("|");
          }

          stringBuilder.Append("((");
          stringBuilder.Append(FormatRecursively(node.GetSubtree(i)));
          stringBuilder.Append(")>0)");
        }

        stringBuilder.Append(")-0.5)*2");
        // MATLAB maps false and true to 0 and 1, resp., we map this result to -1.0 and +1.0, resp.
        break;
      }
      case Sine:
        stringBuilder.Append("sin(");
        stringBuilder.Append(FormatRecursively(node.GetSubtree(0)));
        stringBuilder.Append(")");
        break;
      case Subtraction: {
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
        break;
      }
      case Tangent:
        stringBuilder.Append("tan(");
        stringBuilder.Append(FormatRecursively(node.GetSubtree(0)));
        stringBuilder.Append(")");
        break;
      case HyperbolicTangent:
        stringBuilder.Append("tanh(");
        stringBuilder.Append(FormatRecursively(node.GetSubtree(0)));
        stringBuilder.Append(")");
        break;
      default: {
        if (node.Symbol is AiryA) {
          stringBuilder.Append("airy(");
          stringBuilder.Append(FormatRecursively(node.GetSubtree(0)));
          stringBuilder.Append(")");
        } else if (node.Symbol is AiryB) {
          stringBuilder.Append("airy(2, ");
          stringBuilder.Append(FormatRecursively(node.GetSubtree(0)));
          stringBuilder.Append(")");
        } else if (node.Symbol is Bessel) {
          stringBuilder.Append("besseli(0.0,");
          stringBuilder.Append(FormatRecursively(node.GetSubtree(0)));
          stringBuilder.Append(")");
        } else if (node.Symbol is CosineIntegral) {
          stringBuilder.Append("cosint(");
          stringBuilder.Append(FormatRecursively(node.GetSubtree(0)));
          stringBuilder.Append(")");
        } else if (node.Symbol is Dawson) {
          stringBuilder.Append("dawson(");
          stringBuilder.Append(FormatRecursively(node.GetSubtree(0)));
          stringBuilder.Append(")");
        } else if (node.Symbol is Erf) {
          stringBuilder.Append("erf(");
          stringBuilder.Append(FormatRecursively(node.GetSubtree(0)));
          stringBuilder.Append(")");
        } else if (node.Symbol is ExponentialIntegralEi) {
          stringBuilder.Append("expint(");
          stringBuilder.Append(FormatRecursively(node.GetSubtree(0)));
          stringBuilder.Append(")");
        } else if (node.Symbol is FresnelCosineIntegral) {
          stringBuilder.Append("FresnelC(");
          stringBuilder.Append(FormatRecursively(node.GetSubtree(0)));
          stringBuilder.Append(")");
        } else if (node.Symbol is FresnelSineIntegral) {
          stringBuilder.Append("FresnelS(");
          stringBuilder.Append(FormatRecursively(node.GetSubtree(0)));
          stringBuilder.Append(")");
        } else if (node.Symbol is Gamma) {
          stringBuilder.Append("gamma(");
          stringBuilder.Append(FormatRecursively(node.GetSubtree(0)));
          stringBuilder.Append(")");
        } else if (node.Symbol is HyperbolicCosineIntegral) {
          stringBuilder.Append("Chi(");
          stringBuilder.Append(FormatRecursively(node.GetSubtree(0)));
          stringBuilder.Append(")");
        } else if (node.Symbol is HyperbolicSineIntegral) {
          stringBuilder.Append("Shi(");
          stringBuilder.Append(FormatRecursively(node.GetSubtree(0)));
          stringBuilder.Append(")");
        } else if (node.Symbol is Norm) {
          stringBuilder.Append("normpdf(");
          stringBuilder.Append(FormatRecursively(node.GetSubtree(0)));
          stringBuilder.Append(")");
        } else if (node.Symbol is Psi) {
          stringBuilder.Append("psi(");
          stringBuilder.Append(FormatRecursively(node.GetSubtree(0)));
          stringBuilder.Append(")");
        } else if (node.Symbol is SineIntegral) {
          stringBuilder.Append("sinint(");
          stringBuilder.Append(FormatRecursively(node.GetSubtree(0)));
          stringBuilder.Append(")");
        } else if (symbol is Variable) {
          var variableTreeNode = (VariableTreeNode)node;
          stringBuilder.Append(variableTreeNode.Weight.ToString(CultureInfo.InvariantCulture));
          stringBuilder.Append("*");
          stringBuilder.Append(variableTreeNode.VariableName + LagToString(currentLag));
        } else if (symbol is FactorVariable) {
          var factorNode = (FactorVariableTreeNode)node;
          var weights = string.Join(" ", factorNode.Weights!.Select(w => w.ToString("G17", CultureInfo.InvariantCulture)));
          stringBuilder.Append($"switch_{factorNode.VariableName}(\"{factorNode.VariableName}\",[{weights}])")
            .AppendLine();
        } else if (symbol is BinaryFactorVariable) {
          var factorNode = (BinaryFactorVariableTreeNode)node;
          stringBuilder.Append(CultureInfo.InvariantCulture,
            $"((strcmp({factorNode.VariableName},\"{factorNode.VariableValue}\")==1) * {factorNode.Weight:G17})");
        } else if (symbol is Power) {
          stringBuilder.Append('(');
          stringBuilder.Append(FormatRecursively(node.GetSubtree(0)));
          stringBuilder.Append(")^round(");
          stringBuilder.Append(FormatRecursively(node.GetSubtree(1)));
          stringBuilder.Append(')');
        } else if (symbol is Root) {
          stringBuilder.Append('(');
          stringBuilder.Append(FormatRecursively(node.GetSubtree(0)));
          stringBuilder.Append(")^(1 / round(");
          stringBuilder.Append(FormatRecursively(node.GetSubtree(1)));
          stringBuilder.Append("))");
        } else if (symbol is Derivative) {
          stringBuilder.Append("fivePoint(");
          // f0
          stringBuilder.Append(FormatRecursively(node.GetSubtree(0)));
          stringBuilder.Append(", ");
          // f1
          currentLag--;
          stringBuilder.Append(FormatRecursively(node.GetSubtree(0)));
          stringBuilder.Append(", ");
          // f3
          currentLag -= 2;
          stringBuilder.Append(FormatRecursively(node.GetSubtree(0)));
          stringBuilder.Append(", ");
          currentLag--;
          // f4
          stringBuilder.Append(FormatRecursively(node.GetSubtree(0)));
          stringBuilder.Append(')');
          currentLag += 4;
        } else if (symbol is Integral) {
          var laggedNode = (LaggedTreeNode)node;
          var prevCounterVariable = CurrentIndexVariable;
          var counterVariable = AllocateIndexVariable();
          stringBuilder.AppendLine(" sum (map(@(" + counterVariable + ") " + FormatRecursively(node.GetSubtree(0)) +
                                   ", (" + prevCounterVariable + "+" + laggedNode.Lag + "):" + prevCounterVariable +
                                   "))");
          ReleaseIndexVariable();
        } else if (symbol is TimeLag) {
          var laggedNode = (LaggedTreeNode)node;
          currentLag += laggedNode.Lag;
          stringBuilder.Append(FormatRecursively(node.GetSubtree(0)));
          currentLag -= laggedNode.Lag;
        } else if (symbol is SubFunctionSymbol) {
          stringBuilder.Append(FormatRecursively(node.GetSubtree(0)));
        } else {
          stringBuilder.Append("ERROR");
        }

        break;
      }
    }

    return stringBuilder.ToString();
  }

  private string LagToString(int lag)
  {
    return lag switch {
      < 0 => "(" + CurrentIndexVariable + "" + lag + ")",
      > 0 => "(" + CurrentIndexVariable + "+" + lag + ")",
      _ => "(" + CurrentIndexVariable + ")"
    };
  }
}
