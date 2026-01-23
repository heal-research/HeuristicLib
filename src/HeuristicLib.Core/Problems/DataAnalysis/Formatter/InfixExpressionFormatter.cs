using System.Globalization;
using System.Text;
using HEAL.HeuristicLib.Genotypes.Trees;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.SearchSpaces.Trees.SymbolicExpressionTree.Formatters;
using HEAL.HeuristicLib.SearchSpaces.Trees.SymbolicExpressionTree.Symbols;
using HEAL.HeuristicLib.SearchSpaces.Trees.SymbolicExpressionTree.Symbols.Math;
using HEAL.HeuristicLib.SearchSpaces.Trees.SymbolicExpressionTree.Symbols.Math.Variables;

namespace HEAL.HeuristicLib.Problems.DataAnalysis.Formatter;

public static class BaseInfixExpressionFormatter
{
  /// <summary>
  ///   Performs some basic re-writing steps to simplify the code for formatting. Tree is changed.
  ///   Removes single-argument +, * which have no effect
  ///   Removes SubFunctions (no effect)
  ///   Replaces variables with coefficients by an explicitly multiplication
  ///   Replaces single-argument / with 1 / (..)
  ///   Replaces multi-argument +, *, /, - with nested binary operations
  ///   Rotates operations to remove necessity to group sub-expressions
  /// </summary>
  /// <param name="tree">The tree is changed</param>
  public static void ConvertToBinaryLeftAssoc(SymbolicExpressionTree tree) => ConvertToBinaryLeftAssocRec(tree.Root.GetSubtree(0), tree.Root.GetSubtree(0).GetSubtree(0));

  private static void ConvertToBinaryLeftAssocRec(SymbolicExpressionTreeNode parent, SymbolicExpressionTreeNode n)
  {
    // recurse post-order
    foreach (var subtree in n.Subtrees.ToArray()) {
      ConvertToBinaryLeftAssocRec(n, subtree); // ToArray required as n.Subtrees is changed in method
    }

    if (n is VariableTreeNode varTreeNode && !varTreeNode.Weight.IsAlmost(1.0)) {
      var mul = new Multiplication().CreateTreeNode();
      var num = (NumberTreeNode)new Number().CreateTreeNode();
      num.Value = varTreeNode.Weight;
      varTreeNode.Weight = 1.0;
      parent.ReplaceSubtree(n, mul);
      mul.AddSubtree(num);
      mul.AddSubtree(varTreeNode);
    } else if (n.Symbol is SubFunctionSymbol) {
      parent.ReplaceSubtree(n, n.GetSubtree(0));
    } else {
      switch (n.SubtreeCount) {
        case 1 when n.Symbol is Addition or Multiplication or And or Or or Xor:
          // single-argument addition or multiplication has no effect -> remove
          parent.ReplaceSubtree(n, n.GetSubtree(0));
          break;
        case 1 when n.Symbol is Division:
          // single-argument division is 1/f(x)
          n.InsertSubtree(0, new Constant { Value = 1.0 }.CreateTreeNode());
          break;
        case > 2 when IsLeftAssocOp(n.Symbol): {
          // multi-argument +, -, *, / are the same as multiple binary operations (left-associative)
          var sy = n.Symbol;

          var additionalTrees = n.Subtrees.Skip(2).ToArray();
          while (n.SubtreeCount > 2) {
            n.RemoveSubtree(2); // keep only the first two arguments
          }

          var childIdx = parent.IndexOfSubtree(n);
          parent.RemoveSubtree(childIdx);
          var newChild = n;
          // build a tree bottom to top, each time adding a subtree on the right
          for (var i = 0; i < additionalTrees.Length; i++) {
            var newOp = sy.CreateTreeNode();
            newOp.AddSubtree(newChild);
            newOp.AddSubtree(additionalTrees[i]);
            newChild = newOp;
          }

          parent.InsertSubtree(childIdx, newChild);
          break;
        }
        case 2 when n.GetSubtree(1).SubtreeCount == 2 &&
                    IsAssocOp(n.Symbol) && IsOperator(n.GetSubtree(1).Symbol) &&
                    Priority(n.Symbol) == Priority(n.GetSubtree(1).Symbol): {
          // f(x) <op> (g(x) <op> h(x))) is the same as  (f(x) <op> g(x)) <op> h(x) for associative <op>
          // which is the same as f(x) <op> g(x) <op> h(x) for left-associative <op>
          // The latter version is preferred because we do not need to write the parentheses.
          // rotation:
          //     (op1)              (op2)
          //     /   \              /  \
          //    a    (op2)       (op1)  c 
          //         /  \        /  \
          //        b    c      a    b      
          var op2 = n.GetSubtree(1);
          var b = op2.GetSubtree(0);
          op2.RemoveSubtree(0);
          n.ReplaceSubtree(op2, b);
          parent.ReplaceSubtree(n, op2);
          op2.InsertSubtree(0, n);
          break;
        }
      }
    }
  }

  public static void FormatRecursively(SymbolicExpressionTreeNode node, StringBuilder strBuilder,
    NumberFormatInfo numberFormat, string formatString, List<KeyValuePair<string, double>>? parameters = null)
  {
    switch (node.SubtreeCount) {
      // This method assumes that the tree has been converted to binary and left-assoc form (see ConvertToBinaryLeftAssocRec). 
      // no subtrees
      case 0 when node is VariableTreeNode varNode: {
        if (varNode.Weight != 1.0) { throw new NotSupportedException("Infix formatter does not support variables with coefficients."); }

        AppendVariableName(strBuilder, varNode.VariableName);
        break;
      }
      case 0 when node is NumberTreeNode numNode: {
        var parenthesisRequired = RequiresParenthesis(node.Parent, node);
        if (parenthesisRequired) {
          strBuilder.Append('(');
        }

        AppendNumber(strBuilder, parameters, numNode.Value, formatString, numberFormat);
        if (parenthesisRequired) {
          strBuilder.Append(')');
        }

        break;
      }
      case 0 when node is LaggedVariableTreeNode varNode: {
        if (!varNode.Weight.IsAlmost(1.0)) {
          AppendNumber(strBuilder, parameters, varNode.Weight, formatString, numberFormat);
          strBuilder.Append('*');
        }

        strBuilder.Append("LAG(");
        AppendVariableName(strBuilder, varNode.VariableName);
        strBuilder.Append(", ")
          .Append(numberFormat, $"{varNode.Lag}")
          .Append(')');
        break;
      }
      case 0 when node is FactorVariableTreeNode factorNode: {
        AppendVariableName(strBuilder, factorNode.VariableName);

        strBuilder.Append('[');
        for (var i = 0; i < factorNode.Weights?.Length; i++) {
          if (i > 0) {
            strBuilder.Append(", ");
          }

          AppendNumber(strBuilder, parameters, factorNode.Weights[i], formatString, numberFormat);
        }

        strBuilder.Append(']');
        break;
      }
      case 0: {
        if (node is BinaryFactorVariableTreeNode factorNode) {
          if (!factorNode.Weight.IsAlmost(1.0)) {
            AppendNumber(strBuilder, parameters, factorNode.Weight, formatString, numberFormat);
            strBuilder.Append('*');
          }

          AppendVariableName(strBuilder, factorNode.VariableName);
          strBuilder.Append(" = ");
          AppendVariableName(strBuilder, factorNode.VariableValue);
        }

        break;
      }
      case 1: {
        // only functions and single-argument subtraction (=negation) or NOT are possible here.
        var token = GetToken(node.Symbol);
        // the only operators that are allowed with a single argument
        if (node.Symbol is Subtraction or Not) {
          if (RequiresParenthesis(node.Parent, node)) {
            strBuilder.Append('(');
          }

          strBuilder.Append(token);
          FormatRecursively(node.GetSubtree(0), strBuilder, numberFormat, formatString, parameters);
          if (RequiresParenthesis(node.Parent, node)) {
            strBuilder.Append(')');
          }
        } else if (IsOperator(node.Symbol)) {
          throw new FormatException($"Single-argument version of {node.Symbol} is not supported.");
        } else {
          // function with only one argument
          strBuilder.Append(token);
          strBuilder.Append('(');
          FormatRecursively(node.GetSubtree(0), strBuilder, numberFormat, formatString, parameters);
          strBuilder.Append(')');
        }

        break;
      }
      case > 1: {
        var token = GetToken(node.Symbol);
        // operators
        if (IsOperator(node.Symbol)) {
          var parenthesisRequired = RequiresParenthesis(node.Parent, node);
          if (parenthesisRequired) {
            strBuilder.Append('(');
          }

          FormatRecursively(node.Subtrees.First(), strBuilder, numberFormat, formatString, parameters);

          foreach (var subtree in node.Subtrees.Skip(1)) {
            strBuilder.Append(" ").Append(token).Append(' ');
            FormatRecursively(subtree, strBuilder, numberFormat, formatString, parameters);
          }

          if (parenthesisRequired) {
            strBuilder.Append(')');
          }
        } else {
          // function with multiple arguments (AQ)

          strBuilder.Append(token);
          strBuilder.Append('(');

          FormatRecursively(node.Subtrees.First(), strBuilder, numberFormat, formatString, parameters);
          foreach (var subtree in node.Subtrees.Skip(1)) {
            strBuilder.Append(", ");
            FormatRecursively(subtree, strBuilder, numberFormat, formatString, parameters);
          }

          strBuilder.Append(")");
        }

        break;
      }
    }
  }

  private static int Priority(Symbol symbol)
  {
    return symbol switch {
      Addition or Subtraction or Or or Xor => 1,
      Division or Multiplication or And => 2,
      Power or Not => 3,
      _ => throw new NotSupportedException()
    };
  }

  private static bool RequiresParenthesis(SymbolicExpressionTreeNode? parent, SymbolicExpressionTreeNode child)
  {
    if (child.SubtreeCount > 2 && IsOperator(child.Symbol)) {
      throw new NotSupportedException("Infix formatter does not support operators with more than two children.");
    }

    // Basically: We need a parenthesis for child if the parent symbol binds stronger than child symbol.
    if (parent?.Symbol is null or ProgramRootSymbol or StartSymbol) {
      return false;
    }

    if (IsFunction(parent.Symbol)) {
      return false;
    }

    if (parent is { SubtreeCount: 1, Symbol: Subtraction }) {
      return true;
    }

    if (child.SubtreeCount == 0) {
      return false;
    }

    var parentPrio = Priority(parent.Symbol);
    var childPrio = Priority(child.Symbol);
    if (parentPrio > childPrio) {
      return true;
    }

    if (parentPrio == childPrio) {
      if (IsLeftAssocOp(child.Symbol)) {
        return parent.GetSubtree(0) != child; // (..) required only for right child for left-assoc op
      }

      if (IsRightAssocOp(child.Symbol)) {
        return parent.GetSubtree(1) != child;
      }
    }

    return false;
  }

  private static bool IsFunction(Symbol symbol)
  {
    // functions are formatted in prefix form e.g. sin(...)
    return !IsOperator(symbol) && !IsLeaf(symbol);
  }

  private static bool IsLeaf(Symbol symbol) => symbol.MaximumArity == 0;

  private static bool IsOperator(Symbol symbol) => IsLeftAssocOp(symbol) || IsRightAssocOp(symbol);

  private static bool IsAssocOp(Symbol symbol)
  {
    // (a <op> b) <op> c = a <op> (b <op> c)
    return symbol is Addition or Multiplication or And or Or or Xor;
  }

  private static bool IsLeftAssocOp(Symbol symbol)
  {
    // a <op> b <op> c = (a <op> b) <op> c
    return symbol is Addition or Subtraction or Multiplication or Division or And or Or or Xor;
  }

  private static bool IsRightAssocOp(Symbol symbol)
  {
    // a <op> b <op> c = a <op> (b <op> c)
    // Negation (single-argument subtraction) is also right-assoc, but we do not have a separate symbol for negation.
    return symbol is Not ||
           symbol is Power; // x^y^z = x^(y^z) (as in Fortran or Mathematica)
  }

  private static void AppendNumber(StringBuilder strBuilder, List<KeyValuePair<string, double>>? parameters, double value, string formatString, NumberFormatInfo numberFormat)
  {
    if (parameters != null) {
      var paramKey = $"c_{parameters.Count}";
      strBuilder.Append(CultureInfo.InvariantCulture, $"{paramKey}");
      parameters.Add(new KeyValuePair<string, double>(paramKey, value));
    } else {
      strBuilder.Append(value.ToString(formatString, numberFormat));
    }
  }

  private static void AppendVariableName(StringBuilder strBuilder, string name) => strBuilder.AppendFormat(name.Contains('\'') ? "\"{0}\"" : "'{0}'", name);

  private static string GetToken(Symbol symbol)
  {
    var tok = InfixExpressionParser.KnownSymbols.GetBySecond(symbol).FirstOrDefault();
    if (tok == null) {
      throw new ArgumentException($"Unknown symbol {symbol} found.");
    }

    return tok;
  }
}

/// <summary>
///   Formats mathematical expressions in infix form. E.g. x1 * (3.0 * x2 + x3)
/// </summary>
public sealed class InfixExpressionFormatter : SymbolicExpressionTreeStringFormatter
{
  /// <summary>
  ///   Produces an infix expression for a given expression tree.
  /// </summary>
  /// <param name="symbolicExpressionTree">The tree representation of the expression.</param>
  /// <param name="numberFormat">
  ///   Number format that should be used for parameters (e.g. NumberFormatInfo.InvariantInfo
  ///   (default)).
  /// </param>
  /// <param name="formatString">The format string for parameters (e.g. \"G4\" to limit to 4 digits, default is \"G\")</param>
  /// <returns>Infix expression</returns>
  public static string Format(SymbolicExpressionTree symbolicExpressionTree, NumberFormatInfo numberFormat,
    string formatString = "G")
  {
    // skip root and start symbols
    var strBuilder = new StringBuilder();
    var cleanTree = new SymbolicExpressionTree(symbolicExpressionTree);
    BaseInfixExpressionFormatter.ConvertToBinaryLeftAssoc(cleanTree);
    BaseInfixExpressionFormatter.FormatRecursively(cleanTree.Root.GetSubtree(0).GetSubtree(0),
      strBuilder, numberFormat, formatString);
    return strBuilder.ToString();
  }

  public string Format(SymbolicExpressionTree symbolicExpressionTree) => Format(symbolicExpressionTree, NumberFormatInfo.InvariantInfo);
}

public sealed class InfixExpressionStringFormatter : SymbolicExpressionTreeStringFormatter
{
  public string Format(SymbolicExpressionTree symbolicExpressionTree)
  {
    var strBuilder = new StringBuilder();
    var parameters = new List<KeyValuePair<string, double>>();
    var cleanTree = new SymbolicExpressionTree(symbolicExpressionTree);
    BaseInfixExpressionFormatter.ConvertToBinaryLeftAssoc(cleanTree);
    BaseInfixExpressionFormatter.FormatRecursively(cleanTree.Root.GetSubtree(0).GetSubtree(0),
      strBuilder, NumberFormatInfo.InvariantInfo, "G", parameters);
    strBuilder.Append($"{Environment.NewLine}{Environment.NewLine}");
    var maxDigits = GetDigits(parameters.Count);
    var padding = parameters.Max(x => x.Value.ToString("F12", CultureInfo.InvariantCulture).Length);
    foreach (var param in parameters) {
      var digits = GetDigits(int.Parse(param.Key[2..]));
      strBuilder.Append($"{param.Key}{new string(' ', maxDigits - digits)} = " +
                        string.Format($"{{0,{padding}:F12}}", param.Value, CultureInfo.InvariantCulture) +
                        Environment.NewLine);
    }

    return strBuilder.ToString();
  }

  private static int GetDigits(int x) => x == 0 ? 1 : (int)Math.Floor(Math.Log10(x) + 1);
}
