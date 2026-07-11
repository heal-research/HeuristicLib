namespace HEAL.HeuristicLib.Genotypes.SymbolicExpressions;

public readonly record struct ExpressionNode
{
    public ExpressionNode(Symbol symbol)
    {
        if (symbol is VariableSymbol or ConstantSymbol)
            throw new ArgumentException("Payload-bearing symbols require their corresponding node payload.", nameof(symbol));
        Symbol = symbol;
        VariableName = null;
        NumericValue = default;
    }

    internal ExpressionNode(VariableSymbol symbol, string variableName)
    {
        Symbol = symbol;
        VariableName = variableName;
        NumericValue = default;
    }

    internal ExpressionNode(ConstantSymbol symbol, double numericValue)
    {
        Symbol = symbol;
        VariableName = null;
        NumericValue = numericValue;
    }

    public Symbol Symbol { get; }
    internal string? VariableName { get; }
    internal double NumericValue { get; }
    internal bool HasVariableName => VariableName is not null;
    internal bool HasNumericValue => Symbol is ConstantSymbol;
    public int Arity => Symbol.Arity;
    public string Name => Symbol.Name;

    public bool TryGetVariableName(out string variableName)
    {
        variableName = VariableName!;
        return Symbol is VariableSymbol;
    }

    public bool TryGetNumericValue(out double value)
    {
        value = NumericValue;
        return Symbol is ConstantSymbol;
    }
}
