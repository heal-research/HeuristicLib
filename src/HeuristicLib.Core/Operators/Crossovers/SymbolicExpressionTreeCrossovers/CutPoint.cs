using HEAL.HeuristicLib.Genotypes.Trees;
using HEAL.HeuristicLib.SearchSpaces.Trees;
using HEAL.HeuristicLib.SearchSpaces.Trees.SymbolicExpressionTree.Grammars;

namespace HEAL.HeuristicLib.Operators.Crossovers.SymbolicExpressionTreeCrossovers;

public class CutPoint
{
  private readonly ISymbolicExpressionGrammar grammar;

  public CutPoint(SymbolicExpressionTreeNode parent, SymbolicExpressionTreeNode child, SymbolicExpressionSearchSpace encoding)
  {
    Parent = parent;
    Child = child;
    ChildIndex = parent.IndexOfSubtree(child);
    grammar = encoding.Grammar;
  }

  public CutPoint(SymbolicExpressionTreeNode parent, int childIndex, SymbolicExpressionSearchSpace encoding)
  {
    Parent = parent;
    ChildIndex = childIndex;
    Child = null;
    grammar = encoding.Grammar;
  }
  public SymbolicExpressionTreeNode Parent { get; }
  public SymbolicExpressionTreeNode? Child { get; }

  public int ChildIndex { get; }

  public bool IsMatchingPointType(SymbolicExpressionTreeNode? newChild)
  {
    if (newChild == null) {
      // make sure that one subtree can be removed and that only the last subtree is removed 
      return grammar.GetMinimumSubtreeCount(Parent.Symbol) < Parent.SubtreeCount &&
             ChildIndex == Parent.SubtreeCount - 1;
    }

    // check syntax constraints of direct parent - child relation
    if (!grammar.ContainsSymbol(newChild.Symbol) ||
        !grammar.IsAllowedChildSymbol(Parent.Symbol, newChild.Symbol, ChildIndex)) {
      return false;
    }

    var result = true;
    // check point type for the whole branch
    newChild.ForEachNodePostfix(n => {
      result =
        result &&
        grammar.ContainsSymbol(n.Symbol) &&
        n.SubtreeCount >= grammar.GetMinimumSubtreeCount(n.Symbol) &&
        n.SubtreeCount <= grammar.GetMaximumSubtreeCount(n.Symbol);
    });

    return result;
  }
}
