# Core concepts

This section defines the vocabulary used throughout the library.

The goal is a single dominant mental model:

> An **algorithm** iteratively transforms an **algorithm state** while operating on **genotypes** from a **search space**, evaluated by a **problem** under an **objective**.

## The contracts at a glance

| Concept | What it is | Where it lives |
|---|---|---|
| Genotype | Your candidate representation | Generic type parameter `TGenotype` (usually a class/record)
| Search space | Validity predicate for genotypes | `ISearchSpace<TGenotype>`
| Objective | Defines direction(s) and ordering | `Objective`, `ObjectiveDirection`
| Objective vector | The measured outcome of evaluation | `ObjectiveVector`
| Solution | Genotype + objective vector | `ISolution<TGenotype>`
| Problem | Owns objective + search space + evaluation | `IProblem<TGenotype, TSearchSpace>`
| Algorithm state | The iteration boundary value produced by algorithms | `IAlgorithmState`
| Algorithm loop | The “one-step” engine + termination | `IIterable<TG, TS, TP, TR>`
| Operators | Pluggable building blocks used by algorithms | `ICreator`, `IEvaluator`, `ISelector`, `ICrossover`, `IMutator`, `IReplacer`, `ITerminator`, `IInterceptor`

## How the types fit together

Most public abstractions follow a consistent generic pattern:

- `TGenotype` is the candidate representation.
- `TSearchSpace : ISearchSpace<TGenotype>` describes which genotypes are valid.
- `TProblem : IProblem<TGenotype, TSearchSpace>` evaluates genotypes and defines the objective.
- `TAlgorithmState : IAlgorithmState` is the per-iteration state produced by the algorithm.

This is deliberate: once you’ve understood one family of types, the rest of the library reads predictably.

## A simple mental picture

At runtime, the loop looks like this:

1. `ExecuteStep(problem, previousState, random)` produces the next state.
2. Optional: `Interceptor.Transform(newState, previousState, ...)` post-processes the state.
3. `Terminator.ShouldContinue(newState, previousState, ...)` decides whether to run another step.

The state itself is the “unit of progress”: it is what streaming execution yields, and it’s what termination and interception reason about.

## Where to dive deeper

- [Problem](problem.md)
- [Search spaces](search-space.md)
- [Objectives & solutions](objectives-and-solutions.md)
- [Operators](operators.md)
- [Algorithm](algorithm.md)
- [Algorithm state](algorithm-state.md)
- [Execution model](execution-model.md)
