# Operator state lifetimes

Status on 2026-11-05:

- **Implemented through step 3:** executor fields, explicit `Sharing`, default sharing within a scope, run-bound instruments and results, and observation through executor calls. Setup, resource supply and handles, observation points, per-trial aspect mappings and live-accumulator accounting factories are removed. Accumulating analyses create per-run results. See the [step 3 migration and validation record](operator-unification-step3-migration.md).
- **Accepted migration decisions:** the existing advised-case grid path, direct `Instrument<TResult>` accumulating definitions, and dynamic performance reading each observation's problem. The developer reviewed the implementation and authorized committing it on 2026-11-05.
- **Deferred:** the broad step 4 documentation rewrite and all simplification selections. Affected examples and directly contradictory documentation have been corrected; retained limitations are recorded in the migration plan.

The P0 through P5 completion and validation results in this plan apply to the earlier implementation, not the accepted default sharing design.

This plan supersedes the earlier recommendation to start with option 1.1 and the scoped state, ancestor reuse, child ownership and child scope policy decisions in the [operator unification plan](operator-role-parameterization-rework.md). Published architecture documentation describes executor fields, explicit sharing and run-bound instrument results. The glossary records that terminology.

### Provisional status of resource handles, recorded on 2026-11-03

The implementation was committed as a provisional checkpoint. Its parts have different standing:

- **Settled:** private mutable data in executor fields, removal of scoped state and the `Stateful*` families, execution-ready runs after setup, fresh Cycle turns by default with explicit executor reuse and the budget API revisions.
- **Provisional:** the resource handle mechanism, comprising `ResourceHandle<T>` and its subclasses, `RunResources`, `AddResource`, `AddResourcePerTrial`, `run.Resource(handle)` and the optional handles on caching, limit and accounting operators.

The developer reaffirmed the problem that handles address: independently constructed executors sometimes need one deliberately shared object, while reusable configurations must stay immutable and independent trials must not share data by default. Options 1.1 and 2 remain rejected for the reasons recorded below.

The developer is not satisfied with the handle mechanism itself:

1. It adds a separate concept for one need.
2. A resource can now be given in two ways: a supplied live object or a handle whose instance is created at runtime.
3. It is invasive: authors branch between private and shared data, and callers manually bind handles to the live objects that runtime advice receives.
4. `AlgorithmRun` now holds resource handling responsibility, which does not fit its role as the execution and lifecycle boundary.

Alternative designs that remove or reshape the mechanism are under discussion. Do not extend the handle mechanism to further operators or advice until that discussion concludes.

### Lifetime binding through `Sharing`, accepted on 2026-11-03

Status: the developer selected this direction on 2026-11-03 to replace the resource handle mechanism and `ReuseExecutors`, and it is implemented as explicit `Sharing`, recorded under [prototype results](#prototype-results-on-2026-11-03) and [migration](#migration-on-2026-11-03). Its public API and the name `Sharing` are accepted. Further decisions of 2026-11-03: a shared configuration shares its bare executor, and every requester applies its own advice around it, exactly as without sharing; `CycleAlgorithm.ReuseExecutors` is removed in favour of `Sharing`; sharing across runs is removed (detail 5); each sharing layer holds a flat table from configuration to executor, filled on first request, replacing the rejected per-type dictionary (detail 3). Detail 4 has a selected direction. Behavioral questions it must answer are in the [system spec](operator-unification-system-spec.md), especially S7, Q5 and Q7.

The fresh default in the rule below is superseded by [default sharing](#default-sharing-the-scope-is-the-unit-accepted-on-2026-11-03): within one scope a configuration reference resolves to one executor, and `Sharing` connects child scopes. The rule describes the implemented behavior until default sharing is implemented.

**Rule.** Resolving a configuration creates a fresh executor with fresh private fields. This is the default and matches the implemented behavior. A node can **bind** a configuration to its own lifetime. Within that node's subtree, every resolution of the bound configuration returns the single instance belonging to the binding node's executor. The configuration reference identifies what is shared, so no placeholder type is needed.

Proposed syntax:

```csharp
var cached = evaluator.Cached();           // the cache is a private executor field, no handle

var cycle = CycleAlgorithm.Create(
        algorithmA with { Evaluator = cached },
        algorithmB with { Evaluator = cached })
    .Sharing(cached);
```

One cache executor exists for the whole Cycle execution. A's and B's evaluators use it in every turn, while budgets and terminators inside A and B still restart each turn. A second run builds a new Cycle executor and therefore a new cache. Sharing a whole child algorithm, `cycle.Sharing(algorithmA)`, reproduces today's `ReuseExecutors` for that child only.

**What it would replace:**

- `ResourceHandle<T>` and its subclasses, `RunResources`, `AddResource`, `AddResourcePerTrial` and `run.Resource(handle)`. `AlgorithmRun` would no longer manage resources.
- The author-side choice between private and shared data. Operators keep all state in fields.
- `CycleAlgorithm.ReuseExecutors`.
- If aspects adopt a configuration/runtime split (spec Q1), the same rule decides the lifetime of aspect runtime parts. `WithAspects` creates them with its own executor. Binding keeps them across turns (spec Q5).

One concept replaces two existing ones, and it follows the composition principle already visible in budget placement (spec S6): placement decides lifetime.

**Relation to earlier alternatives.** It shares the "placement decides lifetime" idea with the binding node discussed on 2026-11-03, which bound a separate resource through a placeholder. Binding the configuration's executor directly removes that placeholder and is preferred. It differs from the rejected shared executor approach because the binding node, not the first requester, determines where the instance lives.

**Details to settle before acceptance:**

1. **Advice on a bound instance.** Accepted by the developer on 2026-11-03 and proven by the prototype. The bound node and its children are constructed once, in the binding node's context. Each requesting context's advice wraps the shared instance for that request. Binding M shares M itself, not the counting that `LimitedToInvocations` adds around it, so A's inner budget still counts only A's calls and restarts every turn.
2. **Granularity.** Binding shares a whole executor. To share only part of an operator's data, structure the configuration so the shared part is its own node, for example by binding the caching wrapper rather than the evaluator inside it.
3. **Typed views.** Decided by the developer on 2026-11-03: a sharing layer holds one executor per shared configuration. A request for another executor type, such as the same operator under a different search state, is an error rather than a second instance. Default sharing extends this rule to every scope table.
4. **Shared accounting with a separate terminator (spec S2).** Not solved by binding alone. `AfterOperatorCountTerminator` and `AfterOperatorDurationTerminator` read a counter that counting advice writes. A terminator is a leaf, so it cannot place that advice itself. Direction selected by the developer on 2026-11-04, not yet designed in detail: counting and timing become ordinary wrapper configurations, for example `var counted = evaluator.Counted()`, whose executor owns the counter. The algorithm uses `counted` in its slot and the terminator refers to the same `counted`. Under default sharing both resolve in one scope, because wrappers such as `TerminatedBy` open no boundary, so they receive the same executor without `Sharing` or handles, and the terminator reads the count through an executor control. Separate runs build separate scopes and therefore separate counters. This is the first concrete case of the [aspects as automatic composition](aspects-as-automatic-composition.md) design, questions 1 and 4, and reopens the 2026-10-14 backlog rejection of advice based configured wrappers. Once implemented, the remaining handles, `RunResources`, `AddResource`, `AddResourcePerTrial` and `run.Resource` are removed. **Replaced on 2026-11-04** by [run-bound results](#advice-and-scope-layers-under-discussion-since-2026-11-04): the counter is a result definition that the counting wrapper writes and the terminator reads, one per run. The run table stays as the result table, and the supply APIs are removed.
5. **Sharing across runs (spec Q7)** cannot be expressed by binding. Decided by the developer on 2026-11-03: `AddResource`, `AddResourcePerTrial`, `SuppliedResourceHandle` and the experiment supply are removed without replacement. All of them are new on this branch, so the result matches `main`, where every caching evaluator executor builds its own cache. Q7 moves to the developer backlog.
6. **Reading results after a run (spec Q5).** Open; governed by the [aspects design](aspects-as-automatic-composition.md), question 6.
7. **Prior exclusions.** This plan excluded nested resource lifetimes and ancestor lookup. Binding reintroduces nested lookup, explicitly. Unlike the removed scoped state, only explicitly bound configurations are shared, nothing is shared implicitly, and the result does not depend on resolution order.

#### Prototype results on 2026-11-03

**API accepted on 2026-11-03.** Sharing is declared on the configuration whose executor lifetime the sharing should follow. Its parameters are the configurations to share. Algorithms and operators both offer it, through `SharingAlgorithm` and `SharingOperator`. The method is named `Sharing`, confirmed by the developer on 2026-11-03.

```csharp
var shared = target.Sharing(configurationA, configurationB);
```

**Placement in the resolution scope.** The scope must take part: a wrapper only chooses the scope it hands to its child, and descendants resolve their own children through that scope. Sharing cannot be implemented as advice either. Advice wraps an executor after construction, innermost first, so replacing a fresh executor with the shared one would discard the requester's own advice, contrary to detail 1.

Sharing is therefore a second kind of scope layer context beside aspects. `With(aspects)` adds advice for the subtree resolved through the new layer. `Sharing(configurations)` adds shared configurations for that subtree. Each sharing layer holds one flat table from configuration reference to executor, its keys fixed by the `Sharing` call and each entry empty until its first request (accepted 2026-11-03, replacing a per-type dictionary). Resolution walks the same parent chain that it already walks for advice and returns the nearest layer's executor, constructing it in that layer on first request, without advice; the regular resolution then applies the requester's advice. A request that the stored executor does not fit, such as other run types, is an error. Building shared executors eagerly in the sharing node was rejected: the node receives untyped configurations, so it cannot choose the typed resolution its descendants will request, and building through the regular resolution would apply the advice above the sharing point twice and in reversed order. The lookup check is needed either way, because a descendant cannot know that a configuration is shared. There is no separate registry, identity mechanism or lifetime policy. After migration, these layers replace the flat run resource table, so the scope keeps one sharing mechanism rather than gaining a second.

Thirteen behavioral tests, now `ConfigurationSharingTests`, pass. All 3254 tests existing at the time still passed unchanged.

| Verified behavior                                               | Result                                                                                    |
| --------------------------------------------------------------- | ----------------------------------------------------------------------------------------- |
| Shared M in Cycle(A, B), two cycles                             | One M executor instead of four. Its private call count continues to 10                    |
| Two runs of the same configuration                              | Two independent shared executors, each counting 10                                        |
| Shared cache with a per-turn budget on the cache                | Same states as without sharing. Two inner evaluations instead of six                      |
| Shared M with `LimitedToInvocations(M, 5)` inside A             | Identical to S6 version 1: 16 calls, inner budget restarts every turn. M constructed once |
| `Cycle(limitedA, B).Sharing(limitedA, B)`                       | Identical log to the former `ReuseExecutors`. Sharing only `limitedA` keeps A, rebuilds B |
| Sharing inside A only                                           | A's two slots share one M per A turn. B and later A turns get new ones                    |
| Nested sharing points                                           | The nearest enclosing sharing point wins                                                  |
| `LimitEvaluator` allowance shared by a refiner and an evaluator | Same result as the handle-based test, without a handle                                    |
| Sharing operator with two child slots using M                   | One M per sharing operator executor, rebuilt with A each turn                             |
| Operator sharing inside algorithm sharing                       | The nearest sharing point wins across algorithms and operators                            |

**Finding: where a shared executor is built decides which advice reaches inside it.** Advice placed above the sharing point applies inside the shared executor: a run-level counter on the evaluator inside a shared cache counts the single cache miss. Advice placed between a requester and the sharing point wraps only the shared configuration itself on each request. In `Cycle(a.LimitedToInvocations(evaluator, 5), b).Sharing(cached)`, A's budget selects the evaluator inside the shared cache, which was constructed once at the sharing point, so the budget counts nothing and A runs all its states. A budget on `cached` itself counts A's calls and restarts every turn. Moving the budget above the sharing point counts every cache miss.

A first prototype rejected such advice with an error. That check needed per shared executor bookkeeping of everything constructed inside it and of every requester's advice, including children constructed later. It was removed to keep sharing a plain scope layer. A pointcut that selects nothing is already silent elsewhere in the library, for example a budget on an operator the algorithm never uses. If misplacement proves to be a real problem, it should be solved together with that general case, for example by reporting advice that never matched.

**Finding: data that must be shared independently of behavior belongs in its own configuration node.** Sharing shares a whole executor, with its children and the advice inside it. The handle design shared only the cache data, so each requester kept its own caching executor with its own advice around the inner evaluator. If that is required, the cache store can become a child configuration of the caching evaluator, with the store as its executor. Sharing the store then shares only the data. This turns resources into ordinary configuration nodes rather than a separate concept. Proposed, not accepted.

**Finding: sharing uses reference identity,** like pointcuts and observation. Calling `evaluator.Cached()` twice creates two equal but distinct configurations, and sharing one of them does not affect the other. Users must keep the shared configuration in a variable.

**Implementation notes:** the resolver's construction calls pass through a delegate that chooses the constructing layer, which `HLib0001` flags; the resolver is the one place that constructs executors, so the developer accepted local suppression on 2026-11-03. Sharing a configuration that the subtree never uses is silently accepted and constructs nothing.

#### Migration on 2026-11-03

Started by the developer for the decided parts. Done:

- `Sharing` storage is the flat table described above, with the one-type rule tested.
- `CachingEvaluator.Cache`, `DynamicCachingEvaluator.Cache`, `LimitEvaluator.Counter`, `EvaluationCacheHandle` and `DynamicEvaluationCacheHandle` are removed. The cache and the allowance are private executor fields again, and the tests that shared them through handles share the configuration instead.
- `CycleAlgorithm.ReuseExecutors`, the `reuseExecutors` parameters of `CycleWith` and the matching `CycleAlgorithmExecutor` constructor parameter are removed. Their tests use `Sharing`.
- The cross-trial cache supply in `OperatorPreparationSpecs` is removed with the cache handle.
- Execution resolution, execution nodes, operator implementation, developer guidelines § 4.3, the composition and meta-algorithm guides and the glossary describe sharing. The glossary gains a shared configuration entry.

Not done, because the terminators depend on it: `ResourceHandle<T>`, `CountAccumulatorHandle`, `DurationAccumulatorHandle`, `SuppliedResourceHandle`, `RunResources`, `scope.Resource`, `run.Resource`, `AddResource` and `AddResourcePerTrial` remain. `AddResource` is the only way to connect a terminator's handle to the counter that a counting or timing aspect writes, so removing the supply API now would leave `AfterOperatorCountTerminator` and `AfterOperatorDurationTerminator` unusable. They are removed together once detail 4 is settled, which the [aspects as automatic composition](aspects-as-automatic-composition.md) design now governs through its "Separate termination" scenario and question 4.

Done at the developer's request: `DynamicEvaluationCache<TKey>` is internal and takes its size limit and grace count in its constructor. Its former `Bind` check for participants with different source problems or grace counts could no longer fail, because each cache belongs to one executor. `EvaluationCache<TKey>` stays public: the published cache authoring example and the Experimental dynamic cache use it as a storage helper for operator authors.

Aspects keep their runtime data, such as counters and recordings, for now (developer, 2026-11-03). Moving that data into the executors of installed wrappers belongs to the [aspects as automatic composition](aspects-as-automatic-composition.md) design.

#### Default sharing: the scope is the unit, accepted on 2026-11-03

The developer questioned the fresh default, because users likely expect the same configuration reference inside one algorithm to be one executor. Requirements stated by the developer:

- Inner algorithms of a meta-algorithm stay isolated: each Cycle turn and each cycle gets its own executors, even for the same terminator reference.
- Algorithm wrappers such as `TerminatedBy` must not change what is shared. Wrapping an algorithm must not suddenly require `Sharing`.
- Isolation cannot rely on separate references, because a meta-algorithm calls the same configuration repeatedly.
- Sharing that spans algorithms is opted into with `Sharing`.

Models discussed:

1. **Fresh by default**, the behavior before this decision. `Sharing` was needed even inside one algorithm.
2. **Every algorithm resolution opens a unit**, and operators are shared by reference within it. Rejected: wrappers open units too, contradicting the second requirement.
3. **Shared by default with isolation boundaries.** Accepted.

Accepted rules (developer, 2026-11-03):

1. A resolution scope is the sharing unit. Within one scope, each configuration reference resolves to one executor. Using a reference twice in an algorithm, such as one evaluator in two slots, uses one executor and its private data.
2. A run's root is one scope. A meta-algorithm deliberately creates a child scope for each child activation, Cycle for every turn and Pipeline for every stage. A child scope keeps the advice and explicit sharing of its ancestors, but shares no other executors with its parent or its siblings.
3. Wrappers, such as `TerminatedBy` and the budget algorithms, resolve their child in their own scope. Wrapping does not introduce a lifetime boundary. Adding advice can still cause the context conflict in rule 5.
4. `Sharing` remains the opt-in for sharing across child scopes.
5. Requesting a configuration in one scope from two advice contexts is an error, for example a configuration used both inside and outside a budget wrapper. Silently choosing one context would make the advice inside the executor depend on resolution order. The developer accepted the error as practicable for now.

This overrides rules 2 and 6 of the [accepted decision](#problem-and-accepted-decision), which construct a configuration afresh for every request and exclude implicit sharing between resolutions. Rule 7 still holds: Cycle turns stay fresh because every turn resolves in a child scope. In part, it also overrides the 2026-10-13 backlog rejection of per-context node caches in the resolver; the backlog entry records the difference.

Implementation shape, accepted by the developer on 2026-11-03 and not yet implemented:

1. **Child scopes.** `scope.CreateChildScope()` returns a child scope. Meta-algorithms call it for every child activation: Cycle for each turn, Pipeline for each stage and `DynamicRacingAlgorithm` for each contender, before its `With`. The name says that scopes are the units of isolation. `CreateIsolatedScope()` was considered. `Isolated()` was rejected because it does not say that a scope is created. Glossary term: child scope. "Child scope policy" stays `Legacy`. Name confirmed by the developer on 2026-11-04.
2. **Table key.** Each scope has one table, keyed by configuration reference alone. An entry holds the executor without advice and the layer that built it. Every request applies its own advice around that executor. This shares the underlying executor, not the wrappers created for each request. Built in counting advice passes the same accumulator to its wrappers, but consumer advice can create wrappers with private mutable data. A configuration offering two operations has one underlying executor and one set of private fields per scope. A request for other run types stays an error.
3. **Advice context means the building layer.** Superseded on 2026-11-04 by [advice belonging to the scope](#advice-and-scope-layers-under-discussion-since-2026-11-04); kept for the record. `With` and `Sharing` views are layers of the same scope; a typed view counts as the layer it views. For a configuration shared by default, a request from a layer other than the one that built the entry is an error. `Sharing` layers count, because the layer chain also decides which shared children an executor gets. For an explicitly shared configuration, the sharing layer is the builder (rule 4), and requests from any layer below it are legal and apply their own advice around the entry, as detail 1 requires; `Cycle(a.LimitedToInvocations(m, 5), b).Sharing(m)` is the reference case. False positives, where the extra advice would not touch the configuration, are accepted. The error names the remedy, sharing the configuration on a node enclosing both uses, which fixes the building layer for both. It also states the consequence: advice added below that sharing point still wraps the configuration itself, but no longer reaches the children inside it. Wording clarified and accepted by the developer on 2026-11-04.
4. **Explicit sharing uses the same table.** The nearest `Sharing` layer listing a configuration builds it. The entry is stored in the table of that layer's scope, with the sharing layer as its builder, and is visible from child scopes below it. A shared configuration also used outside the sharing node in the same scope is therefore an error, as is nested `Sharing` of one configuration within one scope. Across child scopes the nearest sharing point still wins.
5. **`SharingOperator` is removed.** Operators never create child scopes, so inside one scope it could only move the building layer, which changes advice placement and adds conflicts. `SharingAlgorithm` stays as the opt-in for sharing across child activations. Reintroduce operator sharing if an operator ever creates child scopes.

The developer assigns the use of child scopes to meta-algorithm authors: a meta-algorithm that resolves child activations without `CreateChildScope()` shares their executors, and knowing what scopes do is part of writing one. The meta-algorithm guide documents this; no analyzer is planned.

Unchanged: every run and every trial already builds its own root scope, and `MetaOptimizationProblem` builds one per evaluation.

##### Advice and scope layers, under discussion since 2026-11-04

Deferred at the developer's request on 2026-11-03 until the sharing rules were settled, and reopened on 2026-11-04: whether advice should keep using resolution scope layers, or scopes should be redesigned for aspects. The building layer rule (rule 3) exists only because advice layers are finer than the sharing unit, so implementation of default sharing waits for this discussion. The gaps recorded in the [aspects plan](aspects-as-automatic-composition.md#review-findings-in-the-current-implementation) are discussed together with it.

Facts established on 2026-11-04: under default sharing, run aspects apply to the run's root scope and every meta-algorithm child activation gets a child scope. The only library feature that adds advice to part of a scope is the operator budget family, through `scope.With(aspect)`; `DynamicRacingAlgorithm` adds its tracking advice for a contender that gets its own child scope anyway. Subtree advice combined with sharing by reference is what forces the building layer rule, because a shared executor's children can be built under one set of advice only.

**Decided by the developer on 2026-11-04: advice belongs to the scope.** Options considered: keeping subtree advice with the building layer rule, advice belonging to the scope, and automatically substituting wrapper configurations within a subtree, which would have reopened the 2026-10-14 rejection of advice expressed as wrapper configurations.

- Adding aspects to a scope adds them to that scope's list instead of creating a layer. They apply to every later resolution in the scope and in its child scopes. Later added advice wraps closer to the executor, so a budget's counting stays inside run-level analysis. The complete ordering rule, corrected after the review below, is recorded under [design review](#design-review-on-2026-11-04).
- Layers and the building layer rule (implementation shape rule 3) are removed. A scope is its table, its aspect list and its parent.
- A budget counts every call of its operator in its scope, including calls from other wrappers outside the budget in the same scope, such as a `TerminatedBy` terminator. This differs from subtree advice only in configurations that the building layer rule would have rejected.
- One guard remains: adding advice that selects an executor the scope has already built is an error. In that rare case the outcome depends on resolution order: if something outside the budget builds the operator first, the error is raised; otherwise the outside calls are counted.

**Further decisions by the developer on 2026-11-04:**

- **Sharing belongs to the scope.** `Sharing(x)` marks `x` in the scope where the sharing node is resolved. `x` is built there with that scope's advice and is visible to its child scopes, each of which applies its own advice around it. Refined in [step 1](#step-1-decisions-accepted-on-2026-11-04): `x` is built in a child scope of its own that inherits the sharing scope's advice. Implementation shape rule 4's errors for using `x` inside and outside the sharing node, and for nested sharing, disappear, because one scope has one entry anyway. Explicit sharing matters only across child scopes, and the nearest sharing scope wins.
- **Authoring API.** `scope.AddAspect(aspect)` adds advice to the current scope, and `scope.CreateChildScope(params aspects)` creates a child scope that starts with its own aspects. Narrowed in [step 1](#step-1-decisions-accepted-on-2026-11-04): `CreateChildScope()` and `Create()` take no aspects, so `AddAspect` is the only scope-level way to add advice. `With` is removed: it would now mutate, and developer guidelines § 8.12 reserves `With*` for copies. Both stay public so third-party budgets and the Experimental racing algorithm use exactly what built-in budgets use. They are machinery for node authors; users attach aspects at run setup, or through configurations whose nodes call this API.
- **Failure ends the scope tree.** All scopes of one run share one construction guard. When a `CreateExecutor`, an advice `Wrap` or the executor type check throws, the guard is marked failed, and every later resolution through that tree, child scopes included, throws and asks for a new scope. Rejections before anything is built, the signature check and the advice `Check`, change nothing and stay retryable; `TryResolve` reports only those, and construction failures propagate. No half-advised executor or stale aspect is reachable. This replaces [review follow up 2](#review-follow-ups-before-implementation). About ten retry tests move to a new scope, and the execution resolution sentence on retrying a failed construction through a direct scope changes.

- **Scope-level sharing method.** `scope.Share(configurations)` replaces `scope.Sharing(...)`, because it now changes the scope like `AddAspect`. The user-facing `algorithm.Sharing(...)` is unchanged.
- **Executors own collected data,** narrowed to execution data by the seventh round below. Explicit wrapper configurations and attached traces describe what to collect: what to count, which value to trace, retention and clocks. Each executor gets fresh data, so lifetimes follow the sharing rules: one data set per scope, per run at the root, per turn inside Cycle and across turns with `cycle.Sharing(...)`. Results are read from the run afterwards; the API is open. A caller-supplied callback such as `Observed(log)` stays allowed as the caller's deliberate sink, which covers the "one sink for many runs" case of spec Q7. Caller-owned collectors placed in configurations were rejected, because reusing the configuration would write every run into one collector, the reason option 1.1 was rejected.

- **Attachment through composition.** Users attach aspects through an algorithm wrapper, name open and not `With*`, whose node calls `scope.AddAspect` as budgets do. Its aspects apply to the scope it is resolved in: the run at the root, one child activation inside a meta-algorithm. For users, attaching analysis is then composition like everything else. The developer wants to remove run setup if nothing else needs it.
- **Results published by executors,** superseded by the seventh round below; reading results from the run by definition remains. Executors that collect data publish it, and the run answers queries keyed by the definition the user configured, matched by reference: a trace definition added to an algorithm is the key for reading that trace's results. The run keeps the published results, not the executors.

**Decided by the developer in the fifth round on 2026-11-04:**

- **Collectors are configurations,** superseded by the seventh round below. A trace or clock definition is a configuration whose executor is the collector. Everything that uses it resolves it like any child: the attachment wrapper, an explicit `Observed(definition)` wrapper and a terminator that reads it. Its lifetime therefore follows the ordinary rules: one collector per scope, a fresh one per Cycle turn and one across turns with `cycle.Sharing(definition)`. Two traces with one clock share one clock executor within a scope, so a clock ticks once because of sharing rather than an advice duplicate rule; this may settle part of spec Q4. The collector publishes its results to the run under its definition. Rejected: every wrapper creating its own collector, which would give two collectors when one definition is attached twice in a scope.
- **Run setup is removed.** `AlgorithmRunSetup`, `ExperimentRunSetup`, the `setup` parameter of `CreateRun` for runs and experiments, `AddAspect`, `AddAspectPerTrial`, `AddResource`, `AddResourcePerTrial` and the per-trial aspect handles go in the change that delivers their replacements: attachment through the algorithm wrapper, counting and timing configurations instead of accounting handles ([detail 4](#lifetime-binding-through-sharing-accepted-on-2026-11-03)) and results read from the run. Per-trial variants are unnecessary because executor-owned data already makes trials independent. `RunTraceExtensions` and the three PythonInterop calls move to the wrapper. `ResolutionScope.Create(aspects)` stays for building executors directly. Refined in [step 1](#step-1-decisions-accepted-on-2026-11-04): `ResolutionScope.Create()` followed by `AddAspect` stays, and `ResolutionScopeBuilder` goes with run setup.
- **Cycle interceptors replace observation points,** recorded in the [aspects plan](aspects-as-automatic-composition.md#trace-collection-separated-from-attachment-accepted-on-2026-11-04).

**Sixth round on 2026-11-04:**

- **Attachment wrapper name.** Decided by the developer: `Advised`, as in `algorithm.Advised(aspects)`.
- **Results reopened.** The developer reopened whether collected results are bound to executors, because a user who asks a Cycle to trace every occurrence of an operator expects one trace for the run and should not need `Sharing` or knowledge of executors for it. The [aspect configuration lifetime review](#aspect-configuration-lifetime-review-on-2026-10-29) had recommended one collector per handle per run.

**Decided by the developer in the seventh round on 2026-11-04: results are bound to the run.** Execution data and results are separate systems.

- **Execution data** changes what an algorithm does, such as adaptive step sizes, a budget wrapper's private counter or a cache. It belongs to executors and follows the sharing rules.
- **Results** are what users read during and after a run: traces, counts, durations and clocks. A run holds one result per definition, matched by reference and created at its first lookup. Every wrapper that records into a definition, whether explicit or installed by advice, writes into that one result, independently of scopes, turns and `Sharing`. `run.Result(definition)` returns it; there is no list of results per definition. Per-turn breakdowns come from clock values in the samples. Each experiment trial and each nested run has its own results, and results survive a failure.
- Explicit wrappers and advice look a result up through a public scope method when they are built, so third-party collectors use the same path as built-in ones. Today's run resource table is the mechanism, without the supply APIs.
- A count or duration terminator reads a run-bound result: `evaluator.Counted(evaluations)` with `AfterCount(evaluations, 10_000)` limits the whole run, also inside Cycle turns. A per-turn budget uses a budget wrapper, whose counter is private execution data. This replaces the direction of [detail 4](#lifetime-binding-through-sharing-accepted-on-2026-11-03).
- Superseded: executor-owned collected data, now narrowed to execution data; collectors as configurations; publication by executors; and the `Results` list proposed in the sixth round. Rejected: one result per collector executor, and results whose lifetime depends on where the wrapper is placed.

**Decided by the developer in the eighth round on 2026-11-04:**

- **Three parts, no event system.** Execution is configurations, executors and the sharing rules. Results are the run table, one result per definition. Observation connects them: a wrapper around an executor call records into a result, explicitly through `Observed(...)` or automatically through `Advised(...)`. Observation attaches only to executor calls: operator calls, algorithm calls and the states an algorithm call yields. Turns, pipeline stages, racing contenders and restarts are calls of child algorithms. Values inside an executor are read through its controls by a wrapper around it. An internal moment worth observing that is none of these becomes an operator slot. A separate event system, such as observation points, is not added; the [backlog](developer-backlog.md#observation-points-as-a-separate-observation-concept) records why.
- **Elapsed time** starts at run start and counts only while the run executes, so paused time is excluded. Callers time wall-clock duration themselves around the run. This settles the open origin of the [2026-10-29 review](#aspect-configuration-lifetime-review-on-2026-10-29).
- Cycle gets no interceptor slots; recorded in the [aspects plan](aspects-as-automatic-composition.md#trace-collection-separated-from-attachment-accepted-on-2026-11-04).

**Decided by the developer in the ninth round on 2026-11-04:**

- **One aspect per definition per run.** For each definition attached through `Advised`, the run creates one aspect bound to that definition's result and keeps it in its table next to the result. `Advised` adds that aspect to its scope, so an `Advised` resolved again in every Cycle turn adds the same aspect. The advice receives the result through its constructor and `Wrap` stays unchanged, preserving the 2026-10-29 rejection of [run data in every wrapping hook](developer-backlog.md#resource-lookup-on-every-advice-wrapping-hook). The existing removal of duplicate advice by reference then makes a definition attached twice record once and a clock shared by two traces tick once. A plain `Aspect` with a caller-owned callback is added as it is. This explicitly accepts the per-run runtime aspect that the 2026-10-29 discussion left unauthorized. Rejected: passing run results to every `Wrap`, and live results created by callers and placed in configurations.
- **Definitions name their source.** `mutator.Invocations.Trace(...)` and `evaluator.Invocations.Count()` create definitions over an observation source, so the common case is one line. `Advised(definition)` places a definition at its source. `x.Observed(definition)`, `x.Counted(definition)` and `x.Timed(definition)` place it where they are written, for any configuration whose observation type fits; the source then matters only for `Advised`. Rejected: definitions without a location, which make both paths name it.
- **Clocks are results,** widened after the [design review](#design-review-on-2026-11-04) to an open clock contract. A clock is a counter, a duration or `Clock.Elapsed`, the run's active time. A trace records its clocks' current values with each sample, and `ObservationClock` and `ElapsedTimeClock` stop being types of their own. `Advised(trace)` also attaches the trace's clocks; an explicit `Observed(trace)` places only the trace, so its counting clocks must be attached or placed as well.
- **Observation sources stay.** `Invocations` and `Iterations` describe the calls of one configuration, an executor-call boundary rather than an observation point. Only the point-based sources such as today's `cycle.Turns` and `cycle.Cycles` are removed; a `cycle.Turns` convenience may return as a source over the inner algorithms' calls.
- **Spec Q3, Q4, S9 and Q6** are accepted, recorded in the [system spec](operator-unification-system-spec.md#next-discussion).

**Raised by the developer on the same day:** a trace definition with a value function, such as `o => o.Offspring.Count`, holds a delegate. Passing it to `Advised` puts the delegate into the algorithm configuration, which developer guidelines § 4.8 forbids because delegates are not persistable. The round-4 allowance of a caller callback such as `Observed(log)` in a configuration has the same problem.

**Decided by the developer in the tenth round on 2026-11-04:**

- **Delegates are allowed in definitions for now.** Trace values and callbacks may be delegates, and `Advised` places them into the algorithm configuration, so an algorithm with such analysis attached cannot be persisted. This is a temporary exception to developer guidelines § 4.8, accepted to keep this branch in scope; the redesign is a [backlog item](developer-backlog.md#runs-execution-and-experiments). No run-time attachment list is added, so run setup is removed as decided. When implemented, § 4.8 names this exception and links the backlog item.
- **Counting and timing.** `x.Invocations.Count()` counts completed calls, `x.Invocations.CountCandidates()` counts candidate work and is rejected for operations without it, `x.Invocations.Duration()` measures time inside calls including failed attempts, and `a.Iterations.Count()` counts iterations. `x.Counted(counter)` and `x.Timed(duration)` place them explicitly. `AfterOperatorCountTerminator(counter, limit)` and `AfterOperatorDurationTerminator(duration, limit)` read the run-bound value, which accumulates over the whole run; a terminator still stops only the stream it is placed on. Budget wrappers such as `LimitedToInvocations`, `OperatorBudgetAlgorithm` and `OperatorDurationBudgetAlgorithm` keep a private counter as execution data, one per scope, so inside a Cycle turn they restart every turn. Removed: `ResourceHandle<T>`, `CountAccumulatorHandle`, `DurationAccumulatorHandle`, `SuppliedResourceHandle`, `run.Resource`, the public `RunResources`, which becomes the internal result table, and the fluent `CountInvocations(counter)`, `CountCandidates(counter)` and `MeasureDuration(duration)` with live accumulators, including their `out var counter` forms. A counter that nothing attaches or places stays at zero, so its terminator never stops the run; resolution cannot detect this reliably, so it is documented.

No question of this discussion remains open. The developer accepted the consolidated design together with the design review below on 2026-11-04.

##### Design review on 2026-11-04

At the developer's request an independent reviewer checked the decisions above against the code and the repository standards and reported six major findings and one minor one. All were confirmed against the code. The developer accepted the resolutions on the same day.

1. **Fresh collector state per run.** Run-bound storage alone left mutable parts of a definition shared: `TraceBestSoFar` creates its `BestSoFar` when the trace is defined, and `EveryNthRetention` keeps its own counter. A definition therefore holds only immutable settings, and each run builds everything that changes from it: entries, retention progress, best-so-far state and the graph and history of an accumulating analysis such as `RankAnalyzer`. Retention becomes an immutable policy whose progress belongs to the result. State captured by a caller's own delegate remains the caller's, under the temporary § 4.8 exception.
2. **Observation sources describe wrappers.** `ObservationSource.Create` took a function producing an aspect, which gave the explicit path no way to build an operation's observation wrapper. A source is instead a configuration and a function that wraps an executor so that each call produces an observation. Observation advice and `x.Observed(d)` both call it, and third-party operations supply it through the public `ObservationSource.Create` exactly as built-in operations do. Evidence: an operation defined outside the library, traced explicitly and through `Advised`.
3. **Algorithm-call observations.** `Iterations` reports only yielded states, so it shows neither the start of a turn nor a turn that yields nothing. A source of algorithm calls is added next to `Iterations`, for example `algorithm.Calls` with the name open. It reports when a call starts and when it ends, with the outcome completed, failed or stopped early. `Calls.Count()` counts started calls, so a turn's number is known when it begins and a stopped turn still counts; operator counts keep counting completed calls (spec S9), because an algorithm call spans time.
4. **One ordering rule.** The earlier statement that later added advice wraps closer contradicted spec Q4 and today's rule within a scope, where earlier advice is closer. From the advice closest to the executor outward: advice of a child scope comes before its parent's, as today; within a scope, later added aspects come first, so `X.Advised(a).Advised(b)`, whose outer node adds `b` first, puts `a` closer to X as the configuration nests and keeps a budget's counting inside run-level analysis; within one aspect the pairs keep their listed order, and a trace lists its clocks first; advice is matched to the target first, and of duplicates the closest occurrence is kept. Two traces sharing clock C then always order C before their observations, wherever and in whichever order they were attached, so both read C after it ticked. Spec Q4 and the custom advice guide's "earlier advice wraps closer" change with it.
5. **Open clock contract.** "A clock is a counter, a duration or `Clock.Elapsed`" excluded domain clocks such as the Experimental `EpochClock`, which reads `Problem.CurrentEpoch` and is checked by a scenario test. A clock is anything a trace reads at each sample. The library supplies counters, durations and `Clock.Elapsed`, and custom clocks keep implementing `Clock<T>` outside the core package.
6. **Per-trial aspects.** `AddAspectPerTrial`, released on `main`, also served to build callbacks tagged with their trial for live output. The developer accepted removing it: per-trial results are read from each trial's run afterwards, and live per-trial tagging waits in the [backlog](developer-backlog.md#scenarios-awaiting-a-concrete-need), with a per-run factory receiving the trial as its candidate shape. Developer guidelines § 1.1 permits the breaking change.
7. **Spec S8.** Its promise that one trace passed to several runs collects all of them is superseded by run-bound results; its within-run expectation stays.

Clarifications accepted with the review:

- **Elapsed time** counts only time spent inside the root algorithm's execution, that is, inside its `MoveNext` calls, which excludes pauses and the time a consumer spends between streamed states. The root scope owns the result table and the elapsed clock, so direct resolution without an `AlgorithmRun`, such as `MetaOptimizationProblem`'s, works the same.
- **Guard.** The already-built guard checks only advice that becomes newly effective after matching and duplicate removal, so adding a definition again is a no-op even after its target was built. Adding advice to a scope that already has child scopes is an error, so a running turn never sees its advice change.
- **Definition identity.** Definitions are classes with reference identity (developer guidelines § 4.9). Calling `evaluator.Invocations.Count()` again creates a new definition with its own result, so callers keep the variable.
- **Termination.** A run-bound counter accumulates over the whole run, but a terminator stops only the stream it is placed on; to stop the run, place it on the root. Document that a Cycle without `MaximumCycles` keeps cycling when an exhausted inner terminator ends every turn immediately.
- **Layering.** The Execution layer defines the definition contract, creating the per-run result and its aspect, and the run table publishes the pair together. Analysis implements traces on it, so resolution does not depend on Analysis. Each result type documents whether it can be read during the run: traces through snapshots, accumulating analyses after the run.

The review found the scope model, explicit sharing, one construction hook, the separation of execution data from results, constructor-bound advice dependencies, the budget rules and the interceptor decisions sound. The limitation that child-scope advice cannot reach inside an executor shared from an ancestor stays explicit.

##### Review follow ups before implementation

Review findings recorded on 2026-11-03, with the developer's decisions of 2026-11-04. The scope model remains accepted.

1. **Construction across operations.** `IOperator<TOperation, TCandidate>` permits separate explicit `CreateExecutor` implementations on one configuration. The `CountingSource` fixture in `OperatorPreparationSpecs.Resources.cs` uses that form. Its hooks happen to return the same executor shape, but the public contract does not require equivalent initialization or support for every advertised operation on each returned executor. Establish how those hooks satisfy the accepted single executor identity. Checking the requested interface alone does not prove that the first operation resolved cannot change initialization. Required evidence: resolve both operations in either order and verify the same intended state and behavior.

   **Developer, 2026-11-04:** one configuration with two construction hooks, as in `CountingSource`, describes two operators and must not be allowed; what happens when a consumer implements both anyway still needs an answer. Otherwise the review recommendation is accepted: an author contract plus the existing fit check, and the evidence above.

   **Decided by the developer on 2026-11-04:** multi-operation configurations stay, as the [operator rework](operator-role-parameterization-rework.md#33-multi-operation-configurations) requires, with exactly one construction hook. The established form implements `CreateExecutor` once as a public method, because the hook's signature does not name the operation. A configuration that implements the hook separately for several operations is rejected at its first resolution with an error naming the operations, found by a per-type check of the interface mapping, so the result does not depend on which operation is requested first. `CountingSource` moves to the single hook form. Forbidding multi-operation configurations and keying the table by operation were rejected.

2. **Failure and retry.** Consider a budget whose first child is constructed in its `With` layer before a later child throws. Retrying the budget creates another layer, while a retained child entry still names the first layer. The proposed conflict rule could reject that retry. Current direct resolution permits retry after construction failure. Decide what survives failure and whether the scope remains usable. Required evidence: retry a partially constructed budget and a failed advice wrap, with explicit expectations for retained children and private data. The partially constructed budget case is a reasoned risk, not a reproduced failure of the unimplemented design.

   **Reopened by the developer on 2026-11-04,** after advice moved into the scope: no executor may keep half its advice, and nothing may report into an aspect left behind by a failed attempt. The decision below no longer holds, because a failed budget would leave its aspect in the scope's list. **Replaced the same day:** a construction failure ends the scope tree, recorded under [advice and scope layers](#advice-and-scope-layers-under-discussion-since-2026-11-04). Rolling back the failed attempt's entries and aspects was the rejected alternative.

   Superseded decision of 2026-11-04: no rollback. Executors constructed before a failure stay in the scope table, and a later attempt reuses them. Consequences that were recorded:
   - A retry from the same layer, such as a `TryResolve` fallback or a second direct `Resolve`, reuses the retained executors. Only the configurations that failed are constructed again, so retained private data continues rather than restarting.
   - A retry that creates a new advice layer, such as resolving a budget again, requests the retained executors from another building layer and raises the rule 3 error. Their children were built with the first attempt's advice, which would no longer be counted, so reuse there would be silently wrong. Such a retry needs a new scope.
   - The sentence in [execution resolution](../docs/contributing/architecture/execution-resolution.md#controls-failures-and-lifetime) that promises new private data on another attempt changes accordingly.

3. **Wrapper state.** Sharing a bare executor while applying advice per request does not necessarily behave like sharing one fully advised executor. Consumer advice may allocate private wrapper data. Establish the intended lifetime of that data without assuming every wrapper is stateless. Required evidence: two resolutions in the same context share the underlying executor while a stateful advice wrapper exhibits the explicitly chosen lifetime. Carry that expectation into the manual and automatic composition equivalence scenarios in the [aspects plan](aspects-as-automatic-composition.md).

   **Decided by the developer on 2026-11-04:** advice wrappers are created per request and keep no private data. Data that must accumulate belongs to the advice or to an object it receives, such as a `CountAccumulator`. Documented in execution resolution, the glossary entry for join points and the custom advice guide, because explicit `Sharing` already applies advice per request.

The accepted context conflict diagnostic also needs a qualification: moving `Sharing(X)` to an enclosing node fixes where X is built, but advice below that point cannot reach X's children. The suggested remedy must explain that consequence rather than imply that moving sharing always preserves budget or observation behavior. Accepted on 2026-11-04 and recorded in rule 3.

The [aspect configuration lifetime review](#aspect-configuration-lifetime-review-on-2026-10-29) records a later proposal, not an accepted extension of this implementation. Its placement and typed API discussion lives in [operator rework section 11](operator-role-parameterization-rework.md#11-aspect-configuration-rework-and-independent-review).

##### Implementation sequence, confirmed on 2026-11-04

The developer confirmed this order in principle. Each step builds and passes its tests on its own, and each is reviewed by staging before it is committed.

1. **Scope model.** Default sharing; advice and sharing in the scope through `scope.AddAspect`, `scope.Share` and `scope.CreateChildScope()`, replacing `With` and `scope.Sharing`; child scopes in Cycle turns, Pipeline stages and racing contenders; a construction failure ending the scope tree; the single construction hook check; the ordering and guard rules of the [design review](#design-review-on-2026-11-04); removal of `SharingOperator`; and the retry tests moved to a new scope.
2. **Results and observation, added alongside the old APIs.**
   - The run result table and the Execution-layer definition contract.
   - Definitions for traces, counters, durations and clocks, with per-run collector state.
   - Observation sources as wrappers, and the algorithm-call source.
   - `Advised`, `Observed`, `Counted`, `Timed` and `Clock.Elapsed`.
   - Count and duration terminators reading run-bound results.
3. **Removals and migration.** Remove run setup, `ResolutionScopeBuilder`, resource handles, observation points, Cycle points and per-trial aspects. Migrate `RunTraceExtensions`, PythonInterop, tests, samples and documentation snippets.
4. **Documentation.** Architecture docs, guides and the glossary; the temporary § 4.8 exception in the developer guidelines, linking the [backlog item](developer-backlog.md#runs-execution-and-experiments); the custom advice guide's ordering sentence.

Names still to confirm with the developer when their step reaches them: the algorithm-call source (placeholder `algorithm.Calls`), the definition and result type names, and `Clock.Elapsed`. `Advised`, `Observed`, `Counted` and `Timed` are accepted.

**Baseline before step 1.** At `c0563bb4`, restore and the Release build pass with existing warnings and no errors, all 3266 solution tests pass, and the whitespace, style and error-severity analyzer checks pass.

##### Step 1 decisions, accepted on 2026-11-04

**Where an explicitly shared executor is built.** A shared configuration is built on its first request, which usually comes from a child scope such as a Cycle turn, so the sharing scope already has child scopes by then. Building it literally in the sharing scope, as the [scope model](#advice-and-scope-layers-under-discussion-since-2026-11-04) first stated, contradicted accepted behavior in two ways. A budget's `CreateExecutor` adds its advice to the scope it is built in, which the [design review](#design-review-on-2026-11-04) guard rejects once that scope has child scopes, so `Cycle(limitedA, B).Sharing(limitedA)` failed although the [verified behavior table](#prototype-results-on-2026-11-03) accepts it. Shared siblings built in one scope also shared their children, so `Cycle(A, B).Sharing(A, B)` no longer gave the former `ReuseExecutors` log.

The developer accepted building each shared configuration in a new child scope of the sharing scope, the scope it would have had as a child activation, which lives as long as the sharing scope. Its entry stays in the sharing scope's table. It receives the sharing scope's advice, while the advice its own construction adds and the children it builds stay its own, so shared configurations share a child only when that child is shared too. Sharing therefore changes how long an executor lives and never which advice reaches it, as [detail 1](#lifetime-binding-through-sharing-accepted-on-2026-11-03) requires. The developer may simplify this ruling later. The rejected alternatives are recorded in the [backlog](developer-backlog.md#building-shared-executors-in-the-sharing-scope-itself).

**Implementation choices confirmed by the developer:**

- `Share` is an error in a scope that has child scopes. Sharing a configuration again changes nothing.
- `AddAspect` puts the added aspect closest to the executor. Adding an aspect the scope already has changes nothing.
- `AddAspect` and `Share` return nothing, and `AddAspect` takes one aspect.
- The single construction hook check applies to operators. Consumer roles and algorithms with several search states rely on the rejection of a request for other run types.
- Pointcut matching precedes construction for every role, so a pointcut fault is a retryable rejection.
- A rejection inside a parent's construction ends the scope tree, because the parent's construction failed.
- An existing executor that does not fit a request, such as one built for other run types, is a retryable rejection.
- Accepted limitations: the guard may reject typed advice for another operation of a configuration already built, and its message addresses node authors rather than users who meet it through a budget.

**One way to add advice.** Step 1 first kept aspect parameters on `ResolutionScope.Create` and `CreateChildScope`, which applied in listed order, beside `AddAspect`, which wraps closest. Node authors then had two ways with opposite orders. The developer decided:

- `AddAspect` is the only scope-level way to add advice. `ResolutionScope.Create()` and `CreateChildScope()` take no aspects, and of the aspects a scope receives, the one added last wraps closest to the executor.
- Users attach advice to an algorithm through `Advised`, delivered in step 2. Until run setup and `ResolutionScopeBuilder` are removed in step 3, `ResolutionScopeBuilder.Build` adds its aspects last to first, so the first aspect attached at run setup still wraps closest and observes first, as with nested `Advised`; an aspect attached twice keeps its first position.

**Sharing before first resolution.** A configuration the scope had already resolved, whether it built that executor itself or used it from an enclosing scope's sharing, was shared as resolved, so whether a later `Share` took effect depended on the order of resolution. The developer decided that sharing a configuration the scope has already resolved is an error. A configuration counts as resolved once its resolution starts, on every resolution path, so its own construction can neither share it nor add advice for it. It concerns mainly meta-algorithm authors, who share before resolving anything that uses the configuration, as `Sharing` does before it resolves its child.

##### Step 2 decisions, accepted on 2026-11-04

**Coexistence.** Observation sources, traces, clocks, retention and the operator count and duration terminators change in place, and their callers move in step 2: each name must carry its new meaning, and a second implementation under another name would be a second source of truth. Run setup, `ResolutionScopeBuilder`, the resource handles and their supply, observation points, per-trial aspects and the counting and timing aspects with live accumulators keep attaching plain aspects until step 3 removes them. `RunTraceExtensions` and the three PythonInterop calls therefore move in step 2 rather than step 3. Published prose waits for step 4. Rejected: a bridge that lets run setup attach instruments, and temporary parallel types; see the [backlog](developer-backlog.md#bridging-run-setup-to-instruments).

**Instruments.** The definition contract is `Instrument<TResult>` in the Execution layer, with a non-generic `Instrument` base. It succeeds `ResourceHandle<TResource>`: one result per instrument per run, created at its first lookup and matched by reference, without supply, plus the aspect that places the instrument at its source. `CreateResult(RunResults)` creates the result, and `CreateAspect(result, RunResults)` creates that aspect or returns null. In step 2 instruments and the remaining handles share one run table; step 3 removes the handles. `RunResults` is the narrow public view the hooks receive: `Result(instrument)`, `Aspect(instrument)`, which returns the run's single aspect for it, and `Elapsed`. Nodes reach it through `ResolutionScope.Results`, and callers through `AlgorithmRun.Result(instrument)`. `ObservationPoint` is not a second form of this concept: it is an observation source, the producer side, and goes in step 3. Passing the resolution scope to the hooks was rejected, because they could then resolve or add advice while a construction lock is held. Rejected names: `ResultDefinition`, exact but awkward; `ResultHandle`, which keeps the handle's connotations; `Probe`, which suits clocks placed nowhere poorly.

**Clocks.** A clock is an instrument whose result is read as a time: `Clock<TTime, TResult>` derives from `Instrument<TResult>` and implements `IClock<TTime>`, which with `IClock` types mixed clock lists and typed projections. `IClock` has an internal member, so custom clocks such as `EpochClock` derive from `Clock<TTime, TResult>`. Counters (`Counter`, `OperatorCounter`, `AlgorithmCounter`) are read as `CountAccumulator`, durations (`Duration`, `OperatorDuration`) as `DurationAccumulator`. `Clock.Elapsed` reads `RunResults.Elapsed`: resolution wraps the algorithm executor resolved at the root outside any construction and adds the time spent inside its `MoveNext` calls, counting overlapping calls once, so direct resolution without an `AlgorithmRun` has it too. Removed beyond the plan, with the developer's agreement: `Clock.FromIterations`, `FromInvocations`, `FromCandidates` and `FromElapsedTime`, `ObservationClock`, `OperatorCountClock` and `ElapsedTimeClock`. `Clock.FromCycles` and `FromTurns` return counters until step 3 removes the points.

**Sources.** A source holds its configuration and a factory for the advice that wraps that configuration's executors, so observation advice and explicit placement build the same wrapper. `InvocationSource<TOperation, TCandidate, TObservation>` offers `Count()`, `CountCandidates()` and `Duration()`; `AlgorithmSource<TCandidate, TSearchState, TObservation>` serves `Iterations` and the new `Calls`. `ObservationSource.Create` has one overload for operations and one for consumer-defined roles. `Counted` and `Timed` are one generic wrapper each for every operation, with algorithm forms where an algorithm counter exists. `Observed` needs the observed configuration's own observation type, which C# cannot derive from the operation, so each operation declares one `Observed` line beside its `Invocations`, as `Named` is declared per role; the developer will look at this again after implementation. `IObservationInstrument<in TObservation>.CreateRecorder(RunResults)` is what `Observed` records through. `CountCandidates()` cannot be rejected at compile time, because candidate work is a static flag of the operation: it throws when created for an operation without candidate work, and `Counted` with a candidate counter fails at the start of its `CreateExecutor` on such an operation.

**Algorithm calls.** `algorithm.Calls` reports `AlgorithmCallObservation<TCandidate, TSearchState>` with an `AlgorithmCallEvent`: `Started` at the first `MoveNext`, then `Completed`, `Failed` for an exception other than cancellation, or `Stopped` for cancellation and for a consumer that stops before completion. A run paused by disposing its stream reports nothing, because the root iterator is kept.

**Traces.** `TraceAnalyzer<TObservation, TValue>` is the immutable instrument: sources, clocks, retention and the value function. Its result is a `Trace<TObservation, TValue>`, read through `Trace<TValue>`. Retention is an immutable policy that creates a `RetentionDecider<TValue>` for each result. `source.Trace(value, clocks, retention)` creates a trace; an overload taking a function that creates the value function serves values that keep state across observations, such as best so far, once per run. `Analyzer.Trace` keeps only its form for several sources.

**Advised.** `AdvisedAlgorithm` holds instruments and plain aspects. The first listed wraps closest; a node holding both adds its aspects first, so its instruments wrap closer. Advice that a configuration's own construction adds never applies to that configuration itself, while adding advice that would newly reach any other configuration the scope has resolved stays an error. This refines the step 1 rule that a configuration's own construction cannot add advice for it: the adder is always resolved before its construction adds anything, so the outcome does not depend on resolution order, and spec Q3 still counts `Advised` like every other node. Rejected: keeping the error, which would make a broad pointcut impossible at a run's root after step 3; making `Advised` no pointcut target, which contradicts spec Q3; and resolving its child in a child scope, which would make wrapping change sharing. See the [backlog](developer-backlog.md#advice-that-selects-the-node-adding-it).

**Accumulating analyses** stay aspects in step 2 and become instruments in step 3, as design review item 1 requires.

##### Step 2 implementation and review on 2026-11-04

Step 2 is implemented in the working tree under the decisions above and awaits the developer's review. Nothing is committed. Two internal choices follow from the code rather than from a new decision:

- The explicit operator wrappers `Counted`, `Timed` and `Observed` apply the same advice an aspect applies, through one internal helper that checks the advice, resolves the child and wraps it as resolution does.
- `Observed` and `ObservedAlgorithm` take their source in the constructor but expose only `ChildOperator` or `ChildAlgorithm`. The source supplies the observation wrapper, so a copy with another child observes that child.

**Open decisions, proposed to the developer on 2026-11-04.** The developer accepted decisions 1 and 2 on the same day; the others are not accepted yet.

1. **Per-run value functions.** C# cannot infer `TValue` for `source.Trace(Func<Func<TObservation, TValue>>)`, and an extension block member then also needs its receiver's type argument. Every call spells both, which developer guidelines § 8.6 forbids. The published best-so-far snippet shows the cost: `evaluator.Invocations.Trace<EvaluationObservation<RealVector>, ObjectiveVector>(() => { var best = new BestSoFar(); return o => best.Update(…); })`. Recommendation: replace the overload with `source.Trace(createState, value)`, which a probe confirmed infers fully, as in `evaluator.Invocations.Trace(() => new BestSoFar(), (best, o) => best.Update(o.ObjectiveVectors, o.Problem.Objective))`.

   **Accepted by the developer on 2026-11-04.** `Trace<TState, TValue>(Func<TState> createState, Func<TState, TObservation, TValue> value, clocks, retention)` replaces both per-run overloads, the source's and the one of `Analyzer.Trace` for several sources. `TState` is constrained to classes, because a struct state would silently stop accumulating. The internal `TraceAnalyzer` constructor stays. Not yet implemented: the five call sites that spell both type arguments migrate, among them the published best-so-far snippet, and an API usage spec proves the inference.

2. **Counters at the start of a call.** A trace's clocks wrap closest, so an observation made after a call reads clocks that include it. The start of an algorithm call is reported before the wrappers inside it run, so a `Started` entry reads `Calls.Count()` before its own call is counted. The states a turn yields and its end read the turn's number. Recommendation: accept, as the trace remarks and `AlgorithmCallTests` now state. The alternative is an exception to the ordering rule for clocks of call starts.

   **Accepted by the developer on 2026-11-04.** A `Started` entry reads the count before its own call is counted, and the ordering rule gets no exception. Step 4 documents this with the algorithm-call source.

3. **Direct resolution at the root.** The elapsed-time wrapper means a root request for an algorithm returns a wrapper, neither the executor its hook built nor the same object on a second request. Two tests that checked executor identity or type at the root now resolve in a child scope. Recommendation: accept, because advice wraps per request anyway.
4. **Counting external operations.** Counters come from sources, so an operation defined outside the library can be counted only once it declares an `Invocations` source; `Clock.FromCandidates` used to count any operator. This follows from the accepted removal. Recommendation: accept, since sources are the public mechanism.
5. **Per-trial traces in experiments.** A per-trial aspect cannot take an instrument, and `Advised` changes a case's type. A grid over a typed algorithm can therefore trace each trial only if every case is built advised through an untyped grid, as the migrated experiment spec does. Recommendation: settle it in the step 3 migration plan, together with the removal of per-trial aspects.

Suggestion, not proposed for step 2: `AfterOperatorCountTerminator` and `AfterOperatorDurationTerminator` accept any counter or duration, including algorithm counters, so their names say less than they do.

**Independent review.** A standards review and a spec review compared the working tree with `27c295ac` and reported 12 and 7 findings.

Fixed, each with a test:

- `Observed` and `ObservedAlgorithm` exposed their child twice, as the child and as the source's configuration, so a copy could put one configuration's observation around another child.
- `Counted` and `Timed` copied the counting and timing advice instead of applying it.
- The algorithm counters switched over an internal enum; two small subclasses replace it.
- An algorithm call could report `Started` without an end when its stream failed to start, and a stop lost `Stopped` when the stream's disposal threw. Every call now reports exactly one end, before the call is disposed.
- The turn-number test mixed both paths in its advice branch; it now uses one inner algorithm twice, so each branch is pure.
- The status line of this plan still said the terminators needed handles.

Found during migration and fixed: the first elapsed-time wrapper, written as an async iterator, disposed the root stream inside `MoveNextAsync`, so a disposal failure surfaced as a pause. It is now a transparent enumerator.

Rejected, with the reason:

- Removing `RunResources` now: it was already unreachable to consumers at `27c295ac`, and step 3 removes it with the handles.
- Requiring the same counter or duration type in `Counted` and `Timed`: each takes what it needs, a metric or a time provider, and a duration of one's own can be placed.
- Clocks whose result is the object they read, the dynamic problem or the run's results: the instrument contract requires a result.
- An explicit time provider for `Clock.Elapsed`: the accepted design measures system time and keeps an internal test hook.
- The `scope.AddAspect(scope.Results.Aspect(instrument))` chain: it is the accepted way for a node to attach instruments.
- Renaming the terminators: their names are accepted; the suggestion above stands.
- Binding a trace's clocks once instead of twice per run: two lookups per run cost nothing measurable.
- Splitting `RunResults`: sharing one table with the handles is accepted as transitional.
- A public accessor for a role source's advice factory: the role's author owns that advice and applies it in the role's own wrapper, and no built-in role has access a consumer lacks.
- Restoring `RunTraceExtensions`: its removal in step 2 was accepted with the coexistence decision.
- Keeping `ObservationSource.Configuration` internal: the accepted proposal made it public.

The open decisions above are the remaining findings: the inference cost of the per-run overload is decision 1, and the counter read at a call's start is decision 2.

**Validation.** With SDK 10.0.114 and the Roslyn 5.9 compiler, the Release build has no errors. Compared with a clean build of `27c295ac`, its only new warning is S2326 on the marker type parameter of `IClock<TTime>`, which `IAlgorithm` already carries. All 3339 solution tests pass, up from 3286. The whitespace, style and error-severity analyzer checks, the documentation build and Prettier on the changed plan files pass. Packaging is validated with step 3.

**Review of the step 2 commit.** A review of `ac9bd3f3` against `27c295ac` reported four findings; two are decisions 1 and 2 above, which the developer accepted afterwards.

- Fixed: the elapsed-time wrapper returned for every root request hid the root executor's controls from `ExecutorControls.Find`. It now forwards them, as a transparent wrapper must; a test resolves an algorithm whose executor is its own control at the root.
- Fixed: the XML documentation step 2 added to tests, which developer guidelines § 9.1 forbids, is now ordinary comments. XML documentation that predates step 2 stays, including four blocks step 2 reworded.
- Found while fixing, not changed: the observation wrappers of `Iterations`, since before step 2, and of `Calls` do not forward the wrapped algorithm executor's controls either, nor does `WrappingAlgorithmExecutor`, although execution resolution requires transparent advice wrappers to forward them.

**Left for step 3:** a migration plan for the developer's agreement, then the removal of run setup, `ResolutionScopeBuilder`, the resource handles and `RunResources`, observation points with the Cycle points and `FromCycles` and `FromTurns`, per-trial aspects and the counting and timing aspects with live accumulators; the accumulating analyses as instruments; decision 5; and the root `README.md` example, which already used an API that did not exist at `27c295ac`.

**Step 3 scope accepted on 2026-11-05.** The developer selected finishing step 3 as the baseline before deciding any simplifications. The [concrete migration plan](operator-unification-step3-migration.md) records the removal inventory, proposed experiment and accumulating-result migrations, validation and completion criteria. Its decisions A and B, including the dynamic problem dependency, were accepted on the same day. Broad step 4 rewriting remains deferred; affected examples and directly contradictory API documentation migrate with step 3.

## Problem and accepted decision

The baseline resolver built executors with the requesting context's children and advice while selecting persistent state through configuration ownership and ancestor lookup. This separates observation context from state lifetime, but ordinary operator authors pay for ownership and reconstruction rules even when they need only private fields.

The developer selected the refined 1.2 proposal from the architectural review:

1. Keep reusable algorithm and operator configurations separate from their executors. Configurations hold settings, child configurations and optional resource handles, not the live resources introduced by this design.
2. Put private mutable execution data in the executor that uses it. Repeated construction of the same configuration creates independent private state, including two constructions within one turn. Stateless implementations may still reuse immutable executors.
3. Make sharing explicit through a resource handle. Configurations that use the same handle can obtain the same resource within a run. A handle describes resource creation and sharing identity. It does not contain the live resource.
4. Create one resource per handle per run by default. A caller can supply an existing resource during run preparation, including supplying the same instance to several trials deliberately.
5. Keep contextual child construction, automatic advice weaving, operation checks, observation identity and named execution methods. Sharing a resource does not share an executor or its advice chain.
6. Remove generic scoped state, configuration state ownership, ancestor state lookup and implicit sharing between separate resolutions. Do not replace them with an all-purpose state registry under a new name.
7. Keep fresh Cycle turns as the default. Pass the current search state to the next turn while constructing fresh private execution data. Pause/resume keeps the existing execution.

The developer also confirmed three boundaries on 2026-10-27: support executor and run lifetimes only, keep an explicit Cycle executor reuse option and include handle reference identity in configuration equality. Separate handles remain distinct even when creation settings match. Record copies retain their handle references. No narrower resource lifetime is part of this change.

The follow up decisions on the same date close the remaining product questions. Automatic resource disposal is excluded from this version. Resources needing deterministic cleanup must be supplied and remain caller owned. Strict shared evaluation budgets across concurrent trials are deferred. Operator authors are responsible for correct concurrent access to shared resources, with caching as the worked example. Cycle reuse keeps one executor per child configuration reference within one Cycle executor, so `Cycle(A, A)` reuses one A executor when reuse is enabled.

First restore a coherent working library under this model. Serialization, checkpoints, nested resource lifetimes and other special cases are later candidates to assess against concrete needs, not prerequisites for the migration. The implementation record below records the sharing audit and API proofs.

This is an architectural change with intentional breaking semantics. Approval of the direction is recorded here and needs no repeated approval. The earlier session requested planning only. The implementation request on 2026-10-27 authorizes P0 through P5 under these accepted boundaries.

## Review findings

### Follow up review on 2026-10-28

The discussion below records the review proposals. The developer subsequently accepted the reusable scope builder, abstract resource creation with an explicit supplied resource handle and explicit executors for mutable operators. These accepted changes supersede the corresponding earlier decisions. The builder and handle changes are implemented. The developer also accepted multiple direct operator inputs for budgets, including constructor usage, and rejected the proposed `BudgetTarget.For` wrapper. Lists count all operations of each listed configuration while single operator overloads retain typed selection. MemoryCache is preferred over custom storage and is restored behind `EvaluationCache<TKey>` as described below. The closure discussion retains Cycle executor reuse and the Executor vocabulary. The construction guard stays unchanged after the concurrency clarification below. Routine corrections remove stale published terminology, clarify supersession of the operator unification plan and remove the obsolete commented RoarNet sketch.

1. **Cycle reuse.** Retain fresh executors by default. Retain optional complete executor reuse if preserving adaptive search across finite turns remains a requirement. `EvolutionStrategy.MaximumGenerations` uses an invocation local counter while Gaussian mutation strength lives in its executor, so reuse can preserve learned strength across bounded turns. Count and elapsed time terminators retain their exhausted data too. Removing reuse would simplify Cycle but require explicit resources for selected learning that must survive fresh turns. Do not add a reset protocol to compensate for incompatible reuse.
2. **Combined budgets, accepted and implemented.** `OperatorBudgetAlgorithm<TCandidate, TSearchState>` and `OperatorDurationBudgetAlgorithm<TCandidate, TSearchState>` accept the child algorithm, an operator list and the limit in their constructors. Their extensions accept the same direct lists, without target wrappers. The lists select all operations of each configuration reference. One union pointcut applies one counting or timing advice, so repeated references count once. Each executor creates one private accumulator. Existing single operator variants retain their inferred operation selection. Invocation and candidate work metrics, additive invocation durations and checks after yielded search states are preserved. Candidate accounting rejects selected operations without candidate work. Shared consumption across distinct executors or fresh Cycle turns is expressed through handle based counting and timing aspects with matching terminators. The low level count and duration terminators now hold resource handles instead of live accumulators. Direct runtime accumulator inspection remains available for clocks and private budgets.
3. **Resource creation.** Prefer an abstract `ResourceHandle<TResource>` with one `protected internal abstract TResource CreateResource()` hook and a concrete `SuppliedResourceHandle<TResource>` for resources requiring explicit supply. The latter reports missing supply if lazy creation is attempted. This adds one named supplied resource form and removes `CreateDefault` and its type inspection. The smallest alternative keeps the concrete base and virtual supplied resource fallback while merging the two hooks. In either design, retain reference identity and caller ownership of resources requiring deterministic cleanup. Checking `IDisposable` cannot prove that contract, even when checking the runtime type.
4. **Preparation and resolution.** Prefer a reusable `ResolutionScopeBuilder` whose `Build()` snapshots supplied bindings and aspects into an independently usable scope. Scope lookup may lazily populate defaults but exposes no supply method and has no sealed phase. Repeated builds have independent default resources. Supplied instances remain intentionally shared. Later builder changes cannot affect existing scopes. Keep `AlgorithmRun.AddResource` within the run's existing preparation lifecycle and create its scope when execution starts. Keep experiment validation before installing any trial bindings. A bindings argument to `ResolutionScope.Create` is the smaller alternative but still needs a typed heterogeneous bindings carrier.
5. **Leaf authoring.** Prefer the existing role and executor bases for private mutable data. `AfterIterationsTerminator` demonstrates a small factory and a nested executor owning one counter. A fresh state helper remains technically possible without restoring scoped sharing, but adds a state type, creation hook and forwarding layer across the role families. Choose the authoring experience before migrating the remaining tutorial fixtures. Grouping private executor fields in a private object is valid. Redundant public `State` factories and configuration callbacks are the misleading part of those examples.
6. **Cache storage, revised after developer feedback.** Restore MemoryCache 10.0.11 behind the existing typed `EvaluationCache<TKey>` resource. This replaces the dictionary and insertion order eviction without adding a new public wrapper. Each entry has size one. Statistics and linked entry tracking are explicitly disabled, and the typed store exposes no expiration token or callback configuration. Inspection of the [pinned implementation](https://github.com/dotnet/runtime/blob/v10.0.11/src/libraries/Microsoft.Extensions.Caching.Memory/src/MemoryCache.cs) shows that disposal releases thread local statistics when enabled. The restricted use here has no deterministic cleanup obligation. Transient maintenance tasks can briefly retain the cache. This conclusion applies to this use and version rather than all disposable resources. Existing retention tests verify that discarded executors release candidates and results. MemoryCache admission and compaction replace the custom insertion order policy. No performance improvement is claimed.
7. **Construction recursion.** The current guard holds one root lock throughout user construction hooks. A custom hook waiting for another thread to resolve through the same root can deadlock. Built in construction paths are sequential, including deferred Cycle, Pipeline and DynamicRacing resolution. Experiment trials have separate roots. The earlier recommendation to replace the guard was disproportionate to the demonstrated requirement and is withdrawn pending a concrete parallel construction requirement. The guard remains unchanged. A bare thread local replacement would miss recursion that crosses threads.

Developer guidelines § 4.10 explicitly rejects defensive null checks for nonnullable inputs, so the review's null guard suggestion is not applied. Cache exception handling now distinguishes direct resource constructor argument validation, which uses `ArgumentOutOfRangeException`, from configuration validation, which continues to use `InvalidOperationException` before child construction.

The dynamic cache review found repeated requests for one epoch under a deferred update policy. The closure follow up below now records the accepted one request per observed epoch behavior and its regression coverage.

### Run setup decision, accepted on 2026-10-28

The developer accepted `CreateRun` with an optional setup callback, separating resource and aspect setup from the execution only `AlgorithmRun`. Ordinary callers do not need to know about `ResolutionScopeBuilder`. Both resource and aspect attachment move into setup, including analyzer helpers. This supersedes the earlier decision to retain `AlgorithmRun.AddResource` and its preparation phase.

The accepted public usage keeps fluent setup methods inside the callback:

```csharp
var run = algorithm.CreateRun(problem, random, setup => setup
    .AddResource(cacheHandle, cache)
    .AddAspect(analyzer));
```

Calls without custom setup remain `algorithm.CreateRun(problem, random)`. A callback permits conditional setup and composition through helper methods without exposing a separate build operation. Do not add a public `CreateRunBuilder` alternative. Retaining incomplete setup or building several runs from it has no demonstrated requirement here and would need additional rules for random generators, supplied resources and analyzer instances across builds. The alternative and conditions for reconsideration are recorded in the [developer backlog](developer-backlog.md#explicit-run-builder-alongside-createrun-setup).

A setup callback runs once before the run is returned, and its data is snapshotted so retaining the setup object cannot mutate the run afterward. The run still resolves its graph on first execution and retains normal pause, resume and terminal lifecycle behavior. Direct constructors need the same setup path. Experiment callbacks must keep materialized trial identity, analyzer access and validation before any trial starts. Whether the low level builder remains available to direct resolver authors is distinct from hiding it from ordinary application code. Both the responsibility split and callback API are accepted and implemented. `AlgorithmRunSetup<TCandidate, TSearchState>` collects the root algorithm's resources and aspects. Both concrete problem and interface problem `CreateRun` overloads and direct constructors support the callback. Returned runs start in `Ready`, replacing `Preparing`, and expose execution operations without attachment methods. The scope snapshots setup before return while executor resolution remains lazy on first execution. Analyzer shortcuts now extend the setup type.

`ExperimentRunSetup<TCandidate, TSearchState, TAlgorithm, TKey>` collects per trial aspects and resources against already materialized `Cases`. Case keys are validated before the callback. Trial runs are created only after setup succeeds. `AddAspectPerTrial` returns typed `TrialAspect` mappings whose `Trial` is the materialized case, and each resulting execution trial retains the identical object through `Case`. `AddResourcePerTrial` preserves evaluation and validation of the whole batch before installing bindings. Captured setup objects can be changed later without altering returned runs. Production callers, PythonInterop, tests, samples, the glossary and published examples use the new setup boundary.

The resource table remains protected by a simple lock. Internal operator resource lookups occur in executor construction. The requested `run.Resource(handle)` lookup also allows callers to inspect the run resource directly before, during or after execution. Ordinary operator calls use retained resource references. Deferred construction can perform later lookups during execution, but the table is not in the per candidate operation path. Existing construction guards also serialize construction within a root, and trials have separate tables. No measured contention motivates a reader writer lock change.

Budget extensions explicitly initialize their metric or time provider in every overload, including defaults. This preserves the existing rule of constructor inputs for required settings and init properties for optional settings. No public constructor contract changed in this consistency cleanup. `HeuristicLib.csproj` had only CRLF differences from the indexed LF content, with no BOM difference. Normalizing to LF makes its bytes identical to the index and clears its modified status.

### Run setup implementation validation

The Release build passes and the full solution passes 3234 tests with none skipped. New coverage proves callback execution once, configuration validation before callbacks, setup failure without executor creation, resource and aspect snapshots, direct constructor parity and per trial batch validation before installation. Existing pause and resume, advice ordering, shared cache, concurrency and experiment identity scenarios pass with the migrated API. Documentation examples include an executable experiment setup walkthrough. The documentation build and solution whitespace, style and error severity analyzer checks pass. Changed Markdown passes Prettier. Formatters report workspace loading warnings, and existing compiler and analyzer warnings remain.

### Closure follow ups accepted on 2026-10-28

The developer authorized the remaining cache regression, executor fixture cleanup, accumulator lifetime review and final closure audit. Keep the `Executor` vocabulary and explicit `Cycle.ReuseExecutors` option. Fresh Cycle turns remain the default. This closes the reconsideration of those two accepted decisions without introducing a reset protocol.

The dynamic cache requests at most one update per observed epoch. The shared cache owns that request marker together with entries and hit accounting. Crossing an epoch boundary clears it. A miss still resets the consecutive hit count but does not rearm a pending request. Requests retain the expected epoch check on the problem, and calls into the problem remain outside the cache lock. A deferred policy regression first reproduced nine queued updates where one was intended, including concurrent hits through two executors and a miss while the request was pending.

The developer chose resource handles for the count and duration terminators and requested access to actual resources through the run. `run.Resource(handle)` now forwards to the run's scope, preserving lazy default creation, supplied ownership and resource identity. The developer accepted resource lookup for configurations and advice, with child resolution available only to configurations. `RunResources` exposes the existing resource table through lookup only and is passed to advice wrapping hooks. Count, candidate and duration aspects gain handle overloads while their direct runtime accumulator overloads remain available for clocks and private budgets. Count and duration terminator configurations hold handles and resolve their accumulator when building an executor. This shared advice contract change is implemented across operator, algorithm and consumer role advice, including the typed role bridges. `RunResources` replaces the internal `ResourceTable` rather than adding a second registry. `DurationAccumulatorHandle` supplies default duration accounting alongside the existing count handle. Resource backed aspects resolve their accumulator while wrapping and pass the actual resource to the invocation wrapper. Unifying the configuration and advice APIs to reduce public surface remains an explicitly requested future discussion, not part of this implementation.

Validation of the completed follow ups and advice resource migration passes the Release build and all 3243 solution tests with none skipped. Coverage includes deferred cache updates, public run resource lookup, count and duration handles, supplied accounting, independent repeated trials, unsupported candidate metrics before resource creation and resource identity through operator, algorithm and consumer role advice. Solution whitespace, style and error severity analyzer checks, the documentation build and packaging pass. The closure audit also attached the repository analyzers to Experimental and PythonInterop and found no HLib diagnostics. Existing compiler and analyzer warnings remain. The source and API ledger and Python smoke results are recorded in the [closure audit refresh](operator-role-parameterization-rework.md#closure-audit-refresh-on-2026-10-28).

### Advice dependency and budget API revision accepted on 2026-10-29

The developer accepted removing resource lookup from every advice `Wrap` contract. Advice receives concrete runtime dependencies through its constructor. Resource dependent aspects are assembled per run. The handle based counting and duration aspect overloads are removed without introducing an advice binding or preparation mechanism. Configuration hooks retain handle lookup and callers retain `run.Resource(handle)`. Supply an accumulator under a handle only when a configuration such as a terminator must resolve it or when callers need run based inspection. Measurement alone needs only the concrete accumulator passed to its aspect. Fluent operator counting and timing methods create aspects and published examples use that form.

The count budget family is renamed to `OperatorCountBudgetAlgorithm`, including its executor, alongside `OperatorDurationBudgetAlgorithm`. Both single operation and operator list forms take the required algorithm, observed operator or list and limit in constructors. Optional metric and time provider settings use init properties consistently. Experiment execution status remains unchanged.

This supersedes the earlier acceptance of resource lookup from advice. Configuration and advice API unification remains a separate discussion, with concrete advice dependencies as the current boundary.

Implementation and validation are complete. No advice wrapping hook accepts `RunResources` or a resolution scope. Operator, algorithm and consumer role advice receive concrete dependencies. Regression coverage checks the wrapping contract, per run accounting, explicitly shared supplied accumulators, unsupported candidate metrics and constructor parity. Published counting and timing examples use fluent aspect methods, including measurement without a resource binding and shared accounting with a handle based terminator.

`dotnet build --configuration Release --no-restore` passes. `dotnet test --solution ./HEAL.HeuristicLib.slnx --configuration Release --no-restore --max-parallel-test-modules 1` passes all 3254 tests with none skipped. The focused count budget suite passes 38 tests and the API usage suite passes 222. `npm run docs:build`, solution whitespace, style and error severity analyzer verification, changed Markdown Prettier checks, `git diff --check` and `dotnet pack --configuration Release --no-restore` pass. Existing compiler and analyzer warnings remain, and formatters report workspace loading warnings. No compatibility shims, advice binding stage or preparation mechanism were added.

### Aspect configuration lifetime review on 2026-10-29

Status: reviewed proposal, recorded at the developer's request. Recommendations below are not accepted implementation decisions. They do not supersede the existing advice ownership contract or authorize migrating analyzers. The complete problem, prioritized findings, typed application design and proposed usage are in [operator rework section 11](operator-role-parameterization-rework.md#11-aspect-configuration-rework-and-independent-review).

The subsequent [simplicity checkpoint](operator-role-parameterization-rework.md#simplicity-review-and-governing-design-goal) withdraws a redesigned analyzer lifecycle as a prerequisite for the first change. Counter lifetime follows ownership: the simple budget owns one private accumulator per executor construction, while a handle selects one shared resource per run. An enclosing budget can already span fresh Cycle turns through its existing runtime aspect context. Runtime analyzers can likewise retain data across fresh child executors without becoming resources. The collector proposal below remains conditional on an accepted need for reusable analysis descriptions.

The immediate concern is that fresh Cycle turns should not unintentionally reset all analysis for a child algorithm. Reusable descriptions, runtime advice and wrapping executors can have different responsibilities without requiring a cache of runtime advice for each run. The review recommends persistent resources for explicitly shared analysis data and fresh contextual construction for wrappers.

| Object                                        | Recommended lifetime and ownership                 |
| --------------------------------------------- | -------------------------------------------------- |
| Aspect and advice descriptions                | Reusable immutable configuration                   |
| Runtime advice constructed from a description | Lazy construction for an applicable executor build |
| Wrapping executor                             | Fresh with the executor it wraps                   |
| Invocation locals                             | One call or iterator                               |
| Persistent analysis data                      | One resource per handle per run by default         |
| Supplied collector or sink                    | Caller owned for all participating runs            |

Runtime advice objects need not own persistent state under this model. Reusing existing advice classes as temporary construction helpers is the smaller migration. A description could instead construct its wrapping executor directly once dependencies are available. No additional mandatory public runtime aspect object is justified. One advice object per description per run is a possible alternative, but adds an identity keyed cache, another sharing mechanism and publication and retry rules without solving a demonstrated need beyond explicit resources.

#### Analyzer ownership recommendation

Separate selected sources and immutable analysis settings from a runtime collector. The collector owns history, retention progress, best so far state and synchronization. Those related fields may remain one coherent resource. Do not create one handle per private field. Current [TraceAnalyzer](../src/HeuristicLib/Analysis/Tracing/TraceAnalyzer.cs), [TraceRetention](../src/HeuristicLib/Analysis/Tracing/TraceRetention.cs) and [BestSoFar](../src/HeuristicLib/Analysis/Tracing/BestSoFar.cs) demonstrate why moving only the entries would leave mutable dependencies shared across trials.

Reusable descriptions must not capture live retention objects, mutable functions or runtime sinks. Immutable strategies can describe reusable behavior. Arbitrary callback closures remain runtime setup inputs unless a separately accepted contract makes their ownership explicit. Do not automatically convert today's `TraceAnalyzer` or `AccumulatingAnalyzer` into reusable values.

A collector handle owns immutable settings governing shared collector state. Several descriptions may write compatible observations into it. Conflicting retention, aggregation or axis semantics must be rejected or use separate collectors. Creation cannot depend on the first matching executor or description, and supplied collectors must not be silently reconfigured.

The resource must not contain the complete aspect, pointcuts, observation source configurations, contextual executors or a resolution scope. Current clocks derive from `Aspect`, and trace moments retain clock objects as keys. Reusable clock descriptions therefore need to keep their producer configuration outside the collector. Recorded axes should use stable clock resource identity rather than retain the whole configured clock. A resource factory receives no context and must not resolve a second graph of dependencies.

`run.Resource(analysis.Results)` is a proposed inspection shape, not an existing analyzer API. It can use the existing generic resource lookup without making hosting depend on Analysis. Reading it before a match may create an empty collector, so an empty result does not prove a failed attachment. Collected results remain available after failure or cancellation. Keep synchronized trace reads and immutable snapshots. Accumulating collectors retain their live view contract and are read after all writers finish. Completion of one run does not establish that condition when several runs share the collector.

Deliberate external sinks remain caller owned. A reusable description can name a supplied resource handle for a suitable sink contract, while runtime setup can retain current explicit callback based attachment. Supplying a complete analyzer aspect as the default collector is not the proposed split. Resources requiring deterministic cleanup remain explicitly supplied, with no disposal on run completion and no global collector completion state.

#### Lifetime scenarios and required consequences

| Scenario                      | Required consequence under the recommendation                                                                                                                                                                                   |
| ----------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Fresh A turns inside Cycle    | Fresh executors and wrapping executors share A's collector through its handle. History survives and invocation local iteration numbering restarts.                                                                              |
| Cycle executor reuse          | Retain the complete advised executor by the actual child application reference. Distinct applications around the same A must not collapse to A's reference. Repeating one application reference can reuse it.                   |
| Repeated or concurrent trials | The same descriptions and handles yield independent default resources in separate run tables. Deliberately supplying one collector combines the trials and requires collector synchronization.                                  |
| A and B reference one mutator | Separate contextual executors retain independent private fields. A local aspect sees A's resolutions only. A common aspect may see both. A shared counter does not share adaptive mutator fields.                               |
| Nested applications           | Local advice precedes ancestor advice while resources stay flat. A child boundary cannot observe siblings or the parent's observation points resolved outside that context.                                                     |
| Shared clocks                 | Reuse the same producer description identity before each trace's observations. The resource handle alone does not deduplicate separate producers. Reading a clock does not install its producer outside the placement boundary. |
| Handle based termination      | A counter retained across fresh turns stays accumulated. A fresh terminator executor reading it can already be exhausted. Fresh per turn budgets require private executor data.                                                 |
| Pause and resume              | Keep the existing iterator, executors, wrapping executors and resources without resetting or rebinding.                                                                                                                         |
| Failed construction           | Failed resource creation publishes nothing. Later construction or wrapping failures retain published resources. Direct resolution retries create fresh executors and advice. A failed run remains terminal.                     |

[CycleAlgorithm](../src/HeuristicLib/Algorithms/Composition/CycleAlgorithm.cs) currently keys its local reuse dictionary by child configuration reference. [RunResources](../src/HeuristicLib/Execution/Resources/RunResources.cs) publishes one resource per handle after successful creation. These mechanisms are retained responsibilities rather than reasons to introduce resource ancestry or resolver owned executor caches.

The table's synchronization guarantees concern resource integrity, not strict concurrent reservations or deterministic cross trial ordering. Shared counters do not turn separate checking and accounting into a strict budget. Shared clocks can include concurrent work beyond the particular observation being recorded. Reusing a sink deliberately may also combine run local clock coordinates, so users must choose compatible axes and result semantics.

Elapsed clocks require an explicit origin decision. Current [ElapsedTimeClock](../src/HeuristicLib/Inspection/Clock.cs) starts at object construction. Starting a configured clock merely because `run.Resource` was inspected would make inspection change elapsed time. The review recommends an activation origin independent of inspection. Whether that origin is the first selected execution or run start remains open. Cumulative invocation duration retains its separate meaning, including failed calls and excluding work outside the measured invocation.

#### Acceptance boundary

Keep resource handles and optional runtime supply as settled requirements. The developer reconsidered and rejected storing live resources in reusable configurations again during this discussion. Passing `RunResources` through every `Wrap` also remains rejected. The narrow description binding proposal, analyzer split, collector compatibility rules, clock identity and elapsed origin need explicit architectural acceptance.

The existing accepted section on executors and advice below intentionally preserves advice owned data and says not to route every analyzer or clock through handles. This proposal changes the model only for a separately accepted reusable description path. Counting ergonomics alone does not authorize converting all runtime analyzers. The plan must record the chosen migration scope before implementation.

No implementation or behavioral validation was performed for this review. Existing full solution and focused preparation test results validate the current code, not these proposed lifetimes.

### Standards and specification review fixes on 2026-10-29

All five review findings are addressed. Published advice guidance uses `setup.AddAspect` inside the `CreateRun` callback and describes setup snapshots before the run is returned. Operator authoring guidance distinguishes independent executor fields from shared run resources without ancestor ownership. Dynamic cache binding compatibility remains validated during executor construction, with no repeated binding check or grace parameter on the evaluation path.

`HLib0002` checks resource requests through `ResolutionScope`, `RunResources` and `AlgorithmRun`. Regression cases cover an executor used directly as a resource, a resource containing an executor and ordinary managed data through each lookup API. Four cases reproduced the missing diagnostics before the fix. All 27 operator authoring analyzer tests and 37 dynamic cache tests pass. The Release build and all 3251 solution tests pass with none skipped. The documentation build, solution whitespace, style and error severity analyzer checks, changed Markdown formatting and `git diff --check` pass. Formatters report workspace loading warnings. Existing compiler and analyzer warnings remain.

### Completed follow up validation

The accepted scope builder, abstract handle contract and direct operator list budgets are implemented. MemoryCache replaces the custom store. Sixteen authoring examples now keep their data and behavior directly in executors, including every former public State factory in OperatorAuthoringSpecs. The closure cleanup also migrates the remaining role forwarding fixtures and inlines redundant preparation factories. Private data carriers that lifetime and disposal tests deliberately observe remain local to their executors. Actual search state factory helpers and analyzer result objects remain distinct from executor data. Resource examples are in OperatorPreparationSpecs.Resources.cs, matching the partial class name. Cycle XML documentation explains fresh defaults and the effect of retaining exhausted terminator fields. No Cycle behavior or runtime naming was changed.

The Release build passes. The complete solution passes 3225 tests with none skipped. Focused checks cover builder snapshots, independent resources, borrowed supply, cache retention, combined mutation and crossover budgets, duplicate references, constructor parity and selection of consumer defined operations. Documentation builds successfully. Changed Markdown passes Prettier, and solution whitespace, style and error severity analyzer verification pass. The formatter reports workspace loading warnings without verification failures. Existing compiler and analyzer warnings remain.

The review examined three separate choices: where private state lives, how shared resources enter execution and whether configurations merge with executors. Moving private state into executors supplies most of the simplification. Merging the two type families is a separate and much larger change.

### Option 1.1: live resources in configurations

The strongest benefit is direct constructor injection with little resolver work. The main weakness is that configuration reuse also reuses live resources. `Repeat`, grid copies and a second run would share a cache unless application code rebuilt the resource and configuration graph for every trial. Mistakes silently couple trials or return values computed under another parameter setting.

The executor migration is feasible, but preserving independent experiments requires a new composition function path. That cost makes 1.1 a poor target for this library's reusable configurations. Counting advice is precedent for runtime resource supply, not evidence that configurations already store these resources. This direction is not selected.

### Option 1.2: resource handles in configurations, instances supplied at runtime

The strongest benefit is that independent runs remain the default while selected resources survive fresh turns. Ordinary users do not need setup callbacks to obtain one cache per trial. Operator authors use fields and only expose a resource when sharing has domain meaning.

The cost is explicit and bounded: one flat table maps handle identity to a resource for a run. This is still a registry. It is simpler than scoped state because it has explicit keys, no ancestor ownership and no implicit participation by every configuration. Creation settings belong to the handle, so the first requester cannot choose them.

This direction is feasible with the existing run and construction model. Its remaining design work concerns resource identity, setup timing, coherent resource boundaries, concurrency and cleanup. Adding nested lookup, service type lookup, implicit private state sharing or arbitrary factory dependencies would defeat the intended simplification.

### Option 2: runtime objects as the primary composition model

The strongest benefit is direct authoring with parameters, state and behavior on one object. Its main weakness is the compensating machinery required for fresh turns, experiments and observation of objects that do not exist yet. Those features need construction functions, stable observation identities and a construction context for advice. Binding operators to a run's concrete search space, problem and search state types also remains necessary.

Direct runtime composition is feasible, but preserving the current product capabilities would be a broader redesign. Much of its benefit already follows from executor fields. Merging configurations and executors is not selected for this change.

### Findings grounded in the baseline source

- [ResolutionScope](../src/HeuristicLib/Execution/ResolutionScope.cs), especially `State.GetOrAddOwner`, `FindOwner` and `GetScopedState`, confirms that sharing depends on configuration references and ancestor ownership. Child selection stays local if it happens first. Parent selection can be reused by descendants if it happens first. Both rules explain the order dependence.
- [CycleAlgorithm](../src/HeuristicLib/Algorithms/Composition/CycleAlgorithm.cs) constructs an inner executor on every turn and carries search state forward. the former `ChildScopeSelector` chooses fresh, retained or parent state for the whole activation. Those policies alone do not express one shared cache alongside fresh terminators.
- [CachingEvaluator](../src/HeuristicLib/Operators/Evaluators/CachingEvaluator.cs) already passes a cache to its executor, but obtains it from scoped state. [CountingOperatorAdvice](../src/HeuristicLib/Inspection/CountingOperatorAdvice.cs) instead receives its accumulator directly at runtime. Neither needs to share an executor to share its data.
- [AlgorithmRun](../src/HeuristicLib/Execution/Runs/AlgorithmRun.cs) collects setup while preparing, creates a root scope on first execution and retains its scope and iterator when paused. Resource supply can use the same preparation boundary. Resume does not resolve the graph again.
- [RepeatedExperiment](../src/HeuristicLib/Experiments/RepeatedExperiment.cs) repeats a configuration reference. [ExperimentRun](../src/HeuristicLib/Experiments/ExperimentRun.cs) creates a separate algorithm run for every materialized case. [GridExperiment](../src/HeuristicLib/Experiments/GridExperiment.cs) uses configuration equality for duplicate detection and configurations as trial keys. These facts make per run resources natural and handle equality consequential.
- [EvolutionStrategy](../src/HeuristicLib/Algorithms/Evolutionary/EvolutionStrategy.cs) obtains mutation and its strength control from one resolved executor. [GaussianMutator](../src/HeuristicLib/Encodings/RealVectors/Mutators/GaussianMutator.cs) can therefore keep strength in that executor. Separately resolving the same configuration to find its control would target different private state after this change.
- [OperatorOwnershipTests](../test/HeuristicLib.Tests/Execution/OperatorFoundation/OperatorOwnershipTests.cs) and [AdaptableMutationStrengthTests](../test/HeuristicLib.Tests/Operators/Mutators/AdaptableMutationStrengthTests.cs) require sharing across separate operation views today. They describe an intentional behavior change to audit, not regressions to preserve through hidden sharing.
- [DynamicCachingEvaluator](../src/HeuristicLib.Experimental/Problems/Dynamic/Operators/DynamicCachingEvaluator.cs) keeps entries, epoch and hit accounting together. A shared resource must preserve that coherence. Sharing only the dictionary would be incorrect.
- [LimitEvaluator](../src/HeuristicLib/Operators/Evaluators/LimitEvaluator.cs) reads remaining work, evaluates and increments separately. [CountAccumulator](../src/HeuristicLib/Inspection/CountAccumulator.cs) uses atomic operations, but that alone does not make a shared limit strict under concurrent execution.

### Corrections and qualifications

The following supersede claims in the earlier discussion or review:

1. Option 1.1 is not the smallest coherent target once independent repeated trials and grids are included. Its apparent simplicity omits resource creation per trial.
2. Refined 1.2 does retain a resource registry. The implementation must make its flat structure and limited responsibility explicit.
3. A pure handle is not automatically a structurally comparable or serializable value. Sharing uses identity. Equality of containing configurations needs a stated rule, and persistence needs a format that preserves references.
4. Settings export, reconstruction of sharing relationships and runtime checkpointing are three separate requirements. No option provides a complete JSON round trip merely by using records. Iterator locals also prevent claiming that today's scoped state already provides checkpointing.
5. Executor reuse is only a replacement for Retained within a fixed executor and context. Current retention survives Cycle rebuilds and different requested run types while adopting the current advice. Reusing an executor keeps its existing children and advice. The stronger guarantee is intentionally relinquished by this direction.
6. Pause/resume is not a differentiator between the proposals. Starting another independent execution is. This change must preserve the retained iterator and resource bindings of a paused run.
7. The statement that no configuration currently holds any live dependency is too broad. Dynamic caching references a problem whose epoch changes. The narrower confirmed claim is that current cache and budget stores come from scoped state while counting advice receives its accumulator at runtime. This change does not purify every existing problem or strategy reference.
8. Thread safety and reproducibility are distinct. Deliberately shared mutable resources can make results or stopping behavior depend on scheduling even when individual operations are synchronized. Resource creation order and resource identity must still be deterministic under the chosen contract.
9. Fewer concepts do not establish a performance improvement. Construction cost, allocations and execution overhead require separate measurements if performance claims are made.

### Risks in priority order

1. **Silent changes from repeated references.** Two resolutions of one LimitEvaluator or cursor configuration will no longer share private data. Audit these assumptions before bulk migration and expose a handle only where sharing is intentional.
2. **Invalid or unsafe shared resources.** A shared cache can return results under the wrong evaluation semantics, and an atomic counter does not establish a strict concurrent budget. The resource contract must cover these behaviors independently of the table.
3. **Resources exceeding the cleanup boundary.** Automatic creation is limited to resources that need no deterministic cleanup. Audit the current cache implementation and other private resources against that boundary rather than silently leaving required disposal undone.
4. **Overstated Retained equivalence.** Reusing executors preserves their children and advice. Tests expecting retained private state across rebuilt contexts or run types need deliberate replacement.
5. **Equality and persistence confusion.** Handle identity is accepted, but it affects grid duplicate detection and does not survive reconstruction as object equality. Preserve sharing relationships separately if an export format is later introduced.

## Intended usage

These abbreviated sketches describe the implemented API. Complete executable examples are in OperatorPreparationSpecs.Resources.cs and ResourceCachingDocumentationSpecs.

```csharp
var store = new EvaluationCacheHandle<RealVector> { SizeLimit = 100_000 };
var evaluation = evaluator.Cached(store);
var cycle = CycleAlgorithm.Create(
    algorithmA with { Evaluator = evaluation },
    algorithmB with { Evaluator = evaluation });
var experiment = cycle.Repeat(30);
```

Each trial gets its own store automatically. All turns of that trial obtain the same store through `store`. Each turn still receives fresh algorithm, terminator and evaluation executors. Using the same evaluator configuration without a handle creates a private cache for each executor instead.

Supplying a live instance is a preparation operation:

```csharp
run.AddResource(store, existingStore);
experimentRun.AddResourcePerTrial(_ => (store, sharedInstance));
```

The second line deliberately supplies one instance to every trial. Returning a new instance from each callback supplies independent resources. Neither callback is necessary for the ordinary independent trial case. The callback receives the materialized trial configuration, allowing it to select that configuration's handle when a grid varies handles.

The author side has one explicit choice:

```csharp
var cache = handle is null
    ? CreatePrivateCache(settings)
    : scope.Resource(handle);
return new Executor(childEvaluator, cache);
```

`CreatePrivateCache` and `settings` stand for that operator's own construction code. Ordinary counters and adaptive values need no resource object at all and live directly in executor fields. A handle is appropriate only when an author intentionally supports sharing a coherent resource.

## Accepted contracts and boundaries

The contracts below guided implementation. The implementation record identifies their executable proofs and completed validation.

### Identity and creation

- Use a public typed handle contract, `ResourceHandle<TResource>`, with immutable creation settings and a creation method. A consumer can define both its resource and handle without a library registration list.
- Use handle reference identity for lookup. Equal settings on different handles must not merge resources. A handle is an identity bearing object, not a record whose value equality chooses sharing.
- The creation method receives no requesting configuration, child executor, advice context or resolution scope. It must not select settings from the first caller. Configured creation strategies remain immutable values, rather than closures capturing live state.
- Do not make a handle an operator configuration node merely to reuse node resolution. It has no operation, advice or child graph of its own. Resource creation must not become a second dependency injection graph.
- A record copy keeps the same handle reference unless the user replaces it. It can therefore share with the original within a run while remaining an independent observation target. Separate runs still create separate resources.
- Handle settings are the single source of creation settings for a shared resource. For example, a wrapper's private cache limit must not silently override a supplied handle's limit. Final API design must avoid ambiguous duplicate inputs or reject conflicting inputs explicitly.
- Do not key resources by service type, operator type, configuration equality, configuration reference or an inferred owning ancestor. Different handles of the same resource type must coexist.

### Flat resource lookup and preparation

- One algorithm run creates one resource table. Every context derived with `With(aspect)` and every deferred child construction in that run uses the same table. There is no resource parent, shadowing or inheritance search.
- The table contains only explicitly requested or supplied handles. Resolving a stateful configuration without a handle does not create a table entry.
- A supplied instance wins over default creation. Register it before execution preparation. Reject duplicate supply for a handle and reject supply after the run starts, including while paused. There is no replacement of a live binding.
- Default creation is lazy so unused handles do not allocate resources. Ensure one published instance per handle if supported construction paths can race. Do not assume that making the table safe also makes resource use safe.
- If default creation throws, publish no binding. A later direct construction attempt can retry. If resource creation succeeds and a later executor or advice build fails, retain that resource for the same run. Do not silently reset it on retry. A terminally failed `AlgorithmRun` need not become resumable to support this direct construction behavior.
- Preserve preconstruction advice checks and recursive configuration construction detection. Separate that guard from the state owner table being deleted.
- `AddResourcePerTrial` runs during preparation against already materialized trials. It does not create hidden trials or depend on scheduling order. Specify its failure behavior using the existing preparation conventions and ensure partial setup cannot start trials unexpectedly.
- Direct `ResolutionScope` users need the same typed mechanism with an explicit independent table per root. Choose a small setup entry point rather than introducing a second lifetime model. Its mutability boundary is part of the first proof.

### Executors, advice and controls

Private state follows executor identity. A caller invokes the executor it resolved and gets controls from that same advised executor. Resolving a configuration twice is not a way to get two views of automatically shared private data.

Several operations can share a deliberately exposed resource. The acceptance proof must use a consumer defined operator implementing several operations, with separate executor constructions and distinct advice contexts. It must not add a cache of all executors for the configuration or a catalog of built in operations.

Keep existing advice owned data as advice owned data. `OA(Op)` and `OA(IA(Op))` remain different wrapper chains, and their outer advice may receive the same accumulator directly. Do not route every analyzer, clock or observation through handles. A budget whose private data moves to its executor constructs its counting or timing advice for that construction. If a budget exposes a shared accumulator, share the accumulator without accidentally sharing contextual advice identity or children.

### Cycle and other deferred construction

Fresh turns resolve their inner configurations again through the current context and the same run resource table. Search state handoff remains unchanged. Fresh stopping state must be independent even when the same terminator configuration was resolved earlier elsewhere.

Remove `ChildScopePolicy.Shared` as a blanket sharing mechanism. Its callers choose explicit resources. Remove `ChildScopeSelector` and state rebinding. Replace Retained with the accepted explicit executor reuse option. Keep the reuse store on one Cycle executor and key it by child configuration reference. With reuse enabled, `Cycle(A, A)` uses the same A executor at both positions. Separate configuration objects get separate retained executors. Test this explicitly. Never place this reuse cache in the resolver.

Review Pipeline and DynamicRacing separately. Their fresh stages or contenders still need current advice and explicit search state handoff, but no private state scope. With only the accepted run resource lifetime, a handle reused by two nested Cycle executions or racing contenders shares across both. That is an accepted limit, rather than an invitation to infer a nested owner.

### Cache validity and concurrent sharing

A handle declares identity, not semantic compatibility. Sharing a cache is correct only when its participants agree about key interpretation and evaluation semantics. Ordinary per trial creation protects grids from unintended cross trial reuse. Deliberately supplying one cache across differing problems or evaluator settings can still be wrong.

Define the cache resource contract before exposing it. Account for candidate and key types, comparers, evaluation meaning and invalidation. The dynamic resource must keep entries, epoch and related hit accounting coherent. Do not capture a context's child evaluator inside a shared resource. Document or reject incompatible participants using facts the resource can establish without pretending that arbitrary evaluator functions can be compared automatically.

Concurrent access correctness is part of the operator author's contract, including for user defined operators. A handle and a safe resource table do not serialize calls or protect the resource's mutable data. Authors must establish safe access for every participant when execution can overlap, whether within one run or across trials.

Use caching as a worked example in the user guide and its executable documentation specs. Synchronization belongs with the shared data or must otherwise use one shared synchronization object. A lock on each executor does not protect a cache shared by several executors. Prefer encapsulating the protocol in the resource so its callers cannot bypass it. Operator authors remain responsible for the safety of their key selectors, child calls and other shared dependencies too.

The current `Microsoft.Extensions.Caching.Memory.MemoryCache` already supports concurrent entry storage. Microsoft's [caching documentation](https://learn.microsoft.com/en-us/dotnet/core/extensions/caching#in-memory-caching) describes its concurrent dictionary implementation. That does not make a sequence of lookup, evaluation and publication atomic. Concurrent misses may evaluate the same key more than once, as the [cache population guidance](https://learn.microsoft.com/en-us/aspnet/core/performance/caching/memory?view=aspnetcore-10.0#review-notes-about-in-memory-caching) also explains. Duplicate evaluation on concurrent misses is permitted in this version for otherwise valid cache use.

Choose the smallest synchronization mechanism that protects the actual invariants. A read/write lock is a candidate, not a required API or an assumed improvement over existing concurrent storage. If the backing store changes, prove its access safety. Compound operations such as dynamic invalidation must coordinate cache entries, epoch and accounting together. Prefer evaluating outside the shared lock and checking validity again before publication so a cache does not unnecessarily serialize independent expensive work. Any stronger protocol must justify its cost and account for reentrant child calls.

The cache contract must also establish when reuse is semantically valid. Locking cannot make results from different problems, evaluator settings or incompatible stochastic evaluations interchangeable. The proof must distinguish safe storage, correct evaluation reuse and coordination of concurrent misses.

Strict shared evaluation budgets across concurrent trials are deferred. An atomic accumulator does not make a separate check, evaluation and increment a reservation. Do not advertise a strict shared limit without that additional protocol. The framework provides no automatic synchronization for arbitrary resources and does not promise reproducibility independent of scheduling for intentionally coupled trials.

### Ownership and cleanup

Accepted for this version: no automatic ownership or disposal of resources requiring deterministic cleanup. Caller supplied resources remain caller owned. Neither a trial nor a context may dispose a supplied instance that another trial still uses. The caller keeps such resources valid for all their users, including paused runs that may resume.

Default handle creation is limited to managed data that needs no deterministic cleanup. A resource that requires explicit cleanup must instead be supplied. Its handle must report missing supply explicitly rather than silently creating a resource with no owner. Choose the smallest authoring shape for this in P1 without a separate resource lifecycle hierarchy.

Audit both handle created and private executor resources. `MemoryCache` implements disposal, so its actual configured use needs a deliberate assessment. If it requires deterministic cleanup, use a suitable managed store for automatic creation or require an explicitly owned supplied resource. Do not equate lack of framework disposal with permission to omit necessary cleanup, and do not retain all fresh private caches until run completion merely to centralize their ownership.

Executors and scopes have no general disposal contract today. Preserve pause/resume and keep new disposal machinery outside this change. If automatic disposal is later justified, its separate design must cover normal completion, failed preparation, cancellation, abandoned paused runs and direct scope use. Stream disposal can pause an algorithm run, so it cannot release data required by resume.

### Equality, export and checkpoints

Accepted: a handle's identity participates in the equality of configurations that contain it. Two new handles with identical settings denote different sharing groups. Ordinary record copies preserve identity. This is predictable within one configuration graph, but does not make separately reconstructed graphs equal.

Grid duplicate detection must remain understandable under that rule. Test copies retaining a handle, copies replacing a handle and equal numeric parameter combinations created with new handles. Document that this is configuration equality including sharing identity, not a canonical comparison of graph topology. Do not add graph isomorphism or a settings only equality mode incidentally.

If configuration export is pursued after the rework, it must distinguish creation settings from sharing relationships. A graph format could assign identifiers to handle occurrences and reconstruct one handle object for each identifier without global identifiers on runtime handles. No serializer, graph persistence protocol or cross process equality contract is part of this change. Reassess the need with the existing experiment persistence backlog after the simplified system works.

Checkpointing runtime contents is separate from exporting a recipe. It also requires algorithm progress, iterator state, RNG state and compatible resources. No checkpointing or universal clone/reset protocol is included here.

## Concepts removed, retained and added

Remove configuration state owner entries, ancestor state selection, `GetScopedState`, `CreateScopedState`, `CreateChildState`, `WithState`, `ChildScopeSelector` and `ChildScopePolicy`. Remove or consolidate the `Stateful*` authoring families whose purpose is separate framework managed state. Do not preserve compatibility shims in this alpha migration.

Retain configuration/executor separation, typed operations, fixed child references, contextual advice and its ordering, observation identity, control forwarding, configuration cycle checks, run lifecycle, deterministic RNG assignment and trial scheduling. Aspect layering may still have a parent chain. Removing state ancestry does not mean removing the advice ordering model.

Add typed resource handles, one flat table per independent run/root and preparation methods for supplying resources. Add concrete shared resource types only where the audit demonstrates their meaning. Add the selected local Cycle executor reuse option. Do not add implicit resource discovery, nested resource lifetimes, factory hierarchies, runtime child dispatch, universal reset or deep cloning.

Intentionally give up automatic private state sharing between repeated configuration references, separately resolved operation views, differently advised reconstructions and different requested run types. Explicit resources cover selected needs, not arbitrary private state. The run lifetime also cannot represent every lifetime between one executor and the whole run.

## Detailed implementation sequence

P0 through P5 are complete. The implementation and final validation results are recorded below. Each step records its expected evidence before later steps proceed. Build and test commands run sequentially in this checkout.

P0 precedes the P1 proof. P2 through P4 form a coordinated production migration because deleting the old resolver API immediately breaks its authors and deferred callers. Migrate those callers before the final deletion, or deliver that deletion together with their changes in one compiling package. Do not present a broken intermediate build or a permanent dual state model as a completed step. P5 closes the migration after all callers use the new semantics.

### P0. Settle boundaries and audit sharing assumptions

Use the accepted lifetime, executor reuse, identity and cleanup boundaries. Audit all `GetScopedState`, `CreateScopedState`, `Stateful*`, `CreateChildScope` and child policy uses across `src`, `test`, `analyzers`, samples and documentation. Classify each as private data, a meaningful shared resource, local executor reuse or obsolete infrastructure. For each resource record its cleanup needs and concurrent access contract. Include PythonInterop and Python examples in caller searches.

Start with LimitEvaluator, the two budget wrappers, GaussianMutator, predefined candidate cursors, stateful terminators, DynamicCachingEvaluator, DynamicRacing and the documentation cursor example. Record places where one configuration is resolved more than once within one activation. Do not mechanically convert every former state object into a handle.

Exit evidence: a migration inventory with intended before/after behavior and an explicit answer for every sharing dependent workflow. The inventory must identify exact old tests to replace and old contracts to relinquish.

### P1. Prove the smallest public usage and authoring path

Write focused API usage specs for an ordinary private counter, `Cycle(A(E), B(E))` sharing one cache per trial, explicit supply across trials and a consumer defined resource. Include a consumer defined operation and two operation views sharing only a handle. Establish usable generic inference and one creation settings path before expanding built in APIs.

Implement only enough of the typed handle, flat table and runtime supply path to exercise these specs. Compare the concepts and authoring burden with the earlier enclosing composition declaration and shared wrapper alternatives. Those alternatives preserve generic state ownership and are comparison material, not parallel implementation targets.

Exit evidence: fresh fields require no state protocol, consumer extensions require no built in cases, two handles of one type remain independent and resource creation does not depend on the requesting context. Prove direct scope setup, missing explicit supply diagnostics and compliance with the accepted cleanup boundary. Exercise concurrent cache access through separate executors sharing one resource. Revisit a design that requires nested lookup, generic cloning or a factory graph.

### P2. Replace resolution ownership and connect run preparation

Remove state owner selection from all resolution paths in `ResolutionScope.cs`, `.Operators.cs` and `.Algorithms.cs`. Carry the same resource table through typed scopes and advice contexts. Preserve matching advice checks, wrapper order, control forwarding, cycle detection and current `TryResolve` exception behavior.

Connect `AlgorithmRun` preparation and `ExperimentRun` resource supply. Independent roots and trial runs allocate independent tables. Deferred construction sees the original table. Reject late and duplicate supply. Preserve pause/resume without rebuilding or releasing live resources.

Exit evidence: reversed construction order, distinct contexts, failure retries, separate roots and paused runs all obey the scenario contracts below. No hidden configuration state entry remains.

### P3. Migrate operator and algorithm authors

Move private data into executor fields and pass resolved children through constructors. Remove the separate state generic parameter and state creation hook where their only purpose was framework ownership. Keep stateless authoring conveniences when they still have a responsibility. Do not add one replacement hierarchy per resource kind.

Migrate caches with coherent optional resource contracts. Keep mutation strength on the executor and preserve control access through wrappers. Migrate budgets with private accumulators by default and explicitly shared accumulators only where required by the inventory. Address dynamic state and concurrent correctness in Experimental through the same public mechanism.

Update Roslyn authoring diagnostics and tests. `OperatorAuthoringAnalyzer` currently recognizes scoped state creation and requests, so removing methods without updating diagnostics would leave obsolete rules or gaps. Continue rejecting mutable configuration fields and execution graph dependencies retained as shareable resource data.

Exit evidence: all inventoried stateful leaves and composites use the accepted ownership model. Public APIs, samples and consumer defined test fixtures compile without shims.

### P4. Migrate deferred composition and experiments

Replace Cycle state policies with fresh construction and the explicit executor reuse option. Update overloads and callers. Migrate Pipeline and DynamicRacing without inventing new resource owners for stages or contenders.

Exercise repeated and grid experiments with shared handle references in configurations. Default trials remain independent even under concurrent scheduling. Explicitly supplied resources are the only new mechanism that couples trials. Validate configuration equality, trial keys, observation selection and failure preservation.

Exit evidence: the full representative scenario set passes, including the accepted executor reuse semantics, different advice contexts and the private terminator plus shared cache example.

### P5. Align contracts, examples and final validation

Update developer guidelines § 4.1, § 4.3, § 4.4, § 4.7, § 4.9, § 4.12, § 4.14 and § 4.17 as needed. Update execution nodes, execution resolution, operator implementation, layering, affected API guides, source XML comments and glossary entries together with the implementation. Keep decision history here and rejected alternatives in the backlog.

Replace the stateful composition walkthrough and its imported executable source. Explain independent configuration reuse, handle identity, fresh turns, observation identity and the limit of any executor reuse option. Preserve one executable source for published examples.

Include a caching example that demonstrates correct concurrent access to a shared resource and a clear operator author responsibility statement. Cover shared synchronization identity, compound updates and the distinction between safe storage and duplicate evaluations on concurrent misses. Show the chosen implementation rather than adding a read/write lock solely to illustrate a lock. Keep the public guide accurate about which caches support concurrent use.

Run focused core tests while changing behavior, then the core suite, API usage specs, documentation suite, affected Experimental tests and composed scenarios. Near completion run the full solution tests with one test module at a time, documentation build, whitespace/style/analyzer checks and packaging validation under the selected .NET SDK. Use the exact commands in `AGENTS.md` and [test/README.md](../test/README.md#validation-order). Check changed Markdown with Prettier.

Search for removed APIs and stale ownership promises after migration. Historical plans may retain them when clearly marked historical. No compatibility layer or dual state model is part of the delivered design. Make performance claims only after separate measurements.

## Scenario acceptance criteria

1. **Independent runs:** the same configuration creates fresh private data and separate default resources in two runs. Stateless executor reuse does not expose mutable data.
2. **Repeated references within one turn:** resolving one stateful configuration twice creates independent fields. Supplying the same explicit handle shares only the resource.
3. **Fresh Cycle turns:** terminators reset, the current search state carries forward and a cache handle survives all turns of one trial. Prior parent resolution does not change this.
4. **Trial isolation:** concurrent repeats and grid points each create one resource per handle. No setup callback is required for isolation.
5. **Deliberate sharing across trials:** supplying the same resource shares it. Supported shared caches and counters tolerate concurrent access. Strict shared budget enforcement is deferred and no test or documentation claims it follows from an atomic counter.
6. **Construction order:** parent then child, child then parent and siblings agree about resource sharing. Different handles never merge because their settings or resource types match.
7. **Advice contexts:** `OA(Op)` and `OA(IA(Op))` keep their wrapper order and may share an explicitly supplied outer accumulator. Sharing does not capture the first context's children.
8. **Composite children:** children with different advice contexts obtain distinct executors and the intended resource references. Resource data retains no child executor or scope.
9. **Controls and views:** adaptive controls affect the executor actually invoked. Separately constructed operation views are independent unless they use a common handle. Consumer operations work unchanged through the generic mechanism.
10. **Resume:** pausing and resuming keeps private fields, resources, children, advice and RNG continuation. Starting a new run does not.
11. **Configuration copies and grids:** copies retain handles but have their own observation identity. Equality and duplicate grid handling include handle reference identity. Numeric settings alone do not silently define resource identity.
12. **Failure and setup:** invalid advice is rejected before child work where currently promised. Failed resource creation publishes nothing. Later construction failures do not reset published resources. Duplicate and late supplies fail predictably.
13. **Nested composition:** under the accepted run lifetime, the same handle shared across nested compositions denotes one run resource. No implicit inner override or shorter lifetime appears.
14. **Dynamic caching:** invalidation, epoch and hit accounting remain coherent. Incompatible sharing is rejected or explicitly outside the cache contract. Concurrent support is demonstrated rather than inferred from table safety.
15. **Ownership:** a borrowed resource is never disposed by a trial. Default creation needs no deterministic cleanup. Resources that require it need explicit supply, and missing supply fails clearly. Paused runs retain their resource references without ending caller ownership.
16. **Persistence boundary:** API documentation distinguishes settings, sharing relationships and checkpoints without claiming that any unsupported round trip exists.
17. **Executor reuse:** the explicit Cycle option retains one inner executor per child configuration reference with its private data, children and advice within the owning Cycle executor. `Cycle(A, A)` reuses one A executor, while separate child configuration objects retain separate executors. A new Cycle executor has independent private data. Each turn still starts a new inner enumeration with the current search state. Reuse does not promise retained private state across different advice contexts or run types.
18. **Shared synchronization:** concurrent calls through separate executors access one cache safely. Any required synchronization is shared by those executors. Tests coordinate overlapping accesses deliberately and verify validity across publication and dynamic invalidation where supported. Duplicate evaluation on concurrent misses remains permitted and is not reported as corruption or strict budget enforcement.

Put lookup and lifecycle invariants in core unit tests, intended API syntax in API usage specs, dynamic behavior in Experimental tests and complete Cycle/experiment stories in scenario tests. Published walkthroughs remain in the documentation suite. Do not duplicate the same assertion suite in every project.

## Confirmed boundaries

- **Lifetime boundary, accepted.** Only executor private state and run resources initially. A trial is one run. Sharing for one nested composition would require another explicit design and is not inferred from handle placement.
- **Retained execution, accepted.** Keep an explicit Cycle option that reuses an inner executor by child configuration reference within one fixed Cycle executor. It retains children and advice too. Private state across differently advised rebuilds is intentionally not preserved.
- **Configuration equality, accepted.** Handle reference identity participates in containing configuration equality. Equal creation settings do not make separate handles interchangeable. A settings comparison or persisted graph equivalence would be separate work.
- **Cleanup, accepted.** No automatic disposal in this version. Default resources need no deterministic cleanup. Resources that need it must be supplied and remain caller owned.
- **Concurrent access, accepted.** The operator author provides correct access patterns for shared resources and documents supported concurrency. Caching supplies the worked example. Shared synchronization belongs with the resource, and the choice of a lock or concurrent storage follows its invariants.
- **Strict shared budgets, deferred.** Atomic counters do not guarantee that concurrent trials cannot overshoot a shared evaluation limit. Reservation semantics are outside this rework.
- **Special cases, deferred.** Reassess serialization, checkpoints, nested resource lifetimes and other extensions after the simplified library is working. Do not make them prerequisites.

No product decision above remains open. The implementation record captures the sharing inventory, focused proofs, production migration and broader validation. Deferred capabilities remain outside this change.

## Earlier alternatives

An enclosing composition declaration such as `SharedOperators = [cachedEvaluator]` and a wrapper such as `evaluator.Cached().Shared()` were considered. Both select sharing for arbitrary configuration state and need an owner/lifetime rule. Preserving contextual children also keeps separate state reconstruction machinery. They are not selected because the developer chose explicit resource sharing and executor private state instead.

See [the rejected approaches](developer-backlog.md#discussed-tried-and-rejected) for those alternatives, live resources in reusable configurations and the runtime object composition route. Existing rejections of resolver owned child retention, per context executor caches, advice created configuration children and generic invocation dispatch remain relevant. This plan does not reopen them.

## Implementation record

### P0 sharing inventory

Verified a clean `operator-unification` worktree at `90fd75d9`. The selected SDK is 10.0.402 under global.json roll forward. Baseline Release build passed with 157 warnings. All 2721 core tests passed.

| Existing owner                                                                 | Classification and migration                                                                                                                                                           | Relinquished behavior and replacement evidence                                                                                                                                                                                                       |
| ------------------------------------------------------------------------------ | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| LimitEvaluator                                                                 | Private count per executor by default. RefinerEvaluatorAccountingTests exposes deliberate allowance sharing, so an optional CountAccumulatorHandle preserves that workflow explicitly. | Repeated resolutions no longer consume one implicit allowance. Update EvaluationResolutionTests. Strict concurrent budgets stay deferred.                                                                                                            |
| OperatorBudgetAlgorithm and OperatorDurationBudgetAlgorithm                    | Private accumulator and newly constructed advice per executor. Existing advice supplied accumulators stay advice owned.                                                                | Rebuilds restart usage. Update DeferredAlgorithmResolutionTests budget cases while retaining contextual observations and pause/resume.                                                                                                               |
| GaussianMutator                                                                | Mutation strength field on its executor.                                                                                                                                               | A control affects the same advised executor only. Replace cross resolution sharing expectations in ConcreteOperatorResolutionTests and control tests.                                                                                                |
| PredefinedCandidatesCreator and documentation cursor                           | Private cursor field.                                                                                                                                                                  | Each construction restarts its cursor. Update ConcreteOperatorResolutionTests and replace StatefulCompositionDocumentationSpecs.                                                                                                                     |
| AfterIterationsTerminator, AfterElapsedTimeTerminator and StagnationTerminator | Private counter, timer and history on executors.                                                                                                                                       | Fresh turns always restart stopping data, including after a parent resolution. Explicit Cycle reuse retains it.                                                                                                                                      |
| CachingEvaluator                                                               | Optional typed evaluation cache handle. No handle means one private cache per executor.                                                                                                | Shared cache tests must opt in explicitly. Update EvaluationResolutionTests and add concurrent cache proofs.                                                                                                                                         |
| DynamicCachingEvaluator                                                        | Optional typed dynamic cache resource coordinating entries, epoch and grace accounting.                                                                                                | One shared synchronization protocol, evaluation outside the lock and epoch validation before publication. Update DynamicEvaluationCacheTest.                                                                                                         |
| DynamicRelativeQualityEvaluator and ReevaluationInterceptor                    | Private executor fields.                                                                                                                                                               | Reconstructed contexts restart history and epoch tracking. Update Experimental resolution tests.                                                                                                                                                     |
| DynamicRacing                                                                  | Private hall of fame and incumbent data on its executor. Resolve fresh contenders through the current scope.                                                                           | No contender state owners. A handle reused by contenders denotes one run resource. Update DynamicRacingResolutionTests.                                                                                                                              |
| Cycle                                                                          | Local executor reuse dictionary keyed by child configuration reference when enabled.                                                                                                   | Replace Fresh/Retained/Shared tests in DeferredAlgorithmResolutionTests, CycleAlgorithmTests, AlgorithmObservationTests and CompositionSpecs. Remove retained state across rebuilt Cycle executors and run types. `Cycle(A, A)` reuses one executor. |
| Pipeline                                                                       | Fresh executor per stage through the current scope.                                                                                                                                    | No state ancestry. Search state and contextual advice remain.                                                                                                                                                                                        |
| Stateful role families including Experimental move roles                       | Remove separate state parameter and creation hook. Author executors directly.                                                                                                          | Migrate OperatorAuthoringSpecs, MoveResolutionTests and stateful test fixtures. No replacement resource for ordinary counters.                                                                                                                       |
| Resolution owners and child state APIs                                         | Remove obsolete infrastructure. Retain a separate construction cycle guard and advice layering.                                                                                        | Replace ancestry tests in ExecutorResolutionTests.Selection/Lifetimes, TopologyResolutionTests, StateViewResolutionTests and MixedOperatorResolutionTests with private independence and explicit resource identity tests.                            |
| OperatorAuthoringAnalyzer                                                      | Replace scoped state recognition with resource graph dependency checks. Keep configuration mutation and observation point checks.                                                      | Remove the obsolete state generic argument diagnostic and migrate analyzer fixtures.                                                                                                                                                                 |

Repeated references occur in separate operation views, parent and child advice contexts, composite child slots, repeated Cycle positions and repeated experiment configurations. Their private data becomes independent except for explicit Cycle executor reuse. Handle identity alone selects resource sharing. Search state handoff and observation reference identity are unchanged.

Caller searches include src, test, analyzers, samples and python-samples. PythonInterop and Python examples contain no scoped state authoring calls. Experimental's commented RoarNet sketch is stale authoring material and will be migrated or removed with obsolete references.

### Resource and cleanup decisions

The cache implementation will use a bounded managed dictionary with shared synchronization, avoiding a new lifecycle obligation for automatically created or private caches. The existing MemoryCache exposes disposal and its eviction machinery is unnecessary for this contract. The new store needs no deterministic cleanup. It retains at most SizeLimit entries when bounded and permits duplicate evaluations on concurrent misses. Key equality uses the key type's default comparer. Participants must agree on key interpretation and evaluation semantics.

Dynamic caches additionally coordinate epoch and grace accounting in the resource. They reject different source problem identities and incompatible grace settings. No resource retains a child executor, scope or advice context. Authors remain responsible for key selectors, evaluators and dynamic problem concurrency.

The minimal handle contract has a protected creation method with a supplied only default. A consumer can subclass it for immutable creation settings. Handle equality is reference identity. Direct roots accept AddResource before their first resolution or resource lookup. Algorithm runs accept supply only while Preparing. Experiment callbacks see materialized trial configurations and finish before any supplies are applied, following AddAspectPerTrial preparation conventions. Duplicate supply and partially started trials are checked before applying the batch.

### P1 proof results

Implemented ResourceHandle<TResource>, one flat resource table per root/run, AddResource and AddResourcePerTrial. Handles seal object equality to reference identity. The default creation method reports missing explicit supply. Direct roots freeze supply on first resolution or lookup. Creation failure publishes nothing and later executor failure preserves successful creation.

Six OperatorPreparationSpecs passed, including a consumer defined operation, an operator with two operation views, private counters, contextual advice, record copies, same typed handles with distinct identities, Cycle cache reuse and independent default trials. Runtime supply deliberately shares a cache across trials with the callback selecting the materialized configuration's handle. Four ResourceTests passed, including coordinated overlapping misses through separate executors, concurrent lazy publication, failure retry and borrowed ownership.

The authoring proof needs only an ordinary executor field for private data and scope.Resource(handle) for explicit sharing. It needs no ownership declaration, scope ancestry, cloning protocol or factory graph. Unlike the rejected enclosing declaration and shared wrapper, handles identify only deliberately exposed resources. The ordinary cache has one creation settings path and rejects a wrapper SizeLimit combined with a handle.

### Additional migration finding

The first migrated core run identified an intentional refiner allowance workflow in RefinerEvaluatorAccountingTests.WithASharedBudget_ComparisonEvaluationsConsumeTheAlgorithmsBudget. LimitEvaluator therefore accepts an optional typed count handle. Its default remains private. The handle shares atomic accounting but does not turn check then evaluate then increment into a strict concurrent reservation. This is the explicit accumulator case already allowed by the accepted design. The refiner cache workflow likewise uses an explicit cache handle.

### P2 through P4 migration results

This records the initial migration. The accepted follow up above supersedes the original scope preparation and cache implementation choices.

The resolver now uses a flat run resource table and an independent recursive construction guard. Every executor construction receives current children and advice with independent private fields. No state ownership or child scope API remains. AlgorithmRun binds resources during preparation and retains those bindings through pause/resume. Experiment supply materializes callbacks and validates all bindings before applying any.

Core and Experimental authors use private executor fields. Caching exposes explicit handles. LimitEvaluator exposes an optional CountAccumulator handle for the intentional refinement allowance workflow. Other budgets remain private and construct their advice per executor. Cycle reuse is keyed by child reference within one Cycle executor and retains its children and advice. Pipelines and racing contenders resolve fresh executors.

Managed evaluation caches replace MemoryCache because these automatically created resources require no deterministic cleanup. Optional bounded storage uses insertion order eviction. Ordinary cache access is synchronized in the resource. Dynamic cache access coordinates entries, epoch and grace accounting under one shared lock, evaluates outside it and validates the epoch before publication. Its participants must agree on source problem identity and grace count. Evaluation meaning and key compatibility remain the author's responsibility. The unused MemoryCache package reference is removed.

The consumer proofs cover custom operators and operations, handle equality, concurrent repeated trials and explicit cross trial supply. Core tests cover failed creation retry, preserved resources after construction failure, setup failure without partial bindings, pause/resume, grid equality and overlapping cache misses. Dynamic tests cover overlapping misses spanning an epoch change and concurrent grace accounting. Scenario tests distinguish fresh private fields, reused executors and explicitly shared accounting. Existing advice ordering, observation identity and control forwarding tests remain.

### P5 validation record

Before final formatting and review, core passed 2708 tests, API usage passed 213, documentation passed 18 and Experimental passed 244. Additional analyzer and cleanup tests were then added. The first scenario pass found one remaining implicit shared counter assumption. It now declares a CountAccumulator handle explicitly. Final counts and broader checks are recorded after completion.

Final validation used SDK 10.0.402 in the existing operator-unification worktree. Build and test commands ran sequentially. No compatibility shims or permanent dual state model remain. Searches found no removed API calls or stale ownership promises in current production code, analyzers, tests, samples or published documentation. Historical plans remain decision records.

| Check                                                                                                                | Result                                                                                                                  |
| -------------------------------------------------------------------------------------------------------------------- | ----------------------------------------------------------------------------------------------------------------------- |
| `dotnet restore`                                                                                                     | Passed after removing the unused MemoryCache package reference.                                                         |
| `dotnet build --configuration Release --no-restore`                                                                  | Passed with 318 analyzer warnings and no errors. Warnings include unused parameters in migrated test helpers.           |
| `dotnet test --solution ./HEAL.HeuristicLib.slnx --configuration Release --no-restore --max-parallel-test-modules 1` | Passed all 3215 tests with none skipped. Core 2713, API usage 213, documentation 18, Experimental 244 and scenarios 27. |
| `dotnet format whitespace ./HEAL.HeuristicLib.slnx --verify-no-changes --no-restore`                                 | Passed.                                                                                                                 |
| `dotnet format style ./HEAL.HeuristicLib.slnx --verify-no-changes --no-restore --severity warn`                      | Passed.                                                                                                                 |
| `dotnet format analyzers ./HEAL.HeuristicLib.slnx --verify-no-changes --no-restore --severity error`                 | Passed.                                                                                                                 |
| `npm run docs:build`                                                                                                 | Passed, including executable source imports.                                                                            |
| `dotnet pack --configuration Release --no-restore`                                                                   | Passed for core, Experimental and PythonInterop.                                                                        |
| `npx --no-install prettier --check` on changed Markdown, JSON and YAML                                               | Passed.                                                                                                                 |
| `git diff --check`                                                                                                   | Passed.                                                                                                                 |

Obsolete state ownership and removed stateful family tests were replaced by executor independence and resource contract coverage. The final API cache proof combines concurrent repeated trials, fresh terminators on every Cycle turn and one cache per trial. Consumer operations additionally share a resource through distinct advice contexts. Explicit cross trial supply, disposable supply boundaries and dynamic concurrency have dedicated proofs.

All changes remain uncommitted on operator-unification at base commit 90fd75d9. The main checkout was not modified.
