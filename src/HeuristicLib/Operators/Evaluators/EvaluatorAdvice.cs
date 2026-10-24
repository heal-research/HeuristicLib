using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators;

/// <summary>Advice that wraps the evaluation executors built for the evaluators its pointcut selects.</summary>
/// <remarks>
/// A transparent wrapper implements <see cref="IExecutorControlProvider"/> to forward the wrapped executor's
/// controls.
/// </remarks>
public abstract class EvaluatorAdvice<TCandidate> : OperatorAdvice<Evaluation, TCandidate>
{
    public sealed override void Check<TSearchSpace, TProblem, TSearchState>(OperationInfo operation,
        IOperator<Evaluation, TCandidate> source) => Check<TSearchSpace, TProblem>(source);

    public sealed override IExecutor Wrap<TSearchSpace, TProblem, TSearchState>(IExecutor executor,
        OperationInfo operation, IOperator<Evaluation, TCandidate> source) =>
        Wrap((IEvaluationExecutor<TCandidate, TSearchSpace, TProblem>)executor, source);

    /// <summary>Rejects an evaluator this advice cannot wrap for the run's search space and problem, before its executor is built.</summary>
    /// <remarks>The default accepts every evaluator.</remarks>
    protected virtual void Check<TSearchSpace, TProblem>(IOperator<Evaluation, TCandidate> evaluator)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
    { }

    /// <summary>Wraps one evaluation executor built for a selected evaluator.</summary>
    protected abstract IEvaluationExecutor<TCandidate, TSearchSpace, TProblem> Wrap<TSearchSpace, TProblem>(
        IEvaluationExecutor<TCandidate, TSearchSpace, TProblem> executor, IOperator<Evaluation, TCandidate> evaluator)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>;
}
