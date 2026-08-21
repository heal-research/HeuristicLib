# Objectives and evaluated candidates

Evaluation turns a candidate into an objective vector. HeuristicLib stores the candidate and vector together as an evaluated candidate. Selection, analysis and reporting can then reuse the evaluation.

## Single objective

A single objective problem still returns an objective vector with one value. Its objective defines whether smaller or larger values are better.

```csharp
objective: SingleObjective.Minimize
```

Use the problem's comparer when selecting the best result. It already understands the objective direction:

```csharp
var best = state.Population.EvaluatedCandidates
    .MinBy(
        candidate => candidate.ObjectiveVector,
        problem.Objective.TotalOrderComparer)!;
```

Avoid comparing raw values with `<` or `>` in generic code because that silently assumes minimization.

## Multiple objectives

A multiobjective vector might contain cost, duration and emissions. One candidate can be better on cost while another is better on emissions, so there may be no single best answer.

A candidate **dominates** another when it is no worse on every objective and better on at least one. Candidates that are not dominated form a Pareto front. Algorithms such as NSGA-II approximate that front and preserve a diverse set of tradeoffs.

Do not invent a total ordering unless your domain supplies a real preference, such as a weighted utility function. A display order is not automatically an optimization objective.

`MultiObjective.Create` builds directions for several objectives, and multiobjective directions carry no total order comparer, so `TotalOrderComparer` is unavailable by design. Use `ParetoFront.ExtractFrom` to reduce a population to its nondominated members. [Multiobjective optimization](/examples/multi-objective) works through a complete NSGA-II run and shows how to read and use the resulting front.

## Evaluated candidates

An evaluated candidate contains:

- the original candidate
- its objective vector

Treat the pair as one result. If a candidate changes, it must be evaluated again. Mutation and crossover should produce a new candidate rather than modifying a candidate already paired with objective values.

## Reporting results

For a single objective run, report at least the best candidate, objective value, seed and algorithm configuration. For a multiobjective run, report the nondominated set and keep every objective label and unit visible.

Repeated experiments should summarize distributions across runs. A median and spread are more informative than the best value from one lucky seed.
