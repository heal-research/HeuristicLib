using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Analysis.Quality;

public record BestQualityAlgorithmAnalysis<TCandidate, TSearchSpace, TProblem> : Analyzer<QualityState>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    public BestQualityAlgorithmAnalysis(params IReadOnlyList<IEvaluator<TCandidate, TSearchSpace, TProblem>> evaluators)
    {
        Evaluators = evaluators.ToImmutableArray();
    }

    public override QualityState CreateInitialResult() => new();

    public override void RegisterObservations(ObservationPlan observations, QualityState result)
    {
        foreach (var evaluator in Evaluators)
            observations.Observe(evaluator, AfterEvaluation);
        return;

        void AfterEvaluation(IReadOnlyList<ObjectiveVector> objectiveVectors, IReadOnlyList<TCandidate> candidates,
                             TSearchSpace searchSpace, TProblem problem)
        {
            if (objectiveVectors.Count == 0)
                return;
            result.CurrentScore ??= objectiveVectors[0];
            var comp = problem.Objective.TotalOrderComparer;
            foreach (var objectiveVector in objectiveVectors)
            {
                result.CurrentScore = comp.Compare(objectiveVector, result.CurrentScore) < 0 ? objectiveVector : result.CurrentScore;
            }
        }
    }

    private ImmutableArray<IEvaluator<TCandidate, TSearchSpace, TProblem>> Evaluators { get; }
}

public class QualityState
{
    public ObjectiveVector? CurrentScore { get; set; }
}
