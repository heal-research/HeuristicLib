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
declaration is wrong is believed. [Declarations are claims, so check them](/extend/declaring-invariants#declarations-are-claims-so-check-them)
shows how to test a declaration against sample candidates.

## Operator invariants

The second check compares declarations. An **invariant** is a property a candidate can have. A **search space** states the invariants its members satisfy. An **operator** states which invariants its output keeps and which its input needs. [Declaring invariants](/extend/declaring-invariants) shows how to declare them for a search space or operator of your own and how to test that a declaration holds.

<InvariantCheck>

The invariants a fixed cardinality space states, against what three mutators declare about their output. An operator that declares it does not keep an invariant is refused. One that declares nothing is not reported.

</InvariantCheck>

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
        TravelingSalesmanProblem problem)
    {
        // ...
    }
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

Validation follows the children a composition declares, so nested operators are checked with the rest of the graph. [A composition answers for what it resolves](/extend/declaring-invariants#a-composition-answers-for-what-it-resolves) states how a composition declares a child resolved under other types.

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

Because it reads what each configuration declares rather than building anything, it reaches operators a run would only construct much later. A cycling algorithm resolves each stage lazily, once per cycle and during the run. An operator at fault inside one of those stages is still reported before the run starts.

Diagnostics carry the path that leads to the operator:

```csharp
var pipeline = PipelineMutator.Create(new BitSwapMutator(), new BitFlipMutator());
var report = SearchConfigurationValidation.Validate(pipeline, constrained);

Console.WriteLine(report.Diagnostics.Single().Path); // PipelineMutator`1.ChildOperators[1]
```

A composition's own failure is suppressed when a child already reported the same invariant. A composed operator inherits its children's limits, so repeating them would bury the one diagnostic naming the operator you actually have to change.

The walk runs once per validation and never during a run, so it costs nothing per iteration.

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

- [Algorithm runs](/guide/execution/running-algorithms) continues the guide with running a configuration.
- [Configurations](/guide/fundamentals/configurations) — the configuration graph that validation walks.
- [Search spaces](/guide/fundamentals/search-spaces) — the spaces these invariants describe.
- [Operators](/guide/fundamentals/operators) — choosing operators for a representation.
- [Operator composition](/guide/fundamentals/operator-composition) — how composed operators combine their children's contracts.
- [Declaring invariants](/extend/declaring-invariants) — invariants and contracts for components of your own.
