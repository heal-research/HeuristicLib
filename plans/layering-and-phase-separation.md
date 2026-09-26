# Layering and phase separation

Analysis of the layering concerns in HeuristicLib, written against the `analysis-overhaul` branch at `d2ba7789`. No code was changed.

Status: implemented with revisions; current rules are documented in [layering](../docs/contributing/architecture/layering.md), with follow-ups and rejected approaches in the [developer backlog](developer-backlog.md). The findings below describe the original `analysis-overhaul` snapshot; the implementation sequence in Part 6 reflects the settled outcome. The later DI and AOP alignment in [container and aspect framing](container-and-aspect-framing.md) owns phase 4.

The branch consolidated Contracts into the main assembly, kept hosting and concurrency in `Execution` with separate grouping folders, and retained run-specific lifecycle ownership after rejecting a shared host. Lifecycle and enumerator ownership remain together in `AlgorithmRun`; composition and continuation wrappers are not required. The original report's decomposition recommendation is superseded. Layering occupies guideline § 3.5, leaving § 3.6 for the later container/AOP policy.

## Summary

Three findings, in order of how much they matter.

1. Configuration and execution are not layers. They are two phases of the same layer, and asking which one is "more bottom" has no answer because the question assumes a stack where there is a pipeline. The stack metaphor is what makes the rest of the model feel wrong.
2. Analysis is not leaky because it touches two layers. It is leaky because the thing it needs, a way to say *where* to observe, has no type. Its role is played today by a raw reference to a configuration object, so every analysis API drags configuration objects around and looks like a layer violation.
3. `AlgorithmRun` is not badly placed. It is three objects wearing one name: a composition holder, a graph builder and an execution host. Assigning it to a layer cannot work. Splitting it can.

There are also two real dependency cycles in the source, both cheap to break, and neither is the one you would expect.

## Part 1: is "configuration on top of execution" the right metaphor?

No, and I think this is the root of the confusion.

A layer has a direction. The layer above depends on the layer below, and the layer below does not know the layer above exists. TCP knows nothing of HTTP. That asymmetry is the whole content of the idea.

Configuration and execution do not have that relation. Look at the types:

- `IExecutionConfiguration<TInstance>.CreateExecutionInstance(ResolutionScope)` names an instance type, so configuration depends on execution.
- `ResolutionScope.Resolve` is keyed by `IExecutionConfiguration` and returns `IExecutionInstance`, so the resolver depends on both.
- `ObservingMutator<...>.Instance` holds `observedMutator`, an `IMutator<TCandidate>` configuration, because the observation it publishes has to name the configured operator. So an execution instance depends back on configuration.

There is no consistent direction, and there never will be, because `GeneticAlgorithm` and `GeneticAlgorithmInstance` are not two levels of abstraction over the same domain. They are one concept at two points in its lifecycle.

The relation that does fit is describe, resolve, realize. It is the same shape as source code, compiler, running program. Or expression tree, `Compile()`, delegate. Or DI registrations, container, resolved object graph. `ResolutionScope` is the linker in the middle, and it is genuinely well built for that job: declaration and resolution are separate types, the builder cannot escape its callback, decorations compose by an explicit specificity rule, and sibling scopes are isolated so meta-algorithms can express per-cycle freshness. That part of the design is not the problem.

So the metaphor should be:

- Configuration and execution are phases, orthogonal to layers.
- Layers still exist, and every layer above the domain vocabulary has both a configuration face and an execution face.

Your instinct that "execution is at the bottom" is right about one thing and wrong about another. Right: execution instances are where the actual work happens, and nothing in the configuration graph is load-bearing at runtime once resolution is done. Wrong: nothing depends on execution instances in the layering sense, because they are the output of the pipeline rather than a foundation under it. The thing everything actually compiles against, and which knows nothing about anyone, is the domain vocabulary. Candidate, search space, problem, objective, random, search state. That is the real bottom.

There is a second reason "which is the bottom" is hard to answer here, and it is fixable by renaming. The namespace `HEAL.HeuristicLib.Execution` currently holds four unrelated responsibilities:

| What lives there | What it actually is |
| --- | --- |
| `IExecutionConfiguration`, `IExecutionInstance`, `ExecutionSignature` | the composition model |
| `ResolutionScope`, `ResolutionScopeBuilder`, `Decoration`, `DecorationOrigin`, `IExecutionModule` | the linker |
| `AlgorithmRun`, `ExecutionStream`, `RunLifecycleState`, `ExecutionConcurrency` | the run host |
| `BatchExecution` | a parallel-map utility with no connection to any of the above |

One namespace holding a type system, a compiler, a process host and a `Parallel.For` helper will resist any layering story you try to tell about it. "Execution" is doing four jobs as a word.

## Part 2: where the layering actually breaks

I built a namespace dependency matrix over all `using HEAL.HeuristicLib.*` in `src`. Two cycles are real, and one edge is latent risk.

### Cycle 1: operators depend on analysis

`HEAL.HeuristicLib.Operators.*` and `HEAL.HeuristicLib.Algorithms` both reference `HEAL.HeuristicLib.Analysis`, and `Analysis` references them back 16 and 11 times.

The upward edge comes from 25 files, and the entire reason is two types:

- `Analysis/ObservationCounter.cs`, 13 lines, an `Interlocked` wrapper around an `int`.
- `Analysis/ObservationDuration.cs`, 13 lines, an `Interlocked` wrapper around a tick count.

Their consumers are `CountingMutator`, `DurationMeasuringMutator` and their equivalents for every role, plus `LimitEvaluator`, `AfterOperatorCountTerminator`, `AfterOperatorDurationTerminator`, `OperatorBudgetAlgorithm` and `OperatorDurationBudgetAlgorithm`.

Most of those are not analysis at all. `OperatorBudgetAlgorithm` uses the counter to decide when to stop running. That is control flow. The counter got filed under analysis because counting sounds like measurement, and the dependency graph has been upside down ever since. Two 13-line accumulators are holding a cycle open between the operator layer and the analysis layer.

### Cycle 2: the run knows what an analyzer is

`Contracts/Execution/AlgorithmRun.cs` does `using HEAL.HeuristicLib.Analysis` to declare `Add(IAnalyzer)`, and `Contracts/Analysis/IAnalyzer.cs` does `using HEAL.HeuristicLib.Execution` to declare `Install(ResolutionScopeBuilder)`. Both are in the Contracts assembly, so it compiles, but it is a cycle in the conceptual model and it is exactly the one you flagged.

Worth looking at what `IAnalyzer` actually is:

```csharp
public interface IAnalyzer        { void Install(ResolutionScopeBuilder builder); }
public interface IExecutionModule { void Install(ResolutionScopeBuilder builder); }
```

Same signature. Same parameter. Same lifecycle position. `AlgorithmRun` keeps two parallel lists, two `HashSet`s and two `Add` overloads for them.

The documented distinction is real: an analyzer owns results and a module installs behavior. But that distinction is not in the type, it is in the prose. And the split has a cost beyond duplication. In `BeginExecutionSegment`:

```csharp
foreach (var analyzer in analyzers) analyzer.Install(builder);
foreach (var module in modules) builder.Install(module);
```

`ResolutionScopeBuilder.origin` starts at `DecorationOrigin.Configuration` and only `Install(module)` flips it to `Module`. So a decoration declared straight from an analyzer's `Install` is recorded as configuration, and binds tighter than every module decoration.

Correction, 2026-09-23: this does not put it inside a budget wrapper, as an earlier version of this report said. Every configuration decoration is declared in a child scope, an analyzer declares at the root, and within one origin the deeper scope binds tighter, so the budget stays inner. What the wrong origin does is put the analyzer's decoration inside every module decoration regardless of install order, so it observes before analyzers attached ahead of it. `analysis-overhaul` now installs analyzers with module origin.

I checked every `Decorate` call site in the repo. Every one of them is inside an `IExecutionModule.Install` or inside a `CreateExecutionInstance` child scope, so nothing in the library hit this. But the contract let a user-written `IAnalyzer` do it, and the failure would have been silent: correct search states, and an analyzer that reads another analyzer's results inside its callback seeing the previous observation.

### Smaller misplacements

- `SearchConfigurationValidation` lives in `SearchSpaces` but walks a configuration graph and checks `ExecutionSignature`. It is a composition-model concern sitting in the domain vocabulary, and it drags `SearchSpaces -> Operators` along with it.
- `Problems.Dynamic` references `Analysis` for `EpochClock : Clock<int>`. A problem reaching up into analysis. The cause is that `Clock` is filed under analysis when it is really a typed run-scoped axis, useful to anything that wants to timestamp something.
- `Objectives/LexicographicComparer.cs` references `Encodings.Permutations`. The objective vocabulary should not know a concrete encoding exists. Small, but it is the kind of edge that makes a layer rule unenforceable later.
- Resolved since: `TraceFactory` used to call `ResolutionScope.Create()` outside any run to turn aggregation and retention configurations into instances. Aggregation and retention are plain objects now, and the factory is gone.
- `ExperimentRun` reimplements `AlgorithmRun`'s lifecycle state machine. `Preparing`, `TransitionFromRunning`, `EnsurePreparing`, the same `Lock`, the same transitions. Duplicated because the state machine has no owner of its own.

## Part 3: why analysis feels leaky

Your description was that analysis needs the execution part for hooks and the configuration part to say where to attach. I would put it more sharply: analysis is an aspect, and aspects are cross-cutting by definition. Every aspect system has three parts.

| Part | Name in the literature | What HeuristicLib has |
| --- | --- | --- |
| where to inject | pointcut | nothing |
| what to inject | advice | `ObservingMutator` and its four siblings |
| how to inject it | weaver | `ResolutionScope` plus `Decoration` |

The weaver is good. The advice is fine, if repetitive. The pointcut is missing, and its job is done by a bare reference to a configuration object.

That single gap produces everything that feels like a leak:

- Analysis APIs take configuration objects, so analysis looks coupled to configuration. `Analyzer.Trace(mutator, ...)`, `Clock.FromIterations(algorithm)`, `GenealogyAnalyzer(crossovers, mutators, algorithms)`.
- Addressing is by reference equality, so a configuration reused in two places cannot be observed separately, and a record copied with `with` silently loses its address.
- Nothing else can be said. You cannot write "every evaluator", "the root algorithm", "the mutator in stage 2". Only "this exact object".
- The `Analyzer.Trace` overload matrix exists to work around the missing type. The analysis simplification pass reached this conclusion from the usability side and built an anchor type for it, which was later removed; see "The pointcut and the removed anchors" in [container and aspect framing](container-and-aspect-framing.md). Same diagnosis, arrived at from a different direction. I think that agreement is worth something.

So analysis is not violating the layering. It is a legitimate client of the resolution phase, which is the one component whose whole job is knowing how configuration maps to execution. What it lacks is a noun for the thing it points at.

## Part 4: AlgorithmRun

`AlgorithmRun` does three separate jobs.

1. Composition. Holds analyzers and modules while `Preparing`, freezes them on start, rejects late additions.
2. Graph building. Creates the `ResolutionScope`, installs everything, resolves the algorithm instance.
3. Hosting. Owns the `Preparing -> Running -> Paused -> Completed | Canceled | Failed` state machine, holds the `IAsyncEnumerator`, wraps it in `ExecutionStream`, and decides whether cancellation pauses or terminates.

That is one object per phase of the pipeline, fused. You noticed it as "a mixture of all three layers", and I would say the diagnosis is exactly right but the conclusion should not be "find it a layer". It should be "it is three objects".

The fusion has visible costs already:

- Lifecycle transitions are spread across `BeginExecutionSegment`, `CreateStream`, `Track`, `CancelAsync` and three `ExecutionXxx` helpers. `Track` alone sets state in five places including a `finally`.
- The state machine cannot be tested without an algorithm, a problem and a random source.
- `ExperimentRun` needed the same state machine and copied it instead of reusing it.
- The base class is non-generic with `protected` members that the four-parameter derived class must call in a specific undocumented order.

## Part 5: the model I would propose

One stack of layers, cross-cut by two phases. Layers depend downward only. Phases are orthogonal and every layer from L1 up has both faces.

```
L5  Observation and analysis    observations, observing wrappers, traces, clocks, analyzers
L4  Hosting                     run lifecycle, streams, concurrency, experiments
L3  Library content             concrete operators, algorithms, problems, encodings
L2  Resolution                  ResolutionScope, builder, decorations, modules
L1  Composition model           IExecutionConfiguration/Instance, ExecutionSignature, role interfaces
L0  Domain vocabulary           candidate, search space, problem, objective, random, search state, accumulators
```

Three rules make this say something:

- L4 does not depend on L5. A run hosts an execution. It does not know analysis exists. This is the rule that deletes the `AlgorithmRun -> IAnalyzer` edge.
- L4 does not depend on L3. Hosting a run should not require knowing any concrete algorithm.
- L5 attaches through L2 and may read L3. Analysis sits above the library content it wraps, and reaches the graph only through the resolver. It never reaches into hosting.

L5 above L4 may look odd at first, since analysis is attached to a run. But analysis does not depend on the run. It depends on the resolution phase, and the run is only the thing that happens to own the scope. That is why the run can stop knowing about it.

The phases stay orthogonal:

- Describe. Immutable configuration records. Comparable, reusable, inspectable, validatable before anything runs.
- Realize. Run-bound instances with identity and mutable state.

`ResolutionScope` is the one component that is allowed to know both, and that is its definition rather than a leak.

## Part 6: how to get there

Ordered by cost over benefit. Each phase stands alone and is worth doing even if the next never happens.

### Phase 0: write the doctrine down

Add `docs/contributing/architecture/layering.md` with the layer list, the three rules and the phase distinction. Add one guideline under § 3 pointing at it. No code.

This is worth doing first because the later phases are mechanical once the rule exists, and arbitrary without it. § 3.1 already says "describe architecture through responsibilities, not source layout", which is the right principle. It just has not been applied to the top-level shape yet.

### Phase 1: break the two cycles

Implemented with an explicit behavior spec for attachment ordering.

1. Moved the two sinks to `Instrumentation` as `CountAccumulator` and `DurationAccumulator`, removing the operator and algorithm imports of Analysis.
2. Retained `IAnalyzer : IExecutionModule` with no duplicate installation member. `AlgorithmRun.Attach` accepts modules through one ordered list, one reference-identity set and `builder.Install`; the run no longer references Analysis.
   - Experiments own `TrialModule<TAlgorithm, TModule>` and `TrialAttachment<TTrial, TModule>`. The methods are `AttachPerTrial` and `GetAttached`; Analysis retains `TrialAnalyzer.Create` as a convenience. Returned pairs expose `Trial` and `Module`.
   - Attachment order now determines installation order across analyzers and other modules. The behavior specs pass, including reference deduplication and direct analyzer decoration. Configuration-origin precedence is preserved.
3. Move `SearchConfigurationValidation` from `SearchSpaces` to L1 next to `ExecutionSignature`.
4. Kept the clocks in Analysis. Moved `EpochClock`, `EpochWork` and `BestBeforeChangePerformanceAnalyzer` to Experimental's `Analysis/Dynamic` folder and `Analysis` namespace. The remaining dynamic-problem `Observe` call is the doctrine's one allowed exception.
5. Fix the `Objectives -> Encodings.Permutations` edge in `LexicographicComparer`.

### Phase 2: consolidate the assembly and organize Execution

All Contracts source now belongs to the main HeuristicLib assembly, and the Contracts project and package have been removed. Deprecating the published package remains a manual release action. `Execution` contains composition, resolution and hosting types, with ownership enforced by the architecture test.

Run files live under `Execution/Runs`; shared concurrency helpers live under `Execution/Concurrency`. Both retain the `Execution` namespace. `ExperimentRun` remains in `Experiments`. The grouping folders do not create new namespaces.

### Phase 3: characterize lifecycle and retain ownership

The extraction proposal was reconsidered against the actual run semantics. Algorithm runs resume a retained execution; experiment runs coordinate trials and have a terminal lifecycle. Sharing guarded state assignments does not justify a common host.

The committed characterization tests cover preparation, completion, pause/resume, cancellation, failure and experiment early disposal. `AlgorithmRun` keeps lifecycle and enumerator ownership together; `ExperimentRun` keeps scheduling and its lifecycle. The rejected `RunHost` and mandatory composition/continuation extraction are not prerequisites for the next branch.

Two code-inspection findings, competing-stream rejection failing the active run and disposal exceptions leaving an incorrect lifecycle, are recorded as separate focused fixes in [developer backlog](developer-backlog.md).

### Phase 4: give the pointcut a type

Moved to the DI and AOP alignment branch, the second of the two. There the pointcut becomes a type of its own with matching in the style of AspectJ, by type, attribute, wildcard, name and nesting; see [container and aspect framing](container-and-aspect-framing.md). This phase would have made analysis stop feeling like it reaches into configuration, and that is still the goal.

### Phase 5: enforce it

The repo already ships Roslyn analyzers in `analyzers/` for API rules, and `CreateExecutionInstanceAnalyzer` shows the pattern. A namespace layering rule is the same kind of check. Alternatively an architecture test in `HeuristicLib.Tests` asserting the allowed edges, which is cheaper and catches the same regressions at build time.

Without this, phase 0 is a document that decays. `ObservationCounter` did not end up in `Analysis` because anyone decided it should.

## Judgement calls I would not make for you

- The five near-identical `ObservingX` wrappers could collapse behind a generated or reflective mechanism. I would leave them. The duplication is honest and visible, and the alternative adds indirection to the hot path, which § 7.1 and the performance constraint in the design goals both push against. Five files of repetition is a fair price.
- Settled: `IAnalyzer` inherits `IExecutionModule`, and the run uses the module contract. See phase 1 item 2.
- Settled: Contracts was consolidated into the main assembly, including both run types. See phase 2.

## What I would do first

Phase 1 item 1. Moving two 13-line accumulators out of `Analysis` deletes an entire cycle between the operator layer and the analysis layer, touches 25 files mechanically, and risks nothing. It is the highest ratio of structural improvement to effort in this report by a wide margin.

Then phase 1 item 2, because it is the specific edge you noticed. The `DecorationOrigin` hole it would close is already closed, by the run rather than by the type, so the remaining gain is one attachment list and one install path.
