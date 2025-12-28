using System.Text;
using HEAL.HeuristicLib.Encodings.Trees.SymbolicExpressionTree.Formatters;
using HEAL.HeuristicLib.Encodings.Trees.SymbolicExpressionTree.Symbols;
using HEAL.HeuristicLib.Encodings.Trees.SymbolicExpressionTree.Symbols.Math;
using HEAL.HeuristicLib.Encodings.Trees.SymbolicExpressionTree.Symbols.Math.Variables;
using HEAL.HeuristicLib.Genotypes.Trees;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems.DataAnalysis.Symbolic;

namespace HEAL.HeuristicLib.Problems.DataAnalysis.Formatter;

public sealed class SymbolicDataAnalysisExpressionLatexFormatter : ISymbolicExpressionTreeStringFormatter {
  private readonly List<KeyValuePair<string, double>> parameters = [];
  private int paramIdx;
  private int targetCount;
  private int currentLag;
  private string? targetVariable;
  private bool containsTimeSeriesSymbol;

  public string Format(SymbolicExpressionTree symbolicExpressionTree) {
    return Format(symbolicExpressionTree, null);
  }

  public string Format(SymbolicExpressionTree symbolicExpressionTree, string? targetVariable) {
    try {
      StringBuilder strBuilder = new StringBuilder();
      parameters.Clear();
      paramIdx = 0;
      this.targetVariable = targetVariable;
      containsTimeSeriesSymbol = symbolicExpressionTree.IterateNodesBreadth().Any(n => IsTimeSeriesSymbol(n.Symbol));
      strBuilder.AppendLine(FormatRecursively(symbolicExpressionTree.Root));
      return strBuilder.ToString();
    }
    catch (NotImplementedException ex) {
      return ex.Message + Environment.NewLine + ex.StackTrace;
    }
  }

  static bool IsTimeSeriesSymbol(Symbol s) {
    return s is TimeLag or Integral or Derivative or LaggedVariable;
  }

  private string FormatRecursively(SymbolicExpressionTreeNode node) {
    StringBuilder strBuilder = new StringBuilder();
    currentLag = 0;
    FormatBegin(node, strBuilder);

    if (node.SubtreeCount > 0) {
      strBuilder.Append(FormatRecursively(node.GetSubtree(0)));
    }

    int i = 1;
    foreach (var subTree in node.Subtrees.Skip(1)) {
      FormatSep(node, strBuilder, i);
      // format the whole subtree
      strBuilder.Append(FormatRecursively(subTree));
      i++;
    }

    FormatEnd(node, strBuilder);

    return strBuilder.ToString();
  }

  private void FormatBegin(SymbolicExpressionTreeNode node, StringBuilder strBuilder) {
    switch (node.Symbol) {
      case Addition:
        strBuilder.Append(@" \left( ");
        break;
      case Subtraction when node.SubtreeCount == 1:
        strBuilder.Append(@"- \left( ");
        break;
      case Subtraction:
        strBuilder.Append(@" \left( ");
        break;
      case Multiplication:
        break;
      case Division when node.SubtreeCount == 1:
        strBuilder.Append(@" \cfrac{1}{");
        break;
      case Division:
        strBuilder.Append(@" \cfrac{ ");
        break;
      case Absolute:
        strBuilder.Append(@"\operatorname{abs} \left( ");
        break;
      case AnalyticQuotient:
        strBuilder.Append(@" \frac { ");
        break;
      case Average: {
        // skip output of (1/1) if only one subtree
        if (node.SubtreeCount > 1) {
          strBuilder.Append(@" \cfrac{1}{" + node.SubtreeCount + @"}");
        }

        strBuilder.Append(@" \left( ");
        break;
      }
      case Logarithm:
        strBuilder.Append(@"\log \left( ");
        break;
      case Exponential:
        strBuilder.Append(@"\exp \left( ");
        break;
      case Square:
        strBuilder.Append(@"\left(");
        break;
      case SquareRoot:
        strBuilder.Append(@"\sqrt{");
        break;
      case Cube:
        strBuilder.Append(@"\left(");
        break;
      case CubeRoot:
        strBuilder.Append(@"\operatorname{cbrt}\left(");
        break;
      case Sine:
        strBuilder.Append(@"\sin \left( ");
        break;
      case Cosine:
        strBuilder.Append(@"\cos \left( ");
        break;
      case Tangent:
        strBuilder.Append(@"\tan \left( ");
        break;
      case HyperbolicTangent:
        strBuilder.Append(@"\tanh \left( ");
        break;
      case AiryA:
        strBuilder.Append(@"\operatorname{airy}_a \left( ");
        break;
      case AiryB:
        strBuilder.Append(@"\operatorname{airy}_b \left( ");
        break;
      case Bessel:
        strBuilder.Append(@"\operatorname{bessel}_1 \left( ");
        break;
      case CosineIntegral:
        strBuilder.Append(@"\operatorname{cosInt} \left( ");
        break;
      case Dawson:
        strBuilder.Append(@"\operatorname{dawson} \left( ");
        break;
      case Erf:
        strBuilder.Append(@"\operatorname{erf} \left( ");
        break;
      case ExponentialIntegralEi:
        strBuilder.Append(@"\operatorname{expInt}_i \left( ");
        break;
      case FresnelCosineIntegral:
        strBuilder.Append(@"\operatorname{fresnel}_\operatorname{cosInt} \left( ");
        break;
      case FresnelSineIntegral:
        strBuilder.Append(@"\operatorname{fresnel}_\operatorname{sinInt} \left( ");
        break;
      case Gamma:
        strBuilder.Append(@"\Gamma \left( ");
        break;
      case HyperbolicCosineIntegral:
        strBuilder.Append(@"\operatorname{hypCosInt} \left( ");
        break;
      case HyperbolicSineIntegral:
        strBuilder.Append(@"\operatorname{hypSinInt} \left( ");
        break;
      case Norm:
        strBuilder.Append(@"\operatorname{norm} \left( ");
        break;
      case Psi:
        strBuilder.Append(@"\operatorname{digamma} \left( ");
        break;
      case SineIntegral:
        strBuilder.Append(@"\operatorname{sinInt} \left( ");
        break;
      case GreaterThan:
      case LessThan:
        strBuilder.Append(@"  \left( ");
        break;
      case And:
        strBuilder.Append(@"  \left( \left( ");
        break;
      case Or:
        strBuilder.Append(@"   \left( \left( ");
        break;
      case Not:
        strBuilder.Append(@" \neg \left( ");
        break;
      case IfThenElse:
        strBuilder.Append(@" \operatorname{if}  \left( ");
        break;
      default: {
        if (node is NumberTreeNode numericNode) {
          var numName = "c_{" + paramIdx + "}";
          paramIdx++;
          if (numericNode.Value.IsAlmost(1.0)) {
            strBuilder.Append("1 ");
          } else {
            strBuilder.Append(numName);
            parameters.Add(new KeyValuePair<string, double>(numName, numericNode.Value));
          }
        } else if (node.Symbol is FactorVariable) {
          var factorNode = (FactorVariableTreeNode)node;
          var paramName = "c_{" + paramIdx + "}";
          strBuilder.Append(paramName + " ");
          foreach (var e in factorNode.Symbol.GetVariableValues(factorNode.VariableName)
                                      .Zip(factorNode.Weights, Tuple.Create)) {
            parameters.Add(new KeyValuePair<string, double>("c_{" + paramIdx + ", " + EscapeLatexString(factorNode.VariableName) + "=" + EscapeLatexString(e.Item1) + "}", e.Item2));
          }

          paramIdx++;
        } else
          switch (node) {
            case BinaryFactorVariableTreeNode binFactorNode: {
              if (!binFactorNode.Weight.IsAlmost((1.0))) {
                var paramName = "c_{" + paramIdx + "}";
                strBuilder.Append(paramName + "  \\cdot");
                parameters.Add(new KeyValuePair<string, double>(paramName, binFactorNode.Weight));
                paramIdx++;
              }

              strBuilder.Append("(" + EscapeLatexString(binFactorNode.VariableName));
              strBuilder.Append(LagToString(currentLag));
              strBuilder.Append(" = " + EscapeLatexString(binFactorNode.VariableValue) + " )");
              break;
            }
            case LaggedVariableTreeNode laggedVarNode: {
              if (!laggedVarNode.Weight.IsAlmost(1.0)) {
                var paramName = "c_{" + paramIdx + "}";
                strBuilder.Append(paramName + "  \\cdot");
                parameters.Add(new KeyValuePair<string, double>(paramName, laggedVarNode.Weight));
                paramIdx++;
              }

              strBuilder.Append(EscapeLatexString(laggedVarNode.VariableName));
              strBuilder.Append(LagToString(currentLag + laggedVarNode.Lag));
              break;
            }
            case VariableTreeNode varNode: {
              if (!varNode.Weight.IsAlmost(1.0)) {
                var paramName = "c_{" + paramIdx + "}";
                strBuilder.Append(paramName + "  \\cdot");
                parameters.Add(new KeyValuePair<string, double>(paramName, varNode.Weight));
                paramIdx++;
              }

              strBuilder.Append(EscapeLatexString(varNode.VariableName));
              strBuilder.Append(LagToString(currentLag));
              break;
            }
            default:
              switch (node.Symbol) {
                case ProgramRootSymbol:
                  strBuilder
                    .AppendLine("\\begin{align*}")
                    .AppendLine("\\nonumber");
                  break;
                //else if (node.Symbol is Defun) {
                //  var defunNode = node as DefunTreeNode;
                //  strBuilder.Append(defunNode.FunctionName + " & = ");
                //} else if (node.Symbol is InvokeFunction) {
                //  var invokeNode = node as InvokeFunctionTreeNode;
                //  strBuilder.Append(invokeNode.Symbol.FunctionName + @" \left( ");
                //} else if (node.Symbol is StartSymbol) {
                //  FormatStartSymbol(strBuilder);
                //} else if (node.Symbol is Argument) {
                //  var argSym = node.Symbol as Argument;
                //  strBuilder.Append(" ARG+" + argSym.ArgumentIndex + " ");
                //} 
                case Derivative:
                  strBuilder.Append(@" \cfrac{d \left( ");
                  break;
                case TimeLag: {
                  var laggedNode = (LaggedTreeNode)node;
                  currentLag += laggedNode.Lag;
                  break;
                }
                case Power or Root:
                  strBuilder.Append(@" \left( ");
                  break;
                case Integral: {
                  // actually a new variable for t is needed in all subtrees (TODO)
                  var laggedTreeNode = (LaggedTreeNode)node;
                  strBuilder.Append(@"\sum_{t=" + (laggedTreeNode.Lag + currentLag) + @"}^0 \left( ");
                  break;
                }
                case VariableCondition: {
                  var conditionTreeNode = (VariableConditionTreeNode)node;
                  var paramName = "c_{" + parameters.Count + "}";
                  string p = @"1 /  1 + \exp  - " + paramName + " ";
                  parameters.Add(new KeyValuePair<string, double>(paramName, conditionTreeNode.Slope));
                  paramIdx++;
                  var const2Name = "c_{" + parameters.Count + @"}";
                  p += @" \cdot " + EscapeLatexString(conditionTreeNode.VariableName) + LagToString(currentLag) + " - " + const2Name + "   ";
                  parameters.Add(new KeyValuePair<string, double>(const2Name, conditionTreeNode.Threshold));
                  paramIdx++;
                  strBuilder.Append(@" \left( " + p + @"\cdot ");
                  break;
                }
                case SubFunctionSymbol:
                  // to nothing, skip symbol
                  break;
                default:
                  throw new NotImplementedException("Export of " + node.Symbol + " is not implemented.");
              }

              break;
          }

        break;
      }
    }
  }

  private void FormatSep(SymbolicExpressionTreeNode node, StringBuilder strBuilder, int step) {
    switch (node.Symbol) {
      case Addition:
        strBuilder.Append(" + ");
        break;
      case Subtraction:
        strBuilder.Append(" - ");
        break;
      case Multiplication:
        strBuilder.Append(@" \cdot ");
        break;
      case Division when step + 1 == node.SubtreeCount:
        strBuilder.Append(@"}{");
        break;
      case Division:
        strBuilder.Append(@" }{ \cfrac{ ");
        break;
      case Absolute:
        throw new InvalidOperationException();
      case AnalyticQuotient:
        strBuilder.Append(@"}{\sqrt{1 + \left( ");
        break;
      case Average:
        strBuilder.Append(@" + ");
        break;
      case Logarithm:
      case Exponential:
      case Square:
      case SquareRoot:
      case Cube:
      case CubeRoot:
      case Sine:
      case Cosine:
      case Tangent:
      case HyperbolicTangent:
      case AiryA:
      case AiryB:
      case Bessel:
      case CosineIntegral:
      case Dawson:
      case Erf:
      case ExponentialIntegralEi:
      case FresnelCosineIntegral:
      case FresnelSineIntegral:
      case Gamma:
      case HyperbolicCosineIntegral:
      case HyperbolicSineIntegral:
      case Norm:
      case Psi:
      case SineIntegral:
        throw new InvalidOperationException();
      case GreaterThan:
        strBuilder.Append(@" > ");
        break;
      case LessThan:
        strBuilder.Append(@" < ");
        break;
      case And:
        strBuilder.Append(@" > 0  \right) \land \left(");
        break;
      case Or:
        strBuilder.Append(@" > 0  \right) \lor \left(");
        break;
      case Not:
        throw new InvalidOperationException();
      case IfThenElse:
        strBuilder.Append(@" , ");
        break;
      case ProgramRootSymbol:
        strBuilder.Append(@"\\" + Environment.NewLine);
        break;
      default: {
        //if (node.Symbol is Defun) { } else if (node.Symbol is InvokeFunction) {
        //  strBuilder.Append(" , ");
        //  break;
        //}
        switch (node.Symbol) {
          case StartSymbol:
            strBuilder.Append(@"\\" + Environment.NewLine);
            FormatStartSymbol(strBuilder);
            break;
          case Power:
            strBuilder.Append(@"\right) ^ { \operatorname{round} \left(");
            break;
          case Root:
            strBuilder.Append(@"\right) ^ {  \cfrac{1}{ \operatorname{round} \left(");
            break;
          case VariableCondition: {
            var conditionTreeNode = (VariableConditionTreeNode)node;
            var const1Name = "c_{" + parameters.Count + "}";
            string p = @"1 / \left( 1 + \exp \left( - " + const1Name + " ";
            parameters.Add(new KeyValuePair<string, double>(const1Name, conditionTreeNode.Slope));
            paramIdx++;
            var const2Name = "c_{" + parameters.Count + "}";
            p += @" \cdot " + EscapeLatexString(conditionTreeNode.VariableName) + LagToString(currentLag) + " - " + const2Name + " \right) \right) \right)   ";
            parameters.Add(new KeyValuePair<string, double>(const2Name, conditionTreeNode.Threshold));
            paramIdx++;
            strBuilder.Append(@" +  \left( 1 - " + p + @" \right) \cdot ");
            break;
          }
          case SubFunctionSymbol:
            throw new InvalidOperationException();
          default:
            throw new NotImplementedException("Export of " + node.Symbol + " is not implemented.");
        }

        break;
      }
    }
  }

  private void FormatEnd(SymbolicExpressionTreeNode node, StringBuilder strBuilder) {
    switch (node.Symbol) {
      case Addition:
      case Subtraction:
        strBuilder.Append(@" \right) ");
        break;
      case Multiplication:
        break;
      case Division: {
        strBuilder.Append(" } ");
        for (int i = 2; i < node.SubtreeCount; i++)
          strBuilder.Append(" } ");
        break;
      }
      case Absolute:
        strBuilder.Append(@" \right)");
        break;
      case AnalyticQuotient:
        strBuilder.Append(@" \right)^2}}");
        break;
      case Average:
      case Logarithm:
      case Exponential:
        strBuilder.Append(@" \right) ");
        break;
      case Square:
        strBuilder.Append(@"\right)^2");
        break;
      case SquareRoot:
        strBuilder.Append(@"}");
        break;
      case Cube:
        strBuilder.Append(@"\right)^3");
        break;
      case CubeRoot:
        strBuilder.Append(@"\right)");
        break;
      case Sine:
      case Cosine:
      case Tangent:
      case HyperbolicTangent:
      case AiryA:
      case AiryB:
      case Bessel:
      case CosineIntegral:
      case Dawson:
      case Erf:
      case ExponentialIntegralEi:
      case FresnelCosineIntegral:
      case FresnelSineIntegral:
      case Gamma:
      case HyperbolicCosineIntegral:
      case HyperbolicSineIntegral:
      case Norm:
      case Psi:
      case SineIntegral:
      case GreaterThan:
      case LessThan:
        strBuilder.Append(@" \right) ");
        break;
      case And:
      case Or:
        strBuilder.Append(@" > 0 \right) \right) ");
        break;
      case Not:
      case IfThenElse:
        strBuilder.Append(@" \right) ");
        break;
      case Number:
      case Constant:
      case LaggedVariable:
      case Variable:
      case FactorVariable:
      case BinaryFactorVariable:
        break;
      case ProgramRootSymbol: {
        strBuilder
          .AppendLine("\\end{align*}")
          .AppendLine("\\begin{align*}")
          .AppendLine("\\nonumber");
        // output all parameter values
        if (parameters.Count > 0) {
          foreach (var param in parameters) {
            // replace "." with ".&" to align decimal points
            var paramStr = string.Format(System.Globalization.NumberFormatInfo.InvariantInfo, "{0:G5}", param.Value);
            if (!paramStr.Contains("."))
              paramStr = paramStr + ".0";
            paramStr = paramStr.Replace(".", "&."); // fix problem in rendering of aligned expressions
            strBuilder.Append(param.Key + "& = & " + paramStr);
            strBuilder.Append(@"\\");
          }
        }

        strBuilder.AppendLine("\\end{align*}");
        break;
      }
      default: {
        //if (node.Symbol is Defun) { } else 
        switch (node.Symbol) {
          case InvokeFunction:
            strBuilder.Append(@" \right) ");
            break;
          case StartSymbol:
            break;
          case Argument:
            break;
          case Derivative:
            strBuilder.Append(@" \right) }{dt} ");
            break;
          case TimeLag: {
            var laggedNode = node as LaggedTreeNode;
            currentLag -= laggedNode.Lag;
            break;
          }
          case Power:
            strBuilder.Append(@" \right) } ");
            break;
          case Root:
            strBuilder.Append(@" \right) } } ");
            break;
          case Integral:
            strBuilder.Append(@" \right) ");
            break;
          case VariableCondition:
            strBuilder.Append(@"\right) ");
            break;
          case SubFunctionSymbol:
            break;
          default:
            throw new NotImplementedException("Export of " + node.Symbol + " is not implemented.");
        }

        break;
      }
    }
  }

  private void FormatStartSymbol(StringBuilder strBuilder) {
    strBuilder.Append(targetVariable != null ? EscapeLatexString(targetVariable) : "\\text{target}_{" + targetCount++ + "}");
    if (containsTimeSeriesSymbol)
      strBuilder.Append("(t)");
    strBuilder.Append(" & = ");
  }

  private static string LagToString(int lag) {
    return lag switch {
      < 0 => "(t" + lag + ")",
      > 0 => "(t+" + lag + ")",
      _ => ""
    };
  }

  private static string EscapeLatexString(string s) {
    return "\\text{" +
           s
             .Replace("\\", @"\\")
             .Replace("{", "\\{")
             .Replace("}", "\\}")
           + "}";
  }
}
