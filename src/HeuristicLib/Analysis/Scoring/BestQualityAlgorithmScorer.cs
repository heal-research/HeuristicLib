using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Analysis.Scoring;

public record BestQualityAlgorithmScorer<T, TS, TP> : AlgorithmPerformanceEvaluator<QualityScorerState>
    where TS : class, ISearchSpace<T>
    where TP : class, IProblem<T, TS>
{
    public BestQualityAlgorithmScorer(Objective Objective, params IEvaluator<T, TS, TP>[] Evaluator) : base()
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
                (_, objectives, _, _) => result.CurrentScore = objectives.OrderBy(x => x, Objective.TotalOrderComparer).First());
        }
    }

    public override ObjectiveDirections Objective { get; }
    private IEvaluator<TCandidate, TSearchSpace, TProblem>[] Evaluator { get; }
}
