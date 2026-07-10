namespace HEAL.HeuristicLib.APIs.TreeSearchLib.SearchStates;

using Optimization;
using TreesearchLib;

public abstract class TreeSearchState<TMove> : IState<TreeSearchState<TMove>, ObjectiveVectorQuality>
{
    public abstract ObjectiveVector? Quality();
    public abstract ObjectiveVector Bound();
    public abstract bool IsTerminal();
    public abstract TreeSearchState<TMove> Copy();
    public abstract IEnumerable<TMove> Branches();
    public abstract TreeSearchState<TMove> Branch(TMove move);
    public abstract Objective Objective();

    #region explicit treesearchlib implementation
    IEnumerable<TreeSearchState<TMove>> IState<TreeSearchState<TMove>, ObjectiveVectorQuality>.GetBranches() => Branches().Select(Branch);
    object ICloneable.Clone() => Copy();
    bool IQualifiable<ObjectiveVectorQuality>.IsTerminal => IsTerminal();
    ObjectiveVectorQuality IQualifiable<ObjectiveVectorQuality>.Bound => new(Bound(), Objective());
    ObjectiveVectorQuality? IQualifiable<ObjectiveVectorQuality>.Quality
    {
        get
        {
            var q = Quality();
            return q is null ? null : new ObjectiveVectorQuality(q, Objective());
        }
    }
    #endregion
}
