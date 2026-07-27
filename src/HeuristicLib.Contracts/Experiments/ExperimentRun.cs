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
        if (cases.Count == 0)
        {
            throw new InvalidOperationException("An experiment must contain at least one trial.");
        }

        var keys = new HashSet<TKey>();
        var trials = new List<ExperimentTrial<TCandidate, TSearchSpace, TProblem, TSearchState, TAlgorithm, TKey>>(cases.Count);
        for (var index = 0; index < cases.Count; index++)
        {
            var experimentCase = cases[index];
            if (!keys.Add(experimentCase.Key))
            {
                throw new InvalidOperationException($"Experiment trial key {experimentCase.Key} is not unique.");
            }

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
        {
            throw new InvalidOperationException("The same trial analyzer cannot be attached more than once.");
        }

        var analyzers = Trials.Select(trial => (IAnalyzer)trialAnalyzer.AnalyzerFactory(trialAnalyzer.Selector(trial.Algorithm))).ToImmutableArray();
        for (var index = 0; index < Trials.Length; index++)
        {
            Trials[index].Run.WithAnalyzer(analyzers[index]);
        }

        trialAnalyzers.Add(trialAnalyzer, analyzers);

        return this;
    }

    public ExperimentRun<TCandidate, TSearchSpace, TProblem, TSearchState, TAlgorithm, TKey> WithAnalyzer<TOperator, TResult>(
        Func<TAlgorithm, TOperator> selector,
        Func<TOperator, IAnalyzer<TResult>> analyzerFactory,
        out TrialAnalyzer<TAlgorithm, TOperator, TResult> trialAnalyzer)
        where TResult : class
    {
        trialAnalyzer = TrialAnalyzer.Create(selector, analyzerFactory);
        return WithAnalyzer(trialAnalyzer);
    }

    public IReadOnlyList<TrialAnalysisResult<ExperimentTrial<TCandidate, TSearchSpace, TProblem, TSearchState, TAlgorithm, TKey>, TResult>> GetResults<TOperator, TResult>(
        TrialAnalyzer<TAlgorithm, TOperator, TResult> trialAnalyzer)
        where TResult : class
    {
        var analyzers = trialAnalyzers[trialAnalyzer];

        return Trials.Select((trial, index) =>
        {
            var analyzer = (IAnalyzer<TResult>)analyzers[index];
            return new TrialAnalysisResult<ExperimentTrial<TCandidate, TSearchSpace, TProblem, TSearchState, TAlgorithm, TKey>, TResult>(trial, analyzer, trial.Run.GetResult(analyzer));
        }).ToImmutableArray();
    }

    public ExecutionStream<ExperimentStreamEntry<ExperimentTrial<TCandidate, TSearchSpace, TProblem, TSearchState, TAlgorithm, TKey>, TSearchState>> Stream(ExperimentExecutionPolicy? policy = null, TSearchState? initialState = null, CancellationToken cancellationToken = default)
    {
        var execution = PrepareCombinedExecution(initialState, cancellationToken);
        return new ExecutionStream<ExperimentStreamEntry<ExperimentTrial<TCandidate, TSearchSpace, TProblem, TSearchState, TAlgorithm, TKey>, TSearchState>>(ExecuteCombined(execution, policy ?? ExperimentExecutionPolicy.Sequential(), cancellationToken), cancellationToken);
    }

    public async Task<IReadOnlyList<(ExperimentTrial<TCandidate, TSearchSpace, TProblem, TSearchState, TAlgorithm, TKey> Trial, TSearchState State)>> CompleteAsync(ExperimentExecutionPolicy? policy = null, TSearchState? initialState = null, CancellationToken cancellationToken = default)
    {
        var finalStates = new TSearchState?[Trials.Length];
        await foreach (var entry in Stream(policy, initialState, cancellationToken).WithCancellation(cancellationToken))
        {
            finalStates[entry.Trial.Index] = entry.State;
        }

        var missingStateFailures = Trials.Where(trial => finalStates[trial.Index] is null)
            .Select(trial => (Exception)new ExperimentTrialException<TKey>(trial.Key, new InvalidOperationException("The algorithm did not yield a search state.")))
            .ToList();
        if (missingStateFailures.Count > 0)
        {
            throw new AggregateException(missingStateFailures);
        }

        return Trials.Select(trial => (trial, finalStates[trial.Index]!)).ToImmutableArray();
    }

    public IReadOnlyList<(ExperimentTrial<TCandidate, TSearchSpace, TProblem, TSearchState, TAlgorithm, TKey> Trial, TSearchState State)> Complete(ExperimentExecutionPolicy? policy = null, TSearchState? initialState = null, CancellationToken cancellationToken = default) =>
        CompleteAsync(policy, initialState, cancellationToken).GetAwaiter().GetResult();

    private IAsyncEnumerable<ExperimentStreamEntry<ExperimentTrial<TCandidate, TSearchSpace, TProblem, TSearchState, TAlgorithm, TKey>, TSearchState>> ExecuteCombined(PreparedCombinedExecution execution, ExperimentExecutionPolicy policy, CancellationToken cancellationToken) =>
        policy.Kind == ExperimentExecutionKind.Sequential ? ExecuteSequentially(execution, cancellationToken) : ExecuteConcurrently(execution, policy.MaximumConcurrency, cancellationToken);

    private async IAsyncEnumerable<ExperimentStreamEntry<ExperimentTrial<TCandidate, TSearchSpace, TProblem, TSearchState, TAlgorithm, TKey>, TSearchState>> ExecuteSequentially(PreparedCombinedExecution execution, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        foreach (var preparedTrial in execution.TrialStreams)
        {
            var trial = Trials[preparedTrial.TrialIndex];
            cancellationToken.ThrowIfCancellationRequested();
            await using var enumerator = preparedTrial.Stream.GetAsyncEnumerator(cancellationToken);
            while (true)
            {
                bool hasNext;
                try
                {
                    hasNext = await enumerator.MoveNextAsync();
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception exception)
                {
                    execution.Failures[trial.Index] = new ExperimentTrialException<TKey>(trial.Key, exception);
                    break;
                }

                if (!hasNext)
                {
                    break;
                }

                yield return new ExperimentStreamEntry<ExperimentTrial<TCandidate, TSearchSpace, TProblem, TSearchState, TAlgorithm, TKey>, TSearchState>(trial, enumerator.Current);
            }
        }

        var orderedFailures = execution.Failures.OfType<Exception>().ToList();
        if (orderedFailures.Count > 0)
        {
            throw new AggregateException(orderedFailures);
        }
    }

    private async IAsyncEnumerable<ExperimentStreamEntry<ExperimentTrial<TCandidate, TSearchSpace, TProblem, TSearchState, TAlgorithm, TKey>, TSearchState>> ExecuteConcurrently(PreparedCombinedExecution execution, int maximumConcurrency, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        using var stopSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var channel = Channel.CreateUnbounded<ExperimentStreamEntry<ExperimentTrial<TCandidate, TSearchSpace, TProblem, TSearchState, TAlgorithm, TKey>, TSearchState>>(new UnboundedChannelOptions { SingleReader = true, SingleWriter = false });
        var nextPreparedTrialIndex = -1;
        var workerCount = Math.Min(maximumConcurrency, execution.TrialStreams.Length);

        var workers = Enumerable.Range(0, workerCount).Select(_ => ProduceEntries()).ToArray();
        var completion = CompleteChannelWhenFinished(workers, channel.Writer);
        try
        {
            await foreach (var entry in channel.Reader.ReadAllAsync(cancellationToken))
            {
                yield return entry;
            }

            await completion;
        }
        finally
        {
            await stopSource.CancelAsync();
            await Task.WhenAll(workers);
        }

        cancellationToken.ThrowIfCancellationRequested();
        var orderedFailures = execution.Failures.OfType<Exception>().ToList();
        if (orderedFailures.Count > 0)
        {
            throw new AggregateException(orderedFailures);
        }

        async Task ProduceEntries()
        {
            while (true)
            {
                var preparedTrialIndex = Interlocked.Increment(ref nextPreparedTrialIndex);
                if (preparedTrialIndex >= execution.TrialStreams.Length)
                {
                    return;
                }

                var preparedTrial = execution.TrialStreams[preparedTrialIndex];
                var trial = Trials[preparedTrial.TrialIndex];
                try
                {
                    await foreach (var state in preparedTrial.Stream.WithCancellation(stopSource.Token))
                    {
                        await channel.Writer.WriteAsync(new ExperimentStreamEntry<ExperimentTrial<TCandidate, TSearchSpace, TProblem, TSearchState, TAlgorithm, TKey>, TSearchState>(trial, state), stopSource.Token);
                    }
                }
                catch (OperationCanceledException) when (stopSource.IsCancellationRequested)
                {
                    return;
                }
                catch (Exception exception)
                {
                    execution.Failures[trial.Index] = new ExperimentTrialException<TKey>(trial.Key, exception);
                }
            }
        }
    }

    private static async Task CompleteChannelWhenFinished(Task[] tasks, ChannelWriter<ExperimentStreamEntry<ExperimentTrial<TCandidate, TSearchSpace, TProblem, TSearchState, TAlgorithm, TKey>, TSearchState>> writer)
    {
        try
        {
            await Task.WhenAll(tasks);
            writer.TryComplete();
        }
        catch (Exception exception)
        {
            writer.TryComplete(exception);
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
        var trialStreams = new List<PreparedTrialStream>(Trials.Length);
        var failures = new Exception?[Trials.Length];
        for (var trialIndex = 0; trialIndex < Trials.Length; trialIndex++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var trial = Trials[trialIndex];
            try
            {
                trialStreams.Add(new PreparedTrialStream(trialIndex, trial.Run.Stream(initialState, cancellationToken)));
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                failures[trialIndex] = new ExperimentTrialException<TKey>(trial.Key, exception);
            }
        }

        return new PreparedCombinedExecution(trialStreams.ToImmutableArray(), failures);
    }

    private sealed record PreparedTrialStream(int TrialIndex, ExecutionStream<TSearchState> Stream);

    private sealed record PreparedCombinedExecution(ImmutableArray<PreparedTrialStream> TrialStreams, Exception?[] Failures);
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
