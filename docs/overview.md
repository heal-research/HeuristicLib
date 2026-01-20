# Overview

HeuristicLib is a **library-first** framework for implementing and composing heuristic optimization algorithms in modern C#.

The project’s organizing idea is simple:

> An **algorithm** iteratively transforms an **algorithm state** while operating on **genotypes** from a **search space**, evaluated by a **problem** under an **objective**.

This documentation focuses on the mental model and the stable contracts that shape day-to-day usage. The generated **API** reference is the place for exhaustive type-by-type details.

## What you get

- **Composable algorithms** built out of small operator roles (creator, evaluator, selector, crossover, mutator, replacer, terminator).
- **Explicit randomness** via `IRandomNumberGenerator` for reproducibility and deterministic testing (see [Randomness (RNG) design](randomness.md)).
- **Two execution styles**: run to completion, or stream iteration states.
- **Strong typing**: genotype, search space, and problem fit together through generics.

## A quick end-to-end example

The example below solves the built-in Traveling Salesman example problem using a genetic algorithm. It’s intentionally “plain C#”: create a problem, wire operators, run.

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

var ga = new GeneticAlgorithm<Permutation, PermutationSearchSpace, TravelingSalesmanProblem> {
   PopulationSize = 200,
   Creator = new RandomPermutationCreator(),
   Crossover = new OrderCrossover(),
   Mutator = new SwapSingleSolutionMutator().WithRate(mutationRate: 0.20),
   Selector = new TournamentSelector<Permutation>(tournamentSize: 3),
   Replacer = new ElitismReplacer<Permutation>(elites: 2),
   Evaluator = new DirectEvaluator<Permutation>(),
   Terminator = new AfterIterationsTerminator<Permutation>(maximumIterations: 200)
};

foreach (PopulationIterationState<Permutation> state in ga.ExecuteStreaming(problem, rng))
{
   var best = state.Population.Solutions
      .MinBy(s => s.ObjectiveVector, problem.Objective.TotalOrderComparer)!;

   Console.WriteLine($"Iter {state.CurrentIteration,4}: best = {best.ObjectiveVector}");
}
```

> [!NOTE]
> The default execution loop is streaming-first: `Execute(...)` is implemented as `ExecuteStreaming(...).Last()`. If you want progress reporting, streaming is the most natural hook.

## Where to go next

- If you want the shortest path to running your own problem: see [Getting started](getting-started.md).
- If you want the conceptual map first: see [Core concepts](core-concepts.md).
- If you want to understand iteration/termination/interception precisely: see [Execution model](execution-model.md).
