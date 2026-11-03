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

    public ResolutionScope Build() => ResolutionScope.Create(new RunResources(resources), aspects);

    internal void CheckResourceSupply<TResource>(ResourceHandle<TResource> handle) where TResource : class
    {
        if (resources.ContainsKey(handle))
            throw new InvalidOperationException($"Resource {handle.GetType().Name} has already been supplied.");
    }
}
