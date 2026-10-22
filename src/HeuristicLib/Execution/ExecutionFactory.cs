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

/// <summary>Prepares factories whose persistent state explicitly supports independent copies.</summary>
public static class ExecutionFactory
{
    /// <summary>Creates a factory whose state is copied when its owning scope is forked.</summary>
    /// <remarks>
    /// The copy function must return independent mutable state and must not resolve children. The binding function
    /// resolves children through its supplied scope and uses only its supplied state, never the original state.
    /// Configurations and immutable settings may be shared. Executions and observation bindings are rebuilt.
    /// </remarks>
    public static ExecutionFactory<TExecution> Forkable<TState, TExecution>(TState state,
        Func<TState, TState> copyState, Func<TState, ResolutionScope, TExecution> bind)
        where TExecution : class, IExecutionNode =>
        new Preparation<TState, TExecution>(state, copyState, bind).Bind;

    private sealed class Preparation<TState, TExecution>(TState state,
        Func<TState, TState> copyState, Func<TState, ResolutionScope, TExecution> bind) : IForkableFactoryPreparation
        where TExecution : class, IExecutionNode
    {
        public TExecution Bind(ResolutionScope scope) => bind(state, scope);

        public Delegate Fork() => Forkable(copyState(state), copyState, bind);
    }
}

internal interface IForkableFactoryPreparation
{
    Delegate Fork();
}
