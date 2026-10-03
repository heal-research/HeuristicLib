namespace HEAL.HeuristicLib.Execution;

/// <summary>Binds an execution node to one resolved child of the same execution role.</summary>
/// <remarks>
/// Prepare persistent state before returning this factory. Each invocation supplies the child for one binding;
/// construct the node with that child and the prepared state.
/// </remarks>
/// <typeparam name="TExecution">The execution role of the child and the returned node.</typeparam>
/// <param name="childExecution">The child resolved for this binding.</param>
/// <returns>The execution node bound to the supplied child.</returns>
public delegate TExecution WrapperExecutionFactory<TExecution>(TExecution childExecution)
    where TExecution : class, IExecutionNode;
