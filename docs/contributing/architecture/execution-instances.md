# Configuration vs execution instances

This page documents an advanced execution concept.

For most users and most extension authors, the preferred model is:

1. Stateless operators use role specific stateless bases.
2. Operators needing ordinary execution data use role specific stateful bases with a framework managed state object.
3. Operators needing child execution instances or custom execution structure use an authored execution instance.
4. Algorithms use authored execution instances because they coordinate operators and own execution lifecycle concerns.

You usually do **not** need to work with execution instances directly.

## Why execution instances still exist

HeuristicLib keeps a separation between:

- reusable configurations
- run-bound execution instances

That separation is still useful internally for:

- run-local mutable state
- sharing the same execution instance when the same configuration object is reused in one run
- giving meta-algorithms control over whether execution state resets or persists

## `ExecutionInstanceRegistry`

`ExecutionInstanceRegistry` builds an execution graph from the configuration graph.

Important properties:

- resolution is by configuration object reference
- the same configuration object resolves to the same execution instance within one registry
- different runs can use different registries and therefore different execution graphs

Explicit operator and algorithm instance creation methods receive the registry. Ordinary creation methods should resolve their declared children eagerly. Meta algorithms, budget wrappers and other execution graph compositions may additionally create child registries, register replacements or control execution instance reuse.

## Registry replacements

Advanced execution plumbing can register explicit registry entries before resolving a configuration.

`RegisterInstance(...)` stores an already created execution instance for a configuration identity. This is useful when infrastructure code already owns the instance that should be reused by later resolution.

`RegisterReplacement(...)` stores a replacement configuration for a configuration identity. When the original configuration is resolved, the registry creates the execution instance from the replacement configuration and stores it under the original identity.

Replacement configurations may resolve the original configuration while they are being created. This supports wrapper scenarios such as observable operators, counted operators and measured operators. The registry detects that the original is already being replaced and creates the original execution instance directly for the wrapper.

Most users should not call these methods directly. They are intended for meta-algorithms, observation installation and other advanced execution infrastructure.

## Eager local resolution

The current intended model is eager, local resolution:

1. When an algorithm creates its execution instance, it resolves the operators it will use and passes them to that instance.
2. When a wrapping or multi operator creates its execution instance, it resolves its declared inner operators once.
3. Authored operator instances store resolved children as private instance data.

This gives one-time resolution cost per execution instance and avoids per-call dictionary lookups during steady-state execution.

## Framework managed state lifecycle

A role specific stateful operator base creates one state object whenever it creates an execution instance. `CreateInitialState()` must return a fresh object for every invocation.

Registry identity determines state sharing. Resolving the same configuration object repeatedly through one registry returns the same execution instance and state. Independent registries create independent instances and state objects. A child registry may reuse an instance from its parent registry, so it also reuses that instance's state.

A child registry inherits replacement policy and may reuse instances already resolved by its parent. Runtime composing meta algorithms create child algorithm instances directly through child registries. This gives each requested stage or cycle a new algorithm instance without preventing intentional sharing of operator configurations from the parent execution graph.

Stateful operator calls are not inherently thread safe. An operator may use ordinary mutable state, but concurrent use is valid only when the owning execution path provides suitable synchronization or the state implementation is itself safe for concurrent access.

Execution instances currently have no disposal contract. Framework managed state must therefore not own resources that require deterministic cleanup. Such ownership requires an explicitly authored execution instance and a defined lifecycle mechanism.

## When you should care

Explicitly authored execution instances matter when working on:

- algorithms and meta algorithms
- advanced analyzer and observation plumbing
- operators that coordinate child execution instances or need custom execution structure

Every algorithm uses an authored execution instance. Ordinary operators can instead use stateless or framework managed state authoring bases when they do not coordinate execution graph dependencies.

## Related pages

- [Algorithms](/guide/fundamentals/algorithms)
- [Operator implementation](/contributing/architecture/operator-implementation)
- [Running algorithms](/guide/execution/running-algorithms)
