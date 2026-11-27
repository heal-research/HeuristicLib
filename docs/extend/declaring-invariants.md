# Declaring invariants

Before a run starts, validation refuses an operator whose output may leave the search space or that needs input the space does not guarantee ([Validation and invariants](/guide/fundamentals/invariants-and-validation)). The check compares declarations. Declare them for your own search spaces and operators.

## The three pieces

### An invariant is a property a candidate can have

`ICandidateInvariant<TCandidate>` is a named property, ordinarily a record so equality is by value:

```csharp
public sealed record BoolVectorCardinality(int Cardinality) : ICandidateInvariant<BoolVector>
{
    public string Name => $"Cardinality({Cardinality})";

    public bool Holds(BoolVector candidate)
    {
        return candidate.TrueCount() == Cardinality;
    }
}
```

`Implies` is the one member with a non-obvious job. It answers whether holding _this_ invariant is enough to guarantee another one, and defaults to value equality. Override it where one invariant is strictly stronger:

```csharp
public bool Implies(ICandidateInvariant<BoolVector> other)
{
    return other switch
    {
        BoolVectorCardinality cardinality => cardinality.Cardinality == Cardinality,
        BoolVectorMinimumSetElements minimum => Cardinality >= minimum.Minimum,
        _ => false
    };
}
```

An exact cardinality of 5 implies "at least 2", so an operator requiring the latter is satisfied by a space guaranteeing the former.

Nothing enumerates invariants. Any type may declare a new one, and a custom search space is expected to.

### A search space states what its members satisfy

```csharp
public record FixedCardinalityBoolVectorSearchSpace : SearchSpace<BoolVector>
{
    // ... constructor, Length and Cardinality properties, and Contains override omitted.

    public override ValueArray<ICandidateInvariant<BoolVector>> Invariants =>
        [new BoolVectorLength(Length), new BoolVectorCardinality(Cardinality)];
}
```

The default `ValueArray<T>` is empty. A space that declares no invariants gives an operator no output invariant to answer for, so the output check passes for every operator over its candidate representation. An operator that requires an invariant is still rejected there, because nothing the space declares entails it.

### An operator states its side of the deal

`IOperatorContract<TCandidate>` has two halves, and they decide two independent checks:

```csharp
public record BitSwapMutator
    : SingleCandidateMutator<BoolVector, FixedCardinalityBoolVectorSearchSpace>, IOperatorContract<BoolVector>
{
    public bool? Ensures(ICandidateInvariant<BoolVector> invariant)
    {
        return invariant switch
        {
            BoolVectorLength or BoolVectorCardinality => true,
            _ => null
        };
    }

    public override BoolVector MutateCandidate(
        BoolVector parent,
        IRandomNumberGenerator random,
        FixedCardinalityBoolVectorSearchSpace searchSpace)
    {
        // ...
    }
}
```

`Ensures` is three-valued, and the third value is the important one:

| Answer  | Meaning                                                  | Effect                         |
| ------- | -------------------------------------------------------- | ------------------------------ |
| `true`  | Every candidate this operator produces has the invariant | Compatible for that invariant  |
| `false` | The output may not have it                               | Reported as an incompatibility |
| `null`  | The operator states nothing                              | Not checked                    |

So **declaring is opt-in, per invariant**. An operator that implements nothing, or answers `null`, is never reported. Adding an invariant to a search space therefore does not invalidate operators that make no claim about it.

`Ensures` describes the operator's output. For a mutator, `true` means it keeps the invariant of its input. For a creator, it means every created candidate has it.

The other half is `Requires`, the invariants an operator needs of the candidates handed to it:

```csharp
public ValueArray<ICandidateInvariant<BoolVector>> Requires => [new BoolVectorMinimumSetElements(2)];
```

An operator that swaps the second set element cannot be handed a candidate with fewer than two, so it is usable over a fixed cardinality space with a cardinality of at least two and not over an unconstrained one. A bit flip fails the other way: it accepts every bool vector, and its output can leave a fixed cardinality space. Neither check finds the other's failure, so both run.

## How the two invariant checks run

Given an operator and a search space:

1. **Output check.** For each invariant the space states of its members, if the operator's `Ensures` answers `false`, the operator's output may leave the space.
2. **Input check.** For each invariant in `Requires`, if no invariant the space states `Implies` it, the operator may be handed a candidate it cannot accept.

Each failure becomes a diagnostic naming the invariant that decided it, so a message says which operator to change and why.

## A composition answers for what it resolves

Validation asks each configuration about itself and then follows the children it declares as parameters, so an
operator nested inside a composition is checked with the rest of the graph. A child is checked against its owner's
run. A composition that resolves a child under different types declares those types on the child's parameter, and
the child and everything below it are checked against them instead.

The walk reaches a child only through its declaration. A composition that leaves a child undeclared loses the check
for that child. It does not get a wrong answer. An entry of a `ReferenceListParameter` names a configuration that lives
elsewhere in the graph and is checked there. A value declared with a `SettingParameter` is data, even when its type
closes over a configuration, so it is not checked.

To declare children in your own composition, see
[Writing compositions](/extend/writing-compositions#declare-children-with-a-specific-part). To resolve a child under
another problem and declare that on its parameter, see
[Resolve an operator under another objective](/extend/writing-operators#resolve-an-operator-under-another-objective).

## Declarations are claims, so check them

Nothing forces `Ensures` to be truthful. An operator can claim to preserve cardinality and not do it, and the compatibility check will believe it — the check compares declarations, it does not execute operators.

`OperatorContractVerification.Verify` closes that loop by running the operator over sample candidates and testing the result with `Holds`:

```csharp
var violations = OperatorContractVerification.Verify(
    mutator,
    constrained,
    samples,
    candidate => mutator.MutateCandidate(candidate, random, constrained));

violations.ShouldBeEmpty();
```

Compatibility checking never calls `Holds`. It compares declarations. `Verify` is where `Holds` is applied to actual
output.

Write this test for any operator that declares a contract. Validation trusts every declaration, so a declared
invariant that does not hold lets an operator through that leaves the space.

## Next steps

- [Validation and invariants](/guide/fundamentals/invariants-and-validation) — when validation runs and what it reports.
- [Write an operator](/extend/writing-operators) — the operator the contract belongs to.
- [Search spaces](/guide/fundamentals/search-spaces) — the built-in spaces and their invariants.
