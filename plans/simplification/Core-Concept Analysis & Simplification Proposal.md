# HeuristicLib — Core-Concept Analysis & Simplification Proposal

## 1. Diagnosis: where the complexity actually lives

**Concept inflation is concentrated in the execution model and analysis, not the domain.** The glossary has ~89 entries. Breakdown:

| Area | Entries | Verdict |
|---|---|---|
| Optimization domain (problem, candidate, objective, …) | ~14 | Mostly fine — this is the domain, it *should* have words |
| ML / data | ~12 | Fine, sklearn-like already |
| Algorithms & operators | ~14 | Mostly fine (roles are domain language), but "single-candidate operator", "operation invocation", "item" smell |
| **Execution model** | **~27** | The problem: pointcut, advice, aspect, join point, named configuration, resolution scope, shared configuration, wrapper chain, invocation wrapper, resource, resource handle, run resources, execution concurrency, retry, executor controls… |
| **Analysis & experiments** | **~18** | The second problem: analyzer, snapshot, trace, accumulator, trace retention, clock, time, moment, inspection, observation, observation source, observation point, experiment, trial, trial analyzer |

~620 public type declarations in core; the dominant generic tuple is `TOperation × TCandidate × TSearchSpace × TProblem × TSearchState × TExecutor`. The sub-agent inventory concludes a user must understand **five to six interacting systems** to predict what one configuration does when executed: configuration composition, resolution/lifetime, resources/sharing, advice/wrapper ordering, RNG/concurrency/lifecycle, and (if installed) observation. That is the exact opposite of the stated north star ("a small number of strong concepts").

**Root cause pattern:** every corner case got its own *named mechanism* instead of falling out of one rule. Examples:

- Want to stop after N calls of a shared mutator → invented `ResourceHandle<T>`, `SuppliedResourceHandle`, flat run resource table, lazy serialized default creation, handle reference identity participating in record equality (spec S1/S2).
- Want to observe calls → invented pointcut/advice/aspect/join point (AspectJ vocabulary), *plus* observation sources, *plus* observation points, *plus* predefined inspection aspects.
- Want to wrap any operation generically → invented `IOperation<TSelf>` static-abstract machinery, `OperationInfo`, a private `Invocation` struct per operation, `WrappingExecutor<TExecutor,TWrapper>`, `IInvocation`/`Proceed`, `SupportsCandidateWork`/`CountCandidateWork`/`RequireCandidateWork`.
- Want to select things by name → `INamedConfiguration` + `Named`/`NamedAlgorithm` wrapper records + `Pointcut.Named` (a wrapper whose only job is carrying a string).
- Want pause/cancel through wrappers → `IExecutorControlProvider` + `ExecutorControls.Find` forwarding rules.

Each mechanism is defensible in isolation. Together they violate KISS and the "one honest concept over parallel hierarchies" principle the repo already wrote down.

## 2. The actual core — what HeuristicLib fundamentally *is*

Stripping everything to what the library cannot exist without, I get **two graphs and one bridge**, plus a tiny domain vocabulary:

**Domain (keep, ~8 concepts):**
1. **Problem** — owns the search space and scoring (objective vectors, directions).
2. **Candidate / evaluated candidate** — immutable values.
3. **Search space** — candidate membership/invariants.
4. **Objective (vector, direction)** — the currency of evaluation.
5. **Operator role** — one role = one method shape (creator, evaluator, mutator, crossover, selector, replacer, refiner, terminator). Roles are *domain language*, keep them.
6. **Algorithm** — the thing that yields **search states** (7).
8. **RNG** — explicit randomness. Non-negotiable for reproducibility.

**The two graphs (the user's own beloved separation — keep, it's the best idea in the library):**
9. **Configuration** — immutable, data-like record; serializable; equality by value; children by reference.
10. **Executor** — plain .NET object tree built once per run; hot path with no lookups, no reflection, mutable state in private fields.
11. **Run** — one execution of a configuration on a problem with an RNG.

**The bridge (the *single* internal system that is truly core):**
12. **Resolution** — walk the configuration graph, call each node's factory, produce the executor tree. That's it. One rule: *fresh executor per construction; a scope may cache per configuration reference to implement sharing.*

Everything else currently in the glossary is either (a) a convenience that should sit *on top* of these twelve, or (b) a mechanism that should be folded into one of them.

**The sklearn mapping is almost embarrassingly direct:**

| sklearn | HeuristicLib core |
|---|---|
| `Estimator` (params in `__init__`, clonable, serializable) | Configuration record |
| `fit()` | `CreateExecutor` / `CreateRun` |
| `predict` / `transform` | role method on the executor |
| `Pipeline`, `FeatureUnion` (meta-estimators) | composition operators (`Pipeline`, `Cycle`) |
| `GridSearchCV` (meta-estimator wrapping an estimator) | `Experiment` = meta-configuration producing runs |

sklearn stays explainable because composition works through **the same contract at every level** — a Pipeline *is* an Estimator. HeuristicLib already half-knows this: `Cycle`, `Sharing`, `LimitedToInvocations` are configurations. The leak is that *algorithms* and *operators* are parallel hierarchies with parallel advice, parallel resolution paths, parallel naming wrappers.

**Proposed unification: an algorithm is an operator whose role method streams search states.** One configuration contract (`IConfigurationNode<TExecutor>`), one resolution path, one decoration mechanism. `IAlgorithm<TCandidate,TSearchState>` becomes a role interface, not a separate species. This alone deletes the dual advice hierarchies (`AlgorithmAdvice` vs `OperatorAdvice` vs `Advice<TConfiguration,TExecutor>`), dual named wrappers, and half of `ResolutionScope.*.cs`.

## 3. Internal systems: keep / simplify / throw out

### Keep as core (necessary, already simple)
- **Configuration/executor split** with private state in executor fields — this is right and matches your REST-serialization goal. Guard it: *no runtime object references (handles with identity semantics, advice instances) inside configurations*, or serialization stays impossible.
- **Explicit RNG threading** — domain-essential.
- **Search state as the only public progress value; execution data stays out of it** (§4.2) — right rule.
- **`ValueArray<T>`** — small, pays for itself in record equality.
- **Streaming runs (`IAsyncEnumerable` of search states)** — the correct execution boundary.

### Simplify drastically (right need, wrong concept count)

1. **AOP machinery → one "decorator" concept.** Today: pointcut + advice + aspect + join point + named configuration + wrapper chain + layer ordering + "each advice applies once at its first position". All of this is *"at build time, wrap selected executors"*. Minimal form:
   - Configurations get an optional `Name` **property on the record** (data! serializable! deletes `INamedConfiguration`, `Named*`, `Pointcut.Named`).
   - Run setup takes `IReadOnlyList<Decorator>` where `Decorator = (predicate over configuration node, wrapper factory)`. Predicate replaces pointcut (`& | !` on delegates — free). Registration order replaces layer ordering rules. Dedup by wrapper-factory reference replaces "first position wins".
   - 5–6 glossary concepts → 1.

2. **The `IOperation<TSelf>` / invocation-wrapper machinery → hand-written per-role decorators.** This machinery exists to let *one* wrapper type work over *all* operations without per-role code: private `Invocation` struct per operation, `OperationInfo` statics, `WrappingExecutor<TExecutor,TWrapper>`, `Proceed()`, `CountCandidateWork`, `SupportsCandidateWork`, `IExecutorControlProvider` forwarding. That is an enormous generic apparatus to avoid writing ~8 small decorator classes (one per role) — a classic case where DRY ate KISS. Per-role decorators are ~20 lines each, statically typed, debuggable, and need zero reflection-adjacent statics. `Retry`, `Named`, timing, counting become ordinary decorators. Delete: `IOperation<TSelf>` statics, `OperationInfo`, `IInvocation`, `IInvocationWrapper`, candidate-work accounting, executor-control forwarding rules.

3. **Resources/handles/run-resources/instruments → one "run services" idea.** The spec itself already retreated from handles toward run-bound results (S2, 2026-11-04). Go all the way:
   - **Within a run, sharing = an executor cache in the resolution scope** (already exists as `Sharing`).
   - **Cross-cutting shared state = an object created in the `CreateRun` setup closure and captured by the decorators/terminators that need it** (closures — the thing C# already has). `run.Services` lookup for post-run inspection can be one keyed dictionary on the run, nothing more.
   - This dissolves resource handle identity-in-record-equality (which is what currently *blocks* data-like serializable configurations), `SuppliedResourceHandle`, the lazy serialized default-creation rules, and the "advice has no resource lookup" special rule — advice just holds what its constructor captured.

4. **The observation zoo → events + reducers.** Analyzer, trace, accumulator, trace retention, snapshot, clock, time, moment, inspection, observation, observation source, observation point: ~12 concepts. The sub-agent's unification analysis is right — these are two ideas:
   - **Observation**: an immutable record emitted by an executor (role-call observations are auto-emitting decorators; "observation points" are just executors that emit from inside).
   - **Reducer**: a fold over observations attached at run setup. Append-mode = trace; latest-mode = snapshot; aggregate = accumulator; a timestamping decorator = clock; retention = a reducer policy, not a concept. "Inspection" and "analysis" are one subsystem with two documentation audiences.
   - ~12 concepts → 2–3.

5. **Termination & interception → stream combinators.** An interceptor is `Select` over the state stream; a terminator is `TakeWhile`; "external early stopping" is the same thing applied by the caller. Keeping them as *operator roles with their own advice types and control forwarding* is mechanism for its own sake. (Terminators as configurations can stay for composability, but they should be plain functions over states, not participants in the wrapper/control machinery.)

### Throw out / already legacy (validate the direction)
- Scoped state, child scope policy, execution node/instance — already `Legacy`. Their removal worked; use the same playbook below.
- `INamedConfiguration` + `Named*` wrappers → `Name` record property.
- Candidate-work accounting trio (`SupportsCandidateWork`/`CountCandidateWork`/`RequireCandidateWork`) → role decorators count what their own method signature tells them.
- `IExecutorControlProvider`/`ExecutorControls.Find` → controls belong on the executor contract or nowhere.
- Dual algorithm/operator advice hierarchies, `Pointcut` class family, `Aspect.Create` ceremony.

### Corner cases that must fall out of the core (not add to it)
Walking the operator-unification spec's scenarios against the minimal model:
- **S1 (private counters)**: falls out of "fresh executor per construction" — already true.
- **S2/S4 (shared counting)**: falls out of "setup closure captures one counter; decorator and terminator both close over it". No handles.
- **S6/Q5 (budgets around Cycle, per-turn analysis)**: falls out of "scope = executor cache lifetime"; a Cycle turn is a scope, so per-turn *anything* is per-turn by construction, not by a policy flag.
- **Q2 (observe only A's calls of shared M)**: falls out of decoration happening per resolution path, which the current model already does — it just doesn't need AOP vocabulary to say so.
- **Q7 (share one live object across runs)**: caller creates the object, passes it into both run setups. Closure capture again.

## 4. The tension you must resolve consciously

**Data-like serializable configurations vs. reference identity.** Today, sharing is keyed by configuration *reference*, handles participate in record equality by reference, and advice/aspects are runtime objects with identity semantics. All three are incompatible with "configuration graph = pure data records serializable over REST". The fix that *simplifies* rather than adds: **identity in configurations is explicit data** — a `Name`/key property — and all runtime identity (executor caches, shared state, decorators) lives in the run, never in the configuration. That makes serialization natural *and* deletes mechanisms at the same time. This is the single highest-leverage decision in the whole redesign.

## 5. SOLID/KISS scorecard of the current design

- **SRP**: violated by configurations (settings + child declaration + handle identity + factory + observation-point hosting) and by the resolution scope (construction + advice + resources + sharing + controls + run results). The minimal split fixes both.
- **OCP**: the generic operation machinery is an attempt at OCP (wrap any operation without new code) that costs more than it saves — write the 8 decorators instead (KISS > DRY).
- **LSP**: mostly OK, but role arity reduction rules (§8.4/§8.5) exist to manage generic-parameter pain that a leaner contract wouldn't have.
- **ISP**: role interfaces are good (one method each); keep and lean on them.
- **DIP**: good instinct (everything depends on role contracts), undermined by concrete machinery (handles, aspect layering) leaking into contracts.
- **KISS**: the headline violation — ~45 execution/analysis concepts where ~6 suffice.

## 6. Target vocabulary (proposal)

**~15 canonical terms:** problem, candidate, search space, objective, operator, role (creator/evaluator/mutator/crossover/selector/replacer/refiner — terminator/interceptor demoted to stream combinators), algorithm (= streaming operator), search state, configuration, executor, resolution, run, RNG, observation, reducer. Plus `Name` as a property, not a concept. Everything else becomes legacy, following the deprecation pattern the repo already proved with scoped state.

## 7. Suggested next steps

1. Record this direction (or your amended version) as a plan under `plans/` — this is architectural and needs your explicit acceptance before any code moves.
2. First deliverable: the *algorithm-as-operator unification* + *decorator replaces aspect/pointcut/advice* — biggest concept reduction, touches everything else.
3. Then: observation/reducer unification, then run-services replacing handles, then serialization of configurations as the forcing function that keeps the core honest.

Points where I'd challenge my own proposal before you accept it: (a) unifying algorithm into operator raises generic-arity questions that need a concrete prototype signature first; (b) closure-captured sharing is less discoverable than handles for library authors — may need one thin "run services" API to stay explicit per §Explicit dependencies; (c) terminators-as-stream-combinators may hurt the "termination is configurable data" story for REST-driven setups — might keep a data-level terminator description that *compiles to* a combinator. Happy to prototype (a) or grill any of these decisions further.