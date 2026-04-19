# Algorithm

An algorithm drives the optimization process by producing a stream of algorithm states.

For normal authoring, HeuristicLib now promotes a simple model:

- derive from `IterativeAlgorithm<TGenotype, TSearchSpace, TProblem, TSearchState>` if the algorithm has no meaningful custom runtime state
- derive from `StatefulIterativeAlgorithm<TGenotype, TSearchSpace, TProblem, TSearchState, TRuntimeState>` if it does
- implement the step logic on the algorithm type itself
- call operators through `IOperatorExecutor`

That is the main authoring story users should learn first.

## Authoring bases

### `IterativeAlgorithm<...>`

Use this when the algorithm can be written as:

```csharp
protected override TSearchState ExecuteStep(
  TSearchState? previousState,
  IOperatorExecutor executor,
  TProblem problem,
  IRandomNumberGenerator random)
```

This is the common case for algorithms such as:

- `HillClimber<...>`
- `GeneticAlgorithm<...>`
- `NSGA2<...>`
- `EvolutionStrategy<...>`
- `OpenEndedRelevantAllelesPreservingGeneticAlgorithm<...>`
- `AlpsGeneticAlgorithm<...>`

### `StatefulIterativeAlgorithm<...>`

Use this when the algorithm needs hidden per-run state:

```csharp
protected override TRuntimeState CreateInitialRuntimeState();

protected override TSearchState ExecuteStep(
  TSearchState? previousState,
  TRuntimeState runtimeState,
  IOperatorExecutor executor,
  TProblem problem,
  IRandomNumberGenerator random)
```

`TRuntimeState` is internal algorithm execution state. It is not the streamed public search state.

## `IOperatorExecutor`

Algorithms call operators through `IOperatorExecutor` rather than resolving execution instances manually.

Examples:

```csharp
var genotypes = executor.Create(Creator, PopulationSize, random, problem.SearchSpace, problem);
var fitnesses = executor.Evaluate(Evaluator, genotypes, random, problem.SearchSpace, problem);
var parents = executor.Select(Selector, population, problem.Objective, count, random, problem.SearchSpace, problem);
```

This keeps algorithm authoring focused on the optimization logic instead of execution-instance plumbing.

## Evaluator and interceptor

`Algorithm<...>` provides the explicit `Evaluator`.

`StatefulIterativeAlgorithm<...>` additionally supports an optional `Interceptor` that transforms the produced state after each step.

The evaluator is part of the core algorithm story.
The interceptor is currently a practical execution hook and may evolve later when the analyzer/observation system is revisited.

## Internal runtime machinery

The library still uses execution instances internally.

That matters for:

- run-local state
- shared runtime graph resolution
- meta-algorithm behavior

But for normal algorithm authoring, execution instances are infrastructure, not the intended extension model.

If a very advanced algorithm really needs full manual control, it can still implement `IAlgorithm<...>` directly.

## Related pages

- [Algorithm state](algorithm-state.md)
- [Execution model](execution-model.md)
- [Definition vs execution instances](execution-instances.md)
- [Operators](operators.md)
