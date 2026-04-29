# Analyzer architecture

This page explains the current analyzer system in HeuristicLib.

It builds on the ideas from [Observability & analysis](observability-and-analysis.md) and [Definition vs execution instances](execution-instances.md), but adds one crucial concept:

> Analyzer state belongs to the **run**, not to the algorithm/operator definition and not to a short-lived operator execution instance.

## Why analyzers need their own architecture

Analyzer data has a different lifetime from normal operator or algorithm configuration.

- **Definitions** are declarative and reusable.
  - They should contain configuration and graph structure only.
  - They must stay safe to reuse across multiple runs.
- **Execution instances** are runtime objects.
  - They may hold temporary state while something executes.
  - Their lifetime is controlled by `ExecutionInstanceRegistry` and by meta-algorithms such as `CycleAlgorithm`.
- **Analyzer state** is usually meaningful at the **run** level.
  - quality curves
  - genealogy graphs
  - per-iteration statistics
  - accumulated counters and traces

If analysis state were stored directly on a definition, rerunning the same definition would mix old and new results.
If analysis state were stored only inside an observer/decorator execution instance, results would be tied to registry mechanics rather than to the logical run.

That is why HeuristicLib models analyzers as **definition-side descriptions + run-scoped analyzer run states + run-owned analyzer result lookup**.

## The three layers of the analyzer system

### 1) Analyzer definition

An analyzer definition is a reusable object that describes:

- what the analyzer observes
- which operators it needs references to
- how to create its analyzer run state and result for a run

The current contracts are:

```csharp
public interface IAnalyzer
{
  IAnalyzerRunState CreateAnalyzerState();
}

public interface IAnalyzer<out TResult> : IAnalyzer
  where TResult : class
{
  new IAnalyzerRunState<TResult> CreateAnalyzerState();
}
```

An analyzer definition is still part of the **definition graph**.
It is configuration-time information, not runtime mutable state.

### 2) Observation requirements

Analyzers do not mutate the definition graph directly.
Instead, analyzer states declare observation requirements through `RegisterObservations(ObservationPlan)`.

Typical hook points are:

- `IEvaluator<...>`
- `IInterceptor<...>`
- `IMutator<...>`
- `ICrossover<...>`
- `ISelector<...>`
- `IReplacer<...>`
- `ITerminator<...>`
- `ICreator<...>`

The concrete observable wrappers still do the actual callback work:

- `ObservableEvaluator<...>`
- `ObservableInterceptor<...>`
- `ObservableMutator<...>`
- `ObservableCrossover<...>`
- `ObservableSelector<...>`
- `ObservableReplacer<...>`
- `ObservableTerminator<...>`
- `ObservableCreator<...>`

What analyzers contribute is the **declarative registration** of which operator should be observed and with which callback.

### 3) Run-scoped analyzer state

At runtime, every analyzer gets **one analyzer run state per run**.

That instance:

- receives callbacks from all hook points that belong to that analyzer
- owns observation registration
- exposes the user-facing analyzer result

The run-scoped contract is:

```csharp
public interface IAnalyzerRunState
{
  void RegisterObservations(ObservationPlan observations);
}

public interface IAnalyzerRunState<out TResult> : IAnalyzerRunState
  where TResult : class
{
  TResult Result { get; }
}
```

There is also a small optional convenience base class in `HEAL.HeuristicLib.Core`:

```csharp
public abstract class AnalyzerRunState<TAnalyzer>(TAnalyzer analyzer) : IAnalyzerRunState
  where TAnalyzer : IAnalyzer
{
  protected TAnalyzer Analyzer { get; } = analyzer;

  public abstract void RegisterObservations(ObservationPlan observations);
}
```

This helper is only a convenience. It is **not** part of the abstraction assembly, and it does not own run state beyond the analyzer reference.

## Ownership and lifetimes

### Definition owns configuration

The analyzer definition owns:

- configuration
- identity
- references to operators it wants to observe
- the logic for creating analyzer run state and result

It does **not** own mutable run results.

### Analyzer result owns mutable analysis data

The analyzer result owns things like:

- current counters
- in-progress aggregation for the current iteration
- temporary buffers
- best-so-far values while evaluations happen
- a mutable genealogy graph being built during execution

This result exists only for the current run.

### Run owns analyzer result lookup

The `Run` object creates one analyzer run state per analyzer definition and stores that mapping.
Users retrieve analyzer results through the run:

```csharp
var result = run.GetAnalyzerResult(analyzer);
```

The `GetResult(...)` and `TryGetResult(...)` aliases delegate to the same state lookup.

This makes analyzer retrieval:

- independent of individual operator instances
- independent of `ExecutionInstanceRegistry` reuse details
- available through a stable run-level API

## Runtime flow

### Run creation

1. Create analyzer definitions.
2. Create a run with those analyzers:

```csharp
var run = algorithm.CreateRun(problem, analyzer1, analyzer2);
```

3. `Run` creates one analyzer state for each analyzer.
4. Each analyzer state calls `RegisterObservations(...)`.
5. `Run` collects those observation requests in an `ObservationPlan`.
6. When the root registry or a fresh registry is created, `Run` installs the merged observation registrations into that registry. Child registries inherit those replacements from their parent registry.

### Observation installation

The observation plan stores merged observation entries.
Those entries:

- identify the original operator they belong to
- merge multiple analyzer subscriptions for the same operator
- pre-register one observable replacement into an `ExecutionInstanceRegistry`

This keeps analyzer registration declarative while avoiding deep wrapper chains when several analyzers observe the same operator.

### During execution

1. The operator or algorithm performs its normal work.
2. The observable wrapper invokes the analyzer callback.
3. The analyzer callback updates its analyzer result.
4. Users can inspect that result through `Run.GetAnalyzerResult(...)` during or after execution.

There is currently **no separate publish step**. The analyzer run state exposes the result object directly through `IAnalyzerRunState<TResult>.Result`, and `Run.GetAnalyzerResult(...)` returns that result.

## Why the run is the right scope

`ExecutionInstanceRegistry` still matters, but it is not the right place to *own* analysis data.

A registry controls the lifetime of operator and algorithm execution instances.
This is useful for:

- shared sub-graphs
- stateful operators
- meta-algorithm decisions about reuse or reset

But analyzers are usually intended to describe the full logical run.

For example, with `CycleAlgorithm`:

- operator execution instances may reset per cycle
- or may persist per inner algorithm
- but the analyzer result should still describe the whole run

Because analyzer run states are created by `Run` and then registered into every relevant registry, analyzer scope stays stable even when execution registries change.

## Definition-side hooks vs execution-side logic

An analyzer usually has two responsibilities that should stay separate.

### Definition side: what to observe

This side answers:

- Which evaluator should I observe?
- Which interceptor should I observe?
- Do I need crossover, mutation, selector, replacer, creator, or terminator hooks?

This is handled by the analyzer definition holding references to the relevant operators.

### Execution side: what to do with the data

This side answers:

- What mutable result do I accumulate?
- How do I combine several hook points into one analysis result?
- What shape should users retrieve from the run?

This is handled by the analyzer result.

## Example patterns

### `QualityCurveAnalysis<TGenotype, ...>`

This analyzer observes evaluator events and stores a best-so-far quality curve.

Its analyzer result stores:

- current best solution
- current evaluation count
- collected curve points

### `BestMedianWorstAnalysis<T, ...>`

This analyzer observes iteration-boundary interception and stores one entry per completed iteration:

- best solution
- median solution
- worst solution

### `GenealogyAnalysis<T, ...>`

This analyzer combines several hook types:

- crossover hooks
- mutator hooks
- interceptor hooks

Its analyzer result builds a genealogy graph over time.

## Retrieval model

Consumers retrieve analyzer results from the run.

Preferred pattern:

```csharp
var analyzer = new QualityCurveAnalysis<MyGenotype, MySearchSpace, MyProblem>(evaluator);
var run = algorithm.CreateRun(problem, analyzer);
var finalState = run.RunToCompletion(random);

var qualityCurve = run.GetAnalyzerResult(analyzer);
```

Avoid treating observable wrapper instances or execution registries as the result container.
The registry is an execution detail; the run is the public analyzer-result scope.

## Design rules for analyzer authors

### Do

- keep analyzer definitions reusable and configuration-only
- put mutable analysis data into the analyzer result
- register observation needs through `RegisterObservations(...)`
- retrieve analyzer results through `Run.GetAnalyzerResult(...)`
- rely on observable wrappers as the callback mechanism

### Do not

- store mutable analysis results directly on definitions
- treat observable wrapper instances as the permanent home of results
- rely on registry reuse details for analyzer lifetime
- mutate algorithm outcomes from analyzer callbacks

## Relationship to observable wrappers

The analyzer system does **not** replace observable wrappers.
Instead:

- observable wrappers are the **hooking mechanism**
- analyzer run states connect observations to **run-scoped result objects**
- `ObservationPlan` is the **bridge** between declarative analyzer registration and registry-specific wrapper installation

This keeps concerns separate:

- wrappers define *where* callbacks happen
- analyzers define *which* callbacks they want
- analyzer results define *how* data is accumulated

## When to use analyzers vs plain observable operators

The distinction is:

- an **observable operator** is a callback hook on one wrapped definition
- an **analyzer** is a run-scoped analysis object that uses those hooks

In practice:

- choose **observable operators** when you want quick, local instrumentation
  - logging
  - counters
  - ad-hoc diagnostics
  - integration with an external sink that already owns the collected data
- choose **analyzers** when you want reusable analysis that conceptually belongs to the whole run
  - quality curves
  - genealogy
  - traces spanning several hook points
  - analysis that must stay stable across registry recreation in meta-algorithms

If you only need a callback, `ObserveWith(...)` is often enough.
If you want users to retrieve a coherent result object from `Run`, prefer an analyzer.

## Related pages

- [Observability & analysis](observability-and-analysis.md)
- [Definition vs execution instances](execution-instances.md)
- [Execution model](execution-model.md)
