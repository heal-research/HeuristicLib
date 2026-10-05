using System.Diagnostics.CodeAnalysis;

namespace HEAL.HeuristicLib.Execution;

/// <summary>
/// Resolves configuration references into typed execution nodes, preserving their state across observation contexts.
/// </summary>
/// <remarks>
/// Routes sharing and dependency selection to their owners, then obtains nodes from the current execution bindings.
/// A factory receives a scope tied to its original preparation and the requesting observations. It can retain that
/// scope for deferred construction without changing another caller's children or observers.
/// </remarks>
public sealed class ResolutionScope
{
    private readonly ExecutionSharingScope sharing;
    private readonly ExecutionBindings bindings;
    private readonly ExecutionPreparation? owner;
    private readonly ExecutionBindings.WrappedNodes? wrappedNodes;

    internal ResolutionScope(ExecutionSharingScope sharing, ExecutionBindings bindings,
        ExecutionPreparation? owner = null, ExecutionBindings.WrappedNodes? wrappedNodes = null)
    {
        this.sharing = sharing;
        this.bindings = bindings;
        this.owner = owner;
        this.wrappedNodes = wrappedNodes;
    }

    /// <summary>Gets this scope's depth along the current observation path, including retained children.</summary>
    internal int Depth => bindings.Depth;

    /// <summary>Creates an independent root scope without wrappers.</summary>
    public static ResolutionScope Create() => CreateScope(new ExecutionSharingScope(null), null, null);

    /// <summary>Creates an independent root scope with a snapshot of the declared wrappers.</summary>
    public static ResolutionScope Create(Action<ResolutionScopeBuilder> declare) =>
        CreateScope(new ExecutionSharingScope(null), null, declare);

    /// <summary>Creates a fresh child sharing scope, inheriting observations and existing ancestor state.</summary>
    /// <remarks>A fresh child does not reset state that its ancestors already own.</remarks>
    public ResolutionScope CreateChildScope() => CreateScope(new ExecutionSharingScope(sharing), bindings, null);

    /// <summary>Creates a fresh child sharing scope and snapshots its additional declarations.</summary>
    public ResolutionScope CreateChildScope(Action<ResolutionScopeBuilder> declare) =>
        CreateScope(new ExecutionSharingScope(sharing), bindings, declare);

    /// <summary>Gets a retained child scope through the current observation context.</summary>
    /// <remarks>
    /// Keys compare by reference. A factory's scope retains children on its preparation; a standalone scope retains
    /// them on its sharing scope. Use a stable key for each child, allocated outside the binding factory.
    /// </remarks>
    public ResolutionScope GetOrCreateChildScope(object key) => GetChildScope(key, null);

    /// <summary>Gets a retained child scope, declaring its observations only on the first use of the key.</summary>
    /// <remarks>
    /// Later calls reuse the child's state and declarations through the requesting context. They do not rerun the
    /// callback, including when the first declaration failed. Changing a callback's captures is not reconfiguration.
    /// </remarks>
    public ResolutionScope GetOrCreateChildScope(object key, Action<ResolutionScopeBuilder> declare) =>
        GetChildScope(key, declare);

    private ResolutionScope GetChildScope(object key, Action<ResolutionScopeBuilder>? declare)
    {
        var child = owner is null ? sharing.GetRetainedChild(key) : owner.GetRetainedChild(key);
        return bindings.GetRetainedScope(child, declare);
    }

    private static ResolutionScope CreateScope(ExecutionSharingScope sharing, ExecutionBindings? parent,
        Action<ResolutionScopeBuilder>? declare)
    {
        var builder = new ResolutionScopeBuilder(sharing);
        declare?.Invoke(builder);
        return new ResolutionScope(sharing, new ExecutionBindings(parent, builder.Snapshot()));
    }

    /// <summary>Prepares a configuration once and resolves its binding for this observation context.</summary>
    /// <remarks>
    /// <para>
    /// Direct resolution uses this scope's selected preparation, finds one in an ancestor, or reserves one locally.
    /// A factory's scope instead uses its preparation's pinned dependencies. A reused composite keeps those children even
    /// when direct resolution in the requesting child scope has selected different children.
    /// </para>
    /// <para>
    /// Preparation has no scope and runs once per selected execution. The returned factory may run again to bind
    /// children and observations for another context. Pass a static preparation adapter where possible. Generated
    /// wrappers must implement the same configuration contract accepted by that adapter.
    /// </para>
    /// <para>
    /// Preparation faults stay with the selected preparation; binding faults belong to its observation context.
    /// Neither is retried by resolving again. Changing observations can retry binding but never preparation.
    /// </para>
    /// </remarks>
    public TExecution Resolve<TConfiguration, TExecution>(TConfiguration configuration, Func<TConfiguration, ExecutionFactory<TExecution>> prepare)
        where TConfiguration : class, IConfigurationNode
        where TExecution : class, IExecutionNode =>
        ResolveBinding(configuration, prepare).Completed;

    /// <summary>Resolves a configuration whose execution contract is known without a run's type arguments.</summary>
    public TExecution Resolve<TExecution>(IConfigurationNode<TExecution> configuration)
        where TExecution : class, IExecutionNode =>
        Resolve(configuration, static source => source.CreateExecutionFactory());

    /// <summary>Resolves the wrapped operation and projects an optional control from the same source's raw binding.</summary>
    /// <remarks>
    /// Role wrappers need not implement the source's additional control interfaces. Projecting from the raw binding
    /// keeps observations from hiding those controls while operations still use the completed wrapper chain.
    /// The selector runs only after complete resolution succeeds. It must be a side-effect-free projection of an
    /// operation-free control. Invoke operations through the returned wrapped execution. Configured wrappers expose
    /// their own control deliberately; this overload does not search or unwrap their children.
    /// </remarks>
    public TExecution Resolve<TConfiguration, TExecution, TControl>(TConfiguration configuration,
        Func<TConfiguration, ExecutionFactory<TExecution>> prepare, Func<TExecution, TControl?> selectControl, out TControl? control)
        where TConfiguration : class, IConfigurationNode
        where TExecution : class, IExecutionNode
        where TControl : class
    {
        var binding = ResolveBinding(configuration, prepare);
        control = selectControl(binding.Raw);
        return binding.Completed;
    }

    /// <summary>Resolves an optional configuration, returning null for an absent child.</summary>
    [return: NotNullIfNotNull(nameof(configuration))]
    public TExecution? ResolveOptional<TExecution>(IConfigurationNode<TExecution>? configuration)
        where TExecution : class, IExecutionNode =>
        configuration is null ? null : Resolve(configuration);

    /// <summary>Resolves a configuration, reporting an invalid-operation failure as a reason.</summary>
    public bool TryResolve<TExecution>(IConfigurationNode<TExecution> configuration,
        [NotNullWhen(true)] out TExecution? execution, [NotNullWhen(false)] out string? reason)
        where TExecution : class, IExecutionNode =>
        TryResolve(configuration, static source => source.CreateExecutionFactory(), out execution, out reason);

    /// <summary>Resolves through a typed preparation adapter, reporting an invalid-operation failure as a reason.</summary>
    /// <remarks>Other exception types propagate. This operation does not retry faulted construction.</remarks>
    public bool TryResolve<TConfiguration, TExecution>(TConfiguration configuration, Func<TConfiguration, ExecutionFactory<TExecution>> prepare,
        [NotNullWhen(true)] out TExecution? execution, [NotNullWhen(false)] out string? reason)
        where TConfiguration : class, IConfigurationNode
        where TExecution : class, IExecutionNode
    {
        try
        {
            execution = Resolve(configuration, prepare);
            reason = null;
            return true;
        }
        catch (InvalidOperationException exception)
        {
            execution = null;
            reason = exception.Message;
            return false;
        }
    }

    private (TExecution Raw, TExecution Completed) ResolveBinding<TConfiguration, TExecution>(
        TConfiguration configuration, Func<TConfiguration, ExecutionFactory<TExecution>> prepare)
        where TConfiguration : class, IConfigurationNode
        where TExecution : class, IExecutionNode
    {
        if (wrappedNodes is not null && wrappedNodes.Matches(configuration))
            return wrappedNodes.GetNodes<TExecution>();

        var preparation = owner is null
            ? sharing.Select(configuration, prepare)
            : owner.SelectDependency(configuration, prepare);
        preparation.RequireContract<TExecution>();
        return bindings.GetOrBind<TExecution>(preparation);
    }
}
