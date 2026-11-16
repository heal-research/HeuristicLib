namespace HEAL.HeuristicLib.Execution;

/// <summary>
/// A configuration the <see cref="ResolutionScope"/> can resolve into an executor.
/// </summary>
public interface IConfigurationNode
{
    /// <summary>
    /// Whether this configuration itself was written for <paramref name="execution"/>. A configuration that names no search
    /// space, problem or search state supports every execution, which is the default.
    /// </summary>
    /// <remarks>
    /// This is the rule the authoring bases apply when they bridge to an execution's types, so resolution and pre-flight
    /// validation answer one question rather than two that resemble each other. It is decided from type arguments alone:
    /// nothing is constructed, which is what lets validation reach children a run would only build later.
    /// <para>
    /// It answers for this configuration alone. <see cref="ExecutionSignature.Supports(IConfigurationNode)"/> asks the
    /// declared children too, using <see cref="ChildExecution"/> followed by each parameter's
    /// <see cref="Parameter{TOwner, TValue}.ChildExecution"/> mapping. Fixed-signature bases hand their declared types to
    /// children; a composition running a child under other types states them on the parameter.
    /// </para>
    /// </remarks>
    bool Supports(ExecutionSignature execution) => true;

    /// <summary>The execution this configuration hands to the children it declares.</summary>
    /// <remarks>
    /// The default returns <paramref name="execution"/> unchanged. Fixed-signature bases return their declared types,
    /// matching the typed scope their construction hook receives. Each child parameter applies its own
    /// <see cref="Parameter{TOwner, TValue}.ChildExecution"/> mapping after this one.
    /// </remarks>
    ExecutionSignature ChildExecution(ExecutionSignature execution) => execution;

    /// <summary>The parameters this configuration declares, including inherited ones.</summary>
    /// <remarks>
    /// A walk over a configuration graph reaches a child only through its declaration, so a configuration that declares
    /// nothing is a leaf to it, which is the default. See <see cref="IParameter"/> for the form of a declaration.
    /// </remarks>
    IReadOnlyList<IParameter> GetParameters() => [];
}

/// <summary>
/// A configuration whose executor type is known without a run's types, such as a consumer-defined role.
/// </summary>
/// <remarks>
/// Operators and algorithms are not of this kind: their executor type depends on the run's search space and problem,
/// so they build through their own hooks. A wrapper for such a role is another configuration of the role that resolves
/// the configuration it wraps.
/// </remarks>
public interface IConfigurationNode<out TExecutor> : IConfigurationNode
    where TExecutor : class, IExecutor
{
    /// <summary>Builds this configuration's executor, resolving children through the scope.</summary>
    /// <remarks>
    /// Resolution calls this once per resolution scope, on the first request; later requests in the scope receive the same
    /// executor. Validate configuration first. Obtain shared resources through explicit handles when sharing is intended.
    /// Keep private mutable data in executor fields.
    /// </remarks>
    TExecutor CreateExecutor(ResolutionScope scope);
}
