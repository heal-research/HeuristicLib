namespace HEAL.HeuristicLib.Execution;

/// <summary>Registers execution wrappers before resolution begins.</summary>
/// <remarks>
/// Each scope or retained child slot takes a snapshot of the builder's declarations. Keeping the builder and
/// changing it later cannot modify that snapshot. Module registrations bind outside configuration registrations.
/// Modules are installed once by reference.
/// </remarks>
public sealed class ResolutionScopeBuilder
{
    private readonly ExecutionSharingScope sharing;
    private readonly List<WrapperRegistration> registrations = [];
    private readonly HashSet<IExecutionModule> installedModules = new(ReferenceEqualityComparer.Instance);
    private bool installingModule;

    internal ResolutionScopeBuilder(ExecutionSharingScope sharing)
    {
        this.sharing = sharing;
    }

    /// <summary>Declares a generated wrapper for one original source configuration, selected by reference.</summary>
    /// <remarks>Equivalent to wrapping the selection <see cref="NodeSelector.Reference{TConfiguration}"/>.</remarks>
    public ResolutionScopeBuilder Wrap<TConfiguration>(TConfiguration configuration, Func<TConfiguration, TConfiguration> wrap)
        where TConfiguration : class, IConfigurationNode =>
        Wrap(NodeSelector.Reference(configuration), wrap);

    /// <summary>Declares a generated wrapper for every original source configuration the selector matches.</summary>
    /// <remarks>
    /// <para>
    /// Matching happens while resolving, including sources resolved later by deferred children. Generated wrappers are
    /// never offered to a selector; explicit wrappers in the configuration graph are ordinary sources. Children added by
    /// this registration's own wrappers are not offered to it, though other registrations still see them. A source matching
    /// several branches of one selector is wrapped once. A selection matching nothing is valid and adds nothing.
    /// Internal compositions and their private dependencies are never offered to any registration.
    /// Predicates should depend only on configuration data; how often they run is unspecified. A predicate exception
    /// fails that source's binding in the requesting context.
    /// </para>
    /// <para>
    /// The callback receives the original source, even when other wrappers already apply. It runs once per source
    /// execution and registration. Its result must stand in for the role through which the source is resolved. Each
    /// wrapper's factory resolves that source as its context-specific inner chain. Configuration declarations bind
    /// inside module declarations, deeper declarations inside shallower ones, and earlier declarations inside later
    /// ones at equal registration kind/depth. Entry work runs in reverse order.
    /// </para>
    /// </remarks>
    public ResolutionScopeBuilder Wrap<TConfiguration>(NodeSelector<TConfiguration> selector, Func<TConfiguration, TConfiguration> wrap)
        where TConfiguration : class, IConfigurationNode
    {
        registrations.Add(WrapperRegistration.Create(selector, wrap, sharing, installingModule, registrations.Count));
        return this;
    }

    /// <summary>Installs a module once, placing its wrappers outside configuration registrations.</summary>
    public ResolutionScopeBuilder Install(IExecutionModule module)
    {
        if (!installedModules.Add(module))
            return this;

        var previouslyInstallingModule = installingModule;
        installingModule = true;
        try
        {
            module.Install(this);
        }
        finally
        {
            installingModule = previouslyInstallingModule;
        }

        return this;
    }

    internal ImmutableArray<WrapperRegistration> Snapshot() => [.. registrations];
}
