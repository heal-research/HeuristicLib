# Objective values and evaluated candidates

This page explains how HeuristicLib represents optimization goals and evaluated candidates.

## Objective directions and ordering

An objective-direction model defines:

- `ObjectiveDirection[] Directions` (minimize/maximize per objective value)
- `IComparer<ObjectiveValues> TotalOrderComparer` (how to sort objective values)
- `ObjectiveValues Worst` (an extreme “worst possible” value set based on directions)

The `TotalOrderComparer` is important because it makes "best" unambiguous in algorithms that need sorting.

For single-objective problems, the repository provides `SingleObjective` helpers:

- `SingleObjective.Minimize`
- `SingleObjective.Maximize`

## Objective values

`ObjectiveValues` is the value object returned by problem evaluation.

It is a small value object that behaves like a read-only list of doubles and includes multi-objective helpers:

- `CompareTo(other, objectiveDirections)` returning a `DominanceRelation`
- `Dominates(other, objective)` and related convenience methods

For convenience:

- a `double` implicitly converts to single-objective `ObjectiveValues`.

## Evaluated candidate

`EvaluatedCandidate<TCandidate>` combines:

- `TCandidate Candidate`
- `ObjectiveValues ObjectiveValues`

This separation is intentional:

- Candidate = representation (what you search over)
- Objective values = evaluation outcome (what you optimize)

In this repository, `EvaluatedCandidate<TCandidate>` is a simple value object.

## Related pages

- [Problem](problem.md)
- [Operators](operators.md)
