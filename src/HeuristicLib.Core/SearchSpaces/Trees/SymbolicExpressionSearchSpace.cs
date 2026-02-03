using HEAL.HeuristicLib.Encodings.SymbolicExpressionTree.Grammars;
using HEAL.HeuristicLib.Optimization;

namespace HEAL.HeuristicLib.Encodings.SymbolicExpressionTree;

public record SymbolicExpressionSearchSpace(ISymbolicExpressionGrammar Grammar, int TreeLength = 50, int TreeDepth = 50)
  : SearchSpace<SymbolicExpressionTree> {
  #region Parameter properties
  public int TreeLength { get; set; } = TreeLength;

  public int TreeDepth { get; set; } = TreeDepth;

  public ISymbolicExpressionGrammar Grammar { get; set; } = Grammar;

  public int FunctionDefinitions { get; set; }

  public int FunctionArguments { get; set; }
  #endregion

  public override bool Contains(SymbolicExpressionTree genotype) {
    return genotype.Length <= TreeLength &&
           genotype.Depth <= TreeDepth
           && Grammar.Conforms(genotype)
      ;
  }
}
