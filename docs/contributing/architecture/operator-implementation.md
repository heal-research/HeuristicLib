# Operator implementation

::: info Advanced extension
Most applications only configure built-in operators. Use this page when your candidate representation or domain needs a new operation.
:::

Operator configurations are reusable descriptions. Operator execution instances perform the work and own any run scoped data. HeuristicLib provides three authoring paths so an operator can use the smallest execution model that fits its responsibilities.

## Choose an authoring path

| Path                        | Typical base                     | Use when                                                                                     |
| --------------------------- | -------------------------------- | -------------------------------------------------------------------------------------------- |
| Stateless                   | `StatelessEvaluator<...>`        | The operation needs configuration values but no mutable execution data                       |
| Stateful                    | `StatefulEvaluator<..., TState>` | The operation needs framework managed execution data but no execution graph dependencies     |
| Explicit execution instance | `Evaluator<...>`                 | The operator resolves child operators or needs complete control over its execution structure |

The same pattern exists for creators, crossovers, mutators, selectors, replacers, interceptors and terminators.

The unprefixed role base is the common base of the three paths. Stateless and stateful bases derive from it. Derive directly from the unprefixed base when authoring an explicit execution instance.

Some roles add a single-item base as a further stateless convenience, for operations that apply independently to each item of a batch: `SingleCandidateMutator<...>`, `SingleCandidateCrossover<...>`, `SingleCandidateCreator<...>` and `SingleCandidateEvaluator<...>`. Implement the single-item method for one item; the inherited batch-wise role operation handles deterministic per-item random forks and batching. Callers holding the specialized base may invoke the single-item method directly, while ordinary role consumers continue to use the batch role operation. A role gets such a base only when independent per-item application is a genuine shape for it, so Selector, Replacer, Interceptor and Terminator have no equivalent.

The single-item method has its own name so it stays distinct from the batch operation it drives. The default is the role's verb followed by `Candidate`: `MutateCandidate`, `CreateCandidate` and `EvaluateCandidate`. `CrossParents` is the deliberate exception, because a crossover crosses one `Parents<TCandidate>` group into one candidate, so its single-item unit is a parent group rather than a candidate. A creator receives no input at all, so `CreateCandidate` takes only the random number generator and the context it needs; the requested count belongs to the batch operation.

A single-item base owns its batch operation and seals it. A subclass that replaced the batch operation would no longer be the single-item operator its type claims to be, and the single-item method it must still implement would become unreachable. Batching is a configuration decision instead: set the `Concurrency` property to run the per-item calls concurrently. Each item receives a random number generator forked from its batch position, so the result is identical for every concurrency setting and the choice is a performance decision rather than a semantic one. An operator that genuinely needs a different batch-wise operation derives from the stateless base directly, where the batch operation is the authoring surface.

`SingleCandidateMutator` is the reference implementation of this rule; `SingleCandidateCrossover`, `SingleCandidateCreator` and `SingleCandidateEvaluator` follow it.

## Choose an arity

Each role base is available at three arities. Reduced arities exist so an author who needs neither the problem nor the search space does not have to name them.

Interceptor and Terminator retain `TSearchState` in these three ordinary arities because their operations transform or inspect a produced search state. Terminator additionally provides `Terminator<TCandidate>`, `TerminatorInstance<TCandidate>` and matching stateless and stateful bases for termination conditions that inspect none of the operation inputs, such as elapsed time, cancellation and external operator budgets. Interceptor cannot remove `TSearchState` because it returns that type.

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

Reduction changes what the author declares, not where the operator fits. **The role contract names only the candidate.** `IMutator<Permutation>` is the whole contract, so the `SwapMutator` above fills any mutator slot over permutations, and so does an operator authored at the fullest rung against a specific problem.

That is what the ladder buys and it is worth being precise about who pays. The arity lives in the authoring bases, where it does work: the base performs the type check when a run supplies types the operator was not written for, and for a stateless operator the configuration *is* the executable part, so it needs the search space and problem in scope where it is written. A consumer, field or child property never names them. Earlier revisions of this page described the opposite arrangement, where the contracts carried the full triple and reduction was an authoring convenience only; that was reversed by the arity migration.

Wrapping and multi bases are agnostic in the search space and the problem, and take **one** type argument:

```csharp
public sealed record RetryingMutator<TCandidate>(IMutator<TCandidate> ChildMutator, int Attempts)
    : WrappingMutator<TCandidate>(ChildMutator)
{
    protected override IMutatorInstance<TCandidate, TRunSearchSpace, TRunProblem>
        WrapExecutionInstance<TRunSearchSpace, TRunProblem>(
            IMutatorInstance<TCandidate, TRunSearchSpace, TRunProblem> childMutator) =>
        new Instance<TRunSearchSpace, TRunProblem>(childMutator, Attempts);

    // Nested instance, generic in the run's types and constrained the same way.
}
```

Binding is a leaf concept, so a composite never names what its children were written for. The run's search space and problem arrive as *method* type arguments on `WrapExecutionInstance`, are threaded into a nested generic instance class, and the child is resolved against them. A problem-specific child composes in exactly as a universal one does, because the child slot is `IMutator<TCandidate>` either way.

The cost of that agnosticism falls on the composite's author, and only there: a generic method with a constraint clause and a nested generic instance class, instead of a plain nested class. Leaf authoring is unchanged, and every consumer of the composite names one type argument.

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

A stateful operator receives one fresh `TState` for each execution instance. The state can contain counters, caches and ordinary helper data structures.

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

`CreateInitialState()` must return a fresh object. State must not contain operator or algorithm configurations, execution instances, registries or delegates bound to child execution instances. Use the explicit path when an operator needs any of those execution graph dependencies.

Framework managed state does not have a disposal lifecycle. Do not put disposable resources there. Such ownership requires an explicitly authored execution instance and a defined lifecycle mechanism.

## Explicit execution instances

The configuration describes reusable parameters and graph structure. Its instance creation method resolves child configurations through the registry and creates an execution instance. The instance owns operation logic, resolved child instances and mutable execution data.

```csharp
private sealed record ForwardingEvaluator(IEvaluator<RealVector> Inner)
    : Evaluator<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>
{
    public override EvaluatorInstance<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) =>
        new Instance(instanceRegistry.Resolve<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>(Inner));

    private sealed class Instance(IEvaluatorInstance<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem> inner)
        : EvaluatorInstance<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>
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

Resolve ordinary child operators while creating the instance. Retain the registry only when runtime graph construction is an intentional part of the operator.

Return the most concrete accessible instance type that is useful to callers. A private nested instance is returned through its role specific instance base.

## Mutable run parameters

Some specialized operators expose a configured starting value and a mutable current value on their execution instance. Variable strength mutation is the current example:

- `IVariableStrengthMutator.MutationStrength` is immutable reusable configuration.
- `IVariableStrengthMutatorInstance.CurrentMutationStrength` is mutable execution instance data initialized from that configuration.
- `EvolutionStrategy` adapts only the current value on the resolved instance.

The same mutator configuration can therefore be reused for independent runs without one run changing another run's starting strength. This is a specialized contract, not a general convention that operator configuration properties become mutable on execution instances.

## Wrapping and multi operators

Wrapping and multi bases are shortcuts for common explicit execution topologies:

- A wrapping base resolves one child once.
- A multi base resolves several children once.

Each topology is represented by a matching configuration and execution-instance pair, such as `WrappingSelector<...>` with `WrappingSelectorInstance<...>`. Derived configurations and their nested instances use the corresponding pair consistently. The instance base provides canonical protected child storage and preserves the same topology at the type level, even when it does not currently add shared execution behavior. Required resolved children are constructor dependencies rather than optionally initialized properties.

These bases do not have separate stateless and stateful variants. Their purpose is already to coordinate an execution graph. Use the unprefixed role base when a wrapping or multi topology does not fit.

Reach for them when the children are identified by being children and nothing more. `PipelineMutator` and `ChooseOneMutator` apply their children interchangeably, so the inherited `ChildMutators` is the honest name for the slot, and they add no name of their own.

When a child plays a specific part in the operation, derive from the unprefixed role base instead and declare that child yourself:

```csharp
public record EliteSelector<TCandidate> : ISelector<TCandidate>
{
    public ISelector<TCandidate> SelectorForRemaining { get; init; }
    public int Elites { get; init; } = 1;

    public ISelectorInstance<TCandidate, TRunSearchSpace, TRunProblem>
        CreateExecutionInstance<TRunSearchSpace, TRunProblem>(ExecutionInstanceRegistry instanceRegistry)
        where TRunSearchSpace : class, ISearchSpace<TCandidate>
        where TRunProblem : class, IProblem<TCandidate, TRunSearchSpace> =>
        new Instance<TRunSearchSpace, TRunProblem>(
            instanceRegistry.Resolve<TCandidate, TRunSearchSpace, TRunProblem>(SelectorForRemaining), Elites);
    // ...
}
```

`EliteSelector` takes its elites first and asks `SelectorForRemaining` only for the places that are left, so calling that child `ChildSelector` would hide what it does. Wrapping it and adding a second, better-named property is not the fix. The two properties return the same object and a reader cannot tell which one to use. Declaring the child directly also lets the constructor fix how many children there are. `GenderSpecificSelector` needs exactly one `FemaleSelector` and one `MaleSelector`, which a multi base cannot express.

Like every composite, these name only the candidate. A child slot is `ISelector<TCandidate>`, and the run's search space and problem arrive as method type arguments on the creation method. See [Choose an arity](#choose-an-arity) for what that costs the author and why it costs the consumer nothing.

Composition helpers such as choosing one child, applying a transformation and running a pipeline are covered in [Operator composition](/guide/extending/operator-composition).

## Scaffolding roles and cross-cutting concerns

Coding agents are useful optional aids for the repetitive parts of operator authoring. Adding a concrete operator to an existing role is usually small work, because deriving from an existing authoring base is most of it. A new operator role is not: it brings a whole family with it, including configuration and execution-instance arity ladders, stateless and stateful bases, applicable single-item bases, wrapping and multi topologies, construction companions, tests and API usage specs. A new cross-cutting concern brings one role-specific adapter for every applicable role. That expansion is mechanical enough for an agent to draft and large enough to make agent assistance worthwhile.

Agent-produced code is normal contributor-owned source. Inspect and refactor it normally, review every public member as a deliberate API addition, and validate it with the compiler, analyzers, focused behavior tests and API usage specs. Point the agent at the current contracts and guidelines in addition to the closest accepted role family: all eight current roles follow the accepted topology, but their operation inputs, applicability and lifecycle semantics still differ.

An agent will not settle a concern's semantics for you. Implement and review the concern for one representative role first, then state its lifecycle and applicability independently of that role before expanding it. Similar signatures do not prove identical semantics: duration measurement records failed calls because it updates in `finally`, while counting updates only after a successful child call; candidate-count conveniences apply only when the role produces an appropriate candidate collection.

## Configuration and execution rules

- Keep configuration values unchanged during execution. Retained collection inputs are immutable snapshots.
- Keep randomness, the search space and the problem explicit in operation calls.
- Store mutable run data in framework managed state or an explicitly authored execution instance.
- Resolve the same configuration through one registry when sharing its execution instance is intentional.
- Use independent registries when independent execution instances are required.
- Do not assume stateful operation calls are serialized unless the owning execution path guarantees it.

## Roslyn analyzer guardrails

The `OperatorAuthoringAnalyzer` Roslyn analyzer assists with common mistakes across operator roles:

- `HLib0002` reports execution graph dependencies exposed through stateful operator state.
- `HLib0003` reports direct mutation of operator configuration members during operation logic.
- `HLib0004` reports a framework contract type bound as the stateful state type argument, which is usually a reduced-arity mix-up.

These diagnostics are guardrails, not a proof that every invariant is satisfied. They cannot reliably identify every indirect mutation through a referenced collection, helper object or delegate.

## Algorithms use the explicit path

Algorithms coordinate operators and execution flow, so they always use a configuration paired with an explicitly authored execution instance. See [Writing algorithms](/guide/extending/writing-algorithms) for the public authoring model.

## Related pages

- [Operators](/guide/fundamentals/operators)
- [Operator composition](/guide/extending/operator-composition)
- [Running algorithms](/guide/execution/running-algorithms)
- [Writing algorithms](/guide/extending/writing-algorithms)
