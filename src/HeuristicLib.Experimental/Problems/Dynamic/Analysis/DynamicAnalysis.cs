using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Problems.Dynamic.Analysis;

public abstract class DynamicAnalysis<TCandidate>(IDynamicProblem<TCandidate, ISearchSpace<TCandidate>> problem) :
  DynamicAnalysis<TCandidate, ISearchSpace<TCandidate>, IDynamicProblem<TCandidate, ISearchSpace<TCandidate>>>(problem);

public abstract class DynamicAnalysis<TCandidate, TSearchSpace>(IDynamicProblem<TCandidate, TSearchSpace> problem) :
  DynamicAnalysis<TCandidate, TSearchSpace, IDynamicProblem<TCandidate, TSearchSpace>>(problem)
  where TSearchSpace : class, ISearchSpace<TCandidate>;

public abstract class DynamicAnalysis<TCandidate, TSearchSpace, TProblem>
  where TSearchSpace : class, ISearchSpace<TCandidate>
  where TProblem : IDynamicProblem<TCandidate, TSearchSpace>
{
    protected readonly TProblem Problem;

    protected DynamicAnalysis(TProblem problem)
    {
        problem.OnEvaluation += Problem_OnEvaluation;
        Problem = problem;
    }

    protected abstract void Problem_OnEvaluation(object? sender, IReadOnlyList<(TCandidate, ObjectiveVector objective, EvaluationTiming timing)> evaluationLog);
}
