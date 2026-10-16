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

    /// <summary>Prepares this operator once, then binds its child in each construction scope.</summary>
    public ExecutorFactory<IEvaluationExecutor<TCandidate, TRunSearchSpace, TRunProblem>> CreateExecutorFactory<TRunSearchSpace, TRunProblem>()
        where TRunSearchSpace : class, ISearchSpace<TCandidate>
        where TRunProblem : class, IProblem<TCandidate, TRunSearchSpace>
    {
        var create = CreateWrapperFactory<TRunSearchSpace, TRunProblem>();
        return scope => create(scope.Resolve<TCandidate, TRunSearchSpace, TRunProblem>(ChildEvaluator));
    }

    /// <summary>Prepares persistent execution data and returns a constructor accepting the resolved child.</summary>
    /// <remarks>Allocate shared state here; construct nodes with their contextual children in the returned delegate.</remarks>
    protected abstract WrapperExecutorFactory<IEvaluationExecutor<TCandidate, TRunSearchSpace, TRunProblem>> CreateWrapperFactory<TRunSearchSpace, TRunProblem>()
        where TRunSearchSpace : class, ISearchSpace<TCandidate>
        where TRunProblem : class, IProblem<TCandidate, TRunSearchSpace>;
}

public abstract class WrappingEvaluationExecutor<TCandidate, TSearchSpace, TProblem>(IEvaluationExecutor<TCandidate, TSearchSpace, TProblem> childEvaluator)
    : EvaluationExecutor<TCandidate, TSearchSpace, TProblem>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    protected IEvaluationExecutor<TCandidate, TSearchSpace, TProblem> ChildEvaluator { get; } = childEvaluator;
}
