using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Algorithms;

public interface IRacingStateMerger<TCandidate, TSearchState>
    where TSearchState : PopulationState<TCandidate>
{
    TSearchState Merge(IReadOnlyList<TSearchState> states, ObjectiveDirections objective);
}

public sealed record DelegatingRacingStateMerger<TCandidate, TSearchState>(
    Func<IReadOnlyList<TSearchState>, ObjectiveDirections, TSearchState> MergeFunc)
    : IRacingStateMerger<TCandidate, TSearchState>
    where TSearchState : PopulationState<TCandidate>
{
    public TSearchState Merge(IReadOnlyList<TSearchState> states, ObjectiveDirections objective) =>
        MergeFunc(states, objective);
}

public sealed record BestPopulationStateMerger<TCandidate>(int? PopulationSize = null)
    : IRacingStateMerger<TCandidate, PopulationState<TCandidate>>
{
    public PopulationState<TCandidate> Merge(IReadOnlyList<PopulationState<TCandidate>> states,
                                             ObjectiveDirections objective)
    {
        if (states.Count == 0)
            throw new ArgumentException("At least one state is required.", nameof(states));

        var targetSize = PopulationSize ?? states[0].Population.EvaluatedCandidates.Length;
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(targetSize);

        var comparer = objective.TotalOrderComparer is NoTotalOrderComparer
            ? new LexicographicComparer(objective.Directions)
            : objective.TotalOrderComparer;
        var selected = states.SelectMany(state => state.Population.EvaluatedCandidates)
                             .OrderBy(candidate => candidate.ObjectiveVector, comparer)
                             .Take(targetSize);

        return Population.From(selected).ToPopulationState();
    }
}
