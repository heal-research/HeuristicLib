namespace HEAL.HeuristicLib.Execution;

/// <summary>Constructs an executor using children and advice resolved through the supplied scope.</summary>
/// <remarks>
/// Create counters, caches and other shared execution data before returning this function. Resolution may invoke
/// it again with different advice. Resolve children inside the function and pass them to the returned executor;
/// each existing executor keeps its original children. Operation calls use that executor directly.
/// </remarks>
/// <typeparam name="TExecutor">The interface or class of the returned executor.</typeparam>
/// <param name="scope">The scope used to resolve children with the requesting advice.</param>
/// <returns>The executor constructed for this resolution.</returns>
public delegate TExecutor ExecutorFactory<out TExecutor>(ResolutionScope scope)
    where TExecutor : class, IExecutor;
