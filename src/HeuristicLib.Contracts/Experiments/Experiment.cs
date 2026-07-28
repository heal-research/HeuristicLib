using System.Collections.Immutable;
using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Experiments;

public abstract record Experiment<TCandidate, TSearchSpace, TProblem, TSearchState, TAlgorithm, TKey>
    : IExperiment<TCandidate, TSearchSpace, TProblem, TSearchState, TAlgorithm, TKey>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
    where TSearchState : class, ISearchState
    where TAlgorithm : class, IAlgorithm<TCandidate, TSearchSpace, TProblem, TSearchState>
{
    public abstract ImmutableArray<ExperimentCase<TAlgorithm, TKey>> MaterializeCases();
}
