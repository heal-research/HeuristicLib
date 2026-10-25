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
/// A configuration whose executor type is known without a run's types, such as a consumer-defined role.
/// </summary>
/// <remarks>
/// Operators and algorithms are not of this kind: their executor type depends on the run's search space and problem,
/// so they build through their own hooks. Advice for such a role derives from
/// <see cref="Advice{TConfiguration, TExecutor}"/>.
/// </remarks>
public interface IConfigurationNode<out TExecutor> : IConfigurationNode
    where TExecutor : class, IExecutor
{
    /// <summary>Builds this configuration's executor, resolving children through the scope.</summary>
    /// <remarks>
    /// Resolution calls this for every build. Validate the configuration first and take shared execution data, such as
    /// counters or caches, from <see cref="ResolutionScope.GetScopedState{TState}"/>, passing this configuration.
    /// </remarks>
    TExecutor CreateExecutor(ResolutionScope scope);
}
