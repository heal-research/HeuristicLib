using HEAL.HeuristicLib.SearchSpaces.Trees.SymbolicExpressionTree.Grammars;

namespace HEAL.HeuristicLib.SearchSpaces.Trees;

public record SymbolicExpressionTreeSearchSpace : SearchSpace<Genotypes.Trees.SymbolicExpressionTree>
{
  public SymbolicExpressionTreeSearchSpace(ISymbolicExpressionGrammar Grammar, int TreeLength = 50, int TreeDepth = 50)
  {
    this.TreeLength = TreeLength;
    this.TreeDepth = TreeDepth;
    this.Grammar = Grammar;
  }

  public int TreeLength { get; set; }

  public int TreeDepth { get; set; }

  public ISymbolicExpressionGrammar Grammar { get; set; }

  public int FunctionDefinitions { get; set; }

  public int FunctionArguments { get; set; }

  public override bool Contains(Genotypes.Trees.SymbolicExpressionTree genotype)
  {
    return genotype.Length <= TreeLength &&
      genotype.Depth <= TreeDepth
      && Grammar.Conforms(genotype)
      ;
  }
}
