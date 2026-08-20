using HEAL.HeuristicLib.Genotypes.Trees;
using HEAL.HeuristicLib.SearchSpaces.Trees;

namespace HEAL.HeuristicLib.Operators.Crossovers.SymbolicExpressionTreeCrossovers;

public abstract record SymbolicExpressionTreeCrossover : SingleCandidateCrossover<SymbolicExpressionTree, SymbolicExpressionTreeSearchSpace>;
