namespace HEAL.HeuristicLib.Execution;

/// <summary>Wraps the executors built for the configurations its pointcut selects.</summary>
/// <remarks>
/// Each advised role has one library advice base whose <c>Wrap</c> receives every executor built for a selected
/// configuration. Resolution compares advice by reference; data that must outlive one executor belongs to the advice
/// object.
/// </remarks>
public abstract class Advice
{
    private protected Advice() { }

    // Resolution knows a consumer-defined role only by its executor type, so it reaches the role's advice through this
    // untyped entry. Only Advice<TConfiguration, TExecutor> wraps here; other advice returns null.
    internal virtual IExecutor? WrapNode(IExecutor executor, IConfigurationNode source) => null;
}

/// <summary>Advice that attaches to configurations of the role <typeparamref name="TConfiguration"/>.</summary>
/// <remarks>
/// The role lets the compiler reject a pointcut and advice for different roles; it does not change what is wrapped.
/// Only the library's advice bases derive from this type directly.
/// </remarks>
public abstract class Advice<TConfiguration> : Advice
    where TConfiguration : class, IConfigurationNode
{
    private protected Advice() { }
}

/// <summary>Advice that wraps the executors of a consumer-defined role, such as a cooling schedule.</summary>
/// <remarks>
/// Resolution calls <see cref="Wrap"/> for every executor built for a selected configuration. Data that must outlive
/// one executor belongs to the advice object; it is shared by every configuration and resolution the advice is applied
/// to.
/// </remarks>
public abstract class Advice<TConfiguration, TExecutor> : Advice<TConfiguration>
    where TConfiguration : class, IConfigurationNode<TExecutor>
    where TExecutor : class, IExecutor
{
    /// <summary>Wraps one executor built for a selected configuration and returns the executor its callers use.</summary>
    public abstract TExecutor Wrap(TExecutor executor, TConfiguration source);

    internal sealed override IExecutor? WrapNode(IExecutor executor, IConfigurationNode source) =>
        executor is TExecutor typed && source is TConfiguration configuration ? Wrap(typed, configuration) : null;
}
