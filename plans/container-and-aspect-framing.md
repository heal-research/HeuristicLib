# Container and aspect framing

Status: packages 1 and 2 completed and approved for commit. The next step is the graph-selection naming and semantics discussion. Implementation choices below remain open. Stop after each package for review; do not commit automatically.

Reviewed against `37e8bdcc` (the layering overhaul merged into `dev`) on 2026-09-26. Work continues on `container-and-aspect-framing`. The completed layering implementation plan and its obsolete analysis report have been removed; Git history preserves them. Durable rules live in [layering](../docs/contributing/architecture/layering.md), the [developer guidelines](../docs/contributing/developer-guidelines.md), the [design goals](../docs/contributing/design-goals.md) and the [developer backlog](developer-backlog.md).

## Purpose and settled foundations

Use DI and AOP as comparison models for concrete HeuristicLib authoring problems. Extend selection and attached behavior where they help users work with algorithm and operator graphs. An analyzer is already an execution module that owns results. The new capability is selecting more than one known configuration reference and expressing typed behavior beyond successful-operation observation.

**Settled design approach:** discuss a choice when its package needs it, using the current implementation and representative HeuristicLib usage. Do not copy AspectJ semantics or AOP vocabulary merely for conformity. Names must make sense to users who know algorithms and operators but do not know AOP. `Pointcut`, `JoinPoint`, `Advice` and `Aspect` are working comparison terms, not accepted public type names. `NodeSelector` is one candidate for graph selection, not a settled replacement for `Pointcut`. Choose names alongside the responsibility they express, including whether selection addresses configuration objects, graph occurrences, execution instances or operation boundaries. This does not reopen settled foundations.

Preserve these settled foundations:

- Configuration-reference resolution, explicit instance creation and declared child resolution. Type matching selects advice targets; it never supplies dependencies by type.
- Typed role contracts and handwritten wrappers. No dynamic proxies, reflection emit, universal role invocation or runtime service locator.
- `IAnalyzer : IExecutionModule` with one installation contract. Keep `Attach`, `AttachPerTrial` and `GetAttached` as the user-facing attachment methods.
- Run-specific lifecycle ownership. `AlgorithmRun` keeps its lifecycle and retained enumerator; `ExperimentRun` owns trial scheduling. No shared `RunHost`.
- Concept namespaces with corresponding source folders and optional grouping folders beneath them. Do not arrange namespaces or assemblies by layer.
- Nesting pointcuts must not silently split or merge mutable execution state. Selecting observations must not change which callers share the underlying state.

A recommendation below is not a settled decision. Current behavior remains the baseline until an explicitly reviewed change replaces it.

Confirmed for the initial implementation on 2026-09-26: start with typed reference, role and type matching; preserve current instance reuse and origin/depth/declaration ordering; defer nesting; retain the operator name `Interceptor`. Whether state transformation should eventually move from that role to typed algorithm advice remains a separate open design question.

## Current implementation and corrections

### Resolution and construction

The implementation is in [ResolutionScope.cs](../src/HeuristicLib/Execution/ResolutionScope.cs); existing behavior tests are in [ResolutionScopeTests.cs](../test/HeuristicLib.Tests/Execution/ResolutionScopeTests.cs).

- `Decorate` registers `Func<TConfiguration, TConfiguration>` against one configuration reference. There is no pointcut type, role/type pattern registry, name metadata or caller-path matcher.
- `Resolve(configuration, create)` searches the resolving scope and its ancestors. At each scope it checks under-construction instances, cached instances, then declarations for the target. A declaration stops the upward search. If nothing is reusable, resolution builds and caches in the resolving scope.
- Identical decoration chains permit ancestor reuse; they do not guarantee sharing. A child that resolves first keeps its instance, and a parent resolving later builds its own. Siblings cannot inspect each other's caches, but both can reuse an instance already held by their common ancestor. The earlier claims that chain identity alone determines identity and that siblings never share were too strong.
- The barrier is per configuration. Decorating one target does not block ancestor reuse of an unrelated target. Reusing a cached parent configuration also reuses its already-resolved children; they are not resolved again under the requesting scope.
- A chain creates the raw instance first, then creates each wrapper through the supplied creation delegate. Temporary under-construction entries prevent wrappers from rebuilding children. Wrapper configurations produced by decorations do not automatically pass through a fresh top-level `Resolve` lookup. A future matcher must decide which objects it sees.
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

`ResolutionScope`, `ResolutionScopeBuilder`, `Decorate`, `IExecutionModule` and `Install` are current API names. `Decoration` is internal; `DecorationOrigin` is public. `ExecutionSignature.Fits` checks execution-type compatibility, not complete graph validity or configuration settings.

`Interceptor` remains canonical for the role transforming a produced search state. No role rename is planned. The proposed AOP vocabulary uses advice, so it does not require reserving `Interceptor` for a second concept.

Pointcuts, advice and aspects are provisional terms used to discuss the extension; neither those public names nor their type shapes are settled. Compare graph-oriented alternatives when each API is designed. "AspectJ-style" is inspiration, not a matching specification. A CLR type attribute cannot hold a different runtime name for each instance of a proposed `Named` wrapper. Also, directly creating a child bypasses decoration of that child, but descendants it resolves through the scope can still receive their own decorations; earlier prose overstated that bypass.

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

### D2: Pointcut matching and typed authoring

**Settled initial scope:** typed reference, role and type matching. Attributes, names, wildcard strings and nesting are deferred. **Still open:** specify these before implementing a matcher:

- The selection concept's public name, using graph-oriented candidates such as `NodeSelector` alongside the working term `Pointcut`. Choose from concrete usage and what the selected object represents; familiarity with AOP is not a prerequisite for users.
- Reference identity, exact versus assignable types, closed/open generic roles, AND/OR/NOT composition and whether overlapping branches apply one registration once or several times.
- Matching original configurations, configured wrappers and/or wrappers produced by decorations. Avoid recursive advice on advice wrappers; preserve the selected original source in observations.
- How typed selection proves advice can wrap the requested configuration/instance role, including a consumer-defined role. Keep stand-in compatibility checks; no universal invocation or built-in-role switch.
- Configurations created during resolution, delayed child construction, ancestor cache hits and child-scope registrations. Construction-time matching does not imply the complete graph exists at run start.
- Attribute inheritance, type-name/namespace/generic wildcard grammar, and runtime names with equality and persistence semantics. A naming wrapper adds a configuration identity and must not accidentally clone its child's state.

**Proposal for the remaining details:** a typed C# API with assignable-type selection against original configurations. Exact matching rules and API signatures still need review. Pattern pointcuts matching nothing are valid and silent, as already recorded in the backlog. Configuration-taking trace shortcuts remain convenient entry points.

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
| 4. First typed pointcut | Agreed selection contract with one mutator integration and a consumer-defined-role usage spec. Select multiple configurations through the same resolution mechanism. | D2; D3 deferred or resolved; matcher-state ownership. | Focused matcher/resolution tests, core and API usage specs. Include distinct equal configurations, ancestor caches, late children, wrapper exclusion and no matches. |
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

## Out of scope

- Reopening the layering overhaul, Contracts assembly consolidation or shared run-host rejection.
- A lifetime enum/policy replacing `NewExecutionInstancesPerCycle`.
- Implementing the entire rework in one change, automatic commits or compatibility shims.
