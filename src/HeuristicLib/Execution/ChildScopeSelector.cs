namespace HEAL.HeuristicLib.Execution;

/// <summary>Selects the resolution scope for a meta-algorithm's child activation.</summary>
/// <remarks>
/// Prepare one selector per owning meta-algorithm execution state and reuse it across node bindings. Retained
/// scopes are keyed by configuration reference and keep no aspects; each activation receives its requesting
/// scope's aspects. Child scopes continue to reuse factories already owned by an ancestor.
/// </remarks>
public sealed class ChildScopeSelector
{
    private readonly ChildScopePolicy policy;
    private readonly Dictionary<IConfigurationNode, ResolutionScope>? retainedScopes;

    public ChildScopeSelector(ChildScopePolicy policy)
    {
        if (!Enum.IsDefined(policy))
            throw new ArgumentOutOfRangeException(nameof(policy), policy, "Unknown child scope policy.");
        this.policy = policy;
        retainedScopes = policy == ChildScopePolicy.Retained ? new(ReferenceEqualityComparer.Instance) : null;
    }

    /// <summary>Selects fresh child-local, retained child-local or parent state for this activation.</summary>
    public ResolutionScope GetScopeFor(IConfigurationNode configuration, ResolutionScope parentScope)
    {
        if (policy == ChildScopePolicy.Fresh)
            return parentScope.CreateChildScope();
        if (policy == ChildScopePolicy.Shared)
            return parentScope;

        if (!retainedScopes!.TryGetValue(configuration, out var retained))
        {
            retained = parentScope.CreateChildScopeWithoutAspects();
            retainedScopes.Add(configuration, retained);
        }
        return retained.WithAspectsFrom(parentScope);
    }
}
