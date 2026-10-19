namespace HEAL.HeuristicLib.Execution;

/// <summary>Finds the runtime controls of a resolved executor, such as its mutation-strength control.</summary>
public static class ExecutorControls
{
    /// <summary>Returns the executor's control of the requested type, or null when it has none.</summary>
    /// <remarks>
    /// An executor implementing <see cref="IExecutorControlProvider"/> supplies its controls, for example by forwarding
    /// those of the executor it wraps. Any other executor is its own control.
    /// </remarks>
    public static TControl? Find<TControl>(IExecutor executor) where TControl : class =>
        executor is IExecutorControlProvider provider ? provider.Get<TControl>() : executor as TControl;

    /// <summary>Returns the control of an executor that passes its calls to an invocation wrapper.</summary>
    /// <remarks>
    /// A wrapper implementing <see cref="IExecutorControlProvider"/> supplies the controls; otherwise they are those of the
    /// wrapped executor. Operation adapters call this from their wrapping executors.
    /// </remarks>
    public static TControl? Find<TControl, TWrapper>(IExecutor executor, TWrapper wrapper)
        where TControl : class
        where TWrapper : struct, IInvocationWrapper =>
        wrapper is IExecutorControlProvider provider ? provider.Get<TControl>() : Find<TControl>(executor);
}

/// <summary>Supplies an executor's runtime controls when they are not the executor itself.</summary>
/// <remarks>
/// A transparent wrapper forwards the controls of the executor it wraps through <see cref="ExecutorControls.Find{TControl}(IExecutor)"/>.
/// A wrapper that replaces behavior implements the controls it offers itself, or none. An invocation wrapper implements
/// this interface to replace the wrapped executor's controls, as a choice among children does by supplying none.
/// </remarks>
public interface IExecutorControlProvider
{
    /// <summary>Returns the requested control, or null when it is unavailable.</summary>
    TControl? Get<TControl>() where TControl : class;
}
