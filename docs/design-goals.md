# Design goals

This page captures the guiding principles behind HeuristicLib’s architecture.

## Library-first

HeuristicLib is designed as a library:

- No GUI assumptions.
- The caller controls execution and integration.
- Dependencies that affect behavior (especially randomness) are explicit.

## A small set of stable concepts

The public API is built around a deliberately small conceptual core:

- Problem
- Search space
- Objective / solution
- Algorithm as an iterative state transformer (`IIterable`)
- Operators

The goal is that “reading the code” feels predictable: the same concepts reappear with the same names and shapes.

## Composition over graphs

HeuristicLib favors straightforward composition in C#:

- Algorithms are ordinary types that compose operator implementations.
- There is no operator graph runtime to learn.

## Explicit randomness and reproducibility

Randomness is explicit via `IRandomNumberGenerator`.

See [Randomness (RNG) design](randomness.md) for the deterministic forking model used for parallel workflows.

In this repository, `SystemRandomNumberGenerator` is a simple default implementation and supports spawning child generators (`Spawn(...)`) for parallel workflows.

## Focused scope

Compared to HeuristicLab, this repository intentionally narrows the scope:

- No GUI.
- No operator graph runtime.
- No “serialize the whole object graph” model.

The library aims to be easy to embed (services, experiments, notebooks) and easy to test.
