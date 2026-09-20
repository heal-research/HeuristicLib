namespace HEAL.HeuristicLib.Execution;

/// <summary>
/// Adds behavior at chosen configurations in a run's execution graph, before the graph is resolved.
/// </summary>
/// <remarks>
/// <para>
/// A module decorates the configurations it is interested in by calling <see cref="ResolutionScopeBuilder.Decorate"/>.
/// It can declare decorations and cannot resolve anything, because a module must not participate in building the graph
/// it decorates. Decorations compose, so several modules may act on the same configuration without displacing one
/// another.
/// </para>
/// <para>
/// Every decoration a module declares sits outside every decoration the configuration declares: run-level additions
/// wrap configuration-level ones. A module therefore always sees the fully configured operator, and a wrapper that
/// measures an operator never measures the module observing it. Among modules, the first installation for a
/// configuration becomes the innermost wrapper and therefore observes an operation first. A module that depends on
/// another one having already acted, such as a trace that reads its clocks, installs its dependencies before itself.
/// </para>
/// <para>
/// Analyzers are the common kind of module, but nothing about this contract is analysis specific. Writing to a log,
/// reporting progress, advancing a dynamic problem at an iteration boundary or bridging to another runtime are equally
/// valid modules.
/// </para>
/// </remarks>
public interface IExecutionModule
{
    /// <summary>
    /// Declares this object's decorations before the run builds the scope it resolves its execution graph from.
    /// </summary>
    void Install(ResolutionScopeBuilder builder);
}
