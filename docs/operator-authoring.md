# Operator authoring

Operator configurations are reusable descriptions. Operator execution instances perform the work and own any run scoped data. HeuristicLib provides three authoring paths so an operator can use the smallest execution model that fits its responsibilities.

## Choose an authoring path

| Path                        | Typical base                     | Use when                                                                                     |
| --------------------------- | -------------------------------- | -------------------------------------------------------------------------------------------- |
| Stateless                   | `StatelessEvaluator<...>`        | The operation needs configuration values but no mutable execution data                       |
| Stateful                    | `StatefulEvaluator<..., TState>` | The operation needs framework managed execution data but no execution graph dependencies     |
| Explicit execution instance | `Evaluator<...>`                 | The operator resolves child operators or needs complete control over its execution structure |

The same pattern exists for creators, crossovers, mutators, selectors, replacers, interceptors and terminators.

The unprefixed role base is the common base of the three paths. Stateless and stateful bases derive from it. Derive directly from the unprefixed base when authoring an explicit execution instance.

## Stateless operators

A stateless operator configuration also implements its operation logic. Its configuration values and referenced collections must remain unchanged during execution.

```csharp
private sealed record FirstValueEvaluator
    : StatelessEvaluator<RealVector, RealVectorSearchSpace, TestFunctionProblem>
{
    public override IReadOnlyList<ObjectiveVector> Evaluate(
        IReadOnlyList<RealVector> candidates,
        IRandomNumberGenerator random,
        RealVectorSearchSpace searchSpace,
        TestFunctionProblem problem) =>
        candidates.Select(candidate => new ObjectiveVector(candidate[0])).ToArray();
}
```

Use specialized helpers such as `SingleSolutionEvaluator` when the role offers one that exactly matches the operation shape.

## Stateful operators

A stateful operator receives one fresh `TState` for each execution instance. The state can contain counters, caches and ordinary helper data structures.

```csharp
private sealed record CountingEvaluator
    : StatefulEvaluator<RealVector, RealVectorSearchSpace, TestFunctionProblem, CountingEvaluator.ExecutionState>
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
        RealVectorSearchSpace searchSpace,
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
private sealed record ForwardingEvaluator(
    IEvaluator<RealVector, RealVectorSearchSpace, TestFunctionProblem> Inner)
    : Evaluator<RealVector, RealVectorSearchSpace, TestFunctionProblem>
{
    protected override EvaluatorInstance<RealVector, RealVectorSearchSpace, TestFunctionProblem> CreateEvaluatorInstance(ExecutionInstanceRegistry registry) =>
        new Instance(registry.Resolve(Inner));

    private sealed class Instance(IEvaluatorInstance<RealVector, RealVectorSearchSpace, TestFunctionProblem> inner)
        : EvaluatorInstance<RealVector, RealVectorSearchSpace, TestFunctionProblem>
    {
        public override IReadOnlyList<ObjectiveVector> Evaluate(
            IReadOnlyList<RealVector> candidates,
            IRandomNumberGenerator random,
            RealVectorSearchSpace searchSpace,
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

These bases do not have separate stateless and stateful variants. Their purpose is already to coordinate an execution graph. Use the unprefixed role base when a wrapping or multi topology does not fit.

Composition helpers such as choosing one child, applying a transformation and running a pipeline are covered in [Operator composition](operator-composition.md).

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

These diagnostics are guardrails, not a proof that every invariant is satisfied. They cannot reliably identify every indirect mutation through a referenced collection, helper object or delegate.

## Algorithms use the explicit path

Algorithms coordinate operators and execution flow, so they always use a configuration paired with an explicitly authored execution instance. See [Algorithm](algorithm.md) for the corresponding algorithm authoring model.

## Related pages

- [Operators](operators.md)
- [Operator composition](operator-composition.md)
- [Configuration and execution instances](execution-instances.md)
- [Algorithm](algorithm.md)
