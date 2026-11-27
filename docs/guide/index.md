# Introduction

HeuristicLib is a .NET library for building, running and studying heuristic optimization algorithms. It offers ready algorithms for common searches and typed building blocks for custom ones.

Use it when the answer is one of many candidates and checking a candidate is easier than deriving the best one. Typical tasks are scheduling, routing, parameter tuning and symbolic regression.

## The model in one sentence

An **algorithm** uses **operators** to explore the **search space** of a **problem** and yields **search states** that show its progress.

| Concept                 | Question it answers                      | Example                                             |
| ----------------------- | ---------------------------------------- | --------------------------------------------------- |
| Candidate               | What does one possible answer look like? | A vector of four real numbers                       |
| Search space            | Which candidates are valid?              | Four values between `-5.12` and `5.12`              |
| Problem                 | How is a candidate evaluated?            | Calculate the Rastrigin function                    |
| Objective               | Which result is better?                  | A smaller function value                            |
| Algorithm and operators | How do we explore?                       | Genetic algorithm with crossover and mutation       |
| Search state            | What has happened so far?                | Population and objective values after generation 12 |

<ModelDiagram>

The parts stay separate. Compare algorithms on one problem, swap a mutation operator or repeat a configuration with other seeds. Each call starts a fresh run.

</ModelDiagram>

## Configurations and executors

The library works in two layers.

A **configuration** is immutable data that describes an algorithm and its operators. It holds settings such as a population size and child configurations such as the mutator of a genetic algorithm. It holds no executor and no result, so you can copy and reuse it freely. Changing a setting creates a modified copy. A configuration of plain settings, such as a `GaussianMutator`, compares by value and can be stored as data. A `Counter` compares by reference, and a configuration that holds a delegate cannot be persisted.

An **executor** is built from a configuration for one run and holds all runtime state, such as counters and caches. The library builds executors for you, and each run builds its own. No library state carries over between runs. State that your own delegate captures is yours to manage.

A **run** owns one execution, its executors and its **run results**, such as an analyzer's trace or a counter's count.

[Configurations](/guide/fundamentals/configurations) explains how to read, rewrite and share the configuration graph that an algorithm forms with its operators.

## The parts of a run

### Candidate and search space

A candidate is one proposed solution. Its type represents the shape of the answer, such as `RealVector`, `IntegerVector`, `BoolVector` or `Permutation`.

The search space owns validity rules for that type. A `BoundedRealVectorSearchSpace` can specify length and bounds. Operators use that information when creating or modifying candidates. Read [Search spaces](/guide/fundamentals/search-spaces) for the built-in choices.

### Problem and objective

A problem combines the search space, the evaluation of a candidate and the objective.

Evaluation produces an objective vector, even for a single objective. The objective says whether lower or higher values are better and how results compare. Read [Problems](/guide/fundamentals/problems) and [Objectives and candidates](/guide/fundamentals/objectives).

### Algorithm and operators

An algorithm owns the search process and its stopping budget. Operators perform focused steps within that process. A genetic algorithm usually needs operators to create candidates, select parents, cross parents and mutate offspring.

So you can change the variation without writing another generation loop. Type parameters and validation catch incompatible combinations before a run. Read [Algorithms](/guide/fundamentals/algorithms) and [Operators](/guide/fundamentals/operators).

### Search states

Algorithms yield immutable snapshots of useful progress. A population based state contains evaluated candidates. Iteration counts come from observations or counters rather than from the state.

You can consume every state for progress reporting and analysis or ask only for the final state. Read [Search states](/guide/execution/search-states) and [Algorithm runs](/guide/execution/running-algorithms).

### Randomness is an input

Stochastic algorithms receive an explicit random number generator. A recorded seed is part of an experiment configuration, just like the population size or mutation rate. It makes a surprising run reproducible. Read [Reproducible randomness](/guide/execution/randomness).

## Important guarantees

- Search spaces define candidate validity.
- Problems own evaluation and objective semantics.
- Evaluated candidates keep a candidate together with its objective vector.
- A configuration never changes. State the library creates for a run lives in the executors and run results of that run.
- Each call to `RunStreaming` or `RunToCompletionAsync` starts a fresh run.
- The same configuration and seed are intended to produce the same stochastic sequence.
- One run is evidence, not a reliable comparison of stochastic algorithms.

## What is included

- Population based algorithms including genetic algorithms, evolution strategies and NSGA-II
- Local search algorithms including hill climbing
- Search spaces for real, integer, Boolean and permutation vectors
- Reusable creation, crossover, mutation and selection operators
- Single objective and multiobjective evaluation
- Repeatable experiments, parameter grids and analysis helpers
- Specialized support for machine learning, symbolic regression and symbolic expressions
- Source based Python integration through pythonnet

HeuristicLib targets .NET 10. The package is currently prerelease software, so expect APIs to evolve between versions.

## Choose your path

For ordinary use, configure an algorithm and call `RunToCompletion` or `RunToCompletionAsync` with a problem and a random source. Start with [Build your first optimizer](/guide/getting-started).

| Your goal                                              | Start here                                                                                                                                                             | Learn when needed                                           |
| ------------------------------------------------------ | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ----------------------------------------------------------- |
| Practitioner: solve a problem with existing components | [First optimizer](/guide/getting-started) and [Algorithms](/guide/fundamentals/algorithms)                                                                             | Compatible operators and stopping budgets                   |
| Experimenter: compose and compare configurations       | [Configurations](/guide/fundamentals/configurations), [Operator composition](/guide/fundamentals/operator-composition) and [Experiments](/guide/execution/experiments) | Repetitions, parameter grids and trial analysis             |
| Analysis: measure progress or compare runs             | [Analysis of algorithm runs](/guide/execution/observability-and-analysis) and [Experiments](/guide/execution/experiments)                                              | Quality curves, operator measurements and trial comparisons |
| Integrator: control execution in an application        | [Algorithm runs](/guide/execution/running-algorithms)                                                                                                                  | Streaming, cancellation and explicit runs                   |
| Component author: add a problem, operator or algorithm | [Problems](/guide/fundamentals/problems) and [Extend HeuristicLib](/extend/)                                                                                           | The authoring contract for that component                   |

Custom components run through the same entry points as built-in ones.

## A typical workflow

1. Describe valid candidates with a search space.
2. Define how the problem evaluates a candidate.
3. Choose an algorithm and compatible operators.
4. Run with an explicit random seed.
5. Inspect the final result or stream intermediate states.
6. Repeat runs to compare configurations rather than trusting one stochastic result.

[Build your first optimizer](/guide/getting-started) puts these steps into a complete run. [Problems](/guide/fundamentals/problems) then walks through the parts. The [glossary](/reference/glossary) defines the terms.
