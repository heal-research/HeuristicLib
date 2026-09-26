namespace HEAL.HeuristicLib.Execution;

/// <summary>
/// Selects configuration nodes while retaining the configuration type or role used by its predicates.
/// </summary>
/// <remarks>
/// Matching examines the supplied configuration only; it does not traverse a graph, resolve instances or install
/// behavior. Predicates run on each matching query, without caching, and should depend only on configuration data.
/// </remarks>
public sealed class NodeSelector<TConfiguration>
    where TConfiguration : class, IExecutionConfiguration
{
    private readonly Func<TConfiguration, bool> predicate;

    public NodeSelector(Func<TConfiguration, bool> predicate)
    {
        this.predicate = predicate;
    }

    /// <remarks>
    /// A configuration outside <typeparamref name="TConfiguration"/> does not match and never reaches the predicate.
    /// Exceptions from a predicate propagate to the caller.
    /// </remarks>
    public bool Matches(IExecutionConfiguration configuration) =>
        configuration is TConfiguration typed && predicate(typed);

    /// <remarks>
    /// Constructs a predicate selector and intersects it with this selection.
    /// </remarks>
    public NodeSelector<TConfiguration> And(Func<TConfiguration, bool> predicate) =>
        And(new NodeSelector<TConfiguration>(predicate));

    /// <remarks>
    /// Matches only when both selectors match. The right predicate runs only if the left predicate matches.
    /// Neither source selector is changed.
    /// </remarks>
    public NodeSelector<TConfiguration> And(NodeSelector<TConfiguration> other)
    {
        return new(configuration => predicate(configuration) && other.predicate(configuration));
    }

    /// <remarks>
    /// Matches when either selector matches, yielding one match even when both apply.
    /// The right predicate runs only if the left predicate does not match. Neither source selector is changed.
    /// </remarks>
    public NodeSelector<TConfiguration> Or(NodeSelector<TConfiguration> other)
    {
        return new(configuration => predicate(configuration) || other.predicate(configuration));
    }

    /// <remarks>
    /// Constructs the same intersection as <see cref="And(NodeSelector{TConfiguration})"/>.
    /// Both selector operands are evaluated when composing; their predicates run only when matching a node.
    /// </remarks>
    public static NodeSelector<TConfiguration> operator &(NodeSelector<TConfiguration> left, NodeSelector<TConfiguration> right)
    {
        return left.And(right);
    }

    /// <remarks>
    /// Constructs the same union as <see cref="Or(NodeSelector{TConfiguration})"/>.
    /// Both selector operands are evaluated when composing; their predicates run only when matching a node.
    /// </remarks>
    public static NodeSelector<TConfiguration> operator |(NodeSelector<TConfiguration> left, NodeSelector<TConfiguration> right)
    {
        return left.Or(right);
    }
}

public static class NodeSelector
{
    /// <remarks>
    /// Uses object identity, so a structurally equal configuration or a copy made with <c>with</c> is a different target.
    /// The reference's static type determines the selector's configuration type.
    /// </remarks>
    public static NodeSelector<TConfiguration> Reference<TConfiguration>(TConfiguration configuration)
        where TConfiguration : class, IExecutionConfiguration
    {
        return new(candidate => ReferenceEquals(candidate, configuration));
    }

    /// <remarks>
    /// Includes derived classes and interface implementations. A closed role interface selects every configuration
    /// implementing that role, including consumer-defined implementations.
    /// </remarks>
    public static NodeSelector<TConfiguration> OfType<TConfiguration>()
        where TConfiguration : class, IExecutionConfiguration => new(static _ => true);
}
