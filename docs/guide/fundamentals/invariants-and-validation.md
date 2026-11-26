# Validation and invariants

::: info What this answers
Whether an operator can actually be used in a run, and how that is decided before the run starts rather than
discovered from a crash or from bad results afterwards. Two independent questions are asked: whether an operator was
_written for_ this run's types at all, and whether what it _guarantees about its output_ keeps candidates inside the
search space.
:::

## Check whether an operator fits your search space

Two questions decide whether an operator can be used in a run:

1. **Was it written for the run's types?** An operator names the search space and problem it reads. A run over another
   space or problem cannot use it.
2. **Does it keep candidates inside the search space?** A search space states properties its members have, and an
   operator states which of them its output keeps. An operator that does not keep one of them would produce
   candidates outside the space.

A slot of an algorithm only names the candidate type and the operation, so the compiler accepts every bool vector
mutator in a mutator slot. `BitFlipMutator` flips elements independently, so it keeps the length of a `BoolVector` and
can change the number of set elements. Over a `FixedCardinalityBoolVectorSearchSpace`, whose members all have the same
number of set elements, it fails both checks: it is written for `BoolVectorSearchSpace`, and it declares that it does
not keep the cardinality.

```csharp
var constrained = new FixedCardinalityBoolVectorSearchSpace(length: 4, cardinality: 2);

// Accepted by the compiler, refused when a run over the constrained space is created.
IOperator<Mutation, BoolVector> flip = new BitFlipMutator();

// Swaps one set and one cleared element, so the number of set elements is unchanged.
IOperator<Mutation, BoolVector> swap = new BitSwapMutator();
```

Use `BitSwapMutator` over a fixed cardinality space. Creating a run checks both questions before the run starts, as
[When validation happens](#when-validation-happens) shows.

The second check compares what the space and the operator **declare**. It does not run the operator. An operator that
declares nothing about cardinality is not reported, even if its output can change the count, and an operator whose
declaration is wrong is believed. [Declarations are claims, so check them](#declarations-are-claims-so-check-them)
shows how to test a declaration against sample candidates.

## The three pieces

### An invariant is a property a candidate can have

`ICandidateInvariant<TCandidate>` is a named property, ordinarily a record so equality is by value:

```csharp
public sealed record BoolVectorCardinality(int Cardinality) : ICandidateInvariant<BoolVector>
{
    public string Name => $"Cardinality({Cardinality})";

    public bool Holds(BoolVector candidate) => candidate.TrueCount() == Cardinality;
}
```

`Implies` is the one member with a non-obvious job. It answers whether holding _this_ invariant is enough to guarantee another one, and defaults to value equality. Override it where one invariant is strictly stronger:

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
    public bool? Ensures(ICandidateInvariant<BoolVector> invariant) => invariant switch
    {
        BoolVectorLength or BoolVectorCardinality => true,
        _ => null
    };

    public override BoolVector MutateCandidate(
        BoolVector parent,
        IRandomNumberGenerator random,
        FixedCardinalityBoolVectorSearchSpace searchSpace) => ...;
}
```

`Ensures` is three-valued, and the third value is the important one:

| Answer  | Meaning                                                  | Effect                         |
| ------- | -------------------------------------------------------- | ------------------------------ |
| `true`  | Every candidate this operator produces has the invariant | Compatible for that invariant  |
| `false` | The output may not have it                               | Reported as an incompatibility |
| `null`  | The operator states nothing                              | Not checked                    |

So **declaring is opt-in, per invariant**. An operator that implements nothing, or answers `null`, is never reported. Adding an invariant to a search space therefore does not invalidate operators that make no claim about it.

`Ensures` describes the operator's output. For a mutator, `true` means it keeps the invariant of its input; for a creator, it means every created candidate has it.

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

## Operator type compatibility {#the-other-question-was-it-written-for-this-run-at-all}

Invariants answer what an operator _does_. A separate question comes first: can it run here at all? An operator
authored for a specific search space or problem names it in its type arguments, and the run supplies its own:

```csharp
// Written for one specific problem.
public sealed record TravellingSalesmanSpecificCrossover
    : SingleCandidateCrossover<Permutation, PermutationSearchSpace, TravelingSalesmanProblem>
{
    public override Permutation CrossParents(
        Parents<Permutation> parents,
        IRandomNumberGenerator random,
        PermutationSearchSpace searchSpace,
        TravelingSalesmanProblem problem) => ...;
}
```

Put that crossover in an algorithm running over a different permutation problem and the run is refused. The decision
uses only types, so nothing is built to reach it:

- An operator accepts a run whose search space and problem are its declared types or narrower ones. An operator written
  for `BoundedRealVectorSearchSpace` accepts a run over that space or a type derived from it.
- An operator that names no search space or problem, such as `SingleCandidateMutator<Permutation>`, accepts every run
  over its candidate type.
- Algorithms and terminators are also checked against the search state of the run.

Pre-run validation and execution apply the same type rules to configurations reached through declared parameters.
An executor can still fail during construction, and a child created at runtime outside the declared graph is
first checked for type compatibility when the run resolves it.

### A composition answers for what it resolves

Validation asks each configuration about itself and then follows the children it declares as parameters, so an
operator nested inside a composition is checked with the rest of the graph. A child is checked against its owner's
run. A composition that resolves a child under different types declares those types on the child's parameter, and
the child and everything below it are checked against them instead.

The walk reaches a child only through its declaration. A composition that leaves a child undeclared loses the check
for that child; it does not get a wrong answer. An entry of a `ReferenceListParameter` names a configuration that lives
elsewhere in the graph and is checked there. A value declared with a `SettingParameter` is data, even when its type
closes over a configuration, so it is not checked.

To declare children in your own composition, see
[Authoring a composition](/guide/extending/operator-composition#authoring-a-composition). To resolve a child under
another problem and declare that on its parameter, see
[Resolve an operator under another objective](/guide/extending/writing-operators#resolve-an-operator-under-another-objective).

## When validation happens

**Creating a run validates by default.** The whole configuration graph is checked before the run exists — both questions, everywhere they apply:

```csharp
var run = algorithm.CreateRun(problem, random);
// InvalidOperationException: The configuration cannot be used over this search space:
//   - GeneticAlgorithm`1.Mutator: BitFlipMutator was not written for an execution over
//     FixedCardinalityBoolVectorSearchSpace with FuncProblem<BoolVector, ...> producing
//     PopulationState<BoolVector>.
//   - GeneticAlgorithm`1.Mutator: BitFlipMutator does not ensure Cardinality(2), which
//     FixedCardinalityBoolVectorSearchSpace requires of its members, so its output may leave
//     the search space.
```

`RunStreaming`, `RunToCompletion` and `RunToCompletionAsync` go through `CreateRun`, so they validate too.

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

`SearchConfigurationValidation`, `ValidationReport` and `ValidationDiagnostic` are in the `HEAL.HeuristicLib.Execution` namespace. The report exposes its diagnostics as a `ValueArray<ValidationDiagnostic>`.

Every form reports **every** failure it finds rather than stopping at the first, so one pass tells you everything to fix.

::: warning Resolution does not validate
The check happens when a run is created. Resolving an executor directly through `ResolutionScope.Resolve` skips it,
just as `validate: false` does. Call `ValidateAndThrow` first when you want the same checks as `CreateRun`.
:::

## Validation scope {#what-the-walk-covers}

Validation walks the configuration graph over declared parameters, following each configuration a parameter holds or names. It therefore reaches operators nested inside compositions, and it works for algorithms and operators written outside the library that declare their parameters. A configuration that declares nothing is a leaf to the walk.

Because it reads what each configuration declares rather than building anything, it reaches operators a run would only construct much later. A cycling algorithm resolves each stage lazily, once per cycle and during the run; an operator at fault inside one of those stages is still reported before the run starts.

Diagnostics carry the path that leads to the operator:

```csharp
var pipeline = PipelineMutator.Create(new BitSwapMutator(), new BitFlipMutator());
var report = SearchConfigurationValidation.Validate(pipeline, constrained);

Console.WriteLine(report.Diagnostics.Single().Path); // PipelineMutator`1.ChildOperators[1]
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

Compatibility checking never calls `Holds`; it compares declarations. `Verify` is where `Holds` is applied to actual
output.

Write this test for any operator that declares a contract. Validation trusts every declaration, so a declared
invariant that does not hold lets an operator through that leaves the space.

## Choose the right check

| Question                                                  | Use                                                                   |
| --------------------------------------------------------- | --------------------------------------------------------------------- |
| Can this configuration be used for this problem at all?   | `CreateRun` by default, or `Validate` and `ValidateAndThrow` up front |
| Does this one candidate belong to the search space?       | `ISearchSpace.Contains`                                               |
| Does my operator's output actually keep what it declares? | `OperatorContractVerification.Verify` in a test of the operator       |

Validation refuses an incompatible configuration and never repairs a candidate. An operator that can leave its space
repairs or rejects the candidate itself. A search space that states no invariants gives the output check nothing to
check. The input check still applies: an operator that `Requires` an invariant is refused over that space, because
nothing the space states implies it.

## Next steps

- [Configurations](/guide/fundamentals/configurations) — the configuration graph that validation walks.
- [Search spaces](/guide/fundamentals/search-spaces) — the spaces these invariants describe.
- [Operators](/guide/fundamentals/operators) — choosing operators for a representation.
- [Operator composition](/guide/extending/operator-composition) — how composed operators combine their children's contracts.
