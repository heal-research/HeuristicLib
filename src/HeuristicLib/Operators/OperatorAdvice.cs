using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators;

/// <summary>Advice that wraps every operation of the operators its pointcut selects, such as measuring each call's duration.</summary>
/// <remarks>
/// Resolution calls <see cref="Check"/> before it builds a selected operator's executor and <see cref="Wrap"/> after.
/// Data that must outlive one executor, such as a counter, belongs to the advice object; it is shared by every operator
/// and resolution the advice is applied to.
/// </remarks>
public abstract class OperatorAdvice : Advice<IOperator>
{
    /// <summary>Rejects an operator this advice cannot wrap, before its executor is built.</summary>
    /// <remarks>The default accepts every operator.</remarks>
    public virtual void Check(OperationExecutorAdapter operation, IOperator source) { }

    /// <summary>Wraps one executor built for a selected operator and returns the executor its callers use.</summary>
    /// <remarks>Usually returns <c>operation.WrapInvocation(executor, wrapper)</c>, which forwards the executor's controls.</remarks>
    public abstract IExecutor Wrap(IExecutor executor, OperationExecutorAdapter operation, IOperator source);
}

// Lets resolution skip another operation's advice without treating arbitrary Advice subclasses as supported.
internal interface ITypedOperatorAdvice;

/// <summary>Advice that wraps one operation, identified by <typeparamref name="TOperation"/>, of the operators its pointcut selects.</summary>
/// <remarks>
/// A configuration implementing several operations receives this advice only for <typeparamref name="TOperation"/>.
/// Role advice bases such as <see cref="MutatorAdvice{TCandidate}"/> pass typed executors; this base passes
/// <see cref="IExecutor"/>, which implements the operation's executor interface for the run types.
/// </remarks>
public abstract class OperatorAdvice<TOperation, TCandidate> : Advice<IOperator<TOperation, TCandidate>>, ITypedOperatorAdvice
    where TOperation : IOperationDescriptor<TOperation>
{
    /// <summary>Rejects an operator this advice cannot wrap for the run's types, before its executor is built.</summary>
    /// <remarks>The default accepts every operator.</remarks>
    public virtual void Check<TSearchSpace, TProblem, TSearchState>(OperationExecutorAdapter operation, IOperator<TOperation, TCandidate> source)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TSearchState : class, ISearchState
    { }

    /// <summary>Wraps one executor built for a selected operator and returns the executor its callers use.</summary>
    public abstract IExecutor Wrap<TSearchSpace, TProblem, TSearchState>(IExecutor executor, OperationExecutorAdapter operation,
        IOperator<TOperation, TCandidate> source)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TSearchState : class, ISearchState;
}
