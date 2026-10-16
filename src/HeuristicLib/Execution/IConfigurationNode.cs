namespace HEAL.HeuristicLib.Execution;

/// <summary>
/// A configuration the <see cref="ResolutionScope"/> can resolve into an executor.
/// </summary>
public interface IConfigurationNode
{
    /// <summary>
    /// Whether this configuration, and everything it resolves under the same signature, was written for
    /// <paramref name="execution"/>. A configuration that names no search space, problem or search state fits every
    /// execution, which is the default.
    /// </summary>
    /// <remarks>
    /// This is the rule the authoring bases apply when they bridge to an execution's types, so resolution and
    /// pre-flight validation answer one question rather than two that resemble each other. It is decided from type
    /// arguments alone: nothing is constructed, which is what lets validation reach children a run would only build
    /// later.
    /// <para>
    /// A composition that passes its signature through to its children answers for them with
    /// <see cref="ExecutionSignature.Fits(ReadOnlySpan{IConfigurationNode})"/>. One that adapts them does
    /// not, and needs no code to say so.
    /// </para>
    /// </remarks>
    bool Fits(ExecutionSignature execution) => true;
}

/// <summary>
/// A configuration whose executor type is known without a run's types, so it can supply a typed factory directly.
/// </summary>
/// <remarks>
/// Role configurations such as mutators are not of this kind: their executor type depends on the run's search space
/// and problem, so they are resolved through
/// <see cref="ResolutionScope.Resolve{TConfiguration, TExecutor}(TConfiguration, Func{TConfiguration, ExecutorFactory{TExecutor}}, Func{Advice, TConfiguration, WrapperExecutorFactory{TExecutor}})"/>
/// instead.
/// </remarks>
public interface IConfigurationNode<out TExecutor> : IConfigurationNode
    where TExecutor : class, IExecutor
{
    /// <summary>Creates shared execution data once and returns a function that constructs executors using that data.</summary>
    ExecutorFactory<TExecutor> CreateExecutorFactory();
}

/// <summary>A runtime object that performs operations, such as mutating candidates or running an algorithm.</summary>
/// <remarks>Operation-specific interfaces define its callable methods. One executor can serve many calls.</remarks>
public interface IExecutor;
