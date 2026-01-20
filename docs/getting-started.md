# Getting started

This page is the shortest path from **“I have an optimization problem”** to **“I can run an algorithm, see intermediate states, and understand what the pieces do.”**

The quickest way to learn the library is to follow one stable loop:

1. Pick a **genotype** (your candidate representation).
2. Pick or implement a **search space** (valid candidates).
3. Pick or implement a **problem** (evaluation + objective).
4. Choose an **algorithm** and operator set.
5. Run it **streaming-first**, and inspect iteration states.

If you want the conceptual map first, start with [Core concepts](core-concepts.md).

## Prerequisites

- HeuristicLib targets `net10.0` in this repository.
- Reference the `HEAL.HeuristicLib` project (or the corresponding package when consumed via NuGet). It already references `HEAL.HeuristicLib.Abstractions`.

## 1) Run the built-in TSP example with a Genetic Algorithm

HeuristicLib includes a small, deterministic Traveling Salesman sample problem via `TravelingSalesmanProblem.CreateDefault()`.

```csharp
using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Algorithms.Evolutionary;
using HEAL.HeuristicLib.Genotypes.Vectors;
using HEAL.HeuristicLib.Operators.Creators.PermutationCreators;
using HEAL.HeuristicLib.Operators.Crossovers.PermutationCrossovers;
using HEAL.HeuristicLib.Operators.Evaluators;
using HEAL.HeuristicLib.Operators.Mutators;
using HEAL.HeuristicLib.Operators.Mutators.PermutationMutators;
using HEAL.HeuristicLib.Operators.Replacers;
using HEAL.HeuristicLib.Operators.Selectors;
using HEAL.HeuristicLib.Operators.Terminators;
using HEAL.HeuristicLib.Problems.TravelingSalesman;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces.Vectors;
using HEAL.HeuristicLib.States;

var problem = TravelingSalesmanProblem.CreateDefault();
var rng = new SystemRandomNumberGenerator(seed: 123);

var algorithm = new GeneticAlgorithm<Permutation, PermutationSearchSpace, TravelingSalesmanProblem> {
  PopulationSize = 200,
  Creator = new RandomPermutationCreator(),
  Crossover = new OrderCrossover(),
  Mutator = new SwapSingleSolutionMutator().WithRate(mutationRate: 0.20),
  Selector = new TournamentSelector<Permutation>(tournamentSize: 3),
  Replacer = new ElitismReplacer<Permutation>(elites: 2),
  Evaluator = new DirectEvaluator<Permutation>(),
  Terminator = new AfterIterationsTerminator<Permutation>(maximumIterations: 200)
};

foreach (PopulationIterationState<Permutation> state in algorithm.ExecuteStreaming(problem, rng))
{
  var best = state.Population.Solutions
    .MinBy(s => s.ObjectiveVector, problem.Objective.TotalOrderComparer)!;

  Console.WriteLine($"Iter {state.CurrentIteration,4}: best = {best.ObjectiveVector}");
}
```

This example demonstrates the library’s default working style:

- The **algorithm** produces a sequence of **iteration states**.
- A **terminator** decides whether another iteration should run.
- All randomness flows through `IRandomNumberGenerator` (see [Randomness (RNG) design](randomness.md) for seeding + forking).

## 2) Use a minimal custom problem

When your evaluation is naturally “one number to minimize/maximize”, `FuncProblem<TGenotype, TSearchSpace>` is the lightest starting point.

```csharp
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

public sealed record IntGenotype(int Value);

// This search space accepts all values. Real search spaces usually enforce bounds/shape.
sealed class AnyIntSearchSpace : ISearchSpace<IntGenotype> {
  public bool Contains(IntGenotype genotype) => true;
}

var problem = FuncProblem.Create<IntGenotype, AnyIntSearchSpace>(
  evaluateFunc: x => x.Value * x.Value,
  searchSpace: new AnyIntSearchSpace(),
  objective: SingleObjective.Minimize
);
```

From here you can either pick an algorithm that works on your genotype type, or switch to a structured genotype + search space pair that already has standard operators.

## Next steps

- Understand the vocabulary and how types relate: [Core concepts](core-concepts.md)
- Learn how iteration, termination, and interception are wired: [Execution model](execution-model.md)
- Dive into the pieces you’ll implement most often:
  - [Problem](problem.md)
  - [Search spaces](search-space.md)
  - [Objectives & solutions](objectives-and-solutions.md)
  - [Operators](operators.md)
  - [Algorithm](algorithm.md)
  - [Algorithm state](algorithm-state.md)
