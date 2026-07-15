using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Analysis.Scoring;

public record
    BestQualityAlgorithmScorer<TCandidate, TSearchSpace, TProblem> : AlgorithmPerformanceEvaluator<QualityScorerState>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    public BestQualityAlgorithmScorer(ObjectiveDirections Objective,
                                      params IEvaluator<TCandidate, TSearchSpace, TProblem>[] Evaluator) : base()
    {
        this.Objective = Objective;
        this.Evaluator = Evaluator;
    }

    public override QualityScorerState CreateInitialResult() => new() { CurrentScore = Objective.Worst };

    public override void RegisterObservations(ObservationPlan observations, QualityScorerState result)
    {
        foreach (var evaluator in Evaluator)
        {
            observations.Observe(evaluator,
                (_, objectives, _, _) =>
                    result.CurrentScore = objectives.OrderBy(x => x, Objective.TotalOrderComparer).First());
        }
    }

    public override ObjectiveDirections Objective { get; }
    private IEvaluator<TCandidate, TSearchSpace, TProblem>[] Evaluator { get; }
}
