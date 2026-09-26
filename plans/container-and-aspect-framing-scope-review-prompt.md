# Historical second-opinion prompt: scoped selection, instance sharing and wrappers

Status: historical option-A review snapshot, superseded on 2026-09-27 by the selection of typed-factory option C. The questions below preserve the earlier discussion; they are not the current work specification. Use the [factory design plan](execution-bindings-and-shared-state.md) and the [updated scope decision](container-and-aspect-framing.md#case-5-a-shared-composite-retains-its-dependency-bindings) for active work.

Please review the following design problem for HeuristicLib, a C# library for composing optimization algorithms and operators. I want architectural reasoning, concrete examples and tradeoffs, not an implementation or an assumption that standard DI/AOP terminology dictates the answer. Everything necessary for the discussion is below; repository access is not required.

**Decision at the time, now superseded:** use option A: child scopes may add modules, but those registrations do not replace dependencies or the retained scope inside an ancestor-provided composite. Required behavior was to detect and throw when reuse would bypass a required child observation. The detection mechanism, especially its coverage for deferred children, remained open. Directly wrapping shared M as in case 4 and genuine empty selections were to remain valid. Option B's blanket prohibition on child-local modules was not selected. Option C was then a separate investigation into rebuildable typed execution bindings over shared logical execution state; it is now the chosen design direction.

## Existing model and constraints

- Users compose immutable algorithm/operator **configurations** into a graph. Several parents may reference the same configuration object.
- A `ResolutionScope` resolves configurations by **reference identity**, through explicit `CreateExecutionInstance` factories. There is no type-keyed dependency injection or automatic constructor wiring.
- Execution instances own mutable execution state and concrete typed references to their resolved children. Custom algorithms/operators supplied by library users must participate on the same terms as built-in ones.
- Scopes form a parent/child tree. An instance built in a child stays there: its parent and siblings cannot discover it. A child can reuse an instance its ancestor already holds. If no ancestor holds it, siblings build independent instances. A child that built first keeps its instance if the parent later builds one.
- Configurations may explicitly contain wrapping operators. These are ordinary source configuration nodes with their own resolution/ownership semantics, not disposable instrumentation machinery.
- Attached execution modules, including analyzers, register additional behavior through a builder. The builder's registrations are snapshotted when its scope is created. A live scope cannot be mutated, but it can create a new child scope with additional registrations.
- Behavior uses handwritten wrappers implementing typed roles such as mutator or crossover. Existing operator observations run after a successful operation; broader before/after/around behavior is being designed.
- `NodeSelector<TConfiguration>` selects source configurations by reference, role/interface, assignable type or stable typed predicate, with union/intersection. It does not traverse the graph itself. Matching will happen during resolution, including deferred child resolution. Selection by type is not dependency resolution by type.
- Generated decoration wrappers must never become additional selector inputs. Explicit wrappers in the source configuration graph and their source children are selectable.
- A selector matching nothing is valid and silent: no wrapping or reports. Overlap within one union does not duplicate one registration; separate registrations compose.
- Preserve typed role calls and explicit ownership. Do not propose dynamic proxies, runtime code generation, universal role invocation, a runtime service locator, ambient current-scope switching or reflection-based mutation of arbitrary instances.
- General caller-path/nesting selection is deferred. We are currently deciding what ordinary inherited and child-local registrations mean.

## Existing behavior versus the corrected requirement

Today the resolver caches the final decorated instance. A matching decoration declared in a child stops lookup of ancestor instances and causes a fresh underlying instance to be constructed. We consider this coupling a bug: adding observations/wrappers must not change underlying mutable-state sharing.

**Accepted requirement:** resolve underlying execution state independently of attached decorations. If an ancestor already provides the underlying instance, reuse it even when a child adds matching behavior. Preserve independent sibling instances when no ancestor provides one, and preserve child-first local ownership. Do not hoist child-created instances.

This requirement does not yet settle whether different scopes return different wrapper objects around the same underlying instance, how stateful advice is owned, or whether an existing composite's children can be rebound. Sharing underlying state and returning the exact same outer wrapper object are distinct questions.

## Examples

Use one stateful mutator configuration `M`, parent scope `P`, and child scopes `C1`/`C2`. Observer `A` is registered in P and matches M. Observer B, when present, is registered only in C1. A and B have module origin unless stated otherwise.

1. **No ancestor instance:** only C1 and C2 resolve M. Each constructs its own mutator state: `A(M1)` and `A(M2)`. This independence is required. Both may report to the same analyzer A; observer aggregation must not accidentally merge mutator state.
2. **Ancestor instance exists:** P first resolves M as `A(M0)`. Both children resolve M without additional matching behavior. They reuse the parent's instance and existing wrapping; A must not be applied twice.
3. **Nonmatching child registration:** C1 adds a selector that rejects M. Resolving M still follows case 2. A registration that selects no nodes is an ordinary empty observation, not an error.
4. **Matching child registration:** P already holds `A(M0)`; C1 adds B matching M, then directly resolves M. Today it gets a new M1. The required correction is to keep M0's state shared. Parent/sibling calls must remain unaffected by child-only B. What wrapper order and ownership should apply?
5. **Cached composite:** P first resolves algorithm G0, which stores a concrete typed reference to `A(M0)`. C1 then registers B matching only M and resolves G. Reusing G0 preserves G's state, but G0 still invokes its stored `A(M0)` and never resolves M again. Today B is bypassed on this path. Rebuilding G resets/duplicates its state; mutating its child reference affects every caller. Wrapping G's outer call alone does not intercept its internal M calls. What should be legal or expected here?

In case 5, declaring C1's B earlier in time does not by itself help: G constructed in P still binds P's dependencies. These are separate scope declarations, not changes to an already-built scope.

The confirmed acceptance check for case 4 is three successful parent calls and two successful child calls: M0's mutation count must be five, analyzer A must receive five reports, and child-only B must receive two. Assume the inner algorithm is newly constructed and eagerly binds M through C1, rather than reusing an algorithm previously bound in P. Creating another A wrapper binding must neither reset A nor report a child call twice. Wrapper order remains under discussion.

## Deferred resolution

`CycleAlgorithm` stores the scope that created its instance. During execution, it resolves each configured algorithm in a child scope. Its default mode creates a fresh child scope per algorithm visit; another mode caches the first resolved instance per algorithm reference.

Fresh child scopes yield independent instances when no ancestor holds one. They may reuse an ancestor-provided instance; this is accepted. Parent registrations must still apply to source nodes resolved later. `PipelineAlgorithm` also retains its creating scope for delayed child resolution.

Reusing a Cycle/Pipeline instance from C1 does not currently change the retained scope from which its future child scopes are created. The graph is therefore not necessarily fully constructed when the first algorithm instance is cached. There is no universal child-enumeration contract.

The proposed shared-graph boundary must not be interpreted as hiding delayed descendants from parent observers. A declared in P still observes M resolved later through a descendant scope. The limitation concerns an additional child-only B when that child reuses G already bound in P; G keeps its original dependencies or its retained P scope. Eager versus deferred construction is not the dividing line.

Option A was accepted when this prompt was written. Option B would prohibit child-local module registrations and require a common ancestor's declarations to be inherited everywhere. B still permits child scopes and lazy resolution, but gives up general inner-only attachment from case 4; configuration-origin budgets also need separate treatment. That restriction was not selected.

For option C, consider reconstructing typed execution objects that bind different children/observers but share the original logical state of G and M. These objects can retain meaningful configuration context and child bindings; they need not be allocated on every call. Ordinary stateful operator bases already expose a `TState` and a typed operation using it, while advanced authors currently keep both mutable fields and typed children on their execution instances. Show whether C can fit without burdening authors with additional classes, indirections, manual state plumbing or a fragile rule that all instance fields must move into a state object. Sharing ordinary fields does not automatically preserve captured delegates, child caches or suspended async iterators. No performance cost or benefit has been measured.

## Ordering and observer ownership

Current order, **innermost first**, is:

1. Configuration-origin wrappers before module-origin wrappers.
2. Within the same origin, deeper scope before shallower scope.
3. Within the same origin and scope, earlier declaration before later declaration.

For A in P and B in C1, that gives `A(B(M))`: entry runs A then B, and successful exit callbacks run B then A. At one scope, declaring A then B instead gives `B(A(M))`.

Two case-4 possibilities illustrate the tradeoff:

- `B(A(M0))` reuses the existing parent chain and adds a local outer wrapper. It changes the current cross-scope order.
- Parent uses `A_parent(M0)` and child uses `A_child(B(M0))`. This can retain the current order while sharing M0, but requires a separate A wrapper binding. Should its observer/advice state be shared with A_parent, and how can this be explicit for arbitrary stateful wrappers?

An analyzer already owns its result accumulator, and existing observation wrappers capture its callback. Several thin wrapper objects can report to the same analyzer. General advice might also have per-instance state. These are not automatically the same lifetime. Two sibling instances of one M configuration cannot be distinguished solely by M's configuration reference if per-instance results are desired.

There is a concrete ordering constraint: operator duration budgets are configuration-origin wrappers, and analyzer callbacks are module-origin wrappers. The budget must measure the operator without including outer observer callback work. Simply appending a child budget outside an existing parent observer chain violates that property.

Budget algorithms also allocate counters/timers during instance creation and declare wrappers in a child scope before resolving the algorithm they control. Disallowing child-local *modules* alone therefore does not solve all instances of case 5; configuration-origin behavior has the same binding problem.

## Questions to answer

1. What invariants should separate underlying-instance identity, wrapper bindings and observer/advice state? Test them against all five examples and both parent-first and child-first resolution.
2. Which order is easiest to explain for case 4, and why? Give entry and successful-exit order, account for budget isolation, and avoid order depending on whether P happened to resolve first. Explain whether stateful arbitrary wrappers need a new contract.
3. Can case 5 support child-only observation of M while retaining G0 and M0, preserving parent/sibling behavior and satisfying the constraints? Explain the actual routing of G0's internal typed child call. If a solution requires cooperative dependency binding or a changed execution-instance model, state that explicitly rather than treating it as transparent wrapping.
4. Given option A as the decision at the time of this historical review, assess what an option C investigation must prove before adoption. Show author-facing before/after examples and identify costs or restrictions; compare with the capabilities given up by A and B. How do delayed children, custom composite authors, paused async iterators and budget state fit?
5. How can incompatible reuse be detected and rejected before silently bypassing the observer? An instance dictionary identifies constructed objects, but does not say that G holds M or that its M binding lacks B. The existing `underConstruction` map supports wrapper re-entry and is removed after construction; it is not a dependency graph. Assess recording source dependency bindings, including cache hits and transitive dependencies through child scopes. Deferred children may remain unknown: compare complete potential-dependency declarations with conservative rejection on incomplete information, exposing authoring costs and false positives. Distinguish a genuine no-match result from an unexamined cached subtree; never reject direct case-4 wrapping merely because M is cached. Do not assume reflection can enumerate the complete graph.
6. Recommend the smallest coherent model and implementation sequence. Keep accepted requirements, proposals and necessary changes to current behavior separate. Identify any incompatible requirements rather than silently weakening them. Include counterexamples that could invalidate your recommendation.

Please avoid prescribing additional AOP vocabulary unless it clarifies a real distinction. Prioritize predictable state sharing, understandable authoring and a small explicit runtime model over copying another framework.
