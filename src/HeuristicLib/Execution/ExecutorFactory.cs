namespace HEAL.HeuristicLib.Execution;

/// <summary>
/// Holds a typed function that constructs an executor and supplies its controls. Returns that function for a
/// compatible executor interface, and reports incompatible requests before construction.
/// </summary>
public sealed class ExecutorFactory
{
    private readonly Delegate factory;

    private ExecutorFactory(Delegate factory)
    {
        this.factory = factory;
    }

    public static ExecutorFactory FromFactory<TExecutor>(ExecutorFactory<TExecutor> factory)
        where TExecutor : class, IExecutor =>
        FromFactoryWithControls<TExecutor>(scope =>
        {
            var executor = factory(scope);
            return new ExecutorWithControls<TExecutor>(executor, new ObjectControlProvider(executor));
        });

    /// <summary>Stores a construction function that explicitly supplies the returned executor's control provider.</summary>
    public static ExecutorFactory FromFactoryWithControls<TExecutor>(ExecutorFactory<IExecutorWithControls<TExecutor>> factory)
        where TExecutor : class, IExecutor => new(factory);

    // Preserve the authored delegate's covariance; no requested-type closure is introduced here.
    public ExecutorFactory<IExecutorWithControls<TExecutor>> GetFor<TExecutor>()
        where TExecutor : class, IExecutor =>
        factory as ExecutorFactory<IExecutorWithControls<TExecutor>> ??
        throw new InvalidOperationException($"Executor factory {factory.GetType()} cannot provide {typeof(TExecutor)}.");
}

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
