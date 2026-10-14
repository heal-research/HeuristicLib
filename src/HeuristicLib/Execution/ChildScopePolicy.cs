namespace HEAL.HeuristicLib.Execution;

/// <summary>Chooses the resolution scope used when a meta-algorithm activates a child algorithm.</summary>
/// <remarks>Child state reuses factories already owned by an ancestor; only newly selected state is isolated.</remarks>
public enum ChildScopePolicy
{
    /// <summary>Creates new child-local state for every activation.</summary>
    Fresh,
    /// <summary>Retains child-local state per inner configuration reference within its meta-algorithm.</summary>
    Retained,
    /// <summary>Uses the meta-algorithm's state, sharing references with siblings and direct resolution.</summary>
    Shared
}
