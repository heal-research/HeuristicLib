namespace HEAL.HeuristicLib.Encodings.LegacySymbolicExpressions;

public sealed class VariableTreeNode : VariableTreeNodeBase
{

    private VariableTreeNode(VariableTreeNode original) : base(original) { }

    public VariableTreeNode(Variable variableSymbol) : base(variableSymbol) { }
    public new Variable Symbol => (Variable)base.Symbol;

    public override SymbolicExpressionTreeNode Clone() => new VariableTreeNode(this);
}
