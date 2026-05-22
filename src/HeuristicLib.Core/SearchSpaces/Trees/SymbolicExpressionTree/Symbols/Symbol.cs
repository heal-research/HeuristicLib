using HEAL.HeuristicLib.Genotypes.Trees;

namespace HEAL.HeuristicLib.SearchSpaces.Trees.SymbolicExpressionTree.Symbols;

public abstract class Symbol(int minimumArity, int defaultArity, int maximumArity)
{
    public virtual SymbolicExpressionTreeNode CreateTreeNode() => new(this);

    public virtual IEnumerable<Symbol> Flatten()
    {
        yield return this;
    }

    #region Properties
    public double InitialFrequency
    {
        get;
        set
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(value, 0);
            field = value;
        }
    } = 1.0;

    public bool Enabled { get; set; } = true;

    public int MinimumArity { get; set; } = minimumArity;
    public int DefaultArity { get; set; } = defaultArity;
    public int MaximumArity { get; set; } = maximumArity;
    #endregion
}
