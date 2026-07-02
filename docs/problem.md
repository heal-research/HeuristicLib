# Problem

A **problem** defines what it means to evaluate a candidate.

It is the boundary between “search” and “domain”:

- A problem answers **what to optimize**.
- An algorithm answers **how to search**.

## Contract

A problem provides three things:

- a search space
- objective directions and any ordering needed by the problem
- an evaluation function that maps a candidate to an objective vector

This makes problems self-contained: they own the search space, evaluation semantics, and objective directions.

## The base class in this repository

In `HEAL.HeuristicLib` there is a convenient abstract base class:

- `Problem<TCandidate, TSearchSpace>`

It stores the search space and objective-direction model, and leaves `Evaluate(...)` abstract.

## Deterministic vs stochastic evaluation

Even if your evaluation is deterministic, the interface still passes an `IRandomNumberGenerator`.

That design keeps the call sites uniform and makes it easy to introduce stochasticity later without changing algorithm/operator signatures.

## Minimal example: `FuncProblem`

If your objective is a single number, `FuncProblem<TCandidate, TSearchSpace>` is the lightest way to model it:

```csharp
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

public sealed record Candidate(double X);

sealed class AnyCandidateSpace : ISearchSpace<Candidate> {
	public bool Contains(Candidate candidate) => true;
}

var problem = FuncProblem.Create<Candidate, AnyCandidateSpace>(
	evaluateFunc: c => c.X * c.X,
	searchSpace: new AnyCandidateSpace(),
	objective: SingleObjective.Minimize
);
```

## Related pages

- [Search spaces](search-space.md)
- [Objective vectors and evaluated candidates](objectives-and-solutions.md)
- [Operators](operators.md)
