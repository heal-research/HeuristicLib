# Operators

Operators are focused search steps that algorithms combine. Most users configure built-in operators. You only need to create one when your representation or domain has a useful move that the library does not provide.

## Common operator roles

| Role      | Input and output                         | Purpose                             |
| --------- | ---------------------------------------- | ----------------------------------- |
| Creator   | Search space to candidate                | Build an initial solution           |
| Evaluator | Candidate to objective vector            | Measure solution quality            |
| Selector  | Population to selected candidates        | Choose candidates for the next step |
| Crossover | Parent candidates to offspring           | Recombine information               |
| Mutator   | Candidate to changed candidate           | Introduce local variation           |
| Replacer  | Current and new candidates to population | Decide what survives                |

## A genetic algorithm step

A typical generation selects parents, crosses them, mutates offspring, evaluates the new candidates and replaces part of the population. The algorithm controls that sequence. Operators provide each policy.

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

## Choose compatible operators

Start with the candidate representation:

- Real vectors use numerical crossover and mutation.
- Integer vectors need changes that preserve integral values and bounds.
- Boolean vectors use bit based variation: `RandomBoolVectorCreator`, `BitUniformCrossover` and `BitFlipMutator`. Use `BitSwapMutator` instead of `BitFlipMutator` over `FixedCardinalityBoolVectorSearchSpace`, since a single flip changes the number of set bits and leaves that space.
- Permutations need order aware operators that preserve every element exactly once.

Then consider how useful solutions differ from nearby solutions. Small Gaussian changes suit local refinement in continuous spaces. Swap or inversion mutation can preserve useful subsequences in routes. Tournament pressure affects how quickly a population concentrates around good candidates.

Change one policy at a time when comparing configurations and repeat each configuration across seeds.

## Call an operator directly

Operators can also be useful outside a complete algorithm. Direct calls are helpful in tests and when assembling a custom workflow. Resolve the operator with the problem it should use and pass an explicit random source when calling the executor.

A selection executor takes `Select(population, count, random)`. A replacement executor takes `Replace(previousPopulation, offspringPopulation, count, random)`. Both use the objective of their bound problem. Static selector and replacer helpers that take an objective can be called directly with that objective.

## Next steps

- [Algorithms](/guide/fundamentals/algorithms) continues the Fundamentals path and shows how algorithms run operators.
- [Use operator recommendations](/guide/fundamentals/algorithms#use-operator-recommendations)
- [Compose several operators](/guide/extending/operator-composition)
- [Write an operator](/guide/extending/writing-operators)
