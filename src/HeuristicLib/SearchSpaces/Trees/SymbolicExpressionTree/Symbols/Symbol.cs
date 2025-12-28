using HEAL.HeuristicLib.Genotypes.Trees;

namespace HEAL.HeuristicLib.SearchSpaces.Trees.SymbolicExpressionTree.Symbols;

public abstract class Symbol(int minimumArity, int defaultArity, int maximumArity) {
  #region Properties
  private double initialFrequency = 1.0;
  public double InitialFrequency {
    get => initialFrequency;
    set {
      ArgumentOutOfRangeException.ThrowIfLessThan(value, 0);
      initialFrequency = value;
    }
  }
  public bool Enabled { get; set; } = true;

  public int MinimumArity { get; set; } = minimumArity;
  public int DefaultArity { get; set; } = defaultArity;
  public int MaximumArity { get; set; } = maximumArity;
  #endregion

  public virtual SymbolicExpressionTreeNode CreateTreeNode() => new(this);

  public virtual IEnumerable<Symbol> Flatten() {
    yield return this;
  }
}
