# Core simplification plan

Status: started on 2026-11-05. This file is the one record of which simplifications HeuristicLib will make, in which order, and which decisions have been taken. The [reports](simplification/) and their review are input: nothing in them is accepted unless it is recorded here. The phases and their order are accepted. Phases 0 to 3 are implemented and committed. Phase 4 is implemented, validated and committed, following all three accepted decisions in [round 22](#decided-in-round-22). D61 and D63 are implemented, D62 retains existing behavior, and affected documentation is updated. The fork compatibility inspection records remaining merge work rather than a fork implementation. The phase 4b bounded spike is complete with typed construction hooks retained; The developer accepted the measurements and D5's full migration, which is implemented and validated. Phase 4b changes remain uncommitted for review. All of this work is architectural under `AGENTS.md`, so design shapes marked as proposed or open still need the developer's acceptance.

Baseline: [operator unification step 3](operator-unification-step3-migration.md), complete and committed. [Step 4](operator-state-lifetimes.md#implementation-sequence-confirmed-on-2026-11-04) and its documentation rewrite wait for phase 8, so that they describe the simplified system.

## Goal

Simplification is the primary driver. Complex behavior should follow from a small set of consistent rules, and a new scenario should first be expressed with existing systems rather than answered with a new mechanism.

These stay: the domain model, the configuration/executor split, scopes as the sharing unit, `Sharing`, run-bound results, the operation contracts with generic invocation wrappers, truly problem-specific operators, and the rule that external operations can do everything built-in operations can.

## Decisions taken on 2026-11-05

### Configurations are data

Configuration nodes are pure data records. Storing lambdas or live objects on them is against a fundamental guideline that is held very highly. It is not unbreakable: where a special case has a technical need, such as Python interoperability, a documented exception is acceptable. Allowing no delegates under any circumstances stays rejected.

Termination and interception are fundamental parts of an algorithm's configuration, so they stay in the configuration graph. Attaching them to the state stream afterwards is rejected.

### Automatic placement is substitution in the configuration graph

"Aspects" are no longer something placed somewhere and applied during resolution. A substitution is a convenience that maps one configuration graph to another with ordinary wrappers in place. It is a pure function: its result follows only from the graphs and replacements passed to it, including which references they share, and it changes nothing else. Graph rewriting is the name of the technique, and `Substitute` is the general mechanism. It may use lambdas, predicates and function objects, because they are not stored in a configuration. Its result is an ordinary configuration, which obeys the same data rule as a hand-written one. Resolution has no interception left to handle.

The idea and some terminology of aspect-oriented programming may stay in the documentation, but the implementation is a configuration-to-configuration mapping. This is where the work starts. It reopens the [automatic substitution option set aside on 2026-11-04](operator-state-lifetimes.md#advice-and-scope-layers-under-discussion-since-2026-11-04), and the form recorded under [advice expressed as wrapper configurations](developer-backlog.md#advice-expressed-as-wrapper-configurations) stays different: that recipe ran inside the resolver, while a substitution finishes before resolution starts.

### Other decisions

- **`ResolutionScope` must shrink.** It does too many things and has become a god object.
- **Introspection without reflection.** Configuration graphs become introspectable through a declared parameter system along the lines of the [parameter plan](configuration-parameters-and-experiment-persistence.md). Experiment setup should build on it.
- **String-based configuration is a separate later task.** This branch builds the foundation only. REST APIs and configuration from strings are a [backlog item](developer-backlog.md#runs-execution-and-experiments).
- **Design rules come early.** Agents should express new ideas with existing systems. See phase 0.
- **Concepts are presented deliberately.** The fundamental concepts should be clear and shown to users first.
- **`Substitute` is the general mechanism.** Analyzers and budgets use it under the hood. See [D13](#decided-in-round-5).
- **A substitution happens eagerly.** The configuration the caller holds is what runs, and there is no intent node. See [D1](#decided-in-round-1).
- **Parameters are declared by hand for now.** Reflection is avoided, and the cost of that choice gets its own detailed design once its overhead is known. See [D2](#decided-in-round-1).
- **Sequential substitutions are deterministic.** The output of the first is the input of the second. See [D3](#decided-in-round-1).
- **The adaptive mutation strength is explicit.** It is configuration; its runtime part is a plain object holding the current value, and it is given to the strategy. See [D4](#decided-in-round-1).
- **An analyzer is what an aspect was.** It is a collection of substitutions that places the hooks an analysis needs. See [D22](#decided-in-round-6).
- **`Analyzer` is the name to keep.** `Instrument` retires and keeps its name until the type is removed, and `Fits` becomes `Supports`. See [D13](#decided-in-round-2).
- **A meta-algorithm can give children another problem.** It does so while constructing their executors. See [the conditions](#problem-bound-executor-conditions).

### Idea status

Numbers follow the review of the reports on 2026-11-04. "Accepted" records the developer's statement. "Proposed" is a recommendation awaiting a decision.

| #   | Idea                                                               | Status                                                     | Delivered by      |
| --- | ------------------------------------------------------------------ | ---------------------------------------------------------- | ----------------- |
| 1   | Automatic placement as substitution in the configuration graph     | Accepted; names settled, details open                      | Phases 1 and 3    |
| 2   | Constructing a node never changes the rules for its siblings       | Accepted and applied                                       | End of phase 3    |
| 3   | Scope reduced to executor table, child scopes and sharing          | Accepted direction                                         | Phase 4           |
| 4   | Generic configuration introspection                                | Accepted, declared parameters, no reflection               | Phases 1, 2 and 7 |
| 5   | Problem-bound executors                                            | Accepted; implemented after the measured spike             | Phase 4b, D5      |
| 6   | Replace executor controls                                          | Accepted with an explicit strength node                    | Phase 5           |
| 7   | Persistable description with runtime callbacks attached separately | Dropped, absorbed by idea 1                                | Phase 3           |
| 8   | Instruments separate result creation from placement                | Accepted; `Instrument` retires, `Analyzer` stays           | Phases 3 and 6    |
| 9   | Generic walk instead of hand-written `Fits` child lists            | Accepted; `Fits` becomes `Supports`                        | Phase 2           |
| 10  | Fixed-layout trace samples                                         | Accepted, low priority                                     | Phase 6           |
| 11  | Explicit trial identity                                            | Accepted, low priority                                     | Phase 7           |
| 12  | Design rules such as "express before building"                     | Accepted and applied                                       | Phase 0           |
| 13  | Bounded pilot before any migration                                 | Accepted                                                   | Phase 1           |
| 14  | Observation plus fold as the one analysis model                    | Accepted; details at the start of the phase                | Phase 6           |
| 15  | Smaller glossary, tiered by audience                               | Accepted                                                   | Phase 8           |
| 16  | Optional operation protocol                                        | Dropped: `IConfigurationNode<TExecutor>` already offers it | None              |
| 17  | Move invariant inference out of the central path                   | Dropped                                                    | None              |
| 18  | Prepared activation factories for Cycle turns                      | Dropped unless measured                                    | None              |

Rejected ideas are recorded in the [developer backlog](developer-backlog.md#simplification-proposals-rejected-on-2026-11-05).

## Proposed design shape

Nothing below is implemented. The code shows the names decided under [D13](#decided-in-round-5). On 2026-11-05 the call forms and the parameter declaration were compiled as stubs against the built library; nothing was run.

### Substitution

```csharp
// Explicit placement stays as it is.
var calls = ga.Mutator.Invocations.Count();
var explicitly = ga with { Mutator = ga.Mutator.Counted(calls) };

// The base form: a predicate on a node and a replacement, both plain lambdas. Nothing is stored.
// The type of the lambda's parameter says which nodes are considered.
var main = ga.Substitute(
    (IOperator<Evaluation, RealVector> evaluator) => evaluator is INamedConfiguration { Name: "main" },
    evaluator => evaluator.Timed(duration));

// Common patterns: every node of a type, and one node by reference.
var timed = ga.Substitute((IOperator<Evaluation, RealVector> evaluator) => evaluator.Timed(duration));
var counted = ga.Substitute(ga.Mutator, mutator => mutator.Counted(calls));

// A replacement need not wrap anything.
var swapped = ga.Substitute(ga.Mutator, _ => new GaussianMutator(0.5, 0.1));

// There is one path: the call produces the explicit graph, so tests can compare the two by value.
Debug.Assert(explicitly == counted);

// Provisional syntax (D25): an analyzer knows its own source. The result is still a GeneticAlgorithm, because only inner slots change (see D31 for a source on the receiver itself).
var best = ga.Evaluator.Invocations.TraceBestSoFar();
var traced = ga.AnalyzedBy(best);
var run = traced.CreateRun(problem, random);
await run.CompleteAsync();
var curve = run.Result(best).Snapshot();

// A budget keeps its call site and returns the substituted graph.
var limited = ga.LimitedToInvocations(ga.Mutator, 5);
```

Selection needs no dedicated type. The type of the lambda's parameter replaces `Pointcut.OfType`, and C# expressions replace `Named`, `And`, `Or` and `Not`. Production code only selects by reference today, apart from the budgets' `OfType<IOperator>().Not()` seed, which goes with them in phase 3. This is the core version D14 asks for. Passing a reference such as `ga.Mutator` is a shortcut for matching by reference equality. It adds only the error D17 requires when nothing matches.

One small public pair remains, `Substitution`: a predicate and a replacement with the node type erased. A single call that handles different node types needs it, which D17 requires, and so do analyzers, including external ones. It replaces `Aspect`. `node.Substitute(substitutions)` is the form everything builds on, and the other overloads are shortcuts that create a `Substitution`.

Rules of a substitution:

- Several substitutions apply one after another, each seeing the result of the previous one (D28). `O` with `A` and then `O` with `B` gives `A(B(O))`, because `B` matches `O` and not `A`. A node substituted away no longer matches.
- One substitution visits each node of its input once, children first, and never revisits its own replacement.
- The predicate sees the node as the call received it, and the replacement receives that node with its children already substituted. A target is therefore still found when one of its children changed, as Roslyn's `ReplaceNodes` does it, also when an earlier substitution of the same call changed it (D17).
- A selection by type also matches wrappers from earlier substitutions, on purpose: count two mutators, and a later substitution on every counted mutator finds both. The same rule means that substituting every evaluator with `Timed` and then with `Counted` gives `Counted(Timed(Counted(E)))`, which counts twice unless the predicate excludes the wrapper. `Counted<Mutation, T>` and `Counted<Evaluation, T>` are different types, so a selection across operations takes one substitution per operation, unless D11 settles a dispatch. A counted mutator can become a timed mutator, but not a counted evaluator, which the compiler rejects.
- The receiver can match too. It counts as a slot typed by its static type, so wrapping a `GeneticAlgorithm` receiver is an error under D10, while a receiver typed `IAlgorithm<TCandidate, TSearchState>` can hold the wrapper.
- It is memoized by reference, so a reference used in two slots stays one reference. [D26](#decided-in-round-8) settles how this applies to multi-operation nodes and `Sharing` lists.
- A slot that cannot hold the replacement is reported according to [D10](#decided-in-round-3), never silently changed in type.
- It returns the same instance when nothing was selected, and can report that nothing matched. That was [given up](developer-backlog.md#detecting-observation-sources-that-never-resolve) under advice.
- Substituting within one subtree is how a caller scopes it, which also delivers [caller-path selection](developer-backlog.md#matching-and-advice-extensions): `Cycle(a.AnalyzedBy(trace), b)` observes only A's calls of a shared operator.

The rejected "predicate-plus-factory decorators" differ from this: they were registered in setup closures and applied during resolution, which keeps advice's rules under a new name.

Decision A of step 3 also improves: placing an instrument on inner operators keeps the concrete algorithm type, so a grid over `GeneticAlgorithm<T>` no longer loses it at an `Advised` wrapper.

### Parameters

```csharp
public sealed record TournamentSelector<TCandidate> : Selector<TCandidate>
{
    public int TournamentSize { get; init; }

    public static Parameter<TournamentSelector<TCandidate>, int> TournamentSizeParameter { get; } =
        new(nameof(TournamentSize), static selector => selector.TournamentSize, static (selector, value) => selector with { TournamentSize = value });

    public new static IReadOnlyList<IParameter> Parameters { get; } = [TournamentSizeParameter];

    public override IReadOnlyList<IParameter> GetParameters() => [.. base.GetParameters(), .. Parameters];
}
```

- Each parameter is a static member, and `Parameters` is a static collection of the record's own parameters (D24). All type names here are provisional.
- A walk calls `GetParameters()`, which adds the inherited parameters. C# cannot chain overrides by itself without reflection or a generator, so this one line does it. It cannot name a wrong base, and a record with no parameters of its own writes nothing.
- The property's type fixes the owner and the value, so target-typed `new` types the lambdas. A generic factory such as `Parameter.Setting(...)` cannot infer the owner, so that form does not compile.
- The collection comes after its members, because static initializers run in textual order. The compiler warns about the wrong order (CS8601).
- `GetParameters()` is also how a walk holding an erased node reaches the static collection without reflection. It needs its own name, because a type cannot have a static and an instance member with the same name.
- Properties stay the source of values. Metadata is per type and holds no instance data.
- The declared value type says what a walk does with a parameter, so a walk never asks what kind it is ([D41](#decided-in-round-14), [D45](#decided-in-round-15)).
- Declarations in a base record cover inherited settings, because a record's `with` keeps the runtime type.
- Delegate-valued parameters are declared as such, so a walk can report them instead of pretending to persist them.
- Executors keep typed fields. No lookup by parameter happens during execution.

## Order of work

### Phase 0: record and rules

Applied on 2026-11-05 and not committed yet: this plan, the backlog entries, developer guidelines § 2.5 with a pointer from the `AGENTS.md` architecture checkpoint (D6), and § 4.8 reworded from a temporary exception to the guideline-with-documented-exceptions stated above.

### Phase 1: pilot

A time-boxed pilot of ideas 1 and 4, additive on this branch (D23). It starts only when the developer says so. The old path keeps working and its tests stay green with their assertions unchanged. `LimitedToInvocations` and `LimitedToCandidates` appear in five test files today. A test that pins the old budget imports the `.Obsolete` namespace inside its namespace declaration, which wins extension lookup over outer imports, and new tests establish the behavior of a substitution. The pilot contains the core `Substitute` call, hand-written parameter declarations only on the nodes its graphs use, counting and observation wrappers implemented without advice classes, one operator budget, and analyzers installed through their substitutions. The analyzer's user-facing syntax stays provisional (D25).

Exit: the [evidence](#evidence-required) is collected and the developer decides. If it fails, the fallback is to revert the pilot, finish step 4 as planned, merge, and keep only the independent items: phase 0 and ideas 10, 11 and 15.

### Phase 2: parameter contract everywhere

Declarations on every configuration node in core, Experimental and PythonInterop, about 240 records by a rough count. A check for forgotten declarations is [out of scope for this branch](#decided-in-round-19). First consumers: the reflection walk in `SearchConfigurationValidation` and the hand-written `Fits` child lists (idea 9; `Fits(` appears in 26 source files), with no behavior change beyond [D47](#decided-in-round-19). `Fits` is renamed to `Supports` here, because its overrides are rewritten anyway. Composites that run children under other types state the execution of the child on its slot ([D48](#decided-in-round-19)). `CycleAlgorithm` and `PipelineAlgorithm` also lose `TAlgorithm` here (D10).

Progress on 2026-11-06: `TAlgorithm` is gone, and every record with a stored property in the three libraries declares its parameters. Three notes:

- `OperatorCounter.Metric` stays undeclared, because the old counter is a class with no `with` and goes in phase 3 ([D52](#decided-in-round-21)).
- The old budgets declare their targets as reference lists, because they name operators that live elsewhere in the graph, and keep their hand-written check until phase 3. The single target is a one-entry list, which saves a new parameter class.
- `Neighborhood` gains a virtual `GetParameters`, like `Operator` and `Algorithm`, and racing's `StateMerger` gains `init`.

The consumers and the rename followed on the same day. `SearchConfigurationValidation` walks declared parameters, `ExecutionSignature.Supports(node)` replaces the child lists, and `Fits` is gone from code and docs. Seven records state a forward: the three composite operators of `CompositeSearchSpace`, racing, `ReevaluationInterceptor`, and the two PythonInterop records that resolve `ChildRefiner`. A throwaway check over the sources finds only `OperatorCounter.Metric` undeclared, so forgotten declarations are absent today, though nothing guards them after this branch.

Differences beyond D47, each expected:

- A child that a configuration does not declare is a leaf to validation, where the reflection walk followed any property. This applies to external configurations, so the guide now says to declare children, and its `SingleCreateAlgorithm` does.
- Diagnostics follow declaration order, inherited parameters first, rather than the order reflection gave. A node reached twice is therefore reported under whichever path comes first in that order.
- A reference list entry is still visited for invariants and blame, but `Supports` does not ask it. The old budgets keep their hand-written check for their targets until phase 3.

Other changes the plan did not name: `ISlotRewriter.Reference` gains the entry's name, so validation can report its path.

A review of the staged change by a second agent, on the same day, confirmed two defects and found none in the other areas it covered. Both are fixed:

- `ReevaluationInterceptor` was pass-through on the reasoning that an evaluator has no state check, but a consumer's evaluator can have one. It resolves under `ISearchState`, so an evaluator supporting only that was falsely rejected. It now states the forward, and a scan of the sources found no other unstated resolve.
- `PredefinedCandidatesCreator` threw at `GetParameters()` when its candidate is itself a configuration, because `Parameter` decides on the closed type and rejected `ValueArray<TCandidate>` as a list of configurations. Candidates are data, so the parameter is now a `SettingParameter` ([D50](#decided-in-round-20)).

The test for the first defect showed a third problem the review did not report. Since D47, a resolve that failed on a child blamed the parent, saying the parent "is written for its declared signature" without naming the child, where the child's own resolve used to name it. An operator that is itself written for the run now reports through `SearchConfigurationValidation.Mismatch`, as algorithms already do. An operator that is not keeps its existing message.

Cost, from a throwaway Stopwatch program against this working tree and commit `8603b3ef`, medians of 9 rounds across three process runs on one machine. It is not in the repository. Allocations count the measuring thread only.

| Measured                             | Before        | After           | Allocated before | Allocated after |
| ------------------------------------ | ------------- | --------------- | ---------------- | --------------- |
| The question over one GA             | 125 to 128 ns | 222 to 242 ns   | 0 B              | 1,088 B         |
| The question over a cycle of two GAs | 65 to 109 ns  | 501 to 551 ns   | 40 B             | 2,384 B         |
| Resolving one GA in a new scope      | 5.1 to 5.2 µs | 3.3 µs          | 3,944 B          | 5,512 B         |
| Validating one GA                    | 2.3 to 2.4 µs | 0.6 to 0.7 µs   | 2,530 B          | 4,360 B         |
| Completing a two candidate GA        | 13.4 to 18 µs | 11.5 to 12.9 µs | 8.8 to 9.2 KB    | 12.2 to 12.5 KB |
| Completing a cycle of 1,000 stages   | 5.2 to 6.6 ms | 5.5 to 5.6 ms   | not comparable   | not comparable  |

The walk makes the question slower and allocating, because `GetParameters()` builds a list on each call, and it is asked once per resolve. A complete run is not slower, even when a cycle resolves its stages again on every turn, because the question stays small next to a resolve and validation no longer uses reflection. No cache is needed for now. The resolve row is not a gain from this phase alone: its baseline ranged widely across rounds.

### Phase 3: replace interception

Delivers ideas 1, 2, 7 and 8. [Round 21](#decided-in-round-21) holds every decision the steps rely on. Each step is one commit and leaves the solution green. A step adds its new path, migrates the callers in `src`, tests and documentation, and deletes whatever has just lost its last caller. Obsolete code therefore lives as briefly as possible, and no step is only a deletion. These are also the concrete steps of idea 2:

1. **Analyzers (D25, D36, D57).** `AnalyzedBy` and the read check arrive, and every `Advised(instrument)` caller moves. `Observing` and the placement of role sources (D54) arrive here rather than in step 4, because deleting `Advised(Instrument)` removes the only path PythonInterop's callback and traces over a role source had. `Advised(Instrument)`, `Instrument.CreateAspect`, `RunResults.Aspect` and the aspect in `IClock.Bind` are deleted. Tests pinning the reach differences of D1 and D3 are rewritten here.
2. **Counters and durations (D52, D53, D56, D59).** `Count()`, `CountCandidates()` and `Duration()` return the `Counter` and `Duration` nodes, substitution bundles arrive, and `TimedAlgorithm` arrives with `Timed`, `Timing(ga.Iterations, out duration)` and `ga.Iterations.Duration()`. `OperatorCounter`, `AlgorithmCounter`, `ObservationCounter`, the old `Duration` and `OperatorDuration`, D34's adapter, `[OverloadResolutionPriority]` and `DurationMeasuringAlgorithmAdvice` are deleted. Callers move in the same commit, so no adapter is needed for the overlap. The old budgets are unaffected, because they use only the accumulators. `LayerDependencyTests.cs:30,437` and `layering.md` follow D53. Done on 2026-11-10, with three changes the plan did not name: an explicit `Counted` states its metric, because the counter no longer carries one, so a candidate counter placed by hand needs `OperatorCountMetric.Candidates`; `ExplicitPlacement` and the algorithm `Advise` helpers lost their last callers and went; and `Timing` on operators waits for D11 in step 3.
3. **Budgets (D8, D11, D60).** Budgets stop calling `scope.AddAspect` (`OperatorCountBudgetAlgorithm.cs:59,123` in `.Obsolete`, `OperatorDurationBudgetAlgorithm.cs`). The old budgets, the duration advice and the temporary `.Obsolete` namespaces (D23) are deleted. Done on 2026-11-10. The inventory found one gap, settled as [D60](#decided-in-round-21). Changes the plan did not name:
   - `OperatorDurationBudgetAlgorithm` holds a `Duration` node in the form of the count budget, and the typed single-operation budget records are gone, because the general record holds its wrappers. `Counting` and `Timing` into an existing node exist for algorithm sources too, so a placement can also count or time `g.Iterations`. The single-operator `LimitedToDuration` takes an optional time provider instead of two overloads.
   - The internal and public duration advice went, and its 27 test probes moved to `Timed`, dropping only the assertions on the advice object itself. The counting advice keeps its probes until step 5, because the plan names only the duration advice here.
   - Changed assertions, each following D1, D9 or D58. `Budget_CountsOnlyTheCallsOfTheAlgorithmItHolds` replaces the test that a budget counts a direct request in its scope and child scopes, and that building the original first is an error. `AlgorithmWrappers_ResolveInTheScopeOfWhatTheyWrap` requests `budget.Algorithm` rather than the original. `ADurationBudget_WrapsTheOperatorRatherThanTheAnalyzerObservingIt` analyzes first with `Observing`. In `OperatorBudget_RebindingKeepsClockBeforeTraceAndExcludesObservationDuration`, the sibling scope's advice no longer advances the clock, because scope advice sits inside the budget's `Timed`; the trace's own advance still shows the exclusion. `TimedMutator_NewExecutorsShareOnlyTheDurationOfTheirRun` gives an independent scope its own duration, where the advice shared the supplied accumulator. The two multi-target tests that listed the mutator twice name it once, because a repeated `Counting` counts twice, and their counts are unchanged. `AdviceBelowTheSharingPoint_DoesNotReachInsideTheSharedExecutor` and an exact duplicate of `SharedOperator_StillReceivesEachRequestersBudget` were deleted; `SubstitutedBudgetTests` holds their substituted counterparts.
   - D58's count: budget-then-analyze occurs three times, all in tests, two of them on purpose and one through advice. Nothing in `src`, samples or documentation does it.
   - The phase 1 inconsistencies on `operator-composition.md:241` and the seven IDE0065 findings went with the `.Obsolete` namespace.
4. **Racing and the remaining observation paths (D3, D51, D54, D55).** `ApplyingIterationUpdates` arrives and racing is ported. The advice-factory form of `ObservationSource.Create`, `CreateIterationUpdateAspect`, `Aspect.Observe` and racing's contender advice are deleted. [The migration notes](#migration-notes-for-origindynamic-racing) go to the racing branch before this commit; [racing-migration-notes.md](racing-migration-notes.md) holds them for the branch, with code. Done on 2026-11-10. The inventory found no gap, and implementation found one, settled as D51's amendment. Changes the plan did not name:
   - `Observing` moved from `Analysis` to `Inspection` as `ObservationCallbacks`, because racing is content and content must not reference `Analysis`. Racing calls `Substitute`, since `AnalyzedBy` stays in `Analysis`. `layering.md` and § 4.8 name `Observing`.
   - `ApplyingIterationUpdates()` returns `IterationUpdatingAlgorithm<TCandidate, TSearchState>`, a `WrappingAlgorithm` in `Problems.Dynamic`. `DynamicProblem` implements an internal `IIterationUpdatable` beside `IUpdateRequestable`, so `Supports` checks the problem type without reflection and the wrapper ends an iteration without making `ApplyPendingUpdates` public.
   - Racing at HEAD places no iteration updates. The test of per-contender iteration updates therefore wraps each contender in `AlgBuilder`, and its `EvaluatorSelector` returns the evaluator inside the wrapper, which the tracker's substitution reaches. Under `AfterEachIteration`, racing whose contenders are not wrapped never ends its epoch, at HEAD as before.
   - An `EvaluatorSelector` returning an evaluator the contender does not hold now fails with D17's error, where the advice silently tracked nothing.
   - A mismatch on an observed operator names the `Observed` wrapper and blames the operator below it, without what the operator was written for, because the wrapper is now the resolved root.
   - Deleted, because their subject was advice: `AnAnalyzerWrappingDirectly_ObservesInTheOrderItWasAttached` and `ModulesAndAnalyzers_ObserveInAttachmentOrder`, both on the attachment order of aspects, which callbacks under D28 do not have, and `ASourceFromAnAdviceFactory_ReportsThatItCannotBePlaced`, whose source is gone.
   - Changed assertions, each following D1, D3 or the wrapper root. `AlgorithmObservation_NumbersEachCallAndPreservesThePausedInvocationContext` loses the four assertions on observations a child scope added, because a child scope builds the graph it is given. The ten `*Observation_RebindsItsPredecessorAndPreserves*` tests are now `*Observation_Preserves*` and assert `["operation", "observed"]` for the observed graph instead of the order of parent and child advice. `PredefinedAspectSpecs` checks that each of the five observations changes the graph it is placed in, instead of `Pointcut.Matches`. `AMismatch_NamesGenericTypesWithTheirTypeArguments` resolves the operator without an observation, because the observed form names the wrapper; `AnObservationWrittenForANarrowerProblem_IsReportedWhenTheGraphIsResolved` keeps the observed form. Renamed with assertions unchanged: `Cycle_SharedExecutorsKeepTheirChildrenAndTheObservationOfTheirGraph`, `EachScopeKeepsItsExecutorsAndOnlyTheObservedGraphReports` and `AfterEachIterationPolicy_UpdatesAtTheIterationBoundaryOfTheWrappedAlgorithm`. `NewExecutorStartsIndependentBurnInWhilePausedExecutorKeepsProgress` places the evaluator observation in `AlgBuilder` (D3), with assertions unchanged.
   - New tests: `AContendersTracker_SpansItsRestarts`, `ContendersApplyingIterationUpdates_EndTheRacesEpoch`, `TheBuilder_ReceivesTheAlgorithmItReturnedWithoutTheTracker`, and for the wrapper `IterationUpdates_AreAppliedBeforeTheStateIsYielded`, `IterationUpdates_AreNotAppliedAfterTheStreamIsDisposed` and `IterationUpdates_SupportOnlyDynamicProblems`.
   - D58's count: budget-then-analyze occurs three times, all in tests: twice in `OperatorCountBudgetAlgorithmTests` and once in the sibling probe of `DeferredAlgorithmResolutionTests`. The first of these and the probe were advice before this step. Nothing in `src`, samples or documentation does it.
   - `CountingOperatorAdvice` and `AddAspect` keep their probes until step 5.
5. **Custom advice (D56).** Each advice base is checked for a wrapper equivalent open to external authors, and the remaining users move to wrappers or bundled substitutions. `AddAspect`, the scope's aspect list, `AdvisedAlgorithm`, the `Advice` family that wraps executors, the adder exemption, the advice guards and ordering rules, `Pointcut` and `Aspect` are deleted. The rule then holds by construction and is added to the developer guidelines. Done on 2026-11-10. The inventory found no gap. Changes the plan did not name:
   - Every base has an equivalent open to external authors. `OperatorAdvice` becomes a `WrappingOperator<TOperation, TCandidate>` generic in its operation, which passes an `IInvocationWrapper` to `OperationInfo.For(...).Wrap`. `OperatorAdvice<TOperation, TCandidate>` and the role bases become a `WrappingOperator` for one operation that wraps the typed executor; the general form also covers termination and interception. `AlgorithmAdvice` becomes `WrappingAlgorithm`, and `Advice<TConfiguration, TExecutor>` a wrapper that implements the role and declares its child. `Check` goes where its requirement belongs: a requirement of the operation into the constructor, a requirement of the run into `Supports`, which rejects before anything is built and leaves the scope usable, and anything else into `CreateExecutor` before the child is resolved, as `Counted` does. Two differences remain, and neither blocks an external author. A `Supports` rejection carries no message of the wrapper's own, because `SupportedExecutionDescription` is internal. A check in `CreateExecutor` ends the scope tree, where a failed `Check` left it usable.
   - `CountingAlgorithmAdvice`, which counted completed algorithm calls, went with the family and has no built-in successor; `Calls` counts started calls. Its one test probe uses a test-local wrapper.
   - Pages and specs renamed with their subject: `writing-advice.md` is `writing-wrappers.md`, `CustomAdviceDocumentationSpecs` is `CustomWrapperDocumentationSpecs` and `PointcutSpecs` is `SelectionSpecs`. `PredefinedAspectSpecs` is `PredefinedAnalyzerSpecs`, and its test `PredefinedAnalyzersInferEveryRoleFromTheirArguments`. The glossary also marks join point legacy, because it named only the executor that advice wrapped. The sections of `execution-resolution.md` on pointcuts and on advice ordering became one section on substitution and ordering. The README's quick start called `Advised(quality)`, which has not compiled since step 1, and now calls `AnalyzedBy`.
   - Developer guidelines § 4.3 states the rule, with `Share` as the one scope change during construction, which changes only lifetime (D12). § 4.7, § 8.11 and § 8.12 lose their advice examples.
   - Changed assertions, each following D1, D3 or one executor per configuration and scope:
     - `AnEvaluatorObservation_ReportsTheObservedEvaluatorAndComposesWithABudget` reports `Counted(E)` rather than `E`, because the counter now sits closer to the evaluator.
     - Counters held by a run root count per root, as `TimedMutator` does since step 3. `CachingEvaluator_UsesIndependentCachePerExecution` reads 1 for each root instead of 2 on a shared accumulator. `LimitEvaluator_UsesIndependentCounterPerExecution` reads 2 and 1 instead of 3. `CountingMutator_NewExecutorsShareOnlyTheCounterOfTheirRun`, renamed from `..._ShareOnlyTheSuppliedCounter`, counts 1 for the independent root where the shared accumulator read 5 and 8; the parent stays at 4 and 7.
     - Selection by type records configurations, not activations. `RoleSelection_CountsTheConfiguredCompositeAndEachOfItsChildren` records `[small, large, copy]`, children first, where advice recorded `[mutator, small, large]`. `RoleSelection_ReachesTheMutatorsOfTheAlgorithmsACycleHolds`, renamed from `..._ReachesMutatorsOfAlgorithmsResolvedLaterByCycle`, records each mutator once where advice recorded each activation.
     - A repeated resolution in one scope returns the same executor. `GaussianMutator_ReconstructionHasIndependentStrengthAndObservesOnlyContextualCalls` and `CompositeTargetWrapperWrapsTheOneExecutorOfItsScope`, renamed from `CompositeTargetAdviceWrapsEachRequest...`, assert `ShouldBeSameAs` where advice wrapped each request anew; the latter drops its wrap count. `MixedChildrenUseTheSameAncestryAndWrappersInBothDirections` adds that assertion and drops `log.Wraps`.
     - Assertions on the advice object move to the node: the counter and metric in `CountEvaluatorCalls_IncrementsOncePerEvaluateCall` and seven `Count*Calls_IncrementsOncePer*` tests. `InspectionWrappers_CanBeConstructedDirectlyOrThroughInstrumentSources`, renamed from `InspectionAdvice_...`, checks `ChildOperator` instead of `Pointcut.Matches`.
     - Pointcut checks become predicates or references in `NamedOperatorTests` and `OperatorOwnershipTests`. The latter loses the lines that rejected advice for the wrong role, since no advice exists to reject.
     - `Refine_WithItsOwnEvaluator_KeepsComparisonEvaluationsOutOfTheBudget` gets D17's error for counting an evaluator the refiner does not hold, where the advice counted 0.
     - `OperatorLifecycleTests.OperationsAndViews` counts wrapper creations. `TimingOrder` nests the timing explicitly inside or outside the probe, with its ticks of 17 and 22 unchanged.
     - `InvocationWrappersBindOncePerExecutorAndRetainTheirOwnChildWrappers` moves the `Check` hook to wrapper creation. In `CandidateOperationAlgorithmAndSubstitutionBoundariesAreCheckedByTheCompiler`, the row `Aspect.Create(OfType<mutator>, evaluationAdvice)`, CS0311, becomes `Substitution.Of<IOperator<Mutation, RealVector>>(mutator => evaluation)`, CS1662.
     - `ATwoStateAlgorithm_IsBuiltForOneOfItsStatesInAScope` drops the advice's wrap counts. `MoveOperators_AreWrappedByWrappersOfTheirOwnOperation` reads its counts through the root's results.
     - `NewScope_ConstructsPrivateFieldsAndSharesTheDataOfTheConfiguration` keeps the shared count on the wrapper configuration and drops `wrapped [source, source]`. `ChildFirstCollision_TheCompositeInTheChildUsesTheChildsLeafAndItsControl` drops a probe that a child scope placed on the leaf (D3).
     - `LongLivedWrapper_DoesNotRetainDiscardedExecutorData` and `LongLivedState_DoesNotRetainDiscardedChildScopesOrWrappers` become facts that hold weak references to wrappers instead of aspects. `DeferredExecutionRetainsItsScopeButNotTheDataOfDiscardedChildScopes` holds the wrapper configuration that the deferred execution resolves later.
     - `ASharedConfiguration_IsBuiltOnceInAChildScopeOfItsOwn` names `composite[leaf]` instead of `composite[root(leaf)]`, because root-scope advice no longer reaches inside the shared executor (D3).
     - `AlgorithmWrapperSelectedByType_CountsTheResolutionsOfEveryAlgorithm` replaces `Advised_DoesNotSelectItself`, with its count of 3 unchanged.
     - `SemanticCheck_AllowsPublicExecutionTypesInHosting` probes a `Substitution`, and the three location rows for aspect types went.
     - `EachExecutorOfTheWrapperNumbersItsOwnCalls` expects `[1, 2, 1, 1]` instead of `[1, 2, 3]`, because the count lives in the executor. `ATypeSelectionCountsEverySelectedConfiguration` reads 8 through the run, and a predicate substitution replaces the `SmallMutations` pointcut.
   - Renamed with assertions unchanged: `ResolutionFailure_IsTerminal`, `ChildResolvingTheParentsWrapper_ReconstructsTheWrappedExecution`, whose three companion theories now resolve a wrapper for `wrapped`, `ExternalOperationAndRetryAreReachedBySubstitutionAndForwardControls`, `OneSubstitutionCanCountEveryConfiguredMutatorInAnAlgorithm`, `ConsumerDefinedRolesCanBeWrappedDirectly`, `AWrapperLogsTheMutatorItWrapsInARun`, `AGeneralWrapperWrapsEveryOperationItIsPlacedOn`, `AnalyzersCountTimeAndObserveAnOperator`, `CountersCountTheChoiceOncePerCallAndOnlyTheChosenChild`, `AGeneralWrapper_WrapsTerminationAndInterception` and `NamedAlgorithm_IsSelectedByItsNameAndWrappedAsAWhole`.
   - Deleted, because their subject was advice, 81 tests:
     - `OperatorAdviceTests` (8), `PointcutTests` (18), `TypedRoleAdviceTests` (one theory) and `ExecutorResolutionTests.Selection.cs` (7);
     - nine more in `ExecutorResolutionTests`, on ordering, generated wrappers, unmatched references, advice failure, advice instances and installation;
     - 26 in `ResolutionScopeTests`, on advice ordering, the advice guards, role advice and advised chains;
     - eight pointcut specs in `SelectionSpecs`;
     - `Advised_AddingAdviceForAnotherResolvedNode_IsAnError`, the adder exemption, `InstallationFailure_LeavesCollectedDataAvailable`, whose scenario `ResolutionFailure_LeavesCollectedDataAvailable` keeps, `TheSameAspectAddedTwice_ChecksItsAdviceOnce` and `AdviceWrappingAcceptsConcreteDependenciesInsteadOfResolutionContexts`.

     The fixtures `CountingAdvice.cs` and `InspectionAdvice.cs` went with them. D29's rule stays pinned by `SharingOnceTheScopeHasChildScopes_WorksUntilAScopeBelowResolvedTheConfiguration`; the deleted `ChangingAScopeThatHasChildScopes_IsAnError` asserted only the guard of `AddAspect`.

   - Probe-only tests in 35 files were ported with their assertions unchanged. Their probes became `Counted`, `Counting` or test-local wrappers.
   - New tests: none.
   - D58's count: budget-then-analyze still occurs three times, all in tests: twice in `OperatorCountBudgetAlgorithmTests` and once in the sibling probe of `DeferredAlgorithmResolutionTests`. Three other probes in that file were scope advice on a budget and are now `budget.Counting(algorithm.Mutator, out var calls)`, which works because the mutator's reference survives the budget. With `SubstitutedBudgetTests.cs:379`, budget-then-`Counting` occurs four times, all in tests. Nothing in `src`, samples or documentation does either.
   - Selection patterns for D14 and D20, counted outside `SubstitutionTests`, which tests the forms themselves:
     - By reference, 34 times. Thirteen of these are in `src`, which selects only by reference.
     - By the lambda's parameter type, 7 times, all in tests and documentation specs. One more call passes an explicit type argument to `Substitution.Of<IAlgorithm<...>>(replace)`, which compiles because it is not an extension.
     - By a predicate, 6 times: by name twice, in `NamedOperatorTests`; by a property once, the documentation's `MutationStrength: < 1.0`; accepting every node in order to record what is offered twice; and by reference where the target may be missing once. That last one is a helper in `ConcreteOperatorResolutionTests` used for seven targets, because a reference target the configuration does not hold is D17's error.

     No call site tries `.Substitute<T>(...)`. No pattern recurs in production code, so no shortcut is built. Selection by name and a reference that may be missing are the candidates if users repeat them.

   - Decided after review on 2026-11-10. Racing applies iteration updates to each contender itself, in its tracked graph. `Contenders_EndTheRacesEpochWhetherOrNotTheBuilderAppliesIterationUpdates` replaces `ContendersApplyingIterationUpdates_EndTheRacesEpoch` as a theory with its assertions unchanged, so a contender the builder also wraps is covered. The `PredefinedAspectSpecs` rename and the checkpoint wording in `AGENTS.md` follow the removal of advice. A public description of what a node is written for, a completed-call counter and a tolerant reference substitution stay rejected; [the backlog](developer-backlog.md#discussed-tried-and-rejected) records why.

Inventory on 2026-11-08: `.Advised(` appears 126 times in 31 test files, twice in `src` and once in `samples/TravelingSalesman/Program.cs:17`, and advice terms appear on 15 documentation pages. PythonInterop's `AttachTo` returns `AdvisedAlgorithm` (`PythonGenealogyAnalysis.cs`), and developer guidelines § 4.8 names `Advised`.

Test policy: tests whose subject is advice itself are deleted, keeping any scenario still needed. Tests that use advice only as a probe are ported with their assertions unchanged. Tests pinning the D1 and D3 reach differences are rewritten. Every changed assertion is listed in the report.

Documentation policy: this phase fixes only what would become wrong. `writing-advice.md` becomes a page on writing wrappers, and the glossary marks aspect, advice and pointcut as legacy terms. The tiered rewrite stays in phase 8. Selection shortcuts (D14, D20) are not built up front: count the patterns after migrating.

The `AddAspect` sites in `src` are all production callers. Idea 2 is therefore the end of this phase, not an independent quick win: budgets cannot stop adding advice before something else places their counters.

`scope.Share` remains as a scope change made during construction, with its order guard (D12).

### Phase 4: reduce the scope

At the start of phase 4, `ResolutionScope` holds the executor table, the child scope tree, the sharing set and its guard, the construction lock and recursion set, the failure state, the result table reference, root detection for elapsed time, and signature checks. Phase 3 already removed the aspect list and its guards.

- The failure state stays tree-wide for now, after reassessment under D62. Published results remain readable after failure.
- `AlgorithmRun` takes over elapsed-time measurement. Root detection and `ElapsedTimeExecutor` are removed from the scope; direct scope execution leaves elapsed time at zero (D63).
- Signature checking already uses the generic walk of phase 2. It stays at each typed `Resolve`, while exception selection moves to `SearchConfigurationValidation` (D61). `TryResolve` stays public.

What remains is the executor table, the child scope tree and sharing, plus the result table reference, construction lock and recursion set, and tree-wide failure state. Typed scope views and executor signatures stay until D5 is decided.

Implementation on 2026-11-11: D61 centralizes exception selection in `SearchConfigurationValidation.RequireSupport` and retains each typed resolution's graph check. D62 retains tree-wide construction failure, readable results and usable scopes after pre-construction rejection. D63 moves elapsed measurement around each `AlgorithmRun` iterator advance in `try`/`finally`, including the final advance, failures and cancellation. `RunResults` keeps a single active-call stopwatch for live reads. Resolution returns its cached executor directly; root detection and `ElapsedTimeExecutor` are deleted. The internal `TimeProvider` seams remain on scope creation and run construction.

`RunResultsTests` migrates elapsed and elapsed-clock coverage to deterministic runs, covering initial executor construction and consumer-time exclusion, normal completion, pause/resume after disposal or stream cancellation, live reads during an awaited call, failure and terminal cancellation. The three tests for below-root exclusion, overlapping direct-scope calls and hidden-wrapper control forwarding are removed. A replacement test explicitly pins zero elapsed and zero elapsed-clock readings for direct scope execution, the same executor on repeated root resolution, and the `Timed(duration)` alternative. The Cycle reuse test and algorithm authoring spec exercise the root directly without their former below-root workaround. XML documentation, the analyzer and resolution architecture pages, the observation guide and glossary describe run-owned timing and the direct-scope consequence. No problem binding, controls redesign or fork mechanism is implemented. D5 remains phase 4b.

Review follow-up on 2026-11-11: corrected the broad claim that elapsed time excludes all construction. XML documentation, the architecture pages, observation guide and glossary specify that executor construction before the first iterator advance is excluded, while deferred child construction during an advance (for example, a fresh Cycle turn) is included. This clarifies D63's existing measurement boundary without changing execution behavior. Follow-up validation passed: non-incremental Release solution build (0 errors; existing warnings remain), VitePress build, whitespace verification, Prettier on the five changed Markdown files and diff whitespace check. Tests were not rerun for this documentation-only correction; the implementation validation below remains applicable.

Fresh validation on 2026-11-11, using SDK `10.0.402` selected by `global.json` (`10.0.100` with `latestFeature` roll-forward), with .NET commands run sequentially:

| Check                                                                                                                                   | Result                                                                                                                               |
| --------------------------------------------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------ |
| `dotnet restore`                                                                                                                        | Passed after retry outside the sandbox; the sandbox attempt failed on NuGet TLS credentials.                                         |
| `dotnet build --configuration Release --no-restore`                                                                                     | Passed, 0 errors; existing analyzer/compiler warnings remain.                                                                        |
| Focused core tests (`RunResultsTests`, `AlgorithmRunTests`, `ResolutionScopeTests`, `ConfigurationSharingTests`, `CycleAlgorithmTests`) | 75 passed, 0 failed or skipped.                                                                                                      |
| Complete `HeuristicLib.Tests` project, Release, no restore                                                                              | 2,816 passed, 0 failed or skipped.                                                                                                   |
| `HeuristicLib.Tests.ApiUsageSpecs`, Release, no restore                                                                                 | 220 passed, 0 failed or skipped.                                                                                                     |
| `HeuristicLib.Tests.Docs`, Release, no restore                                                                                          | 19 passed, 0 failed or skipped.                                                                                                      |
| `dotnet test --solution ./HEAL.HeuristicLib.slnx --configuration Release --no-restore --max-parallel-test-modules 1`                    | 3,344 passed, 0 failed or skipped, across core, Experimental, scenarios, API usage and documentation.                                |
| `npm run docs:build`                                                                                                                    | Passed after retry outside the sandbox; the sandbox attempt could not replace an existing generated `docs/.vitepress/dist/404.html`. |
| `dotnet format whitespace ./HEAL.HeuristicLib.slnx --verify-no-changes --no-restore`                                                    | Passed after normalizing edited C# files to LF.                                                                                      |
| `dotnet format style ./HEAL.HeuristicLib.slnx --verify-no-changes --no-restore --severity warn`                                         | Passed.                                                                                                                              |
| `dotnet format analyzers ./HEAL.HeuristicLib.slnx --verify-no-changes --no-restore --severity error`                                    | Passed.                                                                                                                              |
| Prettier check of all five changed Markdown files                                                                                       | Passed.                                                                                                                              |
| `git diff HEAD --check`                                                                                                                 | Passed.                                                                                                                              |
| `dotnet pack --configuration Release --no-restore`                                                                                      | Passed; core, Experimental and PythonInterop packages created.                                                                       |

The format checks report a workspace-loading warning; diagnostic inspection identifies a project reference to `HeuristicLib.Analyzers.csproj` without a matching metadata reference. All checks exit successfully; the diagnostic whitespace run loads all projects and reports 0 of 1,105 files formatted. No warning suppression or unrelated cleanup was added. The preceding thread's 2,814 core tests predate this implementation and are superseded by the fresh results above.

Phase 4 is complete and committed. Forking remains source-inspected only, with the merge risks below; problem binding remains phase 4b, and controls and observation redesign remain later phases.

The reduced scope must not rule out scope forking on `origin/dynamic-Racing`. Inspection confirms that moving timing to the run adds no snapshot or context-rebinding dependency to the scope, but copying current executor-table entries alone does not reproduce that branch's behavior. The concrete integration risks are recorded in the [migration notes](#phase-4-fork-compatibility-inspection).

### Phase 4b: problem-binding spike

Accepted on 2026-11-05 as the way to settle D5. It ports mutation, racing, the composite search space, the inner-problem refiners and `DynamicRelativeQualityEvaluator` to executors bound to a problem at construction, adds a small Traveling Thief decomposition as a test, and runs benchmarks. The developer then decides whether the migration goes ahead, before phase 5 writes new executors. See [the conditions](#problem-bound-executor-conditions).

#### Phase 4b findings after the typed-hook checkpoint

**Status: bounded spike and accepted D5 migration complete; validated, uncommitted.** The developer accepted the measurements and authorized proceeding on 2026-11-11. Retain generic typed construction hooks, add the problem object to their scope view, and reduce invocation/executor types. D18 and the fixed search-space/objective rule stay settled. The spike findings below describe the pre-migration baseline; production migration and its validation are recorded separately below.

The spike was an isolated executable outside the solution, discarded after the migration was accepted. It source-compiled the three current libraries, replacing only `ResolutionScope.cs` with a copy using a pair key; bound contracts and ports were internal experiment files. A separate baseline project referenced the actual unmodified core assembly. There was no build-time source generation or bridge to the old invocation contracts.

**Existing mechanisms first.** HEAD already expresses decomposition by constructing tour/packing problem objects, resolving children under their declared signatures, and passing each problem on calls. Composite operators do this with cached `NoProblem` objects; racing constructs meta operators and fresh contender activations; PythonInterop's wrappers directly call the numeric refiner with `InnerProblem`. Binding moves the problem argument from invocation to construction. It requires no new scheduling, activation, sharing or result mechanism.

**Accepted construction shape and migration details:**

- Keep generic candidate/search-space/problem/search-state construction typing. Problem-specific leaf authors receive a typed problem through their scope; compatibility reconciliation stays in the authoring base. Agnostic compositions still override a run-generic hook. The replaced erased hook and author-written problem casts are not retained.
- Invocation contracts shrink: mutation, creation, crossover and evaluation name the candidate only; algorithm executors name their returned state. RNGs, inputs, initial state and cancellation remain invocation inputs. The stateless helper preserves one typed author override and allocates a small bound executor. This removes executor-interface reconciliation and invocation plumbing, **not** the authoring ladder or every typed scope.
- `ProblemScope<TCandidate, TSearchSpace, TProblem, TSearchState>` and `IProblemConfiguration<TCandidate, TExecutor>` are internal isolation seams. A migration should change the existing operator/algorithm hooks and descriptors rather than ship another public configuration family. This experiment's fixed executor type does not justify deleting operation membership checks from the real resolver.
- `ForProblem<TCandidate, TSearchSpace, TProblem>(problem)` deliberately uses explicit arguments. C# cannot infer candidate/search-space arguments from constraints on a concrete problem type. A four-argument form preserves a child's search state. The shorter `scope.For(problem)` remains illustrative; no inference witness is proposed.
- The dynamic-relative evaluator gets its problem from construction. The accepted migration removes its redundant `SourceProblem` setting and factory argument; its provider remains configuration, with one cached reference per bound executor/epoch.

**Resolver and lifetime evidence:**

| Concern               | Executed evidence                                                                                                                                                                                                                                                                                                  |
| --------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| Identity              | Bound entries key on configuration/problem references; plain entries use configuration/null. Equal but distinct records stay distinct. Supported views with different static problem types reuse the same binding for the same references.                                                                         |
| Scope views           | A view holds the same native scope/`Node`, creates no child, and never changes a current-problem field. Only `CreateChildScope` starts a fresh activation.                                                                                                                                                         |
| Plain state           | Plain nodes retain one object across views, including different candidate types. Sharing retains it across their child activations too.                                                                                                                                                                            |
| Sharing               | Shared bound configurations get one executor per problem in the sharing owner's table; unshared child activations start independently. Shared parents retain contextual children; independently shared plain children span problems.                                                                               |
| Multiple operations   | One configuration/hook returns a common mutation/evaluation executor. Both role views reuse its fields for one problem and get independent fields for another.                                                                                                                                                     |
| D29                   | The sharing set and `ResolvedHereOrBelow` still key only on configuration references. Resolving under any problem below an owner prevents its later `Share`; unrelated configurations remain shareable. The original weak-key tracking and configuration-level construction recursion guard remain.                |
| Results/failure       | Plain results remain root/run owned across views and independent across roots. A read does not block `Share`. Construction failure still ends the tree; published results remain readable.                                                                                                                         |
| Validation            | The full `Supports` walk precedes every resolution, including a builder's later-created configuration. `ChildExecution` mappings remain. The prototype reuses `SearchConfigurationValidation.Mismatch` for leaf attribution. `TryResolve` leaves the tree usable on rejection and propagates construction failure. |
| Generic wrapping      | Mutation uses production `IInvocation`, `IInvocationWrapper`, `WrappingExecutor` and successful-call counting. Problem/search-space metadata remains available; `Proceed(executor, random)` still calls the named operation. No invocation context carrier is added.                                               |
| Controls/dynamic data | Gaussian mutation retains batching and `IMutationStrengthControl` forwarding. Private strength survives reuse and stays independent across problems. The relative evaluator keeps an epoch-keyed cache on one typed dynamic problem; updates retain the executor and refresh the cache after evaluation.           |

**Completed ports and execution:**

- The racing port covered the complete current `DynamicRacingAlgorithm.cs` at `8e22bee2`. Meta creation/mutation bind to the separate meta problem; contenders bind in fresh child activations. Burn-in, merging, hall-of-fame selection/revival, projection, performance tracking, untracked builder input, restarts and cleanup remain in the source port. `ObservedEvaluator` and `IterationUpdating` preserve the current substitution and successful-call/update boundaries; they add no new observation lifecycle.
- `RacingChecks` compares outputs, epochs, builder calls, construction counts, starts, disposals and evaluation counts with the original source across **24 combinations**: three update policies, one/three racers, zero/one burn-in epochs and disabled/enabled early termination. Changing challenger values exercises hall-of-fame revival; a hall-disabled comparison distinguishes that path. Additional checks cover builder-applied updates, one repeated contender configuration reference, a late incompatible builder result, and five failure/cleanup phases (step, termination, merge, dispose, merge plus dispose). These behavioral baselines use the unchanged algorithm source in the source-compiled executable, not a merged fork.
- `CompositeCreation`, `CompositeCrossover` and `CompositeMutation` port all three composite operations. Children bind to their corresponding cached `NoProblem` objects through views of the same scope. Creation/crossover match production for empty, singleton and seven-item batches with distinct real/integer candidate types and search spaces. Mutation exercises both `All` branches and one configuration reused for different part search spaces. Child mappings stay declared.
- Both PythonInterop refiner boundaries resolve the numeric child during construction. Checks execute the actual fitter through both wrappers and verify reuse when inner problem/configuration references agree. The numeric port retains `FittingData`, iteration limits, no-op and failure behavior; checks include an alternate fitting dataset and zero iterations. Managed callbacks are used; no Python interpreter is started.
- `TravelingThief.cs` runs a three-city/two-item alternating decomposition: composite outer candidates, permutation tours and Boolean packings, each with its own problem/search space. Tour evaluation reads current packing; packing evaluation reads current tour. The owner updates subproblem objects between turns while retaining child executors. Exhaustive tiny child algorithms keep it deterministic. This is a composition example, not a realistic solver or optimality claim.
- `TimingCheck` runs unchanged `AlgorithmRun` over the prototype resolver. Its deterministic clock excludes 100 seconds of initial binding, includes 7 seconds of deferred binding plus 4 seconds of iterator work, excludes a 50-second pause, resumes the retained iterator, and checks zero elapsed for direct scope execution. Tree-wide failure and result publication remain unchanged.

**Fork inspection:** the recorded inspection at `c35ebf82` still applies. `State.Fork` copies prepared state, then `ExecutionFactory.Forkable` rebinds children; `WithContext` rebinds callbacks. Copying executor references would share fields and retain old children/callbacks. Problem references also need a snapshot policy if a fork copies mutable problems. Root-fork result ownership and D29 integration remain merge work. No fork design or compatibility claim is implemented.

#### Phase 4b measurements

These are frozen measurements against baseline `8e22bee2`. The spike harness and its CSV output were discarded after the migration, so they are not reproducible from this repository; they justify the accepted tradeoff only.

Machine: SDK **10.0.402**, selected by `global.json`; runtime .NET **10.0.13**, Windows x64, 20 logical CPUs. The temporary harness adds no permanent benchmark dependency. Identity uses a non-inlined author override, preallocated input/RNG and consumed output. Gaussian uses the real primitive and identical deterministic batching on two three-coordinate parents. Each case warms for 100,000 operations and takes nine samples with collection between samples: 2,000,000 identity invocations, 50,000 Gaussian invocations, 20,000 cached resolutions or 10,000 constructions. `Stopwatch` measures time and `GC.GetAllocatedBytesForCurrentThread` allocation. Construction includes scope, views, table, validation, guard and executor, not just the leaf.

Fresh typed-hook measurements on 2026-11-11, tiering disabled. Time is median nanoseconds per operation; allocation is median bytes per operation.

| Case                                            | Production ns | Bound ns | Production B | Bound B |
| ----------------------------------------------- | ------------: | -------: | -----------: | ------: |
| Bare identity invocation                        |          2.31 |     3.17 |            0 |       0 |
| Counted identity invocation                     |          9.34 |     6.98 |            0 |       0 |
| Gaussian mutation, two three-coordinate parents |        319.01 |   321.15 |          392 |     392 |
| Cached resolve, retained view                   |         84.04 |    82.58 |           56 |      64 |
| Cached resolve, new view                        |         85.90 |    85.83 |           80 |      96 |
| New root plus first executor                    |        341.84 |   314.90 |        1,344 |   1,456 |
| Fresh child plus first executor                 |        297.28 |   289.96 |          816 |     928 |
| One root, two prospective problems              |        411.33 |   731.80 |        1,424 |   1,736 |

The last row intentionally constructs different amounts of state: the current resolver returns one stateless executor for two prospective problem objects, while binding constructs two. Problems/configurations are preallocated. The production column used the actual production assembly. The spike's own old-path measurements were only a control: their plain entries also used the larger pair key.

The identity helper adds **0.86 ns**, while the smaller counting invocation reduces forwarding cost in this run. Gaussian is effectively unchanged here (**319.01 vs 321.15 ns**, overlapping ranges; **392 B** on both paths including returned candidates). Bare/counting calls allocate zero bytes. New root and child bindings each add **112 B**; two problems add **312 B**. Retained views avoid repeated view allocation; cached resolution still allocates the current guard/delegate. No resolver optimization is included.

Construction distributions overlap, including an outlier in the bound root sample, so median differences do not establish a speedup. Source-compiled old-path invocation controls were 2.38 ns bare and 9.99 ns counted. Default-tiering sensitivity runs changed rankings and allocation as JIT optimization progressed; they were not steady-state speed evidence.

Limitations: one machine/process per configuration, delegate-loop overhead, forced collections, no CPU pinning/confidence intervals, and changed runtime policy with tiering disabled. There is no measurement of end-to-end optimizer speed, cold JIT latency, concurrent construction, Python callbacks or forks. Source-compiling libraries into one executable does not validate package layering, exported API compatibility or Roslyn guardrail coverage of new public hooks. A migration must update those checks and preserve real operation descriptors and external-author paths. No controls/observation redesign or generic invocation path was introduced.

#### Phase 4b validation and recommendation

Fresh validation after typed-hook acceptance:

- Isolated Release build and **21 spike checks** passed, including the matrix/additional cases above. They were executable output, not an extra solution test suite. The spike build had source-linked and fixture warnings; it was not claimed warning-clean.
- The whole solution passed: **3,344 tests, zero failures/skips, 41.557 seconds**, with `--max-parallel-test-modules 1`. This fresh run validates unchanged production libraries/consumers, separately from the bound-path checks. It does not reuse phase 4's validation record.
- Final solution Release build passed (zero warnings/errors in that incremental build). Solution whitespace, warning-level style and error-level analyzer verification passed with workspace-loading warnings. Separate verification of all spike C# files passed for whitespace, warning-level style and error-level analyzers. Prettier checked both changed plans and `git diff --check` passed. No published example or package contents changed; no fresh documentation-build or pack result is claimed.

**Spike recommendation, subsequently accepted: proceed with full migration using the narrower typed construction shape.** Required composition/ownership scenarios work; sampled real mutation work has unchanged allocation and no demonstrated material invocation regression; executor interfaces/call sites lose problem/search-space plumbing without a new public lifetime mechanism. The benefit is smaller execution contracts, not removal of validation, authoring arities or lifetime/failure rules. Accept the measured construction allocation tradeoff explicitly; do not promise a general speedup.

Replace existing role contracts/hooks rather than ship the experimental seam. Preserve operation identity, generic wrappers, external roles, mappings, signature rejection, multi-operation reuse, results, D29, controls and phase 4 elapsed semantics, and migrate consumers with API/architecture/analyzer checks. Fork snapshots/rebinding and root-fork result ownership remain separately unresolved.

#### D5 migration accepted on 2026-11-11

The developer accepted the measured tradeoff and authorized the full migration using typed construction hooks. Typed scope views take the problem object; their search space comes from that object. Operators/algorithms key execution by configuration/problem references; plain hooks retain configuration-only identity. Resolve validates every declared signature, including child mappings, before construction. Result ownership, D29, tree-wide failure, controls, observation boundaries and phase 4 elapsed behavior stay in place. No compatibility shims, new public configuration family, invocation context carrier, fork implementation or phase 5/6 redesign is included.

The production migration changes the existing contracts and callers rather than adding the experimental configuration family. Candidate operations expose candidate-only executor interfaces; termination/interception and algorithms expose their state. Invocation inputs are operation inputs and RNG (plus initial state/cancellation for algorithms). Fixed leaf hooks receive typed scopes, and agnostic hooks keep their generic run types. Stateless context-dependent helpers retain one typed author override behind a small bound executor; context-free helpers can return themselves. Authoring ladders remain available, including their explicit executor bases. Private routing, pipeline, cache and composition executors drop context type arguments they no longer use.

Composite child operators resolve against the cached part `NoProblem` objects. Racing binds its meta operators to `EmptyMetaOptProblem` and contenders in fresh child scopes, preserving tracking/restarts and untracked builder input. Both PythonInterop inner refiners return the numeric child executor resolved against `InnerProblem`. `DynamicRelativeQualityEvaluator` uses the existing fixed-signature evaluator base, declares its child and forwards its candidate contracts, receives the typed problem through its hook, and keeps its epoch reference on that executor. Child mappings stay declared. The Traveling Thief API usage spec alternates exact child searches over tour/packing problems with distinct candidate/search-space types, retaining their executors while changing mutable instance data.

New core tests pin pair reference identity (including equal distinct records), compatible static views, fresh/shared lifetimes, plain-node reuse, preflight on every resolve, the configuration-wide D29 guard and root-owned results readable after tree failure. A native dynamic test pins separate problem references and retained executor state across an epoch change. Existing racing, composite, PythonInterop, external operation, analyzer/code-fix and timing suites are migrated. `AlgorithmRun` changes only construction binding and the executor call: retained-advance timing remains untouched.

No fork design is implemented. The recorded inspection at `origin/dynamic-Racing` `c35ebf82` still requires executor snapshots, callback rebinding and root-fork result ownership integration. Benchmark numbers above are the historical bounded-spike comparison at `8e22bee2`, not a new measurement of the final migrated library. They justify the accepted allocation tradeoff, not an end-to-end speedup claim.

**Fresh production migration validation on 2026-11-12 (SDK 10.0.402):**

- Focused resolution tests passed first, followed by the affected suites. Six new `ProblemBindingTests` cover the new resolver contracts; `TravelingThiefDecompositionSpecs` covers distinct child algorithms/problems/candidates/spaces; the new dynamic-relative test covers problem isolation and retained state across instance updates.
- Final `dotnet test --solution ./HEAL.HeuristicLib.slnx --configuration Release --no-restore --max-parallel-test-modules 1` passed **3,352 tests, zero failures/skips, 55.842 seconds** after the final typed evaluator correction: core 2,822; API usage 221; Experimental 263; documentation 19; scenarios 27. The scenarios exercise both PythonInterop inner-refiner paths. This is new migration evidence, not the phase 4 or spike baseline result.
- `dotnet build --configuration Release --no-restore` passed. The final incremental build reported zero warnings/errors; full rebuilds still report repository compiler/analyzer warnings, so this is not a warning-free rebuild claim.
- `dotnet format whitespace ./HEAL.HeuristicLib.slnx --verify-no-changes --no-restore`, warning-level style verification and error-level analyzer verification passed. These tools emitted workspace-loading warnings.
- `npm run docs:build` passed after the published guidance/glossary changes. `dotnet pack --configuration Release --no-restore` passed after the final code correction. Changed Markdown passed Prettier; `git diff --check` passed. Text was read/written as UTF-8 and repository LF line endings were restored. Final inspection also repaired three already-corrupted comment separators present in `HEAD` in the touched `NoviceFrictionSpecs.cs`; executable code was unchanged.
- One-off migration scripts/tooling and the isolated spike experiment were removed; no benchmark dependency or migration adapter is added to production projects.

**Review decisions on 2026-11-13 (accepted):**

- `Neighborhood` derives from `Operator<MoveCreation<TMove>, TCandidate, TSearchSpace, TProblem, IExecutor>`, so it gets the shared supported-signature check and the typed problem from the base instead of repeating them. Its one hook still returns one executor for all three move operations.
- `OperationInfo.For<…>(problem)` takes the typed problem of the scope that resolves the wrapped child, and `OperationInfo.Wrap(executor, wrapper)` takes no problem. A generic wrapper therefore cannot pass a problem that doesn't fit the run's types. The typed `TOperation.Wrap(executor, wrapper, problem)` keeps its parameter. The executor check used during resolution is an internal static method, and the per-operation instance cache is gone.
- `IInvocation.Proceed(executor, random)` documents that the other executor has to be resolved for the same problem and runs against the problem it was bound to. This is a documented rule, not a runtime check. Built-in choices resolve their siblings through the same scope, so they meet it.
- Executor bases with no search-space parameter drop the candidate type they no longer use. `TerminationExecutor<TCandidate, TSearchState>` becomes `TerminationExecutor<TSearchState>`, `TerminationExecutor<TCandidate>` becomes the non-generic `TerminationExecutor`, and `InterceptionExecutor<TCandidate, TSearchState>` becomes `InterceptionExecutor<TSearchState>`. The test-only external executor interfaces `IChecksumExecutor` and `IReadValueExecutor` drop it as well. Bases with a search-space parameter keep `TCandidate`, because the search-space constraint uses it.
- After a two-axis review on 2026-11-14:
  - Six composition executors (`EliteSelector`, `GenderSpecificSelector`, `PredefinedCandidatesCreator`, `TransformedCreator`, `TransformedCrossover`, `RefinementEvaluator`) never read their problem. They now derive from the candidate-only executor bases, and developer guidelines § 4.12 says so.
  - The executor key's problem is typed `IProblem?`, and nodes without a problem pass `null` explicitly.
  - The docs no longer use the undefined term "plain nodes".
  - The stale "once per resolution scope" algorithm docs and a record holding an `IReadOnlyList` in the Traveling Thief spec are corrected.
  - A new test pins that a declared child's unsupported signature rejects resolution before anything is built.
- Private test and documentation executors drop the `TSearchSpace`/`TProblem` type parameters they no longer use, and tests drop their unused `problem`/`space` locals. Afterwards the full solution passed 3,352 tests, the build reported no errors, the whitespace, style and analyzer checks passed, and the documentation built.

**Follow-up decisions on 2026-11-14 (accepted, implementation spec):** The developer accepted all three follow-ups from the two-axis review. Each item states the shape, the migration and the tests. Backward compatibility is not a goal: migrate every in-repository caller, add no shims, and keep each public type's shape except where an item names a change.

1. **State-only algorithm executor bases.** An algorithm executor that reads neither the search space nor the problem shouldn't have to carry them, just as the candidate-only operator executor bases allow.
   - Add `public abstract class AlgorithmExecutor<TSearchState> : IAlgorithmExecutor<TSearchState>` with the abstract `RunStreamingAsync`. Place it in `Algorithm.cs`, after the problem-bound `AlgorithmExecutor<TCandidate, TSearchSpace, TProblem, TSearchState>`.
   - Add `public abstract class WrappingAlgorithmExecutor<TSearchState>(IAlgorithmExecutor<TSearchState> childAlgorithm) : AlgorithmExecutor<TSearchState>` with the same protected `ChildAlgorithm`. Place it in `WrappingAlgorithm.cs`, after the problem-bound one.
   - Keep both problem-bound bases unchanged. They stay the right base for executors that read the problem, such as the algorithm observation executors and `IterationUpdatingAlgorithm`.
   - Migrate every private or internal executor, in `src` and in tests, whose body reads neither `problem` nor `searchSpace`. Drop the type parameters it then no longer uses, and stop passing `scope.Problem` to it. `TimedAlgorithm` is one of them.
   - Public executor classes keep their shape: `IterativeAlgorithmExecutor`, `CycleAlgorithmExecutor`, `PipelineAlgorithmExecutor`, the three budget executors and `StateTerminatedAlgorithmExecutor`. Derived authors may read their protected problem fields, and reshaping them is not part of this decision.
   - Extend the developer guidelines § 4.12 sentence on candidate-only executor bases to name `AlgorithmExecutor<TSearchState>` for algorithms. Update every contributor or guide page that lists the algorithm executor bases.
2. **Dynamic operators use the problem they are bound to.**
   - `DynamicCachingEvaluator` (corrected on 2026-11-14, after the first implementation attempt; see the bullet at the end of this block):
     - Remove its `SourceProblem` property, `SourceProblemParameter` and the constructor's problem argument. It caches for the problem it is resolved for.
     - It stays an agnostic `WrappingOperator<Evaluation, TCandidate>`, so its child resolves under the run's own types, which are also the types that preflight checks. A fixed-signature base would narrow the child's problem to the declared interface. A child written for a concrete dynamic problem would then pass preflight but fail construction.
     - It becomes `DynamicCachingEvaluator<TCandidate, TKey>`. The cache needs only `EpochSchedule` and `CurrentEpoch`, and `IDynamicProblem<TCandidate, out TSearchSpace>` is covariant in its search space. Every dynamic problem over `TCandidate` is therefore an `IDynamicProblem<TCandidate, ISearchSpace<TCandidate>>`, and the search-space type parameter is not needed.
     - `Supports` adds `execution.Problem.IsAssignableTo(typeof(IDynamicProblem<TCandidate, ISearchSpace<TCandidate>>))` to the base check, so a run over a problem that is not dynamic is rejected when the configuration is resolved. The hook binds the problem with the guarded test the authoring bases use: `scope.Problem is IDynamicProblem<TCandidate, ISearchSpace<TCandidate>> problem ? … : throw ExecutionSignature.Mismatch(this, ExecutionSignature.Describe(<declared types>), ExecutionSignature.Describe(<run types>))`, checked before the child is resolved. The per-call `ReferenceEquals` check and its exception are removed.
     - The constructor becomes `(IOperator<Evaluation, TCandidate> childEvaluator, ICacheKeySelector<TCandidate, TKey> keySelector)`.
   - Extension methods for `DynamicCachingEvaluator`:
     - The extensions on `IDynamicProblem` took the problem only to store it. Remove them, because keeping them would suggest a binding that no longer exists.
     - The evaluator extensions become `CachedPerEpoch()` and `CachedPerEpoch<TKey>(ICacheKeySelector<TCandidate, TKey> keySelector)`, with every type argument inferred. The name avoids reading like the non-dynamic `Cached(...)` overloads of `CachingEvaluator`.
   - `ReevaluationInterceptor<TCandidate, TSearchSpace, TProblem, TSearchState>`:
     - Remove `SourceProblem`, `SourceProblemParameter` and the constructor's problem argument. It resolves its evaluator through its own scope, with `scope.Resolve(Evaluator)`, and reads the epoch from `scope.Problem`.
     - The static `ReevaluationInterceptor.Create` existed only to infer the type arguments from the problem argument. Remove it.
   - Migrate every test and doc caller. Tests that checked the per-call problem-mismatch exception now check that resolving over a problem without the dynamic interface is rejected. Keep the existing epoch, grace-count, size-limit and retention tests passing with unchanged expectations.
3. **One mismatch message.**
   - The three `scope.Problem is TProblem` bridges in `Operator.cs` (two) and `Algorithm.cs` (one) stay as three single expressions. They must produce the same message shape: `ExecutionSignature.Mismatch(this, <written-for>, ExecutionSignature.Describe(typeof(TRunSearchSpace), typeof(TRunProblem), <run search state>))`. Here `<written-for>` is the base's supported-execution description, and `<run search state>` is the run's search-state type (the algorithm's own `TSearchState` in `Algorithm.cs`). Raw `typeof(...).Name` metadata names are no longer used.
   - Add one focused test per bridge. It calls the construction hook directly with a scope whose problem is not a `TProblem`, which only bypassing resolution can produce, and checks the message. Suppress the direct-call analyzer locally, as the resolver does.

Out of scope: reshaping public algorithm executor classes, `IAlgorithm<TCandidate>`'s unused type parameter, and the `IIterationUpdatable` cast in `IterationUpdatingAlgorithm`.

- Implemented items 1–3 on 2026-11-14: added the state-only algorithm and wrapping executor bases, migrated private executors that do not read the search space or problem, and updated their authoring guidance; bound dynamic caching and reevaluation to their scopes' problems, kept the corrected two-parameter cache agnostic so its child retains the run signature, and migrated cache calls to fully inferred `CachedPerEpoch(...)`; unified the three defensive bridge mismatch messages and covered them with direct-hook tests. The cache also has a defensive mismatch test. Final validation: Release build passed with **zero errors and 30 existing warnings**, with no new compiler or Sonar warnings on follow-up changes; Experimental passed **267 tests**, and the whole solution passed **3,360 tests**, both with zero failures or skips. Whitespace, style and analyzer verification passed; the documentation build passed (rerun outside the sandbox after a generated-output permission error); Prettier passed for all **12 changed Markdown files**; `git diff --check HEAD` passed. No design deviation or compatibility shim was needed.
- Spec correction on 2026-11-14: the first attempt stopped as required because the fixed-signature cache base resolved its child under `IDynamicProblem<…>` while preflight checked it under the run's problem. Item 2's cache bullets now keep the agnostic wrapper and drop the search-space type parameter. This stays within the accepted decision, which is to drop `SourceProblem` and use the bound problem; the fixed base was a detail of this spec, not of the decision.

**Follow-up decisions on 2026-11-15 (accepted, implementation spec):** The developer accepted three follow-ups from a review of the uncommitted phase 4b change. Backward compatibility is not a goal: migrate every in-repository caller, including tests, documentation specs and documentation text, and add no shims.

1. **An invocation no longer reports a problem.** `IInvocation.Problem` and `IInvocation.SearchSpace` date from when the problem was a call argument. Their only reader was one test asserting that they exist, and the observation wrappers already take the typed problem at construction.
   - Remove `SearchSpace` and `Problem` from `IInvocation<TResult>`. A wrapper that needs the problem reads the typed `scope.Problem` in its own `CreateExecutor` and keeps it in its wrapper struct or executor.
   - `IOperation<TSelf>.Wrap` becomes `Wrap<TCandidate, TSearchState, TWrapper>(IExecutor executor, TWrapper wrapper)`, with no problem parameter.
   - `OperationInfo.For` becomes `For<TOperation, TCandidate, TSearchState>()`, with no problem parameter. It may return one cached instance per closed set of type arguments again, so a construction allocates nothing for it. `OperationInfo.Wrap(executor, wrapper)` keeps its shape.
   - Each operation's private invocation struct and wrapped executor drop the `TSearchSpace` and `TProblem` type parameters and the fields that held the problem and search space. This covers the nine core operations, the three move operations in Experimental, and the operations defined in tests and documentation specs.
   - `IInvocationObservation.Wrap` and the algorithm observation `Wrap` keep their problem parameter, because observation records report the typed problem.
   - `Proceed(executor, random)` keeps its meaning: another executor runs against the problem it was bound to. Its documentation says only that the other executor must implement the same operation, since an invocation no longer reports a problem that could disagree. Update the wording in `IInvocation`, the developer guidelines, the wrapper guide, the glossary, `operator-implementation.md` and `execution-resolution.md`.
   - `ChoosePerInvocationTests` drops its two assertions on `call.SearchSpace` and `call.Problem`. No other assertion changes.
2. **Operation members name only the types they use.** Every `IsExecutor` implementation tests the candidate or the search state alone.
   - `IOperation<TSelf>.IsExecutor` becomes `IsExecutor<TCandidate, TSearchState>(IExecutor executor)` and `IItemOperation<TSelf>.Route` becomes `Route<TCandidate, TSearchState>(WeightedRouter<IExecutor> children)`, both with `where TSearchState : class, ISearchState` only.
   - The internal `OperationInfo.RequireExecutor` becomes `RequireExecutor<TOperation, TCandidate, TSearchState>`.
   - `ResolutionScope.ResolveOperator` and the role resolution extensions keep their type parameters, because the scope view and the signature check still need the search space and problem.
3. **A fixed-signature parent hands its children what it declares, and the check before construction says so.** This settles "Child preflight under a fixed-signature parent" from the backlog with the option the developer chose: reject up front.
   - The defect: a configuration on a fixed-signature base resolves its children through a scope typed with its declared search space and problem, while the walk of `ExecutionSignature.Supports` asked those children about the run's types. A parent written for `BaseProblem` with a child written for `SubProblem`, run over a `SubProblem`, passed the check and failed during construction, which ends the scope tree and makes `TryResolve` throw.
   - Add `ExecutionSignature ChildExecution(ExecutionSignature execution) => execution;` to `IConfigurationNode`, documented as the execution this configuration hands to the children it declares. The default returns the execution unchanged.
   - `Operator<TOperation, TCandidate>` and `Algorithm<TSelf, TCandidate, TSearchState>` declare it as a public virtual member returning the execution unchanged. The fixed-signature bases override it:
     - `Operator<TOperation, TCandidate, TSearchSpace, TProblem, TExecutor>` returns `new(typeof(TSearchSpace), typeof(TProblem), execution.SearchState)`.
     - `Operator<TOperation, TCandidate, TSearchSpace, TProblem, TSearchState, TExecutor>` and `Algorithm<TSelf, TCandidate, TSearchSpace, TProblem, TSearchState>` return `ExecutionSignature.For<TSearchSpace, TProblem, TSearchState>()`.
   - Both walks apply it before the parameter's own mapping: `ExecutionSignature.Supports` and `SearchConfigurationValidation.ReportMismatch` ask each child about `parameter.ChildExecution(configuration.ChildExecution(execution))`. A parameter that states another execution, as the composite search space, racing and the PythonInterop refiners do, still decides alone, because those mappings ignore their input.
   - No resolution code changes. Construction already resolves the children under the declared types, so the check and construction now use the same types.
   - Tests, in the core suite beside the existing preflight tests:
     - The backlog's case, with a fixed-signature evaluator written for a base problem that declares a child written for a subclass, run over the subclass: `Resolve` rejects before anything is built, `TryResolve` returns false with a reason and leaves the scope usable, and validation blames the child.
     - The same parent typed for the subclass resolves and runs.
     - A fixed-signature parent whose child is written for a wider type than the parent declares still resolves.
   - `DynamicCachingEvaluator` stays an agnostic wrapper with its own `Supports` and guarded problem test. That is the pattern for a wrapper that needs only an interface of the problem while its child may be written for the concrete problem.
   - Documentation in this step: the XML documentation of `IConfigurationNode.Supports`, `IConfigurationNode.ChildExecution` and `ExecutionSignature.Supports`, and the paragraph on typed views in `execution-resolution.md`. The wrapper guide's description of the agnostic pattern waits for the documentation pass below.
   - Remove the backlog entry "Child preflight under a fixed-signature parent" once this is implemented.

- Implemented items 1–3 on 2026-11-15: removed invocation problem/search-space metadata, reduced operation type parameters and cached `OperationInfo`; migrated callers and documentation, aligned both child preflight walks with fixed-signature construction, added the three regression tests and removed the settled backlog entry. No deviations. Validation: Release build 0 errors, 56 existing warnings and no new warnings in touched files; solution tests 3,363 passed, 0 failed, 0 skipped (baseline 3,360 plus 3 added); whitespace, style and analyzer verification each exit 0; documentation build exit 0 after retrying the generated-output cleanup outside the sandbox; Prettier checked all 8 changed Markdown files, exit 0; `git diff HEAD --check` exit 0.

Out of scope for this step: the items under "Later simplifications recorded on 2026-11-15" below.

**Recommendation after implementation:** retain the accepted typed construction shape and the smaller invocation/executor contracts. The ports and current suites preserve the required composition, authoring, ownership, failure and timing behavior. Keep the measured construction allocation cost as an accepted tradeoff, without claiming a general performance gain. Phase 5 controls, phase 6 observations and the recorded fork integration remain separate work.

### Phase 5: executor controls

Accepted (D4). See [the proposal](#executor-control-proposal). `IExecutionNode` arrives here (D19), together with the first node that does not execute anything.

Decided by the developer on 2026-11-16, while drafting the implementation spec:

- `IRunResult` derives from `IExecutionNode` instead of `IExecutor`. A run result is a runtime object created by a configuration node and held in the scope's table, which is what D19 calls an execution node, but it executes nothing.
- A shared node that links an operator and an algorithm to one runtime value has reference equality. The resolver keys by reference, so value equality would make configurations that adapt and configurations that do not compare equal, and keying by value would silently merge independently created nodes.
- **The base value stays plain data, and adaptation is a separate optional link.** This amends the proposal's "holds the current value". `GaussianMutator` keeps `double MutationStrength` as its base, a regular property with value equality, and gains a nullable reference to an adaptation node. Two mutators without an adaptation and equal bases are equal; equal bases with the same adaptation reference are equal. The base stays a declared `double` parameter. An adaptation option suggested during review let the link hold only a scale factor; the developer generalized it.
- **Adaptation is general, not specific to mutation or to the evolution strategy.** Its read side is one interface mapping the base value to the effective value (`double` to `double`), so any operator can make a parameter adaptable through an optional link. Each form is a node with its own runtime object and its own write operation. Scale (multiply) and offset (add) are the first two forms. An algorithm's slot names the form its rule writes, so the evolution strategy's 1/5 success rule takes a scale form. The working name in discussion was "strategy parameter"; the final name is open.
- The base value's adaptation is not itself a declared parameter in this phase. The baseline comes first, and fitting adaptation into the declared parameter system is later work.
- **The concept is named adaptation.** The read side is `IAdaptation`, and an abstract class `Adaptation<TAdapted>` is the base of adaptation nodes, so they are classes with reference equality and cannot be records. "Strategy parameter" was rejected because evolution strategies use it for self-adaptive values carried by each individual, and because the library's _parameter_ already means a declared configuration parameter. Scale and offset are the only forms for now; clamping, absolute override and composite forms are added when a rule needs them.

**Implementation spec (accepted 2026-11-16):** Implementing the interface directly, for example with a record, stays open to external authors; the base class only makes the built-in forms and derived adaptations classes. Backward compatibility is not a goal: migrate every in-repository caller, including tests, documentation specs and documentation text, and add no shims.

1. **Execution nodes (D19).**
   - Add `public interface IExecutionNode;` in `Execution/IExecutionNode.cs`. `IExecutor : IExecutionNode`. `IRunResult : IExecutionNode` instead of `IExecutor`.
   - `IConfigurationNode<out TExecutionNode> where TExecutionNode : class, IExecutionNode`, with `TExecutionNode CreateExecutionNode(ResolutionScope scope)` replacing `CreateExecutor`. Update its XML documentation.
   - `ResolutionScope.Resolve`, `ResolveOptional`, `TryResolve` and the private helpers are constrained to `IExecutionNode`. The table becomes `Dictionary<NodeKey, IExecutionNode>`, renamed from `ExecutorKey`. Messages that say "executor" for these nodes say "execution node" where the node need not execute.
   - Operator and algorithm `CreateExecutor` hooks keep their names.
   - `ChildResolutionAnalyzer`, its code fix and the direct-call check (HLib0001) recognize `CreateExecutionNode` as they recognize `CreateExecutor`; extend their tests.
   - Migrate `Counter`, `Duration` and every test node implementing the interface.
2. **Adaptation** (grouping folder `Operators/Adaptation` in namespace `HEAL.HeuristicLib.Operators`, as `Operators/Composition` is; changed from a top-level `Adaptation` namespace after review, because the base class would share the namespace's name).
   - `public interface IAdaptation : IExecutionNode { double Apply(double baseValue); }`
   - `public abstract class Adaptation<TAdapted> : IConfigurationNode<TAdapted> where TAdapted : class, IAdaptation`, with `public abstract TAdapted CreateExecutionNode(ResolutionScope scope)`. Its documentation states the reference-equality contract: an adaptation is a link, and every place that holds the same instance applies the same runtime value.
   - `public sealed class ScaleAdaptation : Adaptation<AdaptedScale>` and `public sealed class AdaptedScale : IAdaptation`: a settable `Factor` starting at 1.0, and `Apply(b) => b * Factor`.
   - `public sealed class OffsetAdaptation : Adaptation<AdaptedOffset>` and `public sealed class AdaptedOffset : IAdaptation`: a settable `Offset` starting at 0.0, and `Apply(b) => b + Offset`.
   - The forms carry no configuration data; the starting point is the operator's base value. Runtime objects have public constructors so external nodes can produce them. Each node and its runtime object share one file. Changed after review: atomic `Multiply`/`Add` with `Interlocked.CompareExchange` and `Volatile.Read` were replaced by plain settable properties, because they only guarded concurrent writers to one shared adaptation.
   - Slots that read an adaptation are typed `IConfigurationNode<IAdaptation>?`; covariance accepts every form. Slots that write one are typed to the form's node, such as `ScaleAdaptation?`, so `ResolveOptional` infers the runtime type without a cast.
3. **`GaussianMutator`.** Keeps `double MutationStrength` and `MutationStrengthParameter` unchanged. Adds `IConfigurationNode<IAdaptation>? MutationStrengthAdaptation { get; init; }` with a declared child parameter, so `Substitute` and `Sharing` reach it. The executor resolves it optionally and computes `adaptation?.Apply(MutationStrength) ?? MutationStrength` once per `Mutate` call. The executor no longer implements a control.
4. **`EvolutionStrategy`.** Adds `ScaleAdaptation? MutationStrengthAdaptation { get; init; }` with a declared parameter. Unset means no adaptation. When set, the 1/5 success rule multiplies `Factor` by the factor it computes today. Factories take no adaptation argument.
5. **Removals.** `ExecutorControls.cs` with `IExecutorControlProvider`; `IMutationStrengthControl`; control forwarding in `WrappingExecutor`, the nine operation observation executors and `ChoosePerInvocation`'s wrapper; the sixteen `Resolve<TControl>(…, out TControl?)` overloads in the role files; control remarks in `IInvocationWrapper` and `OperationInfo`.
6. **Tests.**
   - New, for adaptation: the forms' arithmetic; equality (mutators without adaptation and equal bases are equal; equal bases with the same adaptation are equal; different adaptation instances are not).
   - New, for the strategy: the D16 table rows with adaptation in place of strength (nothing shared: each turn restarts at the base; adaptation shared: it survives turns; adaptation and mutator shared; mutator shared alone: the strategy adapts a value its mutator does not read, pinned as documented behavior); two mutators under `ChoosePerInvocation` linked to one adaptation both adapt; `Timed`, `Counted` and `PipelineMutator` around the mutator do not stop adaptation; an unset slot does not adapt; an offset adaptation applied by an external test rule; the configuration never changes; independent runs stay independent.
   - Rewrite: `AdaptableMutationStrengthTests`, `MutationParameterValidationTests` and the control parts of `SelectionSpecs`, `OperatorAuthoringSpecs`, `OperatorPreparationSpecs` and `OperatorOwnershipTests`; `ExecutorResolutionTests`' child-first collision test reads its child directly.
   - Delete: `OperationBindingTests.EveryOperationForwardsControlsUnlessItsWrapperSuppliesThem` and its helper types; the provider implementations in `OperationDefinitionDocumentationSpecs` and `CustomWrapperDocumentationSpecs`.
   - Strategies naming `GaussianMutator` stop adapting (D16). Report every expected value that changes with its test name; do not adjust an expectation silently.
7. **Documentation.**
   - Rewrite "Mutable run parameters" in `operator-implementation.md` as adaptable parameters: base value, optional adaptation link, `Apply` once per invocation, an algorithm slot typed to the form it writes, and the sharing rules.
   - Replace the control paragraphs in `execution-resolution.md` § "Controls, failures and lifetime" (rename the section) and "Specialized executor capabilities" in `operator-composition.md` with the adaptation wiring example.
   - Remove control forwarding from `writing-wrappers.md`, developer guidelines § 4 (wrapping executor sentence) and `layering.md`; add `Adaptation` to the layering table (contract in L1, forms in L3).
   - Glossary: `Execution node` becomes `Canonical` with D19's meaning; `Executor` says it is the executing kind of execution node; add `Adaptation` as `Canonical`.
   - Backlog: remove the two entries on controls under "Runs, execution and experiments" and "Optional capabilities and packaging".
   - Plan: amend the executor control proposal's code sample and description to the adaptation shape.

Out of scope: observing adaptation values in traces (phase 6), declared-parameter integration and grids over adaptation (later), forms beyond scale and offset, `IAdaptation<T>` for non-`double` parameters, and the configuration graph check for an adaptation nobody writes or reads.

- Implemented on 2026-11-16: migrated general configuration construction and resolution to `IExecutionNode`/`CreateExecutionNode`, separated run results from executors, added reference-equal scale and offset adaptation links, wired optional adaptation into Gaussian mutation and the evolution strategy, and removed controls, forwarding and all sixteen control-resolution overloads. Updated analyzer/code-fix coverage, adaptation, sharing and external-author tests, executable documentation, glossary, layering and backlog. No design deviations or compatibility shims. The rewritten pipeline case (`EvolutionStrategy_ConfiguredWrapperDoesNotInheritItsChildStrengthControl` → `EvolutionStrategy_ExplicitAdaptationWorksThroughWrappers("pipeline")`) deliberately changes `[0, -1.5, -3]` to `[0, -1.5, -2.5]` because both strategy and mutator explicitly name the adaptation; no existing regression expectation needed a D16 adjustment. Validation: restore passed after retrying NuGet outside the sandbox; Release build passed with zero errors (the last incremental build reported 8 existing warnings); focused adaptation tests **61 passed**, analyzer/code-fix tests **35 passed**, architecture tests **16 passed**; final solution **3,376 passed**, zero failures or skips (core **2,841**, API usage **221**, documentation **20**, Experimental **267**, scenarios **27**; baseline **3,363**). Whitespace, style and analyzer verification each passed; documentation build passed after retrying generated-output cleanup outside the sandbox; Prettier passed for all **10 changed Markdown files**; `git diff --check` passed.

### Phase 6: simplify observation

Delivers ideas 10 and 14. Today's concepts are sources, observation records, instruments, traces, value functions, retention, clocks, counters, durations, accumulating results and `IObservationInstrument`. Proposed model: an instrument creates a per-run result and records one observation into it, and `Observed` places it. A trace, a best-so-far curve, a Pareto front and a genealogy are then all such folds, and retention is a setting of the trace. Counting and timing keep their dedicated wrappers, because they allocate no observation and timing must include failed calls. A detailed inventory comes when this phase starts, and it must not close custom clocks or external collectors. A custom accumulating analyzer still needs a class with three overrides after phase 3, which this phase should reduce. `Instrument` is retired no later than this phase, with `Analyzer` as the surviving name. `Instrument` keeps its name until then, with no separate rename. The non-generic base and the static `Analyzer` factory class cannot both use that name, so one of them needs another name or home. `IClock` extends `ISubstitutionBundle` only so that a trace can place its clocks ([D59](#decided-in-round-21)), which gives read-only clocks such as `Clock.Elapsed` an empty list; this phase separates reading a clock from placing it, so that `IClock` only reads and counters and durations are bundles in their own right.

### Phase 7: experiments on the parameter system

Grids can vary parameters through metadata in-process, and trials get explicit identity instead of using the configuration as their key (`GridExperiment.cs:28`, idea 11). Typed `VaryBy` stays. JSON formats, construction from data, type registries and REST stay in the backlog.

### Later simplifications recorded on 2026-11-15

Raised in the review of the phase 4b change and deferred by the developer. None is accepted as a design; each needs its own discussion before implementation.

- **Fewer executor base classes.** About 30 public executor bases remain, three per candidate role and more for termination, interception and algorithms. Since D5 each one only stores `problem` and `searchSpace`, and the candidate-only ones are empty. The developer wants authors to keep deriving from a base that fits them rather than implementing the executor interface, so the bases stay as a concept. The question is how many are needed, for example whether the search-space-only arity earns its place beside the problem-bound and the empty one.
- **Bind the objective like the problem.** `Select` and `Replace` still take the objective on every call, and every algorithm passes `problem.Objective`, which D18 fixes for the lifetime of the problem object. Selection and replacement executors could read it from the problem they are bound to at construction, as problem-specific operators read their problem. For the genetic algorithm, the evolution strategy, NSGA-II and the hill climber it is the only thing their executor reads from the problem. To settle first: context-free selectors are their own executor today and would need a bound one, and selecting under a derived objective, such as a scalarization, would need another route.
- **One block of resolution extensions per role.** Each role repeats `Resolve`, `ResolveOptional` and `TryResolve` for the untyped scope, the three-argument typed scope and the four-argument typed scope. The executor type no longer depends on the scope's types, so the two typed blocks differ only in the search state they pass to the signature check. Look at this after phase 5 removes `Resolve<TControl>`.
- **Documentation to add in the phase 8 pass.** A new problem object per turn gets a new, empty executor, and the scope keeps the old one, so the meta-algorithm guide should say to update a subproblem in place or to use a child scope per turn. The wrapper guide should show the agnostic pattern for a wrapper that needs only an interface of the problem, as `DynamicCachingEvaluator` uses it.

### Phase 8: documentation and merge

Step 4's architecture and guide rewrite, the tiered glossary with fundamental concepts first, the naming decisions applied, and the merge. `origin/dynamic-Racing` is the longer-lived branch and merges this one, not the other way round. It receives [the migration notes](#migration-notes-for-origindynamic-racing) before phase 3 step 4 is committed.

## Migration notes for `origin/dynamic-Racing`

The branch merges these changes itself. This branch ports racing as it stands at HEAD and tests a per-contender tracker that survives restarts and per-contender iteration updates, so that the merge has a working example: `DynamicRacingAlgorithm.Entry` and the tests `AContendersTracker_SpansItsRestarts`, `ContendersApplyingIterationUpdates_EndTheRacesEpoch`, `TheBuilder_ReceivesTheAlgorithmItReturnedWithoutTheTracker` and `ATraceOfContenders_IsPlacedInTheBuilder`.

| The branch uses                                                                                         | Replacement                                                                                                                                |
| ------------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------ |
| `Aspect.Observe(evaluator, performance.Observe)` for each contender                                     | `algorithm.Substitute(evaluator.Invocations.Observing(performance.Observe))`, in a graph racing builds itself and never persists           |
| `problem.CreateIterationUpdateAspect(algorithm)`                                                        | `algorithm.ApplyingIterationUpdates()`, placed by racing around what the builder returns or by the builder                                 |
| `Scope.With(aspects)`                                                                                   | No replacement needed: the graph already holds the wrappers before it is resolved                                                          |
| `.AddAspect(qualityCurve).AddAspect(bbcp)` on a shared evaluator (`AutoEcPaperScenarioExecution.cs:63`) | `var analyzed = evaluator.AnalyzedBy(qualityCurve, bbcp)` with `evaluator` typed by its interface, before `AlgBuilder` captures `analyzed` |
| `CreateForkedScope`, `WithContext`, `ExecutionFactory.Forkable`                                         | The branch's own feature, which the merge moves onto the [reduced scope](#phase-4-reduce-the-scope)                                        |

What the merge has to keep in mind:

- **D5 construction binds the problem.** Typed scope views take the problem object; direct algorithm resolution takes it during construction, and the executor's invocation omits it. Snapshot/callback rebinding and root-fork result ownership must be integrated with these bindings using the branch's actual mechanisms; copying bound executor references is not a compatibility solution.

- **Lifetimes differ.** HEAD keeps a contender's performance across iterator restarts, and the branch creates a new `ContenderPerformance` for each race. A new performance object means a new graph, and a restart reuses the graph it has.
- **`AlgBuilder` keeps getting the unsubstituted algorithm.** Challengers are built from `entries[0].Algorithm` (`DynamicRacingAlgorithm.cs:189`), which would otherwise leak the incumbent's tracking into them. A test pins this.
- **An outer analyzer does not reach contenders (D3).** A reference target reports the delegate by name. A selection by type or predicate passes over contenders without an error. The author of `AlgBuilder` substitutes inside it, or analyzes a shared operator before capturing it, as in the table.
- **Racing uses `Substitute`, not `AnalyzedBy`.** Racing is content, which must not reference `Analysis`; `Observing` is in `Inspection`.
- **The selected evaluator must be one the contender holds.** It is matched by reference, so an evaluator the contender does not hold fails with D17's error. When racing wraps a contender for iteration updates, the evaluator inside the wrapper is still found.
- **An observation reads the problem at its interface.** `EvaluationObservation.Problem` is an `IProblem<TCandidate, ISearchSpace<TCandidate>>`, which carries `Objective`.
- **Iteration updates need wrapped contenders.** Under `AfterEachIteration`, the epoch loop ends only if each contender applies the updates, so a contender left unwrapped makes the race spin.

### Phase 4 fork compatibility inspection

Inspected the local remote-tracking `origin/dynamic-Racing` at `c35ebf82062c78b3d8269892632ee7ed0adfdce1` on 2026-11-11; this is a source inspection, not a merged or executed compatibility test.

- `ResolutionScope.CreateForkedScope()` calls `state.Fork()` and retains the original parent and observation context. `State.Fork()` rejects construction in progress, creates a peer state with the same ancestor, copies only local factories whose target implements `IForkableFactoryPreparation`, and memoizes each copied delegate by reference. Non-forkable local factories and advice factories are prepared afresh; ancestor state stays shared.
- `ExecutionFactory.Forkable` separates persistent state from execution bindings: its copy function copies the state, and its binding function constructs fresh executions and resolves fresh child bindings. For example, `DynamicRateMutator` copies `SourceProblem`, `LastObservedEpoch` and `RemainingResponseCalls`, then binds a child mutator through the supplied scope. Current executors own both private data and resolved child references. Copying their table references would share mutable state; constructing replacements without a snapshot would lose the learned state. An executor snapshot contract and child rebinding therefore remain merge work, not a feature phase 4 delivers.
- `WithContext(context)` retains prepared state while replacing the observation context through `context.WithState(state)`; existing executions keep their children and advice, and resolving again rebuilds those bindings. Racing's `Entry.Restart` uses this before placing the new race's performance callback. Current repeated resolution returns one executor per configuration reference in its owner, with wrappers already in the graph. Changing that graph does not rebind an existing executor or its callback. The merge must reconcile contender restart tracking, configuration reference identity, explicit sharing and retained executor children; this inspection does not accept a second resolution mechanism.
- The branch's fork predates run-bound `RunResults`. Child scopes in this branch share one result table and stopwatch, and result nodes can live in the root executor table. A peer child fork still needs to preserve the run's results and D29's precise sharing guard without treating published results as private snapshots. Root forks additionally need an explicit result-ownership decision: copying live entries or a `RunResults` reference would couple independent runs and retain the original root used for result lookup. No root-fork result policy is implemented or implied here.
- Signature checks remain on every typed `Resolve` (D61), including contenders created by `AlgBuilder`; declared-child preflight and `ChildExecution` mappings remain intact. Tree-wide construction failure remains D62. Fork integration must preserve those contracts as well as the branch's rule to fork only between invocations. D63 removes hidden timing wrappers and leaves the scope responsible for neither root timing nor overlapping-call accounting.

The scope's executor table, parent links and sharing ownership remain usable foundations for integration. Factory snapshots and observation-context rebinding cannot be merged mechanically. No fork implementation or compatibility guarantee is claimed.

## Relation to the aspects plan

[Aspects as automatic composition](aspects-as-automatic-composition.md) was used as a checklist, not as a specification.

| Aspects plan                                                        | Under substitution                                                                                         |
| ------------------------------------------------------------------- | ---------------------------------------------------------------------------------------------------------- |
| Governing contract: every aspect behavior is expressible explicitly | Fulfilled by construction: the automatic path produces the explicit graph                                  |
| Equivalent behavior of both paths                                   | Holds because there is one path; tests compare a substitution with a hand-written graph by value           |
| Expansion exists only in the execution graph                        | Superseded: the expansion is in the configuration graph                                                    |
| `Counted`, `Timed`, `Observed` as one wrapper per concern           | Kept. Their advice forms go                                                                                |
| Trace collection separated from attachment                          | Kept and completed: `CreateAspect` goes                                                                    |
| Observation attaches to executor calls; Cycle turns are inner calls | Kept. A substitution wraps inner algorithms, subject to D10                                                |
| Results bound to the run, one per definition                        | Kept                                                                                                       |
| Attachment scope, duplicates and ordering                           | Replaced: placement is where the substitution puts it and order is visible nesting, except as listed below |
| Required evidence table                                             | Kept as acceptance scenarios, with "both paths" meaning the substitution and a hand-written graph          |
| Wrapper terminology                                                 | Settled (D13): wrapper stays, and `Substitute` places wrappers                                             |

## What does not simply disappear

The reports describe this design as removing rules. These stay in another form:

- **A clock ticks once, and before the observations that read it.** Today this is advice deduplication with closest-occurrence ordering. Placing at the original reference would be wrong for a second trace on the same target: it would sit inside the first trace's counter and read the clock before that call was counted. Within one call D28 settles it through the listed order, with the clocks after the traces. Each call places what it lists, so a clock shared across separate calls ticks twice ([D36](#decided-in-round-21)).
- **Persistence.** A substituted graph is persistable only if the wrappers it adds are data. `Trace(observation => ...)` holds a delegate and `OperatorDuration` a `TimeProvider`, so instrumented graphs stay under the documented exception until [value strategies](developer-backlog.md#runs-execution-and-experiments) exist for them. The configuration before placement stays clean, and a substitution can be reapplied after loading. Instruments and shared nodes also still need an identity in a persisted form.
- **Reach.** Advice reaches anything resolved below it, including configurations a delegate creates at run time. A substitution reaches only declared children (D3). It also changes only the graph it returns. Today a budget counts every call of its operator in its scope and child scopes, including a direct request for the original algorithm (`DeferredAlgorithmResolutionTests.cs:294`), and the budget and that request use one algorithm executor (`ConfigurationSharingTests.cs:118`). With eager substitution the budget holds a copy of the algorithm, so a direct request for the original neither counts nor shares that executor. This follows from D1.
- **Declaration cost.** Avoiding reflection means every configuration author declares parameters, and a declaration can be forgotten. Analyzers in `analyzers/` are `IsPackable=false` with `PrivateAssets=all`, and the project files contain no packing entry, so external authors do not get a completeness analyzer today (D2). The developer accepts this for now. Its overhead is to be measured, and the mechanism designed in more detail, before the choice is final.

The clock order above was settled in [round 21](#decided-in-round-21).

## Evidence required

Phase 1 passes only if all of this holds without new flags, casts or adapters. Implementation deviating from the accepted design returns to discussion.

- The logs of S6 to S9 and Q2 to Q6 in the [system spec](operator-unification-system-spec.md) match the current implementation, or the developer accepts each difference. A paper walk-through of S6, S8, Q2, Q4 and Q5 on 2026-11-05 raised the clock ordering above and the `Share` guard in D8; nothing has been executed.
- Substitution produces a graph equal by value to the hand-written one, and a seeded run yields the same states with and without counting and observation.
- Each scenario in the [aspects plan's evidence table](aspects-as-automatic-composition.md#required-evidence-before-completion) holds, including two traces sharing a clock.
- These hard cases work: racing with an outer trace, a concretely typed slot, a selection by type that also selects a wrapper from an earlier substitution, children hidden inside a consumer's operator, a multi-operation `Neighborhood`, a consumer-defined multi-operation operator whose operations keep one executor with and without `Sharing`, an external operation with counting and observation, an adaptive mutator under `Sharing`, a problem-specific operator, a cache shared across turns with a budget inside and outside it, two budgets with distinct targets resolved in one scope, nested budgets, several substitutions matching one node including one typed by a concrete class, an analyzer with ordered hooks such as `RankAnalyzer`, an analyzer with hooks on operators and on the algorithm containing them such as `GenealogyAnalyzer`, an analyzer on the receiver's own iterations, and two traces sharing a custom clock that observes, as `CrossoverCallClock` does (`TraceCompositionTests.cs:498`).
- Benchmarks show no hot-path change, and preparation cost is measured separately.
- Graphs built by substitution run with no aspect in any scope, apart from racing's own contender advice until phase 3 ([D27](#decided-in-round-7)). Removing the ordering, duplicate, adder, guard and failure rules is the proof of phase 3 step 6 and phase 4.

### Evidence collected so far

Measured on 2026-11-06 with everything in the pilot's scope in place. Differences and open items are listed below.

- **Benchmarks.** A throwaway Stopwatch program against this working tree and against commit `2b0d874b`, medians of 9 rounds, repeated on one machine; it is not in the repository. The hot path does not change beyond noise: a no-op evaluator call takes 0.3 ns bare, about 8 ns counted and about 22 ns traced, through advice or substitution and before or after the pilot, and about 28 to 32 ns traced with a clock on all of them. A whole GA run, 100 candidates over 200 generations, takes the same 5.4 to 6.3 ms with advice and with substitution. Preparation: `Substitute` for a trace with a clock on a GA takes about 10 µs. A complete run of a two-candidate GA takes about 5 to 6 µs bare at the commit and 6.5 µs now, and about 8 to 9 µs with advice at the commit, against 11 µs with advice and 12 µs already substituted now. So preparation grew by about 1 µs for a bare run and about 3 µs with advice, which is the price of the coexisting paths until phase 4 shows otherwise. One of the three runs of the current code gave 35.6 µs bare and 22.7 µs with advice, in its first two preparation measurements; these outliers are excluded from the ranges. The bare whole run is left out of the comparison, because it was always measured first and warm-up made it the slowest.
- **No aspects.** The new budget, `Counted`, `Observed` and analyzers' substitutions call no `AddAspect`. The callers left in `src` are the old budget, the duration budget, `AdvisedAlgorithm` and racing's contender advice, as D27 expects.

- **Logs.** S6 versions 1 to 3 and S9 parts 1 and 2 match on the new budget (`SubstitutedBudgetTests`). S8 and Q2 to Q6 match through analyzers' substitutions (`SubstitutedAnalysisTests`), with the differences below. S7 needs nothing: the developer removed `ReuseExecutors`, and `cycle.Sharing` takes its place in the cases above and in S8. Graphs from `Counting`, the budget and a trace equal the hand-written ones by value, and seeded runs yield the same states with and without counting, budgets and observation.
- **Aspects table.** Counting, ordering with retry, tracing with retention and clocks, internal progress including a turn that yields no state, deferred construction, shared execution, separate termination, independent runs and an external operation each hold through both paths. Timing holds as a graph equal to the hand-written `Timed`, whose measurements `InstrumentPlacementTests` already compares. Shared clocks hold in one call in either order, and across separate calls they tick twice, the gap D36 accepts.
- **Further differences, each expected.** Q4's same counter written twice in the slot counts 6, because that is what was written. Q4's case 3, a target the graph does not hold, is now D17's error instead of a count of 0. An outer trace no longer reaches racing's contenders (D3): the substitution reports the delegate by name, and a `Substitute` inside `AlgBuilder` records what the outer trace records through advice. An observation's operator field holds the wrapper's child, such as `Counted(O)`, where advice gave `O`. A budget's narrowed counter used as a clock reads the root's count, which stays 0.
- **Clock position follows the named target.** After `a.Counting(m, out c)`, a trace with clock `c` on the original `m` gives `C(T(O))` and reads the count before the call, while a trace on `counted.Mutator` gives `T(C(O))` and reads it after.
- **Differences from the old budget, both expected.** A direct request for the original algorithm neither counts nor shares the budget's executor (D1, the [reach](#what-does-not-simply-disappear) note). A budget on `m` inside a shared cache now gives its caller a private copy of the cache, which counts, where the old budget saw nothing (D9): the same configuration yields the states `A1 A2 B1 A1 A2 B1` and 5 calls to `m` instead of the states `A1 A2 A3 B1 A1 A2 A3 B1` (`ConfigurationSharingTests`).
- **Hard cases that hold.** A concretely typed slot, a selection by type that also selects an earlier wrapper, children hidden inside a consumer's operator, a consumer-defined multi-operation operator with one executor with and without `Sharing`, a multi-operation `Neighborhood` with each operation counted, an external operation with counting and observation, an adaptive mutator counted and under `Sharing`, a problem-specific operator, a cache shared across turns with a budget inside and outside it, two budgets with distinct targets in one scope in either order, nested budgets, several substitutions matching one node, `RankAnalyzer` and `GenealogyAnalyzer` recording what they record through advice, an analyzer on the receiver's own iterations through an interface-typed receiver, and two traces sharing a custom clock that observes.
- **D32 routes.** They were enough for the budget and `Counting` on operators: a hook after a budget finds a reference still in the copy, and a wrapper whose child an earlier call changed works ancestors first or named from the current configuration. One call takes `Substitution.Counting` ([D43](#decided-in-round-14)). An algorithm boundary on a concretely typed receiver gives D10's error, and works through the interface-typed receiver `Counting` returns or a variable typed by the interface.
- **D33.** The default and the comparers behave as decided. The rough edge with a concretely typed target is closed by [D37](#decided-in-round-13).
- **D2.** A declaration takes about 3 lines per parameter, plus 2 lines for `Parameters` and 1 for `GetParameters`: `GeneticAlgorithm` grew by 38 lines for 11 parameters and `EvolutionStrategy` by 34 for 10, a one-parameter wrapper by 7. `new` on `Parameters` is an error-prone detail: it depends on whether a base declares one, and the compiler warns either way (CS0108, CS0109).

Casts and adapters, which the exit criterion above names: `Wrap` casts its untyped executor (D38), and resolution has already checked the operation. `ObservationAdvice` and `AlgorithmObservationAdvice` are internal adapters that let the `Advised` path reuse an observation's `Wrap`, and D34's old counter is the third. These adapters serve only the old path and go with it in phase 3. The new path has no flag and no adapter.

Other changes the plan did not name:

- HLib0003 reported `with` initializers, which the D24 form needs, as mutations. It now exempts only top-level assignments in object and `with` initializers.
- D29's "resolved here or below" must not retain what a discarded child scope resolved, as two existing lifetime tests pin, so each scope keeps it in a `ConditionalWeakTable`.
- A cycle's `Algorithms` gains `init`, so `CreateExecutor` now rejects an empty list as § 4.7 requires. `TAlgorithm` gains a `class` constraint until D10 removes it.
- `[OverloadResolutionPriority(1)]` on the `Instrument` overload of `Result`, because an old counter is both an `Instrument` and a node. It goes with the old counters.
- `CountedAlgorithm<TCandidate, TSearchState, TObservation>`, the counting wrapper of an algorithm boundary, sits beside the old `CountedAlgorithm<TCandidate, TSearchState>` until that one goes.
- A source's `RecordingInto` is abstract, and its implementations for a source from the old advice factory and for a role source throw and name `Advised`. Phase 3 step 4 settles role sources, whose explicit wrappers still work.

Temporary inconsistencies while old and new coexist:

- `docs/guide/extending/operator-composition.md:241` shows the multi-target budget, which now needs the `.Obsolete` namespace.
- Tests importing the old types need the `using` inside their namespace (D23), which `dotnet format style` reports as IDE0065 in 7 places. It is the only style finding, and it goes with the `.Obsolete` namespaces.
- `ObservationSource.Create` keeps its advice-factory form beside the new one until phase 3, so an observation that is a plain value such as `Action<int>` goes only through the old form. Under the new form an observation is an authored type implementing `IInvocationObservation`.

## Open decisions

### Decided in round 1

Decided by the developer on 2026-11-05.

- **D1. A substitution happens eagerly,** before the run, so that the configuration the caller holds is what runs. An intent node that expands when built is not used. The developer confirmed this in round 2. A copy such as `budget with { Algorithm = other }` therefore loses the counter on the new child, so the budget never triggers. Reporting that is a candidate for the [configuration graph check](developer-backlog.md#scenarios-awaiting-a-concrete-need).
- **D2. Parameters are declared by hand for now.** The detailed design, after the overhead is known, decides whether a completeness check, analyzer or generator is added. Metadata is mechanical, unlike the [generated operator families](developer-backlog.md#generated-operator-families) rejected earlier, so a generator would need a fresh explicit decision, and an analyzer or generator must ship in the NuGet package so that external authors are not disadvantaged. The risk to weigh: a forgotten child declaration makes a predicate substitution skip that subtree silently, while a placement by reference fails loudly.
- **D3. Sequential substitutions are deterministic, and a substitution stops at delegates.** The output of the first substitution is the input of the second. A substitution reaches declared children, including a Cycle's inner algorithms. It does not look into what a delegate creates, such as racing's `AlgBuilder`, and whoever writes the delegate can call `Substitute` inside it. An outer trace then stops reaching contenders automatically, so Bernhard Werth's `origin/dynamic-Racing` is affected and should be consulted first.
- **D4. Executor controls are replaced in phase 5.** `MutationStrength` is an explicit configuration node, not an operator, and is given to both the mutator and the strategy. See [the proposal](#executor-control-proposal).
- **D6. The design rules are in [developer guidelines](../docs/contributing/developer-guidelines.md) § 2.5,** aligned with §§ 2.1, 2.4, 3.1 and 3.6 instead of repeating them. Rule 5 was folded into those sections. See [the rules](#adopted-design-rules).
- **D7. The proposed statuses of ideas 7, 8, 9, 10, 11, 13, 14, 17 and 18 are accepted,** as the [status table](#idea-status) shows.

### Decided in round 2

Decided by the developer on 2026-11-05.

- **D9. A substitution target inside a shared configuration is a wrong configuration.** A budget targeting `evaluator` inside a shared `cached` produces a private copy of the cache. Report a `Sharing` entry that its subtree never uses only if that stays one walk over declared parameters with no new mechanism. Otherwise leave it to the [configuration graph check](developer-backlog.md#scenarios-awaiting-a-concrete-need).
- **D13, the settled names.** `Analyzer` stays, and it replaces `Instrument`, which the glossary already uses to define it. `Instrument` keeps its name until the type is removed. `Fits` becomes `Supports`, as § 8.13 suggests and `SupportedExecutionDescription` already reads, because `Fits` is easy to confuse with scikit-learn's `.fit`. `Rewrite` is the wrong word; see [D13](#decided-in-round-5).
- **D14. Selection has a core version and extensions.** The core does the main work, and extensions cover common patterns. Which patterns get extensions is decided during implementation.
- **D16. An unset `Strength` slot means no adaptation.** Detecting a shared mutator with an unshared strength stays out, because it would complicate things too much. See [the proposal](#executor-control-proposal).
- **D17. Several substitutions at once.** Nothing stores pending substitutions, because a stored list would rebuild the aspect list. One call may take several, which apply one after another (D28). A substitution copies every ancestor of what it changes, so within one call a node stays itself for matching while only its children change. With `ga.Evaluator = Cached(E)`, `ga.AnalyzedBy(countE, best)` therefore works, although the first substitution copies `Cached` and `best` targets the original. `GenealogyAnalyzer` needs this too, because it hooks operators and the algorithm containing them. Separate calls carry no such memory, so `ga.AnalyzedBy(countE).AnalyzedBy(best)` no longer finds `Cached`. A placement by reference that matches nothing is an error naming the target and any delegate boundary it could not enter, while a predicate that selects nothing returns the same instance.

### Decided in round 3

Decided by the developer on 2026-11-05.

- **D10. `CycleAlgorithm` and `PipelineAlgorithm` lose `TAlgorithm` in phase 2.** What their algorithms must share is a compatible search state, which `IAlgorithm<TCandidate, TSearchState>` already requires. Their executors resolve through that interface and `Algorithms` has no setter, so the parameter only gives typed reads such as `cycle.Algorithms[0].Terminator` (`InferenceConstructionSpecs.cs:152-158`). The developer accepts losing those reads, and the parameter can return if a need appears. One `Create` and one pair of `CycleWith` overloads remain. `TSelf` on the algorithm bases stays, because `AsGrid` and `Repeat` need it, contrary to the report. Other concretely typed slots, including a consumer's, get a clear error naming the slot.
- **D13, aspect-oriented terms.** If suitable terms are found, these terms leave the API completely. The documentation keeps them as a bridge for readers who know aspect-oriented programming.

### Decided in round 4

Decided by the developer on 2026-11-05.

- **D13, what leaves the API.** `Aspect` goes.
- **D18. A meta-algorithm gives a child another problem through a view.** `scope.For(problem)` returns a view of the same scope, as today's typed views do. The developer pointed out that a child scope would break default sharing: child scopes build independent executors, so one `MutationStrength` used by both children would silently become two. A scope decides lifetime and a problem decides binding, and the two stay separate. The code already separates the view from its storage: each public `ResolutionScope` holds a private `Node` that contains the table (`ResolutionScope.cs:18,241`), so no new concept is needed. Search space and objective stay fixed for the lifetime of a problem object, and a different search space means a different problem object. An operator executor is not shared across problems, which the developer accepts for the regular case. If an operator's private data must survive a change of problem, that case uses one dynamic problem instead, and gives up the assumption that the problem stays immutable. See [the conditions](#problem-bound-executor-conditions).
- **D19. A configuration node creates an execution node.** `IConfigurationNode<TExecutionNode>` produces an `IExecutionNode`, which is the general term. `MutationStrength` shows that the result need not be executable. The glossary lists `Execution node` as a legacy name for executor, so it becomes canonical again with this wider meaning, which matches the existing term execution graph.

### Decided in round 5

Decided by the developer on 2026-11-05.

- **D13, the remaining names.** `Substitute` is the general mechanism for graph rewriting, and analyzers and budgets use it under the hood. A predicate on a node is the base form, and there is no `NodePattern` type. Passing a reference such as `ga.Mutator` is a shortcut that matches by reference equality. `Substitution` replaces `Aspect`. `ga.AnalyzedBy(analyzer)` is the provisional analyzer form, like `TerminatedBy`. Not chosen: `Rewrite`, `Advised` and `Weave`, which the developer rejected; `Replace`, `Select` and `Transform`, which the library already uses for roles; `Decorate`, a second verb that a complete replacement does not fit; and `SubstituteMatching`, which says nothing the arguments do not.
- **D19, the remaining shape.** `IExecutor : IExecutionNode`, and both stay. `IExecutor` is the common base of `IOperatorExecutor` and `IAlgorithmExecutor`, and `IOperator.CreateExecutor`, `Operator<..., TExecutor>`, wrappers and invocation signatures name it. Only `IConfigurationNode<T>` and the scope's table and `Resolve` constraints change, rather than 301 occurrences. `IConfigurationNode<TExecutionNode>.CreateExecutionNode` replaces that interface's `CreateExecutor`. Operators and algorithms do not implement that interface, so their `CreateExecutor` hooks keep their name. Role names stay `MutationExecutor` and so on, because the glossary defines execution as the activity and executor as the object that performs it. Leftovers such as `OperatorExecution1` in `CompositeSearchSpace.cs:36` are an existing inconsistency.

### Decided in round 6

Decided by the developer on 2026-11-05.

- **D13, the core form.** `Substitution` bundles what to match with its replacement, and `node.Substitute(substitutions)` is the form everything builds on.
- **D20. Selecting by type starts with the lambda's parameter type,** as in `ga.Substitute((IOperator<Evaluation, RealVector> evaluator) => ...)`. An explicit type argument does not compile with a generic receiver, because C# applies explicit arguments to the extension's and the method's type parameters together, and it has no partial inference. The developer expects users to try `.Substitute<Evaluator>(e => ...)`, so call sites get another look during the work under D14. One known option: `ga.Substitute<Evaluation>(evaluator => ...)` compiles as an instance member on a base that has `TSelf` and `TCandidate`, which covers algorithms but not operators or receivers typed by interface. A string name is a candidate shortcut for `INamedConfiguration`.
- **D21. No `SubstituteCounted` for now.** A counter created with `ga.Mutator.Invocations.Count()` already holds its source, so a shortcut would name the target twice. Whether that creation syntax stays is D25.
- **D22. An analyzer is what an aspect was:** a collection of substitutions that must be installed so that the hooks an analysis needs are placed. `GenealogyAnalyzer` is the example, because it needs hooks on every crossover, mutator and iteration source (`GenealogyAnalyzer.cs:57-82`). A public `Substitutions`, abstract under [D42](#decided-in-round-14), replaces `CreateAspect(result, results)` in phase 3. It needs no run data, because the wrapper looks the result up when it is built, as `Counted` does today (`Counted.cs:24`). An analyzer built on others, such as a trace with clocks, lists their substitutions too.
- **D23. The pilot is additive on this branch,** because `Substitute` has to reach real nodes. `IConfigurationNode` gets an instance member returning the parameters, with an empty default as `Fits` has, so a node without a declaration is a leaf until phase 2. When a new name clashes with old code that will be deleted, the old code moves to a temporary `.Obsolete` namespace and the new code gets the real name. This applies to the budget, because `LimitedToInvocations` stays in use until phase 3. `Counted` and `Observed` stop using advice in place, through the existing `operation.Wrap`. `OperationInfo.For` must become public for this (`OperationInfo.cs:41`), because external wrappers would otherwise lose the path built-in ones use.
- **D24. One static member per parameter,** and one static `Parameters` collection of them, as [Parameters](#parameters) shows. This replaces a single fluent declaration. Each parameter is then a typed handle that later phases can use, at about twice the lines per node. The collection holds only a record's own parameters, and the one instance line that walks need also adds the inherited ones. A derived record's collection hides its base's with `new`.

### Decided in round 7

Decided by the developer on 2026-11-05.

- **D27. The pilot proves independence from interception.** Under D23 the old resolver stays, and racing's contender advice stays until phase 3 (`DynamicRacingAlgorithm.cs:386`), so the pilot cannot remove the ordering, duplicate, adder, guard and failure rules. It proves that graphs built by substitution do not depend on interception, and the removal stays the proof of phase 3 step 6 and phase 4.
- **D28. Substitutions apply one after another.** Each sees the result of the previous one, so a substitution that replaced a node completely leaves nothing for a later one to match. A later substitution on the same original sits closer to it: `O` with `A` and then `O` with `B` gives `A(B(O))`, because `B` matches `O` and not `A`. `B(A(O))` takes the reverse order, or a `B` that matches `A`. No rule about result types is needed, because a replacement only receives what it matched. This reverses today's order, where the first listed wraps closest (`ResolutionScope.cs:221-224`). The reversal follows from the model: today's order works because the resolver remembers the original and adds outer wrappers around it, and with eager substitution anything other than a later match sitting closer would be confusing. So `TraceAnalyzer.cs:49` and `RankAnalyzer.cs:69` swap their hooks: an analyzer lists its own hook before the clock or graph hook that must run closer to the call. A `Substitution` instance listed twice applies once, at its last position, so two traces sharing a clock in one call count once and both read it after the call was counted. An analyzer therefore keeps its `Substitutions` stable.
- **D29. `Share` gets one precise guard, tried in the pilot.** D8's counter node fails for two budgets with distinct targets resolved one after the other in one scope, in either order. Resolving the first budget's shared counter sets `HasChildScopes` (`ResolutionScope.cs:209-211`), and the second budget's `Share` then throws (`ResolutionScope.cs:93`). Today's budgets coexist there, because they only allocate an accumulator and add advice (`OperatorCountBudgetAlgorithm.cs:92-94`). The pilot replaces both `Share` guards with one rule: sharing is an error only if this scope or a scope below it has already resolved the configuration. A new counter can then be shared at any time, with no construction-order coordination, and a budget does not become a sharing boundary (`ConfigurationSharingTests.cs:116`). D8 and D12 still fall at the end of the pilot.

### Decided in round 8

Decided by the developer on 2026-11-05.

- **D26. Multi-operation nodes under substitution.** One configuration can perform several operations with one executor, which public specs rely on (`OperatorPreparationSpecs.Sharing.cs:19,42,104`), and `Neighborhood` hands itself out for all three move operations (`Neighborhood.cs:32-34`). Advice on one operation leaves the others alone today (`MoveRoleResolutionTests.cs:27`). Two conditions in the walk keep this working, with no new type or call. A substitution leaves alone a slot typed for another operation, with neither type assignable to the other, and that slot keeps the reference, so all operations still reach one executor through the scope's table. A `Sharing` entry keeps naming the original while the replacement still contains it, and otherwise follows the replacement. A wrapper is then built per scope around the shared executor, as advice is today (`ResolutionScope.cs:81-82`), and a complete replacement stays shared. `SharingAlgorithm.Shared` needs one more parameter kind, a reference list, because a complete replacement has to update it anyway. If the `Sharing` condition does not work out in the pilot, it is removed and a shared multi-operation node with one wrapped operation is documented as a wrong configuration, as D9 does.
- **D30. One counter node ([a class under D39](#decided-in-round-13)).** A counter becomes a configuration node with an execution node holding the count, in the form of `MutationStrength`, and is tested in the pilot. It stays in the graph as a reference, so the wrapper, the terminator and a `Sharing` entry that hold it show who uses which counter. It implements `IConfigurationNode<CountAccumulator>`, and wrappers and terminators call `scope.Resolve(counter)` instead of going through `scope.Results` (`Counted.cs:24`, `AfterOperatorCountTerminator.cs:29`). Its lifetime differs from `MutationStrength`, which gets one object per scope: a count read after the run must span the run, and a budget's must restart each turn (D8). A public marker therefore makes the run's root scope the default owner, one check in `Owner` (`ResolutionScope.cs:180`), and a budget narrows it with `scope.Share(counter)` in its own scope, because the nearest sharing scope wins. `run.Result(calls)` then reads the root scope's table, so the separate table in `RunResults` can go by phase 6. The pilot's budget and `ga.Counting` use this one record, which also settles D8 if it holds. If it does not hold, the fallback is two counter kinds, an `Instrument` for analysis and a node for budgets.

### Decided in round 9

Decided by the developer on 2026-11-05.

- **D31. `ga.Iterations` stays as a passive argument.** `ga.Counting(ga.Mutator, out var calls)` takes over the part of a source the developer doubted: it no longer hands out a counter that was never installed. A source has three other jobs. It names a boundary, and an algorithm has two, `Iterations` and `Calls`, so the target alone does not say which is meant. It carries the observation type that analyzers are written against, so `TraceBestSoFar` only accepts evaluators and `TracePopulationQuality` only iterations (`BestSoFar.cs:73`, `PopulationQualityTraces.cs:9`). It is also how an external role gets the same instruments (`ObservationSource.Create`, `PointcutSpecs.cs:278`). A source therefore installs nothing and loses `Count`, `CountCandidates` and `Duration`, with `Trace*` following under D25. An operator keeps the shortcut of passing the slot, because it has one boundary per operation: `ga.Counting(ga.Mutator, out var calls)` and `ga.Counting(ga.Iterations, out var iterations)`. Dropping sources would take one method per instrument and boundary, which multiplies over a hidden axis, and an external role would have to write its own. An algorithm boundary may wrap the receiver, so its overload returns `IAlgorithm<TCandidate, TSearchState>`, and the compiler rather than D10's error reports this. That loses no more than `Advised` does today (`AdvisedAlgorithm.cs:53`). The receiver's `Interceptor` slot would keep the concrete type, but only iterative algorithms have one (`IterativeAlgorithm.cs:21`), so everything else would still need the wrapper. [D32](#decided-in-round-11) settles how this chains.

### Decided in round 10

Decided by the developer on 2026-11-05.

- **D33. The overload taking a configuration also takes an equality comparer.** This follows the .NET convention of `Contains(value, comparer)`. `Substitute(target, replace, comparer)` and `Substitution.Of(target, replace, comparer)` select every reachable node the comparer considers equal to the target, and unlike a predicate they report an error when there is none. `ReferenceEqualityComparer.Instance` and `EqualityComparer<IConfigurationNode>.Default` work as they are. The parameter is `IEqualityComparer<IConfigurationNode>`, because a comparer typed over the target's type lets a wider comparer such as `ReferenceEqualityComparer` widen the inferred `TNode`, and the lambda then no longer sees an operator. Shortcuts such as `Counting` take no comparer, so the parameter does not spread across them, and a caller who needs one uses `Substitute`. [Round 11](#decided-in-round-11) settles what leaving it out means. The pilot checks whether it makes sense for users.

### Decided in round 11

Decided by the developer on 2026-11-06.

- **D33, the default. Leaving out the comparer means reference equality.** A user who supplies an operator means that exact object, and a user who wants another notion asks for it. The parameter is optional and `null` means reference equality, because `ReferenceEqualityComparer.Instance` is not a constant and cannot be a default value, so the documentation has to state it. This differs from the usual .NET default on purpose. Slots left at their defaults are value-equal even in differently configured algorithms, so with value equality `cycle.Counting(gaA.Evaluator, out var c)` would also count `gaB`'s evaluator, which the current reference matching rules out on purpose (`Pointcut.cs:12`, `PointcutTests.cs:8`). Shortcuts such as `Counting` therefore always match by reference. In a predicate, `op == x` is a reference comparison when `op` is typed by an interface, which is the usual form under D20, because a record's `==` is defined on the concrete record. C# warns only when one side has a concrete type (CS0252). Value equality in a predicate takes `op.Equals(x)` or a parameter typed by the concrete record. The comparer form also reports an error when nothing matches, which a predicate does not.

- **D32. A reference that an earlier call copied stays an error, and the pilot adds nothing for it.** Under reference equality (D33), a reference stops matching once an earlier call copied it. A substitution copies only the path from the root to what it changes, and every other slot keeps its reference. `ga.Substitute(ga.Mutator, ...).Substitute(ga.Evaluator, ...)` therefore works, because `ga.Evaluator` is still in the copy. Naming the same wrapped node again, or something inside a wrapped node, works too. A name fails when something below it changed: `ga` and `ga.Iterations` after any change, because the receiver is an ancestor of every node, and `ga.Evaluator` after a change to `E` when that slot holds `Cached(E)`. A node replaced completely also fails, rightly. Every failure is the error D17 requires and never a wrong graph, and the reverse order builds the same graph. The realistic cases are an algorithm boundary after an operator hook, a hook after a budget and a nested algorithm. Which references go stale depends on the shape of the graph and on what ran before, which is the temporal coupling the developer guidelines say to try to remove (`developer-guidelines.md:71`). Reference matching guards this coupling and does not remove it, which the developer accepts for now to keep the design simple.

  The error names the three routes that already exist. One call resolves every target against the same input (D17), which keeps D17's map from copy to original for the length of the call, and a boundary on the receiver itself needs a receiver typed by the interface (D10). Ancestors first works whenever the ancestor is wrapped rather than replaced. Naming the target from the current configuration, as in `a.Counting(a.Iterations, out var i)` or `limited.Counting(limited.Algorithm.Iterations, out var i)`, works as far as typed slots reach. It names what is there now, so naming `a.Mutator` gives `Timed(Counted(M))` where `ga.Mutator` gives `Counted(Timed(M))`. An analyzer names its targets when it is created, so `ga.AnalyzedBy(countE).AnalyzedBy(best)` on `Cached(E)` takes one call or the reverse order.

  The pilot's budget and `ga.Counting` show whether these routes are enough. If they are not, a selector such as `.Substitute(g => g.Mutator, ...)` is the first addition. The developer likes the form and it compiles against a stub, but every shortcut would need it too, it needs a cast to pass an interface-typed slot, and it does not help an analyzer. A comparer for "equal once added wrappers are ignored" remains possible after that, with no new overload because D33 gives it a place. It bends the comparer contract, because that rule compares in one direction, it also selects an equal twin, and shortcuts could not use it. The [rejected alternatives](developer-backlog.md#finding-a-configuration-copied-by-an-earlier-substitution) are in the backlog.

### Decided in round 12

Decided by the developer on 2026-11-06, from conflicts the pilot's implementation found.

- **D29, the one assertion it changes.** `ResolutionScopeTests.ChangingAScopeThatHasChildScopes_IsAnError` asserted the `Share` guard D29 removes, which conflicts with keeping old assertions unchanged. Only that assertion changes, and the test pins D29's rule instead. `AddAspect` keeps its own guard.
- **D26, what "keeps the reference" means when a child changed.** A slot typed for another operation keeps the node, with its children substituted. With `Both(M)` in the mutator and crossover slots, wrapping every mutation node gives `Mutator = W(Both(W(M)))` and `Crossover = Both(W(M))`, with one `Both` copy in both slots, so the scope's table still gives them one executor. Keeping the literal original would give two configurations and two executors, which is what D26 prevents.
- **D34. Old counters reach the changed `Counted` and terminator through the node interface.** `Counted` and `AfterOperatorCountTerminator` change in place, and their counter property is typed `IConfigurationNode<CountAccumulator>`. The old abstract `Counter` moves to `Inspection.Obsolete` under D23, and implements that interface with `scope.Results.Result(this)`, so old counters keep their run-wide result. `Metric` moves onto the wrapper, and the overload taking an `OperatorCounter` passes its metric. This is a small adapter the plan did not name, and it goes with the old counter.
- **D35. `CountAccumulator` moves from L0 to L3.** D30 makes it implement `IExecutor`, which L0 cannot depend on. Only L3 types use it. `DurationAccumulator` stays in L0.
- **D36. Two traces sharing a clock are installed in one call during the pilot.** One call gives `T1(T2(C(O)))` under D28. Separate calls give `T1(C(T2(C(O))))`, which counts twice, so the aspects table's "in either attachment order and scope" does not hold across separate calls. This is accepted for now and recorded as a known gap. No placement helper is built, and [the clock rule](#what-does-not-simply-disappear) is discussed again when placement is implemented.

### Decided in round 13

Decided by the developer on 2026-11-06, from the review of the observation wrappers and from questions the pilot raised.

- **D37. A reference target may be typed concretely.** `Substitute` and `Substitution.Of` get overloads for a target typed `IOperator` or `IAlgorithm` that infer the operation or the search state, so the lambda sees the interface. The concrete rough edge under D33 is closed, and `ConcretelyTypedTarget_CanBeWrapped` pins it.
- **D38. `Observed` stays, and the observation builds its wrapper.** `Observed` is the fluent wrapper that reports what its child does to an instrument. An observation record implements `IInvocationObservation<TOperation, TCandidate, TSelf>`, whose static `Wrap` makes an executor report it, or `IAlgorithmObservation<TCandidate, TSearchState, TSelf>` for an algorithm boundary. No wrapper factory exists, so this corrects phase 3 step 4, which would have been advice under another name, and D23's "through the existing `operation.Wrap`" for `Observed`. `Wrap` casts the untyped executor as `WrappingExecutor` does. The record also carries what its operation takes to observe, and nine advice classes went. The developer rated this the most promising option for now.
- **D39. A counter is a sealed class with reference identity.** This amends D30's "one counter record" (§ 4.9: value semantics only). Two counters differ, so `Counted(E, a) != Counted(E, b)`, and graph equality tells them apart. Copying a configuration keeps the reference, which is the link between wrapper and terminator, and two runs of copies still count separately, because each run has its own scope. A grid built from one prototype with `with` therefore needs no cloning.
- **D40. `IRunResult` replaces a marker on the configuration.** The developer rejected `IRunBoundConfiguration`, because it adds a second flavor of configuration to the two layers of configuration and execution. An execution node implementing `IRunResult` is owned by the run's root, and `run.Result` takes any `IConfigurationNode<TResult>` with such a result, so an external node reads the same way as `Counter`. The developer accepts this as a start and may revisit it.
- **D10, until phase 2.** `TAlgorithm` keeps a `class` constraint, and phase 2 still removes the parameter.
- **`Validate`.** `CreateExecutor` rejecting an empty cycle is fine. A general configuration check is recorded in [the backlog](developer-backlog.md).

### Decided in round 14

Decided by the developer on 2026-11-06, after the pilot's report.

- **The pilot's exit.** The evidence is good enough to continue, once the comments on names ([D44](#decided-in-round-16)) and parameters ([D45](#decided-in-round-15)) are settled. Whether the whole simplification passes shows only when the parts it wants to remove are deleted, at phase 3 step 6 and in phase 4. The pilot does not decide that.
- **D8. A budget's counter is an ordinary configuration node,** which the budget shares in its own scope and which its counting wrappers and the budget reference. Execution data follows the sharing rules, no ancestor lookup is added, and `cycle.Sharing(budget)` keeps working. The pilot showed this for restarts per turn, nested budgets and two budgets in one scope. The rejected alternatives were a nearest-provider lookup, which adds a concept, and a baseline over a run counter, which miscounts when one budget runs interleaved.
- **D12. `Share` during construction stays,** because advanced uses need it. It is the one remaining scope change, with D29's rule as its one guard, and it changes only lifetime.
- **D33, the comparer stays.** Only tests have used it so far, and the developer keeps it for advanced uses.
- **D36, what the pilot adds.** The gap stays accepted until the start of phase 3. Skipping a clock that is already placed is not enough to close it: `T1(C(O))` with `T2` placed below the clock gives `T1(C(T2(O)))`, where `T2` reads the clock before the increment. A fix has to place the second trace above the clock, which a substitution naming `O` cannot do, so it needs either the clock wrapper as the target or another way to tick a clock.
- **D41. `ParameterKind` goes.** The only reader was one check that kept a walk out of reference lists, and each kind only repeated its class. `GetChildren` now returns what holds a place in the graph, which a reference list does not, so that check is gone.
- **D42. `Instrument` requires its substitutions** through the abstract `CreateSubstitutions`, which amends D22's "empty by default", so an analyzer cannot silently place nothing. The base class caches the result behind `Substitutions`, so every author gets stable instances ([round 17](#settled-in-round-17)). A definition with nothing to place, such as `Clock.Elapsed`, returns an empty list. `ParetoFrontAnalyzer` and `BestBeforeChangePerformanceAnalyzer` now place themselves. Test fixtures that only use the old path return an empty list too.
- **D43. `Substitution.Counting` is the one-call form,** beside `Substitution.Of`: `ga.Substitute(Substitution.Counting(ga.Mutator, out var mutations), Substitution.Counting(ga.Iterations, out var iterations))`. `Counting` on a configuration is this with one target. It is written as a static extension, so an external wrapper can add the same form. The developer keeps it for now and may return to it.

### Decided in round 15

Decided by the developer on 2026-11-06.

- **D45. One parameter class per value.** The declared value type says what a parameter is, as the developer pointed out. `Parameter<TOwner, TValue>` covers settings, children, optional children and delegates, and `ListParameter<TOwner, TChild>` and `ReferenceListParameter<TOwner>` stay. A list needs its element type to rebuild it, which would take reflection to get out of `TValue`. A reference list has the same type as a list of children and only names nodes elsewhere, so the type cannot tell them apart. The declared type decides rather than the value, so an instrument that also happens to be a node, such as an old counter in `Observed`, stays a setting. `Parameter` rejects a list of configurations when it is declared, because a walk would otherwise skip it silently. Required and optional children no longer differ, because nullability does not exist at runtime; nothing reads the difference today. Delegates now need `with` like everything else, so racing's `AlgBuilder` and `EvaluatorSelector` gain `init`.

### Decided in round 16

Decided by the developer on 2026-11-06.

- **D44. `Instrument` keeps its name until phase 6 retires it,** as that phase already says. The developer does not want to spend a decision on a name that goes away.
- **What phase 6 should try.** `Instrument` is a definition with three jobs: the key for `run.Result`, the recipe for one empty result per run, and the list of substitutions that place its recording. It covers two different things, analyses that observe calls and values that are only read, such as `Clock.Elapsed` and `EpochClock`, and it is neither a configuration node nor an execution node. So read-only clocks should implement only `IClock`, which leaves the type to what observes and collects, and analyzers should be tried as configuration nodes in the form of `Counter`. Results such as a plain `Dictionary` cannot implement `IRunResult`, which needs checking then. These are inputs to that phase, not decisions.
- **Working rule.** Code that a later accepted step removes gets no decisions of its own. It is changed as far as needed to keep the tests green and to make progress, and the change is mentioned in the report without a question.

### Settled in round 17

A review of the staged pilot on 2026-11-06 found these in the new code. It found nothing in substitution ordering, reference lists, parameter dispatch, observation forwarding or the HLib0003 change. The first and third were confirmed by failing tests before the fix.

- **One lock for results and construction.** Creating a result that read a configuration's result took the two locks in the opposite order from building a configuration that read a result, and that deadlocked. `RunResults.Sync` now guards both tables. The cost is that a live `run.Result` waits while a turn builds its executors, which a read of a configuration's result already did.
- **A cold read has the recursion guard.** A configuration that read its own result overflowed the stack, and now gets the recursive construction error. The read does not check for failure, because the results of a failed run stay readable, also one never built.
- **The configuration decides ownership of its result.** `IConfigurationNode` is covariant, so deciding from the requested type gave a `Counter` held as `IConfigurationNode<IExecutor>` a second count per scope. `Owner` now asks the configuration, which also removes its `bool` parameter.
- **Stable substitutions under concurrent reads.** Twelve copies of `field ??=` could hand out different instances, and a shared clock would then count twice in one call (D28). The base class now caches atomically, so authors write `CreateSubstitutions` and cannot get this wrong. A public `Substitutions` stays what callers read.
- **D29, what a read does.** A read of a run result does not count as resolving it. The review proposed that it should, but `run.Result(budget.Counter)` before `Complete` would then make a budget at the root fail with "cannot be shared", and a read must not break a run. The accepted consequence is that an external run result with children, read before a root-level `Share`, keeps the executor it was first built with instead of one in its own child scope. An accumulator has no children, so it is unaffected. The developer accepted this.
- **Not changed.** A cold read whose hook catches a nested construction failure can still publish its result. Results of a failed run stay readable by design, and nothing resolves through that result afterwards.

### Decided in round 18

Decided by the developer on 2026-11-06.

- **D46. A Roslyn analyzer catches forgotten parameter declarations,** which settles D2's completeness check. A forgotten child makes a walk treat the node as a leaf, so a selection by type or predicate skips that subtree without an error. The analyzer reports a configuration property with no declared parameter, for the library's records and external authors' alike, as D2 requires. It has to ship in the NuGet package, and today's analyzer projects do not, because `HeuristicLib.csproj` references them with `PrivateAssets="all"`. [Round 19](#decided-in-round-19) moves this out of the branch.

### Decided in round 19

Decided by the developer on 2026-11-06.

- **D46 moves out of this branch.** The analyzer and its packaging are out of scope for the simplification, so they get [a backlog item](developer-backlog.md#optional-capabilities-and-packaging) and their own issue and branch afterwards, which also decides how best to do them. This reopens D2's completeness check and leaves the [declaration cost](#what-does-not-simply-disappear) as it stands. The packaging was tried and reverted, because its output paths depend on the folder layout. No analyzer guards phase 2.
- **D47. `Supports` answers for the node alone, and one walk asks the children.** `execution.Supports(node)` asks the node and then its declared children, which replaces the hand-written lists. Composites that resolve children under their own signature but do not forward `Fits` today, such as `EliteSelector`, therefore start to forward. A mismatch below them is reported earlier and appears in `Validate`, which the developer accepts as the defect idea 9 removes.
- **D48. A parameter says what execution its child runs under.** This is how a composite that runs a child under other types is declared. The developer proposed it in place of a flag that would only skip those children, because a forward still checks them against the types they really run under. `Parameter.ChildExecution` maps the owner's `ExecutionSignature` to the child's. It is pass-through when left out, so regular parameters write nothing. The delegate sits on the static parameter, which is metadata per type, so configurations stay data. `ListParameter` gets it when a list crosses such a boundary, and `ReferenceListParameter` never does, because a walk does not ask its entries. Accepted costs: the inner types are stated twice, in `CreateExecutor` and on the slot, and the compiler cannot connect them, while `Resolve` still decides for the children it resolves; and a mismatch below these nodes fails in pre-flight rather than at build.

### Decided in round 20

Decided by the developer on 2026-11-06.

- **D49. `CreateExecutor` moves to a new `IOperator<TCandidate>`.** The hook names no operation, so `IOperator<TOperation, TCandidate>` inherits it, and a configuration has one however many operations it performs. Separate hooks per operation become compile errors, CS0539 for explicit ones and CS8705 for inherited defaults, which replaces the run-time check in `ConstructionHooks` and its inspection of a type's interfaces and methods. Slots keep naming the operation. Accepted costs: authors see a compiler error rather than the library's explanation, explicit hooks change their syntax, and a configuration can again implement one hook per candidate. `Resolve` rejects a second candidate its executor does not serve, but an executor serving both is built by whichever candidate is requested first, with no error. A derived type adding an operation to its base's hook compiles too, and fails at resolution if that executor does not serve the operation.
- **Reflection stays out of program logic.** The developer does not want code that inspects types and lists their methods or interfaces. `typeof` of a concrete type and `IsAssignableTo` are fine, and so is reflection that only builds a better exception message, which keeps `ProducedSearchStates` and `ExecutionSignature.Name`.

- **D50. `SettingParameter<TOwner, TValue>` amends D45.** `Parameter` decides on the closed type, so a value typed by a type parameter is read as a child, or rejected as a list, when it closes over a configuration, although it is data. A terminator stopping at a target candidate is a legitimate case of this. `SettingParameter` is never entered and never reported, whatever its type closes over. It is used where a value is typed by a type parameter, so 3 declarations use it: `PredefinedCandidates` and the two `SourceProblem` values. The other declarations stay `Parameter`, so a concrete setting can be written either way. Accepted limits: `Parameter<X, TCandidate>` still compiles and is silently a child when closed over a configuration, which the later analyzer is to report, and a delegate in a `SettingParameter` is not reported as a boundary.
- **A record never holds or declares an `IEnumerable`, generic or not.** A sequence has no value semantics, which breaks the idea of a record. The developer rejected `IEnumerable<TCandidate>` as the declared value of `PredefinedCandidates`, and the rule is in the [developer guidelines](../docs/contributing/developer-guidelines.md).

### Decided in round 21

Decided by the developer on 2026-11-08, after two independent reviews of the draft and a further review of how analyzers are used. It replaces the seven-step list of phase 3, so earlier references to step 6 now mean the deletions spread over steps 1 to 5.

Analysis:

- **Why placement had traps.** Each separate `Substitute` call is its own rewrite, with its own wrapper order and its own copies of the graph. Advice never had this, because it was matched once at resolution. Analysis therefore happens in one rewrite where it can, and several rewrites are made safe rather than relied on. A substitution stays a pure function of the graph and its arguments.
- **D25. One form for every measurement: define on the source, attach once, read from the run.** This replaces the two-protocol draft in which counters could only come from `Counting(..., out)`.

  ```csharp
  var evaluations = ga.Evaluator.Invocations.CountCandidates();
  var best = ga.Evaluator.Invocations.TraceBestSoFar(clocks: [evaluations]);
  var quality = ga.Iterations.TracePopulationQuality(clocks: [evaluations]);

  var run = ga.AnalyzedBy(best, quality).CreateRun(problem, random);

  run.Result(quality).By(evaluations);
  run.Result(evaluations).CurrentCount;
  ```

  `AnalyzedBy(params)` is one `Substitute` call, so every target is matched against the graph that call received, and nothing named on that graph can go stale within the call ([D58](#decided-in-round-21) covers an earlier rewrite). A trace attaches its clocks itself, listed after its own placement so that they sit closer to the node. One object stamps, is read and can be limited. `Counting(target, out c)` stays as the same thing in one step, returns the same `Counter`, and serves count-only use and terminators, where it cannot be forgotten. `Trace*` stays on sources. This settles D15: a definition keeps its source, because that is how it places itself. A `Counter` stays a sealed class (D39). One created from a source holds its placement privately, outside its declared parameters, under the analysis exception of § 4.8. Custom clocks keep working through `IClock.Substitutions`. The cost is the two-step form the developer doubted, which D57 makes safe.

- **D58. One operation means one `AnalyzedBy` call, made first.** The existing form stays: one call taking several substitutions, applied one after another (D17, D28). A budget or `Counting` before it copies `ga`, so `ga.Iterations` then gives D32's error, while `ga.AnalyzedBy(best, quality).LimitedToInvocations(ga.Mutator, 100)` works because the mutator's reference survives. The documentation says "analyze first, then budget", and D32's error names that route. A generalized call that also takes budgets and wrappers is a [backlog item](developer-backlog.md#runs-execution-and-experiments), and steps 1 and 3 count how often budget-then-analyze occurs as its trigger. A simultaneous pass that orders wrappers by kind is [rejected](developer-backlog.md#analyzer-placement-alternatives-rejected-on-2026-11-08).
- **D36. No clock rule: each call places what it lists.** Analyzer placement is an ordinary `Substitute`, and a clock has no special position. One call lists the clocks after the traces (D28), so a shared clock ticks once and before them. A clock shared across separate `AnalyzedBy` calls ticks twice, as `T1(C(T2(C(O))))`, and a trace on the original `m` after `Counting(m, out c)` gives `C(T(O))` and reads `c` before the call. D58 already asks for one `AnalyzedBy` call per operation; the second case traces `counted.Mutator` instead, or creates the counter from the source and analyzes first. Revised on 2026-11-08 during step 1, which first built a look-through rule; [the backlog](developer-backlog.md#analyzer-placement-alternatives-rejected-on-2026-11-08) records why it went. Observers commute under the observation contract, so most `Advised(a, b)` order reversals (D28) change no assertion.
- **D57. Forgetting to attach is an error on read.** `run.Result(x)`, for a definition that knows a placement, walks the run's declared graph for a parameter holding `x` by reference, through `GetParameters` and `GetValue`, so authors declare nothing beyond their parameters. If there is none and the walk crossed no delegate slot, it throws. An unvisited Cycle branch is still found and reads 0, which is true. A closure such as AutoEC's sits behind a delegate and is not checked. Failed runs behave the same. A discarded `AnalyzedBy` result is caught as well. Internal lookups, as from a trace's `CreateResult`, are not checked. Known limits: an undeclared child is a leaf, and a terminator holding a never-attached counter still never stops, so the documentation uses `Counting(..., out)` there. Instrument analyzers need no node form for this.
- **D59. `Substitute` takes substitution bundles, and `AnalyzedBy` only names it.** Decided by the developer on 2026-11-10 during step 2, when `Count()` stopped returning an instrument and `AnalyzedBy(params IReadOnlyList<Instrument>)` could no longer take a counter. A substitution bundle is a flat, ordered list of substitutions that belong together; `ISubstitutionBundle` (working name) in `Execution` has one member, `Substitutions`. A single `Substitution` is a bundle of one, and `Instrument`, `IClock` and with them `Counter` and `Duration` are bundles of their placements, so analysis builds on substitution rather than the reverse, and one call can mix traces, counters and plain substitutions. `AnalyzedBy` takes bundles too and adds nothing but typing the result by the receiver's interface, which an analyzer of the receiver itself needs. D57's check applies to any bundle. Rejected for now, with their triggers in [the backlog](developer-backlog.md#scenarios-awaiting-a-concrete-need): a composite bundle, and an `IAnalyzer` on top, which would be an empty marker in the wrong layer. A first draft named the interface `IAnalyzer` and made `Substitution` an analyzer, which turned the dependency around. `Instrument` gets no further design, because phase 6 retires it.

Counters, durations and budgets:

- **D52. The old counter types go in phase 3.** `OperatorCounter`, `AlgorithmCounter`, `ObservationCounter`, the old `Duration` and `OperatorDuration`, D34's adapter and `[OverloadResolutionPriority]` go. `Count()`, `CountCandidates()` and `Duration()` stay on sources and return the new nodes. This corrects the phase 2 note that said phase 6.
- **D53. Duration is a node.** A sealed class whose execution node is `DurationAccumulator`, which moves from L0 to L3 as `CountAccumulator` did (this amends D35). `Timed`, the duration budgets and `AfterOperatorDurationTerminator` use it. `TimeProvider` stays on the node as a documented exception, and taking time from the run is a [backlog item](developer-backlog.md#runs-execution-and-experiments).
- **D11. A multi-target budget owns its counter.** `ga.LimitedToInvocations(100, (g, calls) => g.Counting(g.Mutator, calls).Counting(g.Crossover, calls))`, and the same for candidates and `LimitedToDuration`. The lambda runs during the call and is not stored. It needs `Counting(target, existingCounter)` and `Timing(target, existingDuration)`. The narrowed counter, which reads 0 at the root, is never handed out, and the wrappers are inside the limited algorithm by construction. An untyped `IReadOnlyList<IOperator>` is rejected, because a dispatch member on `IOperator` with default implementations per operation is ambiguous (CS8705) for multi-operation nodes. `Counting(m, c)` written twice places two wrappers and counts twice. Counting every operation of a node takes one substitution per operation. [D60](#decided-in-round-21) replaces the multi-target count call site.
- **D60. One general count form.** Decided by the developer on 2026-11-10 during step 3. The counter carries no metric since step 2, so D11's "the same for candidates" would have made `LimitedToInvocations(100, lambda)` and `LimitedToCandidates(100, lambda)` identical, and a forgotten metric in the lambda would silently count calls under a name that says candidates. The multi-target form is therefore `ga.LimitedToCount(100, (g, calls) => g.Counting(g.Mutator, calls).Counting(g.Evaluator, calls, OperatorCountMetric.Candidates))`, where each `Counting` states its metric as an explicit `Counted` does, so one budget can mix them. `LimitedToInvocations(op, n)` and `LimitedToCandidates(op, n)` stay as the single-target shortcuts that fix the metric. Duration has one unit, so `LimitedToDuration(max, (g, time) => ..., timeProvider)` keeps its name. Rejected: both D11 names with identical bodies, and a metric-bound handle for the lambda, which would add a public type only to guard one usage.

Observation:

- **D51. Racing tracks contenders with a callback analyzer.** Racing places `Observing(performance.Observe)` on each contender's evaluator, in a graph it builds itself and never persists, under the analysis bullet of § 4.8. No tracker node, `Share`, `Resolve` or reset is needed. Amended by the developer on 2026-11-10 during step 4: `Observing` derives and publishes nothing, so it moved to `Inspection`, where content may use it, and racing places it with `Substitute`. The layering rule forbids content to reference `Analysis`, and an exception for racing or a racing-only `IObservationInstrument` were the rejected alternatives.
- **D54. Sources and callbacks.** The advice-factory overload of `ObservationSource.Create` goes. New roles are recommended to be operations and get `Observed`, `Counted`, `Timed` and `Retry`. A plain node role writes its own wrapper record and passes a replacement `(node, instrument) => new ObservedSchedule(node, instrument)`. The documentation shows it with an interface-typed argument, because a concrete type hits D10. That delegate stays reachable through the trace, so § 4.8's analysis bullet is extended to it. `Observing(callback)` (working name) replaces `Aspect.Observe`, for PythonInterop too (`PythonGenealogyAnalysis.cs:297`). Every API taking a callback documents that it produces a non-serializable graph.
- **D55. Iteration updates are a wrapper.** `ApplyingIterationUpdates()` (working name) takes the problem from the call it wraps and completes the iteration before each state is yielded, as the observer does today (`AlgorithmObservation.cs:51-53`). `Supports` requires a dynamic problem. Tests cover pausing, disposal and racing's epoch loop.

Advice replacement and naming:

- **D56. Wrappers replace custom advice.** The forms are `WrappingOperator` with `operation.Wrap` and an `IInvocationWrapper`, `WrappingAlgorithm`, or bundled substitutions. Before deletion, every advice base's capabilities, including its `Check` hook, are checked for a wrapper equivalent open to external authors. `TimedAlgorithm`, `Timing(ga.Iterations, ...)` and `ga.Iterations.Duration()` replace `DurationMeasuringAlgorithmAdvice`.
- **D13, wrapper stays.** The glossary defines it structurally, and the documentation mentions "decorator" as a bridge. `Substitute` remains the mechanism, because it does not say whether the replacement wraps the original or replaces it outright.

Racing: [the migration notes](#migration-notes-for-origindynamic-racing) hold what the merge needs. An outer trace does not reach contenders, which is provisional.

### Decided in round 22

Decided by the developer on 2026-11-11, at the start of phase 4.

- **D61. The signature check stays at each `Resolve`, and only its wording leaves the scope.** Both typed `Resolve` paths already ask `ExecutionSignature.Supports`, the walk of D47, so phase 2 delivered this item. The check cannot move to the run alone: a child that a delegate creates, as racing's `AlgBuilder` does, is not declared and is first seen when it is resolved, and a rejection before anything is built is what leaves the scope usable for `TryResolve`. Checking only the outermost `Resolve` would need the root detection this phase removes. `SearchConfigurationValidation.RequireSupport` now chooses the exception, so each `Resolve` makes one call. Messages and `TryResolve` are unchanged.
- **D62. The failure state stays tree-wide for now.** A failed composite leaves its already built children in its scope's table, shared children it triggered in an ancestor's table, any `Share` it made, and whatever it did to executors that existed before. A run never meets these, because a failed run does not resolve again. A caller that catches the failure and resolves again through the same tree would, and so would a composite that catches a child's construction failure. Failing only the requesting scope misses what was left in ancestors, and covering ancestors reaches the root. A rollback would need a journal and still could not undo side effects. Without the state, `TryResolve` could also no longer tell a rejection from a construction failure. The developer may revisit this later.
- **D63. Elapsed time is measured only through a run.** Accepted by the developer on 2026-11-11 as the recommended answer to the preceding thread's D1. `AlgorithmRun` starts and stops measurement around each call advancing its retained algorithm iterator, using `try`/`finally`. Pauses and time spent consuming states remain excluded. `RunResults` retains the elapsed value and stopwatch state so `Clock.Elapsed` can read them during execution; the run's single active stream removes the need to count overlapping calls. Remove the scope's root detection and `ElapsedTimeExecutor`, returning the executor itself. Keep the internal `ResolutionScope.Create(TimeProvider)` seam and add an internal run constructor taking a `TimeProvider` for deterministic tests. Direct scope users accept that `scope.Results.Elapsed` and observations using `Clock.Elapsed` stay zero, without an error; document this and point to explicit `algorithm.Timed(duration)` measurement when needed. Move the elapsed-time and elapsed-clock tests onto runs, and remove the three tests specific to the hidden wrapper, below-root exclusion and overlapping direct-scope calls. Cover the accepted zero behavior explicitly. No further phase 4 design decision remains open.

### Decided later

- **D5. Problem-bound executors (idea 5).** Bind an executor to one problem object at construction, and model a changing problem as a mutable problem instance, as `DynamicProblem` already does. The measured [phase 4b spike](#phase-4b-problem-binding-spike) was accepted, and the production migration retains generic typed construction hooks with the problem on their scope view. See [the conditions](#problem-bound-executor-conditions).

## Proposals behind the open decisions

### Executor control proposal

The strategy and its mutators name one explicit adaptation node. The base strength remains plain configuration data; the adaptation holds a scale factor at runtime.

```csharp
var adaptation = new ScaleAdaptation();
var gaussian = new GaussianMutator(0.5, 0.1)
{
    MutationStrengthAdaptation = adaptation
};
var strategy = new EvolutionStrategy<RealVector>
{
    Creator = creator,
    Mutator = gaussian.Timed(duration),
    MutationStrengthAdaptation = adaptation
};
```

```csharp
// Fresh adaptation each turn by default; retain it across turns explicitly.
var cycle = CycleAlgorithm.Create(strategy, other).Sharing(adaptation);
```

Both the mutator and strategy resolve the adaptation through the ordinary scope. `ScaleAdaptation : Adaptation<AdaptedScale>` creates a runtime factor initially 1. `IAdaptation.Apply(baseValue)` reads the effective value, and the strategy's 1/5 success rule multiplies `AdaptedScale.Factor`. An `OffsetAdaptation` similarly creates an `AdaptedOffset` with a settable `Offset`, for other rules. External nodes can implement `IConfigurationNode<IAdaptation>` directly. The read slot accepts every form; the strategy's write slot names `ScaleAdaptation`. Wrappers forward nothing, and several mutators adapt together by naming the same link. Observing adaptation values waits for phase 6.

Decided details (D16, amended 2026-11-16):

- **An unset strategy slot means no adaptation.** A Gaussian mutator with no link uses its configured base unchanged. Strategies naming Gaussian mutators must opt in explicitly; factories take no adaptation argument.
- **Share the adaptation whenever the mutator is shared or its value must survive a turn.** Sharing a configuration shares its children only when they are shared too.

  | Shared on the Cycle     | Effect                                                                                    |
  | ----------------------- | ----------------------------------------------------------------------------------------- |
  | Nothing                 | A fresh adaptation each turn; mutator and strategy agree                                  |
  | `adaptation`            | The value survives turns; mutator and strategy agree                                      |
  | `adaptation`, `mutator` | Adaptation and mutator private data survive turns; both agree                             |
  | `mutator` only          | The mutator reads its original adaptation; each strategy writes another that nobody reads |

  No detection is added. Documentation and tests cover this case; a later configuration graph check may report it.

- An adaptation the mutator does not read is left to that same later check.

### Problem-bound executor conditions

Direction favored; decided from the [phase 4b spike](#phase-4b-problem-binding-spike) (D5). The developer's proposal resolves the conflict recorded earlier, that an executor bound to one problem cannot keep its state across a switch: the executor stays bound to one problem object while that object changes underneath, so state survives. [`DynamicProblem`](../src/HeuristicLib.Experimental/Problems/Dynamic/DynamicProblem.cs) already works this way. Its objective and search space are fixed at construction, its environment advances through `Update()`, and a reader/writer lock keeps one evaluation in one environment.

Findings on 2026-11-05, by text search:

- About 111 signature lines in `src` and 107 in `test` carry the `random, searchSpace, problem` parameters. About 12 operator files read a member of the problem, 7 of them in core, mostly `Objective` and `Evaluate`.
- No `problem with` or `searchSpace with` exists in `src`. One operator caches instance data: `DynamicRelativeQualityEvaluator` keeps its source problem and a best-known objective vector, which it refreshes by epoch (`DynamicRelativeQualityEvaluator.cs:68-73,91-95`). It already meets the condition on epochs below, and needs its reference check (`:85`) only because it is not bound to the problem.
- Three kinds of operator call children with another problem: racing's meta creator and mutator (`DynamicRacingAlgorithm.cs:130,276`), the composite search space's parts (`CompositeSearchSpace.cs:55-56`), which pass a `NoProblem` carrying only the part's search space, and two inner-problem refiners in PythonInterop (`PythonInterOptEquationScoring.cs:26`, `ExtendedSymbolicRegressionProblem.cs:40`).

The developer requires that funneling a different problem or search space to a child stays supported, for algorithms and operators alike, and that a meta-algorithm does it while constructing executors. The scenario to support is a problem-specific meta-algorithm that decomposes a Traveling Thief problem into a tour and a packing subproblem, each with its own search space and genotype, solved by inner algorithms, together with composite search spaces and genotypes. Proposed shape, not implemented:

```csharp
protected override IAlgorithmExecutor<...> CreateExecutor(ResolutionScope<TravelingThiefProblem> scope)
{
    var tourProblem = new TourProblem(scope.Problem);       // a separate problem object with its own search space
    var packingProblem = new PackingProblem(scope.Problem);
    return new Executor(
        scope.For(tourProblem).Resolve(TourAlgorithm),      // a different genotype follows from the child's problem
        scope.For(packingProblem).Resolve(PackingAlgorithm),
        tourProblem, packingProblem);
}
```

If a subproblem's quality depends on the other part's current solution, the meta-algorithm updates its subproblem between turns. That is the mutable problem again, with the meta-algorithm as its owner, and inner state survives. Building a new child per turn stays possible but starts fresh unless the state is shared.

A design must settle these points:

- **Truly problem-specific operators stay first-class,** such as planned vehicle routing operators that read instance data. A typed problem at construction replaces the generic call argument.
- **What may change under the hood.** Recommended (D18): instance data only. Search space and objective stay fixed for the lifetime of the problem object, as in `DynamicProblem`, because a population would not stay valid across such a change. A different search space means a different problem object at construction.
- **Executors bind to the problem object, not to values read from it.** Values derived from a problem that can change are not cached, or are checked against its epoch.
- **When a switch takes effect.** Recommended: the guarantee `DynamicProblem` already gives, that one evaluation is scored by one environment. Qualities already in a population become stale, which `ReevaluationInterceptor` handles for dynamic problems today.
- **Independent trials.** A mutable problem shared across trials makes them dependent. As with dynamic problems today, the caller owns this.
- **Scope.** `scope.For(problem)` is a view of the same scope, not a child scope (D18). The search space always comes from the problem, and a child that only needs a search space gets a `NoProblem` as today.
- **Default sharing across problems.** Today a scope holds one executor per configuration (`ResolutionScope.cs:191`). Proposed: an operator or algorithm has one executor per configuration and problem, because an executor is bound to one problem. A node whose hook receives a plain scope, such as a strength or a counter, cannot read a problem and has one runtime object per scope, whichever view asks. Same configuration and same problem therefore behave exactly as today, and problems are compared by reference.
- **`Sharing` across problems.** `Sharing` and Cycle turns change lifetime only, and the same rule applies inside them. A shared strength stays one across subproblems. A shared operator requested for another problem gets its own executor bound to that problem, so it never uses the wrong problem. The cost is that two subproblems do not share an operator's private data, which the developer accepts. Data meant to cross belongs in a node with a plain hook, or the case uses one dynamic problem instead of two problems.
- **Authoring.** A stateless operator cannot be its own executor once it needs the problem or search space. A base class can keep today's one-method override and allocate a small bound executor per build, so the author's call site stays as simple. This means the authoring ladder of §§ 8.4 and 8.5 does not vanish completely; the executor generics and typed scope views do. Allocation per build must be benchmarked.
- **`Fits`.** A bound build checks the problem type when it asks for it. Whether a separate pre-flight check remains depends on `TryResolve` and on validation reaching children that a run builds later.
- **Cost.** Every operator changes, which is why the spike comes before any migration.

Nothing in phases 1 to 4 prevents or requires this, except that typed scope views and arity ladders stay until it is decided.

### Adopted design rules

Applied as developer guidelines § 2.5, with a pointer from the `AGENTS.md` architecture checkpoint:

1. **Express before building.** Write a new scenario with existing nodes and rules first. Propose a new mechanism only when that is not reasonably possible, and state which existing mechanism it makes redundant.
2. **One composition mechanism.** Behavior is added by nodes and conveniences generate nodes. No new mechanism changes an executor after construction.
3. **The executor graph follows from the configuration graph and the problem.** No new dependency on anything else, such as attachment order or request context, without a recorded decision.
4. **Define errors out of existence.** A new guard is a signal of temporal coupling to remove, not a fix.

The rules are phrased as limits on new work, because advice still exists until phase 3. A fifth proposed rule, on keeping the concept count deliberate, is covered by §§ 2.1 and 3.1 and was folded into a sentence in § 2.5.

Not adopted from the reports: a fixed numeric concept budget, and "three production users" as a reason to remove something, because in-repo usage says nothing about external consumers. Three users may still be asked of a newly proposed generic mechanism.
