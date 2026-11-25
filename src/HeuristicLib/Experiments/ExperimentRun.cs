using System.Runtime.CompilerServices;
using System.Threading.Channels;
using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Experiments;

public sealed class ExperimentRun<TCandidate, TSearchSpace, TProblem, TSearchState, TAlgorithm>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
    where TSearchState : class, ISearchState
    where TAlgorithm : class, IAlgorithm<TCandidate, TSearchState>
{
    private readonly Lock sync = new();
    private RunLifecycleState lifecycleState = RunLifecycleState.Ready;

    // Completes once the lifecycle has left Running after StartTrials, so CompleteAsync can join the transition.
    internal Task ExecutionCompletion { get; private set; } = Task.CompletedTask;

    /// <summary>The experiment defining this run's parameter candidates, repetitions and stream setting.</summary>
    public Experiment<TCandidate, TAlgorithm, TSearchState> Experiment { get; }

    /// <summary>The problems in the order used to assign trial positions.</summary>
    public ImmutableArray<TProblem> Problems { get; }

    public IRandomNumberGenerator Random { get; }

    public ImmutableArray<ExperimentTrial<TCandidate, TSearchSpace, TProblem, TSearchState, TAlgorithm>> Trials { get; }

    public RunLifecycleState LifecycleState
    {
        get
        {
            lock (sync)
                return lifecycleState;
        }
    }

    /// <summary>Creates a run with the supplied configurations, skipping configuration validation.</summary>
    public ExperimentRun(Experiment<TCandidate, TAlgorithm, TSearchState> experiment, TProblem problem, IRandomNumberGenerator random)
        : this(experiment, [problem], random, null, false) { }

    internal ExperimentRun(Experiment<TCandidate, TAlgorithm, TSearchState> experiment, ImmutableArray<TProblem> problems, IRandomNumberGenerator random,
        IReadOnlyList<ISubstitution>? substitutions, bool validate)
    {
        Experiment = experiment;
        Problems = problems;
        Random = random;

        if (problems.IsEmpty)
            throw new ArgumentException("Supply at least one problem.", nameof(problems));

        var parameterCandidateCount = experiment.ParameterCandidateCount;
        var trials = ImmutableArray.CreateBuilder<ExperimentTrial<TCandidate, TSearchSpace, TProblem, TSearchState, TAlgorithm>>(
            checked(problems.Length * parameterCandidateCount * experiment.Repetitions));
        var parameterCandidates = experiment.DecodeAll(parameterCandidateCount);

        var algorithmsWithSubstitutions = new IAlgorithm<TCandidate, TSearchState>[parameterCandidateCount];
        for (var parameterCandidateIndex = 0; parameterCandidateIndex < parameterCandidateCount; parameterCandidateIndex++)
        {
            try
            {
                algorithmsWithSubstitutions[parameterCandidateIndex] = AlgorithmExtensions.ApplySubstitutions(parameterCandidates[parameterCandidateIndex].Algorithm, substitutions);
            }
            catch (Exception exception)
            {
                throw new ExperimentTrialException(0, parameterCandidateIndex, 0, exception);
            }
        }

        for (var problemPosition = 0; problemPosition < problems.Length; problemPosition++)
        {
            var problem = problems[problemPosition];
            var problemRandom = random.Fork(problemPosition);
            for (var parameterCandidateIndex = 0; parameterCandidateIndex < parameterCandidateCount; parameterCandidateIndex++)
            {
                var (algorithm, settings) = parameterCandidates[parameterCandidateIndex];
                if (validate)
                {
                    try
                    {
                        AlgorithmExtensions.ValidateConfiguration<TCandidate, TSearchSpace, TProblem, TSearchState>(algorithmsWithSubstitutions[parameterCandidateIndex], problem);
                    }
                    catch (Exception exception)
                    {
                        throw new ExperimentTrialException(problemPosition, parameterCandidateIndex, 0, exception);
                    }
                }

                var parameterCandidateRandom = experiment.ShareRandomStreamsAcrossParameterCandidates
                    ? problemRandom
                    : problemRandom.Fork(parameterCandidateIndex);
                for (var repetition = 0; repetition < experiment.Repetitions; repetition++)
                {
                    var trialRandom = parameterCandidateRandom.Fork(repetition);
                    AlgorithmRun<TCandidate, TSearchSpace, TProblem, TSearchState> run;
                    try
                    {
                        run = AlgorithmExtensions.CreateRunAndCheckRequiredNodes<TCandidate, TSearchSpace, TProblem, TSearchState>(
                            algorithmsWithSubstitutions[parameterCandidateIndex], problem, trialRandom, substitutions);
                    }
                    catch (Exception exception)
                    {
                        throw new ExperimentTrialException(problemPosition, parameterCandidateIndex, repetition, exception);
                    }

                    trials.Add(new(problemPosition, parameterCandidateIndex, repetition, algorithm, settings, run));
                }
            }
        }

        Trials = trials.MoveToImmutable();
    }

    public ExecutionStream<ExperimentStreamEntry<ExperimentTrial<TCandidate, TSearchSpace, TProblem, TSearchState, TAlgorithm>, TSearchState>> Stream(ExecutionConcurrency? concurrency = null, TSearchState? initialState = null, CancellationToken cancellationToken = default)
    {
        var execution = PrepareCombinedExecution(initialState, cancellationToken);
        return new(StreamScheduledExecution(execution, concurrency ?? ExecutionConcurrency.Sequential(), cancellationToken), cancellationToken);
    }

    public ImmutableArray<Task<(ExperimentTrial<TCandidate, TSearchSpace, TProblem, TSearchState, TAlgorithm> Trial, TSearchState State)>> StartTrials(ExecutionConcurrency? concurrency = null, TSearchState? initialState = null, CancellationToken cancellationToken = default)
    {
        var execution = PrepareCombinedExecution(initialState, cancellationToken);
        var scheduledExecution = ScheduleTrials(execution, concurrency ?? ExecutionConcurrency.Sequential(), null, cancellationToken);
        ExecutionCompletion = TrackCompletion(scheduledExecution.TrialCompletions, scheduledExecution.WorkersCompletion, cancellationToken);
        return scheduledExecution.TrialCompletions.Select((completion, index) => RequireFinalState(Trials[index], completion)).ToImmutableArray();
    }

    private async IAsyncEnumerable<ExperimentStreamEntry<ExperimentTrial<TCandidate, TSearchSpace, TProblem, TSearchState, TAlgorithm>, TSearchState>> StreamScheduledExecution(PreparedCombinedExecution execution, ExecutionConcurrency concurrency, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        using var stopSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var channel = Channel.CreateUnbounded<ExperimentStreamEntry<ExperimentTrial<TCandidate, TSearchSpace, TProblem, TSearchState, TAlgorithm>, TSearchState>>(new UnboundedChannelOptions { SingleReader = true, SingleWriter = concurrency.MaximumConcurrency == 1 });
        var scheduledExecution = ScheduleTrials(execution, concurrency, channel.Writer, stopSource.Token);
        try
        {
            await using var enumerator = channel.Reader.ReadAllAsync(cancellationToken).GetAsyncEnumerator(cancellationToken);
            while (true)
            {
                ExperimentStreamEntry<ExperimentTrial<TCandidate, TSearchSpace, TProblem, TSearchState, TAlgorithm>, TSearchState> entry;
                try
                {
                    if (!await enumerator.MoveNextAsync())
                        break;
                    entry = enumerator.Current;
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    TransitionFromRunning(RunLifecycleState.Canceled);
                    throw;
                }
                catch
                {
                    TransitionFromRunning(RunLifecycleState.Failed);
                    throw;
                }

                yield return entry;
            }

            try
            {
                await scheduledExecution.WorkersCompletion;
                cancellationToken.ThrowIfCancellationRequested();
                ThrowTrialFailures(scheduledExecution.TrialCompletions);
                TransitionFromRunning(RunLifecycleState.Completed);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                TransitionFromRunning(RunLifecycleState.Canceled);
                throw;
            }
            catch
            {
                TransitionFromRunning(RunLifecycleState.Failed);
                throw;
            }
        }
        finally
        {
            try
            {
                await stopSource.CancelAsync();
            }
            finally
            {
                await scheduledExecution.WorkersCompletion.ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
                TransitionFromRunning(RunLifecycleState.Stopped);
            }
        }
    }

    private ScheduledExecution ScheduleTrials(PreparedCombinedExecution execution, ExecutionConcurrency concurrency, ChannelWriter<ExperimentStreamEntry<ExperimentTrial<TCandidate, TSearchSpace, TProblem, TSearchState, TAlgorithm>, TSearchState>>? progressWriter, CancellationToken cancellationToken)
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

    private async Task<TrialExecutionResult> ExecuteTrial(PreparedTrialStream preparedTrial, ChannelWriter<ExperimentStreamEntry<ExperimentTrial<TCandidate, TSearchSpace, TProblem, TSearchState, TAlgorithm>, TSearchState>>? progressWriter, CancellationToken cancellationToken)
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
                    await progressWriter.WriteAsync(ExperimentStreamEntry.From(trial, state), cancellationToken);
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            throw new ExperimentTrialException(trial.ProblemPosition, trial.ParameterCandidateIndex, trial.Repetition, exception);
        }

        return new TrialExecutionResult(producedState, finalState);
    }

    private static async Task<(ExperimentTrial<TCandidate, TSearchSpace, TProblem, TSearchState, TAlgorithm> Trial, TSearchState State)> RequireFinalState(ExperimentTrial<TCandidate, TSearchSpace, TProblem, TSearchState, TAlgorithm> trial, Task<TrialExecutionResult> completion)
    {
        var result = await completion;
        if (!result.ProducedState)
            throw new ExperimentTrialException(trial.ProblemPosition, trial.ParameterCandidateIndex, trial.Repetition, new InvalidOperationException("The algorithm did not yield a search state."));

        return (trial, result.FinalState!);
    }

    private async Task FinishWorkers(Task[] workers, ImmutableArray<TaskCompletionSource<TrialExecutionResult>> completionSources, ChannelWriter<ExperimentStreamEntry<ExperimentTrial<TCandidate, TSearchSpace, TProblem, TSearchState, TAlgorithm>, TSearchState>>? progressWriter, CancellationToken cancellationToken)
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

        if (cancellationToken.IsCancellationRequested)
        {
            foreach (var trial in Trials)
                await trial.Run.CancelAsync().AsTask().ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
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
        lock (sync)
        {
            EnsureReady();
            if (Trials.Any(trial => trial.Run.LifecycleState != RunLifecycleState.Ready))
                throw new InvalidOperationException("An experiment run cannot start after one of its trials has been started directly.");
            lifecycleState = RunLifecycleState.Running;
        }
    }

    private void EnsureReady()
    {
        if (lifecycleState != RunLifecycleState.Ready)
        {
            throw new InvalidOperationException($"An experiment run can only start while it is {RunLifecycleState.Ready}. Its current lifecycle state is {lifecycleState}.");
        }
    }

    private void TransitionFromRunning(RunLifecycleState next)
    {
        lock (sync)
        {
            if (lifecycleState == RunLifecycleState.Running)
                lifecycleState = next;
        }
    }

    private async Task TrackCompletion(ImmutableArray<Task<TrialExecutionResult>> trialCompletions,
        Task workersCompletion, CancellationToken cancellationToken)
    {
        await workersCompletion.ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
        if (cancellationToken.IsCancellationRequested)
            TransitionFromRunning(RunLifecycleState.Canceled);
        else if (trialCompletions.Any(completion => completion.IsFaulted))
            TransitionFromRunning(RunLifecycleState.Failed);
        else if (trialCompletions.Any(completion => completion.IsCanceled))
            TransitionFromRunning(RunLifecycleState.Canceled);
        else
            TransitionFromRunning(RunLifecycleState.Completed);
    }

    private PreparedCombinedExecution PrepareCombinedExecution(TSearchState? initialState, CancellationToken cancellationToken)
    {
        StartExecution();
        try
        {
            var preparedTrials = ImmutableArray.CreateBuilder<PreparedTrial>(Trials.Length);
            for (var trialIndex = 0; trialIndex < Trials.Length; trialIndex++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var trial = Trials[trialIndex];
                try
                {
                    preparedTrials.Add(new PreparedTrialStream(trialIndex,
                        trial.Run.StreamForTerminalCancellation(initialState, cancellationToken)));
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception exception)
                {
                    preparedTrials.Add(new FailedPreparedTrial(trialIndex, new ExperimentTrialException(trial.ProblemPosition, trial.ParameterCandidateIndex, trial.Repetition, exception)));
                }
            }

            return new PreparedCombinedExecution(preparedTrials.MoveToImmutable());
        }
        catch (OperationCanceledException)
        {
            TransitionFromRunning(RunLifecycleState.Canceled);
            throw;
        }
        catch
        {
            TransitionFromRunning(RunLifecycleState.Failed);
            throw;
        }
    }

    private abstract record PreparedTrial(int TrialIndex);
    private sealed record PreparedTrialStream(int TrialIndex, ExecutionStream<TSearchState> Stream) : PreparedTrial(TrialIndex);
    private sealed record FailedPreparedTrial(int TrialIndex, Exception Exception) : PreparedTrial(TrialIndex);
    private sealed record PreparedCombinedExecution(ImmutableArray<PreparedTrial> PreparedTrials);
    private sealed record ScheduledExecution(ImmutableArray<Task<TrialExecutionResult>> TrialCompletions, Task WorkersCompletion);
    private sealed record TrialExecutionResult(bool ProducedState, TSearchState? FinalState);
}

public sealed class ExperimentTrial<TCandidate, TSearchSpace, TProblem, TSearchState, TAlgorithm>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
    where TSearchState : class, ISearchState
    where TAlgorithm : class, IAlgorithm<TCandidate, TSearchState>
{
    /// <summary>The problem this trial runs on.</summary>
    public TProblem Problem => Run.Problem;

    /// <summary>The zero-based position of the problem in the run.</summary>
    public int ProblemPosition { get; }

    /// <summary>The zero-based index of the parameter candidate across the experiment's grids.</summary>
    public int ParameterCandidateIndex { get; }

    /// <summary>The zero-based repetition index for this parameter candidate and problem.</summary>
    public int Repetition { get; }

    /// <summary>The parameter candidate before run-time substitutions are applied.</summary>
    public TAlgorithm Algorithm { get; }

    /// <summary>The varied descriptors in first declaration order, with values read after variation and before substitutions are applied.</summary>
    /// <remarks>
    /// A list parameter reports the configuration's stored <see cref="IReadOnlyList{T}"/>. Read it through the parameter's
    /// typed <see cref="IParameter{TOwner, TValue}"/> form to get a <see cref="ValueArray{T}"/>.
    /// </remarks>
    public ImmutableArray<(IParameter Parameter, object? Value)> Settings { get; }

    /// <summary>The independent algorithm run that executes this trial and owns its results.</summary>
    public AlgorithmRun<TCandidate, TSearchSpace, TProblem, TSearchState> Run { get; }

    internal ExperimentTrial(int problemPosition, int parameterCandidateIndex, int repetition, TAlgorithm algorithm, ImmutableArray<(IParameter Parameter, object? Value)> settings,
        AlgorithmRun<TCandidate, TSearchSpace, TProblem, TSearchState> run)
    {
        ProblemPosition = problemPosition;
        ParameterCandidateIndex = parameterCandidateIndex;
        Repetition = repetition;
        Algorithm = algorithm;
        Settings = settings;
        Run = run;
    }
}

public sealed record ExperimentStreamEntry<TTrial, TSearchState>(TTrial Trial, TSearchState State);

public static class ExperimentStreamEntry
{
    public static ExperimentStreamEntry<TTrial, TSearchState> From<TTrial, TSearchState>(TTrial trial, TSearchState state) => new(trial, state);
}

/// <summary>Reports a trial setup or execution failure with the affected trial positions.</summary>
public sealed class ExperimentTrialException : Exception
{
    /// <summary>The problem position. Failure to apply substitutions reports the first problem because all problems share the result of applying them.</summary>
    public int ProblemPosition { get; }

    /// <summary>The global parameter candidate index across the experiment's grids.</summary>
    public int ParameterCandidateIndex { get; }

    /// <summary>The repetition position. A substitution or configuration validation failure reports the first repetition it affects.</summary>
    public int Repetition { get; }

    /// <summary>Creates a trial failure with its zero-based positions and underlying exception.</summary>
    public ExperimentTrialException(int problemPosition, int parameterCandidateIndex, int repetition, Exception innerException)
        : base($"Experiment trial for problem {problemPosition}, parameter candidate {parameterCandidateIndex}, repetition {repetition} failed.", innerException)
    {
        ProblemPosition = problemPosition;
        ParameterCandidateIndex = parameterCandidateIndex;
        Repetition = repetition;
    }
}
