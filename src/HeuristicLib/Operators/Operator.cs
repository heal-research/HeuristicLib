using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators;

/// <summary>An operator whose factory initializes shared data once and constructs executors for each run's types.</summary>
public abstract record Operator<TOperation, TCandidate> : IOperator<TOperation, TCandidate>
    where TOperation : IOperationDescriptor<TOperation>
{
    public OperatorExecutorFactory CreateExecutorFactory() => CreateOperatorFactory();

    internal virtual string SupportedExecutionDescription => "its declared signature";

    public virtual bool Fits(ExecutionSignature execution) => true;

    protected abstract OperatorExecutorFactory CreateOperatorFactory();
}

/// <summary>An operator written for fixed search-space and problem contracts, with a typed executor construction function.</summary>
public abstract record Operator<TOperation, TCandidate, TSearchSpace, TProblem, TExecutor> : Operator<TOperation, TCandidate>
    where TOperation : IOperationDescriptor<TOperation>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
    where TExecutor : class, IExecutor
{
    internal override string SupportedExecutionDescription => ExecutionSignature.Describe(typeof(TSearchSpace), typeof(TProblem));

    public override bool Fits(ExecutionSignature execution) =>
        execution.SearchSpace.IsAssignableTo(typeof(TSearchSpace)) && execution.Problem.IsAssignableTo(typeof(TProblem));

    protected sealed override OperatorExecutorFactory CreateOperatorFactory() => OperatorExecutorFactory.FromFactory(CreateFactory());

    /// <summary>Creates shared execution data and returns a function that resolves children and constructs an executor.</summary>
    protected abstract ExecutorFactory<TExecutor> CreateFactory();
}

/// <summary>An operator written for fixed search-space, problem and search-state contracts, with a typed executor construction function.</summary>
/// <remarks>A run fits when its types convert to the declared ones; an operation returning its search state requires it exactly.</remarks>
public abstract record Operator<TOperation, TCandidate, TSearchSpace, TProblem, TSearchState, TExecutor> : Operator<TOperation, TCandidate>
    where TOperation : IOperationDescriptor<TOperation>
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

    protected sealed override OperatorExecutorFactory CreateOperatorFactory() => OperatorExecutorFactory.FromFactory(CreateFactory());

    /// <summary>Creates shared execution data and returns a function that resolves children and constructs an executor.</summary>
    protected abstract ExecutorFactory<TExecutor> CreateFactory();
}
