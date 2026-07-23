# Core concepts

This section defines the vocabulary used throughout the library.

The goal is a single dominant mental model:

> An **algorithm** produces a stream of **search states** while operating on **candidates** from a **search space**, evaluated by a **problem** into an **objective vector** interpreted by **objective directions**.

## The contracts at a glance

| Concept              | What it is                                              | Where it lives                                                                                              |
| -------------------- | ------------------------------------------------------- | ----------------------------------------------------------------------------------------------------------- |
| Candidate            | The algorithm-facing object being searched              | Generic type parameter `TCandidate`                                                                         |
| Search space         | Validity predicate for candidates                       | `ISearchSpace<TCandidate>`                                                                                  |
| Objective directions | Minimize/maximize direction per objective               | `ObjectiveDirections`                                                                                       |
| Objective vector     | The measured outcome of evaluation                      | `ObjectiveVector`                                                                                           |
| Evaluated candidate  | Candidate + objective vector                            | `EvaluatedCandidate<TCandidate>`                                                                            |
| Problem              | Owns search space, evaluation, and objective directions | `IProblem<TCandidate, TSearchSpace>`                                                                        |
| Search state         | The public progress value produced by algorithms        | `ISearchState`                                                                                              |
| Algorithm loop       | The step based algorithm authoring model                 | `IterativeAlgorithmInstance<...>`                                                                           |
| Execution instance   | Run scoped behavior, resolved dependencies and mutable data | `AlgorithmInstance<...>` or an operator instance contract                                                |
| Operators            | Pluggable building blocks used by algorithms            | `ICreator`, `IEvaluator`, `ISelector`, `ICrossover`, `IMutator`, `IReplacer`, `ITerminator`, `IInterceptor` |

## How the types fit together

Most public abstractions follow a consistent generic pattern:

- `TCandidate` is the candidate representation.
- `TSearchSpace : ISearchSpace<TCandidate>` describes which candidate values are valid.
- `TProblem : IProblem<TCandidate, TSearchSpace>` evaluates candidates and defines objective directions.
- `TSearchState : ISearchState` is the public streamed state produced by the algorithm.

This is deliberate: once you’ve understood one family of types, the rest of the library reads predictably.

## A simple mental picture

During execution, the loop looks like this:

1. The algorithm configuration resolves child execution instances and creates an algorithm execution instance.
2. `ExecuteStep(previousState, problem, random)` on the iterative execution instance produces the next public state.
3. Optional: `Interceptor.Transform(newState, previousState, ...)` post processes the state.
4. Streaming continues until the algorithm completes or an external termination wrapper stops it.

The public search state is the “unit of progress”: it is what streaming execution yields, and it’s what termination and interception reason about.

The algorithm execution instance owns resolved child instances, private execution data and the execution behavior. Configurations remain reusable and unchanged during execution.

## Where to dive deeper

- [Problem](problem.md)
- [Search spaces](search-space.md)
- [Objective vectors and evaluated candidates](objective-vectors-and-evaluated-candidates.md)
- [Operators](operators.md)
- [Algorithm](algorithm.md)
- [Search state](algorithm-state.md)
- [Execution model](execution-model.md)
