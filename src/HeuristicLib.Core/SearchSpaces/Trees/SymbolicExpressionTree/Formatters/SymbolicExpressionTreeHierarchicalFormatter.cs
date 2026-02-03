using HEAL.HeuristicLib.Genotypes.Trees;

namespace HEAL.HeuristicLib.SearchSpaces.Trees.SymbolicExpressionTree.Formatters;

public sealed class SymbolicExpressionTreeHierarchicalFormatter : ISymbolicExpressionTreeStringFormatter
{
  public string Format(Genotypes.Trees.SymbolicExpressionTree symbolicExpressionTree)
  {
    var sw = new StringWriter();
    RenderTree(sw, symbolicExpressionTree);

    return sw.ToString();
  }

  private static void RenderTree(TextWriter writer, Genotypes.Trees.SymbolicExpressionTree tree) => RenderNode(writer, tree.Root, string.Empty);

  public static void RenderNode(TextWriter writer, SymbolicExpressionTreeNode node, string prefix)
  {
    var label = node.ToString() ?? "";
    writer.Write(label);
    if (node.SubtreeCount <= 0) {
      writer.WriteLine();

      return;
    }

    var padding = prefix + new string(' ', label.Length);
    for (var i = 0; i != node.SubtreeCount; ++i) {
      char connector;
      char extender;
      if (i == 0) {
        if (node.SubtreeCount > 1) {
          connector = RenderChars.JunctionDown;
          extender = RenderChars.VerticalLine;
        } else {
          connector = RenderChars.HorizontalLine;
          extender = ' ';
        }
      } else {
        writer.Write(padding);
        if (i == node.SubtreeCount - 1) {
          connector = RenderChars.CornerRight;
          extender = ' ';
        } else {
          connector = RenderChars.JunctionRight;
          extender = RenderChars.VerticalLine;
        }
      }

      writer.Write(string.Concat(connector, RenderChars.HorizontalLine));
      var newPrefix = string.Concat(padding, extender, ' ');
      RenderNode(writer, node[i], newPrefix);
    }
  }

  // helper class providing characters for displaying a tree in the console
  public static class RenderChars
  {
    public const char JunctionDown = '┬';
    public const char HorizontalLine = '─';
    public const char VerticalLine = '│';
    public const char JunctionRight = '├';
    public const char CornerRight = '└';
  }
}
