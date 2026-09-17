using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Problems.Dynamic;

public sealed record QualityCurvePerEpochAnalysis<TCandidate, TSearchSpace, TProblem>
    : DynamicAnalysis<TCandidate, TSearchSpace, TProblem, QualityCurvePerEpochAnalysisResult<TCandidate>>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : DynamicProblem<TProblem, TCandidate, TSearchSpace>
{
    public QualityCurvePerEpochAnalysis(TProblem problem, params IReadOnlyList<IEvaluator<TCandidate>> evaluators)
        : base(problem, evaluators)
    { }

    public override QualityCurvePerEpochAnalysisResult<TCandidate> CreateInitialResult() => new(Problem.Objective);
}

public sealed class QualityCurvePerEpochAnalysisResult<TCandidate>(ObjectiveDirections objective)
    : IDynamicAnalysisResult<TCandidate>
{
    private readonly List<(TCandidate candidate, ObjectiveVector objectiveVector, EvaluationTiming timing)> bestPerEpoch = [];

    public IReadOnlyList<(TCandidate candidate, ObjectiveVector objectiveVector, EvaluationTiming timing)> BestPerEpoch => bestPerEpoch;

    public void AfterEvaluationLog(object? sender, IReadOnlyList<(TCandidate candidate, ObjectiveVector objective, EvaluationTiming timing)> evaluationLog)
    {
        foreach (var e in evaluationLog.Where(x => x.timing.Valid))
        {
            if (bestPerEpoch.Count > 0)
            {
                var best = bestPerEpoch[^1];
                if (best.timing.Epoch == e.timing.Epoch && objective.TotalOrderComparer.Compare(best.objectiveVector, e.objective) >= 0)
                    continue;
            }

            bestPerEpoch.Add(e);
        }
    }
}
