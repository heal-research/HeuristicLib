# Objectives & solutions

This page explains how HeuristicLib represents optimization goals and evaluated candidates.

## Objective

An `Objective` defines:

- `ObjectiveDirection[] Directions` (minimize/maximize per dimension)
- `IComparer<ObjectiveVector> TotalOrderComparer` (how to sort objective vectors)
- `ObjectiveVector Worst` (an extreme “worst possible” vector based on directions)

The `TotalOrderComparer` is important because it makes “best” unambiguous in algorithms that need sorting.

For single-objective problems, the repository provides `SingleObjective` helpers:

- `SingleObjective.Minimize`
- `SingleObjective.Maximize`

## ObjectiveVector

`ObjectiveVector` is what problem evaluation returns.

It is a small value object that behaves like a read-only list of doubles and includes multi-objective helpers:

- `CompareTo(other, objective)` returning a `DominanceRelation`
- `Dominates(other, objective)` and related convenience methods

For convenience:

- a `double` implicitly converts to a single-objective `ObjectiveVector`.

## Solution

`ISolution<TGenotype>` combines:

- `TGenotype Genotype`
- `ObjectiveVector ObjectiveVector`

This separation is intentional:

- Genotype = representation (what you search over)
- Objective vector = evaluation (what you optimize)

In this repository, `Solution<TGenotype>` is a simple record implementing `ISolution<TGenotype>`.

## Related pages

- [Problem](problem.md)
- [Operators](operators.md)

