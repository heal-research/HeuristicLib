# Running algorithms

Running an algorithm combines an immutable configuration with one problem and one explicit random source. Each execution starts with fresh run state.

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
    <p>Fresh execution state</p>
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

## Stream or complete

Use `Stream` when the caller needs progress:

```csharp
await foreach (var state in algorithm.Stream(problem, random, ct: cancellationToken))
{
    RenderProgress(state);
}
```

Use `CompleteAsync` when the final state is enough:

```csharp
var finalState = await algorithm.CompleteAsync(
    problem,
    random,
    ct: cancellationToken);
```

Both forms represent a new run. Do not call one after the other expecting the second call to continue the first.

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

## Explicit runs for analysis

The direct extensions cover most applications. Create a run object when you need to attach analyzers or inspect results owned by one execution:

```csharp
var run = algorithm.CreateRun(problem, random);
var finalState = await run.CompleteAsync();
```

See [Observability and analysis](/guide/execution/observability-and-analysis) for an analyzer example.

## Pause and resume an explicit run

An explicit `AlgorithmRun` starts in `Preparing` and accepts attachments until `Stream` starts execution. Consume each returned stream only once and finish or dispose it before requesting another stream from the run.

Ending enumeration early pauses the run. Cancellation through the ordinary stream or enumeration token also pauses it. Call `Stream` again on the same run to resume its retained execution state; modules are installed and the algorithm is resolved only on the first start. Supply an initial state only on that first start.

Natural completion sets `Completed`. Module installation or execution failures are terminal; cancellation during installation sets `Canceled`. A failed or canceled run cannot resume. [Experiment runs](/guide/execution/experiments#run-lifecycle) use terminal cancellation and do not support pause and resume.
