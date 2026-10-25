# Operator Parameterization and Role Unification

**Decision status**: The generic consuming contract remains the accepted direction. Standalone prototype checkpoints stopped after checkpoint 3 on 2026-10-16. Continue the remaining questions through actual core migrations, reusing `prototypes/operator-role-integrated` and moving useful checks into regular tests. Execution and resolution design simplification is required during Package 2 closure, before the Package 3 role cutovers; its concrete design was accepted on 2026-10-18 (Package 2a). An invocation foundation step before the role cutovers (Package 2b) and the Package 3 decisions were accepted on 2026-10-19. The fifth review decisions of 2026-10-23, one observation source and operation types without adapters, and its factory review, one `CreateExecutor` hook with execution state through the scope, are accepted.

**Implementation status**: Package 1 supplies the production resolver/advice foundation. Package 2 cuts mutation and evaluation over to `IOperator<TOperation, TCandidate>` across repository consumers, with generic naming/retry, shared counting/timing, adapted observations and real GA/EvolutionStrategy canaries. Package 2a replaces the construction carriers, delegate call wrappers and callback helpers with direct construction and struct invocation wrappers; all solution tests pass. The developer accepted the measured performance on 2026-10-18, completing Packages 2 and 2a. Package 2b is complete: its explicit latency/allocation budgets pass and the developer accepted the small stack-cost increase on 2026-10-20. Package 3 migrates creation, crossover, selection, refinement and replacement across repository consumers, with passing solution tests and performance gates recorded below. Its review decisions through the seventh review, including step 1 of the fifth review and factory review decisions, are implemented, and the developer accepted Package 3 on 2026-10-24 at commit `acd1aa06`. Step 2 of the factory review lands with Package 4, whose design the developer accepted on 2026-10-25; it is implemented with three deviations recorded there and awaits the developer's review.

**Target**: .NET 10 and C# 14, using the SDK selected by `global.json`.

**Baseline**: `a2643bc1005dc47611699cec603ba0106e4231df`. Review and solution probes were run on 2026-10-16 against its Release backend.

This plan replaces the earlier draft, feasibility discussion and design experiments as the specification for this change. Historical measurements remain evidence for their particular implementations. Published architecture documentation continues to describe shipping behavior until the relevant implementation package lands. Editing this plan does not authorize Git operations or begin production implementation.

**Naming decision (2026-10-16; accepted)**: An operation is the supported action; an operation descriptor such as `Mutation` identifies that action in a configuration contract. An executor is the callable runtime object implementing one or more operations. An invocation or call is one actual method call. Execution remains the activity, including execution state, time, concurrency and graph; the `Execution` namespace and run lifecycle responsibilities remain unchanged.

Apply this vocabulary throughout the implementation, API excerpts, tests, samples and documentation, including types introduced in later packages:

- Runtime interfaces and bases use executor names: `IExecutionNode` becomes `IExecutor`, `IMutatorExecution` becomes `IMutationExecutor`, and `IAlgorithmExecution` becomes `IAlgorithmExecutor`. Creation, evaluation, selection, refinement, replacement, termination and interception use the corresponding operation noun; crossover retains its existing noun.
- `PreparedOperator` becomes `OperatorExecutorFactory`; the checked non-generic `PreparedFactory` and `PreparedWrapper` become `ExecutorFactory` and `WrapperExecutorFactory`, alongside their existing generic delegate families. `PreparedOperatorAdvice` becomes `OperatorAdviceWrapperFactory`. The planned `PreparedComposite` follows the `CompositeExecutorFactory` family.
- `BoundExecution` and `IBoundExecution` become `ExecutorWithControls` and `IExecutorWithControls`, with `Executor` and `ControlProvider` properties. `IControls`, `ExecutionControls` and `NoControls` become `IExecutorControlProvider`, `ObjectControlProvider` and `EmptyExecutorControlProvider`.
- `IOperation` becomes `IOperationDescriptor`; `OperationBinding` becomes `OperationExecutorAdapter`. `TOperation`, the descriptor names and named operation methods such as `Mutate` remain unchanged.
- `ICallHooks` and `CallFrame` become `IInvocationCallbacks` and `InvocationNotifier`, used by the optional `CallbackInvocationWrapper`. Starting callbacks return invocation-local data, such as a timestamp. The intermediate `IOperationSuccessCallback` is removed by the call-wrapper consolidation below.
- Factory entry points use `CreateExecutorFactory` and `GetExecutorFactory`; advice uses `CreateWrapperFactory` and `GetWrapperFactory`; descriptors use `CreateExecutorAdapter`. These names change no initialization, sharing, callback or operation behavior.

The glossary records the current vocabulary. Historical measurement reports retain the names of the implementations they measured. No compatibility aliases or forwarding APIs are introduced.

**Invocation naming decision (2026-10-18; accepted)**: An operation is the supported action; an invocation is one actual use, such as invoking mutation through `Mutate`. Use `IInvocationWrapper` and `IInvocationCallbacks` for behavior around one invocation. Wrapper implementations use the behavior followed by `InvocationWrapper`, including `CallbackInvocationWrapper` and `CountingInvocationWrapper`; the adapter exposes `WrapInvocation`. `InvocationNotifier` notifies callbacks at invocation entry, success, failure and completion, keeping invocation data and preserving failures. Its name is accepted; its need as a separate type remains open for Package 2a. These are naming changes only; responsibilities, visibility and execution remain unchanged. Keep named executor methods and `OperationExecutorAdapter`; reconsider broader factory, adapter and helper responsibilities in Package 2a.

**Generic parameter ordering decision (2026-10-17; accepted)**: Put the operation descriptor first in types and generic methods. Keep the candidate/context group together: `TOperation, TCandidate, TSearchSpace, TProblem, TSearchState, ...`, omitting absent parameters and retaining managed `TState` last. Use `IOperator<Mutation, RealVector>` consistently across the current implementation and later packages. This changes parameter order only; operation and executor meanings remain unchanged. Historical prototype sources remain snapshots.

## 1. Problem, scope and success criteria

The concrete problem is duplicated composition and inspection machinery across nine operator roles. A generic configuration wrapper cannot implement arbitrary nominal interfaces such as `IMutator<TCandidate>`, so sharing only wrapper internals leaves nominal configuration shells in every role.

The selected solution changes consuming slots to `IOperator<TOperation, TCandidate>`, with descriptors such as `Mutation` and `Evaluation`. Generic `Named`, `Retry` and whole-call choice configurations then fit those slots directly. Role-specific operation methods and ordinary leaf authoring signatures remain: `Mutate`, `Evaluate`, `Cross`, and their single-item counterparts are useful domain APIs.

The migration must achieve all of the following:

- Reduce net production machinery after counting the new preparation, binding and advice infrastructure.
- Preserve candidate-only consuming contracts: search space, problem and search state do not become algorithm property type arguments.
- Preserve configuration-reference identity, ancestor reuse, child-local ownership, advice ordering and weak lifetime behavior.
- Preserve all nine roles, their authoring paths, recommendation flows, weights, routing granularity, defaults and operation semantics.
- Support an executor implementing several operations, with one shared factory and intentionally shared state.
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

public interface IOperator<TOperation, TCandidate> : IOperator
    where TOperation : IOperationDescriptor<TOperation>
{
    IOperatorContract<TCandidate>? CandidateContract =>
        this as IOperatorContract<TCandidate>;

    OperatorExecutorFactory CreateExecutorFactory();
}
```

`IOperator<TOperation, TCandidate>` is the single consuming contract for an operator slot. Remove nominal configuration interfaces such as `IMutator<TCandidate>` at their per-role cutover, migrating every in-repository caller. Keep role-specific executor interfaces such as `IMutationExecutor<...>`. `TCandidate` remains invariant.

`IAlgorithm<TCandidate>` and `IAlgorithm<TCandidate, TSearchState>` remain in `Algorithms`, including heterogeneous algorithm collections and their current hosting surface. An algorithm never implements `IOperator<AlgorithmOperation<...>, TCandidate>`. Synchronous operator composition therefore has no overload accepting an algorithm.

Keep `IRecommends<TOperator> where TOperator : class, IOperator`. A recommendation for mutation becomes `IRecommends<IOperator<Mutation, TCandidate>>`. Preserve explicit value > problem recommendation > search-space recommendation, decline-and-continue behavior, aggregated missing-parameter diagnostics, and a fresh configuration for each successful recommendation. `Create(...)` infers from required operators; `For(...)` consults recommendations. Optional collaborators continue to use their current defaults rather than acquiring new recommendation rules.

### 2.2 One preparation owner, several typed construction views

Separate shared execution-data initialization from selecting a construction function:

1. `CreateExecutorFactory()` validates immutable configuration and creates an `OperatorExecutorFactory` once for the configuration reference in its selected owner state. It receives no scope and no requesting run types. Allocate persistent counters, caches and other execution state here.
2. `OperatorExecutorFactory.GetExecutorFactory(adapter)` checks the requested operation and run signature and returns a checked `ExecutorFactory`. It may produce several typed construction views over the same prepared state. It must not initialize that state again or retain children from a previous binding.

```csharp
namespace HEAL.HeuristicLib.Execution;

public abstract class OperatorExecutorFactory
{
    public abstract ExecutorFactory GetExecutorFactory<
        TOperation, TCandidate, TSearchSpace, TProblem, TRunSearchState>(
        OperationExecutorAdapter<TOperation, TCandidate,
            TSearchSpace, TProblem, TRunSearchState> adapter)
        where TOperation : IOperationDescriptor<TOperation>
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TRunSearchState : class, ISearchState;
}
```

This is a deliberate change from the earlier plan. A configuration shared through two operation interfaces has one non-generic preparation method, not two independent state initializers. Stateless and stateful authoring bases hide the prepared carrier; a domain leaf still overrides only its role method and, when applicable, `CreateInitialState`.

Two smaller fixes were considered and are insufficient together without the ownership split. Making only the bound carrier covariant repairs simple compatible factory conversions but does not adapt invariant contextual wrapper delegates. Keying preparation directly by operation/run signature avoids the cast but initializes a shared operator more than once. The selected split keeps one logical owner and permits several checked constructors. It adds a preparation carrier and per-view cache entries; their allocation, complexity and net source cost must earn their place at Package 0.

Use existing `Fits` metadata for preflight compatibility checks. `GetExecutorFactory` is the authoritative check for the selected view and must reject an incompatible signature before invoking a construction delegate or resolving a child. Successfully completed owner preparation remains available if a later view or advice binding fails. Failed owner preparation is not cached. This separates configuration validation from run-signature binding; the corresponding operator factory rules must be updated with the production foundation.

An author of an explicit executor still writes one typed construction factory, using an authoring-base hook. The base's public preparation entry is sealed; there is no second public factory path that independently initializes state. A topology prepares persistent policy/state once, then supplies a run-generic binding recipe. Never put configurations, scopes, child executors or delegates bound to those children in ordinary framework-managed leaf state.

### 2.3 Operation descriptors and their executor contracts

Descriptors use domain nouns and provide their binding through a static declaration. No central enumeration discovers or registers operations.

```csharp
public interface IOperationDescriptor<TSelf> where TSelf : IOperationDescriptor<TSelf>
{
    static abstract OperationExecutorAdapter<TSelf, TCandidate,
        TSearchSpace, TProblem, TRunSearchState>
        CreateExecutorAdapter<TCandidate, TSearchSpace, TProblem, TRunSearchState>()
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TRunSearchState : class, ISearchState;
}
```

All nine descriptors and their semantics belong to the foundation specification:

| Descriptor     | Executor contract and operation                                            | Declared candidate work after success |
| -------------- | -------------------------------------------------------------------------- | ------------------------------------- |
| `Creation`     | `ICreationExecutor.Create(count, ...)`                                     | Returned candidate count              |
| `Crossover`    | `ICrossoverExecutor.Cross(parentGroups, ...)`                              | Returned offspring count              |
| `Mutation`     | `IMutationExecutor.Mutate(candidates, ...)`                                | Returned candidate count              |
| `Evaluation`   | `IEvaluationExecutor.Evaluate(candidates, ...)`                            | Input candidate count                 |
| `Selection`    | `ISelectionExecutor.Select(population, objective, count, ...)`             | Returned selected count               |
| `Refinement`   | `IRefinementExecutor.Refine(candidates, ...)`                              | Returned refined candidate count      |
| `Replacement`  | `IReplacementExecutor.Replace(previous, offspring, objective, count, ...)` | Returned survivor count               |
| `Termination`  | `ITerminationExecutor.IsTerminalState(state, ...)`                         | Unsupported                           |
| `Interception` | `IInterceptionExecutor.Transform(current, previous, ...)`                  | Unsupported                           |

Retain existing role operation signatures and result representations. In particular, this change does not resolve the existing requirements/glossary versus evaluator-output discrepancy: the baseline evaluator returns objective vectors. Changing evaluator output or candidate flow requires separate scope; do not smuggle it into role parameterization.

Executor interfaces keep meaningful contravariance in search space and problem. The termination executor remains contravariant in search state; interception remains invariant because it returns that state type. Invalid substitution fails at binding with a useful signature diagnostic.

There is no `ISynchronousOperation` marker because operation adapters implement the synchronous executor signatures below. There is no empty candidate-counting marker; metric applicability is explicitly checked at binding, as specified in section 5.

### 2.4 Public operation executor adapters

`OperationExecutorAdapter` is an advanced **public extension contract**, located with execution preparation machinery. It is hidden by authoring conveniences, but external descriptor/binding authors must be able to implement it. Its runtime counterpart is still called an executor; this is not a replacement `IExecutionBinding` vocabulary.

Its required surface is:

```csharp
public abstract Type ExecutorType { get; }
public abstract bool SupportsCandidateWork { get; }
public abstract WrapperExecutorFactory Identity();
public abstract ExecutorFactory Wrap(
    IOperator<TOperation, TCandidate> child, WrapperExecutorFactory wrapper);
public abstract ExecutorFactory Compose(
    IReadOnlyList<IOperator<TOperation, TCandidate>> children,
    CompositeExecutorFactory composite);
public abstract WrapperExecutorFactory WrapInvocation(IInvocationWrapper invocationWrapper);
```

`Forward(child)` is a concrete convenience implemented as `Wrap(child, Identity())`, not another abstract responsibility. The typed implementation base, which knows `TExecutor`, implements checked carriers, child resolution, identity and wrapping once. An operation author supplies one named-method call bridge and its candidate-work rule. Counting, lifecycle behavior and retry use that same bridge. Multi-child construction and whole-call choice belong to Package 3.

The public runtime extension contract is:

```csharp
public interface IInvocationWrapper
{
    Func<TArguments, TResult> Wrap<TArguments, TResult>(
        Func<TArguments, TResult> invokeChild,
        Func<TArguments, TResult, int>? countCandidateWork);
}
```

The operation adapter supplies the typed child function and calls Wrap once per constructed executor. The named method passes its inputs as a value tuple to the returned function; an argument-free operation uses `ValueTuple`. The optional count function defines candidate work from those inputs and the result; null means unsupported. Only consumers measuring candidate work invoke it, after success. Delegates and captures are created during construction and retained by that executor, never cached on the prepared owner. New bindings wrap their current children and advice. The contract covers entire synchronous calls; operation-specific transformations still use typed executors. It carries no child array, RNG slot or result carrier. Do not add a second universal `Invoke` method to domain executor interfaces. Erasure/checking remains confined to preparing or binding factories.

**Open call wrapping decision (2026-10-17; accepted)**: Use `IInvocationWrapper` and `WrapInvocation` as the stable primitive for operation-independent call behavior. Remove feature-specific retry hooks and the separate `Success`/`Hooks` adapter profiles, including their per-operation executors and `IOperationSuccessCallback`. Retry, call/candidate counting and duration use ordinary invocation wrappers. `CallbackInvocationWrapper` retains optional lifecycle callbacks and their shared failure handling above that primitive; callbacks carry no candidate counts. External wrappers and advice use the same contract for built-in and external operations without core changes. `SynchronousPolicy`/`ISynchronousPolicy`, `OperationCallResult` and `RetryPolicy` remain removed. Preserve named operation signatures, exception selection, attempt limits, controls, ownership and lifecycle semantics.

Keep checked wrapper carriers. A later simplification is acceptable only if it preserves owner preparation, variance, controls, external implementation and measured costs. Validate further composition requirements during the owning core migrations rather than extending standalone prototypes.

### 2.5 Typed resolution stays convenient

Retain thin `Resolve`, `ResolveOptional` and `TryResolve` overloads whose parameter is the corresponding closed operation contract. For example, a mutation overload takes `IOperator<Mutation, TCandidate>` and returns `IMutationExecutor<TCandidate, TSearchSpace, TProblem>`. A typed scope therefore still supports `typed.Resolve(mutator)` and the control-returning form without explicit executor type arguments. These adapters do not recreate nominal configuration interfaces or wrappers.

Supply one such family per built-in operation; an external operation author supplies its corresponding typed extension through the same binding bridge. Do not attempt to infer a hidden `TExecutor` solely from a constraint, or claim C# can obtain an associated executor type from `TOperation`. The shared resolver checks the prepared carrier, while each thin operation extension supplies its named executor type statically.

The existing three-argument typed scope remains usable for state-independent operations and binds their unused run-state argument as `ISearchState`. The four-argument scope passes the actual run state. State-dependent operations require an appropriate state view; binding a population-specific interceptor through an `ISearchState` view fails explicitly. No consumer must name run types on an operator property to make this work.

## 3. Resolution, variance and multi-operation state

### 3.1 Executors and their controls

Use a covariant read-only interface in factory signatures, retaining an ordinary sealed class as the carrier:

```csharp
public interface IExecutorWithControls<out TExecutor> : IExecutor
    where TExecutor : class, IExecutor
{
    TExecutor Executor { get; }
    IExecutorControlProvider ControlProvider { get; }
}

public sealed class ExecutorWithControls<TExecutor>(
    TExecutor executor, IExecutorControlProvider controlProvider) : IExecutorWithControls<TExecutor>
    where TExecutor : class, IExecutor
{
    public TExecutor Executor { get; } = executor;
    public IExecutorControlProvider ControlProvider { get; } = controlProvider;
}
```

Factories return `IExecutorWithControls<TExecutor>`, not the invariant concrete class. `ExecutorFactory.FromFactory<TAuthoredExecutor>` wraps at the **authored** executor type when the carrier is created. `GetFor<TRequestedExecutor>` checks/converts the stored delegate; it must not introduce a lambda closing over `TRequestedExecutor` that discards the original covariance. Wrapper constructors receive and return the bound interface for their exact requested view.

Covariance alone is insufficient for contextual wrappers and invariant wrapper-factory delegates. Use the owner/view separation below so the same source can bind another compatible state view without re-preparing state or casting an old wrapper constructor to an incompatible delegate type.

### 3.2 Owner records and construction-view factories

Adapt `ResolutionScope.State` internally. Preserve one owner record per configuration reference; do not change source identity to `(configuration, operation)` and thereby split state.

Each operator owner record contains:

- One successfully prepared `OperatorExecutorFactory`.
- Checked construction factories keyed by the complete view: candidate, operation, search space, problem, run search state and requested executor type.
- Advice preparation keyed weakly by advice reference, with one prepared advice per selected configuration and typed wrapper constructors separated by view.

View keys distinguish constructors, not logical executions or state owners. Cache no contextual executors. A stateless or ordinary stateful prepared leaf may itself retain one raw node, as it does today. View construction happens with the owner's state and the requesting scope's aspect chain. Resolve every declared child through that construction scope.

For a requested view, obtain/check the raw construction factory and prepare/bind its applicable advice constructors before invoking that raw factory. This makes a selected unsupported metric fail before constructing its child graph. Then construct the raw node, apply the already validated wrappers in the existing application order, and expose controls only after the complete chain succeeds. This preflight changes constructor/error ordering where necessary; it does not traverse or resolve absent sources to discover them.

Preserve current ancestor reuse and no hoisting: a child-first owner stays child-local; a parent-first owner is reused by descendants. `With` changes aspects without new state. Fresh/Retained/Shared activation continues to be owned by `ChildScopeSelector`; do not introduce a second scope abstraction or resolver-owned child-retention keys.

Cycle detection remains keyed by configuration reference across all views and related scopes. Changing operation cannot evade a construction cycle. Failed view binding/construction is retryable without discarding successful owner preparation. Advice state survives contextual rebuilding; original parent nodes keep their original children. Weak advice keys and owner-local caches must not keep discarded configurations, aspects or fresh states alive through global descriptor caches.

### 3.3 Multi-operation configurations

Support a configuration implementing several `IOperator<TOperation, TCandidate>` interfaces in one owner scope. It implements `CreateExecutorFactory()` once. A prepared raw executor may implement all of its named executor contracts, or an explicit prepared object may construct distinct operation nodes over the same private state.

The prepared object's binding implementation declares those views; there is no library registry of known operation combinations. Resolving the second operation must not reinitialize the shared state, regardless of resolution order. Operations with intentionally independent state use separate configuration objects. Contract queries and typed advice use the selected operation view, not an arbitrary first interface on the object.

The old slice's test expecting the second role to fail is a documented limitation of that slice, not an acceptance test for the new foundation. Replace it in the integrated prototype with successful shared-state tests, including an external executor implementing two operations.

### 3.4 Runtime controls

Keep operation-free lookup:

```csharp
public interface IExecutorControlProvider
{
    TControl? Get<TControl>() where TControl : class;
}
```

Controls refer to the prepared mutable data used by the callable executor. Query them only after the entire advised chain has bound successfully, and invoke work through the advised `Executor`.

- Transparent `Named`, retry of the same child, and inspection hooks forward exactly the child's control provider.
- General advice or wrappers that replace the effective executor must explicitly return its controls or `EmptyExecutorControlProvider`; preserving an unrelated child's controls is incorrect.
- Multi-child choice and pipelines expose `EmptyExecutorControlProvider` by default. A domain-specific composite may expose a deliberately authored aggregate control, but projection never recursively picks an arbitrary child.
- A configuration with its own controls decides their relationship to its child explicitly. Rebinding never resets a live value; independent owners start from the configured initial value.

The control suite covers stacked names/retry/advice; updates observed by later calls; explicit composite boundaries; external control interfaces; shared multi-operation state; parent reuse; and Fresh/Retained/Shared rebinding. The adaptive canary is the real `EvolutionStrategy`, whose executor reads and updates `IMutationStrengthControl`. A manual control update and a GA comparison remain useful tests but do not replace that canary.

## 4. Composition and deterministic routing

### 4.1 Generic whole-call topologies

`Named<TOperation, TCandidate>` adds metadata and forwards the child. It creates no extra callable operation node when no advice selects it. Wrapper and child remain separately selectable configurations, with reference identity preserved.

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

`Retry<TOperation, TCandidate>` catches only a directly thrown `RetryableOperationException`. `MaximumAttempts` includes the first call and must be at least one; `.Retry(2)` permits two calls. Exhaustion propagates the last original failure. Other exceptions, cancellation and `AggregateException` are not unwrapped or retried. This preserves the batch engine's failure shape.

Every attempt uses the same input values, child executor and RNG object. The wrapper makes no random draws and performs no fork, rewind or snapshot. Draws and state changes made by a failed attempt remain; there is no rollback of partial child effects or inspection data. An operator may signal `RetryableOperationException` only when another invocation under that contract is meaningful. An external operation does not become transaction-safe merely because its signature is synchronous.

Keep retry as configured behavior and counting/timing as advice. `Retry.Execute` is a runtime call helper; it does not prepare configurations or resolve children. No `AdvisedOperator` configuration or second persistence model is introduced for runtime advice.

## 5. Inspection, advice dispatch and metric applicability

### 5.1 A real protocol for general and typed operator advice

Add a public non-generic `OperatorAdvice : Advice<IOperator>` for operation-independent synchronous concerns. It prepares a `OperatorAdviceWrapperFactory` once per advice/configuration/owner. That factory has a run-generic `GetWrapperFactory` method receiving the typed source and `OperationExecutorAdapter`; it calls `adapter.WrapInvocation(...)`. For example:

```csharp
public abstract class OperatorAdvice : Advice<IOperator>
{
    public abstract OperatorAdviceWrapperFactory CreateWrapperFactory(IOperator source);
}

public abstract class OperatorAdviceWrapperFactory
{
    public abstract WrapperExecutorFactory GetWrapperFactory<
        TOperation, TCandidate, TSearchSpace, TProblem, TRunSearchState>(
        IOperator<TOperation, TCandidate> source,
        OperationExecutorAdapter<TOperation, TCandidate,
            TSearchSpace, TProblem, TRunSearchState> adapter)
        where TOperation : IOperationDescriptor<TOperation>
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TRunSearchState : class, ISearchState;
}
```

An external allocation tracker or call counter can implement this once and participate on an external operation through that operation's binding adapter. This replaces the previous incomplete `MemoryAllocationAspect` example, which omitted its essential dispatch mechanism.

Keep `OperatorAdvice<TOperation, TCandidate>` for typed observations and operation-specific callbacks. It likewise separates once-only preparation from run-specific wrapper binding, and its base supplies the internal dispatch bridge. Advice applicability includes the selected operation view: mutation-specific advice on a configuration implementing mutation and evaluation applies only to mutation. General `OperatorAdvice` can apply to both, sharing its intentionally common prepared state. Pointcuts still select configuration references; this adds no caller-path or occurrence selection model.

Resolution dispatches through the supported general/typed preparation protocols. Do not cast every matching advice to one closed generic advice base. A matching unsupported `Advice` subclass fails with `InvalidOperationException` naming its type, the selected operation and the supported extension bases; it never leaks `InvalidCastException`. The generic `Advice<TConfiguration>` base alone does not promise that it can wrap an arbitrary operation method.

Inspection factories receiving `IOperator<TOperation, TCandidate>` retain that operation filter in their advice, including candidate clocks and budgets. The same object may implement several operation interfaces, so a reference pointcut alone is insufficient to restrict its operation view. An explicitly general factory receiving only the non-generic `IOperator` may observe all its operation views. Reuse the hook implementation behind these adapters; do not manufacture another per-role counter implementation.

Preserve first-position advice deduplication, scope-layer order and typed source identity. Exact wrapper constructors are created/cached for the current view, while persistent advice state is prepared once. The wrapper-factory delegate is invariant; do not assume bound-view covariance makes old wrapper delegates interchangeable.

### 5.2 Candidate metrics fail explicitly during binding

Select **preparation/binding-time rejection** for unsupported candidate metrics. This is intentional, replacing the former compile-time marker constraint. Candidate/operation compatibility and applying synchronous topology extensions to algorithms remain compile-time checks; metric applicability follows the same checked binding boundary as hidden run-type compatibility.

`CountCandidates`, candidate clocks, count-based terminators and candidate budgets must require `binding.SupportsCandidateWork` before constructing an operator view. A selected unsupported operation throws `InvalidOperationException` with the operation, metric and configuration identified. It must never silently skip the metric or pretend that an unsupported operation processed zero candidates.

A binding with candidate work supplies a typed counting function according to section 2.3. Candidate-counting advice invokes it only after success. Call counting, duration and retry ignore it. An external scalar operation declares no candidate work; an external batch operation can supply its own counting function without a marker or library edit. Count totals describe work at the selected boundary, not unique candidates or hidden nested evaluations, and do not imply that the call can be split into independent items.

A budget validates applicability when its selected target is bound, before that target can execute. When the target is absent from the algorithm graph, preserve the current no-match behavior: no counter advances and no new source-discovery traversal is introduced. Include an explicit unsupported-target-in-graph case so a selected but unsupported metric cannot leave a running budget silently inert.

Replace the old `UNSUPPORTED_METRIC` negative compiler probe with a runtime binding-failure test. Keep compile-time rejection for mismatched candidate types, operation slots, observations, stateful algorithm types and unsupported concurrency setters where applicable. Removing `WithConcurrency` also requires replacing that setter-specific probe with checks for the final record API rather than preserving a removed method solely for its diagnostic.

### 5.3 Success, fault and cleanup semantics

Implement success handling through ordinary invocation wrappers. The optional callback wrapper uses an invocation-local token and shares cleanup/failure handling across operations. Counting records a completed successful boundary. Duration includes failed calls and is recorded during cleanup. Retry is separate configured behavior. Do not allocate observation records for simple counting/timing.

The hook failure contract is explicit:

1. If `OnStarting` throws, do not invoke the child; no token exists, so do not invoke `OnFailed` or `OnFinished`.
2. After `OnStarting` succeeds, invoke `OnFinished` exactly once. Invoke `OnFailed` only when the child/continuation throws, and `OnSucceeded` only when it returns successfully. A failure in `OnSucceeded` is a callback failure, not a second failure of the already completed child.
3. If one failure occurs, propagate it with its original stack. If the child and/or callbacks produce several failures, throw `AggregateException` containing the child failure first, then the success/fault callback failure, then cleanup failure in occurrence order. Never mask or discard a failure. Normal cancellation remains `OperationCanceledException` when callbacks succeed; an additional callback failure is a failure aggregate.
4. Inspection/state updates already completed are not rolled back. A callback failure fails the surrounding invocation even if an inner success counter already advanced.

Test those paths independently of the benchmark harness. For retry, inner call counting counts successful attempts, outer call counting counts successful logical invocations, inner duration includes failed attempts, and outer duration includes the entire retry operation. Wrapper placement makes those boundaries explicit.

### 5.4 Streaming algorithms retain their own advice

Keep algorithm advice separate. Measure awaited `MoveNextAsync()` work, including the final false or faulting advance, excluding consumer pauses. Observe each yielded state after interception and preserve the prepared iteration sequence across contextual rebinding. Get-enumerator, advance, cancellation and disposal failures remain distinct test cases; no synchronous retry or success hook is applied to enumerable creation.

Preserve `AlgorithmRun` cancellation and pause semantics: cancellation at root yield boundaries and disposal of a run stream can pause the retained iterator; that is different from disposing the underlying algorithm iterator. Early disposal of an actual iterator still disposes its child exactly once. Test natural completion, pause/resume, cancellation, nested early stopping and faulting disposal through the real host.

## 6. Authoring and configuration ergonomics

Retain stateless, stateful and explicit-executor authoring at all existing reduced arities for all nine roles. Preserve single-item creator, crossover, mutator, evaluator and refiner helpers wherever they exist in the baseline; do not create single-item forms for roles whose semantics are inherently batch/state based. `CrossParents` remains the single crossover override name.

`BatchExecution` continues to own item RNG forks, sequential/concurrent scheduling, order and exception behavior. A single-item base seals its batch method and retains `ExecutionConcurrency`. Use static callbacks with explicit state, without per-call closures. Ordinary stateful leaves prepare one fresh state per owner; composite executors get prepared state and newly resolved children separately.

**Batch modeling proposal (2026-10-17; open)**: Keep call wrapping independent of collection shape. Distinguish one operation call over a batch, independent item mapping, and chunking a call into sub-batches. `BatchExecution` remains shared low-level scheduling machinery; single-item authoring bases explicitly opt into mapping. Population-dependent selection/replacement and native batch evaluation keep their whole-call semantics. A future generic chunking/mapping wrapper would require an explicit capability defining splitting, result assembly, logical item/RNG identity and failure/retry boundaries; neither a list argument nor candidate-work counting establishes that capability. This proposal changes no batch APIs in this package.

Configurations use records and immutable value strategies. Required collaborators use required constructor parameters, following developer guidelines section 4.6; existing required-property algorithm construction remains supported and is not expanded into a new competing input path. Optional settings are `init` properties with defaults. Keep object initializers and `with`, and snapshot retained ordered collections into `ValueArray<T>`. Remove the prototype's redundant `With<Property>` layer rather than shipping it.

Constructors, static inference companions and fluent child-composition extensions must produce equivalent configurations. Do not claim that every `with` eliminates allocation: a single `with` batches setting changes into one record copy; copying a configuration creates a new source identity for resolution and pointcuts.

The complete production GA collaborator/default contract is retained:

- Required creator, crossover and mutator, with their slots converted at the owning role cutover.
- Non-null evaluator and selector defaults from `GeneticAlgorithmDefaults`.
- Optional refiner, terminator and inherited interceptor.
- Population size 100, maximum generations `int?` defaulting to 1000, mutation rate 0.1 and one elite, including current edge-case semantics and the meaning of a null generation limit.

Preserve `EvolutionStrategyDefaults` and every other algorithm's own defaults. Small prototype settings such as population size eight or five generations are explicit fixtures, never new library defaults. Do not rewrite the algorithms to match shortened illustrative snippets.

## 7. Extensibility and repository-rule reconciliation

An external package must be able to compile each of these stories independently against the library assembly as its owning migration lands:

1. A custom synchronous scalar operation, its binding and a candidate-focused configuration, using generic naming, retry, whole-call choice, call counting and duration.
2. A custom batch operation reporting candidate work and participating in candidate clocks/budgets; a scalar counterpart rejected for those metrics.
3. One general custom advice implementation applied to built-in and external operations, including termination and interception, without enumerating them.
4. A generic external `Twice` wrapper implementing `IInvocationWrapper`, applied unchanged to built-in and external operations, with controls and counting boundaries specified. Verify construction-time wrapping and contextual rebinding separately from repeated invocation.
5. One configuration and raw executor implementing two operations, shared once-only state, typed advice restricted to one operation and general advice spanning both.
6. A custom control interface and custom operation recommendation, with a fresh recommended configuration each time.

External descriptor authors must implement the binding protocol, including role adapters; extensibility is not a claim that a new method signature needs no adapter code. Leaf authors and algorithm users do not implement that protocol.

The following changes to current guidance are part of the production packages, not edits to shipping documentation in advance:

- Developer guidelines sections 4.3–4.4 and 4.7: replace the operator's single run-typed preparation factory with one public owner preparation entry plus checked construction-view binding. Keep child resolution in construction delegates, and keep algorithms' existing public factories.
- Sections 4.12 and 8.3–8.5: retain role configuration/executor symmetry, reduced arities and meaningful executor-interface variance; replace only nominal configuration membership with the generic consuming contract. No public per-role shims remain.
- Section 8.11: one adapter per operation/profile remains acceptable. Measure that matrix explicitly; do not introduce a second hidden matrix per aspect type or run-type combination in public APIs.
- Layering: operator consuming contracts/descriptors belong with operator composition contracts; preparation/binding carriers and dispatch belong with execution machinery. Inspection owns metrics and typed observations; `TraceAnalyzer` remains in `Analysis`. Hosting never acquires an Analysis dependency.
- The backlog rejection of typed universal invocation is reopened only for generic **configuration slots** and shared call-wrapper/advice adapters. Named domain operation methods remain. The nominal-membership premise changes because slots now accept the generic contract; C# has not gained structural interface membership.
- The rejection of static execution-role advice/factory objects is reopened narrowly for explicit configuration-owned prepared state and view adaptation. Add no static wrapping members to contravariant executor interfaces, and require the multi-operation fixture before accepting the additional carrier cost.
- The rejection of binding as public runtime vocabulary remains: `OperationExecutorAdapter` is the advanced preparation adapter, while the callable runtime object remains an executor. The rejection of source generation and advice-as-configured-wrapper systems also remains.

Record the measured outcome and the precise reopened premises in this plan and the backlog when production implementation is accepted. Update contributor/architecture documents to the implemented state in the corresponding package; do not silently leave contradictory rules for later authors.

## 8. Performance evidence and acceptance budgets

The retained `results.txt` contains illustrative timings for the original slice, not the revised foundation. Its cheap mutation paths reported raw 1.33 ns, named 1.27 ns, direct counting 8.68 ns, shared success counting 12.40 ns, lifecycle callbacks 16.31 ns and around-policy counting 29.56 ns per batch, with zero measured allocations on those warmed success paths.

The lifecycle case used `SuccessfulCallHooks` and `Unit`; it did **not** measure `DurationHooks`, a `long` timestamp token or `TimeProvider`. Retry was not measured. The success-hook path was approximately 43% slower than the direct counting comparison. The Gaussian results were noisy local samples, not evidence for universal negligible overhead. No historical result is a guarantee for the revised design.

**Gate decision (2026-10-18; accepted)**: The absolute cheap-path budgets and the zero extra allocation budgets below are the blocking performance gate for every remaining package. Numerical workloads, including the 2% comparison, are still measured and reported but no longer block a package. Call wrapping and resolution cost nanoseconds while real operators and evaluations cost microseconds to milliseconds; the cheap-path budgets are what protect lightweight operators.

Use these initial acceptance budgets for Package 0 and the production canary:

- Unadvised generic resolution and `Named` return the same callable node as their child where forwarding suffices; there is no added operation dispatch or invocation allocation.
- Extra warmed per-invocation allocations are 0 B for success counting, actual duration measurement, whole-call choice, a retry that succeeds on its first attempt and representative three-layer stacks. Measure incremental bytes relative to equivalent leaf work. Construction, result storage and failures/exception reporting are accounted for separately.
- Shared success counting may add at most 5 ns/call over an equivalent direct counter. Actual lifecycle/duration wrapping may add at most 10 ns/call over an equivalent direct duration wrapper. Shared whole-call choice/retry may add at most 25 ns/call over the corresponding direct implementation. These are absolute cheap-path deltas, not a promise of zero relative slowdown.
- On representative 32-candidate numerical mutation and evaluation batches, report the median end-to-end overhead relative to equivalent direct instrumentation; the original target was 2%. Cheap single-value/single-candidate cases use the absolute budgets above, and expensive work does not excuse a failed cheap-path budget.

Use matching counters, synchronization, `TimeProvider`, inputs and continuation behavior in each direct/shared comparison. Run Release, warm tiered execution, include several isolated process runs, report distribution and uncertainty, and retain environment/backend hashes. Use a benchmark harness with allocation diagnostics and an anti-elision sink. If uncertainty straddles a budget, improve the measurement before claiming success. Do not repeatedly broaden or rerun checks after a stable result unless a change or unresolved concern justifies it.

Measure actual duration hooks, weighted choice, first-success retry, failure-then-success retry latency, stacked advice, external scalar work, refinement/replacement, and preparation/rebinding costs separately. Failure paths need no zero-allocation claim. Report source reduction and runtime cost together. A failed blocking budget blocks broad migration; it is not waived by referring to a slower workload or changing the threshold after seeing a result.

## 9. Migration packages and coexistence

Packages are sequential and independently reviewable. Package 0 is retained evidence, with its unfinished questions transferred to Packages 1–5 rather than another standalone gate. Earlier references to completing Package 0 before core integration are superseded by this execution decision. A production package is complete only when affected projects compile, its owned behavior tests pass, and its public API/examples and relevant documentation agree.

### Package 0 — Retained integrated evidence

The three checkpoints under `prototypes/operator-role-integrated` remain historical evidence. Do not extend the checkpoint/report series. Ownership, advice, lifecycle and external-extension fixtures move into regular tests as the corresponding core code lands. Validate the remaining retry, choice, routing, canary, hosting, performance and size questions during their actual migrations; section 8 still gates broader consumer cutover.

### Package 1 — Production foundation and resolver ownership

Add generic operator contracts/descriptors beside the existing per-role contracts, checked prepared carriers, covariant bound views, owner/view caches, control providers and advice dispatch. This package does not remove a nominal role until its consumers cut over. Keep the common non-generic operator marker unchanged.

The legacy resolution path and the new path must use the same scope ancestry, owner-selection rules, aspect layers and configuration-reference cycle guard. Their coexistence is a temporary migration detail, not a second public ownership model. A configuration is prepared through its declared path; internal adaptation must reuse its selected owner rather than create an independent state. Cover mixed old/new child graphs explicitly before the canary.

Implemented in the working tree: nominal role resolution adapts configurations declaring the matching generic contract through their prepared owner. Arbitrary low-level factory callbacks cannot switch an existing owner's preparation path. Regular tests cover mixed graphs in both directions, ownership/variance/controls, weak advice and owner lifetimes, all nine descriptors' success/fault/work semantics, metric preflight, lifecycle failure ordering and external public extensions. Generic forwarding and representative authoring helpers remain test fixtures until their owning role cutovers.

Validation on 2026-10-16: Release solution build and all 3,073 solution tests passed, including the external API fixtures. Documentation build, changed Markdown formatting, whole-solution whitespace/style checks and error-level analyzer verification passed. Performance and source-reduction acceptance remain with the consumer cutovers.

**Gate**: core unit tests, focused resolver/control/variance/multi-operation/weak-lifetime suites, external API fixtures and full solution build pass. Update architecture rules and tests for the implemented foundation. No public compatibility aliases or obsolete shims are introduced.

### Package 2 — Mutation/evaluation cutover and real algorithm canaries

Cut over mutation and evaluation across **all** in-repository consumers, including encoding-specific operators, problem-specific leaves, GA, EvolutionStrategy, other algorithms, recommendations, advice/observations, Experimental, PythonInterop, tests, samples and imported documentation examples. Migrate stateless/stateful/explicit bases and the existing single-item helpers, generic naming and retry. Remove those two nominal configuration interfaces and their redundant shells only after their complete caller inventory is migrated.

During this package a GA may have new mutation/evaluation slots and still have legacy creator/crossover/selector/refiner/terminator/interceptor slots. This is intentional per-slot coexistence, not two contracts for the same slot. Generic wrappers are promised only for the roles already cut over; no temporary public nominal wrapper shim is needed. Migrate a role across projects in one package rather than breaking callers until Package 5.

Preserve seeded GA results generation by generation, with defaults and edge cases covered separately from small explicit fixtures. Use `EvolutionStrategy` to prove actual per-generation mutation-strength adaptation through `Retry(...).Named(...).Named(...)` plus inspection. Compare raw and transparent-wrapped control trajectories and outputs; test parent/child and retained contexts.

Implemented on 2026-10-17: mutation/evaluation consumers, authoring bases, naming/retry, counting/timing and observations use the generic contract. All nine operation adapters have one call bridge, with optional candidate-work projection; separate success/lifecycle executors are removed. Regular tests cover external `Twice` on built-in/external operations, construction-time wrapping and contextual child advice, retry boundaries, controls and EvolutionStrategy outputs/strength through stacked wrappers in retained and ancestor-owned contexts. GA histories match `f576e14a`, including recommended operators with default settings and separate rate/limit cases. Historical prototype sources are unchanged.

Validation: Release solution build and all 3,121 solution tests pass, including external API usage and the migrated published advice example. Tests cover same-child retry across all nine descriptors, same arguments/RNG, exhaustion, nested wrappers, live controls, operation/timer failure ordering and call counting/timing that never reads candidate counts. Documentation build, whitespace/style verification, error-level analyzer verification, changed Markdown formatting and package creation pass.

**Performance gate, as measured for Package 2 (superseded by the Package 2a acceptance below).** Three fresh isolated alternating paired runs retain the original workloads and direct references. Consolidated counting/duration/retry add approximately 1.9/6.2/1.8 ns on cheap success paths, within their absolute budgets, with no extra warmed success allocation. Numerical mutation counting/duration/stack meet 2%; mutation retry's 2.2% estimate has uncertainty spanning the budget. Evaluation counting/duration/stack/retry have paired median overheads of 7.6%/9.4%/22.3%/12.1%, all above 2%. The simpler extension contract does not resolve performance acceptance. Fix the failed/uncertain budgets before Package 3. Adapted harness source, distributions, construction/failure/scalar measurements and raw provenance remain local under `artifacts/operator-role-package2`.

Source cost across `src` and `analyzers`: Package 2 is net -409 C# lines against `f576e14a`, including all nine call-wrapper bridges, the public factory contract and optional lifecycle helper. Packages 1 and 2 remain net +1,656 against `94641d41`; overall source reduction is not yet established.

**Gate**: core, API usage, affected Experimental/PythonInterop and documentation validation pass, affected scenario canaries pass, the solution builds, section 8 budgets pass, and source/extension-cost evidence still justifies continuing. Full solution tests are required for this first substantial consumer cutover.

### Package 2a — Execution and resolution design simplification

**Decision (2026-10-18; need and scope accepted)**: Reduce construction machinery exposed to authors, invocation indirections and overlapping execution/resolution abstractions. Better names or authoring helpers alone do not satisfy this step; improve the underlying architecture and call path.

**System-wide scope; motivating example**: The duration advice illustrates a recurring design problem. Its general form requires an inner `WrapperFactory` whose `GetWrapperFactory` method has five generic parameters, a correspondingly parameterized `OperationExecutorAdapter`, a `CallbackInvocationWrapper` and a separate callback implementation to express a small timing concern. The production duration advice follows the same construction structure. Use this as evidence of excessive concepts, authored plumbing and invocation indirections across the system, not as a request to spot-fix duration advice. Review the complete execution/resolution design and compare coherent alternatives across leaves, stateful compositions, wrappers, advice and external operations. A shorter duration implementation alone cannot establish that the architecture is simpler.

**Placement**: Perform this as part of closing Package 2, before Package 3 spreads the design to more roles. Prefer starting after the coherent current call-wrapper implementation is validated and committed as a comparison baseline; bounded prerequisite fixes may come first. Package 2a need not wait for Package 2's failed performance gate to pass, and does not waive that gate.

- Simplify ordinary operator, wrapper and advice authoring. Keep signature binding and checked construction mechanics inside the library wherever authors do not need to control them.
- Shorten actual invocation chains and reduce redundant runtime wrappers and named-method/delegate conversions. Preserve configuration boundaries, advice order, retry/counting semantics and controls.
- Consolidate duplicated resolution and advice construction logic while preserving configuration-reference ownership, shared state, contextual children, variance, failure/retry behavior and weak advice lifetimes. Keep streaming algorithm contracts independent.
- Remove unnecessary public implementation types and overlapping responsibilities. Review callback helpers, control-provider implementations and identity forwarding; keep batch scheduling separate from the common call-wrapping contract.

**Design decision (2026-10-18; accepted)**: Construct executors directly and compile each call wrapper into one executor per layer. The rejected alternative trimmed the callback helpers and advice wrapper factories but kept delegate call wrappers, five-parameter adapters, checked carriers, view caches and executor/control pairs; it left the delegate call chain and most construction concepts in place. This decision supersedes the conflicting contract excerpts and rules in sections 2.2, 2.4, 3.1, 3.2 (view-keyed caches and advice preparation), 3.4 (provider objects), 5.1 and 5.3 (callback failure aggregation); those sections remain as history.

- **Construction.** `OperatorExecutorFactory` has one abstract method, `CreateExecutor<TOperation, TCandidate, TSearchSpace, TProblem, TSearchState>(ResolutionScope<TCandidate, TSearchSpace, TProblem, TSearchState> scope)`, returning `IExecutor`. `OperatorExecutorFactory<TOperation, TCandidate>` narrows it to the run types. Static helpers cover the common shapes: `FromFactory` for fixed-signature leaves, which rejects an incompatible executor type before invoking its factory; `Forward(child)`; and `WrapInvocation(child, wrapper)`. The resolver checks each returned executor type. The non-generic `ExecutorFactory` and `WrapperExecutorFactory` carriers, `GetExecutorFactory`/`GetFor` and the view-keyed factory and advice caches are removed; each resolution constructs again. The delegates `ExecutorFactory<TExecutor>`, `WrapperExecutorFactory<TExecutor>` and `CompositeExecutorFactory<TExecutor>` remain for algorithms, fixed-signature leaves and the remaining nominal roles.
- **Call wrapping.** `IInvocationWrapper.Invoke<TInvocation, TResult>(TInvocation invocation)` receives a struct `IInvocation<TResult>`, whose `Proceed()` calls the wrapped operation method with the original arguments and may be called repeatedly, and whose `CountCandidateWork(result)` supplies the operation's counting rule. Wrappers are structs (`where TWrapper : struct, IInvocationWrapper`), so a layer is one specialized executor without delegates, closures or argument tuples. `IInvocationCallbacks`, `InvocationNotifier`, `CallbackInvocationWrapper`, `Retry.Execute` and the callback failure-aggregation rule are removed; wrappers use ordinary `try`/`catch`/`finally`.
- **Operation adapters.** Descriptors return the non-generic `OperationExecutorAdapter`: executor type, `SupportsCandidateWork`, `RequireCandidateWork` and `WrapInvocation(executor, wrapper)`. Operation authors derive from `OperationExecutorAdapter<TExecutor>` and supply `CreateWrappedExecutor<TWrapper>`, an invocation struct and a wrapping executor. The five- and six-parameter adapter bases are removed. `ResolutionScope.ResolveOperator<TOperation, TCandidate, TSearchSpace, TProblem, TSearchState>(source)` returns `IExecutor`; typed resolution extensions cast it once.
- **Controls.** `IExecutorWithControls`, `ExecutorWithControls`, `ObjectControlProvider` and `EmptyExecutorControlProvider` are removed. An executor's controls are the executor itself unless it implements `IExecutorControlProvider` to forward another executor's controls; `ExecutorControls.Find<TControl>(executor)` follows that forwarding. Generated invocation wrappers, `Named` (which returns its child's executor), `Retry` and the built-in observation executors forward; compositions expose their own.
- **Advice.** `OperatorAdvice` applies to every operation of its selected operators through `Check(operation, source)` and `Wrap(executor, operation, source)`. `OperatorAdvice<TOperation, TCandidate>` applies to one operation through run-generic `Check` and `Wrap` methods receiving `IExecutor`. `MutatorAdvice<TCandidate>` and `EvaluatorAdvice<TCandidate>` derive from it and expose typed `Check<TSearchSpace, TProblem>` and `Wrap<TSearchSpace, TProblem>` without casts; the remaining roles receive typed advice bases at their cutover. Resolution calls every matching `Check` before constructing the raw executor, so an unsupported metric or observation signature still fails before building, then calls `Wrap` in application order. Both `OperatorAdviceWrapperFactory` types and the per-owner advice preparation are removed.
- **Dropped capability.** Operator advice no longer prepares data per selected operator and owner state. The consequences are accepted as uncommon: an advice selecting several operators keeps its own per-operator table; reusing one advice across independent roots no longer restarts its data, so per-run data needs one advice per run; advice data does not follow Fresh or Retained child scopes; advice-driven adaptive operator selection would need its own keyed data and fits better as a configured operator. Algorithm advice keeps its join-point preparation. Operator budgets are unaffected because their counters belong to the budget's prepared state.
- **Unchanged.** Owner preparation by configuration reference, ancestry and reuse, the cycle guard, advice order and first-position deduplication, named executor methods, executor-interface variance, the remaining nominal roles, algorithm contracts and batch scheduling. Candidate-work applicability remains a property of the operation; whether a call can be split into independent items remains with the open batch-modeling proposal in section 6.

Implementation steps:

1. Replace the execution core: invocation contracts, adapters, `OperatorExecutorFactory` helpers, control lookup and the generic resolver path; remove the carriers, pairs, callbacks and caches.
2. Migrate all nine descriptor adapters and their resolution extensions, including the external-operation fixtures.
3. Migrate operator bases, `Named`, `Retry`, mutation and evaluation compositions, and Experimental and PythonInterop callers.
4. Migrate the advice bases, counting, duration and observation advice, and Experimental advice; add `MutatorAdvice` and `EvaluatorAdvice`.
5. Migrate tests, API usage specs, documentation specs and the executor-factory analyzer. Replace callback lifecycle tests with failure-propagation, retry-boundary, early-rejection and control-forwarding tests.
6. Update architecture pages, developer guidelines, the glossary, the extension guides and the backlog to the implemented state.
7. Validate the core, API usage, documentation and algorithm canaries, then the Release solution build and tests and the formatting and analyzer checks. Measure section 8's workloads against the unchanged budgets and update the source-cost ledger; keep raw evidence under ignored `artifacts/`.

Implemented on 2026-10-18. Public execution machinery shrinks from 19 types to 10, plus the two typed role advice bases. An advice layer is one interface call into a wrapping executor compiled for its struct wrapper; the previous path made three indirect calls and packed the arguments into a tuple. A general advice such as duration measurement is one `Wrap` override and a struct. A stateful composition overrides one `CreateExecutor` that receives a typed scope. Source cost against `6310d1c2`: production −174 lines (−266 in changed files, +92 in three new files), tests −197 and documentation +10. Raw evidence and adapted harnesses are under `artifacts/operator-role-package2a`.

Performance, from three alternating isolated launches per build with Package 2's harness and an exported `6310d1c2` build for comparison:

- Cheap paths add −0.2/+0.3/−0.6 ns for counting, duration and retry over direct wrappers (previously +1.7/+5.9/+1.7 ns) and +7.4 ns for the three-layer stack (previously +12.4 ns). Warmed success paths allocate nothing extra.
- Numerical mutation counting, duration, stack and retry are within 2%: −0.0%, −0.4%, +0.5% and −0.3%.
- Numerical evaluation against the original non-generic direct wrappers: counting +1.2% (95% interval up to +3.1%), duration +3.7%, stack +14.5%, retry −3.1%. With the bound evaluator: +0.7% (up to +2.3%), +1.1%, +13.7% and −1.9%. The old design measured +5.2%, +6.2%, +14.2% and +10.5% in the same session.
- Against generic direct wrappers, the harness's `--generic-direct-paired` mode with the shape any library wrapper over run types must have, all four evaluation cases are within 2% with intervals below it, in both designs (stack +0.2%). The remaining evaluation gap comes from shared generic code compared with exact-type hand-written wrappers, not from invocation transitions.
- The failure-then-success retry path costs +709 ns over direct (previously +370 ns) and 464 B (previously 296 B). Construction is cheaper: a fresh named root takes 1,080 ns (previously 2,545 ns) and contextual rebinding 246 ns (previously 336 ns).

**Performance decision (2026-10-18; accepted)**: The developer accepted these results, closing Packages 2 and 2a. Call wrapping and resolution cost nanoseconds, while real operators and especially evaluations cost microseconds to milliseconds; the cheap-path absolute and zero-allocation budgets, which protect lightweight operators, pass. The remaining relative gap for numerical evaluation against exact-type hand-written wrappers is accepted.

**Gate**: Demonstrate fewer exposed construction concepts and fewer unnecessary invocation transitions, with a concise before/after explanation and updated source/extension-cost evidence. Preserve external operation and wrapper extensibility, named executor methods and existing ownership/control guarantees. Validate affected core, API usage, documentation and algorithm canaries, plus the Release solution build/tests. Reuse section 8's workloads and unchanged performance/allocation budgets; no standalone prototype or checkpoint-report series. Complete this step and Package 2 acceptance before Package 3.

### Package 2b — Invocation foundation

**Decision (2026-10-19; accepted)**: Settle the invocation contract before Package 3 spreads it to more roles. Every operation's executor method ends with the call's random number generator, search space and problem. Generic wrappers can read and replace the random number generator and run a call on another executor of the same operation, which makes whole-call choice generic. This supersedes § 2.4's exclusion of an RNG slot and child choice from call wrapping, § 4.1's `ChooseCall` name and the selection and replacement rows of § 4.2's table; those sections remain as history.

- **Shared call arguments.** § 8.7's parameter order becomes mandatory for operations: primary inputs first, then the random number generator, search space and problem. Termination gains a random number generator (below), and external operations follow the same rule. Executors keep their named methods and full parameter lists, so no context carrier is introduced (see the backlog's typed operator invocation entry). Generic wrappers access only the random number generator. They do not know the run's search-space and problem types, and an executor accepts only the run's exact problem type, so a generic wrapper cannot substitute or wrap those arguments; typed role advice already sees and can replace every argument.
- **Invocation members.** `IInvocation<TResult>` exposes `Random`, `Proceed()` and `ProceedWith(IExecutor? executor = null, IRandomNumberGenerator? random = null)`. A null argument keeps the original executor or random number generator; neither can be absent, so null only means "not supplied" (developer guidelines § 8.1). `ProceedWith` accepts any combination: `ProceedWith(random: fork)`, `ProceedWith(child)` or `ProceedWith(child, fork)`. `Proceed()` remains a separately implemented member because a default interface implementation would box the invocation on every call. Separate non-nullable overloads were considered; they add a member for every operation author.
- **Candidate work.** `SupportsCandidateWork` defaults to false, and `CountCandidateWork` has a default body throwing `NotSupportedException`. Operations without candidate work implement neither. Operations with candidate work still state the flag and the count, as `Stream.CanSeek` and `Seek` do; tests covering all nine operations catch a mismatch. The backlog records the rejected alternatives.
- **Controls.** A wrapping executor forwards its wrapped executor's controls unless its wrapper implements `IExecutorControlProvider`, which then supplies them. A choice supplies none.
- **Whole-call choice.** `ChoosePerCall<TOperation, TCandidate>` derives from `MultiOperator<TOperation, TCandidate>` and snapshots `Weights`. Each construction resolves the children and prepares a `WeightedItemSampler` over them. Each call samples one child with the call's random number generator and proceeds on it with the same generator, reproducing `ChooseOneSelector`'s draws. Preparation rejects an empty child collection or a nonempty weight collection of mismatched length. Package 3 deletes `ChooseOneSelector` and `ChooseOneReplacer` in its favor. It also provides whole-call choice for mutation, evaluation and external operations, distinct from per-candidate routing.
- **Termination random number generator.** `ITerminationExecutor.IsTerminalState(state, random, searchSpace, problem)`. Ordinary authoring arities receive `random` after the state; the input-free `Terminator<TCandidate>` form stays input-free. Iterative algorithms pass the iteration's generator, which the interceptor also receives, through their termination hook. `TerminatedBy` passes a fork of the run's generator under a dedicated key, so a drawing terminator never receives the stream of an inner iteration's fork. Existing terminators draw nothing, so seeded results are unchanged. Package 4 keeps this executor signature.
- **Unchanged.** Named executor methods, executor-interface variance, adapters per operation, `Retry`'s same-generator attempts, advice order and ownership.

Implementation steps:

1. Change the invocation contract, candidate-work defaults and control rule; migrate the nine adapters, the external-operation fixtures and existing wrappers.
2. Add the termination random number generator across executor interfaces, authoring bases, leaves, algorithm hooks, `TerminatedBy`, inspection, Experimental, tests and documentation.
3. Add `ChoosePerCall` with tests for weights, random draws, controls and external operations.
4. Update the architecture pages, developer guidelines § 4.19, § 8.7 and § 8.11, the glossary and the composition guide.
5. Validate in the order `AGENTS.md` gives. Measure `ChoosePerCall` against a direct whole-call chooser with the existing harness.

**Gate**: Release solution build and tests pass, seeded GA histories are unchanged, `ChoosePerCall` meets § 8's whole-call choice budget with no extra warmed allocation, and existing wrapper paths keep their measured cost.

Implemented on 2026-10-19. Each built-in adapter's invocation keeps its executor and random number generator in fields, so `ProceedWith` can fall back to them; `Proceed` stays a direct member. Termination, interception and the external test operations no longer declare candidate work. `ExecutorControls.Find<TControl, TWrapper>(executor, wrapper)` gives the nine wrapping executors their controls. Iterative algorithms pass the iteration's generator to their termination hook as its last parameter, after the problem, like `ExecuteStep`; `TerminatedBy` forks the run's generator under the key `ulong.MaxValue`, then by yielded-state index. `ChoosePerCall` resolves its children on each construction and wraps the first through the operation adapter with a file-local wrapper. Known flaw: a drawing terminator receives different streams in an algorithm's `Terminator` setting and through `TerminatedBy`; the backlog records it for a general decision on stable operator streams. The cast in `ProceedWith` stays; the backlog records the rejected alternatives. Source cost against `2310e7c4`: production +240/−71 lines (invocation contract, control helper and nine adapters +113/−31; termination generator +60/−40; `ChoosePerCall` +67 in a new file), tests +679/−135 and documentation +30/−16.

Validation on 2026-10-19: Release solution build and all 3,167 solution tests pass, including the seeded GA generation baselines. Whitespace, style and error-level analyzer verification, the documentation build and changed Markdown formatting pass. The build used SDK 10.0.113 with the Roslyn 5.9 compiler, which the repository analyzers require. Performance is not measured; the § 8 whole-call choice and existing wrapper budgets remain to be run locally with the existing harness.

**Performance measurements (2026-10-20)**: Measured `9572bd41` against Package 2a reference `8182d4fb`; the production, analyzer and test sources at `11785219` match the measured current revision. The existing cheap counting/duration/first-success retry paired comparisons are within their +5/+10/+25 ns budgets, with 0 B extra warmed allocation. Final extended-warmup whole-call choice adds 1.37 ns for two children and 2.16 ns for eight; conservative upper mean-delta envelopes are 1.97 and 2.58 ns, both below +25 ns, with 0 B extra allocation. The final BDN confirmation used three launches, 40 warmups and 15 measured iterations per launch; the initial six-warmup timings were excluded because of late tiering changes. Numerical comparisons remain report-only under § 8's accepted decision.

**Remaining retention assessment**: The production three-layer stack's overhead over its direct reference is 7.78 ns, versus 6.31 ns for the Package 2a baseline: a 1.47 ns point-estimate increase, with 0 B extra allocation in both. The explicit latency/allocation budgets pass, but this does not establish exact cost retention. Recommendation: accept this small increase and close Package 2b; that acceptance has not yet been given. No further measurement run is pending. Detailed distributions, provenance, reproduction scripts and `REPORT.md` remain local under ignored `artifacts/operator-role-package2b/`; the obsolete pause handover was removed after preserving these outcomes here.

**Follow-up decisions (2026-10-20; accepted)**: Keep `TWrapper` specialization; the measured interface-held alternative costs about 4 ns more per cheap counted call without simplifying ordinary call sites. Keep the nine built-in invocation structs' direct `Proceed()` implementations. Forwarding `Proceed() => ProceedWith()` was briefly applied, then reverted at the developer's request because the direct operation calls are clearer. Both methods and their existing contracts remain. These decisions do not close the remaining retention assessment or start Package 3.

**Retention decision (2026-10-20; accepted)**: The developer accepted the recorded 1.47 ns three-layer stack increase, with 0 B extra allocation and passing explicit latency/allocation budgets, and authorized Package 3 implementation. This closes Package 2b's remaining retention assessment. The completed measurements are not rerun for this decision.

### Package 3 — Remaining candidate operations and routing

Cut over creation, crossover, selection, refinement and replacement, including all public slots, recommendations and cross-project callers. Implement complete authoring ladders and observation/counting/timing adapters. Preserve operation-specific pipelines and the four per-item routing forms in section 4; migrate whole-call selector/replacer choice to generic `ChoosePerCall` with weights.

Update construction companions and rate helpers, and remove the migrated nominal interfaces and redundant topologies. Retain real domain-specific composites such as transformed creators and refiner-backed evaluators; generic signatures do not replace their distinct responsibilities.

**Decisions (2026-10-19; accepted)**:

- Whole-call choice uses Package 2b's `ChoosePerCall`. `ChooseOneSelector` and `ChooseOneReplacer` are deleted rather than migrated.
- The per-item forms take § 4.2's names: `ChoosePerCandidateCreator`, `ChoosePerParentGroupCrossover`, `ChoosePerCandidateMutator` (renaming the already migrated `ChooseOneMutator`) and `ChoosePerCandidateRefiner`. They keep `WeightedRouter`, and rate helpers return the renamed types.
- `CreatorAdvice`, `CrossoverAdvice`, `SelectorAdvice`, `RefinerAdvice` and `ReplacerAdvice` derive from `OperatorAdvice<TOperation, TCandidate>` with typed `Check` and `Wrap`, matching `MutatorAdvice`. The per-role counting and duration advice and value strategies give way to the generic ones, as for mutation.
- Performance evidence: counting, duration, retry and the three-layer stack on refinement and replacement, paired against direct wrappers, with no extra warmed allocation. Other workloads are reported only.
- Remaining limits: general `OperatorAdvice` selected through `Pointcut.OfType<IOperator>()` still throws `InvalidCastException` on termination and interception until Package 4. The failure-then-success retry latency is not investigated.

**Gate**: all affected suites/API examples/docs compile and pass; seeded weighted/routed behavior, contracts, cardinality and exception semantics match the baseline. The deleted-file inventory has a behavior-preserving replacement for every public composition, and no remaining caller depends on the removed interfaces.

Implemented on 2026-10-20 from `operator-unification` at `7a8ab0729537b94884d7b40bbdb22c00229af5ad`. Package 2b's retention decision above was explicitly accepted before production edits. The pre-edit symbol/family inventory covered 290 files and classified 36 baseline paths for deletion, splitting or renaming; API validation also identified and migrated one reflection-only slot assertion. The complete caller/deletion manifest and reference audit remain local under ignored `artifacts/operator-role-package3/`.

Creation, crossover, selection, refinement and replacement now consume `IOperator<TOperation, TCandidate>` throughout core, Experimental, PythonInterop, recommendations, algorithm slots, construction companions, inspection, budgets, observations, tests and documentation. All existing explicit, stateless, stateful and applicable single-item authoring arities remain. Fixed-signature leaves use `CreateFactory`; run-generic composites prepare an `OperatorExecutorFactory` once and resolve children through each construction's supplied typed scope. `PredefinedCandidatesCreator` retains its prepared cursor while rebinding its fallback. Transformed creators/crossovers, selection policies, refinement pipelines and refinement-backed evaluation retain their distinct operation logic.

The deletion/replacement inventory is:

| Baseline surface                                                                                   | Replacement or retained behavior                                                                                                                                                                                                           |
| -------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| `ICreator`, `ICrossover`, `ISelector`, `IRefiner`, `IReplacer`                                     | `IOperator<Creation, ...>`, `IOperator<Crossover, ...>`, `IOperator<Selection, ...>`, `IOperator<Refinement, ...>`, `IOperator<Replacement, ...>`; named executor interfaces and their variance remain in the corresponding executor files |
| Five nominal `Named*` configurations and companions                                                | Generic `Named` and `.Named(...)`                                                                                                                                                                                                          |
| Five `Wrapping*` and five `Multi*` configuration shells                                            | `WrappingOperator` and `MultiOperator`; the operation-specific wrapping/multi executor bases remain in matching files                                                                                                                      |
| Five counting and five duration advice files, including their role value strategies and extensions | `CountingOperatorAdvice`, `DurationMeasuringOperatorAdvice`, `OperatorCounting`, `OperatorDurationMeasuring` and generic inspection extensions                                                                                             |
| Nominal resolver and advice declarations in the five interface files                               | Existing operation-resolution extensions, completed with untyped overloads; typed `CreatorAdvice`, `CrossoverAdvice`, `SelectorAdvice`, `RefinerAdvice`, `ReplacerAdvice` using `Check`/`Wrap`                                             |
| `ChooseOneSelector`, `ChooseOneReplacer`                                                           | `ChoosePerCall<Selection, ...>`, `ChoosePerCall<Replacement, ...>`                                                                                                                                                                         |
| `ChooseOneCreator`, `ChooseOneCrossover`, `ChooseOneMutator`, `ChooseOneRefiner`                   | `ChoosePerCandidateCreator`, `ChoosePerParentGroupCrossover`, `ChoosePerCandidateMutator`, `ChoosePerCandidateRefiner`, including companions, extensions and rate helpers                                                                  |

Source cost against the starting checkpoint, counting added and moved machinery throughout production: production -1,493 physical lines and -12 files; tests +459 lines; published documentation +3 lines. C# and Python sample source has no net line change. The built-in adapter count remains nine; ordinary authors use the existing factory and typed advice patterns.

There are no compatibility aliases or forwarding APIs. The nine built-in operation adapters, invocation contracts, direct `Proceed` methods, `ProceedWith`, wrapper specialization, `WeightedRouter` and its samplers are unchanged. The active-source audit finds no dependency on the removed APIs. Samples and python-samples contain no direct references requiring edits; the C# sample builds with the solution. Historical plan/prototype excerpts retain their original names.

Behavior validation includes 12 fixed snapshots captured before the migration at seeds 7 and 42 for creation, crossover, mutation, refinement, selection and replacement. They pin output values, child-batch order and the next RNG draw. The four per-item forms also cover empty batches, single choices, wrong cardinality, unusual weights and concurrent child implementations. The five typed advice bases cover rejection before executor construction, retry after a failed check, contextual rebinding, one preparation and shared state across operations. Existing observations, contracts, ownership/lifetime, failure and seeded GA checks pass. All 3,172 solution tests pass with SDK 10.0.402, including 2,691 core, 211 API usage, 4 documentation, 239 Experimental and 27 scenario tests.

Final tooling validation passes: Release solution build; whole-solution whitespace, warning-level style and error-level analyzer verification; changed Markdown formatting; `npm run docs:build`; and `dotnet pack --configuration Release --no-restore`, producing all three library packages locally. The branch, HEAD and Git index remain unchanged, and the pre-existing untracked `.claude/settings.local.json` is preserved.

Performance uses equivalent direct wrappers with matching atomic accumulators, `TimeProvider.System`, inputs and retry behavior. Three isolated Release processes reused the existing alternating ABBA protocol, with 2.5 seconds of warmup per case and 31 pairs. Replacement counting and duration initially had uncertainty crossing their budgets; a focused confirmation reused the pinned BenchmarkDotNet protocol with three launches, 40 warmups and 15 measured iterations per launch. No completed Package 2b measurement was rerun. Mean overheads are:

| Operation / concern                     | Shared minus direct |                   Uncertainty interval |
| --------------------------------------- | ------------------: | -------------------------------------: |
| Refinement counting                     |            −2.39 ns |                 95%: −3.82 to −0.95 ns |
| Refinement duration                     |            +6.32 ns |                 95%: +3.72 to +8.93 ns |
| Refinement first-attempt success retry  |            +8.46 ns |                95%: −0.02 to +16.95 ns |
| Refinement three-layer stack            |           +15.15 ns |               95%: +10.84 to +19.45 ns |
| Replacement counting, confirmation      |            −0.01 ns | Conservative 99.9%: −0.03 to +0.013 ns |
| Replacement duration, confirmation      |            −0.15 ns | Conservative 99.9%: −0.41 to +0.106 ns |
| Replacement first-attempt success retry |           +11.61 ns |                95%: +6.60 to +16.62 ns |
| Replacement three-layer stack           |           +22.85 ns |               95%: +11.90 to +33.80 ns |

The paired intervals use the three process means; confirmation bounds subtract the direct/shared confidence endpoints conservatively. All eight paths measure 0 B extra warmed allocation, including both stacks. Counting, duration and first-success retry pass § 8's +5/+10/+25 ns budgets with their upper uncertainty bounds below the limits. Stack latency is reported separately; § 8 supplies no separate absolute stack-latency limit, and these results make no exact-cost-retention claim. Raw reports, backend hashes, provenance and logs remain local in the ignored evidence directory.

Python validation separately parses both `.py` files and all 15 code cells in the three notebooks, with no syntax errors or skipped IPython cells. Runtime notebook/demo execution could not be validated: the available Python environment lacks `pythonnet`/`clr`, FastAPI, Uvicorn and SymPy. The existing termination/interception limits and the drawing-terminator stream distinction remain; failure-then-success retry latency was not investigated. Package 4 has not started.

**Review decisions (2026-10-20; accepted)**: The developer's review of the implementation above extends Package 3 before it closes:

- **Termination and interception move into Package 3.** `IOperator<Termination, TCandidate>` and `IOperator<Interception, TCandidate>` replace `ITerminator` and `IInterceptor` across all consumers, including their stateless, stateful and explicit authoring arities, generic naming, wrapping and multi configurations, typed `TerminatorAdvice` and `InterceptorAdvice` deriving from `OperatorAdvice<TOperation, TCandidate>`, and the generic counting and duration advice. The Package 4 split predates Package 2a; the existing descriptors, adapters and four-argument scopes already carry the run's search state. Package 4's state-view tests move here with them: shared broad terminators across distinct compatible state views in both resolution orders, and invariant interceptor mismatches failing before work. This also removes the `InvalidCastException` limit for general `OperatorAdvice`.
- **Operator budgets take any operation.** `LimitedToCalls`, `LimitedToCandidates` and `LimitedToDuration` accept an `IOperator<TOperation, TCandidate>` and infer the operation; an operator implementing several operations names it. The budget algorithms take the operation and an `OperatorCountMetric`. `IOperatorCounting`, `IOperatorDurationMeasuring` and their strategy records are removed, which drops injecting custom counting advice into a budget.
- **Per-role executor bases are removed.** The `Wrapping…Executor` and `Multi…Executor` bases only held a protected child property. Executors implement their operation's executor interface and receive children through their constructors. Developer guidelines § 4.12 changes accordingly; the backlog's rejection of generic wrapping and multi bases shared across roles stands.
- **One per-item choice.** `ChoosePerItem<TOperation, TCandidate>` replaces `ChoosePerCandidateCreator`, `ChoosePerParentGroupCrossover`, `ChoosePerCandidateMutator` and `ChoosePerCandidateRefiner`, keeping § 4.2's routing semantics and `WeightedRouter` behavior. Operations whose call maps items to one result each (creation positions, crossover parent groups, mutated, evaluated and refined candidates) expose the item count and a call that runs each item on its assigned executor through their invocations, with an adapter flag checked before construction. Selection, replacement, termination and interception do not. Evaluation and external item operations gain per-item choice. Rate helpers remain per operation. This settles only the routing case of § 6's batch-modeling proposal; mapping and chunking stay open.
- **One trace entry point.** Each observed role offers its observations through a small handle, and `Analyzer.Trace` exists once over that handle; the per-role trace overload families are removed. Creation, selection, refinement and replacement gain observations, completing this package's observation adapters. This pulls Package 5's trace integration forward; candidate clocks remain there.

The review decisions were implemented on 2026-10-21:

- Termination and interception use `IOperator<Termination, TCandidate>` and `IOperator<Interception, TCandidate>` with typed `TerminatorAdvice` and `InterceptorAdvice`. The state-view tests moved from Package 4 pass.
- `LimitedToCalls`, `LimitedToCandidates` and `LimitedToDuration` are extension-block members over the algorithm, declared in their budget algorithm's file. An explicit call names the algorithm's types before the operation; inference makes that rarely needed. The arity architecture test checks a receiver's type parameters and a member's own separately.
- The role executor bases are deleted, and so is `CompositeExecutorFactory`, which nothing used afterwards.
- `ChoosePerItem` routes through the invocation's `ItemCount` and `ProceedPerItem`. Those use the internal `ItemBatches`, which `WeightedRouter` also uses, and the seeded routing baselines pass unchanged.
- `Observations<TObservation>` handles exist for algorithms, interceptors and every candidate operation, including new creator, selector, refiner and replacer observations. `Analyzer.Trace` has six overloads over them; `TraceAnalyzer<TResult>.Create` remains for custom roles.
- A typed handle names the candidate first, as in `evaluator.Observations<TCandidate, TSearchSpace, TProblem>()`, because explicit type arguments of an extension-block member include the block's own. The algorithm's typed handle is a classic extension method so that its arguments follow the observation's order.

Limits and removed scenarios:

- A budget no longer accepts custom counting advice.
- No test asserts a single accumulator declaration per root scope directly; the budget tests assert behavior.
- An operation defined outside the library needs its own routing code for per-item choice, because `ItemBatches` is internal.
- The Experimental move operators keep their nominal contracts.

Validation passes: Release build, all 3,168 solution tests, whitespace, warning-level style and error-level analyzer verification, changed Markdown formatting, `npm run docs:build` and `dotnet pack --configuration Release --no-restore`. No performance measurement was rerun.

**Second review decisions (2026-10-21; accepted)**: The developer's second review settles these changes before Package 3 closes:

- **Per-candidate choice.** `ChoosePerItem` becomes `ChoosePerCandidate`: an item is not always a candidate, but each item yields exactly one candidate result. An operation whose calls split into items marks its descriptor with `IItemOperation<TSelf>`, and `ChoosePerCandidate<TOperation, TCandidate>` requires that marker, so choosing per candidate for selection, replacement, termination or interception fails to compile. The marked operation's invocation implements `IItemInvocation<TItemResult>`, offering only `ItemCount` and `ProceedWithItems(executor, items)`. The shared router groups the items, restores their order and checks result counts. `SupportsItemRouting`, `RequireItemRouting` and the per-operation routing code are removed.
- **One public router.** `ItemBatches` folds into `WeightedRouter<TChoice>`, whose routing over item positions becomes public. Operations defined outside the library route items through it by implementing `IItemInvocation`.
- **Generic operator observations.** One `OperatorObservation<TOperation, TCandidate>` records each call of any operation: the operator, the search space and problem at the candidate's interfaces, the invocation and the result. Per-operation extension properties read it, as in `observation.Offspring` for crossover. One `OperatorObservationAdvice<TOperation, TCandidate>` and one `Aspect.Observe(source, observe)` serve every operation, including external ones. `IInvocation<TResult>` exposes the call's `SearchSpace` and `Problem` next to `Random` for this. `Analyzer.Trace` takes an operator or algorithm, or a list of them, directly. The per-role observation records and advice, the `Observations()` handles, `Observations<TObservation>` and the typed search-space and problem observations are removed; algorithm observations become `AlgorithmObservation<TCandidate, TSearchState>`.
- **Three measurements.** `ObjectiveVectorsMeasurement<TCandidate>`, `CandidatesMeasurement<TCandidate>` and `OffspringMeasurement<TCandidate>` replace the seven measurement records. The first two read evaluations and population states, the third the offspring of crossover and mutation. Algorithm observations share the state through `IStateObservation<out TSearchState>`, which interceptor traces adapt to. The `Measurement` factories are removed, because a measurement no longer infers its types from a source.
- **Interception names its state.** `Interception<TSearchState>` replaces `Interception`. Interceptors implement `IOperator<Interception<TSearchState>, TCandidate>`, algorithm slots name the algorithm's state, and traces and analyzers infer it. This keeps the exact state match interceptors already require, now at compile time.
- **Experimental moves use operations.** `MoveCreation<TMove>`, `MoveApplication<TMove>` and `MoveEvaluation<TMove>` descriptors replace the nominal `IMoveCreator`, `IMoveApplier` and `IMoveEvaluator` contracts. Move operators become `IOperator<MoveCreation<TMove>, TCandidate>` and so on, which drops variance over the move type.
- **File placement.** Each operation's resolution extensions move into its executor interface's file, and trace files are named after their classes.

The second review decisions were implemented on 2026-10-21:

- Creation, crossover, mutation, evaluation and refinement are marked with `IItemOperation<TSelf>`. `IItemInvocation<TItemResult>` implements `ProceedPerItem(router)` once over `ItemCount` and `ProceedWithItems`, and `WeightedRouter<TChoice>.Route(count, random, handle)` routes item positions publicly. The seeded routing baselines pass unchanged.
- `OperatorObservation<TOperation, TCandidate>` holds the invocation and result as objects. Each built-in invocation implements an internal arguments interface, which the extension properties read. `Aspect.Observe` now also covers termination and external operations.
- `Analyzer.Trace` has value, measurement and measure-function overloads for operators and algorithms, each over one source or a list, and measurement overloads for interceptors, which adapt the returned state to `IStateObservation<TSearchState>`. A measurement's value type is inferred from the aggregation, because one measurement implements several `IMeasurement<,>` interfaces.
- A typed scope resolves an interceptor at the state its operation names, so wrapping interceptors and pipelines over a narrower state resolve inside an algorithm without four explicit type arguments. Algorithm slots still require the exact state at compile time.
- The Experimental genealogy, rank and Pareto-front analyzers lose their search-space and problem type parameters, which observations no longer carry.
- The move executor interfaces keep their variance; `IMoveApplierExecutor` becomes `IMoveApplicationExecutor` after its descriptor. The move bases derive from `Operator<…>`, which supplies the run-type check they implemented by hand, and advice now applies to move operators.
- `BestMedianWorstTrace.cs` and `BestQualityTrace.cs` are named after their classes, `PopulationQualityTraces` and `BestSoFarTraces`.

Limits and removed scenarios:

- An observation no longer reads a narrower search space or problem, so it no longer checks one at resolution; a callback that needs the concrete problem casts it. The observed operator's own check remains.
- The `Observations()` handles and the `Measurement` factories are removed; a measurement names its candidate.
- An interceptor over another state than the algorithm's is rejected at compile time rather than at resolution.
- Each per-candidate choice boxes the invocation once through the `ProceedPerItem` default implementation, and each operator observation boxes the invocation and result.

Validation passes: Release build, all 3,171 solution tests, whitespace, warning-level style and error-level analyzer verification, changed Markdown formatting, `npm run docs:build` and `dotnet pack --configuration Release --no-restore`. No performance measurement was rerun.

**Move follow-up decisions (2026-10-21; accepted)**:

- **A neighborhood is its three move operators.** `Neighborhood<…>` implements `IOperator<MoveCreation<TMove>, TCandidate>`, `IOperator<MoveApplication<TMove>, TCandidate>` and `IOperator<MoveEvaluation<TMove>, TCandidate>` and is its own executor for all three. `MoveCreator`, `MoveApplier` and `MoveEvaluator` return the neighborhood, so advice selecting one of them reaches the neighborhood a run resolves.
- **Move evaluation reports candidate work.** Each call evaluates one candidate and reports one unit, so candidate counting and candidate budgets accept move evaluators.
- **Stateful move bases follow the core order.** Their methods take the state before the random number generator, search space and problem. The state comes from `CreateInitialState()` and is a class, and the stateful evaluator's `Evaluate` receives the previous objective vector like the stateless one.

The move follow-up was implemented on 2026-10-21. `NeighborhoodCreator`, `NeighborhoodApplier`, `NeighborhoodEvaluator` and `IDirectNeighborhood` are deleted. A neighborhood's candidate-only evaluation evaluates the candidate through the problem instead of throwing.

Limits: a neighborhood is not an `Operator<…>` base, so a run it was not written for is reported as written for "its declared signature" rather than naming its search space and problem.

Validation passes: Release build, all 3,172 solution tests, whitespace, warning-level style and error-level analyzer verification and changed Markdown formatting. Published documentation is unchanged.

**Third review decisions (2026-10-21; accepted)**: The developer's third review settles these changes before Package 3 closes. They supersede the second review's per-candidate choice name, its generic operator observations and its three measurements.

- **Item and candidate.** A name says item when it belongs to splitting a call, which any item operation can use. It says candidate when it is about candidates themselves: candidate work, one operation's inputs and outputs, and rate helpers. `ChoosePerCandidate` becomes `ChoosePerItem`, because it accepts every item operation and an item does not always yield a candidate: evaluation yields an objective vector per candidate. The rate helpers return it and keep candidate wording in their documentation. `IItemOperation<TSelf>` becomes `IItemOperationDescriptor<TSelf>`; `IOperationDescriptor` keeps its name. The glossary gains an item entry.
- **Operation authoring base.** `OperationExecutorAdapter<TExecutor, TArguments, TResult>` implements an operation's invocation once: its random number generator, search space and problem, `Proceed`, `ProceedWith` with its cast, and the item call. An operation author supplies a private arguments struct, the named-method call and, for an item operation, the selection of items. The wrapping executor class and the static adapter factory stay per operation: C# needs a nominal class per executor interface, and only a generic method can name the executor interface for the run types. The base is adopted only if it meets § 8's budgets. A shared helper shrinks each operation's resolution extensions without changing their signatures.
- **Typed observations per operation.** Each operation defines its observation record in a file beside its descriptor, such as `CrossoverObservation<TCandidate>` with `Parents` and `Offspring`, and its typed advice builds it from the typed call. Observations keep their domain property names. All nine operations get one, termination included. `OperatorObservation<TOperation, TCandidate>`, `OperatorObservationAdvice<TOperation, TCandidate>`, the internal `I…Arguments` interfaces and `OperatorObservations` are removed.
- **Observation handles.** `Calls` on each operator and `Iterations` on each algorithm return `Observations<TObservation>`. Readings are nested on the handle and named like the observation property they read: `creator.Calls.Candidates`, `crossover.Calls.Offspring`, `mutator.Calls.Offspring`, `evaluator.Calls.ObjectiveVectors` and `EvaluatedCandidates`, `refiner.Calls.Refined`, `selector.Calls.Selected` and `replacer.Calls.Replaced`. Interceptor calls and algorithm iterations over population states offer `ObjectiveVectors` and `EvaluatedCandidates`. Termination has no reading. `calls.Read(o => …)` builds a custom `Readings<TValue>`; the name `Readings` is provisional. Each operation declares its handle and readings in its own files, as an external operation does, and Analysis names no operation. Both types live in `Inspection`. `Aspect.Observe` takes only handles, and traces deduplicate by the observed operator or algorithm. An operator implementing several operations selects one through a typed view, such as `neighborhood.MoveEvaluator.Calls`.
- **Traces without measurements.** `Analyzer.Trace` has four overloads: a value per observation, or readings with an aggregation, each for one handle or a list. `IMeasurement`, `ObjectiveVectorsMeasurement`, `CandidatesMeasurement`, `OffspringMeasurement`, `DelegateMeasurement`, the interceptor state adapter and `TraceAnalyzer<TResult>.Create` are removed; a custom source constructs a handle. Convenience factories such as best-so-far become extensions on readings and so serve every source.
- **IObservable as explanation only.** Handles are cold descriptions: creating one installs nothing, and `Analyzer.Trace` and `Aspect.Observe` attach it through an aspect. The observability guide explains the analogy and shows feeding observations into an Rx subject with `Aspect.Observe(crossover.Calls, subject.OnNext)`. The backlog records why the library implements neither `IObservable<T>` nor Rx-style operators.

Limits and removed scenarios:

- An external operation is observable only once its author writes its observation and handle; no operation gets a generic observation without code.
- Experimental's move operations lose observation until someone writes theirs.
- Measurements are no longer comparable values, and one measurement no longer serves several kinds of source; evaluation, interception and algorithms each declare their readings.
- A user-written measurement class becomes a custom reading on the handle.
- Every trace and observation call site goes through `Calls` or `Iterations`.

The third review decisions were implemented on 2026-10-22 from `9c567b9e`:

- `ChoosePerCandidate` is `ChoosePerItem` again, `IItemOperation<TSelf>` is `IItemOperationDescriptor<TSelf>`, and the glossary has an item entry. The seeded routing baselines pass unchanged.
- `OperationExecutorAdapter<TExecutor, TArguments, TResult>` builds each call's invocation from the operation's private arguments struct. The struct implements `IOperationArguments<TExecutor, TResult>` or, for an item operation, `IItemOperationArguments<TExecutor, TItemResult>`, which adds `ItemCount` and `SelectItems` and implements the per-item call once through `WeightedRouter`. The nine built-in operations, Experimental's move creation and move application, and both external-operation fixtures use the base; `IItemInvocation<TItemResult>` and the per-operation invocation structs are removed. `ResolutionScope.TryResolve(resolve, out executor, out reason)` replaces the `try`/`catch` body of every `TryResolve` extension, including the Experimental move operations and algorithms.
- Each operation declares its observation record, file-local advice, `Calls` handle and readings in an `…Observation.cs` file beside its descriptor; termination has no reading. Algorithms offer `Iterations`, with `ObjectiveVectors` and `EvaluatedCandidates` over population states. `Observations<TObservation>` and `Readings<TValue>` live in `Inspection`. An external operation constructs its handle through the public `Observations<TObservation>` constructor, as the new checksum observation fixture does.
- `Analyzer.Trace` has the four accepted overloads. `TraceBestSoFar`, `TraceBestCandidateSoFar`, `TracePopulationQuality`, `TracePopulationCandidates` and Experimental's similarity and hypervolume traces extend readings; Experimental's `TraceAllPopulations` and `TraceAllObjectiveVectors` merge into `TraceAll` on any readings. `IMeasurement`, the measurement records, `DelegateMeasurement`, `IStateObservation`, `OperatorObservation`, `OperatorObservationAdvice`, `OperatorObservations` and public `TraceAnalyzer<TResult>.Create` are removed; trace construction deduplicates sources internally.
- The observability guide explains handles as cold observables and shows `Aspect.Observe(crossover.Calls, subject.OnNext)`. Operator implementation documents defining an operation on the adapter base and making it observable, and writing operators links to it. The glossary replaces Measurement with Readings and adds Item and Observation handle. The analyzer, execution-resolution, layering and developer-guideline texts no longer name removed types.

Assumptions taken without review, for the developer to confirm:

- The adapter base needs two public interfaces the decision does not name, `IOperationArguments<TExecutor, TResult>` and `IItemOperationArguments<TExecutor, TItemResult>`. A base with the arguments as a type parameter cannot implement `IItemInvocation<TItemResult>` for its invocation, so the item contract moved from the invocation to the arguments. `IOperationArguments` keeps throwing defaults for `CountCandidateWork` and the per-item call, as the invocation had. Operations with candidate work implement `CountCandidateWork` on their struct, so only per-item choice boxes the arguments, through the item interface's default implementation.
- `Observation` remains derivable only through `Observation<TProblem>`, so a consumer-defined role whose calls receive no problem, such as the former cooling-schedule spec, can no longer be traced. That API usage spec is removed rather than adding a problem-free observation base.
- `Aggregate.Single` and `SingleAggregation<TValue>` remain, although nothing uses them since scalar measurements are gone; a value per observation replaces them.

Limits and removed scenarios, beyond those accepted above:

- A consumer-defined role without a problem has no observation handle and no trace.
- Observation advice is file-local, so an operation's observation cannot be installed under another pointcut, such as `Pointcut.OfType`, through the library's advice.
- Experimental's move evaluation keeps its two hand-written invocations, because its executor has two named methods and the base serves one.
- Experimental's whole-population and objective-vector history traces are one `TraceAll`, read through the source's readings.

Source cost against `9c567b9e`: production +33 lines in the core library, −131 in Experimental and none in PythonInterop, together −98 lines with 13 files added and 4 deleted. Tests −25 lines and one file added; published documentation +178 lines. The adapter base alone: the nine descriptors shrink from 592 to 493 lines while the base and invocation contracts grow from 157 to 225, net −31; mutation shrinks from 67 to 57 lines and the scalar checksum fixture from 87 to 85. The resolution helper removes 299 lines from 13 extension files for +10 in `ResolutionScope`. The adapter count stays at nine built-in plus two fixtures, and an operation author now writes the arguments struct, the wrapping executor and the static factory.

Performance reused the Package 3 harness, direct references and ABBA protocol, three isolated Release processes with 2.5 seconds of warmup and 31 pairs. Both duration intervals crossed the +10 ns budget, so the pinned BenchmarkDotNet confirmation reran them with three launches, 40 warmups and 15 measured iterations. Mean overheads are:

| Operation / concern                     | Shared minus direct |                  Uncertainty interval |
| --------------------------------------- | ------------------: | ------------------------------------: |
| Refinement counting                     |            −1.38 ns |                95%: −1.72 to −1.04 ns |
| Refinement duration, ABBA               |           +12.12 ns |               95%: +8.20 to +16.03 ns |
| Refinement duration, confirmation       |            +0.18 ns | Conservative 99.9%: −0.51 to +0.86 ns |
| Refinement first-attempt success retry  |           +10.37 ns |               95%: +8.29 to +12.45 ns |
| Refinement three-layer stack            |           +21.77 ns |              95%: +20.09 to +23.44 ns |
| Replacement counting                    |            −0.33 ns |                95%: −0.58 to −0.08 ns |
| Replacement duration, ABBA              |           +14.47 ns |               95%: +8.67 to +20.28 ns |
| Replacement duration, confirmation      |            −0.82 ns | Conservative 99.9%: −1.34 to −0.29 ns |
| Replacement first-attempt success retry |           +13.99 ns |              95%: +10.00 to +17.98 ns |
| Replacement three-layer stack           |           +23.66 ns |              95%: +15.35 to +31.97 ns |

All paths measure 0 B extra warmed allocation. Counting, confirmed duration and first-success retry pass § 8's +5/+10/+25 ns budgets with their upper bounds below the limits, so the base is kept. As in the earlier Package 3 measurement, the ABBA duration estimates overstate what the confirmation finds. Retry and stack estimates are 0.8 to 6.6 ns above the earlier Package 3 run; the runs come from different sessions without a same-session reference, so this is no retention claim. Raw reports, logs, the added confirmation class and hashes remain local under ignored `artifacts/operator-role-package3/third-review/`.

Validation passes: Release build, all 3,169 solution tests (2,689 core, 209 API usage, 240 Experimental, 4 documentation and 27 scenario tests), whitespace, warning-level style and error-level analyzer verification, changed Markdown formatting, `npm run docs:build` and `dotnet pack --configuration Release --no-restore`. Build warnings in changed files predate this change. The branch, HEAD and Git index are unchanged.

**Fourth review decisions (2026-10-22; accepted)**: The developer's review of the third review's implementation settles two of its open points. The first supersedes the assumption that every observation derives from `Observation<TProblem>`.

- **Observations need not carry a problem.** `IProblemObservation<out TProblem>` replaces the `Observation` and `Observation<TProblem>` bases and their internal `ObservedProblem`. Operator and algorithm observation records implement it through their own `Problem` property. `Observations<TObservation>` accepts any observation type, so a source whose calls receive no problem has a handle and a trace of one value per observation again. `Read` becomes an extension constrained to `IProblemObservation<IProblem>`, which covariance satisfies for every record.
- **A trace merges sources by boundary and reading.** Handles match only with the same source reference and observation type, and readings only when their read functions are also equal delegates. Different readings of one source and the handles of different operations of one configuration are each observed. `Analyzer`, the analyzer architecture and the observability guide document the rule.

The fourth review decisions were implemented on 2026-10-22:

- `IProblemObservation<TProblem>` lives in `Inspection`; the ten observation records, the checksum fixture and the operator-implementation example implement it.
- An internal `ObservationIdentity` keys a trace's sources; handles and readings carry it.
- `ConsumerDefinedRolesCanFeedATrace` is restored with a cooling-schedule handle of the consumer's own. New tests cover two readings of one source and two operations of one configuration.

Limits:

- `Read` is an extension member, so C# no longer accepts `calls.Read<double>(o => [...])` with only the value type given. Callers let the value type be inferred, as in `calls.Read(o => o.Offspring.Select(c => c[0]).ToArray())`; three tests changed.
- A handle without a problem offers no readings, including aggregations such as `MinMeanMax` that need no objective; it is traced one value per observation.

Validation passes: Release build, all 3,173 solution tests, whitespace, warning-level style and error-level analyzer verification, changed Markdown formatting and `npm run docs:build`. The Git index is unchanged.

**Fifth review decisions (2026-10-23; accepted, step 1 implemented)**: The developer's review of the fourth review's implementation reshapes observation and the call system before Package 4. They supersede the fourth review's problem interface and reading identity, the third review's readings and operation authoring base, including its assumption about the argument interfaces, and the 2026-10-16 descriptor naming.

Observation:

- **One observation source.** `ObservationSource<TObservation>`, a provisional name, replaces `Observations<TObservation>` and `Readings<TValue>`. Operators offer `Calls` and algorithms `Iterations` as before, and a source is used directly, as in `Analyzer.Trace(crossover.Calls, o => o.Offspring.Count)` and `Aspect.Observe(crossover.Calls, subject.OnNext)`. The observed configuration remains a constructor argument; its property becomes internal.
- **Records are the call.** A record holds the operator's inputs and outputs in their original types, plus the call's search space and problem, typed as today, because operator and algorithm calls receive them as ordinary arguments. A record of a call without them, such as a consumer-defined cooling schedule, holds none. Nothing is added for analysis, such as objective directions on evaluation records, and no base record or interface declares the problem.
- **No projections.** `Readings<TValue>`, a `Reading` value, `Read` or `Select`, the named readings on sources and `IProblemObservation` are removed. Traces read records through a function, and `IAggregation` is removed, so `Analyzer.Trace` has one form. Counting, collecting and single values become plain functions in that form.
- **Ranking takes its directions as an argument.** Ranking becomes plain functions such as best and best-median-worst that receive the values and their objective directions, and the trace's function passes the call's own, as in `Analyzer.Trace(evaluator.Calls, o => o.ObjectiveVectors.BestMedianWorst(o.Problem.Objective))`. Sources and traces carry no problem function. Ranking within one call never conflicts, because each call is ranked by its own problem.
- **Best-so-far.** A best-so-far object keeps its running best across calls, as today. The first directions it receives fix it, and a later call with other directions or another dimensionality throws. Passing fixed directions, such as `sphere.Objective`, instead of each call's own remains possible.
- **Trace helpers.** `TracePopulationQuality` and `TracePopulationCandidates` target iterations over `PopulationState` and take no lambda; they key on a state type any algorithm can yield, not on an operation. `TraceBestSoFar` and `TraceBestCandidateSoFar` target evaluator calls, where all their current uses are, and take no lambda either. Each helper passes the call's own directions.
- **Duplicate sources.** A trace observes each source once, by configuration reference and observation type. Read-function equality leaves the identity, because one trace applies one function to all its sources.
- **Aspects are lists.** An aspect is the list of its pointcut/advice pairs, so clocks and composite analyzers write `Advices = [.. Aspect.Observe(evaluator.Calls, Record)]` instead of unpacking another aspect's `Advices`.

Calls and operations:

- **One invocation per operation.** Each operation implements a private `Invocation` struct directly on `IInvocation<TResult>`. It supplies `Random`, `SearchSpace` and `Problem`, `Proceed(IExecutor? executor = null, IRandomNumberGenerator? random = null)`, which replaces `ProceedWith`, `CountCandidateWork` for operations with candidate work and, for item operations, `ProceedPerItem(router)` through `WeightedRouter.Route`. `SearchSpace` and `Problem` stay untyped and unused for now, so every advice sees the call's context through the existing invocation rather than a separate interface. `IOperationArguments`, `IItemOperationArguments` and `OperationExecutorAdapter<TExecutor, TArguments, TResult>` are removed, and per-item choice no longer boxes. `IInvocation<TResult>` remains the call system's only interface. Experimental's move evaluation writes its two invocations like any other operation.
- **The operation type holds what every system needs.** Explicit static members of `IOperation<TSelf>` supply the executor interface for the run types, wrap an executor with an invocation wrapper and state whether calls report candidate work; the item marker states that calls split into items. `OperationExecutorAdapter`, `OperationExecutorAdapter<TExecutor>` and the per-operation `ExecutorAdapter` classes are removed. General and typed advice receive `OperationInfo`, one library-owned sealed type, and wrap through `operation.Wrap(executor, wrapper)`. Typed resolution, typed advice bases, observations and authoring bases stay per operation, because C# has no associated types.
- **Names.** `IOperationDescriptor<TSelf>` becomes `IOperation<TSelf>` and `IItemOperationDescriptor<TSelf>` becomes `IItemOperation<TSelf>`. The glossary already defines an operation as the named action an operator performs; it records both names with status `Provisional`, so their similarity to operator can be discussed again.

Factory review (accepted, step 1 implemented): the same session removes the factories between configurations and their executors.

- **One hook per configuration.** Every configuration builds its executor in one `CreateExecutor` hook: run-generic operators override `CreateExecutor<TSearchSpace, TProblem, TSearchState>(scope)`, algorithms `CreateExecutor<TSearchSpace, TProblem>(scope)`, and fixed-signature leaves and own roles `CreateExecutor(ResolutionScope scope)`. `OperatorExecutorFactory` with its private classes and `FromFactory`, `Forward` and `WrapInvocation`, `OperatorExecutorFactory<TOperation, TCandidate>` with the 22 nested `Factory` classes, `ExecutorFactory<TExecutor>`, `WrapperExecutorFactory<TExecutor>` and the hooks `CreateExecutorFactory`, `CreateOperatorFactory`, `CreateFactory`, `CreateIterationFactory` and `CreateWrapperFactory` are removed.
- **Execution state through the scope.** `scope.GetExecutionState(static () => new CountAccumulator())` returns the execution state of the configuration the scope builds and creates it on the first request. Each build receives a new scope that knows its configuration, so no key is passed and a configuration only reaches its own state; calling it on a scope that builds nothing, such as one from `ResolutionScope.Create()`, is a bug. Resolution stores the state in the execution state that owns the configuration, found as today, so every later build there gets the same object across operations and run types, while a fresh child state starts with new state. A configuration has one state object; asking for another type is a bug. The cast stays in resolution's storage, and no public signature uses `object`. Scoped lifetimes in dependency-injection containers and state hooks in UI frameworks, such as React's `useState`, work the same way. This replaces the state variants of `Operator` and `WrappingOperator` considered earlier in the review.
- **Consequences for authors.** Stateful role bases such as `StatefulMutator` keep a typed hook, renamed from `CreateInitialState` to `CreateExecutionState` because the old name reads like the search state a run starts from, and pass its result to the author's method. Validation moves to the start of `CreateExecutor`. A stateful leaf builds a new executor for each resolution, sharing its state, instead of returning one instance.
- **Algorithms prepare and wrap like operators.** An algorithm prepares once per configuration instead of once per run types, and wrapping algorithms build around the resolved child directly. This lands with Package 4's meta-algorithm rework.
- **Advice wraps the built executor.** Resolution applies operator, algorithm and own-role advice itself, each through one library base with `Wrap(executor, source)`: `OperatorAdvice`, `AlgorithmAdvice` and, for own roles, `Advice<TConfiguration, TExecutor>`, whose `Wrap` receives the role type itself. Resolution, which knows only the executor type, reaches it through an internal interface, and only library bases derive from `Advice<TConfiguration>` directly. Own roles no longer pass a callback that casts their advice, so the lambda-based `ResolutionScope.Resolve(configuration, createFactory, createWrapperFactory)` is removed. Advice keeps its data on the advice object, also for algorithms: an algorithm observation's `Iteration` counts every yielded state the advice sees, across fresh activations, so iteration clocks only move forward, while the inner algorithm itself still restarts with fresh execution state.
- **No typed advice base for now.** Typed advice keeps writing its own wrapping executor; today only the library's observation advice does. A base that owns the executor, so that an author overrides one method such as `Mutate(MutatorCall<TCandidate> call)`, needs one public call type per operation and is reconsidered when authors write typed advice.
- **Own roles stay.** `IConfigurationNode<TExecutor>` remains for consumer-defined roles, such as the cooling schedule in the pointcut specs.
- **Per-trial aspects without factories.** `ExperimentRun.AddAspectPerTrial(Func<TAlgorithm, TAspect>)` returns each trial with its aspect. `AspectFactory`, its two generic forms and `GetAspectPerTrial` are removed; the factory only served as the lookup key and as the reason to reject a second add. `AddAspectPerTrial` no longer chains.

Implementation order (accepted 2026-10-24): two steps, each in a fresh session.

1. Before Package 4, in one step: the observation decisions, the call and operation decisions, operator construction through `CreateExecutor` and `GetExecutionState`, and per-trial aspects without `AspectFactory`. Algorithms keep their current factories, `ExecutorFactory<TExecutor>` and `WrapperExecutorFactory<TExecutor>` until step 2, and own roles keep the lambda-based `Resolve` they share with algorithms.
2. With Package 4: algorithms built and wrapped like operators, algorithm advice keeping its data on the advice, own roles with `Advice<TConfiguration, TExecutor>`, and the removal of `ExecutorFactory<TExecutor>`, `WrapperExecutorFactory<TExecutor>` and the lambda-based `Resolve`.

To-dos of step 1: the glossary marks `IOperation` and `IItemOperation` as `Provisional` and updates "trial analyzer factory", "configuration node", "execution state", "operation descriptor" and the reading terms; developer guidelines § 4.3 and § 4.4, the execution-resolution, operator-implementation and analyzer architecture pages and the observability and experiments guides follow the new names; `AspectFactorySpecs`, which tests the predefined aspects, is renamed and `LayerDependencyTests` is updated.

Assumptions to confirm during implementation:

- `MinMeanMax` needs no objective directions, so it becomes a plain function like counting.
- The § 8 budgets are measured again after the adapters are removed.
- Analyzer HLib0002 finds state types in `GetExecutionState` calls instead of base type arguments, and HLib0001 is reworded for `CreateExecutor`.

Limits and removed scenarios:

- One trace no longer combines sources of different observation types, such as mutator and evaluator calls; one unit test does so today.
- A ranking trace written directly passes the directions, as in `o.Problem.Objective`; about 20 places do so today, most of them in library helpers.
- A best-so-far trace over calls with different objectives throws instead of restarting.
- Each operation's invocation writes `Random` and the executor cast again.

**Step 1 implementation (2026-10-24)**: The observation decisions, the call and operation decisions, operator construction through `CreateExecutor` and `GetExecutionState`, and per-trial aspects through `AddAspectPerTrial` are implemented across core, Experimental, PythonInterop, analyzers, tests and documentation, including the step's to-dos. Algorithms and own roles keep their factories until step 2.

Assumption outcomes:

- `MinMeanMax` needs no objective directions and is a plain function over numbers. Confirmed.
- The § 8 budgets pass; see the measurements below.
- HLib0002 holds in part. The stateful bases call `GetExecutionState` in library code, so the analyzer still finds a stateful leaf's state type through its base type argument; it now also checks the state type of every `GetExecutionState` call in author code. HLib0001 is reworded for `CreateExecutor` and matches both hooks. Its code fix covers `CreateExecutor` too: a child resolves through `scope.Resolve<...>(child)` on a scope without run types and through `scope.ResolveOperator(child)` on a run-generic scope.

Consequences found during implementation:

- A stateful leaf builds a new executor for each resolution, so two resolutions share state but neither executor nor control identity. Tests that compared identity now compare the shared value.
- Advice `Check` runs before the construction hook, so a rejected operator creates no execution state.
- Experimental's `HyperVolumeAggregation` becomes the best-so-far object `HyperVolumeSoFar`, and its population-similarity aggregation becomes the `Similarity` function in `PopulationSimilarityExtensions`; the similarity traces target iterations.
- Numerics' internal `OperationInfo` is renamed `NumericOperationInfo`, so test projects that see internals need no alias for the execution type (decided 2026-10-24).
- Test fixtures that kept an executor as their own execution state, so that every build returned the same object, now keep a small state object and build a new executor each time, as authors must; their tests compare the shared value (decided 2026-10-24).
- Removed tests: traces over different readings of one source and over mutator and evaluator calls together, the rejection of a second per-trial add, the `OperatorExecutorFactory` analyzer case and the generic-owner code-fix case.

Source cost against the index state the step started from: production −599 lines, −554 in the core library, −45 in Experimental and none in PythonInterop, with 4 files added, 9 deleted and 1 renamed; analyzers +85 lines; tests −43 lines, with 2 files added and 2 deleted; published documentation −31 lines. The nine built-in operations shrink from 493 to 477 lines and the operation and invocation contracts from 262 to 206, as `OperationExecutorAdapter` gives way to `OperationInfo`. `OperatorExecutorFactory` and the 22 nested `Factory` classes are removed. An operation author writes the invocation struct, the wrapping executor and the operation's static members.

Performance reused the Package 3 harness, direct references and ABBA protocol, adapted only to the `IOperation<TSelf>` constraint: three isolated Release processes with 2.5 seconds of warmup and 31 pairs. Both duration intervals crossed the +10 ns budget again, so the pinned BenchmarkDotNet confirmation reran them with three launches, 40 warmups and 15 measured iterations. Mean overheads are:

| Operation / concern                     | Shared minus direct |                  Uncertainty interval |
| --------------------------------------- | ------------------: | ------------------------------------: |
| Refinement counting                     |            −1.24 ns |                95%: −1.59 to −0.89 ns |
| Refinement duration, ABBA               |            +7.66 ns |               95%: +4.91 to +10.40 ns |
| Refinement duration, confirmation       |            +3.74 ns | Conservative 99.9%: +1.24 to +6.24 ns |
| Refinement first-attempt success retry  |            +7.49 ns |               95%: +4.40 to +10.59 ns |
| Refinement three-layer stack            |           +18.74 ns |              95%: +12.52 to +24.95 ns |
| Replacement counting                    |            +0.09 ns |                95%: −0.45 to +0.62 ns |
| Replacement duration, ABBA              |            +9.61 ns |               95%: +7.75 to +11.46 ns |
| Replacement duration, confirmation      |            +2.71 ns | Conservative 99.9%: −0.74 to +6.16 ns |
| Replacement first-attempt success retry |            +9.36 ns |               95%: +8.43 to +10.30 ns |
| Replacement three-layer stack           |           +20.32 ns |              95%: +18.46 to +22.19 ns |

All paths measure 0 B extra warmed allocation. Counting, confirmed duration and first-success retry pass § 8's +5/+10/+25 ns budgets with their upper bounds below the limits. Retry and stack estimates are 2.9 to 4.6 ns below the third review's run, while the confirmed duration estimates are 3.5 to 3.6 ns above it; the runs come from different sessions without a same-session reference, so this is no retention claim. Raw reports, logs, the adapted harness and hashes remain local under ignored `artifacts/operator-role-package3/fifth-review/`.

Validation passes: Release build, all 3,172 solution tests, whitespace, warning-level style and error-level analyzer verification, changed Markdown formatting, `npm run docs:build` and `dotnet pack --configuration Release --no-restore`. Build warnings in changed files predate this change. The branch, HEAD and Git index are unchanged.

**Sixth review decisions (2026-10-24; accepted, implemented)**: The developer's review of step 1 settles these changes before Package 4. They supersede the factory review's scope that knows the configuration it builds and the fifth review's per-item invocation member and executor-type member.

- **The configuration names itself.** `scope.GetExecutionState(this, create)` replaces `GetExecutionState(create)`: the configuration passes itself instead of the scope remembering what it builds. Resolution finds the configuration's owner as for every configuration and throws unless that configuration is being built at that moment. Every build receives the owner's scope through `WithState`; the scope's `building` field and `Building` are removed. HLib0002 also reports a request whose first argument is not `this`, so misuse shows at compile time and not only when the build runs; the scope's own forwarding call is exempt.
- **Routing is an item operation's capability.** `IItemOperation<TSelf>` declares `Route<TCandidate, TSearchSpace, TProblem, TSearchState>(WeightedRouter<IExecutor> children)`, which returns an executor that runs each item of a call on the child the router assigns it. `ChoosePerItem` returns that executor; `IInvocation.ProceedPerItem` and the file-local `ItemChoiceWrapper` are removed. Item assignment, random draws and the original list for a child that receives every item stay as they are.
- **Candidate work stays on the invocation.** `CountCandidateWork` keeps its throwing default; the backlog's candidate-work capability entry still applies.
- **Typed executor check.** `IOperation<TSelf>.IsExecutor<TCandidate, TSearchSpace, TProblem, TSearchState>(executor)` replaces `GetExecutorType`, so the operation contract holds no `Type`. Resolution checks every built and wrapped executor with it. The check that fixed-signature `Operator<…, TExecutor>` ran before its hook is removed; a wrong executor type now fails right after the hook, still during resolution. `OperationInfo.ExecutorType` is removed.
- **One best-so-far.** `BestSoFar<T>(Func<T, ObjectiveVector> objectiveVector, IComparer<ObjectiveVector>? objectiveComparer = null)` replaces `BestSoFar`, `BestCandidateSoFar<TCandidate>` and the internal `FixedDirections`, in one file with `BestSoFarTraces`. It keeps the fixed-direction and dimensionality checks.
- **The invocation keeps the call's context.** `IInvocation<TResult>` keeps `Random`, `SearchSpace` and `Problem`: they are arguments of the call, and general wrappers see only the invocation, while removing all three would take `ChoosePerCall` its generator. Passing them to `Invoke` beside the invocation would repeat them in every wrapper signature, a struct bundling them is the context carrier § 4.19 of the developer guidelines excludes, and a class invocation with a base would allocate on every call. The backlog records the repeated properties for a later look.
- **Executor bases for operation authors.** `WrappingExecutor<TExecutor, TWrapper>` holds the wrapped executor, cast once from `IExecutor`, and the wrapper, and forwards controls. `RoutingExecutor<TExecutor>` holds the children as a `WeightedRouter<TExecutor>`. Each operation's wrapping and routed executors derive from them, so an operation writes neither control forwarding nor the casts in `Wrap` and routing. The static members and the invocation struct stay per operation: the members name the executor interface for the run types, and a struct has no base class. `IInvocation<TResult>` moves to its own file.

**Sixth review follow-up decisions (2026-10-24; accepted, implemented)**:

- **`BestSoFar` keeps the best objective vector.** The non-generic `BestSoFar(objectiveComparer)` derives from `BestSoFar<ObjectiveVector>` with the identity selector, so the common case reads `new BestSoFar()`. `BestSoFar<T>(selector)` remains for other values and is no longer sealed.
- **Contracts keep `IReadOnlyList<T>`.** Spans in operator contracts are rejected and recorded in the [developer backlog](developer-backlog.md#spans-in-operator-contracts); `ArraySegment` stays an implementation choice. The sweep found no further `ArraySegment` use; `LimitEvaluator` no longer copies the candidates it skips only to count them.

**Sixth review implementation (2026-10-24)**: The decisions are implemented across core, Experimental, tests and documentation, together with a test that a configuration not being built cannot request execution state, from a scope that builds nothing or for a child whose build has finished.

Consequences found during implementation:

- Algorithms and own roles, which keep their factories until step 2, also receive the owner's scope through `WithState` and request their state by passing themselves.
- `OperationInfo` exposes no operation name: its per-operation dispatch instance names the operation by its type in messages, and the executor check it replaces `ExecutorType` with is internal to resolution.
- `ExecutorControls.Find<TControl, TWrapper>` is removed: `WrappingExecutor<TExecutor, TWrapper>`, its only caller, asks its wrapper for controls itself. `WeightedRouter<TChoice>` gains an internal `Cast<TOther>()`, which `RoutingExecutor<TExecutor>` uses to type the children once.
- A routed executor hands each child a batch through the router's list overload, as an `ArraySegment` over the grouped inputs instead of a new array; assignments, random draws and the original list for a child receiving every item are unchanged.

Source cost, counted from a snapshot taken partway through and file by file before it: production about −8 lines, about −2 in the core library and −6 in Experimental, with 3 files added and 1 deleted; tests +22 lines; published documentation +3 lines. The nine built-in operations grow from 477 to 484 lines: each item operation gains its routed executor (+3), and each other operation loses its control forwarding (−2). `ChoosePerItem` shrinks by 9 lines, `Operator` by 23 and the best-so-far file pair from 138 to 105, while the two executor bases add 45.

Performance reran the fifth review's harness unchanged: three isolated Release processes, 2.5 seconds of warmup and 31 ABBA pairs. Both duration intervals crossed the +10 ns budget again, so the pinned BenchmarkDotNet confirmation reran them with three launches, 40 warmups and 15 measured iterations:

| Operation / concern                     | Shared minus direct |                  Uncertainty interval |
| --------------------------------------- | ------------------: | ------------------------------------: |
| Refinement counting                     |            −1.84 ns |                95%: −2.95 to −0.72 ns |
| Refinement duration, ABBA               |            +8.71 ns |               95%: +6.43 to +10.98 ns |
| Refinement duration, confirmation       |            +1.43 ns | Conservative 99.9%: +0.14 to +2.72 ns |
| Refinement first-attempt success retry  |            +6.75 ns |               95%: −0.26 to +13.77 ns |
| Refinement three-layer stack            |           +14.36 ns |               95%: −1.04 to +29.76 ns |
| Replacement counting                    |            −0.36 ns |                95%: −1.43 to +0.70 ns |
| Replacement duration, ABBA              |            +9.28 ns |               95%: −0.69 to +19.25 ns |
| Replacement duration, confirmation      |            −0.79 ns | Conservative 99.9%: −1.99 to +0.42 ns |
| Replacement first-attempt success retry |            +8.71 ns |               95%: −0.45 to +17.86 ns |
| Replacement three-layer stack           |           +22.84 ns |              95%: +14.18 to +31.50 ns |

All paths measure 0 B extra warmed allocation. Counting, confirmed duration and first-success retry pass § 8's +5/+10/+25 ns budgets with their upper bounds below the limits. The ABBA intervals are wider than in the fifth review's run, and no estimate moved by more than 4.4 ns from it; the runs come from different sessions without a same-session reference, so this is no retention claim. Raw reports, logs, the harness and hashes remain local under ignored `artifacts/operator-role-package3/sixth-review/`.

Validation passes: Release build, all 3,175 solution tests, including the HLib0002 tests for the first argument, whitespace, warning-level style and error-level analyzer verification, changed Markdown formatting, `npm run docs:build` and `dotnet pack --configuration Release --no-restore`. The branch, HEAD and Git index are unchanged.

**Seventh review decisions (2026-10-24; accepted, implemented)**: The developer's review of the sixth review's implementation and of its standards review settles these points.

- **Observation sources keep their name.** "Handle" is too generic, so `ObservationSource<TObservation>` stays, still provisional. Documentation calls the `Calls` or `Iterations` value an observation source and the configuration it observes the observed configuration; the constructor parameter and the internal property are named `configuration`.
- **Execution state.** Resolution creates the state that owns a configuration, but runs use it as execution state, so documentation calls it execution state rather than resolution state.
- **One file per contract.** `IOperator<TOperation, TCandidate>` moves to `IOperator.cs`; `IOperation<TSelf>` and `IItemOperation<TSelf>` live in `IOperation.cs`.
- **No comparer on objective-vector extremes.** `Best`, `Worst` and `Median` over objective vectors lose the `objectiveComparer` parameter this branch added and no caller used.
- **Fixed directions for the hypervolume so far.** `HyperVolumeSoFar` throws on a later call with other objective directions, as `BestSoFar` does.
- **Standards-review suggestions.** `ChoosePerCall` and `ChoosePerItem` share their checks and child resolution through an internal `WeightedChoice`. The analyzers and the code fix recognize execution types through one internal `ExecutionTypes.Is`. A test operator implementing several operations implements `CreateExecutor` once, as a public method: its signature does not name the operation, so one method implements it for every operation. The "Define a new operation" walkthrough is compiled and run in `HeuristicLib.Tests.Docs` and imported into the page.

### Package 4 — State-dependent operations, native hosting and meta-algorithms

Termination and interception moved into Package 3 (see its review decisions). Termination keeps the random number generator added in Package 2b.

Keep `IAlgorithm` independent and migrate Cycle, budgets, IslandModel and other affected meta-algorithms to the new operator collaborators without inventing `AlgorithmOperation`. Use native `AlgorithmRun`, typed scopes and algorithm advice; remove experimental host/contract adapters from the implementation path. Validate Fresh/Retained/Shared reuse, pausing, root-boundary cancellation, nested early stopping and disposal. Preserve all existing algorithm entry points and completion semantics.

Open question from the fifth review's factory review: `CycleAlgorithm` offers an observation source `Turns` with one `CycleTurn` record per inner algorithm's turn, holding the cycle number, the inner algorithm and the state it starts from. An analyzer observing the inner algorithm's iterations then knows when that algorithm restarts: the observed iteration count continues across turns, and a new call to the inner algorithm cannot tell a fresh start from a retained continuation. The developer accepted the names; `Activations` was rejected. Moved to Package 5 by the design pass below.

**Gate**: full solution tests and public hosting/API/documentation examples pass, state-view/advice counters survive rebinding correctly, and no prototype bridge or nominal operator configuration interface is required by production.

**Design pass (2026-10-25; accepted)**: The text above predates Package 2a. Termination and interception moved to Package 3, `AlgorithmRun` already hosts every algorithm natively, and neither an island model nor experimental host or contract adapters exist; `IslandPopulation` is only a search state, and an island workflow stays a backlog item. The `InvalidCastException` limit of Package 3's decisions is gone: `GeneralAdvice_WrapsTerminationAndInterception` passes at `acd1aa06`, because general advice wraps every operation through `OperationInfo`. The one cast left is in algorithm resolution and goes with this package.

Restated scope: step 2 of the factory review, and validation of the meta-algorithms built on it.

- Algorithm bases: `Algorithm` and `IterativeAlgorithm` with their bound variants, `WrappingAlgorithm` and `NamedAlgorithm`.
- Algorithms: `GeneticAlgorithm`, `EvolutionStrategy`, `NSGA2`, `HillClimber`; Experimental's `ALPSGeneticAlgorithm`, `OpenEndedRelevantAllelesPreservingGeneticAlgorithm` and `DynamicRacingAlgorithm`.
- Meta-algorithms: `CycleAlgorithm`, `PipelineAlgorithm`, `OperatorBudgetAlgorithm`, `OperatorDurationBudgetAlgorithm`, `AlgorithmDurationBudgetAlgorithm` and `StateTerminatedAlgorithm`.
- Advice: `AlgorithmAdvice` with the counting, duration and observation advice, and `Advice<TConfiguration, TExecutor>` for consumer-defined roles.
- Resolution: the factory path of `ResolutionScope`, `ExecutorFactory<TExecutor>`, `WrapperExecutorFactory<TExecutor>`, and the lambda-based `Resolve` and `TryResolve`, including the overload projecting controls from the raw executor.
- HLib0001 and its code fix; about 30 test files and the API usage specs; the writing-algorithms and writing-meta-algorithms guides, the execution-resolution, configuration-vs-executors and layering pages, developer guidelines § 4.3, § 4.4, § 4.7 and § 4.17, and the glossary. PythonInterop and the samples use no algorithm factory.

Design:

- **One protected hook.** `IAlgorithm<TCandidate, TSearchState>` declares `CreateExecutor<TSearchSpace, TProblem>(ResolutionScope<TCandidate, TSearchSpace, TProblem, TSearchState> scope)`. The scope is typed as for operators, so algorithms resolve their operators without `scope.For<…>()`. The bases implement it explicitly and expose a protected abstract hook, as `Operator<TOperation, TCandidate>` does; bound bases expose it at their own types. `IAlgorithm<TCandidate>` keeps no member: resolving it for a search state checks that it is an `IAlgorithm<TCandidate, TSearchState>`, replacing the state bridge in every base. Resolution checks `Fits` before the build, as for operators. `CreateExecutorFactory`, `CreateIterationFactory` and the wrapping hook are removed; `NamedAlgorithm` returns `scope.Resolve(ChildAlgorithm)`.
- **One algorithm path in resolution.** It mirrors `ResolveOperator`: fit check, recursion guard, the shared owner lookup, the build with the owner's scope, then each matching `AlgorithmAdvice.Wrap` in application order. Resolution state keeps only owners and their execution state; the legacy factory entry, the per-advice wrapper-factory table and the preparation-path check are removed. A matching advice of another kind fails with a diagnostic instead of an `InvalidCastException`; algorithm advice for another search state is skipped, like operator advice for another operation.
- **Advice.** `AlgorithmAdvice<TCandidate, TSearchState>` has only `Wrap<TSearchSpace, TProblem>(executor, source)`; a `Check` is added when an advice needs one. Counting and duration advice already keep their accumulators; observation advice keeps its iteration counter on itself and increments it atomically. Consumer-defined roles implement `IConfigurationNode<TExecutor>.CreateExecutor(ResolutionScope scope)` and advise through `Advice<TConfiguration, TExecutor>.Wrap(executor, source)`; `Advice<TConfiguration>` becomes derivable only inside the library. Their controls are found through `ExecutorControls.Find`, as for operators.
- **Execution state.** Algorithms with persistent data request it in their build: Cycle its `ChildScopeSelector`, each operator budget one object holding its accumulator and aspect, `DynamicRacingAlgorithm` its hall of fame.

Cycle's policies fit unchanged: `ChildScopePolicy`, `ChildScopeSelector`, `CreateChildState` and `WithState` keep their behavior, and the selector becomes Cycle's execution state, `scope.GetExecutionState(this, () => new ChildScopeSelector(ScopePolicy))`, held by the Cycle's owner as today.

- **Fresh**: inner execution state is new for each activation unless an ancestor owns it, as today. Algorithm advice data no longer restarts with an activation, so iteration numbers continue.
- **Retained**: inner execution state is kept per inner configuration reference, now for every run type the Cycle is built for in its owner; today a second run type there fails. No state references advice any more, so retained state cannot keep an earlier observer alive.
- **Shared**: inner execution state is the Cycle owner's, as today; advice data lives on the advice.

Validation:

- Fresh, Retained and Shared reuse: the existing Cycle reuse, sibling, ancestor, retained-rebinding and observer-lifetime tests and the Cycle analysis scenarios, plus a new test that one Cycle built for two run types in one owner shares its selector.
- Pausing: the paused deferred-stream, paused algorithm-duration and `AlgorithmRun` pause tests. Executors keep their construction scope, so this path does not change.
- Root-boundary cancellation: the terminal-cancellation and pause-on-cancel run tests and Cycle's cancellation check before activating a child.
- Nested early stopping: the inner-budget Cycle test, nested count and duration budgets, and state termination across rebinding.
- Disposal: equal starts and disposals per policy, racing contender disposal and run disposal failures.
- Counters across rebinding: the budget tests, which now take their accumulators from execution state, and a new test that an algorithm observation's iteration count continues across Fresh activations and rebinding. `IterationClock_FollowsWhetherTheCycleRecreatesItsExecutions` expects 1 to 6 for Fresh.

Limits and removed scenarios:

- An algorithm observed through one advice in several places, or by several runs sharing one aspect, numbers its iterations across all of them.
- A consumer-defined role no longer reads controls from its raw executor through advice that does not forward them.

**Decision (2026-10-25; accepted)**: Algorithm advice follows operator advice in visibility. `AlgorithmObservationAdvice` becomes file-local and is reached only through `algorithm.Iterations`, so each advice and its iteration counter observe one algorithm. Counting and duration advice stay public, like their operator counterparts, because they add to an accumulator the caller supplies. If advice types are ever offered publicly for users to select freely, operator and algorithm advice become public together.

**Decision (2026-10-25; accepted)**: Package 4 builds no source marking where an inner algorithm's turn starts. A clock ticks once per event it observes and is not the algorithm's own counter, so an iteration clock over Cycle's inner algorithm reads 1 to 6 across two Fresh turns of three iterations. An analyzer that needs the count per turn observes a second source that ticks at the start of each turn and resets its count there. Package 5 builds that source; the provisional choice is `algorithm.Calls`, one record per call of any algorithm holding its starting state, through ordinary algorithm advice. It serves every meta-algorithm without a new mechanism, but carries no cycle number, and its record arrives at the start of a call with inputs only, unlike operator call records. Package 5 reconsiders it against `cycle.Turns`, which would need a public way for an algorithm to publish its own events, because no advice sees a turn inside Cycle's executor.

**Implementation (2026-10-25; awaiting review)**: Step 2 is implemented as designed, except for the deviations below. `ExecutorFactory<TExecutor>`, `WrapperExecutorFactory<TExecutor>`, the lambda-based `Resolve` and `TryResolve` and the control-projection overload are removed; every algorithm, consumer-defined role and test double builds in `CreateExecutor`. HLib0001 reports direct `CreateExecutor` calls of every role, including child algorithms, and its fix resolves through the scope the hook receives.

Deviations:

- **Algorithm resolution does not check `Fits`.** An algorithm's `Fits` includes its operators, so a check before the build named the algorithm and hid the operator at fault, which `PreflightValidationSpecs.CreatingARun_RefusesAnOperatorWrittenForAnotherSearchSpace` requires at run start. As before Package 4, resolution checks the produced search state, the bound bases check their own search space and problem before calling the hook, and each operator reports its own mismatch when it is resolved. This awaits the developer's decision; the alternative is the design's check with a message naming the first child that does not fit.
- **Algorithm resolution is a public instance method.** `ResolutionScope.Resolve<TCandidate, TSearchSpace, TProblem, TSearchState>(IAlgorithm<TCandidate>)` replaces the extension of the same name, as `ResolveOperator` is public, because the layer rule forbids `Algorithms` from calling resolution internals. Call sites are unchanged.
- **`ChildScopeSelector` suppresses HLib0002** for its table of retained child states, because the selector is now execution state.

Limits, in addition to those of the design:

- The agnostic iterative base inherits the algorithm hook, which declares `IAlgorithmExecutor`. Returning a non-iterative executor, which skips the interceptor, is no longer a compile error there; the bound base keeps the `IterativeAlgorithmExecutor` return type.
- Counting, duration and observation advice data is shared by every configuration and resolution one advice instance wraps.

Removed scenarios: advice preparation and per-owner advice state, control projection from the raw executor, low-level factory callbacks switching an owner's preparation path, and the analyzer and code-fix cases for stored or deferred factories. Tests that compared executor references now compare execution state, because every resolution builds a new executor.

Added tests: a Cycle built for two run types in one scope selects child states through one selector; the iteration clock continues across Fresh, Retained and Shared turns; algorithm advice for another search state is skipped; HLib0001 and its fix cover algorithm hooks, consumer-defined roles and custom resolvers.

Validation: the Release build has no errors; all 3174 solution tests pass; the whitespace, style and analyzer format checks pass; the documentation builds; Prettier passes for the changed Markdown; packages are created.

**Review decisions (2026-10-25; accepted)**:

- **Algorithm resolution checks `Fits`**, which replaces the first deviation. A mismatch names every innermost configuration that does not fit, with its path, such as `Creator: RandomBoolVectorCreator was not written for an execution over …`, and names the algorithm only when it does not fit itself. A configuration that produces several search states declares each of them in `Fits`.
- **The iterative bases require `IterativeAlgorithmExecutor`**, which removes the first limit. The agnostic base narrows the hook with an abstract covariant override. The bound base derives from the bound `Algorithm` base, which checks the run types, implements `IIterativeAlgorithm` itself and declares its own `Interceptor`.
- **The second limit stays.** One advice instance counts every configuration its pointcut selects. A call is reported once per matching wrapper, so nested matches of one wildcard, such as a Cycle and its inner algorithm, each report it.
- **Execution state is renamed scoped state**, with `GetScopedState`, `CreateScopedState` and HLib0002's wording; the glossary lists execution state as legacy. Scoped state lives as long as the resolution scope that owns the configuration: a child scope starts new scoped state, and `With` keeps it.
- **Typed scopes derive from `ResolutionScope`.** `Scope` is removed, and `With` and `CreateChildScope` return the typed scope. Only the library derives from `ResolutionScope`. Because an instance method wins over an extension, the typed `TryResolve` extension calls the base resolution explicitly.
- **Hooks resolve each child into a local variable** named after its property, in build order (developer guidelines § 4.4).
- **Renames.** The executor factory analyzer and its code fix become `ChildResolutionAnalyzer` and `ChildResolutionCodeFixProvider`. Tests named after factories or preparation are named after resolution and scoped state. `IExecutor` has its own file.
- **Formatting is a separate step**: block bodies by default, expression bodies for forwarding members, and line breaks that keep a long signature's method name and body visible.

**Decision (2026-10-25; accepted)**: Own-role advice is found through a library base instead of the internal `IConfigurationNodeAdvice<TExecutor>`, as operator advice is. `ConfigurationNodeAdvice<TExecutor> : Advice<IConfigurationNode<TExecutor>>` is keyed on the executor type, the only type resolution knows, and only the library derives from it. `Advice<TConfiguration, TExecutor>` derives from it and passes the source to its `Wrap` after a type test, so authors write no cast. Its entry for resolution is the internal `WrapNode`, because a `Wrap` overload taking `IConfigurationNode<TExecutor>` would clash with the role's `Wrap` for an advice written for `IConfigurationNode<TExecutor>` itself. The compiler now checks the pairing with a pointcut against the executor type only: a pointcut selecting another role with the same executor type fails at resolution with a message naming both.

**Decision (2026-10-25; accepted)**: The two bound operator bases suppress Sonar S4019. Their leaf hook `CreateExecutor(ResolutionScope)` hides the run-generic hook because the typed scope now derives from `ResolutionScope`, which is intended: a bound operator builds only through the leaf hook, and the bound bases' call reaches it because overload resolution prefers the hook a class declares. Sonar reports the warning only at these two declarations, never in the operators deriving from them. The developer preferred this to renaming either hook, and rejected detaching the bound bases from the agnostic one (see the [backlog](developer-backlog.md#detaching-the-bound-operator-bases-from-the-agnostic-one)).

**Decision (2026-10-25; accepted)**: The internal `IDeclaresSupportedExecution`, which let the fit report say what each base is written for, is reverted (see the [backlog](developer-backlog.md#a-marker-interface-naming-what-a-configuration-is-written-for)). The report names each configuration at fault with its path, as in `Creator: RandomBoolVectorCreator was not written for an execution over …`.

**Decision (2026-10-26; accepted)**: A parameter or local holding an operator's scoped state is named `state` when it is the only state in reach, and `operatorState` next to search states, as in the stateful interceptor and terminator hooks. `GetScopedState`, `CreateScopedState` and the glossary entry keep the technical term.

**Decision (2026-10-26; accepted)**: The public static `ResolutionScope.TryResolve(Func<TExecutor>, …)` is replaced by `TryResolveOperator`, an instance method beside `ResolveOperator` with typed-scope extensions that take the executor interface from the `out` variable and cast once. An operation's `TryResolve` extension is `scope.TryResolveOperator(source, out executor, out reason)`, for built-in and consumer-defined operations alike. The algorithm `TryResolve` and `ResolveOptional` are instance methods beside `Resolve`. The try/catch helper is private. A public helper class was rejected as an API whose only job is a try/catch, and an internal helper because consumer-defined operations could not use it.

**Decision (2026-10-26; accepted)**: `ConfigurationNodeAdvice<TExecutor>` is removed, superseding the decision above that introduced it (see the [backlog](developer-backlog.md#a-library-advice-base-keyed-on-the-executor-type)). The root `Advice` has an internal `WrapNode(IExecutor, IConfigurationNode)` that returns null by default; `Advice<TConfiguration, TExecutor>` derives from `Advice<TConfiguration>` again and overrides it, sealed, with a type test of both arguments and a call to the author's `Wrap`. Resolution reports advice whose `WrapNode` returns no executor of the requested type. `Aspect.Create` now rejects advice for another role with the same executor type at compile time; an aspect that lists the pair itself is reported at resolution. Advice for a configuration's executor type also wraps a resolution of that configuration through a wider executor type, such as `IExecutor`, which was reported before. Deciding the call in `Aspect.Create` was rejected because it changes the public `(Pointcut, Advice)` pairs, and resolution generic in the configuration type because callers would name both types.

### Package 5 — Analysis, integrations and closure

Finish cross-operation candidate clocks, typed observation factories, trace integration and public examples. Add the source that ticks at the start of each turn of an inner algorithm, provisionally `algorithm.Calls`, and decide it against `cycle.Turns`; see Package 4's design pass. `TraceAnalyzer` stays in `Analysis`; counting/timing/observation advice stays in `Inspection`. Preserve source reference identity, clock-before-observation ordering, shared-clock deduplication and result ownership.

Audit all Experimental, PythonInterop, samples, `python-samples`, Roslyn analyzers/code fixes and architecture rules. Earlier packages migrate affected callers; this package proves complete coverage. In particular, update analyzer metadata checks for the common operator marker, managed-state restrictions and the new preparation hooks, with meaningful analyzer tests. Validate Python demonstrations separately because the solution does not include them; list any unavailable external runtime/fixture as an unvalidated integration rather than a pass.

Reconcile developer guidelines, glossary cross-references, architecture guides and the backlog entries identified in section 7. Publish final source/API inventories, measured costs and remaining limits. Remove temporary migration mechanisms. Do not add generic settings methods, compatibility shims or source generators.

**Gate**: Release solution build/tests, documentation/API examples, formatting/style/analyzers, documentation build and package creation pass using repository commands. Report final net source change, adapter/public-surface counts and performance results. There is no predetermined 4,000-line deletion quota and no unsupported “100% extensible” or universal zero-allocation claim.

## 10. Evidence, reproduction and review coverage

### 10.1 Evidence already available

The retained prototype under `plans/prototypes/operator-role-slice` remains useful historical evidence. Its latest functional rerun passed 56 construction assertions, 174 assertions in 11 scenario groups, external consumer checks, eight expected compiler failures and the isolated state-family checks. It still models algorithms as a role, uses both markers and has a redundant setting-method layer. Its state-family resolver installs no advice. Its uniform choice and rejection of a second operation are not the revised target behavior.

Additional review probes under ignored `artifacts/operator-role-review` reproduced the original invariant-envelope regression and the invalid cast for unsupported general advice. Focused solution probes were then compiled as separate core/consumer assemblies. Thirteen assertions passed for the common marker/recommendations, configuration-owned preparation, multiple operation/state views over one owner, independent owner state, shared live controls, general prepared advice on external descriptors, metric rejection before construction, multi-operation reuse and covariant reuse in both state-view orders through the shipping resolver.

Those solution probes are local working evidence, not a retained integrated test suite. Their small owner cache does not implement ancestry, weak lifetimes, aspect ordering or cycle detection, and they make no claims about those invariants. Port their successful cases into Package 0's committed integrated fixtures; do not describe the full revised foundation as verified until that gate passes. No new performance measurements establish the budgets yet.

The first small Package 0 checkpoint is retained under [operator-role-integrated](prototypes/operator-role-integrated/README.md), with its [report](prototypes/operator-role-integrated/checkpoint-1.md), runner and run snapshot. It compiles an isolated backend from current production sources, extending a generated partial copy of the shipping resolver without editing production files. The separate consumer passes 61 runtime assertions and four expected compiler failures for the public API/forwarding subset, multiple operation/state views, ancestry, contextual children, failure recovery, shared cycle detection and owner collectibility. Advice and the full synchronous protocol, remaining operations, native hosting, performance budgets and net source reduction remain unvalidated; Package 0 is incomplete and production migration remains unstarted.

The [second checkpoint](prototypes/operator-role-integrated/checkpoint-2.md) extends that isolated backend with general/typed prepared advice, weak per-owner advice keys, view-specific wrapper constructors and success adapters for the four existing built-in descriptors and external scalar operation. The combined runner passes the original 61 assertions plus 74 advice assertions and five expected compiler failures. It covers operation filtering, state-view rebinding in both orders, ordering/deduplication, live controls through names and inspection, failure recovery and advice collectibility. Detailed run snapshots are retained locally and excluded from version control. Lifecycle/policy profiles, candidate metric rejection, remaining operations, native hosting, performance budgets and net source reduction remain unvalidated; this does not complete Package 0.

The [third checkpoint](prototypes/operator-role-integrated/checkpoint-3.md) adds invocation-local lifecycle tokens, deterministic callback/cleanup failure semantics and actual `long`-token `TimeProvider` duration advice. The combined runner passes 198 runtime assertions and the same five expected compiler failures. A local three-process BenchmarkDotNet harness measures matched direct/shared counting, duration and three-layer stacks; the cheap counting/duration latency and zero-allocation gates pass. Detailed distributions, hashes, gate decisions and host information remain in ignored local output. These measured paths do not establish the numerical 2% gate, around policies, retry/choice, remaining operations, native hosting, preparation costs or net source reduction. Package 0 remains incomplete.

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
