namespace HEAL.HeuristicLib.SearchSpaces.Trees.SymbolicExpressionTree.Symbols.Math.Wildcards;

public class AnyNodeSymbol(int minimumArity, int maximumArity) : Symbol(minimumArity, minimumArity, maximumArity);
