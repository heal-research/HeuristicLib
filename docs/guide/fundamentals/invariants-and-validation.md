# Invariants and validation

::: info What this answers
Whether an operator can actually be used in a run, and how that is decided before the run starts rather than
discovered from a crash or from bad results afterwards. Two independent questions are asked: whether an operator was
*written for* this run's types at all, and whether what it *guarantees about its output* keeps candidates inside the
search space.
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

## How the two invariant checks run

Given an operator and a search space:

1. **Output check.** For each invariant the space states of its members, if the operator's `Ensures` answers `false`, the operator's output may leave the space.
2. **Input check.** For each invariant in `Requires`, if no invariant the space states `Implies` it, the operator may be handed a candidate it cannot accept.

Each failure becomes a diagnostic naming the invariant that decided it, so a message says which operator to change and why.

## The other question: was it written for this run at all?

Invariants answer what an operator *does*. A separate question comes first: can it run here at all? An operator
authored at a bound rung names the search space and problem it was written for, and the run supplies its own:

```csharp
// Written for one specific problem.
public sealed record TravellingSalesmanSpecificCrossover
    : SingleCandidateCrossover<Permutation, PermutationSearchSpace, TravelingSalesmanProblem>;
```

Put that in an algorithm running over a different permutation problem and it cannot work. That is decided entirely by
type arguments, so nothing has to be built to find out:

```csharp
public interface IExecutionInstanceResolvable
{
    /// A configuration that names no search space, problem or search state is written for every run.
    bool Fits(ExecutionSignature execution) => true;
}
```

`ExecutionSignature` is the triple an execution is built for — its search space, its problem, and the search state it
produces. It is built where an execution graph is created, by a run or by validation asking what a run would do, and
passed down unchanged; a configuration reads the parts it is written about and ignores
the rest, so an ordinary operator never builds one:

```csharp
public bool Fits(ExecutionSignature execution) =>
    execution.SearchSpace.IsAssignableTo(typeof(TSearchSpace))
    && execution.Problem.IsAssignableTo(typeof(TProblem));
```

A mutator is not written about a search state, so it does not look at that field. A terminator is, and adds a clause
for it — contravariant, like the other two. An interceptor *returns* its search state, so it is invariant in it and
compares exactly.

The assignability runs toward the declared type because the instance contracts are contravariant in the search space
and problem: an operator written for `BoundedRealVectorSearchSpace` accepts a run over exactly that or narrower.

A meta algorithm that starts an inner run over different types builds a signature for **that** run and passes it down
in turn. Every signature belongs to exactly one run, which is what the boundary rule below follows from.

**This is the same rule the authoring bases apply when they bridge**, so a run and a validation pass cannot disagree.
It is also cheap: two type checks, about 4 ns, paid once per operator per run rather than per iteration.

### A composition answers for what it resolves

`Fits` is a question about a subtree, not about one object. A composition that resolves its children under the
run it was given forwards to them:

```csharp
// WrappingMutator<TCandidate>
public virtual bool Fits(ExecutionSignature execution) => execution.Fits(ChildMutator);

// GeneticAlgorithm<TCandidate>
public override bool Fits(ExecutionSignature execution) =>
    base.Fits(execution) && execution.Fits(Creator, Crossover, Mutator, Terminator, Evaluator, Refiner, Selector);
```

A composition that resolves a child under *different* types — a different candidate, or a problem it adapts and then
calls directly — must **not** forward, because those children belong to a different run. Not forwarding is the default,
so such a composition needs no code: it answers for itself, and its children are never asked about a run they were
never part of. If it wants them checked, it checks them against the inner run's own signature.

That default is deliberate and it fails open. An author who writes a pass-through composition and forgets to forward
loses the check for its children rather than getting a wrong answer, which matches how declaring an invariant contract
is opt in.

## When validation happens

**Creating a run validates by default.** The whole configuration graph is checked before the run exists — both questions, everywhere they apply:

```csharp
var run = algorithm.CreateRun(problem, random);
// InvalidOperationException: The configuration cannot be used over this search space:
//   - GeneticAlgorithm`1.Mutator: FlipOneBitMutator does not ensure Cardinality(2), which
//     FixedCardinalityBoolVectorSearchSpace requires of its members, so its output may leave
//     the search space.
//   - GeneticAlgorithm`1.Crossover: TravellingSalesmanSpecificCrossover was not written for a
//     run over PermutationSearchSpace with FuncProblem`2 producing PopulationState`1.
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

Because it reads what each configuration declares rather than building anything, it reaches operators a run would only construct much later. A cycling algorithm resolves each stage lazily, once per cycle and during the run; an operator at fault inside one of those stages is still reported before the run starts.

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
