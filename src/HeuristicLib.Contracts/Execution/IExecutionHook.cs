namespace HEAL.HeuristicLib.Execution;

/// <summary>
/// Adds behavior at chosen configurations in a run's execution graph, before the graph is resolved.
/// </summary>
/// <remarks>
/// <para>
/// A hook decorates the configurations it is interested in by calling <see cref="ExecutionInstanceResolverBuilder.Decorate"/>. It can
/// declare decorations and cannot resolve anything, because observing a run must not participate in building it.
/// Decorations compose, so several hooks may act on the same configuration without displacing one another.
/// </para>
/// <para>
/// Every decoration a hook declares sits outside every decoration the configuration declares, so that a wrapper which
/// measures an operator never measures the hook observing it. Among hooks, the first installation for a configuration becomes
/// the innermost wrapper and therefore observes an operation first. A hook that depends on another one having already
/// acted, such as a trace that reads its clocks, installs its dependencies before itself.
/// </para>
/// <para>
/// Analyzers are the common kind of hook, but nothing about this contract is analysis specific. Writing to a log,
/// reporting progress or bridging to another runtime are equally valid hooks.
/// </para>
/// </remarks>
public interface IExecutionHook
{
    /// <summary>
    /// Declares this object's decorations before the run builds the resolver it resolves its execution graph from.
    /// </summary>
    void Install(ExecutionInstanceResolverBuilder builder);
}
