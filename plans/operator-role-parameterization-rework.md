# Operator Parameterization and Role Unification

**Decision status**: The generic consuming contract remains the accepted direction. This revision selects solutions to the architecture review findings for the next integrated prototype. It does not claim that the revised foundation has already passed integration or performance validation.

**Implementation status**: Production migration has not started. The retained thin slice is historical feasibility evidence; Package 0 below must pass before production migration.

**Target**: .NET 10 and C# 14, using the SDK selected by `global.json`.

**Baseline**: `a2643bc1005dc47611699cec603ba0106e4231df`. Review and solution probes were run on 2026-10-16 against its Release backend.

This plan replaces the earlier draft, feasibility discussion and design experiments as the specification for this change. Historical measurements remain evidence for their particular implementations. Published architecture documentation continues to describe shipping behavior until the relevant implementation package lands. Editing this plan does not authorize Git operations or begin production implementation.

## 1. Problem, scope and success criteria

The concrete problem is duplicated composition and inspection machinery across nine operator roles. A generic configuration wrapper cannot implement arbitrary nominal interfaces such as `IMutator<TCandidate>`, so sharing only wrapper internals leaves nominal configuration shells in every role.

The selected solution changes consuming slots to `IOperator<TCandidate, TOperation>`, with descriptors such as `Mutation` and `Evaluation`. Generic `Named`, `Retry` and whole-call choice configurations then fit those slots directly. Role-specific execution methods and ordinary leaf authoring signatures remain: `Mutate`, `Evaluate`, `Cross`, and their single-item counterparts are useful domain APIs.

The migration must achieve all of the following:

- Reduce net production machinery after counting the new preparation, binding and advice infrastructure.
- Preserve candidate-only consuming contracts: search space, problem and search state do not become algorithm property type arguments.
- Preserve configuration-reference identity, ancestor reuse, child-local ownership, advice ordering and weak lifetime behavior.
- Preserve all nine roles, their authoring paths, recommendation flows, weights, routing granularity, defaults and operation semantics.
- Support an execution implementing several operations, with one prepared owner and intentionally shared state.
- Preserve live runtime controls through transparent configured wrappers and inspection advice.
- Support external operations, external advice and external generic topologies without a built-in operation registry.
- Keep extra invocation allocations at zero on specified warmed synchronous success paths, and meet the explicit latency budgets in section 8.

Algorithms already have independent `IAlgorithm` contracts in production. Retain that separation. The prototype's `AlgorithmRole<TState>` is an experiment to remove, not a production dependency to decouple. No operator marker claims that the work is computationally cheap or performs no external I/O: these are synchronous signatures, not workload guarantees.

### 1.1 Reproducible source audit

The complete baseline directories contain 159 C# files and 9,409 physical source lines: 125 files / 7,495 lines in `src/HeuristicLib/Operators`, and 34 files / 1,914 lines in `src/HeuristicLib/Inspection`. These totals are an inventory, not a claim that every line is duplicated. The former 76% duplication estimate and 4,000–5,000-line deletion promise are withdrawn because they lack a selected-file manifest and a net implementation comparison.

Package 0 records an audit manifest under its evidence directory. For each tracked baseline file, record the path, baseline blob ID, physical line count, responsibility and intended disposition: retain, migrate, consolidate or delete. Enumerate from `git ls-tree -r --name-only <baseline> -- src/HeuristicLib/Operators src/HeuristicLib/Inspection`; count the content from that commit, not a later working tree or generated output. Physical lines include blank lines and comments, with a trailing newline adding no extra line. Retain the same counting method for the final comparison.

The final ledger includes added and moved machinery anywhere in production source, including `Execution`; count moves once and do not hide additions outside the initial directories. Report production, tests, samples and documentation separately. Report adapter count and extension-author code as well as lines: moving duplication into a new binding hierarchy is not automatically an improvement. Positive net reduction and simpler representative authoring are required before expanding beyond the canary. If the integrated slice cannot demonstrate those benefits within the performance budgets, stop the broader migration and reconsider the bounded alternative of shared inspection with existing role configurations.

## 2. Selected contracts and responsibility boundaries

The excerpts in this section specify the intended contract shape; imports and implementation bodies are omitted where stated. Complete, separately compiled extension examples must be part of Package 0 before these APIs are added to production.

### 2.1 Common marker, generic consuming contract and independent algorithms

Keep the existing non-generic `HEAL.HeuristicLib.Operators.IOperator : IConfigurationNode`. It remains the common operator identity used by pointcuts, recommendations and authoring analyzers. It does not select an operation and does not create a competing nominal-role slot.

```csharp
namespace HEAL.HeuristicLib.Operators;

public interface IOperator<TCandidate, TOperation> : IOperator
    where TOperation : IOperation<TOperation>
{
    IOperatorContract<TCandidate>? CandidateContract =>
        this as IOperatorContract<TCandidate>;

    PreparedOperator Prepare();
}
```

`IOperator<TCandidate, TOperation>` is the single consuming contract for an operator slot. Remove nominal configuration interfaces such as `IMutator<TCandidate>` at their per-role cutover, migrating every in-repository caller. Keep role-specific execution interfaces such as `IMutatorExecution<...>`. `TCandidate` remains invariant.

`IAlgorithm<TCandidate>` and `IAlgorithm<TCandidate, TSearchState>` remain in `Algorithms`, including heterogeneous algorithm collections and their current hosting surface. An algorithm never implements `IOperator<TCandidate, AlgorithmOperation<...>>`. Synchronous operator composition therefore has no overload accepting an algorithm.

Keep `IRecommends<TOperator> where TOperator : class, IOperator`. A recommendation for mutation becomes `IRecommends<IOperator<TCandidate, Mutation>>`. Preserve explicit value > problem recommendation > search-space recommendation, decline-and-continue behavior, aggregated missing-parameter diagnostics, and a fresh configuration for each successful recommendation. `Create(...)` infers from required operators; `For(...)` consults recommendations. Optional collaborators continue to use their current defaults rather than acquiring new recommendation rules.

### 2.2 One preparation owner, several typed construction views

Replace the previous run-generic `Prepare(binding)` contract with two stages:

1. `Prepare()` validates immutable configuration and creates a `PreparedOperator` once for the configuration reference in its selected owner state. It receives no scope and no requesting run types. Allocate persistent counters, caches and other execution state here.
2. `PreparedOperator.Bind(binding)` checks the requested operation and run signature and returns a checked `PreparedFactory`. It may produce several typed construction views over the same prepared state. It must not initialize that state again or retain children from a previous binding.

```csharp
namespace HEAL.HeuristicLib.Execution;

public abstract class PreparedOperator
{
    public abstract PreparedFactory Bind<
        TCandidate, TOperation, TSearchSpace, TProblem, TRunSearchState>(
        OperationBinding<TCandidate, TOperation,
            TSearchSpace, TProblem, TRunSearchState> binding)
        where TOperation : IOperation<TOperation>
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TRunSearchState : class, ISearchState;
}
```

This is a deliberate change from the earlier plan. A configuration shared through two operation interfaces has one non-generic preparation method, not two independent state initializers. Stateless and stateful authoring bases hide the prepared carrier; a domain leaf still overrides only its role method and, when applicable, `CreateInitialState`.

Two smaller fixes were considered and are insufficient together without the ownership split. Making only the bound carrier covariant repairs simple compatible factory conversions but does not adapt invariant contextual wrapper delegates. Keying preparation directly by operation/run signature avoids the cast but initializes a shared operator more than once. The selected split keeps one logical owner and permits several checked constructors. It adds a preparation carrier and per-view cache entries; their allocation, complexity and net source cost must earn their place at Package 0.

Use existing `Fits` metadata for preflight compatibility checks. `Bind` is the authoritative check for the selected view and must reject an incompatible signature before invoking a construction delegate or resolving a child. Successfully completed owner preparation remains available if a later view or advice binding fails. Failed owner preparation is not cached. This separates configuration validation from run-signature binding; the corresponding operator factory rules must be updated with the production foundation.

An explicit execution author still writes one typed construction factory, using an authoring-base hook. The base's public preparation entry is sealed; there is no second public factory path that independently initializes state. A topology prepares persistent policy/state once, then supplies a run-generic binding recipe. Never put configurations, scopes, execution children or delegates bound to those children in ordinary framework-managed leaf state.

### 2.3 Operation descriptors and their execution contracts

Descriptors use domain nouns and provide their binding through a static declaration. No central enumeration discovers or registers operations.

```csharp
public interface IOperation<TSelf> where TSelf : IOperation<TSelf>
{
    static abstract OperationBinding<TCandidate, TSelf,
        TSearchSpace, TProblem, TRunSearchState>
        CreateBinding<TCandidate, TSearchSpace, TProblem, TRunSearchState>()
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TRunSearchState : class, ISearchState;
}
```

All nine descriptors and their semantics belong to the foundation specification:

| Descriptor     | Execution contract and operation                                         | Declared candidate work after success |
| -------------- | ------------------------------------------------------------------------ | ------------------------------------- |
| `Creation`     | `ICreatorExecution.Create(count, ...)`                                   | Returned candidate count              |
| `Crossover`    | `ICrossoverExecution.Cross(parentGroups, ...)`                           | Returned offspring count              |
| `Mutation`     | `IMutatorExecution.Mutate(candidates, ...)`                              | Returned candidate count              |
| `Evaluation`   | `IEvaluatorExecution.Evaluate(candidates, ...)`                          | Input candidate count                 |
| `Selection`    | `ISelectorExecution.Select(population, objective, count, ...)`           | Returned selected count               |
| `Refinement`   | `IRefinerExecution.Refine(candidates, ...)`                              | Returned refined candidate count      |
| `Replacement`  | `IReplacerExecution.Replace(previous, offspring, objective, count, ...)` | Returned survivor count               |
| `Termination`  | `ITerminatorExecution.IsTerminalState(state, ...)`                       | Unsupported                           |
| `Interception` | `IInterceptorExecution.Transform(current, previous, ...)`                | Unsupported                           |

Retain existing role operation signatures and result representations. In particular, this change does not resolve the existing requirements/glossary versus evaluator-output discrepancy: the baseline evaluator returns objective vectors. Changing evaluator output or candidate flow requires separate scope; do not smuggle it into role parameterization.

Execution interfaces keep meaningful contravariance in search space and problem. Termination execution remains contravariant in search state; interception remains invariant because it returns that state type. Invalid substitution fails at binding with a useful signature diagnostic.

There is no `ISynchronousOperation` marker because every `IOperation` binding must implement the synchronous invocation protocol below. There is no empty candidate-counting marker; metric applicability is explicitly checked at binding, as specified in section 5.

### 2.4 Public binding protocol

`OperationBinding` is an advanced **public extension contract**, located with execution preparation machinery. It is hidden by authoring conveniences, but external descriptor/binding authors must be able to implement it. Its runtime counterpart is still called an execution node; this is not a replacement `IExecutionBinding` vocabulary.

Its required surface is:

```csharp
public abstract Type ExecutionType { get; }
public abstract bool SupportsCandidateWork { get; }
public abstract PreparedWrapper Identity();
public abstract PreparedFactory Wrap(
    IOperator<TCandidate, TOperation> child, PreparedWrapper wrapper);
public abstract PreparedFactory Compose(
    IReadOnlyList<IOperator<TCandidate, TOperation>> children,
    PreparedComposite composite);
public abstract PreparedWrapper PolicyWrapper(SynchronousPolicy policy);
public abstract PreparedComposite PolicyComposite(SynchronousPolicy policy);
public abstract PreparedWrapper Success<THook>(THook hook)
    where THook : struct, ISuccessHook;
public abstract PreparedWrapper Hooks<THooks, TToken>(THooks hooks)
    where THooks : struct, ICallHooks<TToken>;
```

`Forward(child)` is a concrete convenience implemented as `Wrap(child, Identity())`, not another abstract responsibility. The typed implementation base, which knows `TExecution`, implements checked carriers, child resolution, identity, wrapping and multi-child construction once. An operation author supplies adapters from its named execution method to the three invocation profiles: success, lifecycle hooks and around policy. The former prototype's `ISynchronousBinding` facilities move into this mandatory protocol; removing the marker must not remove those facilities.

The policy invocation uses typed arguments, a typed result and a cached static continuation. Arguments may be value tuples/structs, without boxing or per-call closure creation. Do not add a second universal `Invoke` method to domain execution interfaces. Erasure/checking is confined to preparing or binding factories, never repeated in the operation hot path.

Keep checked wrapper and composite carriers initially. A later simplification is acceptable only if it preserves owner preparation, variance, controls, external implementation and measured costs. Package 0 settles the public protocol before production migration; delegate simplification is no longer an open-ended production-foundation spike.

### 2.5 Typed resolution stays convenient

Retain thin `Resolve`, `ResolveOptional` and `TryResolve` overloads whose parameter is the corresponding closed operation contract. For example, a mutation overload takes `IOperator<TCandidate, Mutation>` and returns `IMutatorExecution<TCandidate, TSearchSpace, TProblem>`. A typed scope therefore still supports `typed.Resolve(mutator)` and the control-returning form without explicit execution type arguments. These adapters do not recreate nominal configuration interfaces or wrappers.

Supply one such family per built-in operation; an external operation author supplies its corresponding typed extension through the same binding bridge. Do not attempt to infer a hidden `TExecution` solely from a constraint, or claim C# can obtain an associated execution type from `TOperation`. The shared resolver checks the prepared carrier, while each thin operation extension supplies its named execution type statically.

The existing three-argument typed scope remains usable for state-independent operations and binds their unused run-state argument as `ISearchState`. The four-argument scope passes the actual run state. State-dependent operations require an appropriate state view; binding a population-specific interceptor through an `ISearchState` view fails explicitly. No consumer must name run types on an operator property to make this work.

## 3. Resolution, variance and multi-operation state

### 3.1 Covariant bound views

Use a covariant read-only interface in factory signatures, retaining an ordinary sealed class as the carrier:

```csharp
public interface IBoundExecution<out TExecution> : IExecutionNode
    where TExecution : class, IExecutionNode
{
    TExecution Operation { get; }
    IControls Controls { get; }
}

public sealed class BoundExecution<TExecution>(
    TExecution operation, IControls controls) : IBoundExecution<TExecution>
    where TExecution : class, IExecutionNode
{
    public TExecution Operation { get; } = operation;
    public IControls Controls { get; } = controls;
}
```

Factories return `IBoundExecution<TExecution>`, not the invariant concrete class. `PreparedFactory.Raw<TAuthoredExecution>` wraps at the **authored** execution type when the carrier is created. `Require<TRequestedExecution>` checks/converts the stored delegate; it must not introduce a lambda closing over `TRequestedExecution` that discards the original covariance. Wrapper constructors receive and return the bound interface for their exact requested view.

Covariance alone is insufficient for contextual wrappers and invariant wrapper-factory delegates. Use the owner/view separation below so the same source can bind another compatible state view without re-preparing state or casting an old wrapper constructor to an incompatible delegate type.

### 3.2 Owner records and construction-view factories

Adapt `ResolutionScope.State` internally. Preserve one owner record per configuration reference; do not change source identity to `(configuration, operation)` and thereby split state.

Each operator owner record contains:

- One successfully prepared `PreparedOperator`.
- Checked construction factories keyed by the complete view: candidate, operation, search space, problem, run search state and requested execution type.
- Advice preparation keyed weakly by advice reference, with one prepared advice per selected configuration and typed wrapper constructors separated by view.

View keys distinguish constructors, not logical executions or state owners. Cache no contextual execution nodes. A stateless or ordinary stateful prepared leaf may itself retain one raw node, as it does today. View construction happens with the owner's state and the requesting scope's aspect chain. Resolve every declared child through that construction scope.

For a requested view, obtain/check the raw construction factory and prepare/bind its applicable advice constructors before invoking that raw factory. This makes a selected unsupported metric fail before constructing its child graph. Then construct the raw node, apply the already validated wrappers in the existing application order, and expose controls only after the complete chain succeeds. This preflight changes constructor/error ordering where necessary; it does not traverse or resolve absent sources to discover them.

Preserve current ancestor reuse and no hoisting: a child-first owner stays child-local; a parent-first owner is reused by descendants. `With` changes aspects without new state. Fresh/Retained/Shared activation continues to be owned by `ChildScopeSelector`; do not introduce a second scope abstraction or resolver-owned child-retention keys.

Cycle detection remains keyed by configuration reference across all views and related scopes. Changing operation cannot evade a construction cycle. Failed view binding/construction is retryable without discarding successful owner preparation. Advice state survives contextual rebuilding; original parent nodes keep their original children. Weak advice keys and owner-local caches must not keep discarded configurations, aspects or fresh states alive through global descriptor caches.

### 3.3 Multi-operation configurations

Support a configuration implementing several `IOperator<TCandidate, TOperation>` interfaces in one owner scope. It implements `Prepare()` once. A prepared raw execution may implement all of its named execution contracts, or an explicit prepared object may construct distinct operation nodes over the same private state.

The prepared object's binding implementation declares those views; there is no library registry of known operation combinations. Resolving the second operation must not reinitialize the shared state, regardless of resolution order. Operations with intentionally independent state use separate configuration objects. Contract queries and typed advice use the selected operation view, not an arbitrary first interface on the object.

The old slice's test expecting the second role to fail is a documented limitation of that slice, not an acceptance test for the new foundation. Replace it in the integrated prototype with successful shared-state tests, including an external execution implementing two operations.

### 3.4 Runtime controls

Keep operation-free lookup:

```csharp
public interface IControls
{
    TControl? Get<TControl>() where TControl : class;
}
```

Controls refer to the prepared mutable data used by the callable execution. Query them only after the entire advised chain has bound successfully, and invoke work through the advised `Operation`.

- Transparent `Named`, retry of the same child, and inspection hooks forward exactly the child's control provider.
- General advice or wrappers that replace the effective execution must explicitly return its controls or `NoControls`; preserving an unrelated child's controls is incorrect.
- Multi-child choice and pipelines expose `NoControls` by default. A domain-specific composite may expose a deliberately authored aggregate control, but projection never recursively picks an arbitrary child.
- A configuration with its own controls decides their relationship to its child explicitly. Rebinding never resets a live value; independent owners start from the configured initial value.

The control suite covers stacked names/retry/advice; updates observed by later calls; explicit composite boundaries; external control interfaces; shared multi-operation state; parent reuse; and Fresh/Retained/Shared rebinding. The adaptive canary is the real `EvolutionStrategy`, whose execution reads and updates `IMutationStrengthControl`. A manual control update and a GA comparison remain useful tests but do not replace that canary.

## 4. Composition and deterministic routing

### 4.1 Generic whole-call topologies

`Named<TCandidate, TOperation>` adds metadata and forwards the child. It creates no extra callable operation node when no advice selects it. Wrapper and child remain separately selectable configurations, with reference identity preserved.

`ChooseCall<TCandidate, TOperation>` snapshots children and `Weights` into `ValueArray<T>`, prepares a `WeightedItemSampler` once, and selects one child for the entire invocation. Empty weights mean uniform sampling. Preserve `WeightedIndexSampler` semantics for zero, negative, NaN and infinite weights, as well as existing random-draw behavior. Reject an empty child collection or mismatched nonempty weight length during preparation. Do not replace the sampler with `NextDouble() * childCount`.

Generic `WrappingOperator` and `MultiOperator` share preparation/construction responsibilities. They do not claim that every operation supports a sequential result-to-input pipeline. Keep operation-specific pipeline adapters when input/result shapes or semantics differ. Preserve `CandidateContract` forwarding and conservative composite contract composition; do not equate equal execution signatures with equal domain roles.

### 4.2 Per-item routing is retained for four roles

The migration map is semantic, not a blanket replacement of every `ChooseOne*` with `ChooseCall`:

| Existing composition | Target                          | Routing unit                   |
| -------------------- | ------------------------------- | ------------------------------ |
| `ChooseOneCreator`   | `ChoosePerCandidateCreator`     | Each requested output position |
| `ChooseOneCrossover` | `ChoosePerParentGroupCrossover` | Each parent group              |
| `ChooseOneMutator`   | `ChoosePerCandidateMutator`     | Each input candidate           |
| `ChooseOneRefiner`   | `ChoosePerCandidateRefiner`     | Each input candidate           |
| `ChooseOneSelector`  | `ChooseCall<..., Selection>`    | Entire selection call          |
| `ChooseOneReplacer`  | `ChooseCall<..., Replacement>`  | Entire replacement call        |

The four per-item forms continue to use `WeightedRouter`. Assign every position before invoking children, process nonempty child batches sequentially in child order, preserve the original batch when every item chooses one child, enforce the existing result-count requirement, and restore assignment/input order. The same supplied RNG is used for routing and child calls; the router does not fork it. Per-item work inside a child may separately use `BatchExecution` concurrency. Introducing parallel child-batch execution is outside this migration.

Retain weights, snapshots, weighted edge cases, rate helpers and their existing RNG behavior on all migrated forms. Test empty inputs, single choices, unequal and degenerate weights, grouped calls, skipped empty branches, wrong result counts, concurrent child implementations and seeded production comparisons for every routed role.

### 4.3 Retry is explicit, bounded and non-transactional

`Retry<TCandidate, TOperation>` catches only a directly thrown `RetryableFailure`. `MaximumAttempts` includes the first call and must be at least one; `.Retry(2)` permits two calls. Exhaustion propagates the last original failure. Other exceptions, cancellation and `AggregateException` are not unwrapped or retried. This preserves the batch engine's failure shape.

Every attempt uses the same input values, child execution and RNG object. The wrapper makes no random draws and performs no fork, rewind or snapshot. Draws and state changes made by a failed attempt remain; there is no rollback of partial child effects or inspection data. An operator may signal `RetryableFailure` only when another invocation under that contract is meaningful. An external operation does not become transaction-safe merely because its signature is synchronous.

Keep retry as configured behavior and counting/timing as advice. The shared invocation engine does not introduce an `AdvisedOperator` configuration or a second persistence model for runtime advice.

## 5. Inspection, advice dispatch and metric applicability

### 5.1 A real protocol for general and typed operator advice

Add a public non-generic `OperatorAdvice : Advice<IOperator>` for operation-independent synchronous concerns. It prepares a `PreparedOperatorAdvice` once per advice/configuration/owner. That prepared object has a run-generic `Bind` method receiving the typed source and `OperationBinding`; it calls `binding.Success(...)`, `binding.Hooks(...)` or `binding.PolicyWrapper(...)` as appropriate. For example:

```csharp
public abstract class OperatorAdvice : Advice<IOperator>
{
    public abstract PreparedOperatorAdvice Prepare(IOperator source);
}

public abstract class PreparedOperatorAdvice
{
    public abstract PreparedWrapper Bind<
        TCandidate, TOperation, TSearchSpace, TProblem, TRunSearchState>(
        IOperator<TCandidate, TOperation> source,
        OperationBinding<TCandidate, TOperation,
            TSearchSpace, TProblem, TRunSearchState> binding)
        where TOperation : IOperation<TOperation>
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TRunSearchState : class, ISearchState;
}
```

An external allocation tracker or call counter can implement this once and participate on an external operation through that operation's binding adapter. This replaces the previous incomplete `MemoryAllocationAspect` example, which omitted its essential dispatch mechanism.

Keep `OperatorAdvice<TCandidate, TOperation>` for typed observations and operation-specific policies. It likewise separates once-only preparation from run-specific wrapper binding, and its base supplies the internal dispatch bridge. Advice applicability includes the selected operation view: mutation-specific advice on a configuration implementing mutation and evaluation applies only to mutation. General `OperatorAdvice` can apply to both, sharing its intentionally common prepared state. Pointcuts still select configuration references; this adds no caller-path or occurrence selection model.

Resolution dispatches through the supported general/typed preparation protocols. Do not cast every matching advice to one closed generic advice base. A matching unsupported `Advice` subclass fails with `InvalidOperationException` naming its type, the selected operation and the supported extension bases; it never leaks `InvalidCastException`. The generic `Advice<TConfiguration>` base alone does not promise that it can wrap an arbitrary execution method.

Inspection factories receiving `IOperator<TCandidate, TOperation>` retain that operation filter in their advice, including candidate clocks and budgets. The same object may implement several operation interfaces, so a reference pointcut alone is insufficient to restrict its operation view. An explicitly general factory receiving only the non-generic `IOperator` may observe all its operation views. Reuse the hook implementation behind these adapters; do not manufacture another per-role counter implementation.

Preserve first-position advice deduplication, scope-layer order and typed source identity. Exact wrapper constructors are created/cached for the current view, while persistent advice state is prepared once. The wrapper-factory delegate is invariant; do not assume bound-view covariance makes old wrapper delegates interchangeable.

### 5.2 Candidate metrics fail explicitly during binding

Select **preparation/binding-time rejection** for unsupported candidate metrics. This is intentional, replacing the former compile-time marker constraint. Candidate/operation compatibility and applying synchronous topology extensions to algorithms remain compile-time checks; metric applicability follows the same checked binding boundary as hidden run-type compatibility.

`CountCandidates`, candidate clocks, count-based terminators and candidate budgets must require `binding.SupportsCandidateWork` before constructing an operator view. A selected unsupported operation throws `InvalidOperationException` with the operation, metric and configuration identified. It must never silently skip the metric or pretend that an unsupported operation processed zero candidates.

A binding with candidate work supplies the count after a successful call according to section 2.3. An external scalar operation declares no candidate work; an external batch operation can declare and report its own work without a marker or library edit. Count totals describe work at the selected boundary, not unique candidates or hidden nested evaluations.

A budget validates applicability when its selected target is bound, before that target can execute. When the target is absent from the algorithm graph, preserve the current no-match behavior: no counter advances and no new source-discovery traversal is introduced. Include an explicit unsupported-target-in-graph case so a selected but unsupported metric cannot leave a running budget silently inert.

Replace the old `UNSUPPORTED_METRIC` negative compiler probe with a runtime binding-failure test. Keep compile-time rejection for mismatched candidate types, operation slots, observations, stateful algorithm types and unsupported concurrency setters where applicable. Removing `WithConcurrency` also requires replacing that setter-specific probe with checks for the final record API rather than preserving a removed method solely for its diagnostic.

### 5.3 Success, fault and cleanup semantics

Use separate lightweight success hooks, lifecycle hooks with an invocation-local token, and around policies. Counting records a completed successful boundary. Duration includes failed calls and is recorded during cleanup. Do not allocate observation records for simple counting/timing.

The hook failure contract is explicit:

1. If `Enter` throws, do not invoke the child; no token exists, so do not invoke `Fault` or `Finally`.
2. After `Enter` succeeds, invoke `Finally` exactly once. Invoke `Fault` only when the child/continuation throws, and `Success` only when it returns successfully. A failure in `Success` is a callback failure, not a second failure of the already completed child.
3. If one failure occurs, propagate it with its original stack. If the child and/or callbacks produce several failures, throw `AggregateException` containing the child failure first, then the success/fault callback failure, then cleanup failure in occurrence order. Never mask or discard a failure. Normal cancellation remains `OperationCanceledException` when callbacks succeed; an additional callback failure is a failure aggregate.
4. Inspection/state updates already completed are not rolled back. A callback failure fails the surrounding invocation even if an inner success counter already advanced.

Test those paths independently of the benchmark harness. For retry, inner call counting counts successful attempts, outer call counting counts successful logical invocations, inner duration includes failed attempts, and outer duration includes the entire retry operation. Wrapper placement makes those boundaries explicit.

### 5.4 Streaming algorithms retain their own advice

Keep algorithm advice separate. Measure awaited `MoveNextAsync()` work, including the final false or faulting advance, excluding consumer pauses. Observe each yielded state after interception and preserve the prepared iteration sequence across contextual rebinding. Get-enumerator, advance, cancellation and disposal failures remain distinct test cases; no synchronous retry or success hook is applied to enumerable creation.

Preserve `AlgorithmRun` cancellation and pause semantics: cancellation at root yield boundaries and disposal of a run stream can pause the retained iterator; that is different from disposing the underlying algorithm iterator. Early disposal of an actual iterator still disposes its child exactly once. Test natural completion, pause/resume, cancellation, nested early stopping and faulting disposal through the real host.

## 6. Authoring and configuration ergonomics

Retain stateless, stateful and explicit-execution authoring at all existing reduced arities for all nine roles. Preserve single-item creator, crossover, mutator, evaluator and refiner helpers wherever they exist in the baseline; do not create single-item forms for roles whose semantics are inherently batch/state based. `CrossParents` remains the single crossover override name.

`BatchExecution` continues to own item RNG forks, sequential/concurrent scheduling, order and exception behavior. A single-item base seals its batch method and retains `ExecutionConcurrency`. Use static callbacks with explicit state, without per-call closures. Ordinary stateful leaves prepare one fresh state per owner; composite execution nodes get prepared state and newly resolved children separately.

Configurations use records and immutable value strategies. Required collaborators use required constructor parameters, following developer guidelines section 4.6; existing required-property algorithm construction remains supported and is not expanded into a new competing input path. Optional settings are `init` properties with defaults. Keep object initializers and `with`, and snapshot retained ordered collections into `ValueArray<T>`. Remove the prototype's redundant `With<Property>` layer rather than shipping it.

Constructors, static inference companions and fluent child-composition extensions must produce equivalent configurations. Do not claim that every `with` eliminates allocation: a single `with` batches setting changes into one record copy; copying a configuration creates a new source identity for resolution and pointcuts.

The complete production GA collaborator/default contract is retained:

- Required creator, crossover and mutator, with their slots converted at the owning role cutover.
- Non-null evaluator and selector defaults from `GeneticAlgorithmDefaults`.
- Optional refiner, terminator and inherited interceptor.
- Population size 100, maximum generations `int?` defaulting to 1000, mutation rate 0.1 and one elite, including current edge-case semantics and the meaning of a null generation limit.

Preserve `EvolutionStrategyDefaults` and every other algorithm's own defaults. Small prototype settings such as population size eight or five generations are explicit fixtures, never new library defaults. Do not rewrite the algorithms to match shortened illustrative snippets.

## 7. Extensibility and repository-rule reconciliation

An external package must be able to compile each of these stories independently against the integrated prototype assembly:

1. A custom synchronous scalar operation, its binding and a candidate-focused configuration, using generic naming, retry, whole-call choice, call counting and duration.
2. A custom batch operation reporting candidate work and participating in candidate clocks/budgets; a scalar counterpart rejected for those metrics.
3. One general custom advice implementation applied to built-in and external operations, including termination and interception, without enumerating them.
4. A generic `Twice` wrapper using `binding.PolicyWrapper` for both built-in and external operations, with its controls and counting boundaries specified.
5. One configuration and raw execution implementing two operations, shared once-only state, typed advice restricted to one operation and general advice spanning both.
6. A custom control interface and custom operation recommendation, with a fresh recommended configuration each time.

External descriptor authors must implement the binding protocol, including role adapters; extensibility is not a claim that a new method signature needs no adapter code. Leaf authors and algorithm users do not implement that protocol.

The following changes to current guidance are part of the production packages, not edits to shipping documentation in advance:

- Developer guidelines sections 4.3–4.4 and 4.7: replace the operator's single run-typed preparation factory with one public owner preparation entry plus checked construction-view binding. Keep child resolution in construction delegates, and keep algorithms' existing public factories.
- Sections 4.12 and 8.3–8.5: retain role authoring/execution symmetry, reduced arities and meaningful execution variance; replace only nominal configuration membership with the generic consuming contract. No public per-role shims remain.
- Section 8.11: one adapter per operation/profile remains acceptable. Measure that matrix explicitly; do not introduce a second hidden matrix per aspect type or run-type combination in public APIs.
- Layering: operator consuming contracts/descriptors belong with operator composition contracts; preparation/binding carriers and dispatch belong with execution machinery. Inspection owns metrics and typed observations; `TraceAnalyzer` remains in `Analysis`. Hosting never acquires an Analysis dependency.
- The backlog rejection of typed universal invocation is reopened only for generic **configuration slots** and shared policy/advice adapters. Named domain execution methods remain. The nominal-membership premise changes because slots now accept the generic contract; C# has not gained structural interface membership.
- The rejection of static execution-role advice/factory objects is reopened narrowly for explicit configuration-owned prepared state and view adaptation. Add no static wrapping members to contravariant execution interfaces, and require the multi-operation fixture before accepting the additional carrier cost.
- The rejection of binding as public runtime vocabulary remains: `OperationBinding` is the advanced preparation adapter, while the callable runtime object remains an execution node. The rejection of source generation and advice-as-configured-wrapper systems also remains.

Record the measured outcome and the precise reopened premises in this plan and the backlog when production implementation is accepted. Update contributor/architecture documents to the implemented state in the corresponding package; do not silently leave contradictory rules for later authors.

## 8. Performance evidence and acceptance budgets

The retained `results.txt` contains illustrative timings for the original slice, not the revised foundation. Its cheap mutation paths reported raw 1.33 ns, named 1.27 ns, direct counting 8.68 ns, shared success counting 12.40 ns, lifecycle callbacks 16.31 ns and around-policy counting 29.56 ns per batch, with zero measured allocations on those warmed success paths.

The lifecycle case used `SuccessfulCallHooks` and `Unit`; it did **not** measure `DurationHooks`, a `long` timestamp token or `TimeProvider`. Retry was not measured. The success-hook path was approximately 43% slower than the direct counting comparison. The Gaussian results were noisy local samples, not evidence for universal negligible overhead. No historical result is a guarantee for the revised design.

Use these initial acceptance budgets for Package 0 and the production canary:

- Unadvised generic resolution and `Named` return the same callable node as their child where forwarding suffices; there is no added operation dispatch or invocation allocation.
- Extra warmed per-invocation allocations are 0 B for success counting, actual duration measurement, whole-call choice, a retry that succeeds on its first attempt and representative three-layer stacks. Measure incremental bytes relative to equivalent leaf work. Construction, result storage and failures/exception reporting are accounted for separately.
- Shared success counting may add at most 5 ns/call over an equivalent direct counter. Actual lifecycle/duration wrapping may add at most 10 ns/call over an equivalent direct duration wrapper. A general around-policy path may add at most 25 ns/call over the corresponding direct policy. These are absolute cheap-path deltas, not a promise of zero relative slowdown.
- On representative 32-candidate numerical mutation and evaluation batches, the median end-to-end overhead must also stay within 2% of equivalent direct instrumentation. Cheap single-value/single-candidate cases use the absolute budgets above. Both cheap and numerical gates must pass; expensive work does not excuse a failed cheap-path budget.

Use matching counters, synchronization, `TimeProvider`, inputs and continuation behavior in each direct/shared comparison. Run Release, warm tiered execution, include several isolated process runs, report distribution and uncertainty, and retain environment/backend hashes. Use a benchmark harness with allocation diagnostics and an anti-elision sink. If uncertainty straddles a budget, improve the measurement before claiming success. Do not repeatedly broaden or rerun checks after a stable result unless a change or unresolved concern justifies it.

Measure actual duration hooks, weighted choice, first-success retry, failure-then-success retry latency, stacked advice, external scalar work, refinement/replacement, and preparation/rebinding costs separately. Failure paths need no zero-allocation claim. Report source reduction and runtime cost together. A failed budget blocks broad migration; it is not waived by referring to a slower workload or changing the threshold after seeing a result.

## 9. Migration packages and coexistence

Packages are sequential and independently reviewable. Package 0 is the additional integrated feasibility gate; Packages 1–5 are production migration. A package is complete only when affected projects compile, its owned behavior tests pass, and its public API/examples and relevant documentation agree.

### Package 0 — Integrated prototype and baseline ledger

Create a new integrated prototype beside the retained historical slice. It must implement the selected owner/view preparation split, common marker, all nine descriptors, covariant bound interfaces, full synchronous binding protocol, general and typed advice, runtime metric rejection and independent algorithms. Do not patch the old reports to make them appear to validate it.

Port the useful existing fixtures and add the review regression cases, real `EvolutionStrategy` adaptation, weighted routing for all four per-item roles, external extension stories, once-only advice state and multi-operation ownership. Exercise the revised preparation/view caches through `ResolutionScope` behavior, not only an isolated dictionary. An experimental host bridge may validate execution semantics here, but does not establish native public hosting integration.

Produce the source manifest, matched implementation-size comparison and performance report. Freeze complete public contract fixtures, including authoring-base hooks. **Gate**: all correctness/extension fixtures and section 8 budgets pass, with a positive net-reduction case and an explicit inventory of work remaining for native production hosting. Failure keeps production migration unstarted; it does not retroactively erase the accepted direction or historical evidence.

### Package 1 — Production foundation and resolver ownership

Add generic operator contracts/descriptors beside the existing per-role contracts, checked prepared carriers, covariant bound views, owner/view caches, control providers and advice dispatch. This package does not remove a nominal role until its consumers cut over. Keep the common non-generic operator marker unchanged.

The legacy resolution path and the new path must use the same scope ancestry, owner-selection rules, aspect layers and configuration-reference cycle guard. Their coexistence is a temporary migration detail, not a second public ownership model. A configuration is prepared through its declared path; internal adaptation must reuse its selected owner rather than create an independent state. Cover mixed old/new child graphs explicitly before the canary.

**Gate**: core unit tests, focused resolver/control/variance/multi-operation/weak-lifetime suites, external API fixtures and full solution build pass. Update architecture rules and tests for the implemented foundation. No public compatibility aliases or obsolete shims are introduced.

### Package 2 — Mutation/evaluation cutover and real algorithm canaries

Cut over mutation and evaluation across **all** in-repository consumers, including encoding-specific operators, problem-specific leaves, GA, EvolutionStrategy, other algorithms, recommendations, advice/observations, Experimental, PythonInterop, tests, samples and imported documentation examples. Migrate stateless/stateful/explicit bases and the existing single-item helpers, generic naming and retry. Remove those two nominal configuration interfaces and their redundant shells only after their complete caller inventory is migrated.

During this package a GA may have new mutation/evaluation slots and still have legacy creator/crossover/selector/refiner/terminator/interceptor slots. This is intentional per-slot coexistence, not two contracts for the same slot. Generic wrappers are promised only for the roles already cut over; no temporary public nominal wrapper shim is needed. Migrate a role across projects in one package rather than breaking callers until Package 5.

Preserve seeded GA results generation by generation, with defaults and edge cases covered separately from small explicit fixtures. Use `EvolutionStrategy` to prove actual per-generation mutation-strength adaptation through `Retry(...).Named(...).Named(...)` plus inspection. Compare raw and transparent-wrapped control trajectories and outputs; test parent/child and retained contexts.

**Gate**: core, API usage, affected Experimental/PythonInterop and documentation validation pass, affected scenario canaries pass, the solution builds, section 8 budgets pass, and source/extension-cost evidence still justifies continuing. Full solution tests are required for this first substantial consumer cutover.

### Package 3 — Remaining candidate operations and routing

Cut over creation, crossover, selection, refinement and replacement, including all public slots, recommendations and cross-project callers. Implement complete authoring ladders and observation/counting/timing adapters. Preserve operation-specific pipelines and the four per-item routing forms in section 4; migrate whole-call selector/replacer choice to generic `ChooseCall` with weights.

Update construction companions and rate helpers, and remove the migrated nominal interfaces and redundant topologies. Retain real domain-specific composites such as transformed creators and refiner-backed evaluators; generic signatures do not replace their distinct responsibilities.

**Gate**: all affected suites/API examples/docs compile and pass; seeded weighted/routed behavior, contracts, cardinality and exception semantics match the baseline. The deleted-file inventory has a behavior-preserving replacement for every public composition, and no remaining caller depends on the removed interfaces.

### Package 4 — State-dependent operations, native hosting and meta-algorithms

Cut over termination and interception with their full stateful/stateless/explicit authoring families, advice and all consumers. Test shared broad terminators across distinct compatible state views in both resolution orders, including configured naming and typed/general advice. Test that invariant interceptor mismatches fail without executing work or corrupting a successfully prepared owner.

Keep `IAlgorithm` independent and migrate Cycle, budgets, IslandModel and other affected meta-algorithms to the new operator collaborators without inventing `AlgorithmOperation`. Use native `AlgorithmRun`, typed scopes and algorithm advice; remove experimental host/contract adapters from the implementation path. Validate Fresh/Retained/Shared reuse, pausing, root-boundary cancellation, nested early stopping and disposal. Preserve all existing algorithm entry points and completion semantics.

**Gate**: full solution tests and public hosting/API/documentation examples pass, state-view/advice counters survive rebinding correctly, and no prototype bridge or nominal operator configuration interface is required by production.

### Package 5 — Analysis, integrations and closure

Finish cross-operation candidate clocks, typed observation factories, trace integration and public examples. `TraceAnalyzer` stays in `Analysis`; counting/timing/observation advice stays in `Inspection`. Preserve source reference identity, clock-before-observation ordering, shared-clock deduplication and result ownership.

Audit all Experimental, PythonInterop, samples, `python-samples`, Roslyn analyzers/code fixes and architecture rules. Earlier packages migrate affected callers; this package proves complete coverage. In particular, update analyzer metadata checks for the common operator marker, managed-state restrictions and the new preparation hooks, with meaningful analyzer tests. Validate Python demonstrations separately because the solution does not include them; list any unavailable external runtime/fixture as an unvalidated integration rather than a pass.

Reconcile developer guidelines, glossary cross-references, architecture guides and the backlog entries identified in section 7. Publish final source/API inventories, measured costs and remaining limits. Remove temporary migration mechanisms. Do not add generic settings methods, compatibility shims or source generators.

**Gate**: Release solution build/tests, documentation/API examples, formatting/style/analyzers, documentation build and package creation pass using repository commands. Report final net source change, adapter/public-surface counts and performance results. There is no predetermined 4,000-line deletion quota and no unsupported “100% extensible” or universal zero-allocation claim.

## 10. Evidence, reproduction and review coverage

### 10.1 Evidence already available

The retained prototype under `plans/prototypes/operator-role-slice` remains useful historical evidence. Its latest functional rerun passed 56 construction assertions, 174 assertions in 11 scenario groups, external consumer checks, eight expected compiler failures and the isolated state-family checks. It still models algorithms as a role, uses both markers and has a redundant setting-method layer. Its state-family resolver installs no advice. Its uniform choice and rejection of a second operation are not the revised target behavior.

Additional review probes under ignored `artifacts/operator-role-review` reproduced the original invariant-envelope regression and the invalid cast for unsupported general advice. Focused solution probes were then compiled as separate core/consumer assemblies. Thirteen assertions passed for the common marker/recommendations, configuration-owned preparation, multiple operation/state views over one owner, independent owner state, shared live controls, general prepared advice on external descriptors, metric rejection before construction, multi-operation reuse and covariant reuse in both state-view orders through the shipping resolver.

Those solution probes are local working evidence, not a retained integrated test suite. Their small owner cache does not implement ancestry, weak lifetimes, aspect ordering or cycle detection, and they make no claims about those invariants. Port their successful cases into Package 0's committed integrated fixtures; do not describe the full revised foundation as verified until that gate passes. No new performance measurements establish the budgets yet.

### 10.2 Reproducing the historical slice

From the repository root:

```powershell
dotnet build src/HeuristicLib/HeuristicLib.csproj --configuration Release --no-restore
pwsh -File ./plans/prototypes/operator-role-slice/run.ps1 -SkipPerformance
```

Omit `-SkipPerformance` to reproduce its illustrative timing probes. The runner prints SDK/reference-pack versions, HEAD and the backend SHA256. The retained text reports are snapshots and are never updated automatically. These commands validate the old slice, not Package 0.

The integrated runner must print the same provenance, fail on a failed assertion or wrong compiler diagnostic, and separate core, external-consumer, runtime-rejection, hosting and performance results. Retain its source, command and report together. Use the repository's SDK and sequential .NET validation strategy; do not infer a successful artifact from process exit alone.

### 10.3 Required coverage ledger

| Review problem or opportunity                                      | Selected solution                                                                            | Earliest mandatory evidence                          |
| ------------------------------------------------------------------ | -------------------------------------------------------------------------------------------- | ---------------------------------------------------- |
| Invariant controls envelope / lost state variance                  | Covariant bound interface, authored delegate preservation and view-specific constructors     | Package 0; production resolver tests in Package 1    |
| Prototype differs from target                                      | New integrated foundation; old evidence labeled accurately                                   | Package 0                                            |
| Lost per-item routing / missing weights / false parallelism        | Four retained routed forms, weighted whole-call sampler, sequential router semantics         | Package 0; complete production cutover in Package 3  |
| Recommendations lose the common interface                          | Generic contract inherits existing `IOperator`; retain `For` precedence and fresh instances  | Package 0; per-role cutover specs                    |
| General advice has no dispatch; public binding mislabeled internal | Public general/typed prepared-advice protocols and mandatory synchronous binding adapters    | Package 0; production dispatch tests in Package 1    |
| Removed metric marker loses applicability check                    | Explicit checked binding failure for selected unsupported operations                         | Package 0; budget/clock cases in each owning package |
| Performance guarantees exceed evidence                             | Correctly labeled old measurements and explicit cheap/numerical/allocation budgets           | Package 0, canary and final report                   |
| Incomplete role/default/caller inventory                           | Nine-operation matrix; all arities; per-role cross-project caller migration; defaults frozen | Package 0 inventory; Packages 2–5                    |
| Multiple operations split or reject shared state                   | One owner preparation with several explicit typed views                                      | Package 0; production ownership tests in Package 1   |
| Control testing lacks actual adaptation                            | Real EvolutionStrategy trajectories through stacked wrappers/advice                          | Package 0 and Package 2                              |
| Retry/RNG and hook failures unspecified                            | Non-transactional same-generator retry and deterministic failure/cleanup contract            | Package 0; production hook/retry tests               |
| Duplication/deletion numbers not reproducible                      | Baseline manifest, matched slice and full net-addition ledger                                | Package 0 and Package 5                              |
| Current rules contradict target                                    | Scoped rule reconciliation with implementation, preserving unreopened decisions              | Package 1 onward; final audit in Package 5           |

Use `test/README.md` for ownership of unit tests, API usage specs, published examples and scenarios. Plans may show proposed code; published examples must import their validated source. No package gate may be satisfied merely by retaining the old slice's passing assertion count.
