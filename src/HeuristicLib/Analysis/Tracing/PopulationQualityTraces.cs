using HEAL.HeuristicLib.Algorithms;
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
        public TraceAnalyzer<BestMedianWorst> TracePopulationQuality(IReadOnlyList<Clock>? clocks = null,
            TraceRetention? retention = null, IComparer<ObjectiveVector>? objectiveComparer = null) =>
            Analyzer.Trace(iterations,
                observation => observation.State.Population.EvaluatedCandidates.Select(candidate => candidate.ObjectiveVector)
                    .BestMedianWorst(observation.Problem.Objective, objectiveComparer),
                clocks, retention);

        /// <summary>
        /// Records the best, median and worst evaluated candidate of the population each iteration yields, ranked by
        /// the run's objective.
        /// </summary>
        public TraceAnalyzer<BestMedianWorstEntry<TCandidate>> TracePopulationCandidates(IReadOnlyList<Clock>? clocks = null,
            TraceRetention? retention = null, IComparer<ObjectiveVector>? objectiveComparer = null) =>
            Analyzer.Trace(iterations,
                observation => observation.State.Population.EvaluatedCandidates.BestMedianWorst(observation.Problem.Objective, objectiveComparer),
                clocks, retention);
    }
}
