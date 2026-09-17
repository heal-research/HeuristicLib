# First-class analyzers and run lifecycle

Status: implemented and validated.

This plan supersedes the analyzer ownership, anchor, resolver-sharing and run-attachment parts of
[analysis usability follow-up](analysis-usability-follow-up.md). That document remains the record of the previously
implemented revision. The earlier [analysis API simplification](analysis-api-simplification.md) records the first
simplification pass.

## Working agreement

The user reviews by staging files. Do not stage, unstage, reset the index or commit. Preserve their working edits and
treat index changes as review activity.

## Design

### Analyzers are stateful run components

An analyzer owns its mutable results. It is not configuration with a separate analyzer execution instance, and it is
not an execution hook. It installs the observation machinery it needs when it is attached to a run:

```csharp
public interface IAnalyzer
{
    void Install(ExecutionInstanceResolverBuilder builder);
}
```

An analyzer may install zero, one or several hooks. Reusing the same analyzer across runs intentionally combines its
results. An analyzer that is intended to receive observations concurrently is responsible for synchronizing its own
state. Built-in trace analyzers retain synchronization suitable for observing several sources.

### Observation uses existing algorithm and operator concepts

Remove `IAnchor`, `Anchor.At(...)` and the public `IObservationRecorder` authoring contract. The algorithm or operator
configuration already identifies the observed boundary. Typed observation overloads connect that boundary to a
callback:

```csharp
public void Install(ExecutionInstanceResolverBuilder builder)
{
    builder.Observe(firstEvaluator, RecordFirst);
    builder.Observe(secondEvaluator, RecordSecond);
}
```

Two sources may use different callbacks or the same callback. Each `Observe` overload creates a private runtime hook
that wraps the corresponding resolved algorithm or operator. These hooks are reference objects, not records or
serializable configuration; retaining a delegate in them is therefore intentional runtime behavior.

Algorithms and operators do not know about observations. Observation remains an external execution decoration that
creates observable algorithm or operator wrappers.

### Hooks remain public execution infrastructure

`IExecutionHook` stays public for run behavior that is not analysis, including dynamic-problem updates and external
integrations. Analyzers use hooks but do not implement the hook contract themselves.

The resolver builder installs the same `IExecutionHook` object at most once in one resolver, using a typed
reference-identity set. The same hook may be installed independently in any number of resolvers. This replaces the
analysis-specific `Dictionary<object, HashSet<object>>` observation registry and removes object-typed identities from
observation plumbing.

Analyzers and clocks cache any hook whose stable identity is needed for per-resolver deduplication.

### Trace analysis is one analyzer subsystem

`TraceAnalyzer<TResult>` implements `IAnalyzer` and owns the standardized tracing pipeline:

- one or several observed algorithms or operators;
- measurement;
- aggregation instance;
- retention instance;
- clocks;
- trace entries and synchronization.

Aggregation and retention keep their configuration/execution-instance split because they are reusable strategies
inside a trace. This split is not generalized to all analyzers.

Trace factories accept either one source or several compatible sources. Sources are fixed when the analyzer is
created:

```csharp
var quality = algorithm.Evaluator.TraceBestSoFar();

var evaluators = new[] { firstAlgorithm.Evaluator, secondAlgorithm.Evaluator };
var combinedQuality = evaluators.TraceBestSoFar();
```

A combined trace has one accumulator and one result sink. A trace requires at least one source and ignores duplicate
source references.

Remove `ExecutionInstanceResolver` parameters from every public `Analyzer.Trace` API. Trace analyzers privately
resolve their aggregation and retention configurations. Separate analyzers have separate strategy state. Sharing
results is expressed by attaching one analyzer to several sources and runs, not by sharing a resolver between traces.

### Runs have an explicit lifecycle

`AlgorithmRun` remains the single handle for one execution. It owns the algorithm configuration, concrete problem,
RNG, analyzers, hooks and lifecycle. No separate binding, setup or active-run type is introduced.

Add a lifecycle property to the non-generic `AlgorithmRun` base:

```csharp
public RunLifecycleState LifecycleState { get; }
```

The lifecycle states are:

- `Preparing`: analyzers and hooks may be added;
- `Running`: an execution entry point has started and composition is frozen;
- `Paused`: a consumer stopped between yielded root-algorithm states and the run can continue;
- `Completed`: the underlying execution stream ended successfully;
- `Canceled`: cancellation ended execution;
- `Failed`: setup or execution threw an exception;
- `Stopped`: a coordinated execution stopped without a continuation.

`Stream`, `Complete` and `CompleteAsync` are execution entry points. Starting through any of them atomically leaves
`Preparing`, freezes attachments, installs analyzers and hooks, creates the resolver, and resolves the algorithm
execution graph. Resolver construction or algorithm resolution failure moves the run to `Failed`.

Each returned execution stream is single-use. The run permits one active stream. Disposing that stream pauses at the
last yielded root-algorithm state without disposing the underlying algorithm iterator. Calling `Stream` again resumes
that iterator, and calling it after natural completion returns an empty stream. Attachments remain frozen after the
first start.

Replace `ExecutionStarted` with the lifecycle property. Attachment and start errors report the current lifecycle in a
clear exception message.

### Run composition is explicit

Remove the trailing analyzer and hook collections from `CreateRun`. A newly created run is prepared explicitly:

```csharp
var run = algorithm.CreateRun(problem, random)
    .AddAnalyzer(quality)
    .AddExecutionHook(dynamicUpdates);

var result = await run.CompleteAsync();
```

`CreateRun(problem, random)` is the point where the algorithm is applied to the concrete problem, the combination is
validated, the RNG is assigned and a resumable run is created. `AddAnalyzer` and `AddExecutionHook` are fluent and
valid only while the run is `Preparing`. Adding the same analyzer or hook reference repeatedly has no additional
effect.

Direct `algorithm.Stream(...)` and `algorithm.Complete(...)` conveniences may continue to create and execute an
unobserved run. Users who need analyzers or hooks create the run explicitly.

### Experiments use the same run model

An `ExperimentRun` creates one distinct `AlgorithmRun` per materialized trial. Each trial run owns its RNG fork,
attachments and lifecycle; no second execution-handle type is needed.

`TrialAnalyzer` remains the established typed factory and lookup key for creating one analyzer from each trial's
concrete algorithm configuration:

```csharp
var run = experiment.CreateRun(problem, random)
    .AddTrialAnalyzer(qualityPerTrial);
```

`TrialAnalyzer` produces `IAnalyzer` objects rather than hooks. Adding a trial analyzer creates and attaches one
analyzer to every still-preparing trial run and records the typed trial-to-analyzer mapping used by `GetAnalyzers`.
Adding the same trial analyzer twice remains an error because it would make result lookup ambiguous.

`ExperimentRun` receives the same explicit lifecycle model. Its lifecycle describes the coordinated experiment;
individual trial lifecycle states remain available through each trial's `AlgorithmRun`.

### Names follow runtime ownership

Stateful runtime collectors use `Analyzer`, while immutable published values use analysis or snapshot terminology.
Rename the existing specialized collectors accordingly:

- `GenealogyAnalysis` to `GenealogyAnalyzer`;
- `RankAnalysis` to `RankAnalyzer`;
- `ParetoFrontAnalysis` to `ParetoFrontAnalyzer`;
- `BestBeforeChangePerformanceAnalysis` to `BestBeforeChangePerformanceAnalyzer`.

### Review revisions

The API review after the first implementation adds these changes:

- `AlgorithmRun.TracePopulationCandidates(out analyzer, algorithm: null)` is the short path. Omitting the source
  observes the run's root algorithm; supplying one observes a nested algorithm. The method returns the run so fluent
  composition remains intact.
- Specialized trace factories use the same `Trace...` prefix and are extensions on the observed source. This keeps
  trial factories concise: `TrialAnalyzer.Create(algorithm => algorithm.TracePopulationQuality())`.
- `TraceAnalyzer.RequireLatestValue()` reads the common post-completion result. `Snapshot().Values` returns a stable
  value curve when the whole history is needed.
- Restore the term `TraceRetention`. It says that the setting controls which aggregated observations the trace keeps.
- Use `IComparer<ObjectiveVector>` from the objective system directly. Remove the parallel `ObjectiveOrdering` type.
- Keep trace implementation files under `Analysis/Tracing` without changing their namespace. Split the `Analyzer`
  overloads by observed source type and keep shared construction in one internal factory file.
- The source-specific overloads still repeat signatures needed for C# type inference. Do not add a public generic
  observation-source abstraction merely to reduce those lines; revisit the repetition after the public trace shapes
  settle.
- One algorithm run owns one resolved execution and one underlying async iterator. Stream views are exclusive and
  single-consumer. Disposing or canceling a view pauses between root-algorithm yields; a later view continues the same
  iterator. A naturally completed run returns an empty stream.
- Keep only production-facing desired API specs. Prototype-only frequency transposition and detached-anchor behavior
  are not restored as if they had been shipped features.

### Deferred trace-boilerplate reduction

Revisit the repeated observation hooks, runtime wrappers and source-specific trace overloads after merging the dev
branch, which changes the same infrastructure. Compare these options against that merged design:

1. Keep the typed boundary adapters and remove only redundant factory overloads. This preserves direct type inference
   and keeps runtime behavior explicit, but removes the least code.
2. Generate the mechanical hooks, wrappers and overloads from a small boundary description. This removes maintained
   repetition without adding a runtime abstraction, at the cost of source-generation and debugging complexity.
3. Introduce a shared runtime observation-source abstraction. This removes the most handwritten code, but risks
   recreating anchors or delegate-holding binding objects and should be accepted only if the merged design gives it a
   clear semantic role.

Do not choose or implement one of these approaches before reviewing the corresponding dev-branch changes.

## Implementation sequence

1. Add `IAnalyzer` and the run lifecycle contract in `HeuristicLib.Contracts`.
2. Make `AlgorithmRun` attachments mutable only during `Preparing`; add fluent analyzer and hook attachment.
3. Track terminal lifecycle states through synchronous and asynchronous stream consumption.
4. Deduplicate hook installation by typed reference identity in `ExecutionInstanceResolverBuilder`.
5. Add typed callback-based observation overloads for algorithms, evaluators, crossovers, mutators and interceptors.
6. Migrate clocks and analyzers to cached observation hooks, preserving clocks-before-trace ordering.
7. Remove anchors, observation recorders and the analysis-specific observation registry.
8. Convert `TraceAnalyzer` and trace factories to the new analyzer contract and multi-source observation.
9. Remove public resolver-sharing parameters and tests; replace relevant scenarios with combined analyzers.
10. Convert and rename specialized analyzers in core, experimental code and Python interop.
11. Move experiment analyzer creation to `AddTrialAnalyzer` and apply lifecycle tracking to `ExperimentRun`.
12. Update guides, examples, the analyzer architecture document and the canonical glossary.

## Verification

Focused tests must cover:

- separate callbacks for observed operators of the same role;
- one trace observing several compatible sources;
- analyzer reuse across sequential runs;
- built-in trace synchronization for concurrent observations;
- shared clocks installed once per run;
- exact hook-reference deduplication within one resolver and independent installation across resolvers;
- every run lifecycle transition, including setup failure, execution failure, cancellation and early stream disposal;
- rejection of attachments and repeated execution after leaving `Preparing`;
- independent analyzers and lifecycle states for experiment trials;
- trial analyzer lookup after fluent attachment.

During implementation, run focused core tests first. Because this changes public contracts and cross-project execution,
final validation includes restore, Release build, all four test projects, whitespace verification, style verification at
warning severity, analyzer verification at error severity, and `git diff --check`.

Completed validation:

- Release solution build;
- `HeuristicLib.Tests`: 2,017 passed;
- `HeuristicLib.Tests.ApiUsageSpecs`: 146 passed;
- `HeuristicLib.Tests.Experimental`: 167 passed;
- `HeuristicLib.Tests.Scenarios`: 24 passed;
- whitespace, style and analyzer format verification;
- `git diff --check`.

## Out of scope

- A separate analyzer configuration/execution-instance hierarchy.
- A separate algorithm/problem binding type.
- Separate prepared-run and active-run handles.
- A public API for sharing aggregation instances between separate traces.
- A new concurrent ingestion queue for analyzers. Add one only when concrete multi-run workloads require it.
