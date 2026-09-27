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

    /// <summary>Declares a generated wrapper for an original source configuration.</summary>
    /// <remarks>
    /// The callback receives the original source, even when other wrappers already apply. It runs once per source
    /// execution and registration. Each wrapper's factory resolves that source as its context-specific inner chain.
    /// Configuration declarations bind inside module declarations, deeper declarations inside shallower ones, and
    /// earlier declarations inside later ones at equal registration kind/depth. Entry work runs in reverse order.
    /// </remarks>
    public ResolutionScopeBuilder Wrap<TConfiguration>(TConfiguration configuration, Func<TConfiguration, TConfiguration> wrap)
        where TConfiguration : class, IConfigurationNode
    {
        registrations.Add(WrapperRegistration.Create(configuration, wrap, sharing, installingModule, registrations.Count));
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
