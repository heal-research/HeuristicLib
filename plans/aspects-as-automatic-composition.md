# Aspects as automatic composition

Status: architectural direction accepted by the developer on 2026-11-03. This document records the intended contract and work to address later. It does not claim that the current implementation satisfies the contract or approve a particular public API or implementation design.

Related work: [operator state lifetimes](operator-state-lifetimes.md), [operator unification system spec](operator-unification-system-spec.md) and [operator rework section 11](operator-role-parameterization-rework.md#11-aspect-configuration-rework-and-independent-review). This decision governs the responsibility of aspects when those proposals are resumed. Their unresolved APIs and lifetime designs are not accepted by implication.

## Problem

Users should be able to compose execution behavior explicitly. Applying the same behavior at many selected boundaries can become tedious, especially in nested algorithms and during deferred construction. Aspects should automate that placement without becoming the exclusive way to express a behavior or manage its data.

The current executor wrapping machinery is a suitable foundation. However, analysis collection and internal observation attachment currently depend on aspects in ways that ordinary configuration composition cannot fully reproduce through the public API.

## Accepted decisions

### Governing contract

> Aspects automate the placement of ordinary execution wrappers. Every behavior installed by an aspect should also be expressible through explicit configuration composition, with equivalent execution behavior and state lifetime.

The developer explicitly selected this direction. Aspects and manually placed wrappers are two ways to compose the same execution behavior.

The explicit path must work through supported consumer APIs without installing aspects or calling advice to perform the composition. Reimplementing a built in collector or copying its behavior into consumer code is not sufficient evidence that the delivered functionality has both paths.

### Configuration and execution graphs

Explicit composition places the relevant wrapper configurations in the configuration graph. Aspect declarations instead select boundaries and automate construction of their execution wrappers. The complete expansion of automatically applied wrappers exists in the execution graph.

An attachment declaration may itself be represented in the configuration graph if that is the chosen API. This does not require one configuration node for every automatically installed wrapper. The attachment API remains open.

### Equivalent behavior

For equivalent selected boundaries, composition order and runtime dependencies, both paths must preserve:

- Operation results and algorithm progress
- Invocation and candidate accounting
- Observation content and callback order
- Failure and cancellation behavior
- Relevant executor controls
- State ownership, reset boundaries and deliberate sharing

For example, a counting composition around an evaluator and counting advice applied to that evaluator must count the same successful calls. A timing composition and timing advice must include the same work and handle failed calls consistently. No new counting or timing policy is selected by this decision.

The configuration graphs need not have identical node identities or topology. Explicit wrappers introduce selectable configurations that automatically installed execution wrappers do not. A pointcut selecting every configuration can therefore see different structural nodes. This qualification does not excuse differences in the intended decorated computation or collected results. Representative specs must name the compared boundaries explicitly.

### Explicit wrapper configurations, accepted on 2026-11-04

Counting, timing and observation each get one ordinary wrapper configuration per concern that works for any operation, such as `op.Counted(...)`, `op.Timed()` and `op.Observed(...)`. The names are placeholders. The matching advice reuses the same execution wrapper. The configuration's executor owns its data, while advice passes in the accumulator it was given, so advice wrappers keep no private data. A generic advised wrapper per role, rejected on 2026-10-14 because it multiplies roles by concerns, stays rejected. Keeping instrumentation advice only was rejected because it contradicts the governing contract.

Observation sources describe the wrapper both paths use: a configuration and a function that wraps an executor so that each call produces an observation. Observation advice and `x.Observed(d)` call the same function, and third-party operations supply it through the public `ObservationSource.Create`. Accepted after the [design review](operator-state-lifetimes.md#design-review-on-2026-11-04) on 2026-11-04.

Advice belongs to its resolution scope rather than to layers within it, decided in the [lifetime plan](operator-state-lifetimes.md#advice-and-scope-layers-under-discussion-since-2026-11-04) on the same day.

### Trace collection separated from attachment, accepted on 2026-11-04

`TraceAnalyzer<T>` stops being an `Aspect`. The trace becomes a plain collector of entries, retention, clocks and snapshots with a public way to record. Attaching it automatically is a separate step that produces an aspect; attaching it explicitly is an `Observed(...)` wrapper configuration that records into it. Clocks, which also derive from `Aspect` today, become data fed by counting or timing, through a wrapper configuration or through advice. Keeping the trace an aspect with an added public recording method was rejected because collection and attachment would stay combined. API shapes are open.

Collected results are bound to the run, one per definition, decided in the [lifetime plan](operator-state-lifetimes.md#advice-and-scope-layers-under-discussion-since-2026-11-04) on 2026-11-04. Explicit wrappers and advice write into the same result, so both paths have the same data lifetime wherever they are placed. This supersedes the decision of the same day that executors own collected data, and answers question 4 for counters, recordings and clocks.

For Cycle's internal events, the developer prefers existing mechanisms over a separate observation point concept that exists only for observation. Options compared on 2026-11-04: a replaceable reporting slot whose default is the point, which fails because every `with` copy gets a new point; an extra reporting slot next to the point; and replacing points with ordinary operator slots. **Selected by the developer on 2026-11-04:** ordinary operator slots, preferably the existing interception operation rather than new operations: an interceptor after each turn and one after each cycle, which transform the state passed on. Observation points, new on this branch with Cycle as their only user, would then be removed.

**Accepted by the developer on 2026-11-04, superseded the same day as recorded below:** Cycle gets `TurnInterceptor` and `CycleInterceptor` slots of the existing interception operation, named after `Interceptor`; no new operator role is added.

- After each turn, `currentState` is the state the turn ended with and `previousState` the state it started from. After each cycle, they are the states after its last turn and at its start. At the end of a cycle the turn interceptor runs first. The returned state is what the next turn starts from, so migration, re-seeding or re-evaluation between algorithms becomes possible.
- Observation, counting and timing of turns and cycles come from interception observation and generic wrapping, and explicit composition replaces the slot. When observation needs to track turns or cycles, these interceptors serve as the observation boundary.
- Accepted consequences: turns and cycles are reported when they end, so a cycle clock counts completed cycles and a turn stopped mid-way is not reported. An interception call carries no cycle number, turn number or inner algorithm; numbering comes from count clocks on the two slots. A first turn that yields nothing in a run without an initial state has no state, so the turn interceptor is not called. `ObservationPoint`, `CycleTurn`, `CycleStart`, the point-based cycle clocks and their documentation are removed.

**Decided by the developer on 2026-11-04:** Cycle passes every state the inner algorithm yields straight through. When an interceptor returns a different object than it received, Cycle yields that object as one extra state. Holding back one state, so that the transformed state replaces the last inner state, was rejected because it delays every state by one inner step and loses work when an outer consumer stops.

**Reopened by the developer on the same day:** observing an operator requires a configuration reference, so interceptors as observation hooks would need non-null identity defaults throughout the library. The developer questioned whether interceptors should be observation hooks at all, or whether observation needs an explicit system of its own.

**Decided by the developer in the eighth round on 2026-11-04: observation attaches to executor calls, and Cycle gets no interceptor slots.** A turn is one call of an inner algorithm's executor, so wrapping the inner algorithms observes every turn, including its start, its states, its end, a turn that yields nothing and which algorithm ran. Start and end need the algorithm-call source accepted after the [design review](operator-state-lifetimes.md#design-review-on-2026-11-04), because `Iterations` reports only yielded states. This answers question 3: the explicit path wraps the inner algorithms in `Algorithms`, and the automatic path selects them. Turn and cycle numbers come from run-bound counters of those calls, with the cycle derived from the turn and the number of algorithms; a convenience source such as `cycle.Turns` may select the inner algorithms' calls. If an inner algorithm's reference is also used outside the Cycle, its calls there are selected too, and observing two positions of one reference separately needs distinct references.

- Interceptor slots stay nullable and only transform states, so no identity defaults are introduced.
- `TurnInterceptor` and `CycleInterceptor` are not added. Transforming the state between turns, with the yield rule above, waits in the [backlog](developer-backlog.md#scenarios-awaiting-a-concrete-need) for a concrete need.
- `ObservationPoint`, `CycleTurn`, `CycleStart`, the point-based cycle clocks and their documentation are removed. The developer rejected a separate event system, recorded in the [backlog](developer-backlog.md#observation-points-as-a-separate-observation-concept).

### Validation and documentation

The contract must be documented clearly and validated against the delivered system. Passing existing tests of aspect installation alone does not establish equivalence with explicit composition.

Future work must inventory the shipped aspect behaviors and show how each is available through ordinary composition. Important public flows need paired executable API usage specs. Detailed invariants belong in focused behavioral tests.

## Review findings in the current implementation

These findings describe the reviewed code on 2026-11-03. They identify work rather than select replacement APIs.

### Executor wrapping already uses ordinary composition

`Advice.Wrap` receives an executor and returns the executor callers will use. Operation advice uses `OperationInfo.Wrap`, while explicit compositions such as `Retry` use `TOperation.Wrap`. Both reach ordinary executor wrapping machinery. Consumer defined roles participate through `Advice<TConfiguration, TExecutor>`.

Preserve this common foundation. There is no demonstrated need for a separate invocation mechanism for aspects.

### Explicit access to behavior is incomplete

Counting, timing and observation are implemented as execution wrappers, but their existing implementations are not generally available through corresponding public configuration composition paths. A consumer can reproduce several behaviors by writing custom wrappers. That is weaker than delivering the same functionality through both paths.

The future design should reuse the execution behavior rather than maintain independent manual and automatic implementations. It need not expose every implementation type or introduce a public matrix over roles, concerns and run signatures. Developer guidelines § 8.11 still applies.

### Tracing couples collection to automatic attachment

`TraceAnalyzer<TResult>` inherits from `Aspect`. It constructs observation and clock advice internally, and its recording method is private. A manually configured observation composition cannot directly feed the same built in trace through the current public API.

Collection, retention, clocks and result publication must be assessed separately from automatic attachment. The accepted contract requires the delivered tracing functionality to have a usable explicit composition path. It does not select a collector interface, factory hierarchy or result lookup API.

### Internal observation points lack a manual insertion path

Cycle resolves its `Cycles` and `Turns` observation points internally. Advice can wrap their reporting executors. The published point properties are fixed and do not offer replaceable child configuration slots for manually composed reporting behavior.

An outer algorithm composition cannot reliably reconstruct every internal report from yielded states. A turn may yield nothing. Reimplementing Cycle to expose the event is not equivalent to composing the existing Cycle through its public API.

The design must provide an explicit composition path for this behavior or propose a deliberate change to the observation boundary. Making every child replaceable is not an accepted solution by implication.

### Advice identity currently affects data lifetime

Existing advice can retain counters, clocks and recordings across many constructed executors and across runs. An explicit wrapper with its own private counter would behave differently.

The lifetime design must establish the data owner independently of automatic placement. Distinguish the underlying executor, the wrappers applied for each requesting context and the runtime data those wrappers use. Constructing another wrapper must not accidentally reset data intended to survive, and reusing a declaration must not silently couple independent runs.

`AfterOperatorCountTerminator` and `AfterOperatorDurationTerminator` are representative integration cases. Counting or timing behavior updates runtime data that a separate terminator reads. Both paths must be able to establish the same intended lifetime for that data.

## Design work still open

Compare the smallest changes within the existing design with a coherent separation of reusable execution behavior, data ownership and automatic placement. Show consumer usage for both paths before selecting types.

Recommendations from the review, not additional accepted architecture:

- Reuse one execution behavior implementation from explicit composition and advice.
- Keep selection and installation in the aspect mechanism while making collection and publication usable independently.
- Use the accepted lifetime binding direction where it fits. Extract independently shared data into a separate configuration only for a demonstrated need.
- Prefer immutable reusable declarations when declarations become part of reusable configurations.
- Do not require an aspect executor hierarchy merely to mirror algorithms and operators. Any runtime aspect object needs a concrete responsibility.

Questions to resolve during that design:

1. Which public composition entry points expose counting, timing and observation without advice? Decided on 2026-11-04: `x.Counted(definition)`, `x.Timed(definition)` and `x.Observed(definition)`, placed where they are written; see [definitions](operator-state-lifetimes.md#advice-and-scope-layers-under-discussion-since-2026-11-04).
2. How can an explicit composition use the same trace collection, retention and clock functionality? Decided on 2026-11-04: both paths write into the run's one result for the definition.
3. How can consumers explicitly compose behavior at an internal observation point? Decided on 2026-11-04: there are no internal observation points; Cycle turns are calls of the inner algorithms, which are wrapped explicitly.
4. Where are shared counters, recordings and clocks created, and how do both paths obtain them? Decided on 2026-11-04: the run creates them, one per definition, and both paths look them up by definition. Counting and timing stay wrapper configurations, but write into a run-bound result instead of owning their data. See [lifetime plan, detail 4](operator-state-lifetimes.md#lifetime-binding-through-sharing-accepted-on-2026-11-03) and [run-bound results](operator-state-lifetimes.md#advice-and-scope-layers-under-discussion-since-2026-11-04).
5. What determines attachment scope, duplicate application and ordering for reusable aspect declarations? Decided on 2026-11-04: `Advised` attaches to the scope it is resolved in, the run creates one aspect per definition so duplicates apply once, and later added advice wraps closer.
6. How are results accessed after partial execution or failure without retaining discarded executor graphs solely for queries? Decided on 2026-11-04: `run.Result(definition)` reads the run's table, during and after the run and after a failure.

These questions are discussed together with whether advice keeps using resolution scope layers, recorded in the [lifetime plan](operator-state-lifetimes.md#advice-and-scope-layers-under-discussion-since-2026-11-04) since 2026-11-04.

The previously proposed `WithAspects` name and the precise configuration/runtime split remain open. Any wrapper returning a different configuration type must follow developer guidelines § 8.12, which reserves `With*` for changes returning the same type.

## Terminology

The developer requested consideration of a better term than wrapper. No replacement has been accepted. Keep the current glossary and public names until a replacement is selected.

Review recommendation: use composition for the overall model and composed algorithm or composed operator when describing the result. Keep wrapper for the precise component that delegates to another executor while controlling the behavior around that delegation. Composition alone is broader and also includes forms such as Cycle and Pipeline.

Alternatives considered for this naming discussion:

| Term         | Assessment                                                                                 |
| ------------ | ------------------------------------------------------------------------------------------ |
| Composition  | Best description of the overall model, but too broad to identify this specific component   |
| Decorator    | Precise, but the developer prefers to avoid naming the concept after a design pattern      |
| Layer        | Does not state delegation and overlaps the repository's architectural layering terminology |
| Augmentation | Suggests only added behavior, although compositions can replace, retry or suppress work    |
| Interceptor  | Already names an operator role in the glossary                                             |
| Wrapper      | Existing precise term and the fallback explicitly allowed by the developer                 |

These are naming assessments rather than rejected implementation approaches. A future terminology decision belongs in the glossary before migration of names or prose.

## Required evidence before completion

The following are acceptance scenarios to implement later. Their API spelling is deliberately unspecified.

| Scenario                               | Required evidence                                                                                                                          |
| -------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------ |
| Counting and timing                    | Manual and automatic composition produce equal measurements over equivalent boundaries, including the existing failed call semantics       |
| Ordering                               | Counting, observation and retry composed in a specified order produce equivalent callback sequences and results through both paths         |
| Tracing                                | Both paths use the same delivered collection functionality, retention behavior and clock semantics without a consumer reimplementation     |
| Internal progress                      | Both paths record the start and end of each inner algorithm call of a Cycle, including a turn that yields no state                         |
| Deferred construction                  | Both paths work when Cycle constructs fresh children on later turns                                                                        |
| Shared execution                       | Sharing the underlying executor preserves the specified independent requesting wrappers and the intended data lifetimes through both paths |
| Separate termination                   | A counting or timing composition and a separate terminator use the same runtime data through both paths, independent of construction order |
| Independent runs                       | Reusing one configuration, including best-so-far traces and retention, produces independent results per run through both paths             |
| Shared clocks and duplicate attachment | A clock shared by two traces ticks once and before both observations, in either attachment order and scope, following the ordering rule    |
| Consumer extensions                    | A consumer defined operation or role can use both paths without changing library machinery                                                 |

Use `HeuristicLib.Tests.ApiUsageSpecs` for representative public flows, `HeuristicLib.Tests` for focused invariants and `HeuristicLib.Tests.Scenarios` for broader composed workflows, following the [test suite guide](../test/README.md). Publish representative examples from executable sources in `HeuristicLib.Tests.Docs`.

## Future documentation and completion checklist

- [ ] Inventory shipped aspect behavior and its explicit composition path.
- [ ] Present and obtain acceptance of the concrete design and representative APIs.
- [ ] Implement paired usage specs and relevant behavioral scenarios.
- [ ] Close the tracing and internal observation point gaps.
- [ ] Verify state lifetimes together with lifetime binding.
- [ ] Document the durable contract in developer guidelines § 3.2 and the responsibilities of aspects in execution architecture guidance.
- [ ] Update analyzer architecture and user guidance when the implementation supports the new separation.
- [ ] Update glossary entries to describe delivered responsibilities and any accepted terminology change.
- [ ] Run the applicable repository validation from `AGENTS.md` and record results.

Published architecture and user documentation must continue to describe current behavior until the implementation is changed. This plan records the accepted target and the remaining work.
