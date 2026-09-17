namespace HEAL.HeuristicLib.Execution;

/// <summary>
/// A configuration the <see cref="ExecutionInstanceRegistry"/> can resolve into an execution instance.
/// </summary>
public interface IExecutionInstanceResolvable
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
    /// <see cref="ExecutionSignature.Fits(ReadOnlySpan{IExecutionInstanceResolvable})"/>. One that adapts them does
    /// not, and needs no code to say so.
    /// </para>
    /// </remarks>
    bool Fits(ExecutionSignature execution) => true;
}

public interface IExecutionInstance;
