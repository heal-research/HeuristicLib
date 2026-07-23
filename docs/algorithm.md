# Algorithm

An algorithm configuration is a reusable description of a search process. An algorithm execution instance performs that process and produces a stream of public search states.

Algorithms use one authoring path: a configuration paired with an explicitly authored execution instance. HeuristicLib does not provide stateless or framework managed state variants for algorithms because algorithms normally coordinate child operators and own execution flow.

## Authoring an iterative algorithm

Derive the reusable configuration from `IterativeAlgorithm<TCandidate, TSearchSpace, TProblem, TSearchState>`. Put settings and child operator configurations on that type.

Create a nested execution instance derived from `IterativeAlgorithmInstance<TCandidate, TSearchSpace, TProblem, TSearchState>`. Resolve child operators eagerly in `CreateIterativeAlgorithmInstance(...)` then pass them into the instance. Mutable counters, caches and other run scoped data belong on the instance.

```csharp
public sealed record MyAlgorithm<TCandidate, TSearchSpace, TProblem>
    : IterativeAlgorithm<TCandidate, TSearchSpace, TProblem, SingleSolutionState<TCandidate>>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    public required ICreator<TCandidate, TSearchSpace, TProblem> Creator { get; init; }
    public IEvaluator<TCandidate, TSearchSpace, TProblem> Evaluator { get; init; } =
        new ProblemEvaluator<TCandidate, TSearchSpace, TProblem>();

    protected override IterativeAlgorithmInstance<TCandidate, TSearchSpace, TProblem, SingleSolutionState<TCandidate>> CreateIterativeAlgorithmInstance(
        ExecutionInstanceRegistry registry,
        IInterceptorInstance<TCandidate, TSearchSpace, TProblem, SingleSolutionState<TCandidate>>? resolvedInterceptor) =>
        new Instance(resolvedInterceptor, registry.Resolve(Creator), registry.Resolve(Evaluator));

    private sealed class Instance(
        IInterceptorInstance<TCandidate, TSearchSpace, TProblem, SingleSolutionState<TCandidate>>? interceptor,
        ICreatorInstance<TCandidate, TSearchSpace, TProblem> creator,
        IEvaluatorInstance<TCandidate, TSearchSpace, TProblem> evaluator)
        : IterativeAlgorithmInstance<TCandidate, TSearchSpace, TProblem, SingleSolutionState<TCandidate>>(interceptor)
    {
        private int producedStates;

        protected override SingleSolutionState<TCandidate> ExecuteStep(SingleSolutionState<TCandidate>? previousState, TProblem problem, IRandomNumberGenerator random)
        {
            producedStates++;
            var candidate = creator.Create(1, random, problem.SearchSpace, problem)[0];
            var evaluatedCandidate = evaluator.Evaluate([candidate], random, problem.SearchSpace, problem)[0];
            return new SingleSolutionState<TCandidate> { Population = Population.From([evaluatedCandidate]) };
        }
    }
}
```

The `resolvedInterceptor` parameter is the interceptor instance already resolved by the iterative base. The registry remains available for dependencies owned by the concrete algorithm.

The instance creation method returns the most concrete accessible instance type that is useful to callers. A private nested instance is returned through `IterativeAlgorithmInstance<...>`.

## Ownership rules

The configuration owns reusable settings and child configurations. It must not be mutated during execution. Referenced child configurations and behavior affecting collections must also remain unchanged.

The execution instance owns resolved child instances, mutable execution data and execution behavior. Repeated runs create independent execution graphs so their instance data is independent.

The search state is different. It is the public progress value yielded by the algorithm. Private execution data should not be placed in the search state merely to make it available to the next step.

## Why instance creation receives the registry

Algorithm instance creation methods receive the full `ExecutionInstanceRegistry`. Ordinary algorithms use it to resolve child operators once. Meta algorithms and execution wrappers also need it to create child registries, install replacements or control child execution instance reuse.

Resolution remains local and eager for ordinary algorithms. Do not retain the registry merely to resolve ordinary child operators later during step execution. A meta algorithm that creates child algorithm instances during execution may pass its originating registry to its execution instance. It can directly create each algorithm instance through a child registry so parent operator instances and replacement policy remain available.

## Iterative lifecycle

`IterativeAlgorithmInstance<...>` owns the common stream lifecycle. It checks internal completion and cancellation, forks randomness by yielded state count, calls `TryExecuteStep(...)`, applies the resolved interceptor, evaluates terminal state logic, yields the state and then schedules the next iteration with `Task.Yield()`.

`RunStreamingAsync(...)` is sealed on this base so a concrete iterative algorithm cannot accidentally bypass that ordering. Override `HasCompleted(...)`, `ExecuteStep(...)`, `TryExecuteStep(...)` or `IsTerminalState(...)` as required by the algorithm.

## Noniterative algorithms

Derive from `Algorithm<TCandidate, TSearchSpace, TProblem, TSearchState>` when the iterative lifecycle is not appropriate. Implement `CreateAlgorithmInstance(...)` and return an `AlgorithmInstance<...>` that owns the complete streaming behavior.


## Related pages

* [Search state](algorithm-state.md)
* [Execution model](execution-model.md)
* [Configuration and execution instances](execution-instances.md)
* [Operators](operators.md)
