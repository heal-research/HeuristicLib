# Operators

Operators are reusable building blocks that algorithms compose.

The design intent is that many algorithm variants can be expressed by **swapping operator implementations** while keeping the surrounding algorithm structure intact.

## Operator taxonomy

The core roles used across algorithms in this repository are:

- **Creator** (`ICreator`): creates initial genotypes.
- **Evaluator** (`IEvaluator`): evaluates genotypes to objective vectors.
- **Selector** (`ISelector`): selects solutions (usually parents) from a population.
- **Crossover** (`ICrossover`): combines parent genotypes into offspring genotypes.
- **Mutator** (`IMutator`): perturbs genotypes to create variation.
- **Replacer** (`IReplacer`): decides how to form the next population.
- **Terminator** (`ITerminator`): decides whether another iteration should run.
- **Interceptor** (`IInterceptor`): transforms the produced iteration state.

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
> - Operators assume their input genotypes are already within the given search space. Passing out-of-space inputs is considered a usage error and may throw.
> - Operators guarantee that any genotypes they return are within the given search space.
>
> In other words: algorithms typically do not validate or repair genotypes automatically; operators are the enforcement boundary.

## A note on parallelism and reproducibility

Several operator base classes use internal parallelization helpers (for example, creators and mutators operate on batches).

To keep results reproducible:

- Do not use `System.Random` directly inside operators.
- Always take randomness from the provided `IRandomNumberGenerator`.
- If you parallelize internally, prefer the library’s RNG spawning (`random.Spawn(...)`) pattern.

## Common composition helpers

HeuristicLib includes a few small composition patterns that keep calling code clean:

- `mutator.WithRate(mutationRate)` wraps a mutator with a no-op mutator to achieve a per-offspring mutation probability.
- “Multi operators” (for example, `MultiMutator`) choose among several operators using weights.

## Interception vs observation

These are two different hooks with different responsibilities:

- **Interceptor**: part of the algorithm pipeline (it returns a possibly modified state).
- **Observer**: side effects only (logging, metrics, diagnostics).

See [Execution model](execution-model.md) for the current wiring guidance.

## Next

- [Algorithm](algorithm.md)
- [Execution model](execution-model.md)
