# Running algorithms

Running an algorithm combines an immutable configuration with one problem and one explicit random source. Each execution starts with fresh run state.

## Choose how much control you need

Start with a completion method when you only need the final state. Choose streaming for progress or retain an explicit run when you need setup, inspection or pause and resume. These entry points use the same algorithm configuration and execution machinery.

| What you need                   | Entry point                                                                 | Continue below                                                                                 |
| ------------------------------- | --------------------------------------------------------------------------- | ---------------------------------------------------------------------------------------------- |
| Final search state              | `algorithm.Complete(problem, random)` or `CompleteAsync`                    | [Complete or stream](#complete-or-stream)                                                      |
| Progress from a fresh execution | `algorithm.Stream(problem, random)`                                         | [Complete or stream](#complete-or-stream)                                                      |
| Retain control of one execution | `algorithm.CreateRun(problem, random)`                                      | [Explicit runs](#explicit-runs-for-control-and-analysis)                                       |
| Analysis results                | Attach instruments with `AnalyzedBy`, then use `CreateRun` and `run.Result` | [Analysis](#explicit-runs-for-control-and-analysis) and [run results](#read-run-bound-results) |

Executor construction happens automatically. You do not need to manage those steps to use any of these entry points.

<div class="flow-chart flow-chart--execution" role="img" aria-label="An algorithm configuration, problem and random source create a fresh run. Stream observes every state while CompleteAsync returns the final state.">
  <section class="flow-card">
    <h3>Run inputs</h3>
    <p>Algorithm configuration</p>
    <p>Problem</p>
    <p>Random source</p>
  </section>
  <div class="flow-arrow" aria-hidden="true"><span>create</span><strong>↓</strong></div>
  <section class="flow-card flow-card--primary">
    <h3>Algorithm run</h3>
    <p>Independent run results</p>
  </section>
  <div class="flow-arrow" aria-hidden="true"><span>consume with</span><strong>↓</strong></div>
  <div class="flow-chart__outputs">
    <section class="flow-card flow-card--output">
      <h3><code>Stream</code></h3>
      <p>Observe every state</p>
    </section>
    <section class="flow-card flow-card--output">
      <h3><code>CompleteAsync</code></h3>
      <p>Receive the final state</p>
    </section>
  </div>
</div>

## Complete or stream

Use `CompleteAsync` when the final state is enough:

```csharp
var finalState = await algorithm.CompleteAsync(
    problem,
    random,
    ct: cancellationToken);
```

Use `Complete` for synchronous completion. Both completion methods return the final search state without requiring you to retain a run object.

Use `Stream` when the caller needs progress:

```csharp
await foreach (var state in algorithm.Stream(problem, random, ct: cancellationToken))
{
    RenderProgress(state);
}
```

Each call on the configuration starts a new run. To continue an execution after ending enumeration early, retain an [explicit run](#pause-and-resume-an-explicit-run).

## Stopping budgets

Prefer an algorithm's own budget for normal configuration. A genetic algorithm exposes `MaximumGenerations`, which defaults to `GeneticAlgorithmDefaults.MaximumGenerations` (1000) rather than to no limit; set it to `null` for a run that should end on a terminator alone. Other algorithms may use iterations, evaluations or a domain specific condition.

External wrappers are useful when an experiment must impose the same budget across algorithms that expose different controls. When comparing methods, evaluation count is often fairer than generation count because one generation can perform very different amounts of work.

## Cancellation

Pass a cancellation token from the host application. Cancellation is for external interruption such as a user request or service shutdown. It should not replace a deterministic algorithm budget.

```csharp
using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(2));
var finalState = await algorithm.CompleteAsync(problem, random, ct: timeout.Token);
```

## Reproducibility

Create the random number generator from a recorded seed:

```csharp
var random = RandomNumberGenerator.Create(seed: 123);
```

The algorithm forks that source deterministically for its work. Keep the seed, package version, algorithm configuration and problem data with every reported result.

Parallel scheduling can change completion order. Trial identity and seed derivation should not depend on that order. The experiment API handles independent trial runs for you.

## Explicit runs for control and analysis

Create a run object when you need to retain execution control, attach analyzers or inspect results owned by one execution:

```csharp
var run = algorithm.CreateRun(problem, random);
var finalState = await run.CompleteAsync();
```

Completing an explicit run follows the same path as `algorithm.CompleteAsync(problem, random)`. Each `CreateRun` creates independent executors and instrument results. The explicit form gives access to the run lifecycle and `run.Result(instrument)`.

Attach analysis with `AnalyzedBy` before creating the run, and create the run from the configuration it returns. The algorithm owns the search process, including its operators and child algorithms.

See [Observability and analysis](/guide/execution/observability-and-analysis) for an analyzer example.

## Read run-bound results

An instrument defines one result per run. Attach it through `algorithm.AnalyzedBy(instrument)`, call `CreateRun` on the configuration that returns, then read it with `run.Result(instrument)`. Reading a definition the run's algorithm does not hold is an error, because its result would stay empty. A lookup before execution creates the result lazily; execution and later reads use the same object. Reusing the definition across runs gives each run independent data.

Private execution data, such as a cache, belongs to its executor. Use `Sharing` on a configuration enclosing its uses to keep one executor across child scopes. Runs provide no live-object supply API.

## Pause and resume an explicit run

An explicit `AlgorithmRun` is returned in `Ready` without constructing the executor graph. Consume each returned stream only once and finish or dispose it before requesting another stream from the run. Requesting another stream while one is active throws without changing the active execution.

Ending enumeration early pauses the run. Cancellation through the ordinary stream or enumeration token also pauses it. Call `Stream` again on the same run to resume its retained iterator, executors and run-bound results. The root algorithm is resolved only on the first start; resuming continues the retained iterator. Supply an initial state only on that first start.

Natural completion sets `Completed` after iterator cleanup succeeds. A cleanup failure sets `Failed` and propagates the original exception, including an `OperationCanceledException` thrown during cleanup. Execution-graph setup or execution failures are terminal; cancellation thrown during graph setup sets `Canceled`. A failed or canceled run cannot resume. A construction failure also ends its resolution scope tree.

[Experiment runs](/guide/execution/experiments#run-lifecycle) use terminal cancellation and do not support pause and resume. Terminal cancellation of a trial records `Canceled` before iterator cleanup, so that trial remains canceled even if cleanup throws.
