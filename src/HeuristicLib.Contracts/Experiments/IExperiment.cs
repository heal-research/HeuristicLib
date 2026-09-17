using HEAL.HeuristicLib.Algorithms;

namespace HEAL.HeuristicLib.Experiments;

/// <remarks>
/// The search space and problem a run happens over belong to
/// <see cref="ExperimentRun{TCandidate, TSearchSpace, TProblem, TSearchState, TAlgorithm, TKey}"/>, where a problem is
/// supplied.
/// </remarks>
public interface IExperiment<TCandidate, TAlgorithm, TSearchState, TKey>
    where TAlgorithm : class, IAlgorithm<TCandidate, TSearchState>
    where TSearchState : class, ISearchState
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
