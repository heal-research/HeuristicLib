<<<<<<<< HEAD:src/HeuristicLib.Core/SearchSpaces/Trees/SymbolicExpressionTreeSearchSpace.cs
﻿using HEAL.HeuristicLib.SearchSpaces.Trees.SymbolicExpressionTree.Grammars;

namespace HEAL.HeuristicLib.SearchSpaces.Trees;

public record SymbolicExpressionTreeSearchSpace(ISymbolicExpressionGrammar Grammar, int TreeLength = 50, int TreeDepth = 50)
  : SearchSpace<Genotypes.Trees.SymbolicExpressionTree>
{
========
﻿using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.SearchSpaces.Trees.SymbolicExpressionTree.Grammars;

namespace HEAL.HeuristicLib.SearchSpaces.Trees;

public record SymbolicExpressionSearchSpace(ISymbolicExpressionGrammar Grammar, int TreeLength = 50, int TreeDepth = 50)
  : SearchSpace<Genotypes.Trees.SymbolicExpressionTree>
{

  public override bool Contains(Genotypes.Trees.SymbolicExpressionTree genotype)
  {
    return genotype.Length <= TreeLength &&
           genotype.Depth <= TreeDepth
           && Grammar.Conforms(genotype)
      ;
  }

>>>>>>>> main:src/HeuristicLib.Core/SearchSpaces/Trees/SymbolicExpressionSearchSpace.cs
  #region Parameter properties

  public int TreeLength { get; set; } = TreeLength;

  public int TreeDepth { get; set; } = TreeDepth;

  public ISymbolicExpressionGrammar Grammar { get; set; } = Grammar;

  public int FunctionDefinitions { get; set; }

  public int FunctionArguments { get; set; }

  #endregion

<<<<<<<< HEAD:src/HeuristicLib.Core/SearchSpaces/Trees/SymbolicExpressionTreeSearchSpace.cs
  public override bool Contains(Genotypes.Trees.SymbolicExpressionTree genotype)
  {
    return genotype.Length <= TreeLength &&
           genotype.Depth <= TreeDepth
           && Grammar.Conforms(genotype)
      ;
  }
========
>>>>>>>> main:src/HeuristicLib.Core/SearchSpaces/Trees/SymbolicExpressionSearchSpace.cs
}
