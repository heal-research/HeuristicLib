# Configuration vs execution nodes

Resolution works like a DI container keyed by configuration object reference: configurations describe what to create, and a scope creates and reuses execution nodes through explicit factories. Child configurations are resolved explicitly; there is no lookup by service type or constructor auto-wiring.

Wrapper registration supplies an AOP-like part of that model. Execution modules register wrappers for individual configuration references while the scope is being configured, and resolution composes those wrappers as it builds executions. The wrappers implement ordinary typed algorithm or operator contracts.

| Current concept | DI or AOP analogy | Boundary of the analogy |
| --- | --- | --- |
| Configuration object | Registration | Identity is the object reference, even for structurally equal configurations. |
| `ResolutionScopeBuilder` / `ResolutionScope` | Registration / resolution phases | The builder declares wrappers; execution creation remains explicit. |
| Execution module | Module or aspect | `Install` declares behavior; runs accept modules through `Attach`. |
| Wrapper registration | Advice implemented by a wrapper | Handwritten role wrappers; no dynamic proxy or universal invocation. |
| Observation | Successful-operation advice | Operator callbacks follow successful calls; algorithm callbacks follow each yielded state. |

An algorithm run owns one root scope and may create child scopes. Scope reuse follows the lookup rules in [execution resolution](/contributing/architecture/execution-resolution), not a singleton/scoped/transient setting. Execution nodes have no general disposal contract.

For most users and most extension authors, the preferred model is:

1. Stateless operators use role specific stateless bases.
2. Operators needing ordinary execution data use role specific stateful bases with a framework managed state object.
3. Operators needing child execution nodes or custom execution structure use an authored execution node.
4. Algorithms use authored execution nodes because they coordinate operators and own execution lifecycle concerns.

You usually do **not** need to work with execution nodes directly.

## Why execution nodes still exist

HeuristicLib keeps a separation between:

- reusable configurations
- run-bound execution nodes

That separation is still useful internally for:

- run-local mutable state
- sharing persistent state while binding different child and observer contexts for the same selected execution
- giving meta-algorithms control over whether execution state resets or persists

## `ResolutionScope`

`ResolutionScope` builds an execution graph from the configuration graph.

Important properties:

- resolution is by configuration object reference
- the same configuration object resolves to the same execution node within one scope
- different runs can use different scopes and therefore different execution graphs

A configuration prepares an `ExecutionFactory<TExecution>` once per selected execution, without a scope. The returned factory receives the construction frame and resolves children for the requesting observation context. Persistent state stays in the prepared factory; child bindings belong to the returned node. Advanced compositions may retain their frame for deferred construction or create fresh and retained child scopes.

Obtain every child through `Resolve(...)`. Preparing or invoking a child's factory directly bypasses its selected state, binding cache and wrappers. Typed operations still run directly on the returned execution node.

## Wrappers

Advanced execution plumbing can declare that a configuration is wrapped before it is ever resolved. A budget wraps the operator it limits, and an analyzer wraps the operator it observes.

Declaring and resolving are separate types. `ResolutionScopeBuilder` declares wrappers and cannot resolve; the `ResolutionScope` it produces resolves and cannot declare. A scope opens a declaration phase for a child with `CreateChildScope(...)`:

A binding that must retain this child domain uses `GetOrCreateChildScope(key, declare)`, with a stable key allocated during preparation. Its declaration callback receives the original observed source configuration; it never receives a previously generated wrapper. Each generated wrapper resolves that source as its own inner chain.

Wrappers compose rather than replace one another, and a child scope's wrappers apply on top of its ancestors'. Which one ends up innermost, and when two scopes share one execution, follow rules worth understanding before writing meta-algorithms or observation plumbing: see [execution resolution](/contributing/architecture/execution-resolution).

Most users should not declare wrappers directly. They are intended for meta-algorithms, observation installation and other advanced execution infrastructure.

## Eager local resolution

The current intended model is eager, local resolution:

1. When an algorithm creates its execution node, it resolves the operators it will use and passes them to that execution.
2. When a meta algorithm creates its execution node, it resolves its child algorithms the same way.
3. When a wrapping or multi operator binds its execution node, it resolves its declared inner operators for that context.
4. Authored operator executions store resolved children as private execution data.

This gives one-time resolution cost per execution node and avoids per-call dictionary lookups during steady-state execution.

## Framework managed state lifecycle

The stateful leaf bases prepare state and one raw execution once, then retain that execution behind their generated observations. Ordinary leaf overrides do not need to expose this machinery: `CreateInitialState()` and the typed operation remain the authoring hooks. Stateless bases return their configuration through the prepared factory, and single-item bases inherit that behavior while preserving their batching and random forks.

Logical selection determines state sharing independently of wrappers. Parent-first selection lets descendants reuse state. Child-first selection stays local even if the parent later selects that configuration, and siblings cannot read each other's local selections.

A new observation context can bind another node using the same state. It can also bind a composite's pinned children with the new observations. A parent caller retains its original node and observations. An empty child context can reuse an already-completed ancestor binding, while any added declaration causes contextual rebinding.

Fresh child scopes inherit existing ancestor state but select missing state locally. Retained child slots preserve a sharing scope across bindings and apply the requesting context's observations.

Stateful operator calls are not inherently thread safe. An operator may use ordinary mutable state, but concurrent use is valid only when the owning execution path provides suitable synchronization or the state implementation is itself safe for concurrent access.

Execution nodes have no disposal contract. Framework managed state must therefore not own resources that require deterministic cleanup. Such ownership requires an explicitly authored execution node and a defined lifecycle mechanism.

## When you should care

Explicitly authored execution nodes matter when working on:

- algorithms and meta algorithms
- advanced analyzer and observation plumbing
- operators that coordinate child execution nodes or need custom execution structure

Every algorithm uses an authored execution node. Ordinary operators can instead use stateless or framework managed state authoring bases when they do not coordinate execution graph dependencies.

## Related pages

- [Algorithms](/guide/fundamentals/algorithms)
- [Execution resolution](/contributing/architecture/execution-resolution)
- [Operator implementation](/contributing/architecture/operator-implementation)
- [Running algorithms](/guide/execution/running-algorithms)
