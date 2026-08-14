using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Analysis.Quality;

public record HyperVolumeAnalysis<T, TS, TP>(
    ObjectiveDirections ProblemObjective,
    ObjectiveVector ReferencePoint,
    params IEvaluator<T, TS, TP>[] Evaluator)
    : Analyzer<HyperVolumeState<T>>
    where TS : class, ISearchSpace<T>
    where TP : class, IProblem<T, TS>
{
    public override HyperVolumeState<T> CreateInitialResult() => new(ReferencePoint, ProblemObjective);

    public override void RegisterObservations(ObservationPlan observations, HyperVolumeState<T> result)
    {
        foreach (var evaluator in Evaluator)
            observations.Observe(evaluator, (_, evaluatedCandidates, _, _) => AfterEvaluation(result, evaluatedCandidates));
    }

    public void AfterEvaluation(HyperVolumeState<T> result, IReadOnlyList<EvaluatedCandidate<T>> evaluatedCandidates)
    {
        result.AddPoints(evaluatedCandidates);
    }
}

public class HyperVolumeState<T>(ObjectiveVector referencePoint, ObjectiveDirections objective)
    : ParetoState<T>(referencePoint, objective)
{
    private Lazy<double>? hyperVolumeLazy;
    public double HyperVolume => hyperVolumeLazy?.Value ?? 0;

    public override bool AddPoints(IEnumerable<EvaluatedCandidate<T>> evaluatedCandidates)
    {
        if (!base.AddPoints(evaluatedCandidates))
            return false;

        hyperVolumeLazy = new Lazy<double>(() =>
            HyperVolumeCalculator.Calculate(
                Front.Select(x => x.ObjectiveVector),
                ReferencePoint,
                Objective));
        return true;
    }

    public override void Clear()
    {
        base.Clear();
        hyperVolumeLazy = null;
    }
}
