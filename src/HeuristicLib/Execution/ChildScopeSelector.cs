namespace HEAL.HeuristicLib.Execution;

/// <summary>Selects the resolution scope for a meta-algorithm's child activation.</summary>
/// <remarks>
/// A meta-algorithm keeps one selector as its scoped state, so every build in its owning scope, for any run type,
/// shares it. Retained child state is keyed by configuration reference and outlives each activation, so it keeps no
/// aspects: every activation applies its own scope's aspects to it. Child scopes continue to reuse configurations
/// already owned by an ancestor, together with their scoped state.
/// </remarks>
public sealed class ChildScopeSelector
{
    private readonly ChildScopePolicy policy;
#pragma warning disable HLib0002 // keeping retained child states is this selector's purpose; it holds no executor
    private readonly Dictionary<IConfigurationNode, ResolutionScope.State>? retainedStates;
#pragma warning restore HLib0002

    public ChildScopeSelector(ChildScopePolicy policy)
    {
        if (!Enum.IsDefined(policy))
            throw new ArgumentOutOfRangeException(nameof(policy), policy, "Unknown child scope policy.");
        this.policy = policy;
        retainedStates = policy == ChildScopePolicy.Retained ? new(ReferenceEqualityComparer.Instance) : null;
    }

    /// <summary>Selects fresh child-local, retained child-local or parent state for this activation.</summary>
    public ResolutionScope GetScopeFor(IConfigurationNode configuration, ResolutionScope parentScope)
    {
        if (policy == ChildScopePolicy.Fresh)
            return parentScope.CreateChildScope();
        if (policy == ChildScopePolicy.Shared)
            return parentScope;

        if (!retainedStates!.TryGetValue(configuration, out var retained))
        {
            retained = parentScope.CreateChildState();
            retainedStates.Add(configuration, retained);
        }
        return parentScope.WithState(retained);
    }
}
