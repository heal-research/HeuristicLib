using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Analysis.Quality;

public record BestQualityAlgorithmAnalysis<TCandidate, TSearchSpace, TProblem> : Analyzer<QualityState>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    public BestQualityAlgorithmAnalysis(params IEvaluator<TCandidate, TSearchSpace, TProblem>[] Evaluator)
    {
        this.Evaluator = Evaluator;
    }

    public override QualityState CreateInitialResult() => new();

    public override void RegisterObservations(ObservationPlan observations, QualityState result)
    {
        foreach (var evaluator in Evaluator)
            observations.Observe(evaluator, AfterEvaluation);
        return;

        void AfterEvaluation(IReadOnlyList<TCandidate> candidates, IReadOnlyList<EvaluatedCandidate<TCandidate>> evaluatedCandidates,
                             TSearchSpace searchSpace, TProblem problem)
        {
            if (evaluatedCandidates.Count == 0)
                return;
            result.CurrentScore ??= evaluatedCandidates[0].ObjectiveVector;
            var comp = problem.Objective.TotalOrderComparer;
            foreach (var evaluatedCandidate in evaluatedCandidates)
            {
                var o = evaluatedCandidate.ObjectiveVector;
                result.CurrentScore = comp.Compare(o, result.CurrentScore) < 0 ? o : result.CurrentScore;
            }
        }
    }

    private IEvaluator<TCandidate, TSearchSpace, TProblem>[] Evaluator { get; }
}

public class QualityState
{
    public ObjectiveVector? CurrentScore { get; set; }
}
