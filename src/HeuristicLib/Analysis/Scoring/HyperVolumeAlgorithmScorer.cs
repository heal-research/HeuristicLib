using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Analysis.Scoring;

public record HyperVolumeAlgorithmScorer<T, TS, TP, TSearchState>(
    ObjectiveDirections ProblemObjective,
    ObjectiveVector ReferencePoint,
    params IEvaluator<T, TS, TP>[] Evaluator)
    : AlgorithmPerformanceEvaluator<HyperVolumeAlgorithmScorer<T, TS, TP, TSearchState>.State>
    where TS : class, ISearchSpace<T>
    where TP : class, IProblem<T, TS>
    where TSearchState : class, ISearchState
{
    public override State CreateInitialResult() => new();

    public override void RegisterObservations(ObservationPlan observations, State result)
    {
        foreach (var evaluator in Evaluator)
        {
            observations.Observe(evaluator,
                (genotypes, objectives, _, _) =>
                {
                    result.AddPoints(
                        genotypes.Zip(objectives).Select(x => new EvaluatedCandidate<T>(x.First, x.Second)),
                        ProblemObjective, ReferencePoint);
                });
        }
    }

    public override ObjectiveDirections Objective { get; } = SingleObjective.Maximize;

    public class State : ParetoState<T>, IAlgorithmPerformanceState
    {
        public ObjectiveVector CurrentScore => HyperVolume?.Value ?? 0;
    }
}
