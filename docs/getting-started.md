# Getting started

This page is the shortest path from having a problem to running an algorithm and understanding the default extension story.

The most important ideas are:

1. configure problems and operators as plain objects
2. run algorithms streaming-first
3. when you implement your own algorithm, start from `IterativeAlgorithm<...>`

## Run a genetic algorithm

```csharp
using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Algorithms.Evolutionary;
using HEAL.HeuristicLib.Genotypes.Vectors;
using HEAL.HeuristicLib.Operators.Creators.PermutationCreators;
using HEAL.HeuristicLib.Operators.Crossovers.PermutationCrossovers;
using HEAL.HeuristicLib.Operators.Evaluators;
using HEAL.HeuristicLib.Operators.Mutators.PermutationMutators;
using HEAL.HeuristicLib.Operators.Selectors;
using HEAL.HeuristicLib.Problems.TravelingSalesman;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces.Vectors;

var problem = TravelingSalesmanProblem.CreateDefault();
var rng = new SystemRandomNumberGenerator(seed: 123);

var algorithm = new GeneticAlgorithm<Permutation, PermutationSearchSpace, TravelingSalesmanProblem> {
  PopulationSize = 200,
  MaximumGenerations = 200,
  Creator = new RandomPermutationCreator(),
  Crossover = new OrderCrossover(),
  Mutator = new SwapSingleSolutionMutator(),
  MutationRate = 0.20,
  Selector = new TournamentSelector<Permutation>(tournamentSize: 3),
  Elites = 2,
  Evaluator = new DirectEvaluator<Permutation>()
};

await foreach (var state in algorithm.RunStreamingAsync(problem, rng)) {
  Console.WriteLine(state.Population.EvaluatedCandidates.Count);
}
```

This is the intended everyday style:

- configure an algorithm directly
- use algorithm-owned budgets such as `MaximumGenerations` for ordinary algorithm setup
- keep behavior-affecting dependencies explicit
- stream states when you want progress

## Implement a small custom algorithm

```csharp
using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

public sealed record MyAlgorithm<TCandidate, TSearchSpace, TProblem>
  : IterativeAlgorithm<
      TCandidate,
      TSearchSpace,
      TProblem,
      SingleEvaluatedCandidateState<TCandidate>,
      MyAlgorithm<TCandidate, TSearchSpace, TProblem>.ExecutionState>
  where TSearchSpace : class, ISearchSpace<TCandidate>
  where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
  public new sealed class ExecutionState
    : IterativeAlgorithm<
        TCandidate,
        TSearchSpace,
        TProblem,
        SingleEvaluatedCandidateState<TCandidate>,
        ExecutionState>.ExecutionState
  {
    public required ICreatorInstance<TCandidate, TSearchSpace, TProblem> Creator { get; init; }
  }

  public required ICreator<TCandidate, TSearchSpace, TProblem> Creator { get; init; }

  protected override ExecutionState CreateInitialExecutionState(IExecutionInstanceResolver resolver)
  {
    return new ExecutionState {
      Evaluator = resolver.Resolve(Evaluator),
      Interceptor = Interceptor is not null ? resolver.Resolve(Interceptor) : null,
      Creator = resolver.Resolve(Creator)
    };
  }

  protected override SingleEvaluatedCandidateState<TCandidate> ExecuteStep(
    SingleEvaluatedCandidateState<TCandidate>? previousState,
    ExecutionState executionState,
    TProblem problem,
    IRandomNumberGenerator random)
  {
    var candidate = executionState.Creator.Create(1, random, problem.SearchSpace, problem)[0];
    var objectiveValues = executionState.Evaluator.Evaluate([candidate], random, problem.SearchSpace, problem)[0];

    return new SingleEvaluatedCandidateState<TCandidate> {
      Population = Population.From([candidate], [objectiveValues])
    };
  }
}
```

Here the public search state is the current evaluated candidate. If your algorithm needs other public progress data, add it explicitly to your concrete search-state type.

When the algorithm depends on operators, resolve them in `CreateInitialExecutionState(...)` and store the resulting execution instances and other per-run mutable data in the nested `ExecutionState`.

## What not to learn first

You do not need to start with:

- manual execution-instance classes
- full registry control
- meta-algorithms

Those are real parts of the system, but they are not the intended first extension path anymore.

## Next steps

- [Core concepts](core-concepts.md)
- [Algorithm](algorithm.md)
- [Execution model](execution-model.md)
- [Operators](operators.md)
