namespace HEAL.HeuristicLib.Execution;

/// <summary>
/// Hooks itself into a run's execution graph at chosen anchors, before the graph is resolved.
/// </summary>
/// <remarks>
/// <para>
/// A hook decorates the anchors it is interested in by calling <see cref="ExecutionInstanceRegistry.Decorate"/>.
/// Decorations compose, so several hooks may act on the same anchor without displacing one another.
/// </para>
/// <para>
/// Hooks are installed in the order they are passed to the run. The first installation at an anchor becomes the
/// innermost wrapper and therefore observes an operation first. A hook that depends on another one having already
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
    /// Installs this object's decorations into the registry the run is about to resolve its execution graph from.
    /// </summary>
    void Install(ExecutionInstanceRegistry registry);
}
