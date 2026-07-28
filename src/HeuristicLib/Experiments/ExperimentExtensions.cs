using System.Collections.Immutable;
using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Experiments;

public static class ExperimentExtensions
{
    extension<TCandidate, TSearchSpace, TProblem, TSearchState, TAlgorithm, TKey>(IExperiment<TCandidate, TSearchSpace, TProblem, TSearchState, TAlgorithm, TKey> experiment)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TSearchState : class, ISearchState
        where TAlgorithm : class, IAlgorithm<TCandidate, TSearchSpace, TProblem, TSearchState>
    {
        public ExperimentRun<TCandidate, TSearchSpace, TProblem, TSearchState, TAlgorithm, TKey> CreateRun(TProblem problem, IRandomNumberGenerator random) => new(experiment, problem, random);

        public Execution.ExecutionStream<ExperimentStreamEntry<ExperimentTrial<TCandidate, TSearchSpace, TProblem, TSearchState, TAlgorithm, TKey>, TSearchState>> Stream(TProblem problem, IRandomNumberGenerator random, ExecutionConcurrency? concurrency = null, TSearchState? initialState = null, CancellationToken cancellationToken = default) =>
            experiment.CreateRun(problem, random).Stream(concurrency, initialState, cancellationToken);

        public ImmutableArray<Task<(ExperimentTrial<TCandidate, TSearchSpace, TProblem, TSearchState, TAlgorithm, TKey> Trial, TSearchState State)>> StartTrials(TProblem problem, IRandomNumberGenerator random, ExecutionConcurrency? concurrency = null, TSearchState? initialState = null, CancellationToken cancellationToken = default) =>
            experiment.CreateRun(problem, random).StartTrials(concurrency, initialState, cancellationToken);

        public Task<ImmutableArray<(ExperimentTrial<TCandidate, TSearchSpace, TProblem, TSearchState, TAlgorithm, TKey> Trial, TSearchState State)>> CompleteAsync(TProblem problem, IRandomNumberGenerator random, ExecutionConcurrency? concurrency = null, TSearchState? initialState = null, CancellationToken cancellationToken = default) =>
            experiment.CreateRun(problem, random).CompleteAsync(concurrency, initialState, cancellationToken);

        public ImmutableArray<(ExperimentTrial<TCandidate, TSearchSpace, TProblem, TSearchState, TAlgorithm, TKey> Trial, TSearchState State)> Complete(TProblem problem, IRandomNumberGenerator random, ExecutionConcurrency? concurrency = null, TSearchState? initialState = null, CancellationToken cancellationToken = default) =>
            experiment.CreateRun(problem, random).Complete(concurrency, initialState, cancellationToken);
    }

    extension<TCandidate, TSearchSpace, TProblem, TSearchState, TAlgorithm, TKey>(ExperimentRun<TCandidate, TSearchSpace, TProblem, TSearchState, TAlgorithm, TKey> run)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TSearchState : class, ISearchState
        where TAlgorithm : class, IAlgorithm<TCandidate, TSearchSpace, TProblem, TSearchState>
    {
        public async Task<ImmutableArray<(ExperimentTrial<TCandidate, TSearchSpace, TProblem, TSearchState, TAlgorithm, TKey> Trial, TSearchState State)>> CompleteAsync(
            ExecutionConcurrency? concurrency = null, TSearchState? initialState = null, CancellationToken cancellationToken = default)
        {
            var trialTasks = run.StartTrials(concurrency, initialState, cancellationToken);
            try
            {
                return (await Task.WhenAll(trialTasks)).ToImmutableArray();
            }
            catch
            {
                cancellationToken.ThrowIfCancellationRequested();

                var failures = trialTasks.Select(task => task.Exception).OfType<AggregateException>().SelectMany(exception => exception.InnerExceptions).ToList();
                if (failures.Count > 0)
                    throw new AggregateException(failures);

                throw;
            }
        }

        public ImmutableArray<(ExperimentTrial<TCandidate, TSearchSpace, TProblem, TSearchState, TAlgorithm, TKey> Trial, TSearchState State)> Complete(
            ExecutionConcurrency? concurrency = null, TSearchState? initialState = null, CancellationToken cancellationToken = default) =>
            run.CompleteAsync(concurrency, initialState, cancellationToken).GetAwaiter().GetResult();
    }
}
