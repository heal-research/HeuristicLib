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

## Choose a construction form

HeuristicLib offers three ways to construct a standard algorithm. They create the same configuration record and none of them starts a run.

| Form                               | Use it when                                                                          | What the caller supplies                                                           |
| ---------------------------------- | ------------------------------------------------------------------------------------ | ---------------------------------------------------------------------------------- |
| Constructor and object initializer | You need an exact generic type or want every configured member visible as a property | Generic type arguments and every required operator                                 |
| `Create(...)`                      | You chose the required operators and want their types inferred                       | Required operators, followed by any algorithm setting overrides                    |
| `For(...)`                         | The problem or encoding publishes a complete set of suggested operators              | A problem or search space, followed by any operator or algorithm setting overrides |

All three forms use the algorithm's public defaults class for omitted optional settings. Direct construction and `Create(...)` never choose required variation operators for you. `For(...)` is the only form that asks a problem first, then its encoding, for those operators.

The constructor is the direct record API. `Create(...)` and `For(...)` are type inference conveniences over it. After construction, every form supports the same property access and `with` expressions.

## Configure a genetic algorithm

```csharp
var algorithm = GeneticAlgorithm.Create(
    new UniformDistributedCreator(problem.SearchSpace),
    new AlphaBetaBlendCrossover { Alpha = 0.7 },
    new GaussianMutator(mutationRate: 0.2, mutationStrength: 0.15),
    selector: TournamentSelector.For(problem, tournamentSize: 2),
    populationSize: 50,
    maximumGenerations: 100,
    mutationRate: 0.2);
```

Algorithm owned settings such as `MaximumGenerations` describe ordinary run behavior. The operators make search policy visible and replaceable. `Create(...)` infers the generic arguments from those operators.

## Use suggested defaults

Some encodings and problems publish suggested operators. Use `For(problem, ...)` when that complete defaults flow exists:

```csharp
var algorithm = GeneticAlgorithm.For(
    travelingSalesmanProblem,
    populationSize: 100,
    maximumGenerations: 500);
```

The problem gets the first chance to suggest each required operator. A role it declines falls back to the encoding. For `TravelingSalesmanProblem`, the problem supplies order crossover and `PermutationSearchSpace` supplies random creation and inversion mutation. Selector, evaluator and scalar settings come from `GeneticAlgorithmDefaults`.

Each standard algorithm has a public defaults class, such as `GeneticAlgorithmDefaults` or `NSGA2Defaults`. Direct record construction, `Create(...)` and `For(...)` read the same values for omitted algorithm settings. Switching construction forms therefore does not change an omitted population size, selector or evaluator. Encoding and problem defaults are separate because those types own the information needed to recommend variation operators.

`For(searchSpace, ...)` uses encoding defaults alone and returns a configuration that can run against any compatible problem. `EvolutionStrategy`, `NSGA2` and `HillClimber` offer the same two forms, constrained to the operator roles each algorithm requires.

Defaults are documented starting points. They are not tuned for every instance and changing them can change results. State important choices explicitly when a configuration must remain independent of future default changes:

```csharp
var algorithm = GeneticAlgorithm.For(travelingSalesmanProblem)
    with { Crossover = new EdgeRecombinationCrossover() };
```

Use `Create(...)` when no defaults flow exists or when the required operators are part of the experiment. Optional arguments override algorithm defaults in either form.

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
