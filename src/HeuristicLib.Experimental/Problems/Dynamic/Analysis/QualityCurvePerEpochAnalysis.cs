using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Problems.Dynamic.Analysis;

public class QualityCurvePerEpochAnalysis<TCandidate>(IDynamicProblem<TCandidate, ISearchSpace<TCandidate>> problem) :
  DynamicAnalysis<TCandidate>(problem)
{
    private readonly List<(TCandidate solution, ObjectiveVector objectiveVector, EvaluationTiming timing)> bestPerEpoch = [];
    public IReadOnlyList<(TCandidate solution, ObjectiveVector objectiveVector, EvaluationTiming timing)> BestPerEpoch => bestPerEpoch;

    protected override void Problem_OnEvaluation(object? sender, IReadOnlyList<(TCandidate, ObjectiveVector objective, EvaluationTiming timing)> evaluationLog)
    {
        foreach (var e in evaluationLog.Where(x => x.timing.Valid))
        {
            if (bestPerEpoch.Count > 0)
            {
                var best = bestPerEpoch[^1];
                if (best.timing.Epoch == e.timing.Epoch && Problem.Objective.TotalOrderComparer.Compare(best.objectiveVector, e.objective) >= 0)
                {
                    continue;
                }
            }

            bestPerEpoch.Add(e);
        }
    }
}
