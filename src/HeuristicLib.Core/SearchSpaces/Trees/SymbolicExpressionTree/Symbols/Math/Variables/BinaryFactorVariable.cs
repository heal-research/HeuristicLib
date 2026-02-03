using HEAL.HeuristicLib.Genotypes.Trees;

namespace HEAL.HeuristicLib.SearchSpaces.Trees.SymbolicExpressionTree.Symbols.Math.Variables;

public sealed class BinaryFactorVariable : VariableBase
{
  private readonly Dictionary<string, List<string>> variableValues = new();

  public IReadOnlyDictionary<string, List<string>> VariableValues => variableValues;

  public override SymbolicExpressionTreeNode CreateTreeNode() => new BinaryFactorVariableTreeNode(this);

  public IEnumerable<string> GetVariableValues(string variableName) => variableValues[variableName];
}
