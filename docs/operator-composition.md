# Operator composition

Operator composition builds a new operator configuration from one or more existing operator configurations. The resulting configuration remains reusable and resolves its child execution instances as part of the execution graph.

Composition policies describe how child operations are coordinated. `Wrapping*` and `Multi*` bases are authoring infrastructure for implementing those policies. They do not prescribe behavior by themselves.

The built in composition overview is:

| Composition aspect                | Coordination semantics                                                                                            | Helpers and examples                                                                                        | Applicability                                         |
| --------------------------------- | ----------------------------------------------------------------------------------------------------------------- | ----------------------------------------------------------------------------------------------------------- | ----------------------------------------------------- |
| Weighted alternatives per element | Chooses a child independently for each element of a batch, invokes grouped child batches and restores input order | `ChooseOneCreator`, `ChooseOneCrossover`, `ChooseOneMutator`                                                | Creators, crossovers and mutators                     |
| Weighted alternatives per call    | Chooses one child for the complete operation call                                                                 | `ChooseOneSelector`, `ChooseOneReplacer`                                                                    | Selectors and replacers                               |
| Conditional application           | Chooses between an operation and role specific unchanged behavior                                                 | `mutator.WithRate(...)`, `crossover.WithRate(...)`                                                          | Mutators and crossovers                               |
| Sequential composition            | Passes each stage result to the next stage in order                                                               | `PipelineMutator`, `PipelineInterceptor`                                                                    | Mutators and interceptors                             |
| Unconditional postprocessing      | Invokes a source operation and always transforms its complete result                                              | `TransformedCreator`, `TransformedCrossover`                                                                | Creators and crossovers with a mutator transformation |
| Logical composition               | Combines child conditions with short circuit Boolean logic                                                        | `AnyTerminator`, `AllTerminator`                                                                            | Terminators                                           |
| Fallback composition              | Handles part of a request directly and delegates the remainder                                                    | `PredefinedCandidatesCreator`                                                                               | Creators                                              |
| Specialized coordination          | Applies a policy whose meaning depends on one operator contract                                                   | Caching, repetition and evaluation limits, elite inclusion, mate restrictions and gender specific selection | Evaluators and selectors                              |
| Observation and instrumentation   | Wraps an operation boundary to observe, count or measure calls                                                    | `Observable*`, `Counting*` and `DurationMeasuring*`                                                         | All operator roles                                    |

A missing helper usually means that the role does not provide the inputs needed by that policy or that the intermediate result has no single clear meaning. It does not prohibit authors from implementing a domain specific composition with explicit semantics.

## Cardinality conventions

Operator roles usually follow these cardinality conventions:

1. A creator returns the requested number of candidates.
2. An evaluator returns one objective vector for each supplied candidate in the same order.
3. A crossover returns one candidate for each supplied parent group.
4. A mutator returns one candidate for each supplied candidate.
5. A selector and replacer return the requested number of evaluated candidates.

These are conventions rather than universal restrictions. A specialized operator may intentionally change cardinality when its owning algorithm supports that behavior. For example, a replacer could return more candidates than requested to grow a population.

Individual composition policies may require a stronger contract. A composition must document such a requirement when its coordination behavior depends on cardinality.

## Weighted alternatives

`ChooseOneCreator`, `ChooseOneCrossover` and `ChooseOneMutator` select a child independently for each element of a requested batch. Elements assigned to the same child are passed to that child as one batch. Results are then restored to the original element order.

This order restoration requires every selected child to return exactly one result for each assigned element. `ChooseOneCreator` therefore requires each child creator to return the assigned count. `ChooseOneCrossover` and `ChooseOneMutator` require each child to preserve its assigned batch size.

Weights are relative and finite. They must be nonnegative and at least one weight must be greater than zero. Omitting weights assigns the same normalized weight to every child.

`ChooseOneSelector` and `ChooseOneReplacer` make one weighted choice for each complete operation call. The selected child receives the complete population inputs and requested count. This preserves the meaning of a selector or replacer policy as a decision over a complete population instead of mixing fragments from several policies.

```csharp
var creator = ChooseOneCreator.Create(
    [uniformCreator, normalCreator],
    [0.75, 0.25]);

var mutator = ChooseOneMutator.Create(
    [smallMutation, largeMutation],
    [0.8, 0.2]);

var selector = ChooseOneSelector.Create(
    [tournamentSelector, proportionalSelector],
    [0.6, 0.4]);
```

### Conditional application

`mutator.WithRate(mutationRate)` is a weighted choice between the mutator and `NoChangeMutator`. The decision is made independently for each candidate.

`crossover.WithRate(crossoverRate)` is a weighted choice between the crossover and `SelectFirstParentCrossover`. The decision is made independently for each parent group. Skipping crossover therefore returns the first parent candidate.

Rate controlled composition is intended for operations that should be applied conditionally. It is distinct from transformed composition, which always invokes its transformation step.

## Sequential composition

`PipelineMutator` passes the complete result batch from each mutator to the next mutator in order.

```csharp
var mutator = new PipelineMutator<TCandidate, TSearchSpace, TProblem>([
    firstMutator,
    secondMutator
]);
```

`PipelineInterceptor` similarly passes the transformed search state from each interceptor to the next interceptor.

Selectors do not have a general pipeline because it is unclear whether a later selector should receive the original population or the previous selection and which count should apply at each stage. Replacers have the same ambiguity for the previous and offspring populations. A specialized pipeline may define those choices when an algorithm has a concrete use case.

A pipeline stage may change cardinality when the following stages and owning algorithm support it. Pipeline composition itself does not need to restore a relationship with the original input positions.

Creators do not form a natural pipeline because a creator does not consume candidates from another creator. Crossovers also consume parent groups rather than candidates produced by another crossover. Transformed composition covers the common postprocessing need for these roles.

## Transformed creators and crossovers

`TransformedCreator` invokes a creator and then always invokes one mutator on the created candidate batch.

`TransformedCrossover` invokes a crossover and then always invokes one mutator on the resulting candidate batch.

```csharp
var creator = new TransformedCreator<TCandidate, TSearchSpace, TProblem>(baseCreator, repairMutator);
var crossover = new TransformedCrossover<TCandidate, TSearchSpace, TProblem>(baseCrossover, repairMutator);
```

The mutator is invoked once for every outer operation call, even when it performs no changes. A rate controlled or weighted mutator may be supplied deliberately, but users should normally read transformed composition as unconditional postprocessing.

Transformed composition does not require the mutator to preserve cardinality. The owning algorithm remains responsible for accepting the resulting batch shape.

## Terminator composition

`AnyTerminator` stops when any child terminator stops. `AllTerminator` stops when every child terminator stops.

Terminator checks may update execution data. Each composition invokes its child terminators according to ordinary `Any` and `All` short circuit behavior. Child order can therefore matter when terminators have effectful checks.

For an empty child collection, `AnyTerminator` returns false and `AllTerminator` returns true.

See [Operators](operators.md#terminator-ownership) for ownership and lifecycle semantics.

## Specialized wrappers

Some compositions express a role specific policy rather than a general choose one or pipeline pattern.

1. `PredefinedCandidatesCreator` emits configured candidates before delegating remaining requests to a fallback creator.
2. Evaluator wrappers provide caching, repeated evaluation and evaluation limits. These policies rely on objective vectors remaining aligned with their input candidates.
3. Selector wrappers provide policies such as elite inclusion and avoiding equal mates.
4. `GenderSpecificSelector` requests half of the candidates from its female selector and half from its male selector, then returns consecutive female and male pairs.
5. Domain specific operators may provide narrower composition, such as selecting among symbolic expression tree mutations.

These types should document their own batching, state and cardinality behavior because their policies are role specific.

## Observation and instrumentation boundaries

Observable, counted and measured operators are also wrappers. Their placement determines which work they observe.

Every operator role provides public `Observable*`, `Counting*` and `DurationMeasuring*` configurations. Prefer the fluent observation, counting and duration methods because they express the wrapper at the point of composition with less type noise. They return the concrete wrapper types, while direct construction remains available for advanced cases that benefit from naming the instrumentation configuration explicitly.

Wrapping a complete composition observes the outer operation boundary and includes all nested work. Wrapping one child observes only calls that reach that child. For example, measuring a `TransformedCrossover` includes both crossover and mutation work, while measuring its child crossover includes only crossover work.

See [Observability and analysis](observability-and-analysis.md) for observer, counter and duration APIs.

## Specialized execution instance capabilities

General wrappers and compositions currently preserve the operator role contract, but they do not automatically preserve additional execution instance capabilities. In particular, wrapping or composing an `IVariableStrengthMutator` such as `GaussianMutator` currently exposes an ordinary `IMutatorInstance` at the outer boundary.

`EvolutionStrategy` adapts mutation strength only when its resolved mutator instance implements `IVariableStrengthMutatorInstance`. Placing an observable, counting, duration measuring or other general mutator wrapper around a variable strength mutator therefore currently disables that adaptation. The wrapped mutator continues to use its configured mutation strength.

This is a known limitation. Avoid wrapping an adaptive mutator when the evolution strategy must retain mutation strength adaptation. A future design must preserve specialized run scoped capabilities through composition without requiring every general wrapper to contain role specific type checks.

## Randomness and execution instances

Compositions whose role receives an explicit random number generator invoke children in the order defined by their policy. Random draw order is therefore part of reproducible behavior. Adding, removing or reordering child operators may change later random draws even when the same root seed is used. Interceptors and terminators do not receive a random number generator, so their built in compositions are deterministic with respect to child ordering unless a child depends on some other explicit input or external resource.

Child configurations are resolved once for each composition execution instance. Reusing the same child configuration elsewhere in the same execution instance registry reuses the same child execution instance and its execution data.

See [Randomness](randomness.md) and [Configuration vs execution instances](execution-instances.md) for the underlying models.

## Authoring a composition

Use the role specific `Wrapping*` base when a composition coordinates one child of the same role. Use the role specific `Multi*` base when it coordinates several children of the same role.

Derive directly from the unprefixed role base when a composition coordinates different roles or needs a custom execution structure. `TransformedCreator` and `TransformedCrossover` follow this path because each coordinates both a source operator and a mutator.

The authored execution instance owns resolved child instances and mutable execution data. The reusable configuration owns parameters and child configurations.
