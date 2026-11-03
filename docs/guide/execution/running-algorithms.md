# Running algorithms

Running an algorithm combines an immutable configuration with one problem and one explicit random source. Each execution starts with fresh run state.

## Choose how much control you need

Start with a completion method when you only need the final state. Choose streaming for progress or retain an explicit run when you need setup, inspection or pause and resume. These entry points use the same algorithm configuration and execution machinery.

| What you need                          | Entry point                                              | Continue below                                                                               |
| -------------------------------------- | -------------------------------------------------------- | -------------------------------------------------------------------------------------------- |
| Final search state                     | `algorithm.Complete(problem, random)` or `CompleteAsync` | [Complete or stream](#complete-or-stream)                                                    |
| Progress from a fresh execution        | `algorithm.Stream(problem, random)`                      | [Complete or stream](#complete-or-stream)                                                    |
| Retain control of one execution        | `algorithm.CreateRun(problem, random)`                   | [Explicit runs](#explicit-runs-for-control-and-analysis)                                     |
| Runtime analysis or supplied resources | `CreateRun` with a setup callback                        | [Analysis](#explicit-runs-for-control-and-analysis) and [resource supply](#supply-resources) |

Executor construction and aspect weaving happen automatically. You do not need to manage those steps to use any of these entry points.

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
    <p>Independent run resources</p>
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

Without custom setup, completing this explicit run follows the same path as `algorithm.CompleteAsync(problem, random)`. The explicit form gives you the run object for further control and inspection. Each call to `CreateRun` creates a separate execution with independent default resources.

Use the setup callback when attaching runtime analysis or supplying resources. Setup completes before `CreateRun` returns. The algorithm still owns the search process, including when it contains custom operators or child algorithms.

See [Observability and analysis](/guide/execution/observability-and-analysis) for an analyzer example.

## Supply resources

A configuration may contain a `ResourceHandle<TResource>`. All executors using that handle within one run obtain one resource. Separate runs create independent resources by default. Record copies retain handles, so several operators can share resources within each independent trial.

Supply an existing resource through the setup callback:

```csharp
var run = algorithm.CreateRun(problem, random, setup => setup
    .AddResource(cacheHandle, new EvaluationCache<RealVector>()));
```

Duplicate supply is rejected during setup. The returned run accepts no further resources or aspects. Later changes to a captured setup object cannot alter its snapshot. Automatically created resources need no deterministic cleanup. Resources requiring cleanup must be supplied and remain caller owned. Finish all borrowing runs before cleanup.

Use `run.Resource(handle)` to read the resource used by that run's executors. Looking it up before execution lazily creates its default instance, or returns the supplied instance. Repeated lookups return the same object. Separate runs retain independent defaults. The resource itself defines which concurrent reads and writes are supported, and lookup does not transfer ownership.

Inside an experiment's `CreateRun` setup callback, `setup.AddResourcePerTrial(configuration => (handle, resource))` supplies a binding to each materialized case before trial runs are created. Callbacks run in trial order before any bindings are installed. Callback or binding validation failure starts no trials and installs no bindings from that call. Callback side effects remain the caller's responsibility. Returning the same instance deliberately shares it across trials, so its concurrency contract must support their scheduling.

## Pause and resume an explicit run

An explicit `AlgorithmRun` is returned in `Ready` after its optional setup callback completes. Consume each returned stream only once and finish or dispose it before requesting another stream from the run. Requesting another stream while one is active throws without changing the active execution.

Ending enumeration early pauses the run. Cancellation through the ordinary stream or enumeration token also pauses it. Call `Stream` again on the same run to resume its retained iterator, executors and resource bindings. The root scope snapshots setup during creation and the root algorithm is resolved with its aspects only on the first start; resuming continues the retained iterator. Supply an initial state only on that first start.

Natural completion sets `Completed` after iterator cleanup succeeds. A cleanup failure sets `Failed` and propagates the original exception, including an `OperationCanceledException` thrown during cleanup. Execution-graph setup or execution failures are terminal; cancellation thrown during graph setup sets `Canceled`. A failed or canceled run cannot resume. This run lifecycle is separate from a resolution scope's ability to retry a failed construction.

[Experiment runs](/guide/execution/experiments#run-lifecycle) use terminal cancellation and do not support pause and resume. Terminal cancellation of a trial records `Canceled` before iterator cleanup, so that trial remains canceled even if cleanup throws.
