namespace HEAL.HeuristicLib.Execution;

/// <summary>Finds a runtime control interface for an executor, such as its mutation-strength control.</summary>
/// <remarks>Returns null when the requested control is unavailable.</remarks>
public interface IExecutorControlProvider
{
    TControl? Get<TControl>() where TControl : class;
}

/// <summary>Returns the supplied object when it implements the requested control interface.</summary>
public sealed class ObjectControlProvider(object target) : IExecutorControlProvider
{
    public TControl? Get<TControl>() where TControl : class => target as TControl;
}

/// <summary>Returns an executor together with access to its runtime controls.</summary>
/// <remarks>Invoke work through Executor; obtain controls through ControlProvider after all wrappers are constructed.</remarks>
public interface IExecutorWithControls<out TExecutor> : IExecutor
    where TExecutor : class, IExecutor
{
    TExecutor Executor { get; }
    IExecutorControlProvider ControlProvider { get; }
}

/// <summary>Keeps a callable executor and its control provider together as one resolution result.</summary>
public sealed class ExecutorWithControls<TExecutor>(TExecutor executor, IExecutorControlProvider controlProvider) : IExecutorWithControls<TExecutor>
    where TExecutor : class, IExecutor
{
    public TExecutor Executor { get; } = executor;
    public IExecutorControlProvider ControlProvider { get; } = controlProvider;
}

/// <summary>Reports that no runtime controls are available.</summary>
public sealed class EmptyExecutorControlProvider : IExecutorControlProvider
{
    public static EmptyExecutorControlProvider Instance { get; } = new();

    private EmptyExecutorControlProvider() { }

    public TControl? Get<TControl>() where TControl : class => null;
}
