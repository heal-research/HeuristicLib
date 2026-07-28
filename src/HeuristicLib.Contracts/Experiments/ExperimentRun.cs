using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Analysis;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Experiments;

public sealed class ExperimentRun<TCandidate, TSearchSpace, TProblem, TSearchState, TAlgorithm, TKey>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
    where TSearchState : class, ISearchState
    where TAlgorithm : class, IAlgorithm<TCandidate, TSearchSpace, TProblem, TSearchState>
{
    private readonly Dictionary<TrialAnalyzer, ImmutableArray<IAnalyzer>> trialAnalyzers = new(ReferenceEqualityComparer.Instance);
    private bool executionStarted;

    public IExperiment<TCandidate, TSearchSpace, TProblem, TSearchState, TAlgorithm, TKey> Experiment { get; }

    public TProblem Problem { get; }

    public IRandomNumberGenerator Random { get; }

    public ImmutableArray<ExperimentTrial<TCandidate, TSearchSpace, TProblem, TSearchState, TAlgorithm, TKey>> Trials { get; }

    public bool ExecutionStarted => executionStarted || Trials.Any(trial => trial.Run.ExecutionStarted);

    public ExperimentRun(IExperiment<TCandidate, TSearchSpace, TProblem, TSearchState, TAlgorithm, TKey> experiment, TProblem problem, IRandomNumberGenerator random)
    {
        Experiment = experiment;
        Problem = problem;
        Random = random;

        var cases = experiment.MaterializeCases();
        if (cases.Length == 0)
            throw new InvalidOperationException("An experiment must contain at least one trial.");

        var keys = new HashSet<TKey>();
        var trials = new List<ExperimentTrial<TCandidate, TSearchSpace, TProblem, TSearchState, TAlgorithm, TKey>>(cases.Length);
        for (var index = 0; index < cases.Length; index++)
        {
            var experimentCase = cases[index];
            if (!keys.Add(experimentCase.Key))
                throw new InvalidOperationException($"Experiment trial key {experimentCase.Key} is not unique.");

            var trialRandom = experimentCase.RandomForkPath.Aggregate(random, static (current, forkKey) => current.Fork(forkKey));
            var algorithmRun = new AlgorithmRun<TCandidate, TSearchSpace, TProblem, TSearchState>(experimentCase.Algorithm, problem, trialRandom);
            trials.Add(new ExperimentTrial<TCandidate, TSearchSpace, TProblem, TSearchState, TAlgorithm, TKey>(index, experimentCase.Key, experimentCase.Algorithm, algorithmRun, experimentCase.RandomForkPath));
        }

        Trials = trials.ToImmutableArray();
    }

    public ExperimentRun<TCandidate, TSearchSpace, TProblem, TSearchState, TAlgorithm, TKey> WithAnalyzer<TOperator, TResult>(TrialAnalyzer<TAlgorithm, TOperator, TResult> trialAnalyzer)
        where TResult : class
    {
        EnsureNotStarted();
        if (trialAnalyzers.ContainsKey(trialAnalyzer))
            throw new InvalidOperationException("The same trial analyzer cannot be attached more than once.");

        var analyzers = Trials.Select(trial => (IAnalyzer)trialAnalyzer.AnalyzerFactory(trialAnalyzer.Selector(trial.Algorithm))).ToImmutableArray();
        for (var index = 0; index < Trials.Length; index++)
        {
            Trials[index].Run.WithAnalyzer(analyzers[index]);
        }

        trialAnalyzers.Add(trialAnalyzer, analyzers);

        return this;
    }

    public ExperimentRun<TCandidate, TSearchSpace, TProblem, TSearchState, TAlgorithm, TKey> WithAnalyzer<TOperator, TResult>(Func<TAlgorithm, TOperator> selector, Func<TOperator, IAnalyzer<TResult>> analyzerFactory, out TrialAnalyzer<TAlgorithm, TOperator, TResult> trialAnalyzer)
        where TResult : class
    {
        trialAnalyzer = TrialAnalyzer.Create(selector, analyzerFactory);
        return WithAnalyzer(trialAnalyzer);
    }

    public ImmutableArray<TrialAnalysisResult<ExperimentTrial<TCandidate, TSearchSpace, TProblem, TSearchState, TAlgorithm, TKey>, TResult>> GetResults<TOperator, TResult>(TrialAnalyzer<TAlgorithm, TOperator, TResult> trialAnalyzer)
        where TResult : class
    {
        var analyzers = trialAnalyzers[trialAnalyzer];

        return Trials.Select((trial, index) =>
        {
            var analyzer = (IAnalyzer<TResult>)analyzers[index];
            return new TrialAnalysisResult<ExperimentTrial<TCandidate, TSearchSpace, TProblem, TSearchState, TAlgorithm, TKey>, TResult>(trial, analyzer, trial.Run.GetResult(analyzer));
        }).ToImmutableArray();
    }

    public ExecutionStream<ExperimentStreamEntry<ExperimentTrial<TCandidate, TSearchSpace, TProblem, TSearchState, TAlgorithm, TKey>, TSearchState>> Stream(ExecutionConcurrency? concurrency = null, TSearchState? initialState = null, CancellationToken cancellationToken = default)
    {
        var execution = PrepareCombinedExecution(initialState, cancellationToken);
        return new ExecutionStream<ExperimentStreamEntry<ExperimentTrial<TCandidate, TSearchSpace, TProblem, TSearchState, TAlgorithm, TKey>, TSearchState>>(StreamScheduledExecution(execution, concurrency ?? ExecutionConcurrency.Sequential(), cancellationToken), cancellationToken);
    }

    public ImmutableArray<Task<(ExperimentTrial<TCandidate, TSearchSpace, TProblem, TSearchState, TAlgorithm, TKey> Trial, TSearchState State)>> StartTrials(ExecutionConcurrency? concurrency = null, TSearchState? initialState = null, CancellationToken cancellationToken = default)
    {
        var execution = PrepareCombinedExecution(initialState, cancellationToken);
        var scheduledExecution = ScheduleTrials(execution, concurrency ?? ExecutionConcurrency.Sequential(), null, cancellationToken);
        return scheduledExecution.TrialCompletions.Select((completion, index) => RequireFinalState(Trials[index], completion)).ToImmutableArray();
    }

    private async IAsyncEnumerable<ExperimentStreamEntry<ExperimentTrial<TCandidate, TSearchSpace, TProblem, TSearchState, TAlgorithm, TKey>, TSearchState>> StreamScheduledExecution(PreparedCombinedExecution execution, ExecutionConcurrency concurrency, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        using var stopSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var channel = Channel.CreateUnbounded<ExperimentStreamEntry<ExperimentTrial<TCandidate, TSearchSpace, TProblem, TSearchState, TAlgorithm, TKey>, TSearchState>>(new UnboundedChannelOptions { SingleReader = true, SingleWriter = concurrency.MaximumConcurrency == 1 });
        var scheduledExecution = ScheduleTrials(execution, concurrency, channel.Writer, stopSource.Token);
        try
        {
            await foreach (var entry in channel.Reader.ReadAllAsync(cancellationToken))
            {
                yield return entry;
            }

            await scheduledExecution.WorkersCompletion;
            cancellationToken.ThrowIfCancellationRequested();
            ThrowTrialFailures(scheduledExecution.TrialCompletions);
        }
        finally
        {
            await stopSource.CancelAsync();
            await scheduledExecution.WorkersCompletion.ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
        }
    }

    private ScheduledExecution ScheduleTrials(PreparedCombinedExecution execution, ExecutionConcurrency concurrency, ChannelWriter<ExperimentStreamEntry<ExperimentTrial<TCandidate, TSearchSpace, TProblem, TSearchState, TAlgorithm, TKey>, TSearchState>>? progressWriter, CancellationToken cancellationToken)
    {
        var completionSources = Enumerable.Range(0, Trials.Length)
            .Select(_ => new TaskCompletionSource<TrialExecutionResult>(TaskCreationOptions.RunContinuationsAsynchronously))
            .ToImmutableArray();
        var nextTrialIndex = -1;
        var workerCount = concurrency.Kind == ExecutionConcurrencyKind.Sequential ? 1 : Math.Min(concurrency.MaximumConcurrency, execution.PreparedTrials.Length);
        var workers = Enumerable.Range(0, workerCount).Select(_ => ExecuteTrials()).ToArray();
        var workersCompletion = FinishWorkers(workers, completionSources, progressWriter, cancellationToken);

        return new ScheduledExecution(completionSources.Select(source => source.Task).ToImmutableArray(), workersCompletion);

        async Task ExecuteTrials()
        {
            while (true)
            {
                var trialIndex = Interlocked.Increment(ref nextTrialIndex);
                if (trialIndex >= execution.PreparedTrials.Length)
                    return;

                var preparedTrial = execution.PreparedTrials[trialIndex];
                var completionSource = completionSources[trialIndex];
                try
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (preparedTrial is FailedPreparedTrial failedTrial)
                    {
                        completionSource.SetException(failedTrial.Exception);
                        continue;
                    }

                    completionSource.SetResult(await ExecuteTrial((PreparedTrialStream)preparedTrial, progressWriter, cancellationToken));
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    completionSource.TrySetCanceled(cancellationToken);
                    return;
                }
                catch (Exception exception)
                {
                    completionSource.TrySetException(exception);
                }
            }
        }
    }

    private async Task<TrialExecutionResult> ExecuteTrial(PreparedTrialStream preparedTrial, ChannelWriter<ExperimentStreamEntry<ExperimentTrial<TCandidate, TSearchSpace, TProblem, TSearchState, TAlgorithm, TKey>, TSearchState>>? progressWriter, CancellationToken cancellationToken)
    {
        var trial = Trials[preparedTrial.TrialIndex];
        TSearchState? finalState = null;
        var producedState = false;
        try
        {
            await foreach (var state in preparedTrial.Stream.WithCancellation(cancellationToken))
            {
                producedState = true;
                finalState = state;
                if (progressWriter is not null)
                {
                    await progressWriter.WriteAsync(new ExperimentStreamEntry<ExperimentTrial<TCandidate, TSearchSpace, TProblem, TSearchState, TAlgorithm, TKey>, TSearchState>(trial, state), cancellationToken);
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            throw new ExperimentTrialException<TKey>(trial.Key, exception);
        }

        return new TrialExecutionResult(producedState, finalState);
    }

    private static async Task<(ExperimentTrial<TCandidate, TSearchSpace, TProblem, TSearchState, TAlgorithm, TKey> Trial, TSearchState State)> RequireFinalState(ExperimentTrial<TCandidate, TSearchSpace, TProblem, TSearchState, TAlgorithm, TKey> trial, Task<TrialExecutionResult> completion)
    {
        var result = await completion;
        if (!result.ProducedState)
            throw new ExperimentTrialException<TKey>(trial.Key, new InvalidOperationException("The algorithm did not yield a search state."));

        return (trial, result.FinalState!);
    }

    private static async Task FinishWorkers(Task[] workers, ImmutableArray<TaskCompletionSource<TrialExecutionResult>> completionSources, ChannelWriter<ExperimentStreamEntry<ExperimentTrial<TCandidate, TSearchSpace, TProblem, TSearchState, TAlgorithm, TKey>, TSearchState>>? progressWriter, CancellationToken cancellationToken)
    {
        Exception? workerFailure = null;
        try
        {
            await Task.WhenAll(workers);
        }
        catch (Exception exception)
        {
            workerFailure = exception;
        }

        foreach (var completionSource in completionSources)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                completionSource.TrySetCanceled(cancellationToken);
            }
            else if (workerFailure is not null)
            {
                completionSource.TrySetException(workerFailure);
            }
            else
            {
                completionSource.TrySetException(new InvalidOperationException("The experiment trial was not scheduled."));
            }
        }

        progressWriter?.TryComplete(workerFailure);
    }

    private static void ThrowTrialFailures(ImmutableArray<Task<TrialExecutionResult>> trialCompletions)
    {
        var failures = trialCompletions.Select(task => task.Exception).OfType<AggregateException>().SelectMany(exception => exception.InnerExceptions).ToList();
        if (failures.Count > 0)
        {
            throw new AggregateException(failures);
        }
    }

    private void StartExecution()
    {
        EnsureNotStarted();
        executionStarted = true;
    }

    private void EnsureNotStarted()
    {
        if (ExecutionStarted)
        {
            throw new InvalidOperationException("An experiment run can only be configured and executed once. Create a new run for another execution.");
        }
    }

    private PreparedCombinedExecution PrepareCombinedExecution(TSearchState? initialState, CancellationToken cancellationToken)
    {
        StartExecution();
        var preparedTrials = ImmutableArray.CreateBuilder<PreparedTrial>(Trials.Length);
        for (var trialIndex = 0; trialIndex < Trials.Length; trialIndex++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var trial = Trials[trialIndex];
            try
            {
                preparedTrials.Add(new PreparedTrialStream(trialIndex, trial.Run.Stream(initialState, cancellationToken)));
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                preparedTrials.Add(new FailedPreparedTrial(trialIndex, new ExperimentTrialException<TKey>(trial.Key, exception)));
            }
        }

        return new PreparedCombinedExecution(preparedTrials.MoveToImmutable());
    }

    private abstract record PreparedTrial(int TrialIndex);
    private sealed record PreparedTrialStream(int TrialIndex, ExecutionStream<TSearchState> Stream) : PreparedTrial(TrialIndex);
    private sealed record FailedPreparedTrial(int TrialIndex, Exception Exception) : PreparedTrial(TrialIndex);
    private sealed record PreparedCombinedExecution(ImmutableArray<PreparedTrial> PreparedTrials);
    private sealed record ScheduledExecution(ImmutableArray<Task<TrialExecutionResult>> TrialCompletions, Task WorkersCompletion);
    private sealed record TrialExecutionResult(bool ProducedState, TSearchState? FinalState);
}

public sealed class ExperimentTrial<TCandidate, TSearchSpace, TProblem, TSearchState, TAlgorithm, TKey>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
    where TSearchState : class, ISearchState
    where TAlgorithm : class, IAlgorithm<TCandidate, TSearchSpace, TProblem, TSearchState>
{
    internal int Index { get; }

    public TKey Key { get; }

    public TAlgorithm Algorithm { get; }

    public AlgorithmRun<TCandidate, TSearchSpace, TProblem, TSearchState> Run { get; }

    public ImmutableArray<int> RandomForkPath { get; }

    internal ExperimentTrial(int index, TKey key, TAlgorithm algorithm, AlgorithmRun<TCandidate, TSearchSpace, TProblem, TSearchState> run, ImmutableArray<int> randomForkPath)
    {
        Index = index;
        Key = key;
        Algorithm = algorithm;
        Run = run;
        RandomForkPath = randomForkPath;
    }
}

public sealed record ExperimentStreamEntry<TTrial, TSearchState>(TTrial Trial, TSearchState State);

public sealed class ExperimentTrialException<TKey> : Exception
{
    public TKey Key { get; }

    public ExperimentTrialException(TKey key, Exception innerException) : base($"Experiment trial {key} failed.", innerException) => Key = key;
}
