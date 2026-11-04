# Operator unification system spec

Status: behavioral draft, revised on 2026-11-03 after developer review and updated on 2026-11-04 with the decisions taken since. This document separates confirmed expectations from questions that needed answers; all behavioral questions were decided by 2026-11-04. It does not approve an architecture or authorize implementation.

The governing goal is simple common usage and complex behavior composed from a small set of consistent rules. Specify observable outcomes before selecting types, wrappers, construction stages or caches. Existing behavior is evidence, not automatic acceptance of every current rule.

## How to read this spec

- **Confirmed** means the developer has explicitly established the expectation.
- **Current behavior** describes what the implementation does today. It still needs confirmation before it becomes a requirement.
- **Open** means the expected outcome has not been agreed. A recommendation is not an answer.

Each open question uses the same layout: **Setup** shows the code, **Today** shows what happens now, **Question** states the decision in one sentence, **Options** compares the possible outcomes and **Recommendation** gives a proposed answer.

Terms follow the [glossary](../docs/guide/glossary.md). Design alternatives live in the [operator rework plan](operator-role-parameterization-rework.md) and the [lifetime plan](operator-state-lifetimes.md). [Lifetime binding](operator-state-lifetimes.md#lifetime-binding-through-sharing-accepted-on-2026-11-03), accepted and implemented as explicit `Sharing`, and the accepted but unimplemented [default sharing](operator-state-lifetimes.md#default-sharing-the-scope-is-the-unit-accepted-on-2026-11-03) are referenced below where they answer a question. The responsibility of aspects is governed by [aspects as automatic composition](aspects-as-automatic-composition.md).

## Concrete example used throughout

```csharp
IOperator<Mutation, RealVector> mutator = /* shared configuration M */;
IAlgorithm<RealVector, PopulationState<RealVector>> algorithmA = /* A */;
IAlgorithm<RealVector, PopulationState<RealVector>> algorithmB = /* B */;
```

- A and B reference the exact same mutator configuration M.
- Each invocation of A makes three successful calls to M, yields one state and finishes.
- Each invocation of B makes two successful calls to M, yields one state and finishes.
- Each mutation call processes one candidate. No other operator invokes M.
- Unless stated otherwise, Cycle runs at most two cycles with fresh child executors.

Some scenarios change A's behavior. They say so in their setup.

**Measured results.** The results of S6 to S9 and Q2 to Q6 were measured against the current implementation on 2026-11-03 with a temporary probe, which was removed afterwards. The probe used an evaluator as M, which has the same counting and observation behavior. The code snippets here are simplified fragments. Code labeled **Proposed syntax** is not implemented.

**Configuration graph: shared references, no mutable counter**

```text
              Cycle configuration
                /            \
        A configuration    B configuration
                \            /
                 M configuration
                 one shared reference
```

**Execution graph: fresh construction does not share private fields**

```text
Run 1
  Turn A1: A executor 1 → M executor 1
  Turn B1: B executor 1 → M executor 2
  Turn A2: A executor 2 → M executor 3
  Turn B2: B executor 2 → M executor 4
```

Arrows mean child references or calls. A configuration diagram is not an executor cache or a lifetime diagram.

## Confirmed expectations

### S1. Simple private accounting

**Status: confirmed. Current API:**

```csharp
var limited = algorithmA.LimitedToInvocations(mutator, 5);
var first = limited.CreateRun(problem, random1);
var second = limited.CreateRun(problem, random2);
```

```text
Configuration: Budget(limit 5, counts M) → A → M

first run:  budget executor 1 owns counter C1: 0 → 3
second run: budget executor 2 owns counter C2: 0 → 3
```

C1 and C2 are distinct and neither is stored in the configuration. Both runs finish naturally after A's one state, below the limit.

Given an algorithm configured to stop after a selected operator completes a specified amount of work, with no sharing requested.

When its budget executor is constructed.

Then it receives a fresh private counter. The caller configures no handle and supplies no counter. The reusable configuration does not hold the mutable counter.

### S2. Explicit sharing within a run

**Status: the expectation is confirmed. The handle mechanism shown is [provisional](operator-state-lifetimes.md#provisional-status-of-resource-handles-recorded-on-2026-11-03). Its replacement, decided on 2026-11-04, is a run-bound result: the counting wrapper writes a counter definition and the terminator reads the same definition, one counter per run ([lifetime plan, run-bound results](operator-state-lifetimes.md#advice-and-scope-layers-under-discussion-since-2026-11-04)). The participants share the counter, not executors, so the last sentence below holds again; the setup and supply calls shown here go. Current API:**

```csharp
var handle = new CountAccumulatorHandle();
var limited = algorithmA.TerminatedBy(
    new AfterOperatorCountTerminator<RealVector>(handle, 5));
var run = limited.CreateRun(problem, random, setup => {
    var counter = new CountAccumulator();
    setup.AddResource(handle, counter)
         .AddAspect(mutator.CountInvocations(counter));
});
run.Complete();
var observed = run.Resource(handle).CurrentCount; // 3
```

```text
Reusable configuration                 Runtime objects in this run
  Terminator → handle H                 Terminator executor ─┐
  A → M                                 M counting advice ──┼→ counter C
                                        run.Resource(H) ────┘
```

This is the cumbersome setup that started the redesign.

Given separately configured participants that are meant to share one piece of data.

When they run in the same run, including through fresh child constructions.

Then they use the same data instance. Sharing the data does not share their executors, other private fields or advice.

### S3. Independent data across runs

**Status: the expectation is confirmed. Handle mechanism provisional. Current API using `limited` and `handle` from S2, without its setup callback:**

```csharp
var first = limited.CreateRun(problem, random1);
var second = limited.CreateRun(problem, random2);
var c1 = first.Resource(handle);
var c2 = second.Resource(handle);
c1.IncrementBy(3);
```

| Check                                  | Expected value |
| -------------------------------------- | -------------- |
| `ReferenceEquals(c1, c2)`              | `false`        |
| `first.Resource(handle).CurrentCount`  | `3`            |
| `second.Resource(handle).CurrentCount` | `0`            |

Given one reusable configuration and no explicitly supplied instances.

When separate runs or trials execute it, sequentially or concurrently.

Then each run has independent data. Activity in one run does not change another run's data.

### S4. Deliberate sharing across runs

**Status: no longer a requirement. Decided by the developer on 2026-11-03: supply during run setup is removed without replacement, see [Q7](#q7-must-a-caller-be-able-to-share-one-live-object-across-runs). The API below still exists until the accounting handles are replaced. Current API:**

```csharp
var supplied = new CountAccumulator();
var first = limited.CreateRun(problem, random1,
    setup => setup.AddResource(handle, supplied));
var second = limited.CreateRun(problem, random2,
    setup => setup.AddResource(handle, supplied));
first.Resource(handle).IncrementBy(3);
second.Resource(handle).IncrementBy(2);
// both report 5
```

Given a caller supplies an existing instance during run setup.

When a configured participant requests it.

Then it receives the supplied instance. Supplying the same instance to several runs deliberately shares it. The live instance is not stored in the reusable configuration.

### S5. Simple cases stay simple

**Status: confirmed usability requirement.**

```csharp
var configured = algorithmA.LimitedToInvocations(mutator, 5);
```

The caller supplies two domain inputs: the observed operator and the limit. No handle, accumulator, aspect list or lifetime option is required. More advanced features must keep this entry point unchanged. Complex sharing may require explicit choices, but must follow consistent composition rules rather than special exceptions.

## Current behavior to confirm

### S6. Budget placement around a Cycle

**Status: current behavior, measured.**

**Setup.** In S6 and S7, A runs longer than in the shared example: one invocation of A produces up to three states, and each state costs three M calls. Without a budget, one A turn makes nine calls. B is unchanged: two calls, one state. Cycle runs two cycles with fresh executors.

```csharp
// Version 1: budget around A, inside the Cycle
var perTurn = CycleAlgorithm.Create(
        algorithmA.LimitedToInvocations(mutator, 5),
        algorithmB)
    with { MaximumCycles = 2 };

// Version 2: one budget around the whole Cycle
var wholeCycle = (CycleAlgorithm.Create(algorithmA, algorithmB)
        with { MaximumCycles = 2 })
    .LimitedToInvocations(mutator, 10);

// Version 3: both budgets, on the same mutator
var both = (CycleAlgorithm.Create(
            algorithmA.LimitedToInvocations(mutator, 5),
            algorithmB)
        with { MaximumCycles = 2 })
    .LimitedToInvocations(mutator, 10);
```

```text
Version 1                   Version 2                   Version 3

Cycle                       Budget 10 (counts M)        Budget 10 (counts M)
 ├→ Budget 5 (counts M)      └→ Cycle                    └→ Cycle
 │   └→ A → M                    ├→ A → M                    ├→ Budget 5 (counts M)
 └→ B → M                        └→ B → M                    │   └→ A → M
                                                             └→ B → M
```

**Today.** Values in parentheses are counter readings after each yielded state. Version 3 shows `inner / outer`.

| Turn             | Version 1: inner 5                         | Version 2: outer 10                 | Version 3: inner 5 and outer 10           |
| ---------------- | ------------------------------------------ | ----------------------------------- | ----------------------------------------- |
| A, cycle 1       | A1 (3), A2 (6): inner budget stops A       | A1 (3), A2 (6), A3 (9): A finishes  | A1 (3 / 3), A2 (6 / 6): inner stops A     |
| B, cycle 1       | B1 (not counted)                           | B1 (11): outer budget stops the run | B1 (– / 8)                                |
| A, cycle 2       | Fresh inner counter: A1 (3), A2 (6): stops | Not executed                        | Fresh inner: A1 (3 / 11): outer stops run |
| B, cycle 2       | B1 (not counted)                           | Not executed                        | Not executed                              |
| Yielded states   | 6                                          | 4                                   | 4                                         |
| Total calls to M | 16                                         | 11                                  | 11                                        |

**What this shows:**

- The inner budget restarts every turn, because Cycle constructs A and its budget afresh.
- The outer budget counts every M call beneath it, from A and B and across turns, because one budget executor owns one counter for the whole Cycle.
- In version 3, both budgets count the same M calls independently. The inner budget hands control from A to B. The outer budget ends the run.
- No handle, setup callback or sharing option is needed. Placement alone decides what is counted and how long the count lives.
- Both budgets stop at the first state after reaching their limit, so counts can exceed the limit: 6 > 5 and 11 > 10. See S9.

### S7. Executor reuse

**Status: decided by the developer on 2026-11-03. `ReuseExecutors` is removed; `cycle.Sharing(...)` keeps chosen configurations across turns. The table below records the measured behavior before the removal.**

**Setup.** S6 version 1 with reuse enabled:

```csharp
var reused = CycleAlgorithm.Create(
        algorithmA.LimitedToInvocations(mutator, 5),
        algorithmB)
    with { MaximumCycles = 2, ReuseExecutors = true };
```

**Today:**

| Turn             | Fresh executors (default)           | `ReuseExecutors = true`                                 |
| ---------------- | ----------------------------------- | ------------------------------------------------------- |
| A, cycle 1       | Build A. A1 (3), A2 (6): stop       | Build A. A1 (3), A2 (6): stop                           |
| B, cycle 1       | Build B. B1                         | Build B. B1                                             |
| A, cycle 2       | Build A again. A1 (3), A2 (6): stop | Reuse A, counter still 6. A1 (9): stops after one state |
| B, cycle 2       | Build B again. B1                   | Reuse B. B1                                             |
| Total calls to M | 16                                  | 13                                                      |

Reuse keeps the whole executor: its budget counter, its children and all their private fields. A's second turn ends after one state because the retained budget is already exhausted. It still performs three calls, because the budget checks only after a state.

**Developer assessment (2026-11-03).** Whole-executor reuse is rarely what a user wants. A realistic Cycle keeps some state and resets other state: for example, it keeps an evaluation cache or an adaptive mutation strength across turns, but restarts the inner budget or iteration terminator each turn. Which parts to keep depends on the user's intent, not on the operator type. Reuse must therefore be chosen per configuration node, not for a whole child algorithm.

Whole reuse adds nothing that per-node choice cannot express. If a child's stopping condition depends only on the search state passed in, fresh and reused stopping behave the same. Stateful parts that should continue can be kept individually. Keeping a complete child algorithm is the special case where that child is the node chosen.

The developer's lifetime binding rule replaces `ReuseExecutors` and, once detail 4 is implemented, the provisional resource handles. It is recorded in the [lifetime plan](operator-state-lifetimes.md#lifetime-binding-through-sharing-accepted-on-2026-11-03). A node can bind a configuration to its own lifetime with `Sharing`, and every use of that configuration beneath the node then shares one instance. Under the accepted default sharing, each Cycle turn resolves in a child scope, so turns stay fresh and `Sharing` on the Cycle is what keeps an executor across them.

### S8. Runtime analysis added during setup

**Status: behavior confirmed by the developer on 2026-11-03. Naming open.**

**Setup:**

```csharp
var trace = Analyzer.Trace(mutator.Invocations, observation => 1);
var cycle = CycleAlgorithm.Create(algorithmA, algorithmB)
    with { MaximumCycles = 2 };
var run = cycle.CreateRun(problem, random,
    setup => setup.AddAspect(trace));
run.Complete();
```

**Today:**

```text
M executor 1 (A, cycle 1): 3 observations ─┐
M executor 2 (B, cycle 1): 2 observations ─┤
M executor 3 (A, cycle 2): 3 observations ─┼→ one trace: 10 samples
M executor 4 (B, cycle 2): 2 observations ─┘
```

Adding the same `trace` object to a second identical run gives 20 samples in that object. A new trace object for the second run gives an independent 10.

**Confirmed behavior.** One trace given to the run records every selected call in the run, across fresh executors. A caller who passes the same trace to several runs collects all of them in one place.

**Superseded in part on 2026-11-04.** Results are bound to the run, so the same trace definition in two runs gives two independent results of 10 samples each; the within-run expectation stays ([design review](operator-state-lifetimes.md#design-review-on-2026-11-04)).

**Open naming concern.** `Trace` currently does two jobs. It selects what to observe and installs the aspects for it, and it also stores the recorded values. The name does not tell users that the object is a store which keeps accumulating when reused. The answer depends on Q1: with a configuration/runtime split, `Analyzer.Trace(...)` would describe what to record, and the recorded values would become a result of the run.

### S9. When budgets stop, and failed calls

**Decided by the developer on 2026-11-04:** both recommendations are accepted. A budget stops at the first state after its limit is reached and may overshoot by at most one state's work. A failed attempt does not count as an invocation, and durations include failed attempts. The same holds for a run-bound counter read by a terminator.

**Status: current behavior, measured.**

**Part 1: a budget stops between states, not between calls.**

**Setup.** In this part only, A makes four M calls per state and can produce up to three states.

```csharp
var limited = algorithmA.LimitedToInvocations(mutator, 5);
```

**Today:**

```text
4 calls, state 1 yielded   counter 4  → below 5, continue
4 calls, state 2 yielded   counter 8  → 5 reached, stop

Result: 2 states and 8 calls, although the limit is 5.
```

The budget wraps the stream of states that A produces. It can act only when A hands over a state. It cannot stop A halfway through producing a state. A strict limit, never more than five calls, would have to interrupt the operator call itself and discard a half-finished state.

**Question.** Is "stop at the first state after the limit is reached" the intended contract?

**Recommendation.** Yes. Document that budgets are checked between states and can exceed their limit by at most one state's work.

**Part 2: failed calls.** This only matters when something inside the run catches the failure. Under the [failure rule](#q6-failures), an uncaught exception ends the run, so its count no longer matters. Today, the only built-in operator that catches failures is `Retry`, which repeats a call after a `RetryableOperationException`.

**Setup.** M fails on its first call and then succeeds. A uses `mutator.Retry(maximumAttempts: 2)` as its mutator and calls it three times.

**Today:**

| What is measured                   | Result                                  |
| ---------------------------------- | --------------------------------------- |
| Attempts on M                      | 4: one failure, three successes         |
| Invocation count on M              | 3: the failed attempt is not counted    |
| Invocation count on the Retry node | 3                                       |
| Duration measured on M             | Includes the time of the failed attempt |

**Question.** Should a failed attempt count as an invocation for budgets, giving 4 instead of 3?

**Recommendation.** Keep the current behavior. Counts measure completed work. Durations measure time spent, including failures.

## Open behavioral questions

### Q1. Should aspects have a configuration/runtime split?

**Status on 2026-11-04: superseded.** The developer accepted [aspects as automatic composition](aspects-as-automatic-composition.md) on 2026-11-03: aspects automate the placement of ordinary execution wrappers, and every installed behavior must also be expressible through explicit configuration composition. That plan does not require an aspect executor hierarchy merely to mirror operators and algorithms. Aspects keep their runtime data, acting as sinks, for now. The question below is resumed in the [advice and scope layer discussion](operator-state-lifetimes.md#advice-and-scope-layers-under-discussion-since-2026-11-04) and kept for its scenarios.

**Setup, today.** An aspect is one runtime object, added during run setup:

```csharp
var trace = Analyzer.Trace(mutator.Invocations, observation => 1);
var run = algorithmA.CreateRun(problem, random,
    setup => setup.AddAspect(trace));
```

The `trace` object is both the description of what to observe and the store of recorded samples (S8).

**Proposed syntax, not implemented:**

```csharp
var trace = Analyzer.Trace(mutator.Invocations, observation => 1); // description only
var configured = algorithmA.WithAspects(trace);                     // an ordinary wrapper node
var first = configured.CreateRun(problem, random1);   // records its own 3 samples
var second = configured.CreateRun(problem, random2);  // records its own 3 samples
```

**Question.** Should aspects follow the same split as operators and algorithms: an immutable configuration in the graph, and a runtime part created during execution that holds the recorded data?

**Options:**

|                                  | Aspects stay runtime objects (today)               | Configuration/runtime split                                      |
| -------------------------------- | -------------------------------------------------- | ---------------------------------------------------------------- |
| Where attached                   | Run setup only                                     | Anywhere in the graph through `WithAspects`, or during run setup |
| Reusing a configuration for runs | Caller creates a new trace per run, or data merges | Each run gets its own recording                                  |
| Consistency with operators       | Different rule                                     | Same rule: configurations describe, runtime parts hold state     |
| One sink shared across runs      | Natural: pass the same object                      | Needs an explicit way, see Q7                                    |
| Reading results after the run    | Read the trace object                              | Needs a query on the run, see Q5                                 |

**Developer leaning (2026-11-03): yes.** If operators and algorithms have the split and aspects do not, the library is inconsistent. `algorithm.WithAspects(...)` returning a new configured algorithm fits the existing fluent wrappers, such as `LimitedToInvocations`. It should work for algorithms and operators.

**Who performs the weaving.** The `WithAspects` node would, when its own executor is created:

1. create the runtime parts of its aspects,
2. resolve its child with those aspects added to the resolution scope,
3. return the child's executor.

The resolution scope still wraps each matching executor during resolution, as it does today. The wrapper decides which aspects apply to which subtree. `LimitedToInvocations` already works exactly this way (see S6): it creates a counter and counting advice and resolves its child with that aspect. So this is an existing pattern, not a new mechanism. `LimitedToInvocations` would become shorthand for a budget plus `WithAspects`.

**Still open:** the shared sink (Q7) and reading results (Q5).

### Q2. Observing only A's calls when A and B share M

**Setup.** A Cycle runs A and B once each. Both use the same M configuration. The caller wants to count or trace only the M calls that A makes.

**Today.** An aspect added during run setup applies to the whole run. It sees 5 calls, three from A and two from B. There is no way to attach it to A alone. The only way to get 3 is to give A a distinct mutator configuration and observe that one:

```csharp
var namedForA = mutator.Named("A mutator");
// A configured with namedForA as its mutator, B keeps mutator
var trace = Analyzer.Trace(namedForA.Invocations, observation => 1); // 3 samples
```

**Confirmed (2026-11-03):** the expected result is 3. B's 2 calls are excluded.

**What remained open,** in plain terms: there are two places to write this.

```csharp
// (a) attach to A. A and B still share the same M configuration.
var cycle = CycleAlgorithm.Create(algorithmA.WithAspects(trace), algorithmB);

// (b) attach to A's mutator. A gets its own wrapped mutator configuration.
var cycle = CycleAlgorithm.Create(
    algorithmA with { Mutator = mutator.WithAspects(trace) }, algorithmB);
```

Both give 3. The earlier question was whether the library must support (a) or whether (b) is enough.

**Proposed resolution.** If Q1 makes `WithAspects` available for both algorithms and operators, both forms follow from the same rule. Nothing more needs deciding. Only (b) is possible today, through `Named`.

### Q3. Is a wrapper node counted as an algorithm?

**Status: decided by the developer on 2026-11-04.** Count it like every other node: the attachment wrapper, named `Advised`, is an algorithm node that a pointcut selecting every algorithm counts, like `Named` and budgets.

**Setup, today.** A caller counts every algorithm execution in a run with a pointcut that selects all algorithm nodes:

```csharp
var counter = new CountAccumulator();
var everyAlgorithm = Aspect.Create(
    Pointcut.OfType<IAlgorithm<RealVector, PopulationState<RealVector>>>(),
    new CountingAlgorithmAdvice<RealVector, PopulationState<RealVector>>(counter));
```

**Today:**

| Configuration                        | Algorithm nodes that run        | Count |
| ------------------------------------ | ------------------------------- | ----- |
| `A`                                  | A                               | 1     |
| `A.Named("A")`                       | Named, A                        | 2     |
| Cycle(A, B), two cycles              | Cycle, A, B, A, B               | 5     |
| Cycle(`A.Named("A")`, B), two cycles | Cycle, Named, A, B, Named, A, B | 7     |

Every configuration node can be selected by a pointcut. `Named` is itself an algorithm node, so it is counted in addition to A. Budgets from `LimitedToInvocations` are algorithm nodes too and are counted the same way.

**Question.** `A.WithAspects(x)` would also be a wrapper node. Should a pointcut selecting every algorithm count it?

**Options:**

| Option                         | Count for Cycle(`A.WithAspects(x)`, B) | Cost                                                   |
| ------------------------------ | -------------------------------------- | ------------------------------------------------------ |
| Counted, like every other node | 7                                      | None. Same rule as `Named` and budgets                 |
| Invisible to pointcuts         | 5                                      | A special rule: one node type that pointcuts must skip |

**Recommendation.** Count it like every other node. Pointcuts that select every algorithm are rare, and they already see `Named` and budget wrappers.

### Q4. Attaching counting twice

**Status: decided by the developer on 2026-11-04.** With [run-bound results](operator-state-lifetimes.md#advice-and-scope-layers-under-discussion-since-2026-11-04), the run creates one aspect per definition, so the same definition attached twice records once: case 1 gives 3. Case 2 can no longer be written, because a result belongs to one definition; two definitions give two results of 3 each. For low-level aspects, the advice whose target matches is selected first and each distinct advice applies once, so case 3 gives 3. The order of application is not first attachment as recommended below, but the rule recorded under the [design review](operator-state-lifetimes.md#design-review-on-2026-11-04): child scope before parent, later added aspects closer within a scope, listed order within an aspect and the closest duplicate kept. Explicit placement does what is written, so `x.Counted(c).Counted(c)` counts twice.

**Setup.** A calls M three times. One counter. Counting on M is attached in three different ways.

```csharp
// Case 1: the same aspect object added twice
var counting = mutator.CountInvocations(counter);
setup.AddAspect(counting).AddAspect(counting);

// Case 2: two separately created aspects writing to the same counter
setup.AddAspect(mutator.CountInvocations(counter))
     .AddAspect(mutator.CountInvocations(counter));

// Case 3: one advice object in two aspects: first aimed at another operator X, then at M
var advice = new CountingOperatorAdvice<Mutation, RealVector>(counter, OperatorCountMetric.Invocations);
setup.AddAspect(Aspect.Create(Pointcut.Reference(otherOperator), advice))
     .AddAspect(Aspect.Create(Pointcut.Reference(mutator), advice));
```

**Today:**

| Case | Counter after A's 3 calls | Why                                                                                                  |
| ---- | ------------------------- | ---------------------------------------------------------------------------------------------------- |
| 1    | 3                         | Each advice object is applied at most once per node                                                  |
| 2    | 6                         | Two different advice objects both apply, and both increment the same counter                         |
| 3    | 0                         | Duplicates are removed before targets are checked. The pairing aimed at X wins, and M is not counted |

Case 3 is surprising and probably a defect.

**Why "apply once" exists.** Two traces can share one clock, for example a call counter used as the time axis by both. The clock must tick once per call, not once per trace.

**Question.** What single rule should decide all three cases?

**Recommendation.** First select the advice whose target matches the node, then apply each distinct advice once, in the order it was first attached. Results: case 1 gives 3, case 2 gives 6 and case 3 gives 3. Two traces sharing a clock still tick it once. If Q1 adopts the split, the same rule applies to aspect configurations.

### Q5. Does analysis inside a Cycle child restart every turn?

**Status: decided by the developer on 2026-11-04, against the recommendation below.** Results are bound to the run, one per definition ([lifetime plan](operator-state-lifetimes.md#advice-and-scope-layers-under-discussion-since-2026-11-04)). A trace attached inside A's turns continues across turns without `Sharing`: one recording of 6 samples, read with `run.Result(trace)`. The per-instance `run.Results(trace)` list is not added. A user asking to trace an operator in a Cycle expects one trace for the run and should not need to know about executors.

This question exists only if Q1 adopts the split.

**Setup, proposed syntax:**

```csharp
var trace = Analyzer.Trace(mutator.Invocations, observation => 1);
var cycle = CycleAlgorithm.Create(algorithmA.WithAspects(trace), algorithmB)
    with { MaximumCycles = 2 };
```

Cycle constructs A afresh every turn. The `WithAspects` node is part of A's subtree, so it is constructed afresh too, and creates a new runtime part for `trace` every turn. That is the same default that restarts the inner budget in S6 version 1.

**Options:**

| Behavior                    | What is recorded for A's 2 × 3 calls | How the caller gets it                                         |
| --------------------------- | ------------------------------------ | -------------------------------------------------------------- |
| Restart each turn (default) | Two recordings of 3 samples each     | Nothing extra                                                  |
| Continue across turns       | One recording of 6 samples           | Bind `trace` at the Cycle, under the lifetime binding proposal |

Attaching the trace at the root of the run, as in S8, gives one recording of all 10 calls from A and B.

**Reading results.** The recorded data lives in runtime parts inside the run, not in the configuration. The caller therefore needs a query on the run, for example:

```csharp
// Proposed syntax
IReadOnlyList<TraceResult> recordings = run.Results(trace);
```

It would return one entry per runtime part the run created: two when restarting, one when bound.

**Question.** Is "restart each turn, bind to continue" the right default for analysis attached inside a Cycle child, and should results be read from the run per created instance?

**Recommendation.** Yes. It is the same rule as for operators and budgets, so there is nothing new to learn.

### Q6. Failures

**Status: decided by the developer on 2026-11-04.** The outcomes below are accepted: a failure surfaces from `Stream` or `Complete`, the run becomes terminally `Failed` and results recorded before the failure stay readable, which run-bound results guarantee. A construction failure also ends the scope tree, as recorded in the [lifetime plan](operator-state-lifetimes.md#advice-and-scope-layers-under-discussion-since-2026-11-04).

**Confirmed developer rule (2026-11-03):**

- Nothing in the construction process catches failures. An exception propagates to whoever started the work.
- During execution, an exception in an operator ends that iteration. No new state is produced. The caller may still hold the previous state, but there is no guarantee that operators or the algorithm are still in a valid state.

**Measured correction.** Executors are built when execution starts, at the first `Stream` or `Complete`, not in `CreateRun`. Cycle builds child executors at the start of each turn. Construction can therefore fail in the middle of a run.

**Setup.** Cycle(A, B), where B's construction throws, with a trace on M added during setup.

**Today:**

| Step                   | Result                                                        |
| ---------------------- | ------------------------------------------------------------- |
| `CreateRun(...)`       | Returns a run in state `Ready`                                |
| `run.Complete()`       | A's turn runs, then B's construction throws out of `Complete` |
| `run.LifecycleState`   | `Failed`                                                      |
| `run.Complete()` again | Throws: a failed run cannot continue                          |
| Trace                  | Still holds A's 3 samples                                     |

A run object therefore exists after a construction failure, but it is terminal. Data recorded before the failure stays readable.

One deliberate exception to "nothing catches": role helpers such as `TryResolve` let an operator author check whether a child supports an operation. They turn `InvalidOperationException` into a reason instead of propagating it.

**Question.** Are these outcomes acceptable: failure surfaces from `Stream` or `Complete`, the run becomes terminally `Failed` and earlier recorded data stays readable?

**Recommendation.** Yes. They follow the developer's rule. Constructing everything during `CreateRun` cannot cover Cycle's per-turn construction anyway.

### Q7. Must a caller be able to share one live object across runs?

**Status: decided by the developer on 2026-11-03.** `AddResource`, `AddResourcePerTrial`, `SuppliedResourceHandle` and the experiment supply are removed without replacement, which matches `main`. The cross-run examples below are deferred to the [developer backlog](developer-backlog.md#scenarios-awaiting-a-concrete-need). The cache supply is already gone; the rest goes with the accounting handles.

**Setup.** S4 shows the current way: the caller supplies an instance during setup. Under lifetime binding, every run builds its own instances, so sharing across runs needs an explicit decision.

**Examples where a caller might want it:**

1. **Warm cache across repetitions.** Thirty repetitions of an algorithm on an expensive problem share one evaluation cache, so repeated candidates are evaluated only once. The trials are then no longer independent, and their results depend on execution order.
2. **Global budget across an experiment.** All trials together may use at most one million evaluations. This is common in tuning or racing setups.
3. **One sink for many runs.** All runs write observations to one log, file, database, dashboard or Python consumer.
4. **Knowledge carried across runs.** A surrogate model, archive or hall of fame keeps improving from run to run.
5. **Externally owned services.** A connection pool, GPU evaluation service or rate-limited remote client is created once by the application and disposed by it.

**Assessment.** Examples 3 and 5 are common, and the object is already owned by the caller. Examples 1, 2 and 4 deliberately give up trial independence and are rarer.

**Question.** Which of these must the library support, and must a caller-owned object reach the configuration through something other than an ordinary reference, such as a constructor argument or a captured callback?

**Recommendation.** Support examples 3 and 5 through ordinary caller-owned references, because the caller explicitly wants one shared object there. Defer 1, 2 and 4 until a concrete use case requires them. Note that this allows a live reference in a configuration when the caller provides it on purpose, which needs explicit agreement given the immutability goal.

## Next discussion

Decided since the 2026-11-03 revision: S7 (`ReuseExecutors` removed), Q7 (cross-run supply removed, examples deferred) and the expected result of Q2, whose attachment forms follow from the aspects design. Q1 is superseded by the aspects plan. On 2026-11-04 the developer decided Q5 (results are bound to the run), Q3, Q4, S9 and Q6. No behavioral question in this spec remains open.

For each answer, record acceptance explicitly in this spec. Once the scenarios are agreed, compare designs against them and implement representative executable specs. A passing test of the old implementation does not count as acceptance.
