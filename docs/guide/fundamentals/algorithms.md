# Algorithms

An algorithm controls how a search begins, how one state becomes the next and when the run stops. You configure it as a plain C# value, then run it with a problem and random number generator.

## Choose a starting point

| Algorithm           | Use it when                                                              |
| ------------------- | ------------------------------------------------------------------------ |
| `HillClimber`       | A meaningful local mutation exists and a fast baseline is useful         |
| `GeneticAlgorithm`  | A population and recombination can explore a broad search space          |
| `EvolutionStrategy` | You are optimizing real valued candidates through mutation and selection |
| `NSGA2`             | The problem has competing objectives and you want a diverse Pareto front |

`NSGA2` differs from the others in what it returns. It approximates a set of tradeoffs rather than one best candidate, and it needs a selector and replacer that understand dominance. See [Multiobjective optimization](/examples/multi-objective).

Start with the simplest algorithm that represents the search you need. A hill climber makes a useful baseline. A more elaborate method should earn its complexity through repeated experiments.

## Configure a genetic algorithm

```csharp
var algorithm = new GeneticAlgorithm<RealVector, RealVectorSearchSpace, TestFunctionProblem>
{
    PopulationSize = 50,
    MaximumGenerations = 100,
    Creator = new UniformDistributedCreator(problem.SearchSpace),
    Crossover = new AlphaBetaBlendCrossover { Alpha = 0.7 },
    Mutator = new GaussianMutator(mutationRate: 0.2, mutationStrength: 0.15),
    Selector = TournamentSelector.For(problem, tournamentSize: 2),
    MutationRate = 0.2,
    Elites = 1
};
```

Algorithm owned settings such as `MaximumGenerations` describe ordinary run behavior. The operator properties make search policy visible and replaceable.

## Run it

Ask for only the final state when intermediate progress is not needed:

```csharp
var finalState = await algorithm.CompleteAsync(problem, random);
```

Stream states for progress, visualization or early inspection:

```csharp
await foreach (var state in algorithm.Stream(problem, random))
{
    Console.WriteLine(state.Population.EvaluatedCandidates.Count);
}
```

Each call starts a fresh run. See [Running algorithms](/guide/execution/running-algorithms) for cancellation, budgets and reproducibility.

## Compare algorithms fairly

- Use the same problem instances and seed schedule.
- Compare outcomes over repeated runs.
- Record evaluation counts when algorithms do different amounts of work per iteration.
- Include a simple baseline.
- Avoid tuning on the same instances used for the final comparison.

The [Experiments](/guide/execution/experiments) API helps express repetitions and parameter grids.

## Custom algorithms

Most domain customization belongs in a problem or operator. Create an algorithm only when the state transition or control flow itself is new. Read [Writing algorithms](/guide/extending/writing-algorithms) before implementing one.
