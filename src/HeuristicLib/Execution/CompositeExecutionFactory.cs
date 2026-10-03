namespace HEAL.HeuristicLib.Execution;

/// <summary>Binds an execution node to an ordered immutable array of children of the same execution role.</summary>
/// <remarks>
/// Prepare persistent state before returning this factory. Each invocation supplies the children for one binding,
/// preserving their order, repeated references and empty arrays.
/// </remarks>
/// <typeparam name="TExecution">The execution role of the children and the returned node.</typeparam>
/// <param name="childExecutions">The children resolved for this binding.</param>
/// <returns>The execution node bound to the supplied children.</returns>
public delegate TExecution CompositeExecutionFactory<TExecution>(ImmutableArray<TExecution> childExecutions)
    where TExecution : class, IExecutionNode;
