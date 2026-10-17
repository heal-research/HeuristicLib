# Operator implementation

::: info Advanced extension
Most applications only configure built-in operators. Use this page when your candidate representation or domain needs a new operation.
:::

Operator configurations are reusable descriptions. Operator executors perform the work and own any run scoped data. HeuristicLib provides three authoring paths so an operator can use the smallest execution model that fits its responsibilities.

Operator configurations prepare typed executor factories. Each factory binds an executor to resolved children and observations.

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

Interceptor and Terminator retain `TSearchState` in these three ordinary arities because their operations transform or inspect a produced search state. Terminator additionally provides `Terminator<TCandidate>`, `TerminationExecutor<TCandidate>` and matching stateless and stateful bases for termination conditions that inspect none of the operation inputs, such as elapsed time, cancellation and external operator budgets. Interceptor cannot remove `TSearchState` because it returns that type.

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

Reduction changes what the author declares, not where the operator fits. **Mutation and evaluation slots name the candidate and operation, without run types.** `IOperator<Mutation, Permutation>` is the whole contract, so the `SwapMutator` above fills any mutator slot over permutations, and so does an operator authored at the fullest rung against a specific problem.

The arity lives in the authoring bases: the base performs the type check when a run supplies types the operator was not written for, and for a stateless operator the configuration _is_ the executable part, so it needs the search space and problem in scope where it is written. A consumer, field or child property never names them.

`WrappingOperator<TOperation, TCandidate>` and `MultiOperator<TOperation, TCandidate>` store one child or an ordered child collection. They forward `Fits` and candidate contracts. Their protected `CreateOperatorFactory()` initializes persistent data once; the returned `OperatorExecutorFactory<TOperation, TCandidate>` constructs executors for the run's search-space, problem and state types. Resolve children through the supplied construction scope.

Use `.Named("name")` for a transparent selectable boundary and `.Retry(3)` to allow up to three attempts after a directly thrown `RetryableOperationException`. Both preserve the child's control provider. The [stateful composition walkthrough](/guide/extending/operator-composition#a-complete-stateful-composition) shows the complete authoring pattern. Roles not yet using `IOperator<TOperation, TCandidate>` retain their nominal topology bases.

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

A stateful operator receives one fresh `TState` for each selected logical execution. Its base calls `CreateInitialState()` during preparation and retains one raw executor across observation contexts. Independent logical executions receive independent state. The state can contain counters, caches and ordinary helper data structures.

```csharp
private sealed record CountingEvaluator
    : StatefulEvaluator<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem, CountingEvaluator.ExecutionState>
{
    public sealed class ExecutionState
    {
        public int Calls { get; set; }
    }

    protected override ExecutionState CreateInitialState() => new();

    protected override IReadOnlyList<ObjectiveVector> Evaluate(
        IReadOnlyList<RealVector> candidates,
        ExecutionState state,
        IRandomNumberGenerator random,
        BoundedRealVectorSearchSpace searchSpace,
        TestFunctionProblem problem)
    {
        state.Calls++;
        return candidates.Select(_ => new ObjectiveVector(state.Calls)).ToArray();
    }
}
```

`CreateInitialState()` must return a fresh object. State must not contain operator or algorithm configurations, executors, scopes or delegates bound to child executors. Use the explicit path when an operator needs any of those execution graph dependencies.

Framework managed state does not have a disposal lifecycle. Do not put disposable resources there. Such ownership requires an explicitly authored executor and a defined lifecycle mechanism.

## Explicit executors

The configuration describes reusable parameters and graph structure. For mutation and evaluation, the protected `CreateFactory()` prepares persistent execution data without a scope. Its returned factory resolves child configurations through the supplied scope and creates a node for that context. The executor owns typed operation logic and resolved child references; prepared mutable data survives contextual rebinding.

```csharp
private sealed record ForwardingEvaluator(IOperator<Evaluation, RealVector> Inner)
    : Evaluator<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>
{
    protected override ExecutorFactory<IEvaluationExecutor<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>> CreateFactory() =>
        scope => new Executor(scope.Resolve<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>(Inner));

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

Resolve declared child operators inside the returned factory. Allocate persistent counters, caches and helper data before returning it. Retain the scope only when deferred graph construction is an intentional part of the operator. Algorithms resolve their configured parameters and keep parameter-derived behavior in their execution code rather than constructing hidden operators.

Return the most concrete accessible executor type that is useful to callers. A private nested executor is returned through its role specific executor base.

## Mutable run parameters

Some specialized operators expose a configured starting value and a mutable current value on their executor. Variable strength mutation is the current example:

- `GaussianMutator.MutationStrength` is immutable reusable configuration.
- `IMutationStrengthControl.CurrentMutationStrength` is mutable execution data initialized from that configuration. This control has no mutation operation.
- `EvolutionStrategy` projects the optional control from its selected mutator's raw binding and invokes mutation through the wrapped executor. Generated observations preserve adaptation and report each mutation once. The control and operation retain the same selected logical state across rebinding.

The same mutator configuration can therefore be reused for independent runs without one run changing another run's starting strength. This is a specialized contract, not a general convention that operator configuration properties become mutable on executors.

Checking the wrapped mutator with `is IMutationStrengthControl` is insufficient: an observation wrapper can implement the mutation role without implementing the source's additional control interface. Resolve the control separately from the selected configuration's raw executor, and keep mutation calls on the advised executor. Generic naming, retry and inspection wrappers forward the child's control provider. A composite exposes its own controls; the resolver does not search arbitrary children. See [Controls, failures and lifetime](/contributing/architecture/execution-resolution#controls-failures-and-lifetime) for the current projection boundary.

## Wrapping and multi operators

Mutation and evaluation use `WrappingOperator<TOperation, TCandidate>` and `MultiOperator<TOperation, TCandidate>`, with `ChildOperator` and `ChildOperators` configuration properties. Their executors may use role-specific storage such as `WrappingMutationExecutor.ChildMutator`. Keep shared counters and caches in the factory owner, and resolve current children inside its returned construction function. Existing executors keep their original children when a new context is resolved.

The remaining nominal roles retain `CreateWrapperFactory` and `CreateCompositeFactory` hooks. Those return constructors receiving one resolved child or an immutable array of children. Their matching executor bases provide protected child storage.

When a child plays a specific part in the operation, derive from the unprefixed role base instead and declare that child yourself:

```csharp
public record EliteSelector<TCandidate> : ISelector<TCandidate>
{
    public ISelector<TCandidate> SelectorForRemaining { get; init; }
    public int Elites { get; init; } = 1;

    public ExecutorFactory<ISelectionExecutor<TCandidate, TRunSearchSpace, TRunProblem>>
        CreateExecutorFactory<TRunSearchSpace, TRunProblem>()
        where TRunSearchSpace : class, ISearchSpace<TCandidate>
        where TRunProblem : class, IProblem<TCandidate, TRunSearchSpace> =>
        scope => new Executor<TRunSearchSpace, TRunProblem>(
            scope.Resolve<TCandidate, TRunSearchSpace, TRunProblem>(SelectorForRemaining), Elites);
    // ...
}
```

`EliteSelector` takes its elites first and asks `SelectorForRemaining` only for the places that are left, so calling that child `ChildSelector` would hide what it does. Wrapping it and adding a second, better-named property is not the fix. The two properties return the same object and a reader cannot tell which one to use. Declaring the child directly also lets the constructor fix how many children there are. `GenderSpecificSelector` needs exactly one `FemaleSelector` and one `MaleSelector`, which a multi base cannot express.

Like every composite, these name only the candidate. A child slot is `ISelector<TCandidate>`, and the run's search space and problem arrive as method type arguments on the creation method. See [Choose an arity](#choose-an-arity) for what that costs the author and why it costs the consumer nothing.

Composition helpers such as choosing one child, applying a transformation and running a pipeline are covered in [Operator composition](/guide/extending/operator-composition).

`WeightedRouter<TChoice>` supplies the per-input routing used by `ChooseOneCreator`, `ChooseOneCrossover`, `ChooseOneMutator` and `ChooseOneRefiner`. Create it with `WeightedRouter.Create(choices)` for uniform routing or `WeightedRouter.Create(choices, weights)`, using `WeightedIndexSampler` semantics; weights are compiled once per router. Its `Route` overloads accept either inputs or a requested output count, an RNG and a handler that receives one choice and its batch. It assigns every input before handling any batch, handles the batches in choice order, skips empty batches and restores results to their original positions. When every input goes to the same choice, that choice receives the original inputs. Each handler must return exactly the batch size; a mismatch throws `InvalidOperationException`. `ChooseOneSelector` and `ChooseOneReplacer` choose once for the whole call and sample their child from a `WeightedItemSampler<T>`.

Algorithms use the router to apply parameter-derived behavior in their execution code. GA, NSGA2 and ALPS create `WeightedRouter.AtRate(mutator, mutationRate)` in their executor constructor. The router owns the resolved mutator and a null pass-through choice, so the executor retains only the router for mutation. It routes each offspring to the mutator with the rate's probability, and the algorithm mutates only that batch. Rates at most zero, negative infinity and NaN route every offspring to null; rates at least one and positive infinity route every offspring to the mutator, which receives the original offspring. Neither extreme draws from the RNG. The algorithms use the step's same RNG for routing and mutation, so observers count only offspring routed to the configured mutator. `WeightedRouter.AtRate(rate)` supplies boolean choices for callers that need a decision, and `AppliedAtRate` remains a configuration helper for explicitly configured compositions using the same rate weights.

## Scaffolding roles and cross-cutting concerns

Coding agents are useful optional aids for the repetitive parts of operator authoring. Adding a concrete operator to an existing role is usually small work, because deriving from an existing authoring base is most of it. A new operator role is not: it brings a whole family with it, including configuration and executor arity ladders, stateless and stateful bases, applicable single-item bases, wrapping and multi topologies, construction companions, tests and API usage specs. A new cross-cutting concern brings one role-specific adapter for every applicable role. That expansion is mechanical enough for an agent to draft and large enough to make agent assistance worthwhile.

Agent-produced code is normal contributor-owned source. Inspect and refactor it normally, review every public member as a deliberate API addition, and validate it with the compiler, analyzers, focused behavior tests and API usage specs. Point the agent at the current contracts and guidelines in addition to the closest accepted role family: all eight current roles follow the accepted topology, but their operation inputs, applicability and lifecycle semantics still differ.

An agent will not settle a concern's semantics for you. Implement and review the concern for one representative role first, then state its lifecycle and applicability independently of that role before expanding it. Similar signatures do not prove identical semantics: duration measurement records failed calls because it updates in `finally`, while counting updates only after a successful child call; candidate-count conveniences apply only when the role produces an appropriate candidate collection.

## Configuration and execution rules

- Keep configuration values unchanged during execution. Retained collection inputs are immutable snapshots.
- Keep randomness, the search space and the problem explicit in operation calls.
- Store mutable run data in framework managed state or an explicitly authored executor.
- Resolve the same configuration through one scope when sharing its logical execution state is intentional.
- Use independent root scopes for independent executions. Sibling scopes select independent state only when no ancestor already owns that configuration.
- Do not assume stateful operation calls are serialized unless the owning execution path guarantees it.

## Roslyn analyzer guardrails

The `OperatorAuthoringAnalyzer` Roslyn analyzer assists with common mistakes across operator roles:

- `HLib0002` reports execution graph dependencies exposed through stateful operator state.
- `HLib0003` reports direct mutation of operator configuration members during operation logic.
- `HLib0004` reports a framework contract type bound as the stateful state type argument, which is usually a reduced-arity mix-up.

These diagnostics are guardrails, not a proof that every invariant is satisfied. They cannot reliably identify every indirect mutation through a referenced collection, helper object or delegate.

## Algorithms use the explicit path

Algorithms coordinate operators and execution flow, so they always use a configuration paired with an explicitly authored executor. See [Writing algorithms](/guide/extending/writing-algorithms) for the public authoring model.

## Related pages

- [Operators](/guide/fundamentals/operators)
- [Operator composition](/guide/extending/operator-composition)
- [Running algorithms](/guide/execution/running-algorithms)
- [Writing algorithms](/guide/extending/writing-algorithms)
