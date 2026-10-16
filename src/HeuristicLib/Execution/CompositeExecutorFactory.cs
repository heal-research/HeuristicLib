namespace HEAL.HeuristicLib.Execution;

/// <summary>Constructs an executor from an ordered immutable array of child executors of the same operation interface.</summary>
/// <remarks>
/// Create shared execution data before returning this function. Each construction supplies the resolved children,
/// preserving their order, repeated references and empty arrays.
/// </remarks>
/// <typeparam name="TExecutor">The operation interface of the children and returned executor.</typeparam>
/// <param name="childExecutors">The children resolved for this construction.</param>
/// <returns>The executor using the supplied children.</returns>
public delegate TExecutor CompositeExecutorFactory<TExecutor>(ImmutableArray<TExecutor> childExecutors)
    where TExecutor : class, IExecutor;
