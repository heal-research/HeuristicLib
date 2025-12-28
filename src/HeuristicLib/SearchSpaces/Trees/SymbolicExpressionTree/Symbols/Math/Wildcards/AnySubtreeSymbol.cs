namespace HEAL.HeuristicLib.SearchSpaces.Trees.SymbolicExpressionTree.Symbols.Math.Wildcards;

public class AnySubtreeSymbol(int minimumArity, int maximumArity) : Symbol(minimumArity, minimumArity, maximumArity);
