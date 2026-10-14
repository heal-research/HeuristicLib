namespace HEAL.HeuristicLib.Execution;

/// <summary>
/// Selects configurations whose execution operations receive advice, retaining the type used by its predicates.
/// </summary>
/// <remarks>
/// Matching examines the supplied configuration only; it does not traverse a graph, resolve executions or install
/// behavior. Predicates run on each matching query, without caching, and should depend only on configuration data.
/// <see cref="AspectBuilder.Install{TConfiguration}(Pointcut{TConfiguration}, Func{TConfiguration, TConfiguration})"/>
/// applies a wrapper to each source configuration that matches during resolution.
/// </remarks>
public sealed class Pointcut<TConfiguration>
    where TConfiguration : class, IConfigurationNode
{
    private readonly Func<TConfiguration, bool> predicate;

    public Pointcut(Func<TConfiguration, bool> predicate)
    {
        this.predicate = predicate;
    }

    /// <remarks>
    /// A configuration outside <typeparamref name="TConfiguration"/> does not match and never reaches the predicate.
    /// Exceptions from a predicate propagate to the caller.
    /// </remarks>
    public bool Matches(IConfigurationNode configuration) =>
        configuration is TConfiguration typed && predicate(typed);

    /// <remarks>
    /// Constructs a predicate selector and intersects it with this selection.
    /// </remarks>
    public Pointcut<TConfiguration> And(Func<TConfiguration, bool> predicate) =>
        And(new Pointcut<TConfiguration>(predicate));

    /// <remarks>
    /// Matches only when both selectors match. The right predicate runs only if the left predicate matches.
    /// Neither source selector is changed.
    /// </remarks>
    public Pointcut<TConfiguration> And(Pointcut<TConfiguration> other)
    {
        return new(configuration => predicate(configuration) && other.predicate(configuration));
    }

    /// <remarks>
    /// Matches when either selector matches, yielding one match even when both apply.
    /// The right predicate runs only if the left predicate does not match. Neither source selector is changed.
    /// </remarks>
    public Pointcut<TConfiguration> Or(Pointcut<TConfiguration> other)
    {
        return new(configuration => predicate(configuration) || other.predicate(configuration));
    }

    /// <remarks>
    /// Constructs the same intersection as <see cref="And(Pointcut{TConfiguration})"/>.
    /// Both selector operands are evaluated when composing; their predicates run only when matching a node.
    /// </remarks>
    public static Pointcut<TConfiguration> operator &(Pointcut<TConfiguration> left, Pointcut<TConfiguration> right)
    {
        return left.And(right);
    }

    /// <remarks>
    /// Constructs the same union as <see cref="Or(Pointcut{TConfiguration})"/>.
    /// Both selector operands are evaluated when composing; their predicates run only when matching a node.
    /// </remarks>
    public static Pointcut<TConfiguration> operator |(Pointcut<TConfiguration> left, Pointcut<TConfiguration> right)
    {
        return left.Or(right);
    }
}

public static class Pointcut
{
    /// <remarks>
    /// Uses object identity, so a structurally equal configuration or a copy made with <c>with</c> is a different target.
    /// The reference's static type determines the selector's configuration type.
    /// </remarks>
    public static Pointcut<TConfiguration> Reference<TConfiguration>(TConfiguration configuration)
        where TConfiguration : class, IConfigurationNode
    {
        return new(candidate => ReferenceEquals(candidate, configuration));
    }

    /// <remarks>
    /// Includes derived classes and interface implementations. A closed role interface selects every configuration
    /// implementing that role, including consumer-defined implementations.
    /// </remarks>
    public static Pointcut<TConfiguration> OfType<TConfiguration>()
        where TConfiguration : class, IConfigurationNode => new(static _ => true);
}
