# Container and aspect framing

Status: packages 1 and 2 committed as `0236ab08`; packages 4a and 4b committed as `79be85b0`. On 2026-09-27 the user selected option C with typed execution factories, superseding the earlier option-A limitation. Child-scope bindings must be able to reach through shared composites while preserving logical execution state. The [factory design plan](execution-bindings-and-shared-state.md) now precedes the dependent package 4c integration; do not implement option A's reuse-rejection mechanism first. Resolution-time matching, stable predicates and decoration-independent state sharing remain agreed. Concrete contracts, ownership and migration still require review. Stop after each package; leave the Git index untouched.

Reviewed against `37e8bdcc` (the layering overhaul merged into `dev`) on 2026-09-26. Work continues on `container-and-aspect-framing`. The completed layering implementation plan and its obsolete analysis report have been removed; Git history preserves them. Durable rules live in [layering](../docs/contributing/architecture/layering.md), the [developer guidelines](../docs/contributing/developer-guidelines.md), the [design goals](../docs/contributing/design-goals.md) and the [developer backlog](developer-backlog.md).

Implementation update, 2026-09-27: the reviewed naming migration is committed as `d5ad7fb2`; C2 and C3 were completed and validated locally. The user has authorized direct implementation in the library and normal tests, with explicit review stops and temporary breaking checkpoints where needed. The first package extracts core persistent state while preserving existing creation behavior. C4 remains outstanding and will compare the integrated implementation against the previous version in a temporary worktree before final acceptance. The shipping resolver retains its existing behavior until the factory cutover.

## Purpose and settled foundations

Use DI and AOP as comparison models for concrete HeuristicLib authoring problems. Extend selection and attached behavior where they help users work with algorithm and operator graphs. An analyzer is already an execution module that owns results. The new capability is selecting more than one known configuration reference and expressing typed behavior beyond successful-operation observation.

**Settled design approach:** discuss a choice when its package needs it, using the current implementation and representative HeuristicLib usage. Do not copy AspectJ semantics or AOP vocabulary merely for conformity. Names must make sense to users who know algorithms and operators but do not know AOP. `Pointcut`, `JoinPoint`, `Advice` and `Aspect` are working comparison terms, not accepted public type names. `NodeSelector` is now the preferred name for configuration-node selection; its API shape remains open. Choose names alongside the responsibility they express, including whether selection addresses configuration objects, graph occurrences, execution nodes or operation boundaries. This does not reopen settled foundations.

Preserve these settled foundations:

- Configuration-reference resolution, explicit instance creation and declared child resolution. Type matching selects advice targets; it never supplies dependencies by type.
- Typed role contracts and handwritten wrappers. No dynamic proxies, reflection emit, universal role invocation or runtime service locator.
- Node selection addresses source configuration nodes. Wrappers introduced by decoration are excluded from selection, even when they are internally represented by configuration objects.
- `IAnalyzer : IExecutionModule` with one installation contract. Keep `Attach`, `AttachPerTrial` and `GetAttached` as the user-facing attachment methods.
- Run-specific lifecycle ownership. `AlgorithmRun` keeps its lifecycle and retained enumerator; `ExperimentRun` owns trial scheduling. No shared `RunHost`.
- Concept namespaces with corresponding source folders and optional grouping folders beneath them. Do not arrange namespaces or assemblies by layer.
- Nesting pointcuts must not silently split or merge mutable execution state. Selecting observations must not change which callers share the underlying state.
- Underlying execution-node reuse must be independent of attached decorations. A matching child registration must not cause a fresh underlying instance when an ancestor already provides one. Scope-specific wrapper identities and their state ownership remain design questions; this correction is not implemented yet.
- Child scopes may add modules. Under selected option C, a new binding of a shared composite must reach the child's required observations without resetting logical state or changing existing parent/sibling bindings. Parent registrations still apply to deferred source-node resolution. Silent omission remains unacceptable; case 5 and the factory plan define the design work needed to satisfy this requirement.

A recommendation below is not a settled decision. Current behavior remains the baseline until an explicitly reviewed change replaces it.

Initially confirmed on 2026-09-26: typed reference, role and type matching; preservation of current reuse and ordering; deferred nesting; and retention of `Interceptor`. Subsequent scope review supersedes blanket reuse preservation: a child decoration must not force another underlying instance. Existing ordering remains the comparison baseline, with its extension to wrappers around reused instances under discussion. Whether state transformation should eventually move from the operator role to typed algorithm advice remains a separate open question.

## Pre-factory implementation baseline and corrections

This section records the behavior before the factory cutover, including the former `Decorate` spelling and public `DecorationOrigin`. The current M1a working tree implements preparation and contextual binding, including reuse through composites; consult the [factory design](execution-factory-design.md#resolver-responsibilities) for that implementation. The reviewed [wrapper registration vocabulary](execution-factory-design.md#wrapper-registration-and-internal-ownership) now uses `Wrap`, `WrapperRegistration` and `WrappedNodes`, with private module-ordering metadata. Later advice signatures and precedence extensions remain open. The baseline below remains useful for behavioral comparisons and is not a description of the new resolver.

### Resolution and construction

The implementation is in [ResolutionScope.cs](../src/HeuristicLib/Execution/ResolutionScope.cs); existing behavior tests are in [ResolutionScopeTests.cs](../test/HeuristicLib.Tests/Execution/ResolutionScopeTests.cs).

- `Decorate` registers `Func<TConfiguration, TConfiguration>` against one configuration reference. `NodeSelector<TConfiguration>` now provides standalone reference/type matching, union/intersection and typed `And(predicate)` conditions, but is not yet accepted by `Decorate` or observation APIs. There is no role/type registration mechanism in the resolver, built-in name metadata or caller-path matcher.
- `Resolve(configuration, create)` searches the resolving scope and its ancestors. At each scope it checks under-construction instances, cached instances, then declarations for the target. A declaration stops the upward search. If nothing is reusable, resolution builds and caches in the resolving scope.
- Identical decoration chains permit ancestor reuse; they do not guarantee sharing. A child that resolves first keeps its instance, and a parent resolving later builds its own. Siblings cannot inspect each other's caches, but both can reuse an instance already held by their common ancestor. The earlier claims that chain identity alone determines identity and that siblings never share were too strong.
- The barrier is per configuration. Decorating one target does not block ancestor reuse of an unrelated target. Reusing a cached parent configuration also reuses its already-resolved children; they are not resolved again under the requesting scope.
- A chain creates the raw instance first, then creates each wrapper through the supplied creation delegate. Temporary under-construction entries prevent wrappers from rebuilding children. Wrapper configurations produced by decorations do not automatically pass through a fresh top-level `Resolve` lookup. The agreed selector integration must preserve their exclusion from selection; that integration is not implemented yet.
- Builder and scope are separate types, and `Build` snapshots declarations. The callback can retain the builder and mutate it later without affecting the existing scope. The escaped-builder test covers this. The earlier claim that such code cannot be written was incorrect.
- A run owns a root scope and may contain child scopes or several execution graphs. Scopes have no execution-node disposal contract. The DI analogy must not imply conventional lifetime registrations or deterministic disposal.

### Attachments, observation and instrumentation

[AlgorithmRun](../src/HeuristicLib/Execution/Runs/AlgorithmRun.cs) accepts modules while preparing, deduplicates by reference and installs in attachment order at execution start. [ExperimentRun](../src/HeuristicLib/Experiments/ExperimentRun.cs) creates per-trial attachments; attaching the same trial factory twice is rejected. These are completed layering work.

Current observation wrappers cover algorithms, evaluators, crossovers, mutators and interceptors. Operator callbacks run after a successful operation. [Algorithm observation](../src/HeuristicLib/Analysis/Tracing/Observations/AlgorithmObservation.cs) runs for each yielded search state before forwarding it, rather than once after the async stream completes. There is no general before/throwing/finally/around advice API.

Counting increments after successful calls; duration measurement records in `finally`, including failed calls. Their sinks are now `Instrumentation.CountAccumulator` and `DurationAccumulator`. Reusing a wrapper configuration with a caller-owned sink shares that sink even across independent execution nodes. Budgets create their instrumentation during instance creation. Advice objects, analyzers, clocks and underlying execution nodes need separate ownership discussions.

The dynamic-problem observation dependency remains the exact exception in [layering](../docs/contributing/architecture/layering.md#dependency-rules). Removing it depends on algorithm-boundary advice, not merely pointcuts.

### Precedence

`ResolutionScope.Chain` orders wrappers **innermost to outermost**: configuration origin before module origin, deeper scope before shallower scope, then earlier decoration declaration before later declaration. `Install` stamps module origin and deduplicates module objects per builder. The final key is decoration sequence, including nested installations, not a separate rank per module.

For wrappers A then B at the same origin and depth, the chain is `B(A(target))`. Entry work runs B then A; successful exit callbacks run A then B. "First installed runs first" is therefore incorrect for before/around advice. Traces install clocks first so their callbacks update before the trace reads them.

Configuration-origin duration budgets stay inside module observations so they exclude observer callback work. Around advice that retries, suppresses calls or throws introduces ordering questions the current sort keys do not answer.

### Names and analogies

`ResolutionScope`, `ResolutionScopeBuilder`, `Decorate`, `IExecutionModule`, `Install` and the standalone `NodeSelector` API are current API names. `Decoration` is internal; `DecorationOrigin` is public. `ExecutionSignature.Fits` checks execution-type compatibility, not complete graph validity or configuration settings.

`Interceptor` remains canonical for the role transforming a produced search state. No role rename is planned. The proposed AOP vocabulary uses advice, so it does not require reserving `Interceptor` for a second concept.

`NodeSelector` names the first standalone selection API under review. Pointcut remains an AOP comparison term; advice and aspect names and their type shapes are still provisional. `IConfigurationNode` and `IExecutionNode` are now the common contracts, with `...Execution` on role-specific runtime contracts and bases; the naming migration preserves the existing object-returning creation methods and resolution behavior. "AspectJ-style" is inspiration, not a matching specification. A CLR type attribute cannot hold a different runtime name for each instance of a proposed `Named` wrapper. Also, directly creating a child bypasses decoration of that child, but descendants it resolves through the scope can still receive their own decorations; earlier prose overstated that bypass.

## Decisions before dependent implementation

### D1: State transformation responsibility and naming

**Settled naming decision:** retain `Interceptor`. With advice as the proposed AOP vocabulary, there is no naming collision to resolve. `StateTransformer` describes the operation but does not convey its iteration timing more clearly, so the rename would not solve the stated problem. See the [rejected rename rationale](developer-backlog.md#renaming-interceptor-only-to-make-room-for-aop).

**Open:** whether state transformation needs a separate operator role or should become typed algorithm advice. Also decide whether the execution module contract serves as the public aspect abstraction; avoid an overlapping `IAspect` introduced only for the analogy.

Current [IterativeAlgorithmExecution](../src/HeuristicLib/Algorithms/BaseClasses/IterativeAlgorithm.cs) performs `step -> Transform -> IsTerminalState -> yield`, then retains the transformed state for the next step and completion check. `Transform` receives the previous state, iteration RNG, search space and problem. `RemoveDuplicatesInterceptor` is a concrete population transformation; pipelines and stateful implementations are also supported. [IterativeAlgorithmExecutionTests](../test/HeuristicLib.Tests/Algorithms/IterativeAlgorithmExecutionTests.cs) characterize termination order and the iteration RNG.

An ordinary wrapper around `RunStreamingAsync` can replace a state seen by the outer consumer but cannot replace the inner iterator's retained state or undo its termination decision. Pointcut selection alone does not create that missing boundary. Equivalent advice requires an explicit typed state-transformation boundary inside the iteration loop, with a contract that consumer-defined algorithms can implement. It must not silently apply to algorithms that expose only a stream.

| Option | Benefit | Cost or condition |
| --- | --- | --- |
| Keep an explicit role | A reusable, configured algorithm dependency with existing composition, validation and state sharing. | Maintains a full operator family alongside algorithm advice. Clarify which concern belongs where. |
| Replace the role with algorithm advice | One mechanism for selecting and composing algorithm-boundary behavior. | Needs the internal typed boundary, configuration/value semantics, shared-state ownership and equivalent termination/feedback/RNG behavior. Stream wrappers alone are insufficient. |
| Keep transformation strategies behind an advice adapter | Separates reusable transformation logic from where it is applied. | Risks two overlapping public authoring surfaces; justify with real reuse before adding both. |

**Recommendation, not decision:** defer replacing the role until the typed algorithm boundary has a concrete design. Compare the first two options with a deduplication example and a stateful transformation shared by two algorithms. The proposal must preserve next-step input, termination, RNG, pausing, ordering and shared state. Do not implement both surfaces merely to postpone the choice.

Retaining `IExecutionModule` and `Install` is a proposal independent of this role decision. Proposed AOP type names such as `Advice`/`AroundAdvice` still need API design; keeping `Interceptor` does not settle those signatures. The precise operator boundary remains after a successful step and before termination evaluation and publication, with the transformed state feeding the next iteration.

The earlier proposed glossary status `Analogue` is optional. The first package uses an explicitly labelled analogy table and keeps canonical terms unchanged.

### D2: Node selection and typed authoring

**Settled initial scope:** typed reference, role and type matching. Attributes, names, wildcard strings and nesting are deferred. The selection discussion on 2026-09-26 adds these requirements:

- Keep reference identity selection and preserve the selected role's typed advice data. Selecting a crossover reference must permit crossover-specific information rather than reducing it to an opaque call.
- Role selection covers matching algorithm/operator configurations throughout the applicable graph, with typed advice. An optional predicate receives the typed configuration and returns whether to include it.
- Type matching includes derived types and interface implementations. Exact concrete type equality is not the default.
- Support composing selections, including several references and filtered role selections. Union and intersection retain the shared declared configuration type or role; mixed-role combinations remain deferred.

**Settled selection boundary:** selectors match source configuration nodes, including for broad role or future wildcard selections. Wrappers introduced by weaving/decorating a selected source do not become selector inputs. All registrations selecting a source see that same original configuration, not the progressively wrapped configuration produced by earlier registrations. Observation attribution retains the selected source. This is an integration rule about which nodes are offered to selectors, not a type-based exclusion inside `NodeSelector.Matches`.

If modules A and B select source M at the same origin and scope, both registrations apply to M; B does not select A's wrapper. Current ordering produces `B(A(M_instance))` when A declares first. Entry runs B then A; successful exit callbacks run A then B. Origin and scope-depth precedence still apply, and declaration sequence remains the final key. Selection does not introduce another ordering rule.

Explicit wrappers already present in the source configuration graph remain configuration nodes and are eligible for selection. Consequently, an explicitly configured named wrapper and its child can both match a broad role selector. Excluding woven wrappers does not turn explicit configuration wrappers into metadata. Delayed source configurations remain eligible as they are resolved; the distinction is their role in source composition versus decoration, not whether the object existed at run start.

Acceptance case for resolver integration: two modules use the same broad mutator selector, and their observation wrappers also implement the mutator role. Each source node receives both registrations once; neither module's generated wrappers are offered to either selector, including typed filter callbacks. Repeat with an explicit source wrapper to prove it and its child remain separately eligible. This guards against recursive weaving and selection that changes merely because another module was attached.

**Current standalone API:** `NodeSelector<TConfiguration>` retains a configuration type or role. `NodeSelector.Reference(configuration)` uses reference identity and infers the reference's static type; `NodeSelector.OfType<TConfiguration>()` includes derived classes and interface implementations. `And(predicate)` adds a typed condition without mutating the original selector. `Matches` examines one supplied configuration and short-circuits on incompatible types or when a preceding predicate determines the result. Queries do not traverse graphs, resolve instances or cache results; exceptions from evaluated predicates propagate. Resolver integration is not implemented; its agreed evaluation timing is described below. API usage specs show references declared as `IMutator<RealVector>` retaining that role through composition. Additional concrete-to-role adaptation and advice inference still need review with resolver integration.

Selecting a configuration does not by itself identify a particular caller edge or invocation.

**Settled composition design:** `.Or(other)` is union and `.And(other)` is intersection. The `|` and `&` operators delegate to those methods. Operands share the same declared configuration type or role. Construction creates a new selector and evaluates no predicates; matching evaluates the left predicate first, then the right only if the result is still undetermined. Original selectors remain unchanged. A selector remains a definition; none of these operations enumerates graph nodes.

**Settled predicate convenience:** `And(predicate)` constructs `new NodeSelector<TConfiguration>(predicate)` and delegates to `And(selector)`. The existing constructor already supplies a predicate selector; there is no additional predicate-selector type or special filtering implementation. This replaces the initial `Where` spelling, avoiding an enumerable-result implication and making intersection the single composition mechanism.

The `|` and `&` operands are both evaluated while composing selectors. Predicate short-circuiting occurs later, when matching a node. C# requires `operator true` and `operator false` as well as `|`/`&` to enable `||`/`&&`; no such truth operators are introduced because a selector has no per-node truth value until a node is supplied. See the [C# conditional logical operator specification](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/language-specification/expressions#12173-user-defined-conditional-logical-operators). Boolean matching short-circuiting and selector-expression construction are distinct.

**Settled overlap rule:** if an exact-reference branch and a broader type/property branch both match the same source, their union still yields one match. One behavior registered against that union must not execute twice per operation merely because two branches matched. Two separately registered behaviors remain two intended contributions. Same-role composition retains typed arguments/results for future advice; mixed-role composition needs its own callback design. An intersection applied after union constrains every branch, while constraining one branch before union leaves the other branch unaffected. Normal C# precedence applies: `a | b & c` means `a | (b & c)`; use parentheses for `(a | b) & c`.

**Settled for resolver integration:** evaluate selectors during resolution, including deferred source-node resolution such as children of `CycleAlgorithm`. Matching is not a per-operation condition or a one-time traversal at run start. Treat configuration predicates as stable because configurations are assumed immutable; changing captured run state is not a supported way to retarget an installed selection. This does not promise a particular predicate invocation count or settle match-cache placement, failure handling or execution-node ownership.

**Still open:** specify these before implementing the dependent resolver integration:

- Additional concrete-to-role ergonomics, open generic roles and any later complement/exclusion operators. A closed role such as `IMutator<TCandidate>` provides information an open role across all candidate types cannot statically provide to one callback.
- Match-cache ownership, retention and predicate-failure behavior, including what a repeated resolution does after a predicate throws. Stable matching results do not imply that execution nodes can be shared or hoisted.
- How typed selection proves advice can wrap the requested configuration/instance role, including a consumer-defined role. Keep stand-in compatibility checks; no universal invocation or built-in-role switch.
- Ancestor cache hits and child-scope registrations, including cached composites and the scope retained by delayed construction. Review the examples below before choosing the integration mechanism.
- Attribute inheritance, type-name/namespace/generic wildcard grammar, and runtime names with equality and persistence semantics. A naming wrapper adds a configuration identity and must not accidentally clone its child's state.

**Proposals for the remaining details, not decisions:**

- Separate underlying-instance lookup from binding attached wrappers. The previously proposed matching-registration barrier was rejected in scope review because it changes mutable state sharing. Inherited registrations retain their original declaration scope, origin and sequence; inheritance does not reinstall them. The examples below distinguish current behavior from the revised requirement; the cache and binding mechanism is still open.
- A mixed-role selection does not provide one role-specific argument/result shape. Compare keeping a group of independently typed registrations with introducing a common callback contract for concerns such as timing. Grouping typed registrations need not erase their types.
- Keep opaque-call advice outside the first selection implementation. A role-independent timing callback could be supported by handwritten role adapters that keep arguments/results in their typed wrappers. A continuation callback would be a separate advice design, not a universal operation on execution nodes. Consumer-defined roles need an explicit adapter path, and callback costs need measurement.

Pattern selectors matching nothing are valid and silent, as already recorded in the backlog. Configuration-taking trace shortcuts remain convenient entry points.

**Named-wrapper proposal:** a role-specific wrapper can hold a child and an immutable `Name` property. Name selection can then be an ordinary typed `And(predicate)` condition over that property; it does not require a CLR attribute, string wildcard grammar or another matcher primitive. The [node selection usage specs](../test/HeuristicLib.Tests.ApiUsageSpecs/Execution/NodeSelectionSpecs.cs) demonstrate this with a private consumer-authored `NamedMutator`, not a new library naming API.

The wrapper is an additional configuration node. A name predicate selects its boundary, while a broad mutator selector can match both the wrapper and its child; future resolver integration must make those separate boundaries visible rather than silently deduplicating them. The example resolves the child through the existing wrapping base and returns its instance, so two names over a shared child in one scope retain that child's instance. It does not assign metadata to the underlying child or change cross-scope cache rules. A universal `NamedOperator<T>` cannot acquire arbitrary nominal operator roles automatically; naming wrappers must preserve the chosen role contract.

**Settled for named wrappers:** named configurations belong to the source configuration graph. A named wrapper is an explicit selectable node, and its child remains a source node; the woven-wrapper exclusion does not hide either one.

Before shipping naming helpers, review duplicate names, comparison rules, wrapper equality/persistence and a possible shared naming interface. Names remain outside the first selector package; the property-filter example demonstrates the proposed direction.

**Graph-node vocabulary, agreed on 2026-09-27:** rename `IConfigurationNode` (including its generic form) to `IConfigurationNode`, and `IExecutionNode` to `IExecutionNode`. Role-specific runtime types use `...Execution`, such as `IOperatorExecution`, `IMutatorExecution` and `IterativeAlgorithmExecution`; ordinary configuration types retain names such as `IOperator`, `IMutator` and concrete algorithm/operator names without `Node`. The [complete naming family](execution-factory-design.md#agreed-naming-family) retains `ResolutionScope`, `NodeSelector`, `IExecutionModule` and run lifecycle names, and uses `ExecutionFactory` / `CreateExecutionFactory` for the factory rework. No shared `INode`, child-enumeration contract or generic invocation is implied.

Resolution is not a one-to-one lifecycle conversion: one configuration can produce several execution nodes in different scopes, and stateless operators can return themselves. The rename preserves those facts. Keep it in a behavior-preserving commit before the separate factory/resolution commit, following the [agreed boundaries](execution-bindings-and-shared-state.md#agreed-commit-boundaries). `CreateExecutionInstance` keeps returning an object in the naming commit; changing to `CreateExecutionFactory` belongs to the behavioral commit. The naming decision does not approve the remaining option-C contract details or start implementation.

The earlier `Anchor<TObservation>` experiment merely renamed one configuration reference and was removed. Selecting more than one known reference is what must justify the new abstraction.

#### Package 4c scope examples for discussion

The following records **current reference-decoration outcomes**, not the selected target behavior. Cases 1-3 remain required; case 4's state duplication and case 5's missed child observation must be addressed by option C. The earlier option-A acceptance of fixed composite bindings has been superseded. Let `P` be a parent scope, `C` its child, and `M` one source configuration whose factory creates a fresh stateful mutator execution. A root observation registration `A` selects `M`; a child registration `B`, where present, is also of module origin. The same configuration reference is used throughout each example.

| Case | Resolution sequence | Current outcome with equivalent reference decorations |
| --- | --- | --- |
| Inherited registration, no ancestor instance | Two sibling scopes each resolve `M`; `P` has never resolved it. | Each sibling builds its own raw instance with `A`. The registration is inherited once per chain; instances stay in the scopes that built them. |
| Existing ancestor instance | `P` resolves `M`, then either child resolves `M` without another matching declaration. | Both reuse `P`'s already-decorated instance. `A` is not added again, and mutable state is shared. |
| Nonmatching child registration | `P` resolves `M`; `C` adds a selector that rejects `M`, then resolves `M`. | `C` still reuses the parent instance. Having registrations for other targets is not a reuse barrier. |
| Matching child registration | `P` resolves `M`; `C` adds `B` matching `M`, then resolves `M`. | `C` builds a new raw instance and both wrappers: `A(B(M_child))`. This is not an extra wrapper around `M_parent`. Parent resolution remains unchanged. The order follows existing depth precedence for registrations of the same origin. |
| Cached composite | `P` resolves algorithm `G`, which resolves and retains `M`; `C` adds a registration matching only `M`, then resolves `G`. | `G` is reused, including its existing `M`. Resolution does not visit `M` again, so the child registration does not affect calls through `G`. Directly resolving `M` in `C` can build another instance without replacing the one held by `G`. |

Resolution order also matters: if `C` resolves first and `P` later, each keeps its own instance. A child-built instance is never visible to its parent or siblings. Equivalent selections alone do not establish execution-node identity.

**Deferred example in the current implementation:** `CycleAlgorithm` retains the scope that created its instance and resolves its configured algorithms in child scopes during execution. With `NewExecutionInstancesPerCycle = true`, it creates a child scope per algorithm visit. When no eligible ancestor instance exists, separate visits build separate instances and inherited registrations apply to each. An eligible pre-resolved ancestor instance can still be reused; a fresh child scope alone does not guarantee fresh instances. With the flag false, the cycle instance caches the first resolved instance per algorithm reference. Selector integration must cover deferred resolution without hoisting execution nodes into the scope that owns a selector registration or match cache. `PipelineAlgorithm` likewise retains its creating scope for delayed child resolution; reusing its instance from another scope does not replace that retained scope.

**Settled after reviewing the examples:** inherited selectors reach configurations resolved in descendant scopes, including deferred children. Siblings without an ancestor-provided instance retain independent underlying state; siblings reusing an ancestor-provided instance share that state. No matching targets is a valid, silent result, with no wrappers or callbacks. There is no requirement to materialize a joinpoint list. Case 4 must reuse the ancestor's underlying `M_0` regardless of the child's matching registration; building `M_1` merely to attach `B` is a defect in the current rule. Child-first then parent resolution still keeps separate instances; no hoisting is introduced. Sharing the observer's accumulator does not imply sharing mutator state.

**Selected reach, still-open mechanism:** option C constructs scope-specific typed bindings over shared logical state, including a reused composite's descendants. It must separate deferred child-state selection from observation context. Wrapper ordering/state ownership remain design questions. A resolution-scope subtree is not a configuration-graph subtree or a caller path; this change does not introduce general caller-path selection.

**Match-cache question remains separate:** caching stable yes/no results may avoid repeated predicate work, but must not move or merge execution nodes. Caching at the registration's ancestor scope can retain short-lived configurations from many child scopes; caching only in a resolving scope can repeat matching across cycles. Choose ownership after agreeing the observable scope cases rather than letting an optimization determine state sharing.

#### Case 4: shared underlying instance, attached behavior and ordering

Distinguish three responsibilities without yet adding public types: the source execution node and its mutable state, the wrapper objects routing a call, and the observer or accumulator receiving reports. An explicit wrapper in the source configuration graph remains a source node; this distinction does not authorize recreating its state as disposable decoration machinery.

**Confirmed acceptance case:** the outer algorithm uses ancestor-provided `M_0`; a newly constructed inner algorithm eagerly resolves its M dependency in the inner scope, which adds observer B. Three successful outer calls plus two inner calls produce five mutations on `M_0`, five reports to outer observer A and two reports to inner observer B. This confirms reporting and state sharing, not a wrapper-order or general advice-state-lifetime design. The relevant assumption is that the inner algorithm binds its dependencies in the inner scope; eager resolution alone does not avoid case 5 if that algorithm execution is already cached in the parent.

| Proposed order for child calls | Effect | Cost or limitation |
| --- | --- | --- |
| `B(A(M_0))` | Add the child wrapper outside the existing parent chain. Entry work runs B then A; successful exit callbacks run A then B. | Reuses the parent's wrapper objects, but reverses current scope-depth precedence. If B is a configuration-origin duration budget and A a module observer, this also times A's callback, violating current isolation. |
| `A_child(B(M_0))` alongside parent `A_parent(M_0)` | Preserve current origin/depth/declaration order while sharing the underlying instance. For A and B of module origin, entry work runs A then B; successful exit callbacks run B then A. | A needs another wrapper binding around a different child, sharing the intended observer state. Arbitrary stateful wrapper objects cannot simply be reconstructed without deciding state ownership. |

**Recommendation, not decision:** preserve the existing ordering and bind thin wrappers around the shared underlying instance. The current observation adapter already captures a callback, so separate wrapper objects can report to the same analyzer without invoking it twice for one call. This is not yet a general solution for arbitrary stateful advice/wrappers, whose retained state may need an explicit binding contract. Reordering must not depend on whether an ancestor happened to resolve first. A single mutable wrapper must not be rewired while parent callers still hold it.

**Observer ownership proposal:** keep one attached analyzer's result sink shared across the selected instances it observes. A new child scope or wrapper binding does not implicitly clone/reset that analyzer. Per-instance/per-cycle results require an explicit state factory or grouping identity; current mutator observations identify the source configuration, which alone cannot distinguish two instances of the same configuration in sibling scopes. `AttachPerTrial` remains the existing way to choose per-trial attachments. More general advice-state lifetime remains open.

#### Case 5: a shared composite retains its dependency bindings

**Decision on 2026-09-27: choose option C with typed execution factories.** This supersedes the 2026-09-26 option-A decision to retain fixed composite bindings and reject incompatible reuse. A child scope must be able to obtain a new typed binding of an ancestor-provided logical G, reaching the child's observations on M while preserving the intended G and M state. Existing parent/sibling bindings remain unchanged.

For example, P registers outer observer A and creates G with M. C adds inner observer B and requests G. Current code reuses the old G object and bypasses B. The selected target is a C-specific G binding whose typed M calls reach both A and B without resetting either source's logical state. Three parent calls and two child calls advance shared M state five times and report five times to A, twice to B. Rejecting this supported case is no longer the planned final behavior.

An outer wrapper around today's G cannot redirect its stored child reference. Mutating that reference affects other callers; rerunning today's factory may reset state. Typed preparation and repeatable binding provide the selected cooperation boundary. Execution nodes still contain meaningful typed children and immutable context; ordinary role calls do not acquire a resolver parameter.

| Direction and disposition | Benefit | Required tradeoff |
| --- | --- | --- |
| A: retain existing bindings and reject incompatible reuse; superseded | Preserves the concrete G object and its existing references. | Gives up child-specific observation through that composite and requires difficult detection for opaque/deferred children. Do not implement this as an intermediate prerequisite. |
| B: common-ancestor module declarations only; not selected | Uniform inherited module declarations. | Removes useful child-local observation and does not by itself address configuration-origin budgets. |
| C: shared logical state with typed factories and scope-specific bindings; selected | Rebind typed child calls while preserving state and existing callers' behavior. | Requires reviewed preparation/binding contracts, dependency identity, deferred scopes, iterator/advice ownership and measured costs. Exact API shapes and implementation are not yet approved. |

The [factory design plan](execution-bindings-and-shared-state.md) owns the next design and proof packages. The [design comparison](execution-bindings-design-investigation.md) records the evaluated candidates and current sketches. Ordinary users and leaf authors should remain insulated; non-terminal operators and algorithms need concrete before/after examples before migration is planned.

**Requirements retained from the earlier review:** no silent observer omission; independent siblings without ancestor state; ancestor-state reuse; child-first ownership; no hoisting; source attribution and generated-wrapper exclusion; outer observations of deferred children; valid empty selections; clock/trace order and duration-budget isolation. Child-scope binding is not general caller-path selection.

**Open design cases:** preserving G's dependency when C already owns a different logical M; stable derived configurations and declaration identities; deferred Cycle/Pipeline child ownership versus observer context; richer execution capabilities; initialization and binding failures; paused enumerations; resource ownership; and binding/cache costs. Preserving an active iterator's original binding is the current recommendation, not a promise to migrate suspended execution to new observers.

**Superseded option-A work:** the earlier proposal to record dependencies primarily to detect missing observations and reject composite reuse is no longer the next package. Its finding that `underConstruction` is not a persistent dependency graph remains relevant. Option C may need dependency records to preserve identities, but those serve rebinding rather than retaining option A's limitation. Any rejection policy for temporarily unsupported factories must be explicitly designed during migration and must not quietly become the final solution for supported composites.

Ambient scope switching, runtime service lookup during role calls, dynamic proxies, reflection-based rewriting and globally installing child-only observers remain excluded. Option B's rationale remains in the [developer backlog](developer-backlog.md#prohibiting-all-child-scope-module-installation).

**Next within the current rework:** review the concrete factory design and focused proofs, then write the bounded migration plan before package 4c integration. The [old second-opinion prompt](container-and-aspect-framing-scope-review-prompt.md) is historical context for the superseded option-A decision, not the current task specification.

### D3: Nesting and instance sharing

**Settled after scope review and the option-C decision:** logical state sharing must not depend on decorations; preserve ancestor reuse, child-first local ownership and no hoisting. This supersedes the original matching-decoration reuse barrier. Case 5 now requires scope-specific rebinding through shared composites; its detailed ownership rules belong to the factory design. General nesting remains deferred. **Open for later general nesting:** an immediate caller edge, a structural ancestor path, a resolution scope or dynamic call flow? These differ in shared graphs and delayed construction. `Resolve` currently receives a configuration and creation delegate, not an explicit caller edge/path. A transient construction stack cannot identify every caller of a reused instance.

Acceptance case: a genetic algorithm and a hill climber share one stateful mutator configuration and instance. Advice selecting only hill-climber calls must leave the shared mutation counter and RNG behavior unchanged and must not observe genetic-algorithm calls. Decorating the shared instance observes both; creating another raw instance splits state.

**Proposal for later review:** compare explicit caller-edge wrappers around a shared target with a deliberately narrower scope-selection feature. Scope selection must not be called general nesting. Caller-edge wrappers still need explicit path propagation for delayed children, ownership, wrapper reuse and performance evidence; they are not an approved implementation.

Do not change caching or promise zero call-path cost to make nesting appear solved. Preserve no-hoisting and ancestor reuse unless a separate change is explicitly accepted.

### D4: Advice precedence

**Initial baseline:** current origin, depth and declaration ordering. Scope review reopened how this order extends to different wrapper bindings of the same underlying instance; retaining it is the recommendation in case 4, not a settled binding design. Same-scope declaration ordering remains unchanged. **Also open:** how advice kinds within one module compose, and whether a later explicit-precedence facility has a concrete need. Specify observable entry/exit order, not just wrapper order.

**Proposal:** use nested-wrapper entry/exit order without a new priority property. Before implementation, define advice-failure behavior, whether around advice can proceed zero/multiple times, and what finally/throwing advice sees under suppression, replacement or retry. Acceptance tests must preserve duration-budget isolation and clock-before-trace reads.

### D5: Advice ownership and algorithm boundaries

**Open:** distinguish reusable descriptions, per-target advice state and caller-owned modules intentionally shared across targets/runs. Reusing a pointcut must not implicitly choose advice-state reuse. Retain analyzer/result ownership; do not resolve trace retention or aggregation through a run.

For algorithms, distinguish stream creation, enumeration start, each yield, normal completion, exception/cancellation and early disposal. Decide the boundary of each advice API before adding async wrappers. A run may pause while retaining its iterator; iterator disposal is not synonymous with pausing.

**Proposal:** first design synchronous advice for one operator role with explicit state ownership, then algorithm iteration/stream advice separately. A common callback context across roles is optional and needs a concrete counting/timing use case. The [typed invocation rejection](developer-backlog.md#typed-operator-invocation) permits internal callback type erasure, not replacement of role methods with generic invocation.

## Review packages

Each row is a review stop. Implement at most one package per review cycle. At the start of a package, reassess the current code, present the choices that block that package, and resolve them before dependent implementation. Leave later choices open. Later rows are a proposed sequence, not authorization to settle open decisions silently; their AOP labels are working terms.

| Package | Reviewable result | Dependencies | Validation |
| --- | --- | --- | --- |
| 1. Current model and plan | Correct the plan, add DI/AOP orientation, correct architecture/glossary sharing and builder wording, and update the stale instrumentation backlog item. No C# or public API changes. | Unblocked; first package. | Existing focused resolution/observation tests, API usage specs, docs build, diff/link checks. |
| 2. Resolution characterization | Cover siblings reusing a pre-resolved ancestor, child-first resolution and decorations on unrelated targets. Correct matching XML/test descriptions; preserve runtime behavior. | Review package 1; independent of naming. | Focused resolution tests and release build for XML contracts. |
| 3. State transformation design | Review whether to retain the role or expose an internal algorithm advice boundary. Retain the name `Interceptor`; if the role is replaced, removal follows equivalent algorithm advice. | D1 and the relevant algorithm part of D5; may follow package 7's design before its implementation. Does not block non-algorithm pointcuts. | Compare current termination, feedback, RNG and shared-state examples. Any later role replacement needs release build, core, API usage and experimental tests, then one full solution run. |
| 4a. Standalone typed node selector | Reference and assignable-type matching, including role interfaces, plus typed conditions (initially `Where`, renamed to `And` in 4b). Query supplied configurations without resolver integration. Include a consumer-authored naming-wrapper usage example. | Agreed reference/role/type selection and typed filters; new API shape reviewed in this package. | Focused matcher tests, core and API usage specs, Release build and scoped formatting/analyzers. Include distinct equal configurations, derived types, consumer interfaces, filter short-circuiting and unchanged source selectors. |
| 4b. Selector composition | Union/intersection through `Or`/`And` and `|`/`&`, with `And(predicate)` delegating through a predicate selector. Retain one shared declared configuration type or role. | Accepted D2 composition rules. Additional role-adaptation conveniences remain for review with real registration usage. | Focused matcher tests, core and API usage specs, Release build and scoped formatting/analyzers. Cover overlap, precedence/grouping, short-circuiting, predicate failures and unchanged operands. |
| 4c.1. Shared state and binding integration | Integrate the reviewed option-C factory design, proving direct decoration and rebinding through a shared composite before adding selectors. Its implementation scope and subdivisions come from the factory migration plan; there is no preceding option-A guard package. | [Factory design/proof/migration plan](execution-bindings-and-shared-state.md), including case 4/5 ownership, ordering, capabilities and deferred construction. No implementation yet. | Focused sharing/ordering/failure tests, Release build, core and API usage specs. Cover child-first collisions, sibling independence, repeated resolution, observer accumulation, no double wrapping, budgets and deferred-child bindings. |
| 4c.2. Selector registration and resolution | One mutator integration and a consumer-defined-role usage spec. Select multiple configurations through the reviewed factory/binding mechanism. | Package 4c.1 reviewed; D2 integration choices and match-cache ownership. Option C is the foundation; general caller-path nesting stays separate. | Focused matcher/resolution tests, core and API usage specs. Include overlapping selections, ancestor state, reused composites, late children, generated-wrapper exclusion and valid empty selections. |
| 5. First typed advice | Agreed synchronous advice kinds for one role, with traces for success, failure, advice failure and around short-circuit/retry. | D4 and synchronous D5. | Focused advice tests, core, API usage specs and a local lightweight-operation overhead comparison. |
| 6. Role expansion | One further role per package, retaining consumer role extensibility. Migrate its observation adapter where appropriate. | Packages 4-5 reviewed. | Focused role tests, core and API usage specs; experimental tests for affected consumers. |
| 7. Algorithm advice | Agreed iteration/stream boundaries and algorithm observation migration. Replace the dynamic-problem exception in a separately reviewed follow-up. | Algorithm D5 and D4. | Lifecycle/observation tests, core, API usage and experimental tests; selected workflow scenarios. |
| 8. Rich matching | One package per accepted attribute, name or wildcard feature. Nesting has its own design review and implementation package. | Corresponding D2 choices; D3 for nesting. | Focused matching/sharing tests, core and API usage specs; performance evidence if the call path changes. |
| 9. Instrumentation and budgets | Prove one role/concern migration, retaining success-only counts, failure-inclusive timing, sink ownership and configuration origin. Expand after review. | Advice supports those semantics. Do not assume all wrappers/factories disappear. | Instrumentation/budget tests, core, API usage specs; selected racing/cycle scenarios at completion. |
| 10. Docs and guardrail | Publish aspect authoring docs with supported APIs. Separately audit/extend `HLib0001` for delayed creation if legitimate base/bridge calls can be distinguished. | Implemented APIs; diagnostic design. | Docs build/API usage specs; focused Roslyn analyzer/code-fix tests for diagnostic changes. |
| 11. Revisit the resolver design | Reassess `ResolutionScope`, its internal components and the public wrapper/advice registration surface using the completed AOP implementation and actual authoring examples. The user remains dissatisfied with the current design; accepting the factory/resolution checkpoint does not settle its final structure. Look for simpler component boundaries and terminology while preserving shared state, scoped behavior and typed role operations. | AOP rework completed. This is a required follow-up, not a reason to expand the current factory package. | Present the reassessment and proposed simplifications for review before another implementation package. |

For C# packages use `dotnet restore`, `dotnet build --configuration Release --no-restore` and the selected `dotnet test --configuration Release --no-restore` scope. Run repository whitespace, style and analyzer verification for changed C# code. Follow [AGENTS.md](../AGENTS.md) and [test/README.md](../test/README.md); do not repeatedly run scenarios. Substantial public API or shared-invariant integration needs one complete solution test run at completion.

## Package 1 review record

- Changed: current-behavior corrections and DI/AOP orientation, plan dependencies/review stops, and the instrumentation backlog's retired layering reference.
- Preserved: runtime code, public names, attachment methods, state sharing, ordering and lifecycle ownership.
- Validation: `dotnet restore` passed. Release focused resolution/observation tests passed (29), iteration-boundary tests passed (8), and all API usage specs passed (185). `npm run docs:build` and `git diff --check` passed. Test builds emitted existing analyzer warnings in unchanged C# files. No runtime change required a full core/scenario run or C# formatting pass.
- Settled in review: typed reference/role/type matching first; preserve current sharing and precedence; defer nesting; retain `Interceptor` as the operator name; settle remaining choices when their package needs them and choose public terminology for HeuristicLib users rather than AOP conformity.
- Remaining: state-transformation role versus algorithm advice, new AOP API names, detailed matching semantics, per-kind precedence/exception behavior and advice ownership/algorithm boundaries. D1-D5 distinguish these from the accepted initial scope.

## Package 2 review record

- Removed the obsolete layering analysis report after confirming its active decisions and follow-ups are already in the architecture docs, backlog and this plan. `Test-Path` returns `False`; no references to the deleted report remain in plans, docs or `AGENTS.md`.
- Added five resolution test cases: decorated and undecorated ancestor reuse across sibling scopes, decorated sibling isolation when no ancestor has resolved the target, undecorated child-first resolution, and reuse when the child decorates another configuration. Existing decorated child-first coverage now also checks that both scopes retain their own cached instance.
- Corrected the overly broad sibling-sharing test name and removed misleading test descriptions. Updated resolution XML to describe per-configuration lookup barriers, local caches, ancestor reuse, builder snapshots, declaration order and module origin.
- Runtime implementation and public signatures are unchanged. Packages 1 and 2 were approved for commit on 2026-09-26 before continuing with graph-selection naming and semantics.
- Validation passed: `dotnet restore`; `dotnet build --configuration Release --no-restore --no-incremental` (69 existing warnings, zero errors); a subsequent core-project Release build after the final XML wording correction; and `dotnet test --project test/HeuristicLib.Tests/HeuristicLib.Tests.csproj --configuration Release --no-restore --filter-class '*ResolutionScopeTests'` (26 passed).
- Whitespace, style at warning severity and analyzer verification at error severity passed with `--include` restricted to the two changed C# files. The format tools reported workspace-loading warnings. `git diff --check` passed. Broader test suites were not repeated because the package changes tests and documentation only.
- Review stop: no graph-selection or advice API has been introduced. Discuss the relevant selection semantics and public names against the current code before implementing that package; algorithm-boundary decisions remain separate.

## Package 4a review record

- Added `NodeSelector<TConfiguration>` with reference identity, assignable-type/role matching, typed property filters and an explicit single-node matching query. The implementation is independent of resolution, registration, invocation, wrapper precedence and execution-state ownership.
- Added six focused tests for equal-but-distinct references, derived classes, consumer-defined interfaces, reference-preserving filters, short-circuiting and immutable filter composition. Four API usage specs show role/type inference, configuration properties, a private named-wrapper example and preservation of its shared stateful child instance in one scope.
- Split the former selection package into standalone matching, composition and resolver integration so each can be reviewed against actual code. Names remain a proposed property-filter pattern; no public naming wrapper, common node-interface rename, untyped advice or graph traversal was added.
- Validation passed: `dotnet restore`; full Release build with `--no-restore` (69 existing warnings, zero errors); focused selector tests (6); complete core tests (2,283); and API usage specs (189). Tests used the Release build with `--no-restore --no-build`.
- Scoped whitespace, style at warning severity and analyzer verification at error severity passed for the three added C# files. The format tools reported workspace-loading warnings. `npm run docs:build` and `git diff --check` passed. No scenarios or full solution test run were needed for this independent selection primitive.
- Reviewed together with package 4b before adding registration. Resolver match timing, advice inference and the public naming contract remain separate decisions. Subsequent discussion settled that decoration-produced wrappers are excluded from selector inputs; enforce and test this when integrating with resolution.

## Package 4b review record

- Added immutable union/intersection through `Or`/`And`, with `|`/`&` delegating to the named methods. Predicates evaluate left to right during matching and short-circuit when the result is known. Composition itself evaluates no predicates. Both operands retain one declared configuration type or role.
- Renamed selector `Where` to `And`, migrating its unit tests and usage specs without a compatibility alias. The predicate overload constructs `NodeSelector<TConfiguration>` through its existing public constructor and calls selector intersection; no additional predicate-selector class or separate filtering path was added. Ordinary LINQ `Where` calls remain unchanged.
- Added twelve focused cases for union/intersection membership, operator/method equivalence, overlapping selections without merging equal-but-distinct nodes, lazy predicate evaluation, short-circuiting, nominal role checks, predicate failures and grouping. Added two usage specs for a shared mutator role and reusable named-property predicates.
- Validation passed: `dotnet restore`; full Release build with `--no-restore` (65 existing warnings, zero errors); focused selector tests (18); complete core tests (2,295); and API usage specs (191). Tests used the Release build with `--no-restore --no-build`. No diagnostics referred to the changed C# files.
- Scoped whitespace, style at warning severity and analyzer verification at error severity passed for the three changed C# files, with the existing workspace-loading warnings. `npm run docs:build` and `git diff --check` passed.
- Review correction: removed all six runtime null guards from `NodeSelector`, following the existing developer-guidelines § 6.1 contract for nonnullable references. Release rebuild passed (28 existing warnings, zero errors), all 18 focused selector tests passed, and scoped whitespace/style/analyzer verification passed with workspace-loading warnings. No null-bypass tests were added; valid-input behavior and public nullable annotations are unchanged.
- Review completed: packages 4a and 4b approved for commit on 2026-09-26. No resolver registration, invocation wrappers, sharing/precedence changes, mixed-role advice, node-contract renames or public naming wrappers. Review registration ergonomics and match evaluation with concrete mutator and consumer-defined-role examples before package 4c.

## Out of scope

- Reopening the layering overhaul, Contracts assembly consolidation or shared run-host rejection.
- A lifetime enum/policy replacing `NewExecutionInstancesPerCycle`.
- Implementing the entire rework in one change, automatic commits or compatibility shims.
