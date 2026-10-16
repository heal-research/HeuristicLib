namespace HEAL.HeuristicLib.Execution;

/// <summary>
/// Holds a typed function that wraps an existing executor and supplies the resulting executor's controls.
/// Checks that the function accepts the requested executor interface before it is applied.
/// </summary>
/// <remarks>A forwarding wrapper preserves the child's control provider. A replacement explicitly chooses its controls.</remarks>
public sealed class WrapperExecutorFactory
{
    private readonly Delegate wrapper;

    private WrapperExecutorFactory(Delegate wrapper)
    {
        this.wrapper = wrapper;
    }

    public static WrapperExecutorFactory From<TExecutor>(WrapperExecutorFactory<IExecutorWithControls<TExecutor>> wrapper)
        where TExecutor : class, IExecutor => new(wrapper);

    public WrapperExecutorFactory<IExecutorWithControls<TExecutor>> GetFor<TExecutor>()
        where TExecutor : class, IExecutor =>
        wrapper as WrapperExecutorFactory<IExecutorWithControls<TExecutor>> ??
        throw new InvalidOperationException($"Wrapper factory {wrapper.GetType()} cannot wrap {typeof(TExecutor)}.");
}

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
