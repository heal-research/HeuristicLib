namespace HEAL.HeuristicLib.Execution;

/// <remarks>
/// Aspects are installed in declaration order. The snapshot is immutable even if a caller retains and later
/// changes the builder.
/// </remarks>
public sealed class AspectBuilder
{
    private readonly List<Aspect> aspects = [];
    internal AspectBuilder() { }

    public AspectBuilder Install<TConfiguration>(TConfiguration configuration, Func<TConfiguration, TConfiguration> recipe)
        where TConfiguration : class, IConfigurationNode => Install(Aspect.Create(configuration, recipe));

    /// <remarks>
    /// Recipes receive the selected configuration, including composite targets, and must return a single-child
    /// wrapper of the resolved role. Only its advice is prepared; its child is never independently resolved.
    /// Earlier declarations are applied first, so their entry work runs inside later declarations.
    /// </remarks>
    public AspectBuilder Install<TConfiguration>(Pointcut<TConfiguration> pointcut, Func<TConfiguration, TConfiguration> recipe)
        where TConfiguration : class, IConfigurationNode => Install(Aspect.Create(pointcut, recipe));

    /// <remarks>
    /// Compositions expand at their installation position. Reusing the same aspect retains its rule identities;
    /// its first occurrence determines its position throughout the scope's aspect collection.
    /// </remarks>
    public AspectBuilder Install(Aspect aspect)
    {
        aspects.Add(aspect);
        return this;
    }

    internal ImmutableArray<Aspect> Snapshot() => [.. aspects];
}
