namespace HEAL.HeuristicLib.Execution;

/// <remarks>
/// Each build snapshots the supplied bindings and aspects into an independent scope. Later changes to this builder
/// do not affect existing scopes. Default resources are independent across builds, while supplied instances are shared.
/// The builder remains usable after building a scope.
/// </remarks>
public sealed class ResolutionScopeBuilder(params IReadOnlyList<Aspect> aspects)
{
    private readonly List<Aspect> aspects = [.. aspects];
    private readonly Dictionary<object, object> resources = new(ReferenceEqualityComparer.Instance);

    public ResolutionScopeBuilder AddAspect(Aspect aspect)
    {
        aspects.Add(aspect);
        return this;
    }

    /// <remarks>Duplicate handle bindings are rejected. Supplied resources remain caller owned.</remarks>
    public ResolutionScopeBuilder AddResource<TResource>(ResourceHandle<TResource> handle, TResource resource)
        where TResource : class
    {
        CheckResourceSupply(handle);
        resources.Add(handle, resource);
        return this;
    }

    /// <remarks>
    /// An aspect supplied several times applies once, at its first position. The first aspect supplied wraps closest to
    /// each executor, so aspects observe in the order they were first attached. An aspect added to a scope later wraps
    /// closer, so the scope receives the aspects last to first.
    /// </remarks>
    public ResolutionScope Build()
    {
        var scope = ResolutionScope.Create(new RunResources(resources));
        List<Aspect> attached = [.. aspects.Where(new HashSet<Aspect>(ReferenceEqualityComparer.Instance).Add)];
        for (var index = attached.Count - 1; index >= 0; index--)
            scope.AddAspect(attached[index]);
        return scope;
    }

    internal void CheckResourceSupply<TResource>(ResourceHandle<TResource> handle) where TResource : class
    {
        if (resources.ContainsKey(handle))
            throw new InvalidOperationException($"Resource {handle.GetType().Name} has already been supplied.");
    }
}
