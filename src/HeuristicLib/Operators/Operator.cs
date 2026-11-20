using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators;

/// <summary>An operator that builds a problem-bound executor for each run's types in one hook.</summary>
public abstract record Operator<TOperation, TCandidate> : IOperator<TOperation, TCandidate>
    where TOperation : IOperation<TOperation>
{
    IExecutor IOperator<TCandidate>.CreateExecutor<TSearchSpace, TProblem, TSearchState>(
        ResolutionScope<TCandidate, TSearchSpace, TProblem, TSearchState> scope) => CreateExecutor(scope);

    internal virtual string SupportedExecutionDescription => "its declared signature";

    public virtual bool Supports(ExecutionSignature execution) => true;

    /// <inheritdoc cref="IConfigurationNode.ChildExecution"/>
    public virtual ExecutionSignature ChildExecution(ExecutionSignature execution) => execution;

    /// <inheritdoc cref="IConfigurationNode.GetParameters"/>
    public virtual IReadOnlyList<IParameter> GetParameters() => [];

    /// <inheritdoc cref="IOperator{TCandidate}.CreateExecutor{TSearchSpace, TProblem, TSearchState}"/>
    protected abstract IExecutor CreateExecutor<TSearchSpace, TProblem, TSearchState>(
        ResolutionScope<TCandidate, TSearchSpace, TProblem, TSearchState> scope)
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

    public override bool Supports(ExecutionSignature execution) =>
        execution.SearchSpace.IsAssignableTo(typeof(TSearchSpace)) && execution.Problem.IsAssignableTo(typeof(TProblem));

    /// <inheritdoc cref="IConfigurationNode.ChildExecution"/>
    public override ExecutionSignature ChildExecution(ExecutionSignature execution) =>
        new(typeof(TSearchSpace), typeof(TProblem), execution.SearchState);

    // Supports has already accepted the run's types, so the run's problem is a TProblem.
    protected sealed override IExecutor CreateExecutor<TRunSearchSpace, TRunProblem, TRunSearchState>(
        ResolutionScope<TCandidate, TRunSearchSpace, TRunProblem, TRunSearchState> scope) =>
        scope.Problem is TProblem problem
            ? CreateExecutor(new ResolutionScope<TCandidate, TSearchSpace, TProblem, TRunSearchState>(scope, problem))
            : throw ExecutionSignature.Mismatch(this, SupportedExecutionDescription, ExecutionSignature.Describe(typeof(TRunSearchSpace), typeof(TRunProblem), typeof(TRunSearchState)));

    /// <summary>Builds this operator's executor for the scope's problem, resolving children through the scope.</summary>
    /// <remarks>
    /// Resolution calls this once per configuration and problem in a scope, on the first request; later requests for that
    /// pair receive the same executor. Validate the configuration first and look up run results through the scope.
    /// Keep private mutable data in executor fields; explicit sharing retains the executor across child scopes.
    /// </remarks>
    protected abstract TExecutor CreateExecutor<TSearchState>(ResolutionScope<TCandidate, TSearchSpace, TProblem, TSearchState> scope)
        where TSearchState : class, ISearchState;
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

    public override bool Supports(ExecutionSignature execution) =>
        execution.SearchSpace.IsAssignableTo(typeof(TSearchSpace))
        && execution.Problem.IsAssignableTo(typeof(TProblem))
        && execution.SearchState.IsAssignableTo(typeof(TSearchState));

    /// <inheritdoc cref="IConfigurationNode.ChildExecution"/>
    public override ExecutionSignature ChildExecution(ExecutionSignature execution) =>
        ExecutionSignature.For<TSearchSpace, TProblem, TSearchState>();

    // Supports has already accepted the run's types, so the run's problem is a TProblem.
    protected sealed override IExecutor CreateExecutor<TRunSearchSpace, TRunProblem, TRunSearchState>(
        ResolutionScope<TCandidate, TRunSearchSpace, TRunProblem, TRunSearchState> scope) =>
        scope.Problem is TProblem problem
            ? CreateExecutor(new ResolutionScope<TCandidate, TSearchSpace, TProblem, TSearchState>(scope, problem))
            : throw ExecutionSignature.Mismatch(this, SupportedExecutionDescription, ExecutionSignature.Describe(typeof(TRunSearchSpace), typeof(TRunProblem), typeof(TRunSearchState)));

    /// <inheritdoc cref="Operator{TOperation, TCandidate, TSearchSpace, TProblem, TExecutor}.CreateExecutor{TSearchState}"/>
    protected abstract TExecutor CreateExecutor(ResolutionScope<TCandidate, TSearchSpace, TProblem, TSearchState> scope);
}
