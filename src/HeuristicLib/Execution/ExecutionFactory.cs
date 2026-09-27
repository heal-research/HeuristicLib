namespace HEAL.HeuristicLib.Execution;

/// <summary>Binds a typed execution node to resolved children and observations in a construction scope.</summary>
/// <remarks>
/// A configuration prepares this factory once per logical execution, without a scope. Allocate persistent state
/// during preparation and capture it in the factory. Resolution may invoke the factory repeatedly for different
/// observation contexts; resolve children and construct scope-specific execution nodes inside it. Operator calls
/// use the returned typed node directly and do not invoke the factory or consult the resolver.
/// </remarks>
/// <typeparam name="TExecution">The execution role returned by each binding.</typeparam>
/// <param name="scope">The construction frame that pins dependencies and supplies the requesting observations.</param>
/// <returns>The execution node bound for this scope.</returns>
public delegate TExecution ExecutionFactory<out TExecution>(ResolutionScope scope)
    where TExecution : class, IExecutionNode;
