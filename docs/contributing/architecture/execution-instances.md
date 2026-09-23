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

## `ResolutionScope`

`ResolutionScope` builds an execution graph from the configuration graph.

Important properties:

- resolution is by configuration object reference
- the same configuration object resolves to the same execution instance within one scope
- different runs can use different scopes and therefore different execution graphs

Explicit operator and algorithm instance creation methods receive the scope. Ordinary creation methods should resolve their declared children eagerly. Meta algorithms, budget wrappers and other execution graph compositions may additionally create child scopes, declare decorations or control execution instance reuse.

Obtain every child, operator or algorithm through `Resolve(...)`. Calling `CreateExecutionInstance(...)` on a child configuration bypasses the decorations that observation depends on, and does so silently: the search states are still correct, but analyzers observing that child, or any operator inside it, record nothing.

## Decorations

Advanced execution plumbing can declare that a configuration is wrapped before it is ever resolved. A budget wraps the operator it limits, and an analyzer wraps the operator it observes.

Declaring and resolving are separate types. `ResolutionScopeBuilder` declares decorations and cannot resolve; the `ResolutionScope` it produces resolves and cannot declare. A scope opens a declaration phase for a child with `CreateChildScope(...)`:

```csharp
var childScope = scope.CreateChildScope(child =>
    child.Decorate(ObservedOperator, current => CountedOperatorFactory(current, counter)));

return new(childScope.Resolve<TCandidate, TRunSearchSpace, TRunProblem, TSearchState>(Algorithm), counter, MaximumCount);
```

Decorations compose rather than replace one another, and a child scope's decorations apply on top of its ancestors'. Which one ends up innermost, and when two scopes share one instance, follow rules worth understanding before writing meta-algorithms or observation plumbing: see [instance resolution](/contributing/architecture/instance-resolution).

Most users should not declare decorations directly. They are intended for meta-algorithms, observation installation and other advanced execution infrastructure.

## Eager local resolution

The current intended model is eager, local resolution:

1. When an algorithm creates its execution instance, it resolves the operators it will use and passes them to that instance.
2. When a meta algorithm creates its execution instance, it resolves its child algorithms the same way.
3. When a wrapping or multi operator creates its execution instance, it resolves its declared inner operators once.
4. Authored operator instances store resolved children as private instance data.

This gives one-time resolution cost per execution instance and avoids per-call dictionary lookups during steady-state execution.

## Framework managed state lifecycle

A role specific stateful operator base creates one state object whenever it creates an execution instance. `CreateInitialState()` must return a fresh object for every invocation.

Scope identity determines state sharing. Resolving the same configuration object repeatedly through one scope returns the same execution instance and state. Independent scopes create independent instances and state objects. A child scope may reuse an instance from its parent, so it also reuses that instance's state.

A child scope inherits its ancestors' decorations and may reuse instances already resolved by its parent — it does so exactly when it adds no decoration of its own, so that the parent's instance is what the child would have built anyway. Sibling scopes never share. Runtime composing meta algorithms resolve each child algorithm through a freshly created child scope rather than through the parent. This gives each requested stage or cycle a new algorithm instance without preventing intentional sharing of operator configurations from the parent execution graph, and because the child scope inherits its parent's decorations, observation keeps working. Resolve through the child scope; do not call `CreateExecutionInstance(...)` on the child configuration.

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
- [Instance resolution](/contributing/architecture/instance-resolution)
- [Operator implementation](/contributing/architecture/operator-implementation)
- [Running algorithms](/guide/execution/running-algorithms)
