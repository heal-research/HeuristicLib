namespace HEAL.HeuristicLib.Encodings.SymbolicExpression.Symbols.Math.Wildcards;

public class AnySubtreeSymbol(int minimumArity, int maximumArity) : Symbol(minimumArity, minimumArity, maximumArity);
