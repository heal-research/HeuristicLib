# Algorithm runs

Running an algorithm combines an immutable configuration with one problem and one explicit random source. Each execution starts with fresh run state.

## Choose how much control you need

Start with a completion method when you only need the final state. Choose streaming for progress or retain an explicit run when you need analysis, inspection or pause and resume. These entry points use the same algorithm configuration and execution machinery.

| What you need                   | Entry point                                                            | Continue below                                                                                 |
| ------------------------------- | ---------------------------------------------------------------------- | ---------------------------------------------------------------------------------------------- |
| Final search state              | `algorithm.RunToCompletion(problem, random)` or `RunToCompletionAsync` | [Run to completion or stream](#run-to-completion-or-stream)                                    |
| Progress from a fresh execution | `algorithm.RunStreaming(problem, random)`                              | [Run to completion or stream](#run-to-completion-or-stream)                                    |
| Retain control of one execution | `algorithm.CreateRun(problem, random)`                                 | [Explicit runs](#explicit-runs-for-control-and-analysis)                                       |
| Analysis results                | Pass analyzers to `CreateRun`, then use `run.Result`                   | [Analysis](#explicit-runs-for-control-and-analysis) and [run results](#read-run-bound-results) |

Executor construction happens automatically. You do not need to manage those steps to use any of these entry points.

<ModelDiagram>

`RunStreaming` yields every search state of a fresh run. `RunToCompletionAsync` consumes the same stream and returns its last state.

</ModelDiagram>

## Run to completion or stream

Use `RunToCompletionAsync` when the final state is enough:

```csharp
var finalState = await algorithm.RunToCompletionAsync(
    problem,
    random,
    ct: cancellationToken);
```

Use `RunToCompletion` for synchronous completion. Both completion methods return the final search state without requiring you to retain a run object.

Use `RunStreaming` when the caller needs progress:

```csharp
await foreach (var state in algorithm.RunStreaming(problem, random, ct: cancellationToken))
{
    RenderProgress(state);
}
```

Each call on the configuration starts a new run. To continue an execution after ending enumeration early, retain an [explicit run](#pause-and-resume-an-explicit-run).

## Stopping budgets

Prefer an algorithm's own budget for normal configuration. A genetic algorithm exposes `MaximumGenerations`, which defaults to `GeneticAlgorithmDefaults.MaximumGenerations` (1000) rather than to no limit. Set it to `null` for a run that should end on a terminator alone. Other algorithms may use iterations, evaluations or a domain specific condition.

External wrappers are useful when an experiment must impose the same budget across algorithms that expose different controls. When comparing methods, evaluation count is often fairer than generation count because one generation can perform very different amounts of work.

## Cancellation

Pass a cancellation token from the host application. Cancellation is for external interruption such as a user request or service shutdown. It should not replace a deterministic algorithm budget.

```csharp
using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(2));
var finalState = await algorithm.RunToCompletionAsync(problem, random, ct: timeout.Token);
```

## Reproducibility

Create the random number generator from a recorded seed:

```csharp
var random = RandomNumberGenerator.Create(seed: 123);
```

The algorithm forks that source deterministically for its work. [Randomness](/guide/execution/randomness) shows how to draw values, fork independent streams and reproduce reported results.

## Explicit runs for control and analysis

Create a run object when you need to retain execution control, attach analyzers or inspect results owned by one execution:

```csharp
var run = algorithm.CreateRun(problem, random);
var finalState = await run.CompleteAsync();
```

Completing an explicit run follows the same path as `algorithm.RunToCompletionAsync(problem, random)`. Each `CreateRun` creates independent executors and run results ([Introduction](/guide/#configurations-and-executors)). The explicit form gives access to the run lifecycle and `run.Result(configuration)`.

Pass analysis to `algorithm.CreateRun(problem, random, [analyzer])`. [Configurations](/guide/fundamentals/configurations#substituting-for-one-run) explains how substitutions for one run differ from a configuration you keep with `Substitute`, and which configuration names their targets when a budget produced the algorithm. [Analysis of algorithm runs](/guide/execution/observability-and-analysis) shows the analyzers you can pass.

## Read run-bound results

An analyzer creates one result per run. Pass it to `algorithm.CreateRun(problem, random, [analyzer])`, then read it with `run.Result(analyzer)`. An analyzer that places wrappers must be held by the run's algorithm, or lookup throws unless a declared delegate prevents proving it absent. A lookup before execution creates the result lazily. Execution and later reads use the same object. Reusing an analyzer across runs gives each run independent data, and one run keeps one result for all placements of an analyzer, also across child activations such as the turns of a cycle. Counters and durations also create run results, read through their configurations in the same way.

A result stays readable after completion, cancellation, failure and pausing. Whether a read during execution is safe depends on the result type: a trace supports snapshots, while an accumulating result is read once execution has stopped.

Private execution data, such as a cache, belongs to an executor, not to the run. [How execution works](/extend/how-execution-works) explains the difference.

## Pause and resume an explicit run

An explicit `AlgorithmRun` is returned in `Ready` without constructing the executor graph. Its `LifecycleState` is `Ready`, `Running`, `Paused`, `Completed`, `Canceled` or `Failed`. The run accepts no further analyzers after `CreateRun` returns. Consume each returned stream only once and finish or dispose it before requesting another stream from the run. Requesting another stream while one is active throws without changing the active execution.

Ending enumeration early, including disposing the stream, leaves the run `Paused`. Cancellation through the ordinary stream or enumeration token also pauses it. Call `Stream` again on the same run to resume its retained iterator, executors and run-bound results. The root algorithm is resolved only on the first start. Resuming continues the retained iterator. Supply an initial state only on that first start.

<RunLifecycle>

`AlgorithmRun` states. Each returned stream has one consumer. A failed or canceled run cannot resume.

</RunLifecycle>

Natural completion sets `Completed` after iterator cleanup succeeds. A cleanup failure sets `Failed` and propagates the exception. If the executor throws during iteration, including an `OperationCanceledException`, the explicit run sets `Failed`, disposes the iterator and propagates the execution exception. Cancellation thrown during execution-graph setup sets `Canceled`. A failed or canceled run cannot resume. A construction failure also ends its resolution scope tree.

[Experiment runs](/guide/execution/experiments#run-lifecycle) use terminal cancellation and do not support pause and resume. Terminal cancellation of a trial records `Canceled` before iterator cleanup, so that trial remains canceled even if cleanup throws.

## Next steps

- [Search states](/guide/execution/search-states) continues the guide with what a run yields.
