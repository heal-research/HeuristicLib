# Operators

Operators are reusable building blocks that algorithms compose.

The design intent is that many algorithm variants can be expressed by **swapping operator implementations** while keeping the surrounding algorithm structure intact.

## Operator taxonomy

The core roles used across algorithms in this repository are:

- **Creator** (`ICreator`): creates initial candidates.
- **Evaluator** (`IEvaluator`): turns candidates into evaluated candidates.
- **Selector** (`ISelector`): selects evaluated candidates, usually as parents.
- **Crossover** (`ICrossover`): combines parent candidates into offspring candidates.
- **Mutator** (`IMutator`): perturbs candidates to create variation.
- **Replacer** (`IReplacer`): decides how to form the next population.
- **Terminator** (`ITerminator`): observes produced search states and decides whether the owning lifecycle should stop.
- **Interceptor** (`IInterceptor`): transforms the produced search state.

The genetic algorithm (`GeneticAlgorithm<...>`) is the easiest place to see all of these roles working together.

## The “shape” of an operator

Operators are intentionally uniform:

- `IRandomNumberGenerator` is always explicit.
- `searchSpace` is passed in so operators can validate or generate within constraints.
- `problem` is passed in so operators can access the objective or problem-specific logic.

This consistency reduces cognitive load: once you’ve implemented one operator, the next one feels familiar.

> [!IMPORTANT]
> Search-space dependent operators are responsible for adhering to the provided search space.
>
> - Operators assume their input candidates are already within the given search space. Passing out-of-space inputs is considered a usage error and may throw.
> - Operators guarantee that any candidates they return are within the given search space.

## Choosing an operator authoring path

Operator authoring is based on who owns execution data and execution graph dependencies. Choose the narrowest path that fits the operator.

1. Use the role specific stateless base, such as `StatelessCreator`, `StatelessEvaluator`, `StatelessSelector`, `StatelessCrossover`, `StatelessMutator`, `StatelessReplacer`, `StatelessInterceptor` or `StatelessTerminator`, when operation logic needs configuration data but no mutable execution data.
2. Use the role specific stateful base, such as `StatefulCreator<..., TState>`, `StatefulEvaluator<..., TState>`, `StatefulSelector<..., TState>`, `StatefulCrossover<..., TState>`, `StatefulMutator<..., TState>`, `StatefulReplacer<..., TState>`, `StatefulInterceptor<..., TState>` or `StatefulTerminator<..., TState>`, when operation logic needs execution instance scoped data but no execution graph dependencies.
3. Derive directly from the unprefixed role base, such as `Creator`, `Evaluator`, `Selector`, `Crossover`, `Mutator`, `Replacer`, `Interceptor` or `Terminator`, and author the matching execution instance when the operator needs child execution instances or custom execution structure.
4. Implement the role configuration and execution instance interfaces directly when the role bases do not fit.

The unprefixed role base is the common base for all three paths. Stateless and stateful bases derive from it. Authors normally derive directly from the unprefixed base only for the full control path.

### Stateless operators

A stateless operator configuration also performs the operation. Configuration values and referenced collections must remain unchanged during execution. Specialized role helpers may build on this path for common operation shapes.

### Stateful operators

The framework creates one `TState` for each execution instance and passes it to operation logic. `CreateInitialState()` must return a fresh state object each time it is called. State may contain ordinary mutable data and helper data structures. It must not contain operator or algorithm configurations, execution instances, resolvers, registries or delegates bound to child execution instances.

Resolving the same operator configuration more than once in one registry returns the same execution instance and therefore the same state. Resolving it through independent registries creates independent execution instances and state objects. Stateful operators must not assume that operation calls are serialized or that their state is safe for concurrent access unless the owning execution path provides that guarantee.

Framework managed state has no disposal lifecycle. State that owns disposable resources or requires explicit cleanup belongs in an explicitly authored execution instance after execution instance lifecycle support has been defined.

### Explicit execution instances

The configuration describes reusable parameters and graph structure. The authored execution instance owns operation logic, mutable execution data and resolved child execution instances.

Wrapping and multi bases are topology specific shortcuts within this path. A wrapping base resolves one child once. A multi base resolves several children once. They do not have separate stateless and stateful variants because their purpose is already execution graph coordination. The unprefixed role base remains available when those shortcuts do not fit.

### Analyzer guardrails

The `OperatorAuthoringAnalyzer` applies across operator roles. `HLib0002` reports execution graph dependencies exposed through stateful operator state. `HLib0003` reports direct mutation of operator configuration members during operation logic.

These diagnostics are guardrails for common authoring mistakes, not a proof that configuration and state obey every invariant. In particular, the analyzer cannot reliably identify every indirect mutation through a referenced collection, helper object or delegate. Operator authors remain responsible for keeping configurations reusable and keeping execution graph dependencies out of framework managed state.

## Terminator ownership

`ITerminator` is a state-based stopping role: it receives a produced public search state and returns whether execution should stop after that state has been observed.

The owner of the terminator determines what that stop means:

- If an algorithm exposes a `Terminator` property, the terminator is part of that algorithm's internal completion semantics.
- If a wrapper such as `WithMaxIterations(...)` or `StateTerminatedAlgorithm` owns the terminator, the terminator is external early stopping over the yielded stream.

In both cases, the produced state that satisfies the terminator remains part of the stream. The terminator stops future production or consumption; it does not remove the triggering state.

Supplying an `initialState` to resume an algorithm does not make that state newly produced. Terminators should be invoked only for states produced by the current execution.

`ITerminatorInstance.IsTerminalState(...)` may update run-local state. Treat it as an effectful transition, not as an idempotent predicate for speculative probing. An owning execution instance should call a terminator instance at most once for each produced public state. Sharing the same stateful terminator instance between an algorithm-owned internal criterion and a wrapper-owned external criterion should happen only when shared state is intentional.

## Operator composition

Operators can be combined through weighted alternatives, conditional application, sequential pipelines, transformed composition, logical terminators and specialized wrappers. Composition policies define batching, ordering and any stronger cardinality requirements they need.

See [Operator composition](operator-composition.md) for the available forms, their semantics and authoring guidance.

## Next

- [Algorithm](algorithm.md)
- [Operator composition](operator-composition.md)
- [Execution model](execution-model.md)
