# Termination Overhaul

## Motivation

HeuristicLib currently has several pieces of termination machinery, but the design vocabulary does not clearly distinguish two different concerns:

1. an algorithm deciding that its own run is complete, and
2. an outside consumer deciding to stop asking an algorithm for more states.

This plan establishes the terminology and semantic rules that should guide the next API and documentation changes.

## Implementation Progress

- `StateTerminatedAlgorithm` no longer checks a supplied `initialState`; external state-based termination now observes only states produced by the current execution.
- `IterativeAlgorithm` now separates pre-step internal completion (`HasCompleted(...)`) from produced-state termination (`IsTerminalState(...)`) and structural exhaustion (`TryExecuteStep(...)` returning `false`).
- `GeneticAlgorithm` has algorithm-owned `MaximumGenerations` and optional internal `Terminator` support, with stop-if-any semantics.
- `EvolutionStrategy`, `NSGA2`, `AlpsGeneticAlgorithm`, and `OpenEndedRelevantAllelesPreservingGeneticAlgorithm` now also expose algorithm-owned `MaximumGenerations` budgets.
- `HillClimber` structurally completes when no strictly improving neighbor exists, instead of yielding an unchanged previous state.
- `CycleAlgorithm` has a reference spec for inner GA budgets plus external early stopping over the composed stream, including partial-cycle behavior.
- `OperatorBudgetAlgorithm` can apply external early stopping over observed operator usage by installing a counted runtime replacement through the execution-instance registry. `WithMaxEvaluatorCalls(...)` observes `Evaluate(...)` calls on the algorithm's configured evaluator, `WithMaxEvaluatedGenotypes(...)` observes genotypes processed inside those evaluator batches, and `WithMaxCount(...)` covers other explicit operator-budget cases with a counted replacement factory.
- API usage specs now distinguish ordinary algorithm-owned completion from external early stopping; examples keep `WithMaxIterations(...)` only where the caller is intentionally applying an outside cap.
- Documentation has been updated in `docs/execution-model.md` and `docs/operators.md` for internal completion, external early stopping, initial-state resume semantics, state-based terminator timing, and structural completion.

## Iterative Algorithm Audit

Current `IterativeAlgorithm` subclasses fall into these buckets:

- `GeneticAlgorithm`: done for this pass. It has `MaximumGenerations`, optional internal `Terminator`, and tests for fresh runs, resume, invalid budgets, and stop-if-any composition.
- `EvolutionStrategy`: done for the ordinary generation-budget pass. It has `MaximumGenerations`; no custom internal terminator hook was added in this slice.
- `NSGA2`: done for the ordinary generation-budget pass. It has `MaximumGenerations`; no custom internal terminator hook was added in this slice.
- `AlpsGeneticAlgorithm`: done for the ordinary generation-budget pass. It has `MaximumGenerations`, but the real ALPS age-layer mechanics remain separate backlog work.
- `HillClimber`: done for this pass. It structurally completes when no strictly improving neighbor exists.
- `OpenEndedRelevantAllelesPreservingGeneticAlgorithm`: done for the ordinary generation-budget pass. It has `MaximumGenerations` as a normal execution budget; its open-ended search logic still has no structural completion condition.
- `DynamicRacingAlgorithm`: leave for a later experimental-workflow pass. It is an orchestration/racing algorithm around inner executions and dynamic epochs, so the right completion unit may be epoch, race, evaluation budget, or an external workflow cap rather than a plain generation count.
- `ParameterlessPopulationPyramid`: ignored for this pass because it is currently commented-out code.
- API-spec-only toy algorithms in `AlgorithmAuthoringSpecs`: leave wrapper-capped. They exist to document authoring mechanics, not to model production completion semantics.

Future refactoring may introduce a shared evolutionary-algorithm base class or helper layer. If that happens, `MaximumGenerations` is a strong candidate for that shared responsibility because it now has the same meaning across generation-producing evolutionary algorithms.

## Core Terminology

### Internal termination

Internal termination is an algorithm's own configured or structural completion condition.

Examples include:

- a genetic algorithm generation budget, such as `MaximumGenerations = 200`
- a local-search completion rule, such as no improving neighbor exists
- an exhausted queue, frontier, layer schedule, or worklist
- an algorithm-owned stagnation rule
- a custom `ITerminator` that describes when this configured algorithm considers itself complete

Internal termination is part of the reusable algorithm configuration or the algorithm's honest execution semantics.

### State-based terminator

A state-based terminator decides based on a produced `TSearchState`.

The produced state that satisfies a state-based terminator is part of the run result. The terminator stops future execution, not publication of the state that triggered it.

### Structural completion

Structural completion means the algorithm has no meaningful next state to produce.

Examples include an empty queue, an exhausted frontier, an exhausted layer schedule, an empty worklist, or a local search with no possible improving move.

A structurally complete algorithm is represented by an exhausted stream, not by a synthetic final state.

### External termination

External termination is an outside consumer, wrapper, experiment, service, UI, or meta-algorithm stopping consumption early.

Examples include:

- a shared experiment cutoff applied uniformly to many different algorithm configurations
- an interactive UI, notebook, or service that stops consuming progress once it has enough
- cancellation, graceful stop, pause, wall-clock, or request-timeout policies
- a meta-optimizer that cuts off poor candidate configurations early
- an orchestration layer that switches strategy once an observed target quality is reached
- a one-off run policy that should not become part of the reusable algorithm configuration
- a cap over a composed workflow, such as stopping a whole cycle or pipeline early

External termination belongs to the caller or broader workflow, not to the wrapped algorithm's own definition of completion.

## Semantic Rules

Algorithm execution continues only while both of these are true:

1. the algorithm has not internally completed, and
2. the external consumer still wants another state.

Internal termination is authoritative. External termination can shorten a run, but it cannot extend a run past internal completion.

That means an external wrapper such as `StateTerminatedAlgorithm` is an early-stopping tool. It is not a way to force an algorithm beyond its own completion condition. If a caller wants an algorithm to run longer, it should configure the algorithm's internal termination differently or explicitly start a new run, restart, cycle, or other meta-algorithm behavior.

## State Production And Check Timing

State-based terminators evaluate produced public search states and stop future execution only after the terminal state has been yielded or observed.

The normal state-based loop is:

```text
produce raw next state
apply interceptor, if configured
yield/observe public next state
check state-based terminators against public next state
stop before producing another state if needed
```

State-based terminators operate on the public produced state. If an interceptor transforms a state before it is yielded, termination checks must use the intercepted state, not the raw pre-interceptor state.

Structural completion is different. If an algorithm cannot produce a meaningful next state, the stream ends without yielding another state. If a caller iterates an already structurally complete execution instance, the loop body should not run because the stream has no more states.

## Initial State And Resume Semantics

A supplied `initialState` is resume input from outside the algorithm. It is not a newly produced state.

Therefore, a supplied `initialState` should not be yielded automatically and state-based terminators should not be invoked on it by default. This avoids accidentally advancing stateful terminators when no new state has been produced by the current execution.

A generated initial state is different. When an algorithm starts without a supplied `initialState`, the first state it creates is a normal produced state. It is yielded and may satisfy state-based termination immediately.

For example, a genetic algorithm with no supplied `initialState` may produce generation 0, yield generation 0, and then stop before generation 1 if a state-based terminator is satisfied. A resumed genetic algorithm should use the supplied state as the previous state, then yield and check only the next newly produced generation.

## Stateful Terminator Check Contract

`ITerminatorInstance.IsTerminalState(...)` may mutate run-local state. An execution instance must treat it as an effectful transition, not as an idempotent predicate.

The owning execution instance should call a terminator instance at most once for each produced public state, and never for speculative probing.

This also means a terminator instance should not be shared between an algorithm-owned internal criterion and a wrapper-owned external criterion unless shared state is intentional and documented.

## Algorithm Configuration Guidance

Common expected budgets should be simple algorithm properties. For example, a genetic algorithm should be able to expose a property such as `MaximumGenerations` instead of requiring users to configure a specific maximum-generation terminator object for the ordinary case.

Budget names must identify their unit. A generation, iteration, step, cycle, evaluation, and yielded state are distinct budget units.

Algorithms should prefer domain-specific budget names such as `MaximumGenerations` or `MaximumCycles`. A generic name such as `MaximumIterations` is acceptable only when the algorithm defines iteration as its honest public progress unit.

A budget property counts exactly the unit named by the property. Whether that unit maps one-to-one to yielded states is part of the algorithm's documented stream contract. For example, the current genetic algorithm design may define one generation as producing one public `PopulationState`, but that is a genetic algorithm contract, not a library-wide rule.

External early-stopping wrappers that count stream output should use yielded-state language rather than algorithm-internal iteration language.

Evaluation-count budgets should be expressed through explicit observation or counter-based terminators at the chosen operator boundary. Counting an outer caching evaluator and counting the inner evaluator are different budgets. This makes evaluation budgets a poor first-pass generic algorithm property unless the API explicitly defines the counted boundary.

Algorithms may also expose an optional algorithm-owned `Terminator` hook for custom internal completion rules. This hook is for stop rules that belong to the configured algorithm itself.

Common budget properties and optional internal `Terminator` hooks are additive. The algorithm stops when any internal completion criterion says stop.

For example:

```text
InternalStop =
  MaximumGenerationsReached
  OR EvaluationBudgetReached
  OR CustomTerminator.IsTerminalState(...)
  OR StructuralAlgorithmCompletion
```

Setting a custom internal `Terminator` must not override or disable configured budget properties unless a future API says so explicitly. The default behavior should be stop-if-any.

Implementation does not need to mirror public configuration one-to-one. A simple property such as `MaximumGenerations` may be implemented internally by composing a corresponding terminator-like execution-instance check. The public configuration should express what the user means; the execution instance may choose the cleanest mechanism.

## Completion, Early Stopping, And Continuation

Use completion language for internal algorithm-owned done-ness and early-stopping language for external caller-owned decisions.

- **Completed** means the execution instance has internally terminated or structurally completed.
- **Early-stopped** means an outside owner stopped consuming while the inner execution instance may still be capable of producing more states.
- **Continuation** is only meaningful for an execution instance that was early-stopped, not completed.
- **Cancellation** is an external execution interruption, not internal completion or normal external early stopping.

A completed execution instance is exhausted and must not be continued. Calling its stream again, if the API permits it, should produce no further states.

An early-stopped execution may be continuable only if the API explicitly preserves the execution instance and resumes consuming it. Calling `Run...` again should not be assumed to continue the same execution instance unless the API says so directly.

Cancellation is not an internal completion condition and should not be reported as algorithm completion. Cancellation may prevent more states from being produced, but it should remain distinguishable from both internal completion and normal external early stopping.

In nested execution, completion is scoped to the owner. An inner algorithm can complete without completing the meta-algorithm that owns it. A wrapper can early-stop a composed stream without changing whether the inner or outer algorithms consider themselves complete.

## Choosing Internal Or External Termination

Prefer internal termination for ordinary algorithm setup because it preserves the actual algorithm type and avoids unnecessary nesting.

Use internal termination when the stop rule belongs to the reusable algorithm configuration:

- `MaximumGenerations` on a genetic algorithm
- an algorithm-owned stagnation condition
- a custom algorithm-specific `ITerminator`
- a structural completion condition such as an empty queue or no improving move

Use external wrapping termination when the stop rule belongs to outside ownership or cross-cutting orchestration:

- applying one experiment cutoff to many algorithm configurations
- stopping a dashboard or notebook stream once enough progress was shown
- enforcing service-level cancellation, graceful stop, pause, wall-clock, or request-time policies
- cutting off weak configurations during meta-optimization
- stopping a larger composed workflow early without changing the reusable inner algorithm configurations

The practical rule is:

- if the algorithm should still be described as "configured to stop this way", use internal termination
- if the outside workflow is merely deciding how long to observe or consume the algorithm, use external termination

## Reference Scenarios

### Genetic algorithm budget

A genetic algorithm with `MaximumGenerations = 200` internally completes after 200 generations.

An external wrapper may stop it after 50 generations. The wrapper may not force it to continue to 500 generations.

If a genetic algorithm has `MaximumGenerations = 200` and `Terminator = new StagnationTerminator(...)`, it stops when either the generation budget or stagnation terminator fires.

### Structural completion

A local search with no improving move, an exhausted queue or frontier, or an empty worklist is internally complete.

External termination cannot meaningfully extend that algorithm. Continuing after completion must be modeled as a new run, a restart, a cycle, or another explicit meta-algorithm.

### Cycle with inner budgets and external early stop

A `CycleAlgorithm` may run an inner algorithm with `MaximumGenerations = 100` for `MaximumCycles = 5`, yielding up to 500 inner generation states.

If the cycle re-instantiates the inner algorithm each cycle, each cycle gets a fresh independent 100-generation internal budget.

Wrapping that cycle with an external cap of 250 yielded states should produce two full cycles plus 50 generation states of the third cycle.

This scenario should become a reference spec because it exercises all key semantics:

- inner algorithm internal termination
- meta-algorithm internal termination
- fresh inner execution-instance state per cycle
- external early stopping over the composed stream

### Evaluation-count budget boundary

Evaluation-count budgets should make the observed boundary explicit.

For example, with a `CachingEvaluator` wrapping a `DirectEvaluator`, counting calls to the outer caching evaluator answers "how many evaluation requests did the algorithm make?", while counting calls to the inner direct evaluator answers "how many uncached evaluations happened?"

Both budgets are valid, but they are not the same budget. Users should be able to choose the operator boundary they observe and pass that counter into a counter-based terminator.

## Stateful Terminator Sharing

Terminator definitions may be reusable configuration objects, but terminator instances are stateful execution objects.

Stateful terminator state lives on the resolved execution instance. Reusing the same terminator configuration object within one execution-instance registry may share terminator state according to normal registry identity rules.

Users who need independent counters should use distinct terminator configuration objects or distinct execution-instance scopes.

## Naming Follow-Up

`ITerminatorInstance.IsTerminalState(...)` is authoritative. A `true` result means the owning lifecycle must stop according to that owner's semantics:

- internal completion for algorithm-owned terminators
- external early stopping for wrapper-owned terminators

This replaced the older `ShouldTerminate(...)` wording because the decision is not advisory.

The same terminology pressure applies to run and execution method names. Names such as `Run`, `Execute`, `Resume`, and `Continue` should be revisited when continuation becomes a first-class API question, especially to avoid implying that a completed execution instance can be extended.

## Naming Decision Sketch

The current implementation keeps the `ITerminator` / `ITerminatorInstance` type names while using `IsTerminalState(...)` for the state-based check.

Current leaning:

- Prefer `IsTerminalState(...)` over the old `ShouldTerminate(...)` wording for state-based terminator checks. A terminator result is authoritative for its owning lifecycle, so "should" was too advisory.
- Keep `ITerminator` / `ITerminatorInstance` as type names for now. They are recognizable domain names, and there is no clearly better type name yet. The misleading part is the advisory method name, not necessarily the role name.
- Keep completion wording consistent for internally completed executions. This may eventually put pressure on names such as `RunToCompletion(...)`, but run-method naming should wait until continuation semantics are clearer.
- Use completion wording for pre-step algorithm-owned done-ness (`HasCompleted(...)`) and terminal-state wording for post-state checks (`IsTerminalState(...)`).
- Keep `RunToCompletion(...)`, `Run(...)`, and `RunStreaming(...)` naming out of this branch unless a small local rename becomes unavoidable. The run-method naming scheme is a broader API design topic and should get its own plan or branch.
- Keep run results as search states in this branch. A distinct completion-result object is a larger result-model decision and is tracked separately in the developer backlog.

Current decisions:

- The state-based check method has been renamed from `ShouldTerminate(...)` to `IsTerminalState(...)`.
- `ITerminator` / `ITerminatorInstance` remain the type names unless a future design finds a materially clearer replacement.
- Internal algorithm-owned terminal-state checks and external wrapper-owned terminal-state checks can share the same interface when both inspect produced states. The important distinction is ownership, not necessarily mechanism.
- `StateTerminatedAlgorithm` remains useful as an adapter and composition tool for applying state-based early stopping around algorithms or workflows without changing their reusable configuration.
- `CancellationToken` is the run-level cancellation mechanism. Cancellation is not internal completion, not a terminal search state, and not normal external early stopping.
- `CancellationTokenTerminator(...)` is the graceful external-stop counterpart to run-level cancellation. A run-level `CancellationToken` may interrupt the current iteration wherever the algorithm or an operator checks it. A cancellation-token terminator checks only after a produced state has been yielded or observed, so the current iteration completes and only future consumption stops.
- `AfterElapsedTimeTerminator(...)` is the graceful external-stop mechanism for elapsed runtime budgets. It uses `TimeProvider` for testable time access and measures elapsed time from terminator execution-instance creation. `StateTerminatedAlgorithm` resolves terminators before wrapped algorithms so this starts as early as that wrapper can control, including wrapped algorithm instancing. If a caller needs an exact call-site start time, it can create a timeout `CancellationTokenSource` immediately before calling `Run...(...)` and use `CancellationTokenTerminator(...)`.
- Determinism is expected only when algorithm configuration does not depend on outside mutable state. A `CancellationTokenTerminator(...)` captures a live external token, so reusing the same terminator after cancellation will stop later executions immediately after their first produced state. `AfterElapsedTimeTerminator(...)` and future operator-duration timing have the same outside-state caveat.
- `PauseTokenTerminator` should not be restored as a state-based terminator. A pause/stop signal from outside execution is execution control, not a fact about a produced search state.

Remaining naming and lifecycle questions:

- Revisit run-method names later with a clear story for completed versus early-stopped execution instances.
- If a future completion-result API reports stop reasons, decide method names and result shape together so lifecycle vocabulary aligns.
- Add operator-duration timing as a future slice. It is a different budget unit from general elapsed run time and needs its own naming and observation design.

External termination should be understood primarily as ownership. It means something outside the algorithm decides, using whatever policy it owns, whether to continue drawing states from the stream. In many simple runs this can produce the same visible sequence as an internal budget, much like LINQ `Take(n)` can expose the same first `n` items as a naturally finite source, but the ownership distinction matters for reusable configuration, composition, continuation, and future lifecycle metadata.

## Evaluation And Operator Budget Decision

Evaluation-count and operator-budget limits are common in optimization experiments, but they are trickier than generation or cycle budgets because the observed boundary matters.

Options to compare:

- Add algorithm properties such as `MaximumEvaluations` where users naturally expect them.
- Offer evaluation budgets through terminators or wrapper algorithms that observe an explicit evaluator boundary.
- Model evaluation budgets through evaluator or general-operator decorators such as counting or limiting wrappers, then create a max-operator-count terminator for that counted operator. The eventual API should probably hide the ceremony behind a small helper that creates the counter and terminator together.
- Introduce a more general internal termination configuration object instead of pre-loading every algorithm with separate properties such as `MaximumGenerations`, `MaximumEvaluations`, `MaximumRuntime`, `MaximumStagnation`, and so on.

Current decision:

- Keep only the most essential algorithm-native budgets as dedicated properties. `MaximumGenerations` qualifies because it is a common, obvious unit for generation-producing evolutionary algorithms.
- Do not add `MaximumEvaluations` as a routine algorithm property in this branch.
- Treat operator-budget termination as external early stopping by default. If an evaluator, selector, mutator, or other operator reaches a call-count, time, or resource budget, the inner algorithm is not necessarily complete; the surrounding execution policy stopped consuming it.
- Keep a clear manual model for advanced users: wrap the operator whose boundary should be observed, configure the algorithm to use that wrapper, and attach a terminator or external stop policy to the wrapper's runtime count.
- Keep the lower-level counter-plus-terminator style for advanced cases, especially when one budget is shared across several observed operators. `AfterOperatorCountTerminator(...)` remains useful there because it terminates from a shared counter without owning how that counter is incremented.
- Do not require ordinary users to manually wire the counted wrapper and the matching terminator as separate objects for common evaluator cases. Helpers such as `WithMaxEvaluatorCalls(...)` and `WithMaxEvaluatedGenotypes(...)` should perform the correct replacement and attach the corresponding external early-stopping policy. Broader operator budgets should use the general `WithMaxCount(...)` factory shape unless a helper clearly earns its place.
- Keep `WithMaxCount(...)` as the general operator-budget helper. Evaluator budgets are common enough to keep `WithMaxEvaluatorCalls(...)` and `WithMaxEvaluatedGenotypes(...)` as convenience helpers, but other operator families should use `WithMaxCount(...)` unless repeated real usage shows that a specific shortcut earns its place.
- Prefer ordinary terminators, operator counters, wrappers, and helper APIs for less universal budgets such as maximum evaluations, maximum operator calls, stagnation, target quality, or runtime.
- Avoid the word "invocation" for public counter APIs because it hides the counted unit. Use `Calls` for method calls at one observed operator boundary and domain-specific item names such as `EvaluatedGenotypes` for batched elements processed inside those calls. HeuristicLib currently uses `Genotype` as the generic solution-candidate term; if the library later renames that concept, these APIs should be revisited together.
- Start the counter naming cleanup with evaluators: `CountEvaluatorCalls(...)` counts `Evaluate(...)` method calls, while `CountEvaluatedGenotypes(...)` counts genotypes processed inside evaluator batches. Avoid also adding a generic `CountCalls(...)` alias for evaluators because it creates two public names for the same counter.

Open operator-budget API questions:

- What exactly counts as an evaluation: requests to the algorithm's configured evaluator, evaluated genotypes inside batched evaluator calls, cache misses in a wrapped evaluator, objective-function calls, or something else?
- If an evaluator batch would exceed the budget, should the system reject the batch, partially evaluate it, finish the batch and stop afterward, or require algorithms to request budget before producing offspring?
- How should helper APIs discover or replace the relevant operator on an algorithm configuration without requiring every algorithm to expose the same evaluator property shape?
- Would a future internal termination configuration object give a better long-term API than accumulating separate nullable budget properties?

Nullable budget properties versus a single internal termination configuration:

- Many nullable properties are very discoverable for common cases and read well in object initializers, but they can bloat algorithm APIs, create unclear precedence rules, and encourage every algorithm to accumulate a different menu of stop criteria.
- A single internal termination configuration scales better for many criteria and can compose policies uniformly, but it may be less discoverable, more verbose for the common case, and may hide important domain units behind a generic abstraction.
- A hybrid may be the best long-term shape: keep first-class properties only for the few domain-native budgets users expect on the algorithm itself, and use explicit terminator/configuration objects for richer or less universal criteria.

## Expected Follow-Up Work

- [x] Update `docs/execution-model.md` with the terminology, precedence rule, and internal/external usage guidance.
- [x] Update `docs/operators.md` to explain that `ITerminator` can be owned internally by an algorithm or externally by a wrapper.
- [x] Document state-based terminator check timing, structural completion, supplied versus generated initial states, and the stateful terminator invocation contract.
- [x] Add desired-state API specs for genetic algorithm internal termination:
  - simple budget property
  - optional internal terminator
  - budget and terminator composed with stop-if-any semantics
- [x] Use `GeneticAlgorithm` as the first implementation slice after the terminology is documented.
- [x] Add a reference spec for cycle plus external early stopping. The implemented spec uses a small 3 x 5 capped-at-8 scenario rather than the illustrative 100 x 5 capped-at-250 numbers.
- [x] Add structural-completion behavior and specs for local search.
- [x] Add regular generation budgets to the other evolutionary algorithms where the unit is intuitive: ES, NSGA2, ALPS GA, and OERAPGA.
- [x] Audit remaining `IterativeAlgorithm` subclasses for ordinary internal budgets, structural completion, or wrapper-only semantics.
- [x] Document budget-unit naming guidance and make evaluation-count budget boundaries explicit in examples.
- [x] Rename state-based terminator checks from `ShouldTerminate(...)` to `IsTerminalState(...)`.
- [ ] Decide the broader naming scheme for terminal states, completion, early stopping, and future run/resume/continue APIs.
- [x] Decide whether evaluation-based budgets belong on algorithms, terminators, wrappers, evaluator decorators, or a more general internal termination configuration.
- [x] Add the first operator-budget wrapper and evaluator helpers so users do not manually wire matching counted evaluator wrappers and terminators.
- [x] Keep and cover the lower-level `AfterOperatorCountTerminator(...)` style for advanced shared-counter budgets across multiple observed operators.
- [x] Start counter naming cleanup with evaluator counters: `CountEvaluatorCalls(...)` and `CountEvaluatedGenotypes(...)`.
- [x] Rename creator, mutator, and crossover call counters to `CountCreatorCalls(...)`, `CountMutatorCalls(...)`, and `CountCrossoverCalls(...)`.
- [x] Rename selector, replacer, interceptor, and terminator call counters to `CountSelectorCalls(...)`, `CountReplacerCalls(...)`, `CountInterceptorCalls(...)`, and `CountTerminatorCalls(...)`.
- [x] Add selector item counter `CountSelectedSolutions(...)`.
- [x] Add item-count helpers for other batched operator outputs, including created genotypes, crossed genotypes, mutated genotypes, evaluated genotypes, selected solutions, and replacement solutions.
- [x] Keep broader operator-budget use cases on the generic `WithMaxCount(...)` factory shape instead of adding selector-specific helper methods.
- [x] Decide whether and how to generalize operator-budget helpers beyond evaluator calls: keep `WithMaxCount(...)` as the general factory shape, with evaluator-specific helpers as common-case conveniences.
- [x] Add and document `CancellationTokenTerminator(...)` for graceful external stop after the current produced state, distinct from immediate run-level `CancellationToken` cancellation.
- [x] Add wall-clock/runtime termination with clear time-provider and post-state versus immediate-interruption semantics.
- [ ] Add operator-duration timing as a separate future design, including naming and observed-boundary semantics.
- [ ] Consider whether a future completion-result API should expose a typed stop reason. Ending a stream can mean internal completion, external early stopping, cancellation, or failure, but this plan does not require that API.
- [ ] Before deleting this overhaul plan, move every durable design decision and important explanation from this file into the appropriate long-lived documentation file so the architectural rationale is not lost.

## Assumptions

- Internal termination always wins over external continuation.
- External termination can only stop earlier than internal completion.
- Restarting after completion must be modeled explicitly as a new run, restart, cycle, or other meta-algorithm behavior.
- Completion is scoped to the execution owner in nested workflows.
- State-based terminators check produced public states after interception and after yield/observation.
- Supplied `initialState` values are resume inputs, not produced states.
- Generated initial states are produced states and are eligible for state-based termination checks.
- Terminator checks may be effectful and must not be used for speculative probing.
- Cancellation is interruption, not algorithm completion.
- Evaluation-count budgets depend on the observed operator boundary.
- The first artifact is this design plan, not an implementation patch.
