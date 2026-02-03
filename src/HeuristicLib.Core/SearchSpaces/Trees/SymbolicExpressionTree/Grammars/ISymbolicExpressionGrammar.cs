using HEAL.HeuristicLib.Encodings.SymbolicExpressionTree.Symbols;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Encodings.SymbolicExpressionTree.Grammars;

public interface ISymbolicExpressionGrammar : ISymbolicExpressionGrammarBase {
  ProgramRootSymbol ProgramRootSymbol { get; }
  StartSymbol StartSymbol { get; }

  int MinimumFunctionDefinitions { get; set; }
  int MaximumFunctionDefinitions { get; set; }
  int MinimumFunctionArguments { get; set; }
  int MaximumFunctionArguments { get; set; }

  bool ReadOnly { get; set; }

  bool Conforms(SymbolicExpressionTree symbolicExpressionTree);
}

public static class SymbolicExpressionGrammarExtensions {
  extension(ISymbolicExpressionGrammar grammar) {
    public SymbolicExpressionTree MakeStump(IRandomNumberGenerator random) {
      var rootNode = grammar.ProgramRootSymbol.CreateTreeNode();
      var tree = new SymbolicExpressionTree(rootNode);
      if (rootNode.HasLocalParameters) rootNode.ResetLocalParameters(random);
      var startNode = grammar.StartSymbol.CreateTreeNode();
      if (startNode.HasLocalParameters) startNode.ResetLocalParameters(random);
      rootNode.AddSubtree(startNode);
      tree.Root = rootNode;
      return tree;
    }

    public void AddFullyConnectedSymbols(Symbol root, params ICollection<Symbol> symbols) {
      foreach (var symbol in symbols) {
        grammar.AddSymbol(symbol);
        grammar.AddAllowedChildSymbol(root, symbol, 0);
        if (symbol.MaximumArity == 0)
          continue;
        foreach (var symbol1 in symbols) {
          grammar.AddAllowedChildSymbol(symbol, symbol1);
        }
      }
    }
  }
}
