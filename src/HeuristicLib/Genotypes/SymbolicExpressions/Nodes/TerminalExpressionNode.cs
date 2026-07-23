using Generator.Equals;

namespace HEAL.HeuristicLib.Genotypes.SymbolicExpressions;

[Equatable]
public abstract partial record TerminalExpressionNode : ExpressionNode
{
    private protected TerminalExpressionNode(TerminalSymbol symbol)
        : base(length: 1, depth: 1)
    {
    }

    [IgnoreEquality]
    public abstract override TerminalSymbol Symbol { get; }

    public override ExpressionNode GetChild(int index)
    {
        throw new ArgumentOutOfRangeException(nameof(index));
    }

    internal override ExpressionNode WithChild(int index, ExpressionNode replacement)
    {
        throw new ArgumentOutOfRangeException(nameof(index));
    }

    internal override ExpressionNode WithChildren(IReadOnlyDictionary<int, ExpressionNode> replacements)
    {
        if (replacements.Count != 0)
            throw new ArgumentException("A terminal node cannot have child replacements.", nameof(replacements));

        return this;
    }

}

[Equatable]
public sealed partial record VariableExpressionNode : TerminalExpressionNode
{
    public VariableExpressionNode(VariableSymbol symbol, string variableName)
        : base(symbol)
    {
        Symbol = symbol;
        VariableName = ValidateVariableName(symbol, variableName);
    }

    [IgnoreEquality]
    public override VariableSymbol Symbol { get; }

    public string VariableName { get; }

    private static string ValidateVariableName(VariableSymbol symbol, string variableName)
    {
        if (string.IsNullOrWhiteSpace(variableName))
            throw new ArgumentException("Variable name must not be empty.", nameof(variableName));
        if (!symbol.Variables.Contains(variableName, StringComparer.Ordinal))
            throw new ArgumentException($"Variable symbol does not allow variable '{variableName}'.", nameof(variableName));

        return variableName;
    }
}

[Equatable]
public sealed partial record NumericConstantExpressionNode : TerminalExpressionNode
{
    public NumericConstantExpressionNode(ConstantSymbol symbol, double value)
        : base(symbol)
    {
        Symbol = symbol;
        Value = ValidateValue(symbol, value);
    }

    [IgnoreEquality]
    public override ConstantSymbol Symbol { get; }

    public double Value { get; }

    private static double ValidateValue(ConstantSymbol symbol, double value)
    {
        if (symbol is FixedConstantSymbol fixedConstant && !value.Equals(fixedConstant.Value))
            throw new ArgumentException($"Fixed constant symbol '{fixedConstant.Name}' requires value {fixedConstant.Value}.", nameof(value));

        return value;
    }
}

[Equatable]
public sealed partial record PayloadlessTerminalExpressionNode : TerminalExpressionNode
{
    public PayloadlessTerminalExpressionNode(PayloadlessTerminalSymbol symbol)
        : base(symbol)
    {
        Symbol = symbol;
    }

    [IgnoreEquality]
    public override PayloadlessTerminalSymbol Symbol { get; }
}
