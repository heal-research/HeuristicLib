namespace HEAL.HeuristicLib.Encodings.LegacySymbolicExpressions;

public sealed class SimpleSymbol(int minimumArity, int maximumArity) : Symbol(minimumArity, minimumArity, maximumArity)
{
    public SimpleSymbol(int arity) : this(arity, arity) { }
}
