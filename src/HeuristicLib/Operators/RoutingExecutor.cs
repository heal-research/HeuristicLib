using HEAL.HeuristicLib.Execution;

namespace HEAL.HeuristicLib.Operators;

/// <summary>Base for an item operation's routed executor, which runs each item of a call on the child a router assigns it.</summary>
/// <remarks>
/// An item operation's <see cref="IItemOperation{TSelf}.Route{TCandidate, TSearchSpace, TProblem, TSearchState}"/>
/// returns a sealed class that derives from this base and implements the operation's executor interface. Its named
/// method, such as Mutate, routes the call's items through <see cref="Children"/> with the call's random number generator
/// and calls each child with its items and the call's other arguments. The routed executor exposes no controls of its
/// children.
/// </remarks>
/// <param name="children">The children and their weights; every child implements <typeparamref name="TExecutor"/>.</param>
public abstract class RoutingExecutor<TExecutor>(WeightedRouter<IExecutor> children)
    where TExecutor : class, IExecutor
{
    /// <summary>The children and their weights, typed as the operation's executor interface.</summary>
    protected WeightedRouter<TExecutor> Children { get; } = children.Cast<TExecutor>();
}
