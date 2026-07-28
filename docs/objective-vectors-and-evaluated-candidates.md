# Objective vectors and evaluated candidates

This page explains how HeuristicLib represents optimization goals and evaluated candidates.

## Objective directions and ordering

An objective-direction model defines:

- `ObjectiveDirection[] Directions` (minimize/maximize per objective value)
- `IComparer<ObjectiveVector> TotalOrderComparer` (how to sort objective vectors)
- `ObjectiveVector Worst` (an extreme “worst possible” vector based on directions)

The `TotalOrderComparer` is important because it makes "best" unambiguous in algorithms that need sorting.

For single-objective problems, the repository provides `SingleObjective` helpers:

- `SingleObjective.Minimize`
- `SingleObjective.Maximize`

## Objective vector

`ObjectiveVector` is the value object returned by problem evaluation.

It is a small value object that behaves like a read-only list of doubles and includes multi-objective helpers:

- `CompareTo(other, objectiveDirections)` returning a `DominanceRelation`
- `Dominates(other, objective)` and related convenience methods

For convenience:

- a `double` implicitly converts to a single-objective `ObjectiveVector`.

## Evaluated candidate

`EvaluatedCandidate<TCandidate>` combines:

- `TCandidate Candidate`
- `ObjectiveVector ObjectiveVector`

This separation is intentional:

- Candidate = representation (what you search over)
- Objective vector = evaluation outcome (what you optimize)

In this repository, `EvaluatedCandidate<TCandidate>` is a simple value object.

Use `EvaluatedCandidate.From(candidate, objectiveVector)` or `candidate.ToEvaluated(objectiveVector)` when the candidate value should determine the generic type.

## Related pages

- [Problem](problem.md)
- [Operators](operators.md)
