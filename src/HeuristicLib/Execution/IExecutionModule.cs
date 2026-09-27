namespace HEAL.HeuristicLib.Execution;

/// <summary>
/// Adds behavior at chosen configurations in a run's execution graph, before the graph is resolved.
/// </summary>
/// <remarks>
/// <para>
/// A module wraps the configurations it is interested in by calling <see cref="ResolutionScopeBuilder.Wrap"/>.
/// It can declare wrappers and cannot resolve anything, because a module must not participate in building the graph
/// it wraps. Wrappers compose, so several modules may act on the same configuration without displacing one
/// another.
/// </para>
/// <para>
/// Every wrapper a module declares sits outside every wrapper the configuration declares: run-level additions
/// wrap configuration-level ones. A module therefore always sees the fully configured operator, and a wrapper that
/// measures an operator never measures the module observing it. Among modules at the same scope depth, the first
/// registration is innermost: successful exit callbacks run first, and entry work runs last. A module that depends on
/// another one having already acted, such as a trace that reads its clocks, installs its dependencies before itself.
/// </para>
/// <para>
/// Analyzers install most of the modules a run sees, but nothing about this contract is analysis specific. Writing to a
/// log, reporting progress, advancing a dynamic problem at an iteration boundary or bridging to another runtime are
/// equally valid modules.
/// </para>
/// </remarks>
public interface IExecutionModule
{
    /// <summary>
    /// Declares this object's wrappers before the run builds the scope it resolves its execution graph from.
    /// </summary>
    void Install(ResolutionScopeBuilder builder);
}
