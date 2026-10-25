using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators;

/// <summary>An operator that builds its executor for each run's types in one hook.</summary>
public abstract record Operator<TOperation, TCandidate> : IOperator<TOperation, TCandidate>
    where TOperation : IOperation<TOperation>
{
    IExecutor IOperator<TOperation, TCandidate>.CreateExecutor<TSearchSpace, TProblem, TSearchState>(
        ResolutionScope<TCandidate, TSearchSpace, TProblem, TSearchState> scope) => CreateExecutor(scope);

    internal virtual string SupportedExecutionDescription => "its declared signature";

    public virtual bool Fits(ExecutionSignature execution) => true;

    /// <inheritdoc cref="IOperator{TOperation, TCandidate}.CreateExecutor{TSearchSpace, TProblem, TSearchState}"/>
    protected abstract IExecutor CreateExecutor<TSearchSpace, TProblem, TSearchState>(ResolutionScope<TCandidate, TSearchSpace, TProblem, TSearchState> scope)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TSearchState : class, ISearchState;
}

/// <summary>An operator written for fixed search-space and problem contracts, with a typed executor hook.</summary>
public abstract record Operator<TOperation, TCandidate, TSearchSpace, TProblem, TExecutor> : Operator<TOperation, TCandidate>
    where TOperation : IOperation<TOperation>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
    where TExecutor : class, IExecutor
{
    internal override string SupportedExecutionDescription => ExecutionSignature.Describe(typeof(TSearchSpace), typeof(TProblem));

    public override bool Fits(ExecutionSignature execution) =>
        execution.SearchSpace.IsAssignableTo(typeof(TSearchSpace)) && execution.Problem.IsAssignableTo(typeof(TProblem));

    // The call reaches the leaf hook, because overload resolution prefers the hook this class declares.
    protected sealed override IExecutor CreateExecutor<TRunSearchSpace, TRunProblem, TRunSearchState>(
        ResolutionScope<TCandidate, TRunSearchSpace, TRunProblem, TRunSearchState> scope) => CreateExecutor(scope);

    // S4019: the leaf hook hides the run-generic one on purpose; a bound operator builds only through this hook.
#pragma warning disable S4019
    /// <summary>Builds this operator's executor, resolving children through the scope.</summary>
    /// <remarks>
    /// Resolution calls this for every build. Validate the configuration first and take shared execution data from
    /// <see cref="ResolutionScope.GetScopedState{TState}"/>.
    /// </remarks>
    protected abstract TExecutor CreateExecutor(ResolutionScope scope);
#pragma warning restore S4019
}

/// <summary>An operator written for fixed search-space, problem and search-state contracts, with a typed executor hook.</summary>
/// <remarks>A run fits when its types convert to the declared ones; an operation returning its search state requires it exactly.</remarks>
public abstract record Operator<TOperation, TCandidate, TSearchSpace, TProblem, TSearchState, TExecutor> : Operator<TOperation, TCandidate>
    where TOperation : IOperation<TOperation>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
    where TSearchState : class, ISearchState
    where TExecutor : class, IExecutor
{
    internal override string SupportedExecutionDescription => ExecutionSignature.Describe(typeof(TSearchSpace), typeof(TProblem), typeof(TSearchState));

    public override bool Fits(ExecutionSignature execution) =>
        execution.SearchSpace.IsAssignableTo(typeof(TSearchSpace))
        && execution.Problem.IsAssignableTo(typeof(TProblem))
        && execution.SearchState.IsAssignableTo(typeof(TSearchState));

    // The call reaches the leaf hook, because overload resolution prefers the hook this class declares.
    protected sealed override IExecutor CreateExecutor<TRunSearchSpace, TRunProblem, TRunSearchState>(
        ResolutionScope<TCandidate, TRunSearchSpace, TRunProblem, TRunSearchState> scope) => CreateExecutor(scope);

    // S4019: the leaf hook hides the run-generic one on purpose; a bound operator builds only through this hook.
#pragma warning disable S4019
    /// <inheritdoc cref="Operator{TOperation, TCandidate, TSearchSpace, TProblem, TExecutor}.CreateExecutor(ResolutionScope)"/>
    protected abstract TExecutor CreateExecutor(ResolutionScope scope);
#pragma warning restore S4019
}

