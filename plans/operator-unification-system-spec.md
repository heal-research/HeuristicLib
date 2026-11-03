# Operator unification system spec

Status: initial behavioral draft on 2026-10-29. This document separates confirmed expectations from questions that still need answers. It does not approve an architecture or authorize implementation. Proposed syntax in the other plans is not a requirement here.

The governing goal is simple common usage and complex behavior composed from a small set of consistent rules. Specify observable outcomes before selecting types, wrappers, construction stages or caches. Existing behavior is evidence, not automatic acceptance of every current rule.

## How to read this spec

- **Confirmed** means the developer has explicitly established the expectation in the discussion.
- **Current behavior** describes the implementation and still needs confirmation if it becomes a requirement for the rework.
- **Open** means the expected outcome has not been agreed. A recommendation is not an answer.

Each scenario identifies what is given, what happens and what the caller can observe. Terms follow the [glossary](../docs/guide/glossary.md). Design alternatives remain in the [operator rework plan](operator-role-parameterization-rework.md) and [lifetime plan](operator-state-lifetimes.md).

## Concrete example used throughout

The examples below use these hypothetical test fixtures. Their behavior is specified here, not inferred from a production algorithm:

```csharp
IOperator<Mutation, RealVector> mutator = /* shared configuration M */;
IAlgorithm<RealVector, PopulationState<RealVector>> algorithmA = /* A */;
IAlgorithm<RealVector, PopulationState<RealVector>> algorithmB = /* B */;
```

- A and B reference the exact same mutator configuration M.
- Each invocation of A makes three successful calls to M, yields one state and finishes.
- Each invocation of B makes two successful calls to M, yields one state and finishes.
- Each mutation call processes one candidate. No other operator invokes M.
- `problem` is compatible with both algorithms. `random`, `random1` and `random2` are suitable RNG instances.
- Unless stated otherwise, Cycle runs at most two cycles with fresh child executors.

Code labeled **Current API** uses existing entry points but is an illustrative fragment using these fixtures. These snippets have not been compiled or executed as tests. Code labeled **Proposed syntax** is not implemented and does not settle the public API.

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

  One shared configuration M, four contextual executor constructions.
  An explicitly shared resource may connect them without merging them.
```

Arrows in these diagrams mean child references or calls. Resource connections are labeled separately. A configuration diagram is not an executor cache or a lifetime diagram.

## Confirmed expectations

### S1. Simple private accounting

**Status: confirmed ownership expectation. Current API:**

```csharp
var limited = algorithmA.LimitedToInvocations(mutator, 5);
var first = limited.CreateRun(problem, random1);
var second = limited.CreateRun(problem, random2);
```

```text
Configuration: Budget(limit 5, observes M) → A → M

first execution:  Budget executor 1 owns C1: 0 → 3
second execution: Budget executor 2 owns C2: 0 → 3

C1 and C2 are distinct. Neither counter is stored in the configuration.
Both executions finish naturally after A's one state, below the limit.
```

The private counters are explanatory labels, not public result handles. The observable checks are independent execution and identical stopping behavior.

Given an algorithm configured to stop after a selected operator completes a specified amount of work, with no explicit sharing requested.

When its budget executor is constructed.

Then it receives a fresh private counter. The caller does not need to configure a handle or supply a counter. The reusable configuration does not retain the mutable counter.

The exact work unit and the point at which stopping takes effect are separate questions below.

### S2. Explicit sharing within a run

**Status: confirmed resource identity. Current API for the existing accounting connection:**

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

This is the cumbersome current setup to improve. There is no existing handle overload of `mutator.CountInvocations` in this checkout. The desired simpler configuration syntax remains open.

Given independently configured participants that name the same resource handle.

When they obtain that resource in one run, including through different aspect contexts or fresh child constructions.

Then they obtain the same resource instance. Sharing the resource does not itself share their executors, private fields or contextual advice.

### S3. Independent default resources across runs

**Status: confirmed. Current API using the `limited` configuration and `handle` from S2, without its setup callback:**

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

This isolates resource identity. The direct increment represents a resource writer. These runs have no counting advice installed, so this example does not claim that executing them automatically measures M.

Given one reusable configuration and its resource handles, with no supplied runtime instances.

When separate runs or trials execute it, sequentially or concurrently.

Then each run obtains independent default resources. Activity in one run does not change the other run's counters through those handles.

### S4. Deliberate runtime supply

**Status: confirmed. Current API:**

```csharp
var supplied = new CountAccumulator();
var first = limited.CreateRun(problem, random1,
    setup => setup.AddResource(handle, supplied));
var second = limited.CreateRun(problem, random2,
    setup => setup.AddResource(handle, supplied));
first.Resource(handle).IncrementBy(3);
second.Resource(handle).IncrementBy(2);
```

```text
Run 1 resource table: H ─┐
                        ├→ supplied counter C = 5
Run 2 resource table: H ─┘
```

Both resource reads return the same instance and report `5`. As in S3, the explicit increments isolate sharing from instrumentation attachment.

Given a caller supplies an existing resource under a handle during run setup.

When a configured participant requests that handle.

Then it receives the supplied instance. Supplying the same instance to several runs deliberately shares it. The live instance is not stored in the reusable configuration.

### S5. Explicit complexity

**Status: confirmed usability requirement. Current simple case:**

```csharp
var configured = algorithmA.LimitedToInvocations(mutator, 5);
```

The caller supplies two domain inputs: the observed operator and the limit. The acceptance check is that no handle, accumulator, aspect list or lifetime option is required for this case. More advanced alternatives must preserve this entry point's simplicity.

Given a caller only needs a simple private budget.

When they configure it.

Then they provide the observed operator and limit without configuring sharing or lifetimes. More complex sharing may require explicit choices, but must follow consistent composition rules rather than special exceptions.

## Current behavior to confirm

### S6. Budget placement around a Cycle

**Status: current behavior to confirm. Current API:**

```csharp
var perTurn = CycleAlgorithm.Create(
    algorithmA.LimitedToInvocations(mutator, 5), algorithmB)
    with { MaximumCycles = 2 };

var wholeCycle = (CycleAlgorithm.Create(algorithmA, algorithmB)
    with { MaximumCycles = 2 })
    .LimitedToInvocations(mutator, 5);
```

```text
Private budget inside A             Private budget outside Cycle

Cycle                               Budget(limit 5, observes M)
  ├→ Budget(limit 5, observes M)       └→ Cycle
  │    └→ A → M                            ├→ A → M
  └→ B → M                                 └→ B → M
```

| Scheduled turn            | Inside A                            | Outside Cycle                  |
| ------------------------- | ----------------------------------- | ------------------------------ |
| A1 makes 3 calls          | C1: `0 → 3`, yield A1               | C: `0 → 3`, yield A1           |
| B1 makes 2 calls          | Not counted by A's budget, yield B1 | C: `3 → 5`, yield B1 then stop |
| A2 makes 3 calls          | Fresh C2: `0 → 3`, yield A2         | Not executed                   |
| B2 makes 2 calls          | Not counted by A's budget, yield B2 | Not executed                   |
| Observable yielded states | A1, B1, A2, B2                      | A1, B1                         |

No resource handle is needed for either placement. The enclosing budget's runtime advice reaches fresh child constructions through the existing context.

Given fresh Cycle turns and a private budget placed inside child A.

When Cycle constructs A again.

Then A's newly constructed budget has a fresh counter.

Given instead one private budget enclosing the Cycle.

When fresh A and B executors invoke the selected operator through that enclosing context.

Then they contribute to the enclosing budget's existing counter. An explicit handle is unnecessary for this one composition's internal accounting.

### S7. Executor reuse

**Status: current behavior to confirm. Current API:**

```csharp
var limitedA = algorithmA.LimitedToInvocations(mutator, 3);
var cycle = CycleAlgorithm.Create(limitedA, algorithmB)
    with { MaximumCycles = 2, ReuseExecutors = true };
```

| Event                       | Counter owned by A's retained budget executor |
| --------------------------- | --------------------------------------------- |
| First A invocation starts   | `0`                                           |
| A1 makes 3 calls and yields | `3`, then the budget ends that enumeration    |
| Second A invocation starts  | Still `3`                                     |
| A2 makes 3 calls and yields | `6`, then the budget ends that enumeration    |

An exhausted counter does not imply that the current budget prevents the next invocation from doing work. Its check follows the yielded state. Retaining the executor also does not retain a completed invocation's iterator. Whether this stopping behavior is desired belongs to S9.

Given a Cycle configured to reuse an executor.

When it invokes that executor again.

Then its private fields, child executors and advice remain retained. Exhausted private budget data remains exhausted. Reuse is not a reset operation.

### S8. Existing runtime analysis

**Status: current behavior to confirm. Current API:**

```csharp
var trace = Analyzer.Trace(mutator.Invocations, observation => 1);
var cycle = CycleAlgorithm.Create(algorithmA, algorithmB)
    with { MaximumCycles = 2 };
var run = cycle.CreateRun(problem, random,
    setup => setup.AddAspect(trace));
run.Complete();
var samples = trace.SampleCount; // 10
```

```text
M executor 1: 3 observations ─┐
M executor 2: 2 observations ─┤
M executor 3: 3 observations ─┼→ one installed trace: 10 samples
M executor 4: 2 observations ─┘
```

Reusing this same runtime trace in another identical run currently produces 20 total samples. Constructing a new trace for that run produces an independent history of 10. Neither behavior requires a new result resource type.

Given one runtime analyzer installed for a run and selecting a child configuration.

When that child receives fresh executors over several Cycle turns.

Then observations contribute to that analyzer's continuing history. Fresh executor construction does not recreate the installed analyzer. Separate analyzers created for separate trials have separate histories.

### S9. Budget stopping and failed calls

**Status: current behavior to confirm. Concrete execution timeline:**

For this scenario only, an algorithm makes four successful mutation calls before each yielded state and can produce at least two states. Its invocation budget is five.

```text
Start                          counter 0
Four calls, yield state 1       counter 4 → continue
Four calls, yield state 2       counter 8 → stop

Observed result: 8 calls and 2 yielded states, despite a limit of 5.
```

For a separate failing call that takes 7 ms:

| Measurement                     | Change from that failed call |
| ------------------------------- | ---------------------------- |
| Successful invocation count     | `0`                          |
| Successful candidate work count | `0`                          |
| Invocation duration             | `+7 ms`                      |

The decision is whether this cooperative stopping contract is sufficient. A strict maximum of five calls is a different behavior and must not be inferred from the example's limit.

Given the current invocation or candidate budget.

When a selected operation succeeds.

Then its accounting advances. Failed calls do not advance those counts. Duration accounting includes failed calls.

When the algorithm yields a state and the budget has been reached.

Then the current budget stops further iteration after that yielded state. It does not reserve work or prevent every invocation that could exceed the limit. Concurrent strict reservations are not currently provided.

## Open behavioral questions

### Q1. What must be reusable together?

**Current API: analysis is attached during runtime setup.**

```csharp
var configured = algorithmA.LimitedToInvocations(mutator, 5);
var trace = Analyzer.Trace(mutator.Invocations, observation => 1);
var run = configured.CreateRun(problem, random,
    setup => setup.AddAspect(trace));
```

**Proposed syntax, not implemented: analysis selection travels with the configuration.**

```csharp
var configured = algorithmA.WithAspects(analysisDescription);
var first = configured.CreateRun(problem, random1);
var second = configured.CreateRun(problem, random2);
```

| Outcome to decide                                | Runtime setup approach         | Reusable description approach          |
| ------------------------------------------------ | ------------------------------ | -------------------------------------- |
| Running the configuration without analysis setup | No trace                       | Configured analysis runs automatically |
| Obtaining independent histories                  | Construct a trace for each run | Must be defined by the new behavior    |
| Copying or passing the configuration onward      | Analysis is separate           | Analysis selection travels with it     |

`analysisDescription` is intentionally unspecified. This question is about the required outcome, not its type.

Given an algorithm with chosen accounting and analysis.

When a caller saves that configuration for later runs or passes it into an experiment.

Must accounting and analysis selections travel with it automatically, or may analysis be selected separately during run or experiment setup?

Resolve accounting and arbitrary analysis separately. A requirement for configured shared counting does not establish a requirement for reusable callback bearing traces.

### Q2. What does local observation select?

**Current API: attaching a reference selected trace at the root observes both paths.**

```csharp
var trace = Analyzer.Trace(mutator.Invocations, observation => 1);
var cycle = CycleAlgorithm.Create(algorithmA, algorithmB)
    with { MaximumCycles = 1 };
var run = cycle.CreateRun(problem, random,
    setup => setup.AddAspect(trace));
```

```text
Cycle
  ├→ A → M: 3 calls ─┐
  └→ B → M: 2 calls ─┴→ current root trace: 5 samples

Desired local observation: A's 3 samples only, excluding B's 2.
```

**Open:** must the caller achieve the local result while A and B keep the same configured M reference, or may they configure a distinct observed boundary for A? Both can express three samples, but place different requirements on the system.

Given A and B both reference the same mutator configuration, and the caller wants observations only for A.

When both execute.

Must the caller be able to attach that observation to A without changing its mutator parameter, or is explicitly configuring a distinct observed boundary for A sufficient?

The expected observation set is the decision. A wrapper, pointcut path or attachment API is not yet the answer.

### Q3. Does adding instrumentation add an observable boundary?

**Current composition example:** a broad algorithm invocation counter selects both `algorithmA.Named("A")` and its child A. When the single yielded state is consumed and enumeration completes, there are two completed selected algorithm boundaries rather than one. Ordinary naming already follows this rule.

```text
Before attachment          Ordinary selectable attachment    Transparent attachment
A → M                      Attachment → A → M                [attachment context] A → M
1 algorithm boundary       2 algorithm boundaries             1 algorithm boundary
```

The two attachment columns are competing proposed semantics. Neither is selected. The observable decision is whether the broad measurement changes from one to two when instrumentation is added.

Given a broad observation selecting all algorithms or operators.

When another caller adds local instrumentation without changing the underlying work.

Should the broad observation receive exactly the same events as before, or may it also observe the configured instrumentation boundary?

This outcome must be decided before choosing ordinary selectable composition or transparent attachment behavior.

### Q4. What does repeating an attachment mean?

**Current API for one shared advice instance:**

```csharp
var counter = new CountAccumulator();
var shared = mutator.CountInvocations(counter);
var run = algorithmA.CreateRun(problem, random,
    setup => setup.AddAspect(shared).AddAspect(shared));
run.Complete();
// counter.CurrentCount == 3
```

| Attachment for A's 3 calls                                           | Current counter result                       |
| -------------------------------------------------------------------- | -------------------------------------------- |
| Same runtime counting aspect attached twice                          | `3`                                          |
| Two calls to `mutator.CountInvocations(counter)`, each attached once | `6`                                          |
| Same advice paired first with a nonmatching pointcut, then with M    | `0`, because deduplication precedes matching |

These are current results to evaluate. They do not settle the behavior of any future reusable description.

Given one counting or clock definition included by two observations.

When one selected operation executes.

Should it advance once because the definition is shared? If two independently created definitions write to the same counter, should it advance twice? If their selections differ, should those selections combine or should the first occurrence win?

Existing reference deduplication is evidence for an answer, not a substitute for describing the expected result.

### Q5. What state must persist in complex analysis?

**Concrete input sequence:** A's first turn reports values `[8, 6]`. Its second reports `[7, 5]`. Smaller is better and retention keeps every second observation.

| State after the second turn                        | If analysis continues | If analysis restarts each turn     |
| -------------------------------------------------- | --------------------- | ---------------------------------- |
| Running best after the first observation of turn 2 | `6`                   | `7`                                |
| Retained best values in one continuing history     | `[6, 5]`              | Separate histories `[6]` and `[5]` |
| Observation clock                                  | `4`                   | `2` for the current turn           |

To distinguish retention progress itself, also use a first turn with three observations and a second with one. Continuing every second retention records the second turn's first observation. Restarting retention does not.

The table supplies possible outcomes, not approved behavior. Repeat the same comparison across two independent runs and two deliberately coupled runs before selecting ownership rules.

Given analysis spanning several fresh child executions.

When execution moves to another turn or another run.

Which values continue and which restart: history, best so far, retention progress and clock readings? When callers deliberately share results across runs, which of those values should be shared together?

Answer only for analysis workflows required by Q1. Do not design a universal collector lifecycle in anticipation of an unanswered requirement.

### Q6. What should failures leave observable?

**Concrete resource and construction timeline:**

```text
Attempt 1: create resource for H → factory throws → no resource published
Attempt 2: create resource for H → publish C → executor construction throws
Attempt 3: request H → obtain the same C → executor construction succeeds
```

This is current direct resolution behavior. It does not mean an `AlgorithmRun` that entered `Failed` can be resumed.

For an unsupported candidate measurement, use a selected termination operation, which has no candidate work. Current operator advice checking rejects it before constructing that selected operator. Earlier siblings may already have created resources. Whether new configuration forms must preserve exactly that ordering is the open requirement.

Given an unsupported measurement, a failing resource creation or a failed executor construction.

When construction fails and a caller inspects results or retries through direct resolution.

Which resources may already exist, which data remains available and what is retried? Distinguish direct resolution from restarting a terminally failed run. Confirm the current guarantees against concrete user needs before generalizing failure ordering across all construction paths.

## Next discussion

Start with Q1 and Q2. They establish what the system must let the caller express. Then specify event boundaries and repeated attachment behavior through Q3 and Q4. Analysis lifecycle follows the required workflows. Technical failure examples follow the chosen behavior.

For each answer, add a concrete scenario with expected counts or observations and mark its acceptance explicitly. Once the necessary scenarios are agreed, compare designs against them and implement representative executable specs. Do not infer acceptance from a passing test of the old implementation.
