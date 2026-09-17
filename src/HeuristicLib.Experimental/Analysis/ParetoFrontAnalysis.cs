using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Analysis;

public record ParetoFrontAnalysis<T, TS, TP>(
    ObjectiveDirections ProblemObjective,
    ObjectiveVector ReferencePoint,
    params IEvaluator<T>[] Evaluator)
    : Analyzer<ParetoState<T>>
    where TS : class, ISearchSpace<T>
    where TP : class, IProblem<T, TS>
{
    public override HyperVolumeState<T> CreateInitialResult() => new(ReferencePoint, ProblemObjective);

    public override void RegisterObservations(ObservationPlan observations, ParetoState<T> result)
    {
        foreach (var evaluator in Evaluator)
            observations.Observe<T, TS, TP>(evaluator, (objectiveVectors, candidates, _, _) => AfterEvaluation(result, candidates.ToEvaluated(objectiveVectors)));
    }

    public void AfterEvaluation(ParetoState<T> result, IReadOnlyList<EvaluatedCandidate<T>> evaluatedCandidates)
    {
        result.AddPoints(evaluatedCandidates);
    }
}
