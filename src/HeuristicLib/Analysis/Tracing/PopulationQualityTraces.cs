using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Inspection;
using HEAL.HeuristicLib.Objectives;

namespace HEAL.HeuristicLib.Analysis;

public static class PopulationQualityTraces
{
    extension<TCandidate, TSearchState>(ObservationSource<AlgorithmObservation<TCandidate, TSearchState>> iterations)
        where TSearchState : PopulationState<TCandidate>
    {
        /// <summary>
        /// Records the best, median and worst objective vector of the population each iteration yields, ranked by the
        /// run's objective.
        /// </summary>
        public TraceAnalyzer<AlgorithmObservation<TCandidate, TSearchState>, BestMedianWorst> TracePopulationQuality(IReadOnlyList<IClock>? clocks = null,
            TraceRetention? retention = null, IComparer<ObjectiveVector>? objectiveComparer = null) =>
            iterations.Trace(
                observation => observation.State.Population.EvaluatedCandidates.Select(candidate => candidate.ObjectiveVector)
                    .BestMedianWorst(observation.Problem.Objective, objectiveComparer),
                clocks, retention);

        /// <summary>
        /// Records the best, median and worst evaluated candidate of the population each iteration yields, ranked by
        /// the run's objective.
        /// </summary>
        public TraceAnalyzer<AlgorithmObservation<TCandidate, TSearchState>, BestMedianWorstEntry<TCandidate>> TracePopulationCandidates(IReadOnlyList<IClock>? clocks = null,
            TraceRetention? retention = null, IComparer<ObjectiveVector>? objectiveComparer = null) =>
            iterations.Trace(
                observation => observation.State.Population.EvaluatedCandidates.BestMedianWorst(observation.Problem.Objective, objectiveComparer),
                clocks, retention);
    }
}
