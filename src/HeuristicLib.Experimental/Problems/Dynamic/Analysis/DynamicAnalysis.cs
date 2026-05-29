using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Problems.Dynamic.Analysis;

public abstract class DynamicAnalysis<TGenotype>(IDynamicProblem<TGenotype, ISearchSpace<TGenotype>> problem) :
  DynamicAnalysis<TGenotype, ISearchSpace<TGenotype>, IDynamicProblem<TGenotype, ISearchSpace<TGenotype>>>(problem);

public abstract class DynamicAnalysis<TGenotype, TSearchSpace>(IDynamicProblem<TGenotype, TSearchSpace> problem) :
  DynamicAnalysis<TGenotype, TSearchSpace, IDynamicProblem<TGenotype, TSearchSpace>>(problem)
  where TSearchSpace : class, ISearchSpace<TGenotype>;

public abstract class DynamicAnalysis<TGenotype, TSearchSpace, TProblem>
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : IDynamicProblem<TGenotype, TSearchSpace>
{
    protected readonly TProblem Problem;

    protected DynamicAnalysis(TProblem problem)
    {
        problem.OnEvaluation += Problem_OnEvaluation;
        Problem = problem;
    }

    protected abstract void Problem_OnEvaluation(object? sender, IReadOnlyList<(TGenotype, ObjectiveVector objective, EvaluationTiming timing)> evaluationLog);
}
