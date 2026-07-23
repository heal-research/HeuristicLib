namespace HEAL.HeuristicLib.APIs.TreeSearchLib.SearchStates;

using Optimization;
using TreesearchLib;

public abstract class TreeSearchState<TMove> : IState<TreeSearchState<TMove>, ObjectiveVectorQuality>
{
    protected abstract ObjectiveVector? Quality { get; }
    protected abstract ObjectiveVector Bound { get; }
    protected abstract bool IsTerminal { get; }
    protected abstract TreeSearchState<TMove> Copy();
    protected abstract IEnumerable<TMove> Branches();
    protected abstract TreeSearchState<TMove> Branch(TMove move);
    protected abstract ObjectiveDirections Objective { get; }

    #region explicit treesearchlib implementation
    IEnumerable<TreeSearchState<TMove>> IState<TreeSearchState<TMove>, ObjectiveVectorQuality>.GetBranches() =>
        Branches().Select(Branch);

    object ICloneable.Clone() => Copy();
    bool IQualifiable<ObjectiveVectorQuality>.IsTerminal => IsTerminal;
    ObjectiveVectorQuality IQualifiable<ObjectiveVectorQuality>.Bound => new(Bound, Objective);
    ObjectiveVectorQuality? IQualifiable<ObjectiveVectorQuality>.Quality
    {
        get
        {
            var q = Quality;
            return q is null ? null : new ObjectiveVectorQuality(q, Objective);
        }
    }
    #endregion
}
