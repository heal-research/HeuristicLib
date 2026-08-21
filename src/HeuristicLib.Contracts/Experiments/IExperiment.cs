using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Experiments;

public interface IExperiment<TCandidate, in TSearchSpace, in TProblem, TSearchState, TAlgorithm, TKey>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
    where TSearchState : class, ISearchState
    where TAlgorithm : class, IAlgorithm<TCandidate, TSearchSpace, TProblem, TSearchState>
{
    ImmutableArray<ExperimentCase<TAlgorithm, TKey>> MaterializeCases();
}

public sealed record ExperimentCase<TAlgorithm, TKey>
{
    public TAlgorithm Algorithm { get; }

    public TKey Key { get; }

    public ImmutableArray<int> RandomForkPath { get; }

    public ExperimentCase(TAlgorithm algorithm, TKey key, IReadOnlyList<int> randomForkPath)
    {
        Algorithm = algorithm;
        Key = key;
        RandomForkPath = randomForkPath.ToImmutableArray();
    }
}

public static class ExperimentCase
{
    public static ExperimentCase<TAlgorithm, TKey> From<TAlgorithm, TKey>(TAlgorithm algorithm, TKey key, IReadOnlyList<int> randomForkPath) =>
        new(algorithm, key, randomForkPath);
}
