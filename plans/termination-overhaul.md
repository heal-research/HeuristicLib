# Termination Overhaul

## Motivation

HeuristicLib currently has several pieces of termination machinery, but the design vocabulary does not clearly distinguish two different concerns:

1. an algorithm deciding that its own run is complete, and
2. an outside consumer deciding to stop asking an algorithm for more states.

This plan establishes the terminology and semantic rules that should guide the next API and documentation changes.

## Implementation Progress

- `TerminatableAlgorithm` no longer checks a supplied `initialState`; external state-based termination now observes only states produced by the current execution.
- `IterativeAlgorithm` now separates pre-step internal completion (`HasCompleted(...)`) from produced-state termination (`IsTerminalState(...)`) and structural exhaustion (`TryExecuteStep(...)` returning `false`).
- `GeneticAlgorithm` has algorithm-owned `MaximumGenerations` and optional internal `Terminator` support, with stop-if-any semantics.
- `EvolutionStrategy`, `NSGA2`, `AlpsGeneticAlgorithm`, and `OpenEndedRelevantAllelesPreservingGeneticAlgorithm` now also expose algorithm-owned `MaximumGenerations` budgets.
- `HillClimber` structurally completes when no strictly improving neighbor exists, instead of yielding an unchanged previous state.
- `CycleAlgorithm` has a reference spec for inner GA budgets plus external early stopping over the composed stream, including partial-cycle behavior.
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
- cancellation, pause, wall-clock, or request-timeout policies
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

That means an external wrapper such as `TerminatableAlgorithm` is an early-stopping tool. It is not a way to force an algorithm beyond its own completion condition. If a caller wants an algorithm to run longer, it should configure the algorithm's internal termination differently or explicitly start a new run, restart, cycle, or other meta-algorithm behavior.

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

## Stateful Terminator Invocation Contract

`ITerminatorInstance.ShouldTerminate(...)` may mutate run-local state. An execution instance must treat it as an effectful transition, not as an idempotent predicate.

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
  OR CustomTerminator.ShouldTerminate(...)
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
- enforcing service-level cancellation, pause, wall-clock, or request-time policies
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

A `CycleAlgorithm` may run an inner algorithm with `MaximumGenerations = 100` for `MaximumCycles = 5`, yielding up to 500 inner iterations.

If the cycle re-instantiates the inner algorithm each cycle, each cycle gets a fresh independent 100-generation internal budget.

Wrapping that cycle with an external 250-iteration early stop should produce two full cycles plus 50 iterations of the third cycle.

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

`ITerminatorInstance.ShouldTerminate(...)` is legacy wording and should not be treated as advisory. A `true` result means the owning lifecycle must stop according to that owner's semantics:

- internal completion for algorithm-owned terminators
- external early stopping for wrapper-owned terminators

Future API naming should consider whether this method should be renamed or split so the name communicates an authoritative transition rather than a suggestion.

The same terminology pressure applies to run and execution method names. Names such as `Run`, `Execute`, `Resume`, and `Continue` should be revisited when continuation becomes a first-class API question, especially to avoid implying that a completed execution instance can be extended.

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
- [ ] Document budget-unit naming guidance and make evaluation-count budget boundaries explicit in examples.
- [ ] Revisit `ShouldTerminate`, `Run`, `Execute`, `Resume`, and `Continue` naming as follow-up API design work.
- [ ] Consider whether a future completion-result API should expose a typed stop reason. Ending a stream can mean internal completion, external early stopping, cancellation, or failure, but this plan does not require that API.

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
