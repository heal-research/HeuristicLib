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
- If you parallelize internally, fork deterministic child generators (see [Randomness (RNG) design](randomness.md)).

## Common composition helpers

HeuristicLib includes a few small composition patterns that keep calling code clean:

- `mutator.WithRate(mutationRate)` wraps a mutator with a no-op mutator to achieve a per-offspring mutation probability.
- `ChooseOne*` helpers (for example, `ChooseOneMutator` and `ChooseOneCrossover`) choose among several operators using weights.
- `Pipeline*` helpers (for example, `PipelineMutator` and `PipelineInterceptor`) apply several operators in sequence.

## Direct invocation for stateless operators

Concrete stateless operators should usually expose a public static method using the role verb so callers can use the operator logic directly without instantiating an operator object.

- creators -> `Create(...)`
- mutators -> `Mutate(...)`
- crossovers -> `Cross(...)`
- evaluators -> `Evaluate(...)`
- selectors -> `Select(...)`
- replacers -> `Replace(...)`
- terminators -> `ShouldTerminate(...)`
- interceptors -> `Transform(...)`

When the operator has instance configuration, make that configuration explicit in the static method signature. When an identical static signature would collide with the instance method, use either a companion static helper type or a nearby overload that exposes the required parameters directly.

## Choosing a base class when implementing an operator

The base classes in `src/HeuristicLib.Core/Operators` are mainly **authoring conveniences**.
They do not introduce fundamentally different operator concepts; they package common implementation patterns on top of the role interfaces (`ICreator`, `IMutator`, `IEvaluator`, ...).

Use this checklist:

1. **First choose the operator role**
   - creation -> `ICreator`
   - evaluation -> `IEvaluator`
   - selection -> `ISelector`
   - variation of existing genotypes -> `IMutator` / `ICrossover`
   - survivor selection -> `IReplacer`
   - stopping rule -> `ITerminator`
   - state post-processing -> `IInterceptor`

2. **If the operator is stateless and naturally processes one item at a time, prefer `SingleSolution*`**
   - Examples: `SingleSolutionCreator`, `SingleSolutionMutator`, `SingleSolutionCrossover`, `SingleSolutionEvaluator`
   - Use this when the batch implementation is just “apply the same logic independently to each element”.
   - These bases already provide the batch loop and RNG splitting behavior.

3. **If the operator is stateless but needs custom batch logic, use `Stateless*`**
   - Examples: `StatelessSelector`, `StatelessReplacer`, `StatelessTerminator`, `StatelessInterceptor`
   - Also use this for creators/mutators/crossovers/evaluators when you want to optimize across the whole batch instead of implementing per-item logic.

4. **If the operator needs mutable per-run memory, use `Stateful*`**
   - Examples: `StatefulTerminator`, `StatefulEvaluator`, `StatefulMutator`, ...
   - Put configuration on the definition object.
   - Put mutable runtime data into the nested `State` that becomes part of the execution instance.
   - See [Definition vs execution instances](execution-instances.md) for the mental model.

5. **If the operator wraps exactly one inner operator of the same role, use `Decorator*`**
   - Examples: `WrappingEvaluator`, `WrappingMutator`, `WrappingSelector`, ...
   - Typical use cases: caching, limiting, adding elites before delegating, prepending predefined solutions.
   - If the wrapper does **not** need its own extra execution state, prefer the lower-arity `Decorator*<...>` base.
   - If it does need mutable per-run wrapper state, use the `Decorator*<..., TState>` form.

6. **If the operator combines several inner operators of the same role, use `Composite*`**
   - Examples: `MultiMutator`, `MultiCrossover`, `MultiTerminator`, ...
   - Typical use cases: pipelines, weighted choice among operators, AND/OR combinations of terminators.
   - If the wrapper does **not** need its own extra execution state, prefer the lower-arity `Composite*<...>` base.
   - If it does need mutable per-run wrapper state, use the `Composite*<..., TState>` form.

7. **If none of the convenience bases fit, implement the operator contract directly**
   - This is the fallback when you need full control over instancing or execution behavior.
   - At the lowest level, that means implementing `IOperator<TExecutionInstance>` yourself.
   - If you do that, you also need to handle the execution-instance split correctly. See [Definition vs execution instances](execution-instances.md).

### Short version

- plain stateless batch operator -> `Stateless*`
- plain stateless per-item operator -> `SingleSolution*`
- operator with mutable execution state -> `Stateful*`
- operator that wraps one operator -> `Decorator*` (`Decorator*<...>` without extra state, `Decorator*<..., TState>` with extra state)
- operator that coordinates several operators -> `Composite*` (`Composite*<...>` without extra state, `Composite*<..., TState>` with extra state)
- full custom behavior -> implement the contract directly and handle execution instances yourself

## Next

- [Algorithm](algorithm.md)
- [Execution model](execution-model.md)
