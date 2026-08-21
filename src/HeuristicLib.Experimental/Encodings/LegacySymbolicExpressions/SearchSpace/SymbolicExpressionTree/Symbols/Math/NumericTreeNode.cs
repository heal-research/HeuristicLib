namespace HEAL.HeuristicLib.Encodings.LegacySymbolicExpressions;

public abstract class NumericTreeNode : SymbolicExpressionTreeNode
{
    protected NumericTreeNode(Symbol symbol) : base(symbol) { }

    protected NumericTreeNode(NumericTreeNode original) : base(original)
    {
        Value = original.Value;
    }

    public double Value { get; set; }

    public override string ToString() => $"{Value:E4}";
}
