# API Ergonomics: Namespaces, Algorithm Construction, and Iteration-End Observation

## Summary

This plan defines a concrete ergonomics target for the three things that make the first `README.md` example look harder than the task it performs: a long `using` list, spelled-out generic arguments on `GeneticAlgorithm`, and an `IdentityInterceptor` that exists only so an analyzer has something to attach to.

The three are not independent. Four of the fourteen `using` directives in that example exist only to spell generic arguments or the placeholder interceptor, so fixing algorithm construction and analysis attachment removes them without any namespace change at all. The namespace work then addresses what remains.

A fourth part follows from the second rather than from the example: once construction is a factory over shared, public defaults, the algorithm builders have no job left that the factory does not do better.

Every design below was prototyped against the real library and compiled; the negative results are recorded as precisely as the positive ones, because they are permanent language constraints rather than matters of taste. One of them removed a parameter from the accepted design after measurement contradicted the reasoning behind it.

Status: Part 3 is implemented and merged into the working tree. Parts 1, 2 and 4 are settled designs that have not been started; their prototypes were reverted after measurement.

## Motivation

The first `README.md` example loads a TSPLIB instance, runs a genetic algorithm and records a quality curve. It needs fourteen `using` directives and reads like this:

```csharp
var observationPoint =
    new IdentityInterceptor<Permutation, PopulationState<Permutation>>();

var algorithm =
    new GeneticAlgorithm<Permutation, PermutationSearchSpace, TravelingSalesmanProblem>
    {
        // ...
        Interceptor = observationPoint
    };

var qualityAnalyzer = Analyzer.BestMedianWorst(observationPoint);
```

Three separate concepts leak into a task that has none of them: the candidate and search space types are already implied by the problem, the third type argument is not needed at all here, and an interceptor is a transformation concept being used as a marker object.

The target, after the first three parts of this plan:

```csharp
using HEAL.HeuristicLib;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Permutations;
using HEAL.HeuristicLib.Problems.TravelingSalesman;

var problem = new TravelingSalesmanProblem(instance.ToCoordinatesData());

var algorithm = GeneticAlgorithm.Create(
    creator: new RandomPermutationCreator(),
    crossover: new EdgeRecombinationCrossover(),
    mutator: new InversionMutator(),
    selector: TournamentSelector.For(problem, tournamentSize: 3),
    populationSize: 100,
    maximumGenerations: 500,
    mutationRate: 0.05,
    elites: 1);

var run = algorithm
    .CreateRun(problem, RandomNumberGenerator.Create(seed: 42))
    .TrackBestMedianWorst(out var qualityAnalyzer);
```

## Immediate correction, independent of everything else

`new GeneticAlgorithm<Permutation, PermutationSearchSpace>` already compiles and runs with `TravelingSalesmanProblem` today. The third type argument in the `README.md` example is unnecessary, because none of the four operators used there is bound to a specific problem type. The two-argument form is already used once in the test suite.

This is a documentation fix that needs no design work and should not wait for the rest of this plan.

## Part 1: Namespace and source layout

### Problem

Operators are grouped by role first and representation second: `Operators.Creators.PermutationCreators`, `Operators.Crossovers.PermutationCrossovers`, `Operators.Mutators.PermutationMutators`. Users select operators by representation, not by role. Nobody wants every creator in the library; they want everything that works with permutations. One concept therefore costs three `using` directives, and the genotype and search space add two more.

A second observation shapes the fix: a `using` is only needed for a type the caller actually *names*. `BestMedianWorstEntry` never appears in the example because `var` covers it. Consolidation should therefore target only the roughly twenty types users spell, not the whole public surface.

### Direction

Group user-facing namespaces by concept and audience. Keep authoring types — bases, instrumentation pairs, wrapping and multi topologies — in the detailed role namespaces where operator authors already look.

| Namespace | Contents | Audience |
| --- | --- | --- |
| `HEAL.HeuristicLib` | concrete algorithms, `RandomNumberGenerator`, `Analyzer`, run extensions | everyone |
| `HEAL.HeuristicLib.Permutations` | `Permutation`, `PermutationSearchSpace`, and every permutation operator | representation user |
| `HEAL.HeuristicLib.RealVectors`, `.IntegerVectors`, `.BoolVectors`, `.SymbolicExpressions` | same rule per representation | representation user |
| `HEAL.HeuristicLib.Operators` | representation-agnostic operators such as selectors, terminators, replacers | everyone |
| `HEAL.HeuristicLib.Problems.<Domain>` | problem, its data types and its instance loading | domain user |
| `HEAL.HeuristicLib.Operators.Creators`, `.Mutators`, … | authoring bases and instrumentation | operator author |
| `HEAL.HeuristicLib.Algorithms` | algorithm authoring bases, builders | algorithm author |

This is licensed by [developer guidelines § 9.2](../docs/contributing/developer-guidelines.md): user-facing namespaces stay focused on concepts, folders may be more detailed, and one folder need not map to one namespace.

Merging `Genotypes.Vectors`, `SearchSpaces.Vectors` and the three permutation operator namespaces into `HEAL.HeuristicLib.Permutations` was checked for name collisions: ten types, none conflicting.

### Folder rule

Folders mirror the namespace at the concept level and may add role sub-folders that do **not** become namespace segments.

```
src/HeuristicLib/
  Encodings/                                 (folder only, see below)
    Permutations/                            -> HEAL.HeuristicLib.Permutations
      Permutation.cs
      PermutationSearchSpace.cs
      Creators/RandomPermutationCreator.cs   (folder only, same namespace)
      Crossovers/EdgeRecombinationCrossover.cs
      Mutators/InversionMutator.cs
  Operators/
    Selectors/TournamentSelector.cs          -> HEAL.HeuristicLib.Operators
    Authoring/Creators/Creator.cs            -> HEAL.HeuristicLib.Operators.Creators
  Problems/TravelingSalesman/                -> HEAL.HeuristicLib.Problems.TravelingSalesman
    TravelingSalesmanProblem.cs
    InstanceLoading/TsplibTspInstanceProvider.cs   (folder only)
    Operators/                                     (problem-specific operators)
```

The rule in one sentence: **a sub-folder adds a namespace segment only when it changes the audience.** Folder depth is therefore always at least namespace depth, never less. Role sub-folders help contributors navigate source; they should not cost users a `using`.

### Grouping the representation folders

One top-level folder per representation would be real clutter in the source tree. Put them under a single parent folder — and, by the rule above, do not let that parent become a namespace segment. It does not change the audience: everything under it is still for representation users, and there is no second `Permutations` anywhere to disambiguate against.

```
src/HeuristicLib/
  Encodings/                 (folder only, no namespace segment)
    Permutations/            -> HEAL.HeuristicLib.Permutations
    RealVectors/             -> HEAL.HeuristicLib.RealVectors
    IntegerVectors/          -> HEAL.HeuristicLib.IntegerVectors
    BoolVectors/             -> HEAL.HeuristicLib.BoolVectors
    SymbolicExpressions/     -> HEAL.HeuristicLib.SymbolicExpressions
```

This is the first real test of the folder rule, and it passes it: the source tree gets the grouping it needs, and users pay nothing for it.

Naming: the glossary makes `Encoding` canonical for the representation scheme used to express domain-facing solutions as candidates, with `Representation` an accepted alias and `Genotype` an evolutionary-algorithm-flavored alias for *candidate* rather than for the scheme. The current `Genotypes/` folder is therefore the least accurate of the three for a folder that holds candidate types, search spaces and operators together. `Encodings/` is the glossary-canonical choice; `Representations/` is defensible if plain readability outweighs canonical status. That is a naming call, not a structural one, and it does not affect any namespace.

### Shared base types belong beside what shares them, not above it

`Permutation`, `RealVector`, `IntegerVector` and `BoolVector` all derive from `Vector<T>`, and the abstract `Vector` carries real machinery — broadcast-length checks used across the vector operators. That base has to live somewhere, and the tempting answer is a `Vectors/` parent with the four encodings nested inside it.

That answer breaks the scheme, because a folder cannot both bear a namespace and be transparent. If `Vectors/` owns `Vector.cs` it must map to a namespace, and then `Encodings/Vectors/Permutations/` maps to `HEAL.HeuristicLib.Vectors.Permutations`, which is the nesting the flat target was meant to avoid. Making it transparent instead leaves `Vector` in the root namespace, which is worse.

So the invariant is: **a transparent folder owns no types of its own.** It groups, and nothing else. Shared bases therefore sit in a sibling folder that does bear a namespace:

```
src/HeuristicLib/
  Encodings/                 (transparent, owns no files)
    Vectors/                 -> HEAL.HeuristicLib.Vectors      (Vector, Vector<T>)
    Permutations/            -> HEAL.HeuristicLib.Permutations
    RealVectors/             -> HEAL.HeuristicLib.RealVectors
    IntegerVectors/          -> HEAL.HeuristicLib.IntegerVectors
    BoolVectors/             -> HEAL.HeuristicLib.BoolVectors
    SymbolicExpressions/     -> HEAL.HeuristicLib.SymbolicExpressions
```

The folder tree then no longer shows that a permutation is a vector. The type declaration does, and that is the correct place for it: folders are not a type hierarchy, and a user of permutations needs `Vector` in scope only when naming it directly, which is rare enough that the extra `using` is the right price.

The invariant is also worth asserting in the architecture test, since it is what keeps the transparent-folder list from quietly becoming a second, competing namespace scheme.

### Where problems live, and what to call the data-analysis code

Two different questions hide behind `DataAnalysis`, and they should be answered separately.

**The problem type.** `SymbolicRegressionProblem` is bound to one domain, regression, and to one encoding, symbolic expressions. Problems group by domain, so it belongs in `Problems/Regression/` → `HEAL.HeuristicLib.Problems.Regression`, with `Problems/Regression/Operators/` taking `NumericParameterFittingRefiner`. The encoding binding stays visible where it already is, in the type name, exactly as [developer guidelines § 9.3](../docs/contributing/developer-guidelines.md) prescribes. A future non-symbolic regression problem becomes a sibling rather than a competing branch. The intermediate `DataAnalysis` folder simply disappears from the problem side.

**The library code.** `DataAnalysis/` is a HeuristicLab inheritance and it currently holds three unrelated things:

| contents | what it actually is |
| --- | --- |
| `DataFrame`, `Series`, `SupervisedData` | immutable tabular data primitives |
| `Statistics/` — running mean, variance, covariance, moments | numerics, no learning involved |
| `IEstimator`, `IPredictor`, `IPredictionMetric`, `Regression/`, `Inspection/` | supervised learning |

Renaming the folder wholesale keeps three concerns fused. Split them instead, and the name question mostly answers itself:

```
src/HeuristicLib/
  Data/                                    -> HEAL.HeuristicLib.Data
  Statistics/                              -> HEAL.HeuristicLib.Statistics
  Regression/                              -> HEAL.HeuristicLib.Regression
    Inspection/                            (folder only)
    Symbolic/                              (folder only)
```

**Decision: `Regression` stands on its own for now.** A `MachineLearning/` or `SupervisedLearning/` parent buys nothing while regression is the only learning task in the library, and by the folder rule it can be introduced later as a transparent folder without moving a single namespace. Deferring it costs nothing and avoids arguing the name before there is a second task to weigh it against.

**Decision: the term `DataAnalysis` leaves the codebase entirely** — folders, namespaces, and docs. It is a HeuristicLab inheritance that describes none of the three things it currently covers.

That last part is the reason this is its own step. The glossary's `## Data Analysis` section groups `Series`, `Data frame`, `Supervised data` and the predictor terms, and those headings move with the code. A `git grep -i dataanalysis` returning nothing is the acceptance criterion.

### The remaining top-level folders

The same audience rule applies to everything else. Three groups fall out: two folders need a *hoist*, two need a *split*, one needs a *rename*, and the rest stay as they are.

#### `Random/` — hoist the two types a user names

```
Random/
  RandomNumberGenerator.cs              -> HEAL.HeuristicLib
  IRandomNumberGenerator.cs (Contracts) -> HEAL.HeuristicLib
  RandomExtensions.cs, RandomEnumerable.cs, SeedSequence.cs,
  RandomProfile.cs, IRandomEngine.cs, IKeyCombiner.cs,
  RandomEngines/, KeyCombiners/, Distributions/
                                        -> HEAL.HeuristicLib.Random
```

`RandomNumberGenerator.Create(seed)` appears in every example, every test and every documentation snippet. It and its interface belong in the root namespace; engines, key combiners, distributions and seed sequences have an author audience and stay.

Deliberately *not* hoisted: `RandomExtensions`. Its members — `NextInt`, `NextDouble` and friends — are called by operator authors inside `CreateCandidate` and `MutateCandidate`, not by users configuring a run. An operator author therefore needs both usings, which is the correct outcome: hoist only what a *user* names.

#### `Algorithms/` — hoist the run entry points

`AlgorithmExtensions`, currently in `Algorithms/Algorithm.cs`, carries `CreateRun`, `Complete` and `WithMaxIterations`. Every example calls at least one of them, so it belongs in the root namespace alongside the concrete algorithms. The authoring bases — `Algorithm`, `IterativeAlgorithm`, `IAlgorithm`, `IIterativeAlgorithm` — stay in `HEAL.HeuristicLib.Algorithms`.

This is the single most effective hoist in the plan: it is what allows an example to call `.Complete(...)` without importing an authoring namespace.

#### `Execution/` — split by audience

```
Execution/
  AlgorithmRun.cs                       -> HEAL.HeuristicLib
  ExecutionInstanceRegistry.cs, IExecutionInstanceResolvable.cs,
  ExecutionInstanceResolvableExtensions.cs, ExecutionStream.cs,
  ExecutionConcurrency.cs, BatchExecution.cs
                                        -> HEAL.HeuristicLib.Execution
```

`AlgorithmRun` is what `CreateRun` returns and what users attach analyzers to. Everything else is the instance-resolution machinery that only operator and algorithm authors touch.

#### `Analysis/` — split by audience

```
Analysis/
  Analyzer.cs, Analyzer.Factory.cs      -> HEAL.HeuristicLib
  (plus TrackBestMedianWorst from Part 3)
  IAnalyzer.cs, IAnalyzerRunState.cs, AnalyzerRunState.cs,
  ObservationPlan.cs, ObservationPlanExtensions.cs, ObservationCounter.cs,
  TrialAnalyzer.cs, Quality/, Diversity/, Timing/
                                        -> HEAL.HeuristicLib.Analysis
```

`Analyzer.BestMedianWorst(...)` is the entry point users name. The analysis implementations it returns — `BestMedianWorstAnalysis` and its siblings — are reached through `var` and stay where their authors look.

#### `Optimization/` — the one substantive split

This folder currently holds five unrelated things. Splitting it also removes the collision with `Numerics/Optimization/`, where the same word means continuous local optimization:

```
Objectives/                             -> HEAL.HeuristicLib.Objectives
  Objective.cs, ObjectiveDirection.cs, ObjectiveValue.cs, ObjectiveVector.cs,
  SingleObjective.cs, MultiObjective.cs, EvaluatedCandidate.cs
  Comparers/                            (folder only)
    SingleObjectiveComparer.cs, LexicographicComparer.cs, WeightedSumComparer.cs,
    ObjectiveVectorTotalOrderComparer.cs, NoTotalOrderComparer.cs, IndexedComparer.cs
  MultiObjective/                       (folder only)
    ParetoFront.cs, CrowdingDistance.cs, HyperVolumeCalculator.cs,
    DominationCalculator.cs, DominanceRelation.cs

SearchStates/Populations/               -> HEAL.HeuristicLib.SearchStates  (folder only)
  Population.cs, IslandPopulation.cs, ISolutionLayout.cs, SingleSolutionLayout.cs
```

`ObjectiveVector` and `SingleObjective.Minimize` appear in every problem definition, so `Objectives` is a user-facing namespace and should stay one segment deep. The population and layout types describe the shape of a search state and belong beside the states that hold them.

Three types leave the folder entirely because they were never about optimization:

- `Parents<T>` is a crossover input → `HEAL.HeuristicLib.Operators`
- `ISubencodingComparable<TSearchSpace>` is a search-space concept → `HEAL.HeuristicLib.SearchSpaces`
- `DictionaryExtensions` and `DoubleExtensions` are general-purpose helpers with no domain at all. Either make them `internal`, which is what their usage suggests, or move them to a single shared utilities namespace. They should not be public members of an optimization namespace

#### `States/` — rename to match the glossary

`HEAL.HeuristicLib.SearchStates`, since the canonical term is *search state*. It stays a namespace rather than being hoisted: `PopulationState<TCandidate>` is named by terminator and interceptor authors, and after Part 3 users no longer name it at all.

#### `Numerics/` — keep, with one rename

Automatic differentiation, the operation catalog and kernels, `TensorPrimitivesEx`, `ScratchSpans` and Levenberg-Marquardt are computation machinery with an author audience. There is no overlap with `Data` (tabular containers) or `Statistics` (streaming descriptive statistics): no shared types, no shared audience, nothing to merge.

The one change is `Numerics/Optimization/LevenbergMarquardt.cs` → `Numerics/LeastSquares/`, so that `Optimization` means exactly one thing in the codebase.

#### `Collections/` and `Experiments/` — keep

`Collections` holds one type, `ValueArray`, a value-equality array used across the contracts. It is infrastructure rather than a domain, and folding it into `Data` would wrongly imply it is about tabular data. `Experiments` is a coherent concept with its own audience.

#### Priority

The hoists in `Algorithms` and `Random` and the `Execution`/`Analysis` splits are cheap and shorten every example. The `Optimization` split is the one that fixes a genuine organizational problem, and it is independent of everything else in this plan — it can be done at any time, including before the encoding work.

### Making the folder rule mechanically checkable

Because folder depth now exceeds namespace depth in two different ways — transparent parents above, role folders below — an architecture test cannot compare a namespace against its folder path directly. It needs one explicit list of **transparent** folder names that are stripped before the comparison: `Encodings`, `Authoring`, `InstanceLoading`, `Symbolic`, `Inspection` and the operator role folders `Creators`, `Crossovers`, `Mutators`, `Selectors`, `Terminators`, `Replacers`, `Refiners`, `Evaluators`, `Interceptors`.

Note which folders are deliberately *not* on it: `Vectors` under `Encodings/` and `Regression` under `MachineLearning/` both own types and both bear a namespace segment.

The test then asserts two things: that a type's namespace equals its folder path minus the transparent segments, and that no transparent folder directly contains a source file. A newly introduced folder that is neither a namespace segment nor on the list fails, which is the right default — adding a transparent folder becomes a deliberate, reviewed edit to a single list rather than a silent divergence between tree and namespace.

### This settles the problem-specific operator placement question

The backlog records three operator tiers with genuinely different reuse, all currently under `Operators/<Role>/`. The folder rule assigns each tier to where its audience looks:

- general role machinery usable with any candidate and problem stays in `Operators/`
- genotype-bound operators move to `<Representation>/<Role>/`
- problem-bound operators move to `Problems/<Domain>/Operators/`

This does revisit the recorded decision that operator role is the primary namespace grouping, which the backlog already flags as necessary rather than something to break quietly. The genotype-versus-domain naming split recorded in [developer guidelines § 9.3](../docs/contributing/developer-guidelines.md) is preserved and in fact becomes structural rather than conventional.

The rule is mechanically checkable and should become one of the first cases for the planned architecture test suite.

### Risk

`Genotypes.SymbolicExpressions` holds 75 public types. A naive merge into `HEAL.HeuristicLib.SymbolicExpressions` would produce an unusable completion list. Symbol, grammar and authoring types need to stay in a sub-namespace there; only `ExpressionTree`, the search space, the operators and `Symbols` should surface. This is the one representation where the merge needs judgment rather than mechanical application.

## Part 2: Algorithm construction and type inference

### What actually drives inference

`GeneticAlgorithm<TCandidate, TSearchSpace, TProblem>` has three type parameters, and only the operator arguments carry any of them. The problem never appears in a parameter type that mentions `TCandidate` or `TSearchSpace`, and constraints are never used for inference, so the operators are the sole source for those two.

`TProblem` is the interesting one. Every operator role is declared contravariant in it — `ICreator<TCandidate, in TSearchSpace, in TProblem>` — so each operator argument contributes an **upper** bound, while a `TProblem problem` parameter contributes a **lower** bound. Fixing then runs the standard algorithm: discard every candidate that violates a bound, then pick the unique remaining type that all others convert to.

The consequence is a proof, not a preference: **a lower bound can only widen the fixed type, never narrow it.** The final step always selects the type all candidates convert to, and the operator upper bound is by construction at least as wide as the problem argument. Passing the problem cannot pin `TProblem`.

The same algorithm explains why `TSearchSpace` behaves in the opposite direction. With only upper bounds — `RandomPermutationCreator` requires `PermutationSearchSpace`, `EdgeRecombinationCrossover` only `ISearchSpace<Permutation>` — every candidate that some other candidate cannot convert to is discarded first, leaving the *narrowest* survivor. There is no inconsistency; one type parameter has a lower bound in play and the other does not.

Measured against the real library with the factory in place:

| call | inferred `TSearchSpace` | inferred `TProblem` |
| --- | --- | --- |
| `Create(creator, crossover, mutator)`, all-generic operators | `PermutationSearchSpace` | `IProblem<Permutation, PermutationSearchSpace>` |
| `Create(problem, creator, crossover, mutator)`, same operators | `PermutationSearchSpace` | `IProblem<Permutation, PermutationSearchSpace>` — **the problem changed nothing** |
| `Create(creator, crossover, tspBoundMutator)`, no problem anywhere | `PermutationSearchSpace` | `TravelingSalesmanProblem` |

### The problem argument does not belong in the signature

This settles the open question directly: the factory does not need a problem instance, because the problem instance was never doing the work. What pins `TProblem` to a concrete problem type is a **problem-bound operator argument**, and that works with no problem instance in scope at all. An algorithm can therefore be configured once, at whatever problem type its operators demand, and run later against any matching problem.

Three ways to get the problem type you want, in order of preference:

1. Pass a problem-bound operator. `TProblem` pins to its problem type automatically.
2. Accept `IProblem<TCandidate, TSearchSpace>`. This is the right answer whenever every operator is generic, and the resulting algorithm runs against *any* problem over that search space — which is what reusable algorithm configuration means.
3. Spell all three type arguments. Needed only to pre-narrow `TProblem` for a later `with` that assigns a problem-bound operator.

Partial specification — naming `TProblem` and letting the other two infer — is not expressible in C#: type arguments to a generic method are all-or-nothing. Nesting the method in a generic type to split the specification is the `CS0699` dead end already recorded in the backlog.

Because the argument would be inert, it should not be there. The IDE agrees, at `info` severity: `IDE0060: Remove unused parameter 'problem' if it is not part of a shipped public API`. That does not fail the repository style check, which runs at `warn`, but shipping a parameter whose only documented purpose is a type hint it demonstrably does not provide is worse than the diagnostic.

**This does not invalidate the `For(problem, ...)` convention on operators.** `TournamentSelector.For(problem, tournamentSize: 3)` earns its problem argument, because there the problem is the *only* source of `TCandidate`. The general rule, and the one the planned Roslyn analyzer should encode:

> A problem or algorithm argument belongs in an inference helper only when it is the sole inference source for a type parameter that appears in the return type.

For a factory that takes operators it never is. The factory should therefore be named `Create`, replacing the existing `GeneticAlgorithm.Create`, and `For` should stay reserved for helpers where the anchor argument does real work.

The fully defaulted overloads under [Encoding and problem defaults](#encoding-and-problem-defaults) do satisfy the rule, and they satisfy it rather than excepting it. Their parameters are typed `IProblem<TCandidate, TSearchSpace>` and `IProblemDefaults<TProblem, TCandidate, TSearchSpace>`, so the type parameters appear in the argument's type where inference can reach them. The distinction to hold on to:

| parameter | what infers from it |
| --- | --- |
| `TProblem problem` | nothing — `TCandidate` and `TSearchSpace` sit in constraint position, and `TProblem` loses to the operators' upper bound |
| `IProblem<TCandidate, TSearchSpace> problem` | `TCandidate` and `TSearchSpace` |
| `IProblemDefaults<TProblem, TCandidate, TSearchSpace> problem` | all three, including the concrete problem type |

### Why `with` cannot substitute for parameters

A factory that takes only the required operators and leaves the rest to `with` does not work, for three stacked reasons.

**The type is already fixed by the time `with` runs.** With all-generic operators the factory returns `GeneticAlgorithm<…, IProblem<…>>`, per the table above.

**`with` cannot change a type.** `x with { … }` always has the type of `x`. No `with` expression can narrow a generic argument.

**Narrowing would be unsound anyway.** On that type, `Refiner` has type `IRefiner<…, IProblem<…>>`, while `NumericParameterFittingRefiner` is an `IRefiner<…, SymbolicRegressionProblem>`. Contravariance runs the other way: a refiner accepting *any* problem may fill a slot needing a specific one, never the reverse. A refiner that requires `SymbolicRegressionProblem` genuinely cannot be handed an arbitrary `IProblem`. This is a real type error, not an inference artifact, and it is why every member whose type mentions `TProblem` has to be a factory parameter.

### Chosen signature

Every member whose type mentions `TProblem` is a parameter. Scalars keep compile-time defaults; operators are nullable and fall back to the shared defaults.

```csharp
public static GeneticAlgorithm<TCandidate, TSearchSpace, TProblem> Create<TCandidate, TSearchSpace, TProblem>(
    ICreator<TCandidate, TSearchSpace, TProblem> creator,
    ICrossover<TCandidate, TSearchSpace, TProblem> crossover,
    IMutator<TCandidate, TSearchSpace, TProblem> mutator,
    ISelector<TCandidate, TSearchSpace, TProblem>? selector = null,
    IEvaluator<TCandidate, TSearchSpace, TProblem>? evaluator = null,
    IRefiner<TCandidate, TSearchSpace, TProblem>? refiner = null,
    ITerminator<TCandidate, TSearchSpace, TProblem, PopulationState<TCandidate>>? terminator = null,
    IInterceptor<TCandidate, TSearchSpace, TProblem, PopulationState<TCandidate>>? interceptor = null,
    int populationSize = GeneticAlgorithmDefaults.PopulationSize,
    int? maximumGenerations = null,
    double mutationRate = GeneticAlgorithmDefaults.MutationRate,
    int elites = GeneticAlgorithmDefaults.Elites)
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
```

Named arguments keep long call sites readable, and an omitted argument contributes no bound, so omitting an operator correctly widens `TProblem` while supplying one correctly pins it.

The object-initializer style stated in the design goals is not displaced. `with` still works for scalar tweaks and parameter sweeps; it simply cannot change a type argument.

### Shared defaults

Nullable operator parameters need a fallback, and that fallback must be the same value the record property uses. It currently is not: `GeneticAlgorithm.MutationRate` defaults to `0.1` and `GeneticAlgorithmBuilder.MutationRate` to `0.05`. That drift is exactly what a third construction path would compound.

One public static class per algorithm, holding constants for scalars and generic static methods for operators:

```csharp
public static class GeneticAlgorithmDefaults
{
    public const int PopulationSize = 100;
    public const double MutationRate = 0.1;
    public const int Elites = 1;
    public const int TournamentSize = 2;

    public static ISelector<TCandidate, TSearchSpace, TProblem> Selector<TCandidate, TSearchSpace, TProblem>()
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace> =>
        new TournamentSelector<TCandidate>(TournamentSize);

    public static IEvaluator<TCandidate, TSearchSpace, TProblem> Evaluator<TCandidate, TSearchSpace, TProblem>()
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace> =>
        new ProblemEvaluator<TCandidate, TSearchSpace, TProblem>();
}
```

Both forms were verified to work in all three positions that need them:

- as method parameter defaults, which requires the scalars to be `const` rather than `static readonly`
- as record property initializers inside the generic algorithm record, including the generic operator calls
- as the `??` fallback inside the factory

Making the defaults public is not incidental. It is what lets a caller wrap a default instead of replacing it, which is the one thing builders currently offer that a factory otherwise would not — see Part 4.

`TournamentSelector<TCandidate>` derives from the one-arity `StatelessSelector<TCandidate>`, whose interface is contravariant in search space and problem, so a single stored default converts to every `ISelector<TCandidate, TSearchSpace, TProblem>` the factory needs.

### Encoding and problem defaults

A shared default is only useful if it can be the *right* default. `TournamentSelector` is genuinely encoding-agnostic, but a crossover is not — and it is often not even encoding-determined. Edge recombination is a reasonable default for permutations in general; for a travelling salesman problem an order crossover is the better starting point. The mechanism therefore has to let a problem override what its encoding suggests.

**Keying defaults on `TProblem` does not work**, and not only for dependency reasons. The measurement earlier in this part is decisive: with all-generic operators, `TProblem` infers to `IProblem<Permutation, PermutationSearchSpace>`, not to `TravelingSalesmanProblem`. A `typeof(TProblem)` switch would fail to match in exactly the case it was written for, then start matching once some unrelated operator happened to pin the type. Silent, inference-dependent behavior changes are the opposite of a pit of success.

**Carrying the defaults on the types that know them works, and it was verified end to end** against the real `PermutationSearchSpace` and `TravelingSalesmanProblem`.

#### Chosen design: static methods on both sides

Both the encoding and the problem declare their defaults as **static methods taking the instance they describe**. The whole design was prototyped against the real `PermutationSearchSpace`, `TravelingSalesmanProblem` and `QuadraticAssignmentProblem`, and the full core and API usage suites pass with it in place.

Static, because a static member is not a field and therefore can never enter a record's generated `Equals` or `GetHashCode` — enforced by the language rather than by a convention an implementer has to remember. Taking the instance, because defaults are frequently value dependent: a short permutation wants a different mutator than a long one, and a small travelling salesman instance wants a different crossover than a large one.

**Encoding side — one interface per role, required:**

```csharp
public interface IEncodingDefaultCreator<TCandidate, TSearchSpace>
    where TSearchSpace : class, ISearchSpace<TCandidate>
{
    static abstract ICreator<TCandidate, TSearchSpace, IProblem<TCandidate, TSearchSpace>> GetDefaultCreator(TSearchSpace searchSpace);
}

public interface IEncodingDefaultCrossover<TCandidate, TSearchSpace> // GetDefaultCrossover(TSearchSpace)
public interface IEncodingDefaultMutator<TCandidate, TSearchSpace>   // GetDefaultMutator(TSearchSpace)
```

Separate interfaces per role are not stylistic here: they let each algorithm constrain exactly what it needs. A genetic algorithm requires all three; a hill climber requires only creator and mutator, and stays usable with an encoding that has no meaningful default crossover.

```csharp
public record PermutationSearchSpace(int Length)
    : SearchSpace<Permutation>,
      IEncodingDefaultCreator<Permutation, PermutationSearchSpace>,
      IEncodingDefaultCrossover<Permutation, PermutationSearchSpace>,
      IEncodingDefaultMutator<Permutation, PermutationSearchSpace>
{
    public static ICreator<…> GetDefaultCreator(PermutationSearchSpace searchSpace) =>
        new RandomPermutationCreator();

    public static ICrossover<…> GetDefaultCrossover(PermutationSearchSpace searchSpace) =>
        new EdgeRecombinationCrossover();

    public static IMutator<…> GetDefaultMutator(PermutationSearchSpace searchSpace) =>
        searchSpace.Length < 20 ? new SwapMutator() : new InversionMutator();
}
```

**Problem side — one interface, every role optional:**

```csharp
public interface IProblemDefaults<TSelf, TCandidate, TSearchSpace> : IProblem<TCandidate, TSearchSpace>
    where TSelf : class, IProblemDefaults<TSelf, TCandidate, TSearchSpace>
    where TSearchSpace : class, ISearchSpace<TCandidate>
{
    static virtual ICreator<TCandidate, TSearchSpace, IProblem<TCandidate, TSearchSpace>>? GetDefaultCreator(TSelf problem) => null;
    static virtual ICrossover<TCandidate, TSearchSpace, IProblem<TCandidate, TSearchSpace>>? GetDefaultCrossover(TSelf problem) => null;
    static virtual IMutator<TCandidate, TSearchSpace, IProblem<TCandidate, TSearchSpace>>? GetDefaultMutator(TSelf problem) => null;
}
```

```csharp
public class TravelingSalesmanProblem(ITravelingSalesmanProblemData problemData)
    : PermutationProblem(SingleObjective.Minimize, GetEncoding(problemData)),
      IProblemDefaults<TravelingSalesmanProblem, Permutation, PermutationSearchSpace>
{
    public static ICrossover<…>? GetDefaultCrossover(TravelingSalesmanProblem problem) =>
        problem.SearchSpace.Length >= 20 ? new OrderCrossover() : null;
}
```

Three things make this work, and each is worth stating because none is obvious:

- **`static virtual` with a default body**, not `static abstract`. A problem implements only the roles it has an opinion about; the rest inherit the `null` default. Without this, per-role opt-in would need one interface per role on the problem side too, and the factory would need an overload for every combination.
- **The self type is what makes `TProblem` inferable.** A parameter typed `IProblemDefaults<TProblem, TCandidate, TSearchSpace>` puts all three type parameters in the argument's type, where inference can reach them. A plain `TProblem problem` parameter leaves `TCandidate` and `TSearchSpace` in constraint position, and constraints are never inferred — that is the reason the earlier `For(problem, …)` shape could not work.
- **`null` is the opt-out, not the absence of an implementation.** A problem can decline per instance rather than per type, which is what makes `Tsp(30)` take `OrderCrossover` while `Tsp(8)` falls through to the encoding's `EdgeRecombinationCrossover`.

#### The two factory overloads

```csharp
// Problems that state no defaults.
public static GeneticAlgorithm<TCandidate, TSearchSpace, IProblem<TCandidate, TSearchSpace>> Create<TCandidate, TSearchSpace>(
    IProblem<TCandidate, TSearchSpace> problem,
    int populationSize = GeneticAlgorithmDefaults.PopulationSize,
    int? maximumGenerations = null,
    double mutationRate = GeneticAlgorithmDefaults.MutationRate,
    int elites = GeneticAlgorithmDefaults.Elites)
    where TSearchSpace : class, ISearchSpace<TCandidate>,
                         IEncodingDefaultCreator<TCandidate, TSearchSpace>,
                         IEncodingDefaultCrossover<TCandidate, TSearchSpace>,
                         IEncodingDefaultMutator<TCandidate, TSearchSpace> =>
    new()
    {
        Creator = TSearchSpace.GetDefaultCreator(problem.SearchSpace),
        Crossover = TSearchSpace.GetDefaultCrossover(problem.SearchSpace),
        Mutator = TSearchSpace.GetDefaultMutator(problem.SearchSpace),
        Selector = GeneticAlgorithmDefaults.Selector<TCandidate, TSearchSpace, IProblem<TCandidate, TSearchSpace>>(),
        PopulationSize = populationSize,
        MaximumGenerations = maximumGenerations,
        MutationRate = mutationRate,
        Elites = elites
    };

// Problems that do. TProblem pins to the concrete problem type.
public static GeneticAlgorithm<TCandidate, TSearchSpace, TProblem> Create<TProblem, TCandidate, TSearchSpace>(
    IProblemDefaults<TProblem, TCandidate, TSearchSpace> problem,
    …same scalars…)
    where TProblem : class, IProblemDefaults<TProblem, TCandidate, TSearchSpace>
    where TSearchSpace : class, ISearchSpace<TCandidate>, IEncodingDefaultCreator<…>, IEncodingDefaultCrossover<…>, IEncodingDefaultMutator<…>
{
    var searchSpace = problem.SearchSpace;
    var self = problem as TProblem;

    return new()
    {
        Creator = (self is null ? null : TProblem.GetDefaultCreator(self)) ?? TSearchSpace.GetDefaultCreator(searchSpace),
        Crossover = (self is null ? null : TProblem.GetDefaultCrossover(self)) ?? TSearchSpace.GetDefaultCrossover(searchSpace),
        Mutator = (self is null ? null : TProblem.GetDefaultMutator(self)) ?? TSearchSpace.GetDefaultMutator(searchSpace),
        Selector = GeneticAlgorithmDefaults.Selector<TCandidate, TSearchSpace, TProblem>(),
        …
    };
}
```

Read the resolution as: *ask the problem; if it has no opinion for this instance, ask the encoding.* The encoding constraint guarantees a non-null fallback, so the `??` chain can never fail.

`problem as TProblem` rather than `(TProblem)problem` is deliberate. The parameter is interface-typed, so the concrete type has to be recovered, and the CRTP constraint makes that correct only *by convention* — nothing stops a type from declaring `IProblemDefaults<SomeOtherType, …>`. The pattern form degrades to the encoding defaults instead of throwing at runtime. Whether to instead reject the mis-declaration with an analyzer is worth deciding during implementation; the two are complementary.

Overload selection needs no help: a problem implementing `IProblemDefaults<,,>` binds to the second overload because its parameter type is more derived, and one that does not binds to the first. Verified for `TravelingSalesmanProblem` and `QuadraticAssignmentProblem` respectively, with no ambiguity error and no explicit type arguments at either call site.

#### Implementation order for this piece

1. Add the four interfaces to `HeuristicLib.Contracts` under `Operators/Defaults/`, namespace `HEAL.HeuristicLib.Operators`. Nothing else is needed: there is no helper class, no registry and no configuration behind any of this.
2. Implement the three encoding interfaces on `PermutationSearchSpace` first, since it is the case the `README.md` example exercises. Return the operators that documentation already recommends.
3. Add the two `GeneticAlgorithm.Create(problem, …)` overloads.
4. Pin both call shapes with API usage specs — the encoding-only path and the problem-override path — since this is intended public syntax and type inference.
5. Roll the encoding interfaces out to the remaining search spaces. Each is independent, and an encoding that implements none simply does not offer the overload.
6. Add problem defaults only where a real recommendation exists. `TravelingSalesmanProblem` is the motivating case; most problems should implement nothing.

#### Overriding one operator: use `with`, not an optional parameter

The obvious extension — adding `ICrossover<…>? crossover = null` to the factory — was prototyped and **fails**, in a way worth recording because it looks like it should work.

Passing `crossover: new EdgeRecombinationCrossover()` adds an upper bound on `TSearchSpace` at `ISearchSpace<Permutation>`, because that crossover is declared at the one-arity base. Fixing then widens `TSearchSpace` from `PermutationSearchSpace` to `ISearchSpace<Permutation>`, the constraint `TSearchSpace : IDefaultCreator<…>` no longer holds, and the call fails with `CS0311` pointing at the defaults interface rather than at the crossover. Worse, it fails *conditionally*: an operator declared at the concrete search space, such as `OrderCrossover`, would have compiled fine. An optional parameter whose presence sometimes breaks an unrelated constraint is not an API worth shipping.

`with` has none of these problems, because it changes no type argument — only the operator in a slot, which contravariance already permits:

```csharp
var algorithm = GeneticAlgorithm.Create(problem, populationSize: 100)
    with { Crossover = new EdgeRecombinationCrossover() };
```

Verified to compile and run. This is a clean division of labour between the two factory overloads: `Create(problem, …)` is the zero-configuration entry point with `with` for touch-ups, and `Create(creator, crossover, mutator, …)` is the fully explicit form that also pins `TProblem` for problem-bound operators.

#### Decisions and hazards

- **Record equality is handled by the static declaration**, not by convention. An earlier prototype used instance auto-properties and passed the full core suite, but only because every default operator happens to be a parameterless record with value equality. A `static abstract` method removes the question rather than answering it, and unlike a static property it still allows the default to depend on the search space's values.
- **Opting in is per search space and per role.** An encoding that does not implement the three interfaces a genetic algorithm needs simply does not offer its `Create(problem, ...)` overload, and the compiler says so at the call site. That is the right failure mode, but the overload is then invisible for that encoding, which needs documenting.
- **Defaults are public API.** Changing `PermutationSearchSpace.DefaultCrossover` later changes results for everyone who took the default. Document them as *suggested starting points*, and consider whether reproducibility metadata should record which operators were defaulted rather than chosen.
- **Scope.** Creator, crossover and mutator are encoding- or problem-bound and belong here. Selector, terminator and evaluator are not, and stay in `GeneticAlgorithmDefaults`. Each default lives in the type that actually knows the answer.
- **Precedence is fixed at problem over encoding**, with the problem's `null` return as the delegation path back to the encoding. That covers the case a fixed precedence would otherwise block, and it makes the decision value dependent rather than type dependent.

### Sub-decision to settle during implementation

`PopulationSize` and `Selector` are `required` on the `GeneticAlgorithm` record but defaulted on `GeneticAlgorithmBuilder`. Once defaults are centralized, the record can initialize both from `GeneticAlgorithmDefaults` and drop `required`, which makes direct object-initializer construction as forgiving as the factory. This predates the plan and should be decided either way rather than carried into a third form.

Apply the same shape to the other concrete algorithms once it is settled for `GeneticAlgorithm`.

## Part 3: Iteration-end observation

Status: **implemented.** See [Part 3 as built](#part-3-as-built) for what changed against the design below.

### Problem

Attaching a quality analyzer currently requires constructing an `IdentityInterceptor` and assigning it to the algorithm, purely so the observation plan has a reference-identity key to attach an observer to. This forces users to learn a transformation concept in order to perform a read-only capture, and it makes the candidate type appear twice for one idea.

What is actually wanted is an observation point at the end of each iteration, carrying the final state after any interceptor has run — that is, exactly the state the run streams.

### Direction

Make the algorithm itself a legal observation anchor. This is already sanctioned by the glossary, which defines an observation as a read-only capture at a defined *algorithm or operator* boundary.

The only obstacle was that `ObservationPlan.Observe` was written to require `IOperator<…>`. `IAlgorithm` is already an `IExecutionInstanceResolvable<IAlgorithmInstance<…>>`, and `ExecutionInstanceRegistry.RegisterReplacement` already accepts any resolvable. Relaxing two constraints and widening the dictionary key to the existing non-generic `IExecutionInstanceResolvable` is the whole enabling change:

```csharp
where TOperator : class, IExecutionInstanceResolvable<TExecutionInstance>
where TExecutionInstance : class, IExecutionInstance
```

An `ObservableAlgorithm` wrapper then mirrors the existing `ObservableInterceptor` exactly: it wraps `RunStreamingAsync` and notifies observers for each yielded state. Because the wrapper sits outside the algorithm instance, it observes precisely what the async stream returns, after interception.

```csharp
var qualityAnalyzer = Analyzer.BestMedianWorst(algorithm);
var run = algorithm.CreateRun(problem, rng).WithAnalyzer(qualityAnalyzer);
```

This reuses the observation plan, registry replacement and multi-observer merging rather than introducing a second mechanism, and it composes with `ExperimentRun.WithAnalyzer`.

### Semantics and caveats

- The wrapper observes only **yielded** states. Sub-iterations an algorithm does not yield are not observed. This is the correct meaning of iteration end and should be documented as such.
- Anchoring on an inner algorithm of a meta-algorithm observes that inner loop. This is a feature and should be shown as one.
- **Stale-anchor footgun.** The anchor is a reference. `Analyzer.BestMedianWorst(algorithm)` followed by running `algorithm with { PopulationSize = 200 }` silently observes nothing, because the copy is a different anchor object. Interceptors do not have this problem, since `with` copies the interceptor reference along. The mitigation is to resolve the anchor at attach time from the run, and to make that the documented form:

  ```csharp
  var run = algorithm.CreateRun(problem, rng).TrackBestMedianWorst(out var qualityAnalyzer);
  ```

  `Analyzer.BestMedianWorst(algorithm)` stays available for the explicit case.
- The operator-anchored path remains necessary and unchanged. Selection pressure, evaluation counts and crossover statistics are observations inside operators and cannot be derived from search states. This part adds an anchor; it removes nothing.

### Part 3 as built

The enabling change was exactly as designed: two constraints relaxed on `ObservationPlan.Observe` and the dictionary key widened to `IExecutionInstanceResolvable`. The generic parameter was renamed `TOperator` to `TAnchor` to match what it now accepts.

Shipped alongside it:

- `ObservableAlgorithm` with `IAlgorithmObserver`, `ActionAlgorithmObserver` and `ObserveWith`, mirroring the interceptor file.
- `ObservationPlan.Observe` overloads for algorithm anchors.
- `BestMedianWorstAnalysis` gained an `Algorithms` anchor collection. The type was converted off its positional `params IInterceptor<…>[] Interceptor` parameter to a `params IReadOnlyList<…>` constructor with `ValueArray<…> Interceptors` and `Algorithms` properties, per [developer guidelines § 5.2 and § 5.4](../docs/contributing/developer-guidelines.md). The array-typed positional parameter gave the record reference equality over its anchors, which was already wrong before this part. The other five interceptor-anchored analyses were **not** converted; three of them (`AllPopulationsAnalysis`, `AlleleFrequencyAnalyzer`, `PopulationSimilarityAnalyzer`) still carry the same array-in-record equality defect. That asymmetry, and the rest of what this part uncovered in the analysis subsystem, is now tracked as its own backlog item under [Still not taken care of](developer-backlog.md#still-not-taken-care-of), sequenced after Part 2 and before the Part 1 namespace move.
- `Analyzer.BestMedianWorst(params IAlgorithm<…>[])` and `AlgorithmRun.TrackBestMedianWorst(out var analyzer)`.

**One design claim did not hold as written.** The plan states that anchoring on an inner algorithm of a meta-algorithm observes that inner loop, and lists it as a feature to demonstrate. It did not work, and the reason was outside the observation code: `CycleAlgorithm` and `PipelineAlgorithm` called `algorithm.CreateExecutionInstance(childRegistry)` directly instead of `childRegistry.Resolve(algorithm)`, so registered replacements were never consulted. The other four meta-algorithms already resolved through the registry.

This was invisible before this part, because an algorithm could not be an observation anchor at all. Both call sites now resolve through the registry, which preserves the existing instance-per-cycle semantics — a fresh child registry per call still yields a fresh instance — and makes the inner-loop anchor work. The spec covering it is `AnchoringOnAnInnerAlgorithm_ObservesThatInnerLoop`.

One behavior change went in alongside the convention fix: `BestMedianWorstAnalysis` recorded `null!` into a non-nullable `List<BestMedianWorstEntry<TCandidate>>` when it observed an empty population, which surfaces as a `NullReferenceException` far from the cause and contradicts the documented `qualityCurve[^1].Best` usage. It now throws with the same message its sibling `BestMedianWorstPerEvaluationAnalysisState` already used for the same situation.

A user-facing guide for meta-algorithm authors was added at `docs/guide/extending/writing-meta-algorithms.md`, with a warning box stating the registry rule and the fact that `HLib0001` does not cover it. The analyzer itself was deliberately left alone: writing meta-algorithms is an advanced topic, and the documented rule was judged sufficient.

Validation: `dotnet format` whitespace, style and analyzers all clean; `HeuristicLib.Tests` 2016 passed, `ApiUsageSpecs` 129 passed, `Tests.Experimental` 128 passed, `Tests.Scenarios` 23 passed.

## Part 4: Retiring the algorithm builders

### Finding

With the factory in place, the builders have no remaining job. Their only substantive consumer is `PythonInterop`, and it uses them for exactly one thing — assigning optional configuration conditionally:

```csharp
var ga = GeneticAlgorithm.GetBuilder<…>(parameters.Creator!, parameters.Crossover!, parameters.Mutator!);
ga.PopulationSize = parameters.PopulationSize;
if (parameters.Selector != null) { ga.Selector = parameters.Selector; }
var gaAlgorithm = ga.Build() with { Refiner = refiner };
```

Nullable factory parameters express that directly, because `null` already means *use the default*:

```csharp
var gaAlgorithm = GeneticAlgorithm.Create(
    parameters.Creator!, parameters.Crossover!, parameters.Mutator!,
    selector: parameters.Selector,
    refiner: refiner,
    populationSize: parameters.PopulationSize,
    mutationRate: parameters.MutationRate,
    elites: parameters.Elites);
```

The same holds for the conditional crossover in the evolution-strategy branch, which becomes `crossover: parameters.WithCrossover ? parameters.Crossover : null`.

The remaining builder use, in the meta-optimization scenario, reads a default off the builder and wraps it:

```csharp
hc.Evaluator = hc.Evaluator.AsRepeated(11, ObjectiveVectorAggregation.Median).WithCache();
```

Records already support read-then-wrap after construction, so this becomes `algorithm with { Evaluator = algorithm.Evaluator.AsRepeated(…).WithCache() }` — or, when the wrapped default is wanted at the call site, `HillClimberDefaults.Evaluator<…>()`. This is why the defaults classes in Part 2 are public.

The `IBuilderWith*` capability interfaces are implemented by four builders and consumed generically by nothing: no code in the repository constrains a type parameter on them or dispatches through them. They are an abstraction with no reader.

### Recommendation

Retire the builders, and treat the factory as their replacement rather than as a fourth parallel style. Sequence:

1. Add `Create` and the defaults class for `GeneticAlgorithm` (Part 2). Builders keep working.
2. Add both for the remaining concrete algorithms.
3. Port `PythonInterop` and the meta-optimization scenario to `Create`.
4. Delete `GetBuilder`, the concrete builders, `AlgorithmBuilder`, `IAlgorithmBuilder` and `BuilderCapabilities`, along with `GeneticAlgorithmBuilderTests`.

Steps 1 and 2 are additive. Only step 4 is breaking, and by then nothing in the repository depends on what it removes.

### What is genuinely lost

Mutable, statement-by-statement configuration spread across a long method body. That is the style `PythonInterop` uses today, and the port makes those call sites shorter rather than longer. If a future caller needs incremental configuration, `with` provides it without a separate type.

This closes the backlog question about whether builders should become polished and first-class or be phased out, in favor of phasing them out.

## Implementation order

Each step is independently shippable and independently valuable.

1. Fix the `README.md` example to drop the unnecessary third type argument. No code change. **Still outstanding** — the example was edited for Part 3 but its type arguments were left alone. Re-verified against the current tree: the full README shape, including `TournamentSelector.For(problem, 3)` and `CreateRun(problem, …)` with a concrete `TravelingSalesmanProblem`, compiles and runs as `GeneticAlgorithm<Permutation, PermutationSearchSpace>`.
2. ~~Add the iteration-end observation anchor (Part 3) and `TrackBestMedianWorst`.~~ **Done.** The placeholder interceptor is gone from `README.md`, the travelling salesperson example and the observability guide.
3. Centralize `GeneticAlgorithm` defaults and resolve the required-versus-defaulted inconsistency (Part 2). Fixes the existing `0.1` versus `0.05` drift on its own.
4. Add `GeneticAlgorithm.Create` on top of those defaults, replacing the current `Create`.
5. Extend the defaults-plus-factory shape to the remaining concrete algorithms.
6. Port `PythonInterop` and the meta-optimization scenario off builders, then delete the builder types (Part 4).
7. Namespace consolidation (Part 1), starting with `Permutations` as the smallest complete case and leaving `SymbolicExpressions` for last.
8. Folder moves following the namespace rule, plus the architecture test, its transparent-folder list, and the assertion that transparent folders own no types.
9. Split `DataAnalysis` into `Data`, `Statistics` and `Regression`, move `SymbolicRegressionProblem` to `Problems/Regression/`, move the matching glossary headings, and remove the term `DataAnalysis` from the repository. Separate branch.
10. Hoist `AlgorithmExtensions` and `RandomNumberGenerator` to the root namespace, and split `Execution` and `Analysis` by audience. Cheap, and it shortens every example.
11. Split `Optimization` into `Objectives` and the population types, move `Parents`, `ISubencodingComparable` and the two stray extension classes to where they belong, rename `States` to `SearchStates`, and rename `Numerics/Optimization` to `Numerics/LeastSquares`. Independent of everything else here.

Steps 2 through 4 together bring the example from fourteen `using` directives to ten with no namespace work. Step 7 brings it to four.

Steps 7 and 8 are a large mechanical move touching most of `Operators/`, and the backlog already notes that namespace and folder cleanup should stay separate from glossary renaming. They are the natural candidate for their own branch.

## Validation performed

Part 3 is implemented and committed; its results are recorded under [Part 3 as built](#part-3-as-built) and repeated below. Everything else was prototyped against the real library and then reverted, so no Part 1, 2 or 4 code is in the tree.

Algorithm construction:

- The two-argument `GeneticAlgorithm` form compiles and runs with `TravelingSalesmanProblem`.
- The full-parameter factory works, including nullable operator fallbacks and scalar defaults, and runs to completion.
- With all-generic permutation operators and **no problem argument**, inference yields `GeneticAlgorithm<Permutation, PermutationSearchSpace, IProblem<Permutation, PermutationSearchSpace>>`, and that algorithm completes against a concrete `TravelingSalesmanProblem`.
- Adding the problem as a first argument produces the identical type. This is the measurement that removed the parameter from the design.
- A problem-bound mutator, with no problem instance in scope, pins `TProblem` to `TravelingSalesmanProblem` exactly, verified by assignment to an explicitly typed local.
- Explicitly spelling all three type arguments works with no problem instance.
- `Create(...)` followed by `with { Refiner = problemBoundRefiner }` fails with `CS0029`, as analyzed.
- `IDE0060` on the inert problem parameter is reported at `info`, below the repository style check severity.

Shared defaults:

- Scalar `const` defaults work simultaneously as method parameter defaults and as record property initializers.
- Generic static default factories work as property initializers inside a generic record and as `??` fallbacks in the factory.
- Wiring the three default-operator interfaces onto the real `PermutationSearchSpace` and `TravelingSalesmanProblem`, a `Create(problem, ...)` overload spelling no operators and no type arguments resolves `OrderCrossover` from the problem and `RandomPermutationCreator` plus `InversionMutator` from the search space, and runs to completion. `QuadraticAssignmentProblem`, which overrides nothing, falls back to `EdgeRecombinationCrossover`.
- Declaring the interface members at `IProblem<TCandidate, TSearchSpace>` and relying on operator contravariance to narrow them compiles as expected.
- Adding an optional `crossover` parameter to that overload fails with `CS0311`: a crossover declared at the one-arity base widens `TSearchSpace` and breaks the defaults constraint. `with { Crossover = ... }` after the call compiles and runs.
- The full core suite passes with the defaults wired into `PermutationSearchSpace`, and record equality of the search space is unaffected.
- Static default methods on both sides compile and run against the real `PermutationSearchSpace`, `TravelingSalesmanProblem` and `QuadraticAssignmentProblem`.
- Both sides are value dependent: a search space of length 5 yields a `SwapMutator` and one of length 30 an `InversionMutator`; a 30-city travelling salesman problem yields `OrderCrossover` while an 8-city one returns `null` and falls through to the encoding's `EdgeRecombinationCrossover`.
- `static virtual` members with a `null` default body let `TravelingSalesmanProblem` override only the crossover; creator and mutator resolve from the encoding without the problem implementing anything.
- The CRTP parameter makes all three type parameters inferable, so `TProblem.GetDefaultCrossover(...)` compiles and the result types as `GeneticAlgorithm<Permutation, PermutationSearchSpace, TravelingSalesmanProblem>`. `QuadraticAssignmentProblem` binds to the other overload with no ambiguity and no explicit type arguments at either call site.
- Reaching a `static abstract` member through an instance fails with `CS0176`, which is what forces the problem's concrete type into a type parameter and therefore forces the self type.
- Record equality and hash codes of the search space are unaffected by construction rather than by accident.
- With the whole design wired in, `HeuristicLib.Tests` (2012 tests including the prototype's own) and `HeuristicLib.Tests.ApiUsageSpecs` (125 tests) both pass.

Observation:

- The algorithm-anchored analyzer produces the expected quality curve, and an interceptor that truncates a population from sixteen to five is observed as five, confirming the anchor sees the post-interception state.
- Two analyzers anchored on the same algorithm merge into one observable replacement rather than conflicting.
- A copy produced by `with` is not observed, which pins the stale-anchor hazard as tested behavior rather than a documented warning.
- Anchoring on an inner algorithm required resolving inner algorithms through the registry in `CycleAlgorithm` and `PipelineAlgorithm`; see [Part 3 as built](#part-3-as-built).

Namespaces:

- Merging `Genotypes.Vectors`, `SearchSpaces.Vectors` and the three permutation operator namespaces into one namespace produces no name collisions across the ten types involved.

Each accepted design in this plan should be pinned by an API usage spec when implemented, per [developer guidelines § 10.1](../docs/contributing/developer-guidelines.md), since all four parts are about intended public syntax and type inference.

## Related

- [developer-backlog.md](developer-backlog.md) — the open items this plan addresses, and the designs it settled as rejected
- [design-goals.md](../docs/contributing/design-goals.md) — pit of success, small coherent conceptual model
- [developer-guidelines.md](../docs/contributing/developer-guidelines.md) — § 9.2 namespace policy, § 9.3 naming for what binds a type, § 10.1 enforcement mechanisms
- [glossary.md](../docs/guide/glossary.md) — `Encoding` as the canonical term behind the representation folder naming
