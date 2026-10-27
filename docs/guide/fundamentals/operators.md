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

Operators can also be useful outside a complete algorithm. Direct calls are helpful in tests and when assembling a custom workflow. Supply the same search space and random source that the run uses, so validity and reproducibility remain explicit.

## Permutation operators

The `HEAL.HeuristicLib.Encodings.Permutations` namespace provides operators for permutations of the values `0` through `Length - 1`. Crossovers and mutators produce immutable candidates and preserve every value exactly once.

`RandomPermutationCreator` samples a random ordering. `PredefinedPermutationCreator` snapshots its input list and cycles through it across batches. Its `WithRandom` property enables uniform sampling with replacement instead. Each independent execution starts its own cursor. The list must be nonempty, and every predefined permutation must belong to the requested search space. Use the generic `SeededWith` composition when predefined candidates should be consumed once before a fallback creator supplies the rest.

| Crossover                      | Behavior                                                                                                                         |
| ------------------------------ | -------------------------------------------------------------------------------------------------------------------------------- |
| `OrderCrossover`               | Keeps a segment from the first parent and fills gaps in cyclic second-parent order, starting after the segment.                  |
| `OrderCrossover2`              | Keeps a segment from the first parent and fills gaps from the beginning of the second parent.                                    |
| `OrderBasedCrossover`          | Reorders selected values in the second parent according to their first-parent order.                                             |
| `PositionBasedCrossover`       | Keeps selected second-parent positions and fills gaps in first-parent order.                                                     |
| `PartiallyMatchedCrossover`    | Exchanges mapped values to preserve a first-parent segment. Requires at least four elements.                                     |
| `CyclicCrossover`              | Alternates parents between position cycles, choosing the starting parent randomly.                                               |
| `CyclicCrossover2`             | Keeps one randomly selected first-parent cycle and takes other positions from the second parent.                                 |
| `EdgeRecombinationCrossover`   | Builds a tour using parental adjacency information.                                                                              |
| `MaximalPreservativeCrossover` | Keeps a cyclic segment, then prefers unused successors from the second parent before the first. Requires at least four elements. |
| `CosaCrossover`                | Introduces a second-parent successor edge by reversing a cyclic first-parent segment.                                            |
| `UniformLikeCrossover`         | Chooses unused parental values at each position and repairs gaps with unused values.                                             |

| Mutator                         | Behavior                                                                      |
| ------------------------------- | ----------------------------------------------------------------------------- |
| `SwapMutator`                   | Swaps two independently chosen positions; the positions may coincide.         |
| `Swap3Mutator`                  | Rotates values at three distinct positions. Requires at least three elements. |
| `InversionMutator`              | Reverses an inclusive segment.                                                |
| `InsertionMutator`              | Moves one element to another position.                                        |
| `ScrambleMutator`               | Shuffles an inclusive segment with Fisher–Yates.                              |
| `TranslocationMutator`          | Moves an inclusive segment without reversing it.                              |
| `TranslocationInversionMutator` | Moves and reverses an inclusive segment.                                      |

Except for the stated minimum lengths, random crossover and mutation return permutations with fewer than two elements unchanged without drawing randomness. Segment mutations can leave a candidate unchanged. Translocation insertion points refer to the permutation _after_ removing the segment; inserting at the remaining length appends it.

Use `ChooseOneCrossover` and `ChooseOneMutator` to select among explicitly configured operators, optionally with weights. All selected operators must support the permutation length. This executable example combines predefined creation, crossover selection and mutation selection:

<<< ../../../test/HeuristicLib.Tests.Docs/Fundamentals/PermutationOperatorsExample.cs#permutation-operators{csharp}

## Related guides

- [Use operator recommendations](/guide/fundamentals/algorithms#use-operator-recommendations)
- [Compose several operators](/guide/extending/operator-composition)
- [Write an operator](/guide/extending/writing-operators)
- [See how algorithms run them](/guide/fundamentals/algorithms)
