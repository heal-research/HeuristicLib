# Operator Authoring Base Classes

## Motivation

The current architecture has two useful operator authoring models, but their boundaries are not explicit enough.

1. A configuration plus an explicitly authored execution instance models child resolution, private execution data and custom lifecycle behavior cleanly. It also requires more ceremony than a simple operator should need.
2. A configuration containing the operation logic plus a framework managed execution instance and a separate state object keeps simple stateful operators compact. It becomes awkward when child operator instances or other execution graph dependencies are placed in that state object.

The current implementation uses the state object pattern for ordinary operators and algorithms, while wrapping and multi operator bases resolve children and pass delegates back into configuration logic. This makes structural execution dependencies look like ordinary data and spreads execution ownership across configurations, generated instances and state objects.

HeuristicLib should offer distinct authoring paths based on execution ownership. The paths should keep simple operators simple while making execution graph structure explicit. They are authoring choices, not a taxonomy of leaf and nonleaf graph nodes.

## Decision Rationale

Alternatives considered:

1. Keep only the current state based authoring model. This is compact for simple operators, but child execution instances must be stored in state objects or converted into delegates that are passed back to configuration logic.
2. Require every operator author to implement an explicit execution instance. This gives clear ownership and natural object structure, but adds unnecessary ceremony for stateless operators and operators that only need ordinary execution data.
3. Support distinct authoring paths based on execution ownership. This preserves compact authoring for simple operators and uses explicit execution instances when an author needs execution graph dependencies or lifecycle control.

The chosen direction is option 3.

The public surface must remain understandable despite the additional authoring choices. Role specific names, XML documentation, executable API usage specs and analyzer diagnostics must make the boundaries clear.

## Design Principles

1. Operator configurations are reusable. Mutable data that changes during execution must not live on a reusable configuration.
2. Randomness, the search space and the problem remain explicit operation inputs.
3. Execution data belongs to an execution instance, either directly or through a framework managed state object.
4. Child configurations, child execution instances and resolution facilities are execution graph concerns. They require the explicit execution instance path.
5. A state object may contain rich data structures. The restriction is about execution graph dependencies, not about reference types, value types or framework types.
6. The existing role contracts remain the semantic foundation. Authoring bases are convenience layers over those contracts.
7. No universal marker base will be introduced merely to label an authoring path.

## The Three Operator Authoring Paths

### 1. Stateless Operator

#### Concept

A stateless operator has configuration values and operation logic, but no mutable execution data. Its behavior depends only on its configuration and explicit operation inputs.

The configuration may serve as the execution instance when doing so is safe. This is an implementation detail of the convenience base rather than a separate user facing concept.

#### Use cases

1. Mathematical transformations
2. Random candidate generation that stores no state between calls
3. Direct mappings
4. Independent per candidate operations

#### Authoring experience

The author writes one role specific class and implements the role operation such as `Create`, `Evaluate`, `Mutate` or `Cross`.

Existing role specific names such as `StatelessCreator` and `StatelessMutator` express this path well.

#### Invariant

The configuration must not be mutated during execution. Obvious mutable execution state on a stateless operator will be rejected by an analyzer.

Stateless does not mean that the operation is mathematically pure. An explicit random number generator may still be consumed and external inputs may affect the result.

### 2. Stateful Operator

#### Concept

A stateful operator has configuration values and operation logic. The framework creates one state object for each resolved execution instance and passes that state to the operation logic.

The state is execution instance scoped data. It may be mutable and may contain rich helper data structures. Its lifetime can be shorter than the run when execution infrastructure creates fresh instances.

#### Use cases

1. Adaptive operator parameters
2. Historical counters and statistics
3. Candidate buffers
4. Cache data when the operator does not coordinate another operator
5. Intermediate metrics and accumulated values

#### Allowed state

State may contain values such as counters, candidates, objective vectors, arrays, tuples, dictionaries, cache entries and dedicated helper data objects.

#### Disallowed state

State must not contain or provide access to:

1. Operator configurations
2. Algorithm configurations
3. Operator execution instances
4. Algorithm execution instances
5. Other execution instances
6. Execution instance registries
7. Delegates bound to child execution instances

An operator needing any of these belongs on the explicit execution instance path.

#### Authoring experience

The author writes one role specific operator class and one state type. The framework owns the routine execution instance plumbing.

The preferred public names are role specific names such as `StatefulCreator<..., TState>`, `StatefulEvaluator<..., TState>` and `StatefulMutator<..., TState>`.

`StatefulOperator` is the documentation term for this path. The name does not need to encode the complete state restriction because XML documentation, usage specs and analyzers establish that contract.

### 3. Explicit Execution Instance Operator

#### Concept

The operator configuration and operator execution instance are separate authored types. The configuration describes reusable parameters and graph structure. The execution instance owns operation logic, mutable execution data and resolved child execution instances.

This is the full control path.

#### Use cases

1. Wrapping another operator
2. Coordinating several operators
3. Selecting or replacing child behavior during execution
4. Custom execution lifecycle requirements
5. Advanced execution infrastructure

Composite operators normally use this path, but composite is a use case rather than the name of the authoring mechanism.

#### Authoring experience

The author writes a configuration type and an execution instance type. Child execution instances are resolved once during instance creation and stored as private fields on the execution instance.

The existing role configuration and instance interfaces already define the semantic contract for this path. For example, a mutator configuration implements `IMutator<...>` and creates an `IMutatorInstance<...>`.

## Base Type Naming For The Explicit Path

No universal `CompositeOperator` or other marker base will be introduced.

The exact role specific base names must be settled through executable API usage specs before broad migration. The leading options are:

1. Use unprefixed role bases such as `Creator`, `Evaluator` and `Mutator` as genuine common bases for all authoring paths. Stateless and stateful convenience bases derive from them. Authors derive directly from an unprefixed base for the explicit execution instance path.
2. Implement the role interfaces directly when explicit execution instance control is needed. Add no base unless repeated boilerplate proves that one provides real value.
3. Use role specific names with an `Explicit` qualifier only if usage specs show that the distinction is otherwise unclear.

The preferred direction to prototype is option 1. Option 2 remains the fallback when a base does not remove meaningful boilerplate. Names such as `ExplicitExecutionInstanceCreator` are considered too verbose unless no clearer API emerges.

Any explicit path base must do useful work, such as explicitly implementing the public execution instance creation contract and exposing a role specific protected instance creation method. It must not exist only to classify the derived type.

The mutator prototype resolved this checkpoint in favor of option 1. `Mutator` provides the common execution instance creation plumbing. `StatelessMutator`, `StatefulMutator`, `WrappingMutator` and `MultiMutator` derive from it. Authors derive directly from `Mutator` and return a `MutatorInstance` when they need full control. Each remaining operator role should validate the same naming pattern through usage specs before migration.

## Wrapping And Multi Bases

Wrapping and multi bases describe configuration graph topology. They are shortcuts within the explicit execution instance path because their defining responsibility is resolving child execution instances once and transferring ownership to an authored execution instance.

Do not introduce stateless and stateful variants of wrapping and multi bases by default. That would create a cross product between topology and execution data ownership while providing little additional safety. A wrapping or multi execution instance may own ordinary private execution data as well as its resolved children. When the topology specific creation method does not offer enough control, the author can derive directly from the unprefixed role base.

Introduce an additional topology and state convenience base only if repeated usage specs demonstrate substantial boilerplate that cannot be removed through composition or a small protected helper.

## Algorithm Authoring

Algorithms exclusively use the explicit execution instance path.

Algorithms normally coordinate operators and also own execution lifecycle concerns such as streaming, continuation, completion and private execution data. A separate algorithm execution instance gives those responsibilities a natural owner even for a small algorithm.

HeuristicLib does not provide stateless or state object convenience paths for algorithms.

`Algorithm<TCandidate, TSearchSpace, TProblem, TSearchState>` is the common configuration base. It explicitly implements execution instance resolution and exposes `CreateAlgorithmInstance(ExecutionInstanceRegistry)` to derived configurations. The full registry supports meta algorithm child registries and execution wrapper replacements.

`IterativeAlgorithm<TCandidate, TSearchSpace, TProblem, TSearchState>` resolves its optional interceptor once and delegates instance creation to `CreateIterativeAlgorithmInstance(...)`. The algorithm execution instance owns the execution loop, mutable execution data and resolved child execution instances as private fields.

`IterativeAlgorithmInstance<TCandidate, TSearchSpace, TProblem, TSearchState>` provides cancellation handling, deterministic random number generator forking, interception and yielded state semantics. Its streaming entry point is sealed so concrete iterative algorithms cannot bypass that lifecycle.

Algorithm instance bases do not retain `Run` or `ExecutionInstanceRegistry`. Ordinary algorithms receive resolved dependencies through their instance constructors. Runtime composing meta algorithms such as pipelines and cycles may retain their originating registry. They directly create child algorithm instances through normal child registries when configurations must be instantiated during execution, preserving access to parent operator instances and replacement policy.

`IAlgorithm<...>` does not expose an evaluator. Concrete algorithms declare an evaluator only when they use one. Evaluator budget helpers require the observed evaluator explicitly.

## Enforcement And Guardrails

### Stateful Operator State Analyzer

A Roslyn analyzer will inspect the `TState` used by stateful operator bases.

The analyzer will report an error when a state member exposes an execution graph dependency. The initial forbidden set includes:

1. `IExecutionInstance`
2. `IExecutionInstanceResolvable`
3. `ExecutionInstanceRegistry`
4. Known operator configuration contracts
5. Known algorithm configuration contracts

Inspection must include direct fields and properties as well as arrays, tuples and generic type arguments. For user defined state helper types, inspection should recurse with cycle protection. Arbitrary external implementation graphs should not be recursively inspected because that would create fragile diagnostics.

The diagnostic should identify the offending member and explain that execution graph dependencies require the explicit execution instance path.

The analyzer is a practical guardrail, not a proof of data purity. The stateful authoring API must also avoid providing a registry that would encourage delayed child resolution.

### Operator Configuration Mutation Analyzer

A Roslyn analyzer will report direct mutation of reusable operator configuration members during operation logic. The rule applies to every operator authoring path, although the stateless and stateful paths are where configuration logic most commonly performs the operation.

The first version detects direct writes to configuration fields and properties from operation logic. Documentation also requires configuration values and referenced collections to remain unchanged during execution.

The diagnostic should explain that mutable execution data belongs in framework managed state or an explicit execution instance. It is an accident guard rather than a proof of configuration immutability and is not expected to detect every indirect mutation through referenced objects.

### Documentation And Usage Specs

XML documentation and executable API usage specs must show all three operator paths side by side. Each example must explain ownership of configuration, execution data and child execution instances.

Analyzer tests must include valid data state, direct invalid dependencies, dependencies hidden in common containers and stateless mutation violations.

## Migration Strategy

Migration will be incremental and example first.

1. Add executable API usage specs for all three paths using one representative operator role.
2. Resolve the explicit path base type naming checkpoint from those specs.
3. Implement the three paths for that role and add focused tests.
4. Implement both analyzers and their test suites.
5. Apply the established shape to creators, evaluators, selectors, crossovers, mutators, replacers, terminators and interceptors.
6. Migrate stateless and independent per candidate operators to the stateless path.
7. Migrate operators with execution data but no execution graph dependencies to the stateful path.
8. Migrate wrapping, multi, observable and other coordinating operators to explicit execution instances.
9. Migrate algorithm authoring so algorithm execution instances own child instances and loop state directly.
10. Update documentation, examples and API usage specs as each role is migrated.

Backward compatibility is not a goal for this migration. Weak or misleading base class names should be replaced rather than preserved.

## Action Items

1. [x] Add API usage specs that demonstrate the three operator authoring paths for the mutator prototype.
2. [x] Prototype the explicit mutator path with an unprefixed role base and compare it with direct interface implementation.
3. [x] Decide the explicit path base type naming for mutators before broad migration.
4. [x] Introduce role specific stateless and stateful authoring bases.
5. [x] Introduce only those explicit path bases that remove meaningful boilerplate, including wrapping and multi variants.
6. [x] Add the general stateful operator state analyzer using the operator authoring shape rather than a catalog of role specific bases.
7. [x] Add the general operator configuration mutation analyzer.
8. [x] Migrate existing operators to the appropriate authoring path.
9. [x] Migrate algorithms to explicit algorithm execution instances.
10. [x] Update `docs/operators.md`, execution documentation, examples and related API usage specs.
11. [x] Run focused tests while migrating, then run the full release test suite and formatting verification.
