# Search spaces

A search space describes valid candidates. Algorithms use it as a shared contract for creation, mutation and validation.

## Built-in spaces

| Candidate       | Search space                            | Good fit                                         |
| --------------- | --------------------------------------- | ------------------------------------------------ |
| `RealVector`    | `BoundedRealVectorSearchSpace`          | Continuous parameters and numerical optimization |
| `IntegerVector` | `IntegerVectorSearchSpace`              | Counts, choices and bounded discrete parameters  |
| `BoolVector`    | `BoolVectorSearchSpace`                 | Feature selection and yes or no decisions        |
| `BoolVector`    | `FixedCardinalityBoolVectorSearchSpace` | Selection of exactly _k_ items out of _n_        |
| `Permutation`   | `PermutationSearchSpace`                | Orders, tours and assignments without duplicates |

<EncodingGallery>

One candidate from each built-in search space. The dots under the real vector mark each value within the bounds of the Rastrigin space, `-5.12` to `5.12`.

</EncodingGallery>

Choose a representation that makes invalid candidates difficult to express. A permutation is a better model for a tour than an integer vector that needs a duplicate removal rule.

## Define a real vector space

```csharp
using HEAL.HeuristicLib.Encodings.RealVectors;

var space = new BoundedRealVectorSearchSpace(
    length: 4,
    minimum: [-5.12],
    maximum: [5.12]);
```

A one element bound is broadcast across the vector. Use one bound per position when dimensions have different ranges.

```csharp
var mixedSpace = new BoundedRealVectorSearchSpace(
    length: 3,
    minimum: [0.0, -10.0, 1.0],
    maximum: [1.0, 10.0, 100.0]);
```

## Search spaces and operators

Creators need the search space to produce valid initial candidates. Some mutation and crossover operators also need it to clamp, repair or scale changes. The run supplies its search space to every operator call, so operators are configured without one:

```csharp
var creator = new UniformDistributedCreator();
var mutator = new GaussianMutator(
    mutationRate: 0.2,
    mutationStrength: 0.15);
```

Passing a space to `UniformDistributedCreator` pins its bounds to that space instead of the run's.

Choose operators designed for the candidate representation. Real vector crossover has different validity rules from permutation crossover.

::: warning Preserve validity
If a custom operator can leave the search space, repair the candidate in that operator or reject it before evaluation. Do not let invalid values silently reach domain code.
:::

## Validation and invariants {#invariants-and-validation}

A search space can state what its members satisfy, such as a length, a bound or a number of set elements, and an
operator can declare which of those its output keeps. Creating a run compares the two declarations by default. An
operator that declares it does not keep one of the space's invariants stops the run before it starts. For example,
`BitFlipMutator` is refused over a `FixedCardinalityBoolVectorSearchSpace`, and `BitSwapMutator`, which keeps the
number of set elements, is accepted.

The check compares declarations and does not run operators. An operator that declares nothing about an invariant is
not reported, so the warning above still applies to custom operators. Test what an operator declares with
`OperatorContractVerification.Verify`.

See [Validation and invariants](/guide/fundamentals/invariants-and-validation) for the contracts, the two checks, how
to opt out with `validate: false`, and how to test that a declared contract actually holds.

## Custom search spaces

A custom candidate type usually needs a matching search space. The space should own structural constraints that apply to every problem using that candidate. Put problem specific feasibility rules in the problem when they depend on domain data.

Before adding a custom representation, ask whether a built-in vector plus a decoding function is simpler. A direct representation is most useful when it also enables meaningful creation and variation operators.

## Next steps

- [Objectives and candidates](/guide/fundamentals/objectives) continues the guide with objective direction and evaluated candidates.
- [Choose compatible operators](/guide/fundamentals/operators)
- [Define evaluation and objective direction](/guide/fundamentals/problems)
