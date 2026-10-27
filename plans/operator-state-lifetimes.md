# Operator state lifetimes

Status: refined option 1.2 accepted as the architectural direction on 2026-10-27. Implementation has not started. Private state moves into executors, explicit resource handles describe sharing and generic scoped state is removed. Lifetime, executor reuse, identity, cleanup scope and concurrency responsibility are accepted below. Exact APIs and resource contracts need the focused proofs described here.

This plan supersedes the earlier recommendation to start with option 1.1. Published architecture documentation continues to describe the implementation at `26d051e8` until the implementation changes. The glossary records resource terminology as provisional without changing the current scoped state contracts.

## Problem and accepted decision

The current resolver builds executors with the requesting context's children and advice while selecting persistent state through configuration ownership and ancestor lookup. This separates observation context from state lifetime, but ordinary operator authors pay for ownership and reconstruction rules even when they need only private fields.

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

First restore a coherent working library under this model. Serialization, checkpoints, nested resource lifetimes and other special cases are later candidates to assess against concrete needs, not prerequisites for the migration. The sharing audit and focused API proofs remain the next work.

This is an architectural change with intentional breaking semantics. Approval of the direction is recorded here and needs no repeated approval. The work requested in this session is planning, not implementation of the proposed API.

## Review findings

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

### Findings grounded in the current source

- [ResolutionScope](../src/HeuristicLib/Execution/ResolutionScope.cs), especially `State.GetOrAddOwner`, `FindOwner` and `GetScopedState`, confirms that sharing depends on configuration references and ancestor ownership. Child selection stays local if it happens first. Parent selection can be reused by descendants if it happens first. Both rules explain the order dependence.
- [CycleAlgorithm](../src/HeuristicLib/Algorithms/Composition/CycleAlgorithm.cs) constructs an inner executor on every turn and carries search state forward. [ChildScopeSelector](../src/HeuristicLib/Execution/ChildScopeSelector.cs) chooses fresh, retained or parent state for the whole activation. Those policies alone do not express one shared cache alongside fresh terminators.
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

These sketches preserve the review's usage model. Names, overloads and generic inference remain illustrative. They are not compilable examples of the current library.

```csharp
var store = new EvaluationStoreHandle { SizeLimit = 100_000 };
var evaluation = evaluator.Cached(store);
var cycle = Cycle.Create(
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

The second line deliberately supplies one instance to every trial. Returning a new instance from each callback supplies independent resources. Neither callback is necessary for the ordinary independent trial case. The final callback parameter must allow users to select the handle on a materialized trial configuration, especially when grids vary handles.

The author side has one explicit choice:

```csharp
var cache = handle is null
    ? CreatePrivateCache(settings)
    : scope.Resource(handle);
return new Executor(childEvaluator, cache);
```

`CreatePrivateCache` and `settings` stand for that operator's own construction code. Ordinary counters and adaptive values need no resource object at all and live directly in executor fields. A handle is appropriate only when an author intentionally supports sharing a coherent resource.

## Proposed contracts and boundaries

The accepted direction above is fixed. The following details develop those decisions into implementation recommendations, subject to the focused proofs. They must not be treated as already implemented.

### Identity and creation

- Use a public typed handle contract, provisionally `ResourceHandle<TResource>`, with immutable creation settings and a creation method. A consumer can define both its resource and handle without a library registration list.
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

All implementation steps below are pending. Each step records its expected evidence before later steps proceed. Build and test commands run sequentially in this checkout.

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
13. **Nested composition:** under the proposed run lifetime, the same handle shared across nested compositions denotes one run resource. No implicit inner override or shorter lifetime appears.
14. **Dynamic caching:** invalidation, epoch and hit accounting remain coherent. Incompatible sharing is rejected or explicitly outside the cache contract. Concurrent support is demonstrated rather than inferred from table safety.
15. **Ownership:** a borrowed resource is never disposed by a trial. Default creation needs no deterministic cleanup. Resources that require it need explicit supply, and missing supply fails clearly. Paused runs retain their resource references without ending caller ownership.
16. **Persistence boundary:** API documentation distinguishes settings, sharing relationships and checkpoints without claiming that any unsupported round trip exists.
17. **Executor reuse:** the explicit Cycle option retains one inner executor per child configuration reference with its private data, children and advice within the owning Cycle executor. `Cycle(A, A)` reuses one A executor, while separate child configuration objects retain separate executors. A new Cycle executor has independent private data. Each turn still starts a new inner enumeration with the current search state. Reuse does not promise retained private state across different advice contexts or run types.
18. **Shared synchronization:** concurrent calls through separate executors access one cache safely. Any required synchronization is shared by those executors. Tests coordinate overlapping accesses deliberately and verify validity across publication and dynamic invalidation where supported. Duplicate evaluation on concurrent misses remains permitted and is not reported as corruption or strict budget enforcement.

Put lookup and lifecycle invariants in core unit tests, intended API syntax in API usage specs, dynamic behavior in Experimental tests and complete Cycle/experiment stories in scenario tests. Published walkthroughs remain in the documentation suite. Do not duplicate the same assertion suite in every project.

## Confirmed boundaries and remaining proofs

- **Lifetime boundary, accepted.** Only executor private state and run resources initially. A trial is one run. Sharing for one nested composition would require another explicit design and is not inferred from handle placement.
- **Retained execution, accepted.** Keep an explicit Cycle option that reuses an inner executor by child configuration reference within one fixed Cycle executor. It retains children and advice too. Private state across differently advised rebuilds is intentionally not preserved.
- **Configuration equality, accepted.** Handle reference identity participates in containing configuration equality. Equal creation settings do not make separate handles interchangeable. A settings comparison or persisted graph equivalence would be separate work.
- **Cleanup, accepted.** No automatic disposal in this version. Default resources need no deterministic cleanup. Resources that need it must be supplied and remain caller owned.
- **Concurrent access, accepted.** The operator author provides correct access patterns for shared resources and documents supported concurrency. Caching supplies the worked example. Shared synchronization belongs with the resource, and the choice of a lock or concurrent storage follows its invariants.
- **Strict shared budgets, deferred.** Atomic counters do not guarantee that concurrent trials cannot overshoot a shared evaluation limit. Reservation semantics are outside this rework.
- **Special cases, deferred.** Reassess serialization, checkpoints, nested resource lifetimes and other extensions after the simplified library is working. Do not make them prerequisites.

No product decision above remains open. The remaining work is the sharing inventory and focused proofs for the chosen cleanup restriction, cache compatibility and concurrent access, typed authoring and setup failure behavior. Complete those before broad migration. Ask for another product decision only when the evidence exposes a material tradeoff, rather than asking the developer to choose implementation details that follow from the accepted contracts.

## Earlier alternatives

An enclosing composition declaration such as `SharedOperators = [cachedEvaluator]` and a wrapper such as `evaluator.Cached().Shared()` were considered. Both select sharing for arbitrary configuration state and need an owner/lifetime rule. Preserving contextual children also keeps separate state reconstruction machinery. They are not selected because the developer chose explicit resource sharing and executor private state instead.

See [the rejected approaches](developer-backlog.md#discussed-tried-and-rejected) for those alternatives, live resources in reusable configurations and the runtime object composition route. Existing rejections of resolver owned child retention, per context executor caches, advice created configuration children and generic invocation dispatch remain relevant. This plan does not reopen them.
