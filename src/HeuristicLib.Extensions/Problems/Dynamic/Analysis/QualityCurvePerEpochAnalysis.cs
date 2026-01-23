using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Problems.Dynamic.Analysis;

public abstract class DynamicAnalysis<TGenotype, TSearchSpace, TProblem>
  where TGenotype : class
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : IDynamicProblem<TGenotype, TSearchSpace> {
  protected readonly TProblem Problem;

  protected DynamicAnalysis(TProblem problem) {
    problem.OnEvaluation += Problem_OnEvaluation;
    Problem = problem;
  }

  protected abstract void Problem_OnEvaluation(object? sender, IReadOnlyList<(TGenotype, ObjectiveVector objective, EvaluationTiming timing)> evaluationLog);
}

public abstract class DynamicAnalysis<TGenotype, TSearchSpace>(IDynamicProblem<TGenotype, TSearchSpace> problem) :
  DynamicAnalysis<TGenotype, TSearchSpace, IDynamicProblem<TGenotype, TSearchSpace>>(problem)
  where TGenotype : class
  where TSearchSpace : class, ISearchSpace<TGenotype>;

public abstract class DynamicAnalysis<TGenotype>(IDynamicProblem<TGenotype, ISearchSpace<TGenotype>> problem) :
  DynamicAnalysis<TGenotype, ISearchSpace<TGenotype>, IDynamicProblem<TGenotype, ISearchSpace<TGenotype>>>(problem)
  where TGenotype : class;

public class QualityCurvePerEpochAnalysis<TGenotype>(IDynamicProblem<TGenotype, ISearchSpace<TGenotype>> problem) :
  DynamicAnalysis<TGenotype>(problem)
  where TGenotype : class {
  private readonly List<(TGenotype solution, ObjectiveVector objectiveVector, EvaluationTiming timing)> bestPerEpoch = [];
  public IReadOnlyList<(TGenotype solution, ObjectiveVector objectiveVector, EvaluationTiming timing)> BestPerEpoch => bestPerEpoch;

  protected override void Problem_OnEvaluation(object? sender, IReadOnlyList<(TGenotype, ObjectiveVector objective, EvaluationTiming timing)> evaluationLog) {
    foreach (var e in evaluationLog.Where(x => x.timing.Valid)) {
      if (bestPerEpoch.Count > 0) {
        var best = bestPerEpoch[^1];
        if (best.timing.Epoch == e.timing.Epoch && Problem.Objective.TotalOrderComparer.Compare(best.objectiveVector, e.objective) >= 0)
          continue;
      }

      bestPerEpoch.Add(e);
    }
  }
}
