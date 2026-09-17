using HEAL.HeuristicLib.Algorithms;

namespace HEAL.HeuristicLib.Experiments;

public abstract record Experiment<TCandidate, TAlgorithm, TSearchState, TKey>
    : IExperiment<TCandidate, TAlgorithm, TSearchState, TKey>
    where TAlgorithm : class, IAlgorithm<TCandidate, TSearchState>
    where TSearchState : class, ISearchState
{
    public abstract ImmutableArray<ExperimentCase<TAlgorithm, TKey>> MaterializeCases();
}
