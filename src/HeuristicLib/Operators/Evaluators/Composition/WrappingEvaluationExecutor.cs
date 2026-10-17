using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Evaluators;

public abstract class WrappingEvaluationExecutor<TCandidate, TSearchSpace, TProblem>(IEvaluationExecutor<TCandidate, TSearchSpace, TProblem> childEvaluator)
    : EvaluationExecutor<TCandidate, TSearchSpace, TProblem>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    protected IEvaluationExecutor<TCandidate, TSearchSpace, TProblem> ChildEvaluator { get; } = childEvaluator;
}
