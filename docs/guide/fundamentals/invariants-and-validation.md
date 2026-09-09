# Invariants and validation

::: info What this answers
Whether an operator can actually be used with a search space, and how that is decided before a run starts rather than
discovered from bad results afterwards.
:::

## The problem a type cannot solve

An operator's type arguments say what it **reads**. `SingleCandidateMutator<BoolVector, FixedCardinalityBoolVectorSearchSpace>` reads a fixed cardinality space, so it can consult the cardinality.

What they never say is what the operator **preserves**. Preserving a property is a statement about the operator's *output*, and an output property is not part of a signature. Two mutators over bool vectors have identical signatures whether or not they keep the number of set elements constant.

That gap is real and it is not a design oversight to be fixed with more type arguments:

```csharp
var constrained = new FixedCardinalityBoolVectorSearchSpace(length: 4, cardinality: 2);

// Accepted by the compiler. One flip changes the number of set elements, so the
// candidate leaves the space on the first mutation.
IMutator<BoolVector> slot = new FlipOneBitMutator();
```

The invariant system closes the gap with values instead of types. Operators and search spaces each state what they mean, and the statements are compared.

## The three pieces

### An invariant is a property a candidate can have

`ICandidateInvariant<TCandidate>` is a named property, ordinarily a record so equality is by value:

```csharp
public sealed record BoolVectorCardinality(int Cardinality) : ICandidateInvariant<BoolVector>
{
    public string Name => $"Cardinality({Cardinality})";

    public bool Holds(BoolVector candidate) => CountSetElements(candidate) == Cardinality;
}
```

`Implies` is the one member with a non-obvious job. It answers whether holding *this* invariant is enough to guarantee another one, and defaults to value equality. Override it where one invariant is strictly stronger:

```csharp
public bool Implies(ICandidateInvariant<BoolVector> other) => other switch
{
    BoolVectorCardinality cardinality => cardinality.Cardinality == Cardinality,
    BoolVectorMinimumSetElements minimum => Cardinality >= minimum.Minimum,
    _ => false
};
```

An exact cardinality of 5 implies "at least 2", so an operator requiring the latter is satisfied by a space guaranteeing the former.

Nothing enumerates invariants. Any type may declare a new one, and a custom search space is expected to.

### A search space states what its members satisfy

```csharp
public record FixedCardinalityBoolVectorSearchSpace : SearchSpace<BoolVector>
{
    public override IReadOnlyList<ICandidateInvariant<BoolVector>> Invariants =>
        [new BoolVectorLength(Length), new BoolVectorCardinality(Cardinality)];
}
```

An empty list — the default — means the space requires nothing, so every operator over that candidate representation is compatible with it. This is why adopting invariants is a per-space decision and why configurations written before the system existed still validate unchanged.

### An operator states its side of the deal

`IOperatorContract<TCandidate>` has two halves, and they decide two independent checks:

```csharp
public record BitSwapMutator
    : SingleCandidateMutator<BoolVector, FixedCardinalityBoolVectorSearchSpace>, IOperatorContract<BoolVector>
{
    public bool? Ensures(ICandidateInvariant<BoolVector> invariant) => invariant switch
    {
        BoolVectorLength or BoolVectorCardinality => true,
        _ => null
    };
}
```

`Ensures` is three-valued, and the third value is the important one:

| Answer | Meaning | Effect |
| --- | --- | --- |
| `true` | Every candidate this operator produces has the invariant | Compatible for that invariant |
| `false` | The output may not have it | Reported as an incompatibility |
| `null` | The operator states nothing | Not checked |

So **declaring is opt-in, per invariant**. An operator that implements nothing, or answers `null`, is never reported. That keeps the system additive: adding an invariant to a search space does not invalidate operators that never claimed anything about it.

`Ensures` is named for what it covers. A mutator *preserves* an invariant of its input; a creator has no input and *establishes* one. Both are "ensures this of my output", which is why one member serves both.

The other half is `Requires`, the invariants an operator needs of the candidates handed to it:

```csharp
public IReadOnlyList<ICandidateInvariant<BoolVector>> Requires => [new BoolVectorMinimumSetElements(2)];
```

An operator that swaps the second set element cannot be handed a candidate with fewer than two, so it is usable over a fixed cardinality space and not over an unconstrained one. That is the mirror image of the flip mutator, and it is why neither check substitutes for the other: accepting more input makes an operator more widely usable, and so does ensuring more about its output.

## How the two checks run

Given an operator and a search space:

1. **Output check.** For each invariant the space states of its members, if the operator's `Ensures` answers `false`, the operator's output may leave the space.
2. **Input check.** For each invariant in `Requires`, if no invariant the space states `Implies` it, the operator may be handed a candidate it cannot accept.

Each failure becomes a diagnostic naming the invariant that decided it, so a message says which operator to change and why.

## When validation happens

**Creating a run validates by default.** The whole configuration is walked and checked against the problem's search space before the run exists:

```csharp
var run = algorithm.CreateRun(problem, random);
// InvalidOperationException: FlipOneBitMutator does not ensure Cardinality(2), which
// FixedCardinalityBoolVectorSearchSpace requires of its members, so its output may
// leave the search space.
```

`Stream`, `Complete` and `CompleteAsync` go through `CreateRun`, so they validate too.

**Opt out with `validate: false`** when deliberately running a configuration whose declared contracts do not hold — while an operator is being written, or to observe what a mismatched operator actually does:

```csharp
var run = algorithm.CreateRun(problem, random, validate: false);
```

**Ask without running** when the answer is wanted as a report rather than as an exception:

```csharp
var report = algorithm.Validate(problem);

foreach (var diagnostic in report.Diagnostics)
{
    Console.WriteLine($"{diagnostic.Path}: {diagnostic.Message}");
}
```

`algorithm.ValidateAndThrow(problem)` is the same check as a guard clause. `SearchConfigurationValidation.Validate(configuration, searchSpace)` checks any configuration — a single operator, a composition — against a space directly, without a problem.

Every form reports **every** failure it finds rather than stopping at the first, so one pass tells you everything to fix.

::: warning Resolution does not validate
The check happens when a run is created. Reaching an execution instance directly through
`ExecutionInstanceRegistry.Resolve` bypasses it, by design — that is the same escape hatch as `validate: false`, and
it is how the library's own internals build operators.
:::

## What the walk covers

Validation walks the configuration graph by reflection, following any property whose value is a configuration, and any configuration inside an enumerable property. It therefore reaches operators nested inside compositions, and it works for algorithms and operators written outside the library.

Diagnostics carry the path that leads to the operator:

```csharp
var pipeline = PipelineMutator.Create(new BitSwapMutator(), new FlipOneBitMutator());
var report = SearchConfigurationValidation.Validate(pipeline, constrained);

report.Diagnostics.Single().Path;    // "PipelineMutator.ChildMutators[1]"
```

A composition's own failure is suppressed when a child already reported the same invariant. A composed operator inherits its children's limits, so repeating them would bury the one diagnostic naming the operator you actually have to change.

The walk runs once per validation and never during a run, so it costs nothing per iteration.

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

This is the one place `Holds` is used by the library. Compatibility checking never calls it, because it reasons about declarations rather than about particular candidates.

Write this test for any operator that declares a contract. A declared invariant that does not hold is worse than no declaration, because it turns validation from a safety net into a false assurance.

## What this system is not

- **Not a repair mechanism.** Validation refuses an incompatible configuration; it never fixes a candidate. An operator that can leave its space must repair or reject internally.
- **Not a substitute for `Contains`.** `ISearchSpace.Contains` answers about one candidate at runtime. Invariants answer about an operator's behavior in general, before any candidate exists.
- **Not universal.** A space stating no invariants is checked for nothing. Most built-in spaces state a few; only bool vectors currently exercise the interesting cases.

## Next steps

- [Search spaces](/guide/fundamentals/search-spaces) — the spaces these invariants describe.
- [Operators](/guide/fundamentals/operators) — choosing operators for a representation.
- [Operator composition](/guide/extending/operator-composition) — how composed operators combine their children's contracts.
