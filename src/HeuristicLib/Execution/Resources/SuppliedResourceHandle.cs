namespace HEAL.HeuristicLib.Execution;

/// <remarks>This handle requires explicit runtime supply and never creates a default resource.</remarks>
public sealed class SuppliedResourceHandle<TResource> : ResourceHandle<TResource> where TResource : class
{
    protected internal override TResource CreateResource() =>
        throw new InvalidOperationException($"Resource {typeof(TResource).Name} requires explicit supply through AddResource during preparation.");
}
