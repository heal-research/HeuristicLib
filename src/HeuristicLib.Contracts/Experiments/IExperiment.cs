using System.Collections.Immutable;
using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Experiments;

public interface IExperiment<TCandidate, in TSearchSpace, in TProblem, TSearchState, TAlgorithm, TKey>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
    where TSearchState : class, ISearchState
    where TAlgorithm : class, IAlgorithm<TCandidate, TSearchSpace, TProblem, TSearchState>
{
    ImmutableArray<ExperimentCase<TAlgorithm, TKey>> MaterializeCases();
}

public sealed record ExperimentCase<TAlgorithm, TKey>(TAlgorithm Algorithm, TKey Key, ImmutableArray<int> RandomForkPath);

public static class ExperimentCase
{
    public static ExperimentCase<TAlgorithm, TKey> From<TAlgorithm, TKey>(TAlgorithm algorithm, TKey key, IReadOnlyList<int> randomForkPath) =>
        new(algorithm, key, randomForkPath.ToImmutableArray());
}
