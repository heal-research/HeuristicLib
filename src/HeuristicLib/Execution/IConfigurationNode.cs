namespace HEAL.HeuristicLib.Execution;

/// <summary>
/// A configuration the <see cref="ResolutionScope"/> can resolve into an execution node.
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
/// A configuration whose execution node type is known without a run's types, so it can create that execution
/// itself.
/// </summary>
/// <remarks>
/// Role configurations such as mutators are not of this kind: their execution type depends on the run's search space
/// and problem, so they are resolved through
/// <see cref="ResolutionScope.Resolve{TConfiguration, TExecution}(TConfiguration, Func{TConfiguration, ResolutionScope, TExecution})"/>
/// instead.
/// </remarks>
public interface IConfigurationNode<out TExecution> : IConfigurationNode
  where TExecution : IExecutionNode
{
    TExecution CreateExecutionInstance(ResolutionScope scope);
}

public interface IExecutionNode;
