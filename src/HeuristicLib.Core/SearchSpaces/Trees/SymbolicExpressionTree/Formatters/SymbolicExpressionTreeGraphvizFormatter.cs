using System.Text;
using HEAL.HeuristicLib.Genotypes.Trees;
using HEAL.HeuristicLib.SearchSpaces.Trees.SymbolicExpressionTree.Symbols;
using HEAL.HeuristicLib.SearchSpaces.Trees.SymbolicExpressionTree.Symbols.Math;

namespace HEAL.HeuristicLib.SearchSpaces.Trees.SymbolicExpressionTree.Formatters;

public sealed class SymbolicExpressionTreeGraphvizFormatter : ISymbolicExpressionTreeStringFormatter
{
  public bool Indent { get; set; }

  public SymbolicExpressionTreeGraphvizFormatter() => Indent = true;

  public string Format(Genotypes.Trees.SymbolicExpressionTree symbolicExpressionTree)
  {
    var nodeCounter = 1;
    var strBuilder = new StringBuilder();
    strBuilder.AppendLine("graph {");
    strBuilder.AppendLine(FormatRecursively(symbolicExpressionTree.Root, 0, ref nodeCounter));
    strBuilder.AppendLine("}");
    return strBuilder.ToString();
  }

  private string FormatRecursively(SymbolicExpressionTreeNode node, int indentLength, ref int nodeId)
  {
    // save id of current node
    var currentNodeId = nodeId;
    // increment id for next node
    nodeId++;

    var strBuilder = new StringBuilder();
    if (Indent) {
      strBuilder.Append(' ', indentLength);
    }

    // get label for node and map if necessary

    var nodeLabel = node.GetType().ToString();
    var sym = node.Symbol;
    nodeLabel = sym switch {
      ProgramRootSymbol => "PRog",
      StartSymbol => "RPB",
      Addition => "+",
      Subtraction => "-",
      Multiplication => "*",
      Division => "/",
      Absolute => "abs",
      AnalyticQuotient => "AQ",
      Sine => "sin",
      Cosine => "cos",
      Tangent => "tan",
      HyperbolicTangent => "tanh",
      Exponential => "exp",
      Logarithm => "log",
      SquareRoot => "sqrt",
      Square => "sqr",
      CubeRoot => "cbrt",
      Cube => "cube",
      GreaterThan => ">",
      LessThan => "<",
      Variable => $"{((VariableTreeNode)node).Weight} * {((VariableTreeNode)node).VariableName}",
      Number => $"{((NumberTreeNode)node).Value}",
      _ => nodeLabel
    };

    //switch (node) {
    // match Koza style
    //  case(ProgramRootSymbol): return "Prog";
    ////  {nameof(StartSymbol), "RPB"},

    ////  // short form 
    ////  {"Subtraction", "-" },
    ////  {"Addition", "+" },

    ////}; 
    //  default: return "unknown";

    strBuilder.Append(nameof(node) + currentNodeId + "[label=\"" + nodeLabel + "\"");
    // leaf nodes should have box shape
    strBuilder.AppendLine(node.SubtreeCount == 0 ? ", shape=\"box\"];" : "];");

    // internal nodes or leaf nodes?
    foreach (var subTree in node.Subtrees) {
      // add an edge 
      if (Indent) {
        strBuilder.Append(' ', indentLength);
      }

      strBuilder.AppendLine(nameof(node) + currentNodeId + " -- node" + nodeId + ";");
      // format the whole subtree
      strBuilder.Append(FormatRecursively(subTree, indentLength + 2, ref nodeId));
    }

    return strBuilder.ToString();
  }
}
