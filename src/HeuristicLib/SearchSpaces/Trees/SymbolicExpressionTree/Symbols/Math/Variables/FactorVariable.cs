using HEAL.HeuristicLib.Genotypes.Trees;

namespace HEAL.HeuristicLib.SearchSpaces.Trees.SymbolicExpressionTree.Symbols.Math.Variables;

public sealed class FactorVariable : VariableBase {
  private readonly Dictionary<string, Dictionary<string, int>> variableValues = new(); // for each variable value also store a zero-based index

  public IEnumerable<KeyValuePair<string, Dictionary<string, int>>> VariableValues {
    get => variableValues;
    set {
      ArgumentNullException.ThrowIfNull(value);
      variableValues.Clear();
      foreach (var kvp in value) {
        variableValues.Add(kvp.Key, new Dictionary<string, int>(kvp.Value));
      }
    }
  }

  public override SymbolicExpressionTreeNode CreateTreeNode() {
    return new FactorVariableTreeNode(this);
  }

  public IEnumerable<string> GetVariableValues(string variableName) {
    return variableValues[variableName].Keys;
  }

  public int GetIndexForValue(string variableName, string variableValue) {
    return variableValues[variableName][variableValue];
  }
}
