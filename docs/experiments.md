# Experiments

An experiment expands one or more algorithm configurations into independent trials. Every trial owns a separate algorithm run, execution registry, random number generator fork and analyzer state.

Use experiments when you want to repeat an algorithm or compare a grid of configurations without making those executions part of one algorithm.

## Experiments and meta algorithms

Experiments and meta algorithms both coordinate algorithms, but their ownership and result models differ:

| Meta algorithm                                        | Experiment                                                  |
| ----------------------------------------------------- | ----------------------------------------------------------- |
| Coordinates child algorithms inside one algorithm run | Coordinates several independent algorithm runs              |
| May pass search state between child algorithms        | Does not pass search state between trials                   |
| May intentionally share execution instances           | Gives every trial its own root execution registry           |
| Produces one search state stream                      | Provides one stream per trial and an optional combined view |
| Has one analyzer state per attached analyzer          | Creates isolated analyzer states for every trial            |

An experiment is not an algorithm. Grid and repetition composition describe which independent trials to create, not execution steps inside one search process.

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

Grid values are accepted as `IReadOnlyList<T>` and consumed immediately while producing the next immutable grid. Arrays, lists and immutable arrays can therefore be supplied without letting later caller mutation change the grid.

Each `VaryBy(...)` immediately performs a left to right Cartesian expansion and returns a new grid containing the resulting configurations. Its transformation receives every configuration produced by the preceding dimensions but is not retained by the grid. Trial order follows dimension declaration order then value declaration order. Repetition extends each inner case before moving to the next inner case, so a grid containing `A`, `B` followed by `Repeat(2)` produces `(A, 0)`, `(A, 1)`, `(B, 0)`, `(B, 1)`.

Repetition counts must be positive. An empty grid dimension produces an empty grid according to ordinary Cartesian-product semantics; creating an experiment run from it fails because a run must contain at least one trial. A grid that produces equal algorithm configurations throws because the algorithm configuration is its trial key. Use `Repeat(...)` when executing an equal configuration several times is intentional. Experiment runs also reject duplicate trial keys.

Materialized cases, trials, completion results and analysis results are exposed as immutable arrays in their canonical materialization order.

Every materialized trial has a deterministic typed key:

| Composition                    | Key                                                   |
| ------------------------------ | ----------------------------------------------------- |
| `algorithm.Repeat(n)`          | repetition index                                      |
| `algorithm.AsGrid()`           | concrete algorithm configuration                      |
| `algorithm.AsGrid().Repeat(n)` | concrete algorithm configuration and repetition index |

The concrete key for a repeated grid is `(TAlgorithm Inner, int Repetition)`. Materialization order determines trial collections, completion results, analysis results and aggregate failure order.

Creating an experiment run materializes its trials but does not start execution:

```csharp
var run = experiment.CreateRun(problem, random);
```

`CreateRun(...)` expands the finite configuration, creates the typed trial keys, assigns structural random fork paths and creates the child algorithm run setup objects. Analyzer states, execution registries and execution instances are created only when execution starts.

`MaterializeCases()` is an advanced inspection and integration API for callers that need the concrete algorithm configurations, keys and random fork paths without creating a run:

```csharp
var cases = experiment.MaterializeCases();
```

Normal experiment execution does not require this method.

For simple execution without analysis configuration, convenience extensions create the run internally:

```csharp
var results = await experiment.CompleteAsync(problem, random);
```

## Run lifecycle

Experiment runs are configurable until execution starts and single use afterwards:

| `ExecutionStarted` | Analyzer attachment | Execution                               |
| ------------------ | ------------------- | --------------------------------------- |
| `false`            | Allowed             | The first execution call starts the run |
| `true`             | Rejected            | Another execution call is rejected      |

`Stream(...)`, `StartTrials(...)`, `Complete(...)` and `CompleteAsync(...)` start execution when called, not when a returned stream or task is first awaited. Starting combined execution prepares every trial through its ordinary algorithm run API and prevents later direct execution of those trial runs.

Starting one trial run directly prevents later combined execution of the experiment run. Other unstarted trials remain available for individual execution. Runs remain single use after success, failure or cancellation.

The lifecycle guards are usage checks rather than synchronization. Configuring or starting the same run concurrently is unsupported.

## Execution choices

`ExperimentRun` provides two core execution methods:

| API             | Use                                                                               |
| --------------- | --------------------------------------------------------------------------------- |
| `Stream()`      | Observe one combined stream containing the trial and state for every entry        |
| `StartTrials()` | Receive one hot completion task per trial and choose how to await or inspect them |

Completion and manual trial execution are projections over those core methods:

| API                                | Use                                                                                                |
| ---------------------------------- | -------------------------------------------------------------------------------------------------- |
| `Complete()` and `CompleteAsync()` | Convenience extensions over `StartTrials()` that return all final states when every trial succeeds |
| `Trials`                           | Execute or stream selected algorithm runs manually                                                 |

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

Random assignments are based on each trial's structural `RandomForkPath`, not object hash codes or scheduling order. The path is exposed on the trial for reproducibility. The same experiment root generator therefore gives each trial the same child sequence under sequential, unbounded concurrent and bounded concurrent execution.

## Analysis

An experiment analysis uses a `TrialAnalyzer`. It contains a selector from the concrete trial algorithm configuration to exactly one operator and a factory that creates one analyzer for the selected operator:

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

`ExperimentRun` accepts analyzers only through this trial specific binding because each materialized algorithm may expose a different operator configuration. Attach an analyzer directly to `trial.Run` only when configuring that one trial for individual execution.

`WithAnalyzer(...)` evaluates the selector and factory for every already materialized trial before attaching anything from that trial analyzer. If selection or creation fails, that attachment adds no analyzers and the run remains configurable. Attaching the same `TrialAnalyzer` object twice throws.

Selectors and factories are runtime setup helpers. The run retains the concrete analyzer configurations attached to its child algorithm runs. It does not treat the delegates, active runs or execution infrastructure as persistable configuration.

Use another `WithAnalyzer(...)` call when attaching another analyzer, including another analyzer for the same selected operator. Attachments are applied in call order and observation registrations for the same operator are merged without discarding earlier analyzers.

The `TrialAnalyzer` returned through `out` is also the typed lookup object:

```csharp
var results = run.GetResults(bestQuality);
```

Each `TrialAnalysisResult` contains the trial, its concrete analyzer configuration and its typed result. The ordered aggregate is available after every trial run has started. A separately attached analyzer on one trial remains accessible through `trial.Run.GetResult(...)`.

## Failure and cancellation

Failures during trial preparation or execution do not cancel independent trials. Successful trials remain successful and their analyzer results remain available through their algorithm runs.

Explicit cancellation is different. The shared cancellation token stops active trials and prevents pending trials from starting. Combined execution then throws cancellation rather than reporting it as an experiment trial failure.

Cancellation also takes precedence when another trial has already failed. Every task returned by `StartTrials()` still reaches a terminal state, so callers can safely observe the complete task collection after cancellation.

## Related pages

1. [Algorithm](algorithm.md)
2. [Execution model](execution-model.md)
3. [Observability and analysis](observability-and-analysis.md)
4. [Randomness](randomness.md)
