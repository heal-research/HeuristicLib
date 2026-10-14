namespace HEAL.HeuristicLib.Execution;

/// <summary>Selects the configurations whose executions receive advice.</summary>
/// <remarks>Lets pointcuts for different roles share one list.</remarks>
public abstract class Pointcut
{
    private protected Pointcut() { }

    public abstract bool Matches(IConfigurationNode configuration);

    /// <remarks>
    /// Uses object identity, so a structurally equal configuration or a copy made with <c>with</c> is a different target.
    /// The reference's static type determines the selector's configuration type.
    /// </remarks>
    public static Pointcut<TConfiguration> Reference<TConfiguration>(TConfiguration configuration)
        where TConfiguration : class, IConfigurationNode => new ReferencePointcut<TConfiguration>(configuration);

    /// <remarks>
    /// Includes derived classes and interface implementations. A closed role interface selects every configuration
    /// implementing that role, including consumer-defined implementations.
    /// </remarks>
    public static Pointcut<TConfiguration> OfType<TConfiguration>()
        where TConfiguration : class, IConfigurationNode => new OfTypePointcut<TConfiguration>();

    private sealed class ReferencePointcut<TConfiguration>(TConfiguration target) : Pointcut<TConfiguration>
        where TConfiguration : class, IConfigurationNode
    {
        public override bool Matches(TConfiguration configuration) => ReferenceEquals(configuration, target);
    }

    private sealed class OfTypePointcut<TConfiguration> : Pointcut<TConfiguration>
        where TConfiguration : class, IConfigurationNode
    {
        public override bool Matches(TConfiguration configuration) => true;
    }
}

/// <summary>
/// Selects configurations of <typeparamref name="TConfiguration"/> whose execution operations receive advice.
/// </summary>
/// <remarks>
/// Derive to select by configuration data. Matching examines the supplied configuration only; it does not traverse a
/// graph, resolve executions or install behavior. It runs on each matching query, without caching, and should depend
/// only on configuration data. <see cref="Aspect"/> pairs a pointcut with the advice applied to each configuration that
/// matches during resolution.
/// </remarks>
public abstract class Pointcut<TConfiguration> : Pointcut
    where TConfiguration : class, IConfigurationNode
{
    /// <remarks>
    /// A configuration outside <typeparamref name="TConfiguration"/> does not match and never reaches the typed overload.
    /// Exceptions from a pointcut propagate to the caller.
    /// </remarks>
    public sealed override bool Matches(IConfigurationNode configuration) =>
        configuration is TConfiguration typed && Matches(typed);

    public abstract bool Matches(TConfiguration configuration);

    /// <remarks>
    /// Matches only when both pointcuts match. The right pointcut runs only if the left one matches.
    /// Neither source pointcut is changed.
    /// </remarks>
    public Pointcut<TConfiguration> And(Pointcut<TConfiguration> other) => new AndPointcut(this, other);

    /// <remarks>
    /// Matches when either pointcut matches, yielding one match even when both apply.
    /// The right pointcut runs only if the left one does not match. Neither source pointcut is changed.
    /// </remarks>
    public Pointcut<TConfiguration> Or(Pointcut<TConfiguration> other) => new OrPointcut(this, other);

    /// <remarks>Constructs the same intersection as <see cref="And(Pointcut{TConfiguration})"/>.</remarks>
    public static Pointcut<TConfiguration> operator &(Pointcut<TConfiguration> left, Pointcut<TConfiguration> right) =>
        left.And(right);

    /// <remarks>Constructs the same union as <see cref="Or(Pointcut{TConfiguration})"/>.</remarks>
    public static Pointcut<TConfiguration> operator |(Pointcut<TConfiguration> left, Pointcut<TConfiguration> right) =>
        left.Or(right);

    private sealed class AndPointcut(Pointcut<TConfiguration> left, Pointcut<TConfiguration> right) : Pointcut<TConfiguration>
    {
        public override bool Matches(TConfiguration configuration) => left.Matches(configuration) && right.Matches(configuration);
    }

    private sealed class OrPointcut(Pointcut<TConfiguration> left, Pointcut<TConfiguration> right) : Pointcut<TConfiguration>
    {
        public override bool Matches(TConfiguration configuration) => left.Matches(configuration) || right.Matches(configuration);
    }
}
