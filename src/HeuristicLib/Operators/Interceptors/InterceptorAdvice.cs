using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators;

/// <summary>Advice that wraps the interception executors built for the interceptors its pointcut selects.</summary>
/// <remarks>A transparent wrapper implements <see cref="IExecutorControlProvider"/> to forward the child's controls.</remarks>
public abstract class InterceptorAdvice<TCandidate, TSearchState> : OperatorAdvice<Interception<TSearchState>, TCandidate>
    where TSearchState : class, ISearchState
{
    public sealed override void Check<TSearchSpace, TProblem, TRunSearchState>(OperationInfo operation,
        IOperator<Interception<TSearchState>, TCandidate> source) => Check<TSearchSpace, TProblem>(source);

    public sealed override IExecutor Wrap<TSearchSpace, TProblem, TRunSearchState>(IExecutor executor,
        OperationInfo operation, IOperator<Interception<TSearchState>, TCandidate> source) =>
        Wrap((IInterceptionExecutor<TCandidate, TSearchSpace, TProblem, TSearchState>)executor, source);

    /// <summary>Rejects an interceptor this advice cannot wrap for the run's types, before its executor is built.</summary>
    /// <remarks>The default accepts every interceptor.</remarks>
    protected virtual void Check<TSearchSpace, TProblem>(IOperator<Interception<TSearchState>, TCandidate> interceptor)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
    { }

    /// <summary>Wraps one interception executor built for a selected interceptor.</summary>
    protected abstract IInterceptionExecutor<TCandidate, TSearchSpace, TProblem, TSearchState> Wrap<TSearchSpace, TProblem>(
        IInterceptionExecutor<TCandidate, TSearchSpace, TProblem, TSearchState> executor, IOperator<Interception<TSearchState>, TCandidate> interceptor)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>;
}
