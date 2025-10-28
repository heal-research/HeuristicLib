using System.Text;
using HEAL.HeuristicLib.Encodings.SymbolicExpression;

namespace HEAL.HeuristicLib.Operators.SymbolicExpression.Formatters;

public class SymbolicExpressionTreeStringFormatter : ISymbolicExpressionTreeStringFormatter {
  public bool Indent {
    get;
    set;
  } = true;

  public string Format(SymbolicExpressionTree symbolicExpressionTree) {
    return FormatRecursively(symbolicExpressionTree.Root, 0);
  }

  private string FormatRecursively(SymbolicExpressionTreeNode node, int indentLength) {
    var strBuilder = new StringBuilder();
    if (Indent)
      strBuilder.Append(' ', indentLength);
    strBuilder.Append('(');
    // internal nodes or leaf nodes?
    if (node.Subtrees.Any()) {
      // symbol on same line as '('
      strBuilder.AppendLine(node.Symbol.ToString());
      // each subtree expression on a new line
      // and closing ')' also on new line
      foreach (var subtree in node.Subtrees) {
        strBuilder.AppendLine(FormatRecursively(subtree, indentLength + 2));
      }

      if (Indent)
        strBuilder.Append(' ', indentLength);
    } else {
      // symbol in the same line with as '(' and ')'
      strBuilder.Append(node);
    }

    strBuilder.Append(')');
    return strBuilder.ToString();
  }
}
