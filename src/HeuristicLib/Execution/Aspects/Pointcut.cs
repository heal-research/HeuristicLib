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

    /// <summary>Selects configurations for which the supplied predicate returns true.</summary>
    /// <remarks>
    /// This runtime-only convenience may capture state and is not a persistable configuration strategy. The predicate
    /// should examine configuration data only. It runs when matching is queried, only for the declared configuration
    /// type, and its exceptions propagate to the caller.
    /// </remarks>
    public static Pointcut<TConfiguration> Predicate<TConfiguration>(Func<TConfiguration, bool> predicate)
        where TConfiguration : class, IConfigurationNode => new PredicatePointcut<TConfiguration>(predicate);

    /// <summary>Selects configurations carrying the supplied name through <see cref="INamedConfiguration"/>.</summary>
    /// <remarks>
    /// Names compare ordinally and case-sensitively. Every configuration with the name matches; names are labels,
    /// not unique identities. Matching examines the supplied configuration, not its children.
    /// </remarks>
    public static Pointcut<TConfiguration> Named<TConfiguration>(string name)
        where TConfiguration : class, IConfigurationNode => new NamedPointcut<TConfiguration>(name);

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

    private sealed class PredicatePointcut<TConfiguration>(Func<TConfiguration, bool> predicate) : Pointcut<TConfiguration>
        where TConfiguration : class, IConfigurationNode
    {
        public override bool Matches(TConfiguration configuration) => predicate(configuration);
    }

    private sealed class NamedPointcut<TConfiguration>(string name) : Pointcut<TConfiguration>
        where TConfiguration : class, IConfigurationNode
    {
        public override bool Matches(TConfiguration configuration) =>
            configuration is INamedConfiguration named && string.Equals(named.Name, name, StringComparison.Ordinal);
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

    /// <summary>Selects configurations of the declared type that this pointcut does not select.</summary>
    /// <remarks>Configurations outside the declared type still do not match. The source pointcut is unchanged.</remarks>
    public Pointcut<TConfiguration> Not() => new NotPointcut(this);

    /// <remarks>Constructs the same intersection as <see cref="And(Pointcut{TConfiguration})"/>.</remarks>
    public static Pointcut<TConfiguration> operator &(Pointcut<TConfiguration> left, Pointcut<TConfiguration> right) =>
        left.And(right);

    /// <remarks>Constructs the same union as <see cref="Or(Pointcut{TConfiguration})"/>.</remarks>
    public static Pointcut<TConfiguration> operator |(Pointcut<TConfiguration> left, Pointcut<TConfiguration> right) =>
        left.Or(right);

    /// <remarks>Constructs the same complement within the declared type as <see cref="Not"/>.</remarks>
    public static Pointcut<TConfiguration> operator !(Pointcut<TConfiguration> source) => source.Not();

    private sealed class NotPointcut(Pointcut<TConfiguration> source) : Pointcut<TConfiguration>
    {
        public override bool Matches(TConfiguration configuration) => !source.Matches(configuration);
    }

    private sealed class AndPointcut(Pointcut<TConfiguration> left, Pointcut<TConfiguration> right) : Pointcut<TConfiguration>
    {
        public override bool Matches(TConfiguration configuration) => left.Matches(configuration) && right.Matches(configuration);
    }

    private sealed class OrPointcut(Pointcut<TConfiguration> left, Pointcut<TConfiguration> right) : Pointcut<TConfiguration>
    {
        public override bool Matches(TConfiguration configuration) => left.Matches(configuration) || right.Matches(configuration);
    }
}
