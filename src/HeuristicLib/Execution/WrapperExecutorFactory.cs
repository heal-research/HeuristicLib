namespace HEAL.HeuristicLib.Execution;

/// <summary>Constructs a wrapper around one child executor of the same operation interface.</summary>
/// <remarks>
/// Create shared execution data before returning this function. Each construction receives the child resolved
/// for that context; pass that child and the shared data to the new wrapper.
/// </remarks>
/// <typeparam name="TExecutor">The operation interface of the child and returned wrapper.</typeparam>
/// <param name="childExecutor">The child resolved for this construction.</param>
/// <returns>The wrapper around the supplied child.</returns>
public delegate TExecutor WrapperExecutorFactory<TExecutor>(TExecutor childExecutor)
    where TExecutor : class, IExecutor;
