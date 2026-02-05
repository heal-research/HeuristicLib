using HEAL.HeuristicLib.Genotypes.Trees;
using HEAL.HeuristicLib.SearchSpaces.Trees;

namespace HEAL.HeuristicLib.Operators.Creators.SymbolicExpressionTreeCreators;

public abstract class SymbolicExpressionTreeCreator 
  : SingleSolutionStatelessCreator<SymbolicExpressionTree, SymbolicExpressionTreeSearchSpace>;
