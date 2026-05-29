# Operator Authoring Base Classes

## Motivation
In our current architecture, there is friction in balancing the Definition/Configuration of an operator with its Execution/State.

1. **The "Two-Entity Model"** (Operator config + Execution Instance) perfectly models nested instances and composition but requires too much boilerplate for simple, flat operators.
2. **The "Three-Entity Model"** (Operator config + Operator execution logic + State POCO) simplifies leaf nodes, but pollutes the `State` object for composite operators, forcing developers to awkwardly query `state.ActualChildInstance` continuously during the execution loop.

To adhere to our design goals of static type safety, zero-magic context resolution, and a clear developer experience, we will introduce 3 distinct **authoring paths** (base classes). Developers will choose the authoring path that explicitly matches their operator's structural needs.

## Decision Rationale

This plan records the current target direction, not an already implemented architecture. The important decision is to make the authoring model explicit instead of trying to stretch one base-class pattern over all operators and algorithms.

Alternatives considered:

1. **Keep only the current state-based authoring model.** This is compact for simple operators, but it becomes awkward for composite algorithms because child execution instances have to be threaded through state objects.
2. **Move everything back to explicit execution instances.** This is structurally clean for composite workflows, but it adds ceremony for simple leaf operators that do not need nested runtime objects.
3. **Support distinct authoring paths.** This keeps simple operators simple while preserving the stronger two-entity model for algorithms and composite operators.

The chosen direction is option 3. The main tradeoff is a larger public authoring surface, so the base classes, XML docs, examples, and analyzer rules need to make the boundaries obvious.

## The 3 Authoring Paths

### 1. `StatelessOperator<x>` (The Pure Function)
- **Concept:** The operator acts as both the definition and the execution entity. It holds zero runtime state.
- **Use Case:** Simple mathematical transformations, random number generations, or pure mapping functions.
- **DX:** Write a single class with configuration properties and a single pure `Execute(...)` method.

### 2. `StatefulLeafOperator<x, TState>` (The Data-Stateful Operator)
- **Concept:** The operator class holds the execution logic but receives a `TState` object alongside execution inputs. `TState` is strictly a pure data object (or immutable record).
- **Use Case:** Operators that need historical memory or intermediate metrics (e.g., adaptive mutation trackers) but **do not** depend on other nested tools or operators.
- **DX:** Write the operator class and a simple `TState` POCO/Record.

### 3. `CompositeOperator<x>` (The Structural Operator)
- **Concept:** Embraces the classic Two-Entity Model. The Operator class is exclusively a configuration node and factory. The logic, state, and child execution instances exist entirely inside an accompanied `ExecutionInstance` class.
- **Use Case:** Any operator, algorithm, or workflow that requires executing child operators (e.g., a cross-over operator using a sub-selector, or a high-level algorithm block).
- **DX:** Write two classes (Definition and Instance). The definition instantiates the execution instance, allowing natural object graphs (via private fields) without awkward state POCO pollution.

## Algorithm Authoring
While the above describes the three authoring paths available for Operators, **Algorithms** have a more constrained nature. By definition, an Algorithm in an optimization library acts as a coordinator (e.g., a Genetic Algorithm needs to evaluate, select, crossover, and mutate). Because an Algorithm always relies on a graph of sub-operators, it is inherently composite.

Therefore, Algorithms will exclusively follow the **Composite Authoring Path**.
- We will *not* offer "Stateless" or "Stateful Leaf" authoring paths for Algorithms.
- Architecturally, algorithm authoring is not a new or special mechanism; it is the exact same Two-Entity mechanism (Configuration class + Execution Instance class) used by `CompositeOperator`. The `Algorithm` definition acts as the factory, and the `AlgorithmInstance` holds the runtime loop (`MoveNext`), tracking child execution instances safely as private fields.

## Enforcement & Guardrails
Because the boundaries could be blurred by users trying to inject child execution instances into the `TState` of a `StatefulLeafOperator`, we will strictly enforce these rules:

1. **Strict Naming and XML Docs:** Classes will be explicitly named (e.g., `StatefulLeafOperator` instead of just `StatefulOperator`) with clear XML documentation explaining when to rely on a `CompositeOperator`.
2. **Roslyn Analyzer Enforcement:** We will introduce a custom Roslyn analyzer in the `analyzers/` project to police the hierarchy.
   - **Rule:** If a class inherits from `StatefulLeafOperator<..., TState>`, the analyzer incrementally inspects the `TState` type.
   - **Violation:** If `TState` contains properties or fields assignable to `IOperator`, `IAlgorithm`, or `IExecutionInstance`, the compiler will throw an error/warning (e.g., *"State objects in StatefulLeafOperators must be pure data. Found composite dependency 'X'. Inherit from CompositeOperator instead."*).

## Next Steps (Action Items)
- [ ] Introduce the 3 base classes in `HeuristicLib` core modules.
- [ ] Create the Roslyn analyzer to restrict `TState` topology.
- [ ] Migrate existing operators to inherit from the appropriate new base classes.
- [ ] Update `docs/operators.md` and related documentation to reflect the new authoring guidance.
