# Problem

A **problem** defines what it means to evaluate a genotype.

It is the boundary between “search” and “domain”:

- A problem answers **what to optimize**.
- An algorithm answers **how to search**.

## Contract

`IProblem<TGenotype, TSearchSpace>` provides three things:

- `TSearchSpace SearchSpace { get; }`
- `Objective Objective { get; }`
- `ObjectiveVector Evaluate(TGenotype solution, IRandomNumberGenerator random)`

This makes problems self-contained: they own both evaluation and the objective definition (directions + ordering).

## The base class in this repository

In `HEAL.HeuristicLib` there is a convenient abstract base class:

- `Problem<TSolution, TSearchSpace>`

It stores `Objective` and `SearchSpace` and leaves `Evaluate(...)` abstract.

## Deterministic vs stochastic evaluation

Even if your evaluation is deterministic, the interface still passes an `IRandomNumberGenerator`.

That design keeps the call sites uniform and makes it easy to introduce stochasticity later without changing algorithm/operator signatures.

## Minimal example: `FuncProblem`

If your objective is a single number, `FuncProblem<TGenotype, TSearchSpace>` is the lightest way to model it:

```csharp
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

public sealed record Candidate(double X);

sealed class AnyCandidateSpace : ISearchSpace<Candidate> {
	public bool Contains(Candidate genotype) => true;
}

var problem = FuncProblem.Create<Candidate, AnyCandidateSpace>(
	evaluateFunc: c => c.X * c.X,
	searchSpace: new AnyCandidateSpace(),
	objective: SingleObjective.Minimize
);
```

## Related pages

- [Search spaces](search-space.md)
- [Objectives & solutions](objectives-and-solutions.md)
- [Operators](operators.md)
