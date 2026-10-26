# Operator implementation

::: info Advanced extension
Most applications only configure built-in operators. Use this page when your candidate representation or domain needs a new operation.
:::

Operator configurations are reusable descriptions. Operator executors perform the work and own any run scoped data. HeuristicLib provides three authoring paths so an operator can use the smallest execution model that fits its responsibilities.

Operator configurations build their executors in a `CreateExecutor` hook that resolution calls for every build. Each build binds an executor to resolved children and observations and shares the configuration's scoped state.

## Choose an authoring path

| Path              | Typical base                     | Use when                                                                                     |
| ----------------- | -------------------------------- | -------------------------------------------------------------------------------------------- |
| Stateless         | `StatelessEvaluator<...>`        | The operation needs configuration values but no mutable execution data                       |
| Stateful          | `StatefulEvaluator<..., TState>` | The operation needs framework managed execution data but no execution graph dependencies     |
| Explicit executor | `Evaluator<...>`                 | The operator resolves child operators or needs complete control over its execution structure |

The same pattern exists for creators, crossovers, mutators, selectors, replacers, interceptors and terminators.

The unprefixed role base is the common base of the three paths. Stateless and stateful bases derive from it. Derive directly from the unprefixed base when authoring an explicit executor.

Some roles add a single-item base as a further stateless convenience, for operations that apply independently to each item of a batch: `SingleCandidateMutator<...>`, `SingleCandidateCrossover<...>`, `SingleCandidateCreator<...>` and `SingleCandidateEvaluator<...>`. Implement the single-item method for one item; the inherited batch-wise role operation handles deterministic per-item random forks and batching. Callers holding the specialized base may invoke the single-item method directly, while ordinary role consumers continue to use the batch role operation. A role gets such a base only when independent per-item application is a genuine shape for it, so Selector, Replacer, Interceptor and Terminator have no equivalent.

The single-item method has its own name so it stays distinct from the batch operation it drives. The default is the role's verb followed by `Candidate`: `MutateCandidate`, `CreateCandidate` and `EvaluateCandidate`. `CrossParents` is the deliberate exception, because a crossover crosses one `Parents<TCandidate>` group into one candidate, so its single-item unit is a parent group rather than a candidate. A creator receives no input at all, so `CreateCandidate` takes only the random number generator and the context it needs; the requested count belongs to the batch operation.

A single-item base owns its batch operation and seals it. A subclass that replaced the batch operation would no longer be the single-item operator its type claims to be, and the single-item method it must still implement would become unreachable. Batching is a configuration decision instead: set the `Concurrency` property to run the per-item calls concurrently. Each item receives a random number generator forked from its batch position, so the result is identical for every concurrency setting and the choice is a performance decision rather than a semantic one. An operator that genuinely needs a different batch-wise operation derives from the stateless base directly, where the batch operation is the authoring surface.

`SingleCandidateMutator` is the reference implementation of this rule; `SingleCandidateCrossover`, `SingleCandidateCreator` and `SingleCandidateEvaluator` follow it.

## Choose an arity

Each role base is available at three arities. Reduced arities exist so an author who needs neither the problem nor the search space does not have to name them.

Interceptor and Terminator retain `TSearchState` in these three ordinary arities because their operations transform or inspect a produced search state. Like every other operation, both receive the call's random number generator after their primary inputs at each ordinary arity. Terminator additionally provides `Terminator<TCandidate>`, `TerminationExecutor<TCandidate>` and matching stateless and stateful bases for termination conditions that inspect none of the operation inputs, such as elapsed time, cancellation and external operator budgets. Interceptor cannot remove `TSearchState` because it returns that type.

| Declared as                                                  | Operation receives                    | Use when                                       |
| ------------------------------------------------------------ | ------------------------------------- | ---------------------------------------------- |
| `SingleCandidateMutator<TCandidate, TSearchSpace, TProblem>` | parent, random, search space, problem | The operation depends on the problem           |
| `SingleCandidateMutator<TCandidate, TSearchSpace>`           | parent, random, search space          | The operation depends on the search space only |
| `SingleCandidateMutator<TCandidate>`                         | parent, random                        | The operation depends on the candidate only    |

```csharp
public record SwapMutator : SingleCandidateMutator<Permutation>
{
    public override Permutation MutateCandidate(Permutation parent, IRandomNumberGenerator random) =>
        parent.SwapRandomIndices(random);
}
```

Reduction changes what the author declares, not where the operator fits. **Candidate-operation slots name the operation and candidate, without run types.** `IOperator<Mutation, Permutation>` is the whole contract, so the `SwapMutator` above fills any mutator slot over permutations, and so does an operator authored at the fullest rung against a specific problem.

The arity lives in the authoring bases: the base performs the type check when a run supplies types the operator was not written for, and for a stateless operator the configuration _is_ the executable part, so it needs the search space and problem in scope where it is written. A consumer, field or child property never names them.

`WrappingOperator<TOperation, TCandidate>` and `MultiOperator<TOperation, TCandidate>` store one child or an ordered child collection. They forward `Fits` and candidate contracts. Their protected `CreateExecutor<TSearchSpace, TProblem, TSearchState>(scope)` validates the configuration and constructs an executor for the run's search-space, problem and state types on every build. Resolve children through the typed scope it receives, and take persistent data from `scope.GetScopedState(this, create)`, which creates it once for the configuration's owner.

Use `.Named("name")` for a transparent selectable boundary and `.Retry(3)` to allow up to three attempts after a directly thrown `RetryableOperationException`. Both expose the child's controls. `ChoosePerInvocation` chooses one child per call and exposes no controls. A generic wrapper of your own returns `TOperation.Wrap<TCandidate, TSearchSpace, TProblem, TSearchState, TWrapper>(scope.ResolveOperator(ChildOperator), wrapper)` with a struct `IInvocationWrapper`, which works for every operation, including external ones. The wrapper can read the call's random number generator and proceed with another one or on another executor of the same operation; it supplies the wrapping executor's controls by implementing `IExecutorControlProvider`, and otherwise the child's controls pass through. The [stateful composition walkthrough](/guide/extending/operator-composition#a-complete-stateful-composition) shows the complete authoring pattern.

> Watch the last type argument. The stateful bases end in `TState`, so `StatefulMutator<TCandidate, TSearchSpace, TState>` and `StatefulMutator<TCandidate, TSearchSpace, TProblem>` have the same shape. Passing a problem where the state belongs compiles and silently produces a problem-agnostic operator whose state is a problem. `HLib0004` reports this.

## Stateless operators

A stateless operator configuration also implements its operation logic. Its configuration values and referenced collections must remain unchanged during execution.

```csharp
private sealed record FirstValueEvaluator
    : StatelessEvaluator<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>
{
    public override IReadOnlyList<ObjectiveVector> Evaluate(
        IReadOnlyList<RealVector> candidates,
        IRandomNumberGenerator random,
        BoundedRealVectorSearchSpace searchSpace,
        TestFunctionProblem problem) =>
        candidates.Select(candidate => new ObjectiveVector(candidate[0])).ToArray();
}
```

Use specialized helpers such as `SingleCandidateEvaluator` when the role offers one that exactly matches the operation shape.

## Stateful operators

A stateful operator receives one fresh `TState` for each selected logical execution. Its base requests that state from `scope.GetScopedState(this, CreateScopedState)` on every build, so `CreateScopedState()` runs once per owner and each executor built across observation contexts shares the state. Independent logical executions receive independent state. The state can contain counters, caches and ordinary helper data structures.

```csharp
private sealed record CountingEvaluator
    : StatefulEvaluator<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem, CountingEvaluator.State>
{
    public sealed class State
    {
        public int Invocations { get; set; }
    }

    protected override State CreateScopedState() => new();

    protected override IReadOnlyList<ObjectiveVector> Evaluate(
        IReadOnlyList<RealVector> candidates,
        State state,
        IRandomNumberGenerator random,
        BoundedRealVectorSearchSpace searchSpace,
        TestFunctionProblem problem)
    {
        state.Invocations++;
        return candidates.Select(_ => new ObjectiveVector(state.Invocations)).ToArray();
    }
}
```

`CreateScopedState()` must return a fresh object. State must not contain operator or algorithm configurations, executors, scopes or delegates bound to child executors. Use the explicit path when an operator needs any of those execution graph dependencies.

Framework managed state does not have a disposal lifecycle. Do not put disposable resources there. Such ownership requires an explicitly authored executor and a defined lifecycle mechanism.

## Explicit executors

The configuration describes reusable parameters and graph structure. For creation, crossover, mutation, evaluation, selection, refinement and replacement, the protected `CreateExecutor(ResolutionScope scope)` runs for every build. It resolves child configurations through the supplied scope and creates a node for that context. The executor owns typed operation logic and resolved child references; execution data from `scope.GetScopedState(this, create)` survives contextual rebinding.

```csharp
private sealed record ForwardingEvaluator(IOperator<Evaluation, RealVector> Inner)
    : Evaluator<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>
{
    protected override IEvaluationExecutor<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem> CreateExecutor(ResolutionScope scope) =>
        new Executor(scope.Resolve<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>(Inner));

    private sealed class Executor(IEvaluationExecutor<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem> inner)
        : EvaluationExecutor<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>
    {
        public override IReadOnlyList<ObjectiveVector> Evaluate(
            IReadOnlyList<RealVector> candidates,
            IRandomNumberGenerator random,
            BoundedRealVectorSearchSpace searchSpace,
            TestFunctionProblem problem) =>
            inner.Evaluate(candidates, random, searchSpace, problem);
    }
}
```

Resolve declared child operators inside `CreateExecutor`. Take persistent counters, caches and helper data from `scope.GetScopedState(this, create)` rather than creating them in the hook, which would reset them on every build. Retain the scope only when deferred graph construction is an intentional part of the operator. Algorithms resolve their configured parameters and keep parameter-derived behavior in their execution code rather than constructing hidden operators.

Return the most concrete accessible executor type that is useful to callers. A private nested executor is returned through its role specific executor base.

## Mutable run parameters

Some specialized operators expose a configured starting value and a mutable current value on their executor. Variable strength mutation is the current example:

- `GaussianMutator.MutationStrength` is immutable reusable configuration.
- `IMutationStrengthControl.CurrentMutationStrength` is mutable execution data initialized from that configuration. This control has no mutation operation.
- `EvolutionStrategy` projects the optional control from its selected mutator's raw binding and invokes mutation through the wrapped executor. Generated observations preserve adaptation and report each mutation once. The control and operation retain the same selected logical state across rebinding.

The same mutator configuration can therefore be reused for independent runs without one run changing another run's starting strength. This is a specialized contract, not a general convention that operator configuration properties become mutable on executors.

Checking the wrapped mutator with `is IMutationStrengthControl` is insufficient: an observation wrapper can implement the mutation role without implementing the source's additional control interface. Resolve the control separately from the selected configuration's raw executor, and keep mutation calls on the advised executor. Generic naming, retry and inspection wrappers forward the child's controls. A composite exposes its own controls; the resolver does not search arbitrary children. See [Controls, failures and lifetime](/contributing/architecture/execution-resolution#controls-failures-and-lifetime) for the current projection boundary.

## Wrapping and multi operators

Every operation uses `WrappingOperator<TOperation, TCandidate>` and `MultiOperator<TOperation, TCandidate>`, with `ChildOperator` and `ChildOperators` configuration properties. Their executors hold the resolved children themselves. Keep shared counters and caches in the scoped state from `scope.GetScopedState(this, create)`, and resolve current children inside `CreateExecutor`. Existing executors keep their original children when a new context is resolved.

When a child plays a specific part in the operation, derive from `Operator<TOperation, TCandidate>` and declare that child yourself. This configuration excerpt omits the executor body:

```csharp
public record EliteSelector<TCandidate> : Operator<Selection, TCandidate>
{
    public required IOperator<Selection, TCandidate> SelectorForRemaining { get; init; }
    public int Elites { get; init; } = 1;

    protected override IExecutor CreateExecutor<TSearchSpace, TProblem, TSearchState>(
        ResolutionScope<TCandidate, TSearchSpace, TProblem, TSearchState> scope)
    {
        var selectorForRemaining = scope.Resolve(SelectorForRemaining);
        return new Executor<TSearchSpace, TProblem>(selectorForRemaining, Elites);
    }

    // The selection executor takes elites first and requests only the remaining places from its child.
}
```

`EliteSelector` takes its elites first and asks `SelectorForRemaining` only for the places that are left, so calling that child `ChildSelector` would hide what it does. Wrapping it and adding a second, better-named property is not the fix. The two properties return the same object and a reader cannot tell which one to use. Declaring the child directly also lets the constructor fix how many children there are. `GenderSpecificSelector` needs exactly one `FemaleSelector` and one `MaleSelector`, which a multi base cannot express.

Like every composite, these name only the candidate. A child slot is `IOperator<Selection, TCandidate>`, and the run's search space and problem arrive as method type arguments on `CreateExecutor`. See [Choose an arity](#choose-an-arity) for what that costs the author and why it costs the consumer nothing.

Composition helpers such as choosing one child, applying a transformation and running a pipeline are covered in [Operator composition](/guide/extending/operator-composition).

`ChoosePerItem` routes through `WeightedRouter<TChoice>`. It passes a router over the resolved children to its operation's `Route`, whose routed executor assigns every item of a call with the call's random number generator; algorithm code uses the router directly. Create it with `WeightedRouter.Create(choices)` for uniform routing or `WeightedRouter.Create(choices, weights)`, using `WeightedIndexSampler` semantics; weights are compiled once per router. Its `Route` overloads accept either inputs or an item count, an RNG and a handler that receives one choice and its batch of inputs or item positions. It assigns every input before handling any batch, handles the batches in choice order, skips empty batches and restores results to their original positions. When every input goes to the same choice, that choice receives the original inputs. Each handler must return exactly the batch size; a mismatch throws `InvalidOperationException`. `ChoosePerInvocation` chooses once for the whole invocation and samples its child from a `WeightedItemSampler<T>`, using the call's random number generator for the choice and the child.

Algorithms use the router to apply parameter-derived behavior in their execution code. GA, NSGA2 and ALPS create `WeightedRouter.AtRate(mutator, mutationRate)` in their executor constructor. The router owns the resolved mutator and a null pass-through choice, so the executor retains only the router for mutation. It routes each offspring to the mutator with the rate's probability, and the algorithm mutates only that batch. Rates at most zero, negative infinity and NaN route every offspring to null; rates at least one and positive infinity route every offspring to the mutator, which receives the original offspring. Neither extreme draws from the RNG. The algorithms use the step's same RNG for routing and mutation, so observers count only offspring routed to the configured mutator. `WeightedRouter.AtRate(rate)` supplies boolean choices for callers that need a decision, and `AppliedAtRate` remains a configuration helper for explicitly configured compositions using the same rate weights.

## Define a new operation

When no existing role fits, define an operation. It has an executor interface with one named method, an operation type that implements `IOperation<TSelf>` and wraps that executor, and typed resolution extensions. Operators then implement `IOperator<TOperation, TCandidate>` for it, and naming, retry, choice, counting, timing and custom advice work without further code. A built-in operation is defined the same way.

The named method ends with the call's random number generator, search space and problem:

<<< ../../../test/HeuristicLib.Tests.Docs/Architecture/OperationDefinitionDocumentationSpecs.cs#operation-executor{csharp}

The operation implements the static members of `IOperation<TSelf>` explicitly. It recognizes its executor interface, wraps an executor in a private wrapping executor, and holds a private `Invocation` struct for one call. An item operation, marked with `IItemOperation<TSelf>`, also routes a call's items to several executors through a private routed executor. The library bases `WrappingExecutor<TExecutor, TWrapper>` and `RoutingExecutor<TExecutor>` hold the executors, the wrapper and the router, type them as the operation's executor interface and forward controls:

<<< ../../../test/HeuristicLib.Tests.Docs/Architecture/OperationDefinitionDocumentationSpecs.cs#operation-type{csharp}

The invocation is what every wrapper receives: its `Random`, `SearchSpace` and `Problem`, and `Proceed`. An operation whose calls do not split into items implements `IOperation<TSelf>` instead and needs no routed executor. An operation with candidate work returns true from `SupportsCandidateWork` and implements `CountCandidateWork` on its invocation. The static members, the invocation and the two executors stay per operation: C# needs a nominal class for each executor interface, a struct has no base class, and the operation's static members are the only place that names the executor interface for the run's types.

Resolution extensions cast the resolved executor to the operation's interface. `TryResolve` calls `TryResolveOperator`, which takes the interface from the executor variable and reports a failure as a reason:

<<< ../../../test/HeuristicLib.Tests.Docs/Architecture/OperationDefinitionDocumentationSpecs.cs#operation-resolution{csharp}

### Make the operation observable

An operation is observable once it declares three parts beside the operation, as each built-in operation does: an observation record, advice that builds the record from the typed call, and a `Invocations` source. The record carries the call's inputs and outputs together with its search space and problem, so a trace can rank by that problem's objective:

<<< ../../../test/HeuristicLib.Tests.Docs/Architecture/OperationDefinitionDocumentationSpecs.cs#operation-observation{csharp}

`Analyzer.Trace(estimator.Invocations, observation => observation.Estimates.Count)` and `Aspect.Observe(estimator.Invocations, …)` then work as they do for built-in operations. The advice casts because `OperatorAdvice<TOperation, TCandidate>` receives the executor untyped; a role advice base such as `MutatorAdvice<TCandidate>` removes that cast for its operation.

## Scaffolding roles and cross-cutting concerns

Coding agents are useful optional aids for the repetitive parts of operator authoring. Adding a concrete operator to an existing role is usually small work, because deriving from an existing authoring base is most of it. A new operator role is not: it brings a whole family with it, including configuration and executor arity ladders, stateless and stateful bases, applicable single-item bases, wrapping and multi topologies, construction companions, tests and API usage specs. A new cross-cutting concern brings one role-specific adapter for every applicable role. That expansion is mechanical enough for an agent to draft and large enough to make agent assistance worthwhile.

Agent-produced code is normal contributor-owned source. Inspect and refactor it normally, review every public member as a deliberate API addition, and validate it with the compiler, analyzers, focused behavior tests and API usage specs. Point the agent at the current contracts and guidelines in addition to the closest accepted role family: all eight current roles follow the accepted topology, but their operation inputs, applicability and lifecycle semantics still differ.

An agent will not settle a concern's semantics for you. Implement and review the concern for one representative role first, then state its lifecycle and applicability independently of that role before expanding it. Similar signatures do not prove identical semantics: duration measurement records failed calls because it updates in `finally`, while counting updates only after a successful child call; candidate-count conveniences apply only when the role produces an appropriate candidate collection.

## Configuration and execution rules

- Keep configuration values unchanged during execution. Retained collection inputs are immutable snapshots.
- Keep randomness, the search space and the problem explicit in operation calls.
- Store mutable run data in framework managed state or an explicitly authored executor.
- Resolve the same configuration through one scope when sharing its logical scoped state is intentional.
- Use independent root scopes for independent executions. Sibling scopes select independent state only when no ancestor already owns that configuration.
- Do not assume stateful operation calls are serialized unless the owning execution path guarantees it.

## Roslyn analyzer guardrails

The `OperatorAuthoringAnalyzer` Roslyn analyzer assists with common mistakes across operator roles:

- `HLib0002` reports execution graph dependencies exposed through stateful operator state or requested through `GetScopedState`, a `GetScopedState` request that passes a configuration other than `this`, and direct resolution of another configuration's observation point.
- `HLib0003` reports direct mutation of configuration members outside construction, in operators, algorithms and consumer-defined roles alike.
- `HLib0004` reports a framework contract type bound as the stateful state type argument, which is usually a reduced-arity mix-up.

These diagnostics are guardrails, not a proof that every invariant is satisfied. They cannot reliably identify every indirect mutation through a referenced collection, helper object or delegate.

## Algorithms use the explicit path

Algorithms coordinate operators and execution flow, so they always use a configuration paired with an explicitly authored executor. See [Writing algorithms](/guide/extending/writing-algorithms) for the public authoring model.

## Related pages

- [Operators](/guide/fundamentals/operators)
- [Operator composition](/guide/extending/operator-composition)
- [Running algorithms](/guide/execution/running-algorithms)
- [Writing algorithms](/guide/extending/writing-algorithms)
