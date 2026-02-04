using HEAL.HeuristicLib.SearchSpaces.Trees.SymbolicExpressionTree.Grammars;

namespace HEAL.HeuristicLib.SearchSpaces.Trees;

public record SymbolicExpressionTreeSearchSpace(ISymbolicExpressionGrammar Grammar, int TreeLength = 50, int TreeDepth = 50)
  : SearchSpace<Genotypes.Trees.SymbolicExpressionTree>
{
  #region Parameter properties

  public int TreeLength { get; set; } = TreeLength;

  public int TreeDepth { get; set; } = TreeDepth;

  public ISymbolicExpressionGrammar Grammar { get; set; } = Grammar;

  public int FunctionDefinitions { get; set; }

  public int FunctionArguments { get; set; }

  #endregion

  public override bool Contains(Genotypes.Trees.SymbolicExpressionTree genotype)
  {
    return genotype.Length <= TreeLength &&
           genotype.Depth <= TreeDepth
           && Grammar.Conforms(genotype)
      ;
  }
}
