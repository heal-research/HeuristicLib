# Core concepts

This section defines the vocabulary used throughout the library.

The goal is a single dominant mental model:

> An **algorithm** produces a stream of **search states** while operating on **genotypes** from a **search space**, evaluated by a **problem** under an **objective**.

## The contracts at a glance

| Concept | What it is | Where it lives |
|---|---|---|
| Genotype | Your candidate representation | Generic type parameter `TGenotype` (usually a class/record) |
| Search space | Validity predicate for genotypes | `ISearchSpace<TGenotype>` |
| Objective | Defines direction(s) and ordering | `Objective`, `ObjectiveDirection` |
| Objective vector | The measured outcome of evaluation | `ObjectiveVector` |
| Solution | Genotype + objective vector | `ISolution<TGenotype>` |
| Problem | Owns objective + search space + evaluation | `IProblem<TGenotype, TSearchSpace>` |
| Search state | The public progress value produced by algorithms | `ISearchState` |
| Algorithm loop | The step-based algorithm authoring model | `IterativeAlgorithm<...>` |
| Execution state | Hidden per-run mutable execution state | `TExecutionState` on `IterativeAlgorithm<...>` |
| Operators | Pluggable building blocks used by algorithms | `ICreator`, `IEvaluator`, `ISelector`, `ICrossover`, `IMutator`, `IReplacer`, `ITerminator`, `IInterceptor` |

## How the types fit together

Most public abstractions follow a consistent generic pattern:

- `TGenotype` is the candidate representation.
- `TSearchSpace : ISearchSpace<TGenotype>` describes which genotypes are valid.
- `TProblem : IProblem<TGenotype, TSearchSpace>` evaluates genotypes and defines the objective.
- `TSearchState : ISearchState` is the public streamed state produced by the algorithm.

This is deliberate: once you’ve understood one family of types, the rest of the library reads predictably.

## A simple mental picture

At runtime, the loop looks like this:

1. `CreateInitialExecutionState(resolver)` resolves dependencies and prepares per-run mutable state.
2. `ExecuteStep(previousState, executionState, problem, random)` produces the next public state.
3. Optional: `Interceptor.Transform(newState, previousState, ...)` post-processes the state.
4. Streaming continues until the algorithm completes or an external termination wrapper stops it.

The public search state is the “unit of progress”: it is what streaming execution yields, and it’s what termination and interception reason about.

The execution state is the hidden carrier for resolved execution instances and per-run mutable data.

## Where to dive deeper

- [Problem](problem.md)
- [Search spaces](search-space.md)
- [Objectives & solutions](objectives-and-solutions.md)
- [Operators](operators.md)
- [Algorithm](algorithm.md)
- [Search state](algorithm-state.md)
- [Execution model](execution-model.md)
