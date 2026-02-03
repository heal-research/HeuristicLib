namespace HEAL.HeuristicLib.Encodings.SymbolicExpressionTree.Symbols.Math.Variables;

public sealed class BinaryFactorVariable : VariableBase {
  private readonly Dictionary<string, List<string>> variableValues = new();

  public IReadOnlyDictionary<string, List<string>> VariableValues => variableValues;

  public override SymbolicExpressionTreeNode CreateTreeNode() {
    return new BinaryFactorVariableTreeNode(this);
  }

  public IEnumerable<string> GetVariableValues(string variableName) {
    return variableValues[variableName];
  }
}
