# Introduction

This documentation explains **how to think about HeuristicLib**: the core concepts, the execution model, and the architectural decisions that shape the library.

It is **not** an API reference. The generated API docs are the place for type-by-type documentation.

## What HeuristicLib is

HeuristicLib is a library for building and running heuristic optimization algorithms with a small set of composable concepts.

The guiding idea is a single, repeatable loop:

> An **algorithm** iteratively transforms an **algorithm state** while operating on **genotypes** from a **search space**, evaluated by a **problem** under an **objective**.

If you’re new, the canonical story is the **Genetic Algorithm**.

## Where to start

1. [Getting started](getting-started.md) — the shortest path to running a `GeneticAlgorithm`.
2. [Core concepts](core-concepts.md) — the vocabulary and how the pieces fit.
3. [Execution model](execution-model.md) — batch vs streaming, termination, interception, and observation.

## Glossary (short)

- **Genotype**: the candidate representation an algorithm and its operators work on.
- **Search space**: defines which genotypes are valid. See: [Search spaces](search-space.md).
- **Problem**: evaluates genotypes and defines the objective. See: [Problem](problem.md).
- **Objective**: directions (min/max), dimensionality, and ordering of objective vectors. See: [Objectives & solutions](objectives-and-solutions.md).
- **Operators**: reusable building blocks algorithms compose (creator, selector, crossover, mutator, replacer, …). See: [Operators](operators.md).
- **Algorithm state**: the evolving value produced by each iteration. See: [Algorithm state](algorithm-state.md).

## About legacy notes

An older version of `introduction.md` contained historical design notes that are no longer guaranteed to match the current codebase.

Those notes are preserved here for reference:

- [Legacy introduction notes (archived)](legacy-introduction-notes.md)

> [!NOTE]
> The archived notes exist to preserve design history. The rest of this documentation describes the current code and contracts.
