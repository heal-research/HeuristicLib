using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Problems.Dynamic.Analysis;

public class QualityCurvePerEpochAnalysis<TGenotype>(IDynamicProblem<TGenotype, ISearchSpace<TGenotype>> problem) :
  DynamicAnalysis<TGenotype>(problem)
{
  private readonly List<(TGenotype solution, ObjectiveVector objectiveVector, EvaluationTiming timing)> bestPerEpoch = [];
  public IReadOnlyList<(TGenotype solution, ObjectiveVector objectiveVector, EvaluationTiming timing)> BestPerEpoch => bestPerEpoch;

  protected override void Problem_OnEvaluation(object? sender, IReadOnlyList<(TGenotype, ObjectiveVector objective, EvaluationTiming timing)> evaluationLog)
  {
    foreach (var e in evaluationLog.Where(x => x.timing.Valid)) {
      if (bestPerEpoch.Count > 0) {
        var best = bestPerEpoch[^1];
        if (best.timing.Epoch == e.timing.Epoch && Problem.Objective.TotalOrderComparer.Compare(best.objectiveVector, e.objective) >= 0) {
          continue;
        }
      }

      bestPerEpoch.Add(e);
    }
  }
}
