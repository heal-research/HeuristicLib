using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Evaluators;

public abstract class MultiEvaluationExecutor<TCandidate, TSearchSpace, TProblem>(ImmutableArray<IEvaluationExecutor<TCandidate, TSearchSpace, TProblem>> childEvaluators)
    : EvaluationExecutor<TCandidate, TSearchSpace, TProblem>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    protected ImmutableArray<IEvaluationExecutor<TCandidate, TSearchSpace, TProblem>> ChildEvaluators { get; } = childEvaluators;
}
