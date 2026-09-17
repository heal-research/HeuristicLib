using HEAL.HeuristicLib.Algorithms;

namespace HEAL.HeuristicLib.Experiments;

public sealed record RepeatedExperiment<TCandidate, TAlgorithm, TSearchState>
    : Experiment<TCandidate, TAlgorithm, TSearchState, int>
    where TAlgorithm : class, IAlgorithm<TCandidate, TSearchState>
    where TSearchState : class, ISearchState
{
    public TAlgorithm Algorithm { get; }

    public int Repetitions { get; }

    public RepeatedExperiment(TAlgorithm algorithm, int repetitions)
    {
        if (repetitions <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(repetitions), repetitions, "Repetitions must be positive.");
        }

        Algorithm = algorithm;
        Repetitions = repetitions;
    }

    public override ImmutableArray<ExperimentCase<TAlgorithm, int>> MaterializeCases() =>
        Enumerable.Range(0, Repetitions).Select(repetition => ExperimentCase.From(Algorithm, repetition, [repetition])).ToImmutableArray();
}

public sealed record RepeatedExperiment<TCandidate, TAlgorithm, TSearchState, TInnerKey>
    : Experiment<TCandidate, TAlgorithm, TSearchState, (TInnerKey Inner, int Repetition)>
    where TAlgorithm : class, IAlgorithm<TCandidate, TSearchState>
    where TSearchState : class, ISearchState
{
    public IExperiment<TCandidate, TAlgorithm, TSearchState, TInnerKey> InnerExperiment { get; }

    public int Repetitions { get; }

    public RepeatedExperiment(IExperiment<TCandidate, TAlgorithm, TSearchState, TInnerKey> innerExperiment, int repetitions)
    {
        if (repetitions <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(repetitions), repetitions, "Repetitions must be positive.");
        }

        InnerExperiment = innerExperiment;
        Repetitions = repetitions;
    }

    public override ImmutableArray<ExperimentCase<TAlgorithm, (TInnerKey Inner, int Repetition)>> MaterializeCases() =>
        InnerExperiment.MaterializeCases().SelectMany(experimentCase => Enumerable.Range(0, Repetitions).Select(repetition =>
            ExperimentCase.From(experimentCase.Algorithm, (experimentCase.Key, repetition), [.. experimentCase.RandomForkPath, repetition]))).ToImmutableArray();
}

public static class RepeatedExperimentExtensions
{
    extension<TSelf, TCandidate, TSearchState>(Algorithm<TSelf, TCandidate, TSearchState> algorithm)
        where TSelf : Algorithm<TSelf, TCandidate, TSearchState>
        where TSearchState : class, ISearchState
    {
        public RepeatedExperiment<TCandidate, TSelf, TSearchState> Repeat(int repetitions) => new(algorithm.Self, repetitions);
    }

    extension<TCandidate, TSearchState>(IAlgorithm<TCandidate, TSearchState> algorithm)
        where TSearchState : class, ISearchState
    {
        public RepeatedExperiment<TCandidate, IAlgorithm<TCandidate, TSearchState>, TSearchState> Repeat(int repetitions) => new(algorithm, repetitions);
    }

    extension<TCandidate, TSearchState, TAlgorithm, TInnerKey>(IExperiment<TCandidate, TAlgorithm, TSearchState, TInnerKey> experiment)
        where TAlgorithm : class, IAlgorithm<TCandidate, TSearchState>
        where TSearchState : class, ISearchState
    {
        public RepeatedExperiment<TCandidate, TAlgorithm, TSearchState, TInnerKey> Repeat(int repetitions) => new(experiment, repetitions);
    }
}
