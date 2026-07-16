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
            observations.Observe(evaluator, (c, o, _, _) => AfterEvaluation(result, c, o));
    }

    public void AfterEvaluation(HyperVolumeState<T> result, IReadOnlyList<T> genotypes,
                                IReadOnlyList<ObjectiveVector> objectives)
    {
        result.AddPoints(genotypes.Zip(objectives).Select(x => x.First.ToEvaluated(x.Second)));
    }
}
