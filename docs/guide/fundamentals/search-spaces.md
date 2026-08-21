# Search spaces

A search space describes valid candidates. Algorithms use it as a shared contract for creation, mutation and validation.

## Built-in spaces

| Candidate       | Search space               | Good fit                                         |
| --------------- | -------------------------- | ------------------------------------------------ |
| `RealVector`    | `RealVectorSearchSpace`    | Continuous parameters and numerical optimization |
| `IntegerVector` | `IntegerVectorSearchSpace` | Counts, choices and bounded discrete parameters  |
| `BoolVector`    | `BoolVectorSearchSpace`    | Feature selection and yes or no decisions        |
| `Permutation`   | `PermutationSearchSpace`   | Orders, tours and assignments without duplicates |

Choose a representation that makes invalid candidates difficult to express. A permutation is a better model for a tour than an integer vector that needs a duplicate removal rule.

## Define a real vector space

```csharp
using HEAL.HeuristicLib.Encodings.RealVectors;

var space = new RealVectorSearchSpace(
    length: 4,
    minimum: [-5.12],
    maximum: [5.12]);
```

A one element bound is broadcast across the vector. Use one bound per position when dimensions have different ranges.

```csharp
var mixedSpace = new RealVectorSearchSpace(
    length: 3,
    minimum: [0.0, -10.0, 1.0],
    maximum: [1.0, 10.0, 100.0]);
```

## Search spaces and operators

Creators need the search space to produce valid initial candidates. Some mutation and crossover operators also need it to clamp, repair or scale changes.

```csharp
var creator = new UniformDistributedCreator(space);
var mutator = new GaussianMutator(
    mutationRate: 0.2,
    mutationStrength: 0.15);
```

Choose operators designed for the candidate representation. Real vector crossover has different validity rules from permutation crossover.

::: warning Preserve validity
If a custom operator can leave the search space, repair the candidate in that operator or reject it before evaluation. Do not let invalid values silently reach domain code.
:::

## Custom search spaces

A custom candidate type usually needs a matching search space. The space should own structural constraints that apply to every problem using that candidate. Put problem specific feasibility rules in the problem when they depend on domain data.

Before adding a custom representation, ask whether a built-in vector plus a decoding function is simpler. A direct representation is most useful when it also enables meaningful creation and variation operators.

## Next steps

- [Define evaluation and objective direction](/guide/fundamentals/problems)
- [Choose compatible operators](/guide/fundamentals/operators)
- [Understand evaluated candidates](/guide/fundamentals/objectives)
