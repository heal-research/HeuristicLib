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

## Choosing a base class

The base classes in `src/HeuristicLib.Core/Operators` are authoring conveniences on top of the role interfaces.

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

3. **If the operator is stateless but needs custom batch logic, use `Stateless*`**
   - `Stateless*` is the special-case convenience layer built on `NoState`.

4. **If the operator needs mutable per-run memory, use the unprefixed role base**
   - Examples: `Creator`, `Mutator`, `Evaluator`, `Selector`, `Crossover`, `Replacer`, `Terminator`, `Interceptor`
   - Put configuration on the definition object.
   - Put mutable runtime data into `TExecutionState`.

5. **If the operator wraps exactly one inner operator of the same role, use `Wrapping*<..., TExecutionState>`**
   - Examples: `WrappingEvaluator`, `WrappingMutator`, `WrappingSelector`, ...
   - The base resolves the inner execution instance once and passes it to your implementation as a delegate.
   - Store that instance in `TExecutionState`.

6. **If the operator combines several inner operators of the same role, use `Multi*<..., TExecutionState>`**
   - Examples: `MultiMutator`, `MultiCrossover`, `MultiTerminator`, ...
   - The base resolves the inner execution instances once and passes them to your implementation as delegates.
   - Store those instances in `TExecutionState`.

7. **If none of the convenience bases fit, implement the operator contract directly**
   - This is the fallback when you need full control over instancing or execution behavior.
   - If you do that, you also need to handle the definition/execution-instance split correctly. See [Definition vs execution instances](execution-instances.md).

## Short version

- plain stateless batch operator -> `Stateless*`
- plain stateless per-item operator -> `SingleSolution*`
- operator with mutable execution state -> `Creator` / `Mutator` / `Evaluator` / ...
- operator that wraps one operator -> `Wrapping*<..., TExecutionState>`
- operator that coordinates several operators -> `Multi*<..., TExecutionState>`
- full custom behavior -> implement the contract directly and handle execution instances yourself

## Composition helpers

HeuristicLib includes a few small composition patterns that keep calling code clean:

- `mutator.WithRate(mutationRate)` wraps a mutator with a no-op mutator to achieve a per-offspring mutation probability
- `ChooseOne*` helpers choose among several operators using weights
- `Pipeline*` helpers apply several operators in sequence

## Next

- [Algorithm](algorithm.md)
- [Execution model](execution-model.md)
