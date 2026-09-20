using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Objectives;

namespace HEAL.HeuristicLib.Analysis;

public sealed record BestSoFarAggregation : IAggregation<ObjectiveVector, ObjectiveVector>
{
    public IAggregationInstance<ObjectiveVector, ObjectiveVector> CreateExecutionInstance(ResolutionScope scope) => new ExecutionInstance();

    private sealed class ExecutionInstance : IAggregationInstance<ObjectiveVector, ObjectiveVector>
    {
        private ObjectiveVector? best;
        private readonly BestAggregation aggregation = new();

        public ObjectiveVector Aggregate(IReadOnlyList<ObjectiveVector> readings, ObjectiveDirections objective, IComparer<ObjectiveVector>? objectiveComparer = null)
        {
            var current = aggregation.Aggregate(readings, objective, objectiveComparer);
            if (best is null || objective.RequireTotalOrder(objectiveComparer).Compare(current, best) < 0)
                best = current;
            return best;
        }
    }
}

public sealed record BestCandidateSoFarAggregation<TCandidate> : IAggregation<EvaluatedCandidate<TCandidate>, EvaluatedCandidate<TCandidate>>
{
    public IAggregationInstance<EvaluatedCandidate<TCandidate>, EvaluatedCandidate<TCandidate>> CreateExecutionInstance(ResolutionScope scope) => new ExecutionInstance();

    private sealed class ExecutionInstance : IAggregationInstance<EvaluatedCandidate<TCandidate>, EvaluatedCandidate<TCandidate>>
    {
        private EvaluatedCandidate<TCandidate>? best;
        private readonly BestCandidateAggregation<TCandidate> aggregation = new();

        public EvaluatedCandidate<TCandidate> Aggregate(IReadOnlyList<EvaluatedCandidate<TCandidate>> readings, ObjectiveDirections objective, IComparer<ObjectiveVector>? objectiveComparer = null)
        {
            var current = aggregation.Aggregate(readings, objective, objectiveComparer);
            if (best is null || objective.RequireTotalOrder(objectiveComparer).Compare(current.ObjectiveVector, best.ObjectiveVector) < 0)
                best = current;
            return best;
        }
    }
}
