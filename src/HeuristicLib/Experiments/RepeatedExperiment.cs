using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Experiments;

public sealed record RepeatedExperiment<TCandidate, TSearchSpace, TProblem, TSearchState, TAlgorithm>
    : Experiment<TCandidate, TSearchSpace, TProblem, TSearchState, TAlgorithm, int>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
    where TSearchState : class, ISearchState
    where TAlgorithm : class, IAlgorithm<TCandidate, TSearchSpace, TProblem, TSearchState>
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
        Enumerable.Range(0, Repetitions).Select(repetition => new ExperimentCase<TAlgorithm, int>(Algorithm, repetition, [repetition])).ToImmutableArray();
}

public sealed record RepeatedExperiment<TCandidate, TSearchSpace, TProblem, TSearchState, TAlgorithm, TInnerKey>
    : Experiment<TCandidate, TSearchSpace, TProblem, TSearchState, TAlgorithm, (TInnerKey Inner, int Repetition)>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
    where TSearchState : class, ISearchState
    where TAlgorithm : class, IAlgorithm<TCandidate, TSearchSpace, TProblem, TSearchState>
{
    public IExperiment<TCandidate, TSearchSpace, TProblem, TSearchState, TAlgorithm, TInnerKey> InnerExperiment { get; }

    public int Repetitions { get; }

    public RepeatedExperiment(IExperiment<TCandidate, TSearchSpace, TProblem, TSearchState, TAlgorithm, TInnerKey> innerExperiment, int repetitions)
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
            new ExperimentCase<TAlgorithm, (TInnerKey Inner, int Repetition)>(experimentCase.Algorithm, (experimentCase.Key, repetition), [.. experimentCase.RandomForkPath, repetition]))).ToImmutableArray();
}

public static class RepeatedExperimentExtensions
{
    extension<TAlgorithm, TCandidate, TSearchSpace, TProblem, TSearchState>(Algorithm<TAlgorithm, TCandidate, TSearchSpace, TProblem, TSearchState> algorithm)
        where TAlgorithm : Algorithm<TAlgorithm, TCandidate, TSearchSpace, TProblem, TSearchState>
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TSearchState : class, ISearchState
    {
        public RepeatedExperiment<TCandidate, TSearchSpace, TProblem, TSearchState, TAlgorithm> Repeat(int repetitions) => new(algorithm.Self, repetitions);
    }

    extension<TCandidate, TSearchSpace, TProblem, TSearchState>(IAlgorithm<TCandidate, TSearchSpace, TProblem, TSearchState> algorithm)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TSearchState : class, ISearchState
    {
        public RepeatedExperiment<TCandidate, TSearchSpace, TProblem, TSearchState, IAlgorithm<TCandidate, TSearchSpace, TProblem, TSearchState>> Repeat(int repetitions) => new(algorithm, repetitions);
    }

    extension<TCandidate, TSearchSpace, TProblem, TSearchState, TAlgorithm, TInnerKey>(IExperiment<TCandidate, TSearchSpace, TProblem, TSearchState, TAlgorithm, TInnerKey> experiment)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TSearchState : class, ISearchState
        where TAlgorithm : class, IAlgorithm<TCandidate, TSearchSpace, TProblem, TSearchState>
    {
        public RepeatedExperiment<TCandidate, TSearchSpace, TProblem, TSearchState, TAlgorithm, TInnerKey> Repeat(int repetitions) => new(experiment, repetitions);
    }
}
