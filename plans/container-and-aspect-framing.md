# Container and aspect framing

Status: packages 1 and 2 committed as `0236ab08`. Packages 4a and 4b are reviewed and approved for commit. Next discuss selector registration and resolution for package 4c before implementing it. Implementation choices below remain open. Stop after each package for review; do not commit automatically.

Reviewed against `37e8bdcc` (the layering overhaul merged into `dev`) on 2026-09-26. Work continues on `container-and-aspect-framing`. The completed layering implementation plan and its obsolete analysis report have been removed; Git history preserves them. Durable rules live in [layering](../docs/contributing/architecture/layering.md), the [developer guidelines](../docs/contributing/developer-guidelines.md), the [design goals](../docs/contributing/design-goals.md) and the [developer backlog](developer-backlog.md).

## Purpose and settled foundations

Use DI and AOP as comparison models for concrete HeuristicLib authoring problems. Extend selection and attached behavior where they help users work with algorithm and operator graphs. An analyzer is already an execution module that owns results. The new capability is selecting more than one known configuration reference and expressing typed behavior beyond successful-operation observation.

**Settled design approach:** discuss a choice when its package needs it, using the current implementation and representative HeuristicLib usage. Do not copy AspectJ semantics or AOP vocabulary merely for conformity. Names must make sense to users who know algorithms and operators but do not know AOP. `Pointcut`, `JoinPoint`, `Advice` and `Aspect` are working comparison terms, not accepted public type names. `NodeSelector` is now the preferred name for configuration-node selection; its API shape remains open. Choose names alongside the responsibility they express, including whether selection addresses configuration objects, graph occurrences, execution instances or operation boundaries. This does not reopen settled foundations.

Preserve these settled foundations:

- Configuration-reference resolution, explicit instance creation and declared child resolution. Type matching selects advice targets; it never supplies dependencies by type.
- Typed role contracts and handwritten wrappers. No dynamic proxies, reflection emit, universal role invocation or runtime service locator.
- Node selection addresses source configuration nodes. Wrappers introduced by decoration are excluded from selection, even when they are internally represented by configuration objects.
- `IAnalyzer : IExecutionModule` with one installation contract. Keep `Attach`, `AttachPerTrial` and `GetAttached` as the user-facing attachment methods.
- Run-specific lifecycle ownership. `AlgorithmRun` keeps its lifecycle and retained enumerator; `ExperimentRun` owns trial scheduling. No shared `RunHost`.
- Concept namespaces with corresponding source folders and optional grouping folders beneath them. Do not arrange namespaces or assemblies by layer.
- Nesting pointcuts must not silently split or merge mutable execution state. Selecting observations must not change which callers share the underlying state.

A recommendation below is not a settled decision. Current behavior remains the baseline until an explicitly reviewed change replaces it.

Confirmed for the initial implementation on 2026-09-26: start with typed reference, role and type matching; preserve current instance reuse and origin/depth/declaration ordering; defer nesting; retain the operator name `Interceptor`. Whether state transformation should eventually move from that role to typed algorithm advice remains a separate open design question.

## Current implementation and corrections

### Resolution and construction

The implementation is in [ResolutionScope.cs](../src/HeuristicLib/Execution/ResolutionScope.cs); existing behavior tests are in [ResolutionScopeTests.cs](../test/HeuristicLib.Tests/Execution/ResolutionScopeTests.cs).

- `Decorate` registers `Func<TConfiguration, TConfiguration>` against one configuration reference. `NodeSelector<TConfiguration>` now provides standalone reference/type matching, union/intersection and typed `And(predicate)` conditions, but is not yet accepted by `Decorate` or observation APIs. There is no role/type registration mechanism in the resolver, built-in name metadata or caller-path matcher.
- `Resolve(configuration, create)` searches the resolving scope and its ancestors. At each scope it checks under-construction instances, cached instances, then declarations for the target. A declaration stops the upward search. If nothing is reusable, resolution builds and caches in the resolving scope.
- Identical decoration chains permit ancestor reuse; they do not guarantee sharing. A child that resolves first keeps its instance, and a parent resolving later builds its own. Siblings cannot inspect each other's caches, but both can reuse an instance already held by their common ancestor. The earlier claims that chain identity alone determines identity and that siblings never share were too strong.
- The barrier is per configuration. Decorating one target does not block ancestor reuse of an unrelated target. Reusing a cached parent configuration also reuses its already-resolved children; they are not resolved again under the requesting scope.
- A chain creates the raw instance first, then creates each wrapper through the supplied creation delegate. Temporary under-construction entries prevent wrappers from rebuilding children. Wrapper configurations produced by decorations do not automatically pass through a fresh top-level `Resolve` lookup. The agreed selector integration must preserve their exclusion from selection; that integration is not implemented yet.
- Builder and scope are separate types, and `Build` snapshots declarations. The callback can retain the builder and mutate it later without affecting the existing scope. The escaped-builder test covers this. The earlier claim that such code cannot be written was incorrect.
- A run owns a root scope and may contain child scopes or several execution graphs. Scopes have no execution-instance disposal contract. The DI analogy must not imply conventional lifetime registrations or deterministic disposal.

### Attachments, observation and instrumentation

[AlgorithmRun](../src/HeuristicLib/Execution/Runs/AlgorithmRun.cs) accepts modules while preparing, deduplicates by reference and installs in attachment order at execution start. [ExperimentRun](../src/HeuristicLib/Experiments/ExperimentRun.cs) creates per-trial attachments; attaching the same trial factory twice is rejected. These are completed layering work.

Current observation wrappers cover algorithms, evaluators, crossovers, mutators and interceptors. Operator callbacks run after a successful operation. [Algorithm observation](../src/HeuristicLib/Analysis/Tracing/Observations/AlgorithmObservation.cs) runs for each yielded search state before forwarding it, rather than once after the async stream completes. There is no general before/throwing/finally/around advice API.

Counting increments after successful calls; duration measurement records in `finally`, including failed calls. Their sinks are now `Instrumentation.CountAccumulator` and `DurationAccumulator`. Reusing a wrapper configuration with a caller-owned sink shares that sink even across independent execution instances. Budgets create their instrumentation during instance creation. Advice objects, analyzers, clocks and underlying execution instances need separate ownership discussions.

The dynamic-problem observation dependency remains the exact exception in [layering](../docs/contributing/architecture/layering.md#dependency-rules). Removing it depends on algorithm-boundary advice, not merely pointcuts.

### Precedence

`ResolutionScope.Chain` orders wrappers **innermost to outermost**: configuration origin before module origin, deeper scope before shallower scope, then earlier decoration declaration before later declaration. `Install` stamps module origin and deduplicates module objects per builder. The final key is decoration sequence, including nested installations, not a separate rank per module.

For wrappers A then B at the same origin and depth, the chain is `B(A(target))`. Entry work runs B then A; successful exit callbacks run A then B. "First installed runs first" is therefore incorrect for before/around advice. Traces install clocks first so their callbacks update before the trace reads them.

Configuration-origin duration budgets stay inside module observations so they exclude observer callback work. Around advice that retries, suppresses calls or throws introduces ordering questions the current sort keys do not answer.

### Names and analogies

`ResolutionScope`, `ResolutionScopeBuilder`, `Decorate`, `IExecutionModule`, `Install` and the standalone `NodeSelector` API are current API names. `Decoration` is internal; `DecorationOrigin` is public. `ExecutionSignature.Fits` checks execution-type compatibility, not complete graph validity or configuration settings.

`Interceptor` remains canonical for the role transforming a produced search state. No role rename is planned. The proposed AOP vocabulary uses advice, so it does not require reserving `Interceptor` for a second concept.

`NodeSelector` names the first standalone selection API under review. Pointcut remains an AOP comparison term; advice and aspect names and their type shapes are still provisional. `IConfigurationNode` and `IExecutionNode` are proposed renames of current base contracts, not current API names or an approved rename. "AspectJ-style" is inspiration, not a matching specification. A CLR type attribute cannot hold a different runtime name for each instance of a proposed `Named` wrapper. Also, directly creating a child bypasses decoration of that child, but descendants it resolves through the scope can still receive their own decorations; earlier prose overstated that bypass.

## Decisions before dependent implementation

### D1: State transformation responsibility and naming

**Settled naming decision:** retain `Interceptor`. With advice as the proposed AOP vocabulary, there is no naming collision to resolve. `StateTransformer` describes the operation but does not convey its iteration timing more clearly, so the rename would not solve the stated problem. See the [rejected rename rationale](developer-backlog.md#renaming-interceptor-only-to-make-room-for-aop).

**Open:** whether state transformation needs a separate operator role or should become typed algorithm advice. Also decide whether the execution module contract serves as the public aspect abstraction; avoid an overlapping `IAspect` introduced only for the analogy.

Current [IterativeAlgorithmInstance](../src/HeuristicLib/Algorithms/BaseClasses/IterativeAlgorithm.cs) performs `step -> Transform -> IsTerminalState -> yield`, then retains the transformed state for the next step and completion check. `Transform` receives the previous state, iteration RNG, search space and problem. `RemoveDuplicatesInterceptor` is a concrete population transformation; pipelines and stateful implementations are also supported. [IterativeAlgorithmInstanceTests](../test/HeuristicLib.Tests/Algorithms/IterativeAlgorithmInstanceTests.cs) characterize termination order and the iteration RNG.

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

**Current standalone API:** `NodeSelector<TConfiguration>` retains a configuration type or role. `NodeSelector.Reference(configuration)` uses reference identity and infers the reference's static type; `NodeSelector.OfType<TConfiguration>()` includes derived classes and interface implementations. `And(predicate)` adds a typed condition without mutating the original selector. `Matches` examines one supplied configuration and short-circuits on incompatible types or when a preceding predicate determines the result. Queries do not traverse graphs, resolve instances or cache results; exceptions from evaluated predicates propagate. This does not yet decide when the resolver evaluates or caches matches. API usage specs show references declared as `IMutator<RealVector>` retaining that role through composition. Additional concrete-to-role adaptation and advice inference still need review with resolver integration.

Selecting a configuration does not by itself identify a particular caller edge or invocation.

**Settled composition design:** `.Or(other)` is union and `.And(other)` is intersection. The `|` and `&` operators delegate to those methods. Operands share the same declared configuration type or role. Construction creates a new selector and evaluates no predicates; matching evaluates the left predicate first, then the right only if the result is still undetermined. Original selectors remain unchanged. A selector remains a definition; none of these operations enumerates graph nodes.

**Settled predicate convenience:** `And(predicate)` constructs `new NodeSelector<TConfiguration>(predicate)` and delegates to `And(selector)`. The existing constructor already supplies a predicate selector; there is no additional predicate-selector type or special filtering implementation. This replaces the initial `Where` spelling, avoiding an enumerable-result implication and making intersection the single composition mechanism.

The `|` and `&` operands are both evaluated while composing selectors. Predicate short-circuiting occurs later, when matching a node. C# requires `operator true` and `operator false` as well as `|`/`&` to enable `||`/`&&`; no such truth operators are introduced because a selector has no per-node truth value until a node is supplied. See the [C# conditional logical operator specification](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/language-specification/expressions#12173-user-defined-conditional-logical-operators). Boolean matching short-circuiting and selector-expression construction are distinct.

**Settled overlap rule:** if an exact-reference branch and a broader type/property branch both match the same source, their union still yields one match. One behavior registered against that union must not execute twice per operation merely because two branches matched. Two separately registered behaviors remain two intended contributions. Same-role composition retains typed arguments/results for future advice; mixed-role composition needs its own callback design. An intersection applied after union constrains every branch, while constraining one branch before union leaves the other branch unaffected. Normal C# precedence applies: `a | b & c` means `a | (b & c)`; use parentheses for `(a | b) & c`.

**Still open:** specify these before implementing the dependent composition or resolver integration:

- Additional concrete-to-role ergonomics, open generic roles and any later complement/exclusion operators. A closed role such as `IMutator<TCandidate>` provides information an open role across all candidate types cannot statically provide to one callback.
- Predicate evaluation timing, stability, caching and failure behavior. A proposal is a side-effect-free predicate over configuration data, evaluated during resolution; it must not become a per-call condition or depend on changing run state implicitly.
- How typed selection proves advice can wrap the requested configuration/instance role, including a consumer-defined role. Keep stand-in compatibility checks; no universal invocation or built-in-role switch.
- Configurations created during resolution, delayed child construction, ancestor cache hits and child-scope registrations. Construction-time matching does not imply the complete graph exists at run start.
- Attribute inheritance, type-name/namespace/generic wildcard grammar, and runtime names with equality and persistence semantics. A naming wrapper adds a configuration identity and must not accidentally clone its child's state.

**Proposals for the remaining details, not decisions:**

- Construction-time matching must account for late source children and preserve the existing cache rules; it is not a promise to enumerate a complete graph at run start. The source-selection boundary above is settled; the mechanism enforcing it belongs to resolver integration.
- A mixed-role selection does not provide one role-specific argument/result shape. Compare keeping a group of independently typed registrations with introducing a common callback contract for concerns such as timing. Grouping typed registrations need not erase their types.
- Keep opaque-call advice outside the first selection implementation. A role-independent timing callback could be supported by handwritten role adapters that keep arguments/results in their typed wrappers. A continuation callback would be a separate advice design, not a universal operation on execution nodes. Consumer-defined roles need an explicit adapter path, and callback costs need measurement.

Pattern selectors matching nothing are valid and silent, as already recorded in the backlog. Configuration-taking trace shortcuts remain convenient entry points.

**Named-wrapper proposal:** a role-specific wrapper can hold a child and an immutable `Name` property. Name selection can then be an ordinary typed `And(predicate)` condition over that property; it does not require a CLR attribute, string wildcard grammar or another matcher primitive. The [node selection usage specs](../test/HeuristicLib.Tests.ApiUsageSpecs/Execution/NodeSelectionSpecs.cs) demonstrate this with a private consumer-authored `NamedMutator`, not a new library naming API.

The wrapper is an additional configuration node. A name predicate selects its boundary, while a broad mutator selector can match both the wrapper and its child; future resolver integration must make those separate boundaries visible rather than silently deduplicating them. The example resolves the child through the existing wrapping base and returns its instance, so two names over a shared child in one scope retain that child's instance. It does not assign metadata to the underlying child or change cross-scope cache rules. A universal `NamedOperator<T>` cannot acquire arbitrary nominal operator roles automatically; naming wrappers must preserve the chosen role contract.

**Settled for named wrappers:** named configurations belong to the source configuration graph. A named wrapper is an explicit selectable node, and its child remains a source node; the woven-wrapper exclusion does not hide either one.

Before shipping naming helpers, review duplicate names, comparison rules, wrapper equality/persistence and a possible shared naming interface. Names remain outside the first selector package; the property-filter example demonstrates the proposed direction.

**Graph-node vocabulary, proposed rename:** compare `IConfigurationNode` for `IExecutionConfiguration` (including its generic form) and `IExecutionNode` for `IExecutionInstance`. These names would describe membership in the configuration and execution graphs; configuration and execution instance remain useful terms for their responsibilities. No shared `INode`, child-enumeration contract or generic invocation is implied. The current base contracts expose compatibility/creation and marker responsibilities, not a general graph traversal API.

Resolution is not a one-to-one lifecycle conversion: one configuration can produce several execution instances in different scopes, and stateless operators can return themselves. A rename must preserve those facts and account for role-specific instance contracts and `CreateExecutionInstance` terminology. Discuss and review it separately before changing code.

The earlier `Anchor<TObservation>` experiment merely renamed one configuration reference and was removed. Selecting more than one known reference is what must justify the new abstraction.

### D3: Nesting and instance sharing

**Settled for the initial work:** preserve current instance reuse and defer nesting. **Open for the later nesting design:** an immediate caller edge, a structural ancestor path, a resolution scope or dynamic call flow? These differ in shared graphs and delayed construction. `Resolve` currently receives a configuration and creation delegate, not an explicit caller edge/path. A transient construction stack cannot identify every caller of a reused instance.

Acceptance case: a genetic algorithm and a hill climber share one stateful mutator configuration and instance. Advice selecting only hill-climber calls must leave the shared mutation counter and RNG behavior unchanged and must not observe genetic-algorithm calls. Decorating the shared instance observes both; creating another raw instance splits state.

**Proposal for later review:** compare explicit caller-edge wrappers around a shared target with a deliberately narrower scope-selection feature. Scope selection must not be called general nesting. Caller-edge wrappers still need explicit path propagation for delayed children, ownership, wrapper reuse and performance evidence; they are not an approved implementation.

Do not change caching or promise zero call-path cost to make nesting appear solved. Preserve no-hoisting and ancestor reuse unless a separate change is explicitly accepted.

### D4: Advice precedence

**Settled initial rule:** retain current origin, depth and declaration ordering. **Open:** how advice kinds within one module compose, and whether a later explicit-precedence facility has a concrete need. Specify observable entry/exit order, not just wrapper order.

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
| 4c. Selector registration and resolution | One mutator integration and a consumer-defined-role usage spec. Select multiple configurations through the same resolution mechanism. | D2 integration choices; D3 deferred or resolved; matcher-state ownership; packages 4a-4b reviewed. | Focused matcher/resolution tests, core and API usage specs. Include overlapping selections, ancestor caches, late children, wrapper exclusion and no matches. |
| 5. First typed advice | Agreed synchronous advice kinds for one role, with traces for success, failure, advice failure and around short-circuit/retry. | D4 and synchronous D5. | Focused advice tests, core, API usage specs and a local lightweight-operation overhead comparison. |
| 6. Role expansion | One further role per package, retaining consumer role extensibility. Migrate its observation adapter where appropriate. | Packages 4-5 reviewed. | Focused role tests, core and API usage specs; experimental tests for affected consumers. |
| 7. Algorithm advice | Agreed iteration/stream boundaries and algorithm observation migration. Replace the dynamic-problem exception in a separately reviewed follow-up. | Algorithm D5 and D4. | Lifecycle/observation tests, core, API usage and experimental tests; selected workflow scenarios. |
| 8. Rich matching | One package per accepted attribute, name or wildcard feature. Nesting has its own design review and implementation package. | Corresponding D2 choices; D3 for nesting. | Focused matching/sharing tests, core and API usage specs; performance evidence if the call path changes. |
| 9. Instrumentation and budgets | Prove one role/concern migration, retaining success-only counts, failure-inclusive timing, sink ownership and configuration origin. Expand after review. | Advice supports those semantics. Do not assume all wrappers/factories disappear. | Instrumentation/budget tests, core, API usage specs; selected racing/cycle scenarios at completion. |
| 10. Docs and guardrail | Publish aspect authoring docs with supported APIs. Separately audit/extend `HLib0001` for delayed creation if legitimate base/bridge calls can be distinguished. | Implemented APIs; diagnostic design. | Docs build/API usage specs; focused Roslyn analyzer/code-fix tests for diagnostic changes. |

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
