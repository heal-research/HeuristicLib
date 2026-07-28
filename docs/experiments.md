# Experiments

An experiment expands one or more algorithm configurations into independent trials. Every trial owns a separate algorithm run, execution registry, random number generator fork and analyzer state.

Use experiments when you want to repeat an algorithm or compare a grid of configurations without making those executions part of one algorithm.

## Creating experiments

Repeat one algorithm configuration:

```csharp
var experiment = algorithm.Repeat(10);
```

Build a parameter grid and repeat every generated configuration:

```csharp
var experiment = algorithm
    .AsGrid()
    .VaryBy([50, 100, 200], (algorithm, populationSize) => algorithm with
    {
        PopulationSize = populationSize
    })
    .VaryBy([0.05, 0.1], (algorithm, mutationRate) => algorithm with
    {
        MutationRate = mutationRate
    })
    .Repeat(10);
```

Grid values are accepted as `IReadOnlyList<T>` and copied into immutable experiment configuration. Arrays, lists and immutable arrays can therefore be supplied without letting later caller mutation change the grid.

Every materialized trial has a deterministic typed key:

| Composition | Key |
| --- | --- |
| `algorithm.Repeat(n)` | repetition index |
| `algorithm.AsGrid()` | concrete algorithm configuration |
| `algorithm.AsGrid().Repeat(n)` | concrete algorithm configuration and repetition index |

The concrete key for a repeated grid is `(TAlgorithm Inner, int Repetition)`. Materialization order determines trial collections, completion results, analysis results and aggregate failure order.

Creating an experiment run materializes its trials but does not start them:

```csharp
var run = experiment.CreateRun(problem, random);
```

An experiment run is single use. Calling any execution method starts the run, prevents further analyzer attachment and prevents direct execution of its trial runs.

For simple execution without analysis configuration, convenience extensions create the run internally:

```csharp
var results = await experiment.CompleteAsync(problem, random);
```

## Execution choices

`ExperimentRun` provides two core execution methods:

| API | Use |
| --- | --- |
| `Stream()` | Observe one combined stream containing the trial and state for every entry |
| `StartTrials()` | Receive one hot completion task per trial and choose how to await or inspect them |

Completion and manual trial execution are projections over those core methods:

| API | Use |
| --- | --- |
| `Complete()` and `CompleteAsync()` | Convenience extensions over `StartTrials()` that return all final states when every trial succeeds |
| `Trials` | Execute or stream selected algorithm runs manually |

All combined forms accept the general `ExecutionConcurrency` from `HEAL.HeuristicLib.Execution`:

```csharp
ExecutionConcurrency.Sequential()
ExecutionConcurrency.Concurrent()
ExecutionConcurrency.Concurrent(4)
```

Sequential execution follows materialization order. `Concurrent()` permits every trial to run concurrently. `Concurrent(maximumConcurrency)` bounds the number of active trials without promising sequential ordering. `Concurrent(1)` therefore remains categorically concurrent even though only one trial can be active. Scheduling never changes trial keys, random assignments or ordered result collections.

### Complete every trial

Use `CompleteAsync()` when the experiment is expected to succeed as one operation:

```csharp
var results = await run.CompleteAsync(
    ExecutionConcurrency.Concurrent(4),
    cancellationToken: cancellationToken);

foreach (var (trial, state) in results)
{
    Console.WriteLine($"{trial.Key}: {state}");
}
```

Every unaffected trial gets an opportunity to finish. If one or more trials fail, `CompleteAsync()` throws an `AggregateException` after all trials finish. Its inner exceptions are `ExperimentTrialException<TKey>` values in trial materialization order. No completion collection is returned when the combined operation fails.

### Stream combined progress

The combined stream identifies the trial that produced every state:

```csharp
await foreach (var (trial, state) in run.Stream(
    ExecutionConcurrency.Concurrent(4),
    cancellationToken: cancellationToken))
{
    Console.WriteLine($"{trial.Key}: {state}");
}
```

Concurrent stream entry order depends on scheduling. Each entry retains its deterministic trial identity.

Disposing the combined stream before it finishes cancels active trial execution and prevents trials that have not yet been scheduled from starting.

### Inspect individual trial tasks

Use `StartTrials()` when successful trial results must remain directly accessible even if other trials fail:

```csharp
var trialTasks = run.StartTrials(
    ExecutionConcurrency.Concurrent(4),
    cancellationToken: cancellationToken);

try
{
    var results = await Task.WhenAll(trialTasks);
}
catch
{
    var successfulResults = trialTasks
        .Where(task => task.IsCompletedSuccessfully)
        .Select(task => task.Result)
        .ToImmutableArray();

    var failures = trialTasks
        .Select(task => task.Exception)
        .OfType<AggregateException>()
        .SelectMany(exception => exception.InnerExceptions)
        .Cast<ExperimentTrialException<int>>()
        .ToImmutableArray();
}
```

`StartTrials()` starts every returned task according to the selected concurrency. Each successful task returns its trial and final state. Each failed task contains an `ExperimentTrialException<TKey>` for that trial. A trial that produces no state also fails because it has no completion result.

Use `Task.WhenEach()` to process trials in completion order and handle failures individually:

```csharp
var trialTasks = run.StartTrials(
    ExecutionConcurrency.Concurrent(4),
    cancellationToken: cancellationToken);

await foreach (var completedTask in Task.WhenEach(trialTasks))
{
    try
    {
        var (trial, state) = await completedTask;
        Console.WriteLine($"{trial.Key} completed: {state}");
    }
    catch (ExperimentTrialException<int> exception)
    {
        Console.WriteLine($"{exception.Key} failed: {exception.GetBaseException().Message}");
    }
}
```

Every task must be observed. `Task.WhenAll()` is the simplest choice when results are inspected only after all trials finish. `Task.WhenEach()` is useful when results should be processed as soon as their trials finish.

### Execute trials manually

The `Trials` collection is the lowest level execution surface:

```csharp
foreach (var trial in run.Trials)
{
    try
    {
        var state = await trial.Run.CompleteAsync(cancellationToken: cancellationToken);
    }
    catch (Exception exception)
    {
        Console.WriteLine($"{trial.Key} failed: {exception.Message}");
    }
}
```

Manual execution lets callers select trials, provide individual cancellation tokens or consume separate streams. Starting one trial manually prevents later combined execution, but other unstarted trials remain available for manual execution.

## Shared inputs

Every trial owns an independent algorithm run, execution registry, random number generator fork and analyzer state. The experiment run currently forwards the same problem object to every trial. That problem should therefore be immutable or safe for concurrent use when concurrent execution is selected.

A supplied initial search state is also forwarded to every trial. If that state is mutable and trials must not share it, execute the trial runs manually with separate initial states. Per trial problem and initial state factories are deferred.

## Analysis

Attach trial analyzers before execution:

```csharp
var run = experiment.CreateRun(problem, random)
    .WithAnalyzer(
        algorithm => algorithm.Evaluator,
        evaluator => Analyzer.BestQuality(evaluator),
        out var bestQuality);

var trialTasks = run.StartTrials(
    ExecutionConcurrency.Concurrent(),
    cancellationToken: cancellationToken);

await Task.WhenAll(trialTasks);
var results = run.GetResults(bestQuality);
```

Every trial receives its own analyzer configuration and run scoped analyzer state. See [Observability and analysis](observability-and-analysis.md) for the analyzer model.

## Failure and cancellation

Trial failures do not cancel independent trials. Successful trials remain successful and their analyzer results remain available through their algorithm runs.

Explicit cancellation is different. The shared cancellation token stops active trials and prevents pending trials from starting. Combined execution then throws cancellation rather than reporting it as an experiment trial failure.

Cancellation also takes precedence when another trial has already failed. Every task returned by `StartTrials()` still reaches a terminal state, so callers can safely observe the complete task collection after cancellation.

## Related pages

1. [Algorithm](algorithm.md)
2. [Execution model](execution-model.md)
3. [Observability and analysis](observability-and-analysis.md)
4. [Randomness](randomness.md)
