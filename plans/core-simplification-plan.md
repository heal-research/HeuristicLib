# Core simplification plan

Status: started on 2026-11-05. This file is the one record of which simplifications HeuristicLib will make, in which order, and which decisions have been taken. The [reports](simplification/) and their review are input: nothing in them is accepted unless it is recorded here. The phases and their order are accepted. Phase 0 is applied, and code implementation has not started. All of this work is architectural under `AGENTS.md`, so design shapes marked as proposed or open still need the developer's acceptance.

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
| 2   | Constructing a node never changes the rules for its siblings       | Accepted                                                   | End of phase 3    |
| 3   | Scope reduced to executor table, child scopes and sharing          | Accepted direction                                         | Phase 4           |
| 4   | Generic configuration introspection                                | Accepted, declared parameters, no reflection               | Phases 1, 2 and 7 |
| 5   | Problem-bound executors                                            | Direction favored; decided from a spike                    | Phase 4b, D5      |
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

Declarations on every configuration node in core, Experimental and PythonInterop, about 240 records by a rough count, with whatever completeness check D2's detailed design selects. First consumers, with no behavior change: the reflection walk in `SearchConfigurationValidation` and the hand-written `Fits` child lists (idea 9; `Fits(` appears in 26 source files). `Fits` is renamed to `Supports` here, because its overrides are rewritten anyway. Composites that run children under other types remain a declared boundary. `CycleAlgorithm` and `PipelineAlgorithm` also lose `TAlgorithm` here (D10).

### Phase 3: replace interception

Delivers ideas 1, 2, 7 and 8. The analyzer's user-facing syntax is decided at the start of this phase (D25), and so is the shared clock across separate calls ([D36](#decided-in-round-14)). Concrete steps, which are also the concrete steps of idea 2:

1. Budgets stop calling `scope.AddAspect`. `LimitedToInvocations` and the duration equivalents place counting and timing wrappers through a substitution (`OperatorCountBudgetAlgorithm.cs:44,94`, `OperatorDurationBudgetAlgorithm.cs:43,89`). This needs D8.
2. `Advised` is replaced by placement through a substitution (`AdvisedAlgorithm.cs:39,41`). `Instrument<TResult>` loses `CreateAspect`.
3. Racing tracks its contenders through a substitution instead of contender advice (`DynamicRacingAlgorithm.cs:386`). This needs D3.
4. `ObservationSource.Create` loses its advice-factory form, so external operations keep the same path as built-in ones through their observation record ([D38](#decided-in-round-13)). This step also settles role sources, which have no placement yet.
5. `DynamicProblem.CreateIterationUpdateAspect` (`DynamicProblem.cs:130`), which builds an aspect without calling `AddAspect`, becomes a placement. Its documentation still says to pass the aspect to run creation, which should be checked against the removed run setup.
6. With no caller left, `AddAspect`, the scope's aspect list, `AdvisedAlgorithm`, the `Advice` family that wraps executors, the adder exemption, the advice guards and ordering rules go, and so does the temporary `.Obsolete` namespace (D23). The rule then holds by construction. Under D13, `Pointcut`, `Aspect` and the term advice also leave the API.
7. Migrate the 138 `.Advised(` occurrences, 73 test files and 11 documentation pages that use these concepts, and add the rule to the developer guidelines.

Those seven `AddAspect` sites in four files are all production callers. Idea 2 is therefore the end of this phase, not an independent quick win: budgets cannot stop adding advice before something else places their counters.

`scope.Share` remains as a scope change made during construction, with its order guard (D12).

### Phase 4: reduce the scope

Today `ResolutionScope` holds: the executor table, the child scope tree, the sharing set, the aspect list with ordering and duplicate rules, guards, the construction lock and stack, the failure state, the result table reference, root detection for elapsed time, and signature checks.

- Phase 3 removes the aspect list and its guards, and the failure state whose stated reason is an executor left with part of its advice.
- Root detection and `ElapsedTimeExecutor` move to the run, which owns elapsed time.
- Signature checking moves to the generic walk of phase 2. `TryResolve` stays public.

What remains is the executor table, the child scope tree and sharing, plus the result table reference and a lock. Typed scope views stay until D5 is decided.

### Phase 4b: problem-binding spike

Accepted on 2026-11-05 as the way to settle D5. It ports mutation, racing, the composite search space, the inner-problem refiners and `DynamicRelativeQualityEvaluator` to executors bound to a problem at construction, adds a small Traveling Thief decomposition as a test, and runs benchmarks. The developer then decides whether the migration goes ahead, before phase 5 writes new executors. See [the conditions](#problem-bound-executor-conditions).

### Phase 5: executor controls

Accepted (D4). See [the proposal](#executor-control-proposal). `IExecutionNode` arrives here (D19), together with the first node that does not execute anything.

### Phase 6: simplify observation

Delivers ideas 10 and 14. Today's concepts are sources, observation records, instruments, traces, value functions, retention, clocks, counters, durations, accumulating results and `IObservationInstrument`. Proposed model: an instrument creates a per-run result and records one observation into it, and `Observed` places it. A trace, a best-so-far curve, a Pareto front and a genealogy are then all such folds, and retention is a setting of the trace. Counting and timing keep their dedicated wrappers, because they allocate no observation and timing must include failed calls. A detailed inventory comes when this phase starts, and it must not close custom clocks or external collectors. `Instrument` is retired no later than this phase, with `Analyzer` as the surviving name. `Instrument` keeps its name until then, with no separate rename. The non-generic base and the static `Analyzer` factory class cannot both use that name, so one of them needs another name or home.

### Phase 7: experiments on the parameter system

Grids can vary parameters through metadata in-process, and trials get explicit identity instead of using the configuration as their key (`GridExperiment.cs:28`, idea 11). Typed `VaryBy` stays. JSON formats, construction from data, type registries and REST stay in the backlog.

### Phase 8: documentation and merge

Step 4's architecture and guide rewrite, the tiered glossary with fundamental concepts first, the naming decisions applied, and the merge. `origin/dynamic-Racing` must be coordinated before phase 3 at the latest (D3).

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

- **A clock ticks once, and before the observations that read it.** Today this is advice deduplication with closest-occurrence ordering. Placing at the original reference would be wrong for a second trace on the same target: it would sit inside the first trace's counter and read the clock before that call was counted. The placement helper must wrap outside the existing instrument wrappers on that target and skip a counter already placed. That is one local rule with a visible result, but it is still a rule. Within one call D28 settles it through the listed order, and the helper is only needed across separate calls.
- **Persistence.** A substituted graph is persistable only if the wrappers it adds are data. `Trace(observation => ...)` holds a delegate and `OperatorDuration` a `TimeProvider`, so instrumented graphs stay under the documented exception until [value strategies](developer-backlog.md#runs-execution-and-experiments) exist for them. The configuration before placement stays clean, and a substitution can be reapplied after loading. Instruments and shared nodes also still need an identity in a persisted form.
- **Reach.** Advice reaches anything resolved below it, including configurations a delegate creates at run time. A substitution reaches only declared children (D3). It also changes only the graph it returns. Today a budget counts every call of its operator in its scope and child scopes, including a direct request for the original algorithm (`DeferredAlgorithmResolutionTests.cs:294`), and the budget and that request use one algorithm executor (`ConfigurationSharingTests.cs:118`). With eager substitution the budget holds a copy of the algorithm, so a direct request for the original neither counts nor shares that executor. This follows from D1.
- **Declaration cost.** Avoiding reflection means every configuration author declares parameters, and a declaration can be forgotten. Analyzers in `analyzers/` are `IsPackable=false` with `PrivateAssets=all`, and the project files contain no packing entry, so external authors do not get a completeness analyzer today (D2). The developer accepts this for now. Its overhead is to be measured, and the mechanism designed in more detail, before the choice is final.

The clock rule above is accepted provisionally and is discussed again when placement is implemented.

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

### Decided later

- **D45. One parameter class per value.** The developer suggests that the value type already says what a parameter is. Proposed: one `Parameter<TOwner, TValue>` covering settings, children, optional children and delegates, with separate classes only for lists of children and reference lists. Waiting for the developer's decision.
- **D5. Problem-bound executors (idea 5).** Bind an executor to one problem object at construction, and model a changing problem as a mutable problem instance, as `DynamicProblem` already does. The direction is favored, and the [phase 4b spike](#phase-4b-problem-binding-spike) settles it. See [the conditions](#problem-bound-executor-conditions).
- **D11. Untyped multi-target budgets.** `LimitedToInvocations(IReadOnlyList<IOperator>, n)` cannot build a typed wrapper from `IOperator` without a dispatch. Options are a small dispatch member on `IOperator`, or typed targets sharing one counter. In-repo usage alone is not a reason to remove it. Decide in phase 3; the pilot uses one typed target.
- **D15. Does a definition keep its source?** A definition names a source that explicit placement ignores. Decide in phase 6.
- **D25. The analyzer's user-facing syntax.** Decided at the start of phase 3. The pilot only needs the mechanism, so `ga.Substitute(analyzer.Substitutions)` is enough for it, and `AnalyzedBy` stays provisional. The developer doubts the two-step form. `ga.Mutator.Invocations.Count()` returns an `OperatorCounter`, a definition holding its source and metric, and changes nothing in the graph (`ObservationSource.cs:58`). If a user never installs it, `run.Result` still returns a result, because the first lookup creates one (`RunResults.cs:33-35`). The count then reads 0 with no error, and a terminator reading it never stops. Requirements: getting a count or another analysis must be simple, and forgetting to install must not be silent. Candidates: one call that creates and installs, such as `ga.Counting(ga.Mutator, out var calls)`, and a read that fails for an analyzer the run's graph does not contain, which eager substitution makes possible because that graph can be walked. The developer favors the one-call form: `calls` can report the current count or be passed to a terminator. `calls` must be a configuration node, an immutable record with an execution node holding the count, in the form of `MutationStrength` ([D30](#decided-in-round-8)).

## Proposals behind the open decisions

### Executor control proposal

`EvolutionStrategy` asks the resolved mutator for `IMutationStrengthControl`, and `GaussianMutator` implements it. Every wrapper in between must forward it, and `ExecutorControls`, `IExecutorControlProvider` or the control are referenced in 37 source files. Choices and routers hide it, so the strategy silently stops adapting.

```csharp
// Proposed syntax, not implemented. The strength is data; its runtime object holds the current value.
var strength = new MutationStrength(initial: 0.1);
var es = new EvolutionStrategy<RealVector>
{
    Creator = creator,
    Mutator = new GaussianMutator(mutationRate: 0.5, strength).Timed(duration),
    Strength = strength
};

// One executor per scope through default sharing, fresh per Cycle turn, kept across turns on request.
var cycle = CycleAlgorithm.Create(es, other).Sharing(strength);
```

`MutationStrength` implements `IConfigurationNode<TExecutor>`, the existing form for a node that is not an operator, and both the mutator and the strategy call `scope.Resolve(strength)`. Its runtime part is a plain object holding the current value, which implements the empty `IExecutor` marker today and `IExecutionNode` under D19. Default sharing gives both the same object, and no new scope feature is needed. The `Strength` slot is typed `IConfigurationNode<IMutationStrengthControl>?`, so that a consumer's strength node works the same way. Today `GaussianMutator` is the only implementer of the control, and only `EvolutionStrategy` reads it.

Adaptive strength, per-turn reset, deliberate retention and observation of the value all remain. Wrappers forward nothing. Several mutators adapt together by referencing one strength, so "what is the strength of a choice of two mutators" no longer arises. What is lost is a strategy adapting a strength it was never told about. Keeping the strength as an algorithm-owned operation input, the report's preferred fix, stays rejected because it loses the value at each fresh turn, against the [S7 assessment](operator-unification-system-spec.md#s7-executor-reuse).

Decided details (D16):

- **An unset slot means no adaptation,** and the plain `double` constructor means a fixed strength. `new GaussianMutator(0.5, 0.1)` in an `EvolutionStrategy` adapts automatically today, but no recommendation produces `GaussianMutator`, so only callers that name it are affected.
- **The strength must be shared whenever the mutator is, or whenever its value must survive a turn.** A shared configuration shares its children only when they are shared too.

  | Shared on the Cycle   | Effect                                                                                            |
  | --------------------- | ------------------------------------------------------------------------------------------------- |
  | Nothing               | A fresh strength each turn; mutator and strategy agree                                            |
  | `strength`            | The value survives turns; mutator and strategy agree                                              |
  | `strength`, `mutator` | The value and the mutator's private data survive turns; mutator and strategy agree                |
  | `mutator` only        | Wrong: the mutator and the strategy hold different strengths, and nothing reads the adapted value |

  The rule stays as it is, with no detection. The documentation and a test cover this case, and the [configuration graph check](developer-backlog.md#scenarios-awaiting-a-concrete-need) may report it later.

- **A strength the mutator does not read** is left to the same later check.

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
