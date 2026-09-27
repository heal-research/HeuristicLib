using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Evaluators;

/// <remarks>
/// A wrapping evaluator owns a child, so it stays agnostic in the search space and problem and passes the run's triple
/// through unchanged.
/// </remarks>
public abstract record WrappingEvaluator<TCandidate>
    : IEvaluator<TCandidate>
{
    protected WrappingEvaluator(IEvaluator<TCandidate> childEvaluator)
    {
        ChildEvaluator = childEvaluator;
    }

    public IEvaluator<TCandidate> ChildEvaluator { get; init; }

    public virtual bool Fits(ExecutionSignature execution) => execution.Fits(ChildEvaluator);


    /// <summary>
    /// Resolves the child over the run's search space and problem and hands it to <see
    /// cref="WrapExecutionInstance{TRunSearchSpace, TRunProblem}"/>.
    /// </summary>
    public IEvaluatorExecution<TCandidate, TRunSearchSpace, TRunProblem> CreateExecutionInstance<TRunSearchSpace, TRunProblem>(ResolutionScope scope)
        where TRunSearchSpace : class, ISearchSpace<TCandidate>
        where TRunProblem : class, IProblem<TCandidate, TRunSearchSpace> =>
        WrapExecutionInstance(scope.Resolve<TCandidate, TRunSearchSpace, TRunProblem>(ChildEvaluator));

    /// <summary>Wraps the child's execution node in this operator's own.</summary>
    protected abstract IEvaluatorExecution<TCandidate, TRunSearchSpace, TRunProblem> WrapExecutionInstance<TRunSearchSpace, TRunProblem>(IEvaluatorExecution<TCandidate, TRunSearchSpace, TRunProblem> childEvaluator)
        where TRunSearchSpace : class, ISearchSpace<TCandidate>
        where TRunProblem : class, IProblem<TCandidate, TRunSearchSpace>;
}

public abstract class WrappingEvaluatorExecution<TCandidate, TSearchSpace, TProblem>(IEvaluatorExecution<TCandidate, TSearchSpace, TProblem> childEvaluator)
    : EvaluatorExecution<TCandidate, TSearchSpace, TProblem>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    protected IEvaluatorExecution<TCandidate, TSearchSpace, TProblem> ChildEvaluator { get; } = childEvaluator;
}
