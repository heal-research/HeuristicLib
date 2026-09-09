# Generic arity and usability

## Summary

Generic type arguments are HeuristicLib's compatibility mechanism. `TCandidate`, `TSearchSpace`, `TProblem` and, for stateful roles, `TSearchState` thread through every algorithm, operator role and execution instance, which is what makes it a compile-time error to hand a permutation crossover to a real vector algorithm.

The cost is arity in the user's face. A configured genetic algorithm is `GeneticAlgorithm<RealVector, RealVectorSearchSpace, TestFunctionProblem>`; a terminator carries four arguments; a cycle algorithm five; an experiment six. Newcomers report this as the entry barrier, and it is a real one.

This plan records what was measured, what is settled, and what is still open. The direction is to reduce the arity a **user** must name without moving compatibility checking out of the compiler, and to make the checking that cannot stay static fail early and explain itself.

Related: [configuration-arity-migration.md](configuration-arity-migration.md) is the migration plan that acts on this. [developer-backlog.md](developer-backlog.md#discussed-tried-and-rejected) holds the settled rejections that constrain this work. `test/HeuristicLib.Tests.ApiUsageSpecs/Usage/NoviceFrictionSpecs.cs` is the executable record of the friction and of the measurements below.

## What was measured

Each is a guard test in `NoviceFrictionSpecs`, so it fails if the situation changes.

- **The zero-argument construction path is nearly unavailable.** `GeneticAlgorithm.For(problem, …)` requires the problem to implement `IProblemDefaults<TSelf, TCandidate, TSearchSpace>`, and `TravelingSalesmanProblem` is the only problem in the library that does. Every other problem, including every problem a user writes, falls back to `Create(...)` with operators supplied or to direct construction. The failure is a `CS0411` naming all thirteen factory parameters, with no hint that the cause is a missing interface declaration on the problem.
- **`TSearchSpace` and `TProblem` carry little static information.** Every candidate type maps to exactly one search space type, except `BoolVector`, which gained `FixedCardinalityBoolVectorSearchSpace` alongside `BoolVectorSearchSpace` as the first deliberate counterexample. Exactly one operator in the core assembly binds `TProblem` to a concrete problem (`NumericParameterFittingRefiner` → `SymbolicRegressionProblem`); the rest pass it through or pin it to `IProblem<,>`.
- **The search space argument does not protect a constrained subspace.** See the finding under the reduction ladder; both directions of the subspace relation come out wrong.

The first two describe the present library, not an invariant, and neither is a licence to erase the parameter it measures. The third is a property of C#'s variance rules and does not change with the library.

## What is not the problem

- **Construction ergonomics for the supported case.** `For(...)`, `Create(...)` and the `For(problem, ...)` companions on the operator families already infer everything. Where they apply, no type argument is written.
- **Authoring arity for operators that ignore inputs.** The reduced-arity ladder already lets `InversionMutator` be written as `SingleCandidateMutator<Permutation>`.
- **The reduced-arity records as a shorthand.** `GeneticAlgorithm<TCandidate>` fixes `TSearchSpace` to `ISearchSpace<TCandidate>`, which is a *weaker* algorithm, not a shorter spelling of the common one. An operator that reads the search space cannot fill its slots, because the role interfaces declare `in TSearchSpace` and contravariance runs the other way. Adding further reduced arities does not shorten the common case.

The barrier is **naming**, not building. `var` covers a local; a field, a parameter, a return type or a collection element does not have that escape, so the arity is paid again in every calling layer.

## Decisions

### Operators keep receiving the problem at execution time

Considered and rejected: deleting `TProblem` from the role contracts entirely, so operators never receive a problem, and giving problem-bound operators their problem data as a construction-time dependency instead.

It fails on configuration reuse. A crossover that needs TSP coordinates would take the instance at construction, which forces one crossover configuration, and therefore one algorithm configuration, per problem instance. That destroys the property the record design exists for — one configuration running against many instances — and pulls arbitrary non-serializable data into value objects.

`TProblem` on the **execution** layer is what keeps configurations instance-independent, so the execution instance always carries the full set of type arguments. The single operator that binds it is evidence for the design, not against it.

This says nothing about the **configuration** layer. Whether a configuration slot has to name `TProblem` in order for its execution instance to receive one is a separate and still-open question, addressed by the reduction ladder below.

### Do not split a search space when only operator suitability differs

Considered: splitting `PermutationSearchSpace` into an absolute space (assignment, as in QAP) and a relative undirected space (tour, as in TSP), on the grounds that edge recombination and inversion are adjacency operators and the QAP has no adjacency structure.

Rejected as a search space concern. Both spaces contain exactly the same permutations; they differ in their *equivalence relation* — a tour is equal to its rotations and reflections, an assignment only to itself — and in which operators suit the objective. PMX and ERX both produce valid permutations for both problems. Splitting would make an ill-suited operator a compile error, which prohibits a legitimate experiment rather than preventing a mistake.

The rule to apply instead:

> Split a search space when **membership** or the **data operators read** differ. Use problem defaults when only **suitability** differs.

Under that rule the QAP inheriting `EdgeRecombinationCrossover` from the encoding defaults is an argument for `QuadraticAssignmentProblem` declaring its own defaults, not for a new search space.

### Validation is trial resolution, not a second rule engine

A pre-flight pass is wanted independently of the type model: `CycleAlgorithmInstance` resolves child algorithm instances per cycle, so a broken stage currently surfaces mid-run rather than at run start.

If validation grows its own notion of what fits while resolution applies a cast, the two paths drift. Validation must therefore *be* resolution: build the whole execution graph at run start, aggregating per-node failures instead of stopping at the first. There is then only one compatibility rule, and it is the one execution actually uses.

### What can leave the configuration layer: context, not subject

A type parameter moves to the execution layer when it is **context the run supplies and most operators ignore**. It stays on the configuration when it is **what the operator is written about**.

By that rule `TSearchSpace` and `TProblem` move and `TCandidate` stays. `TSearchState` was expected to stay too; implementation showed otherwise, and every role ends at one shape:

```csharp
IMutator<TCandidate>   // and creator, crossover, evaluator, selector, replacer, refiner, terminator, interceptor
```

**Not** the earlier "consumed only may move, produced must stay". That rule was read off the variance annotations, and variance is not what makes the erasure work: the mutator erasure runs through a generic creation method and a type test, not through assignability. Contravariance only decides whether that test also accepts an operator written for a *wider* type — useful, but a separate question from whether the configuration should name the parameter at all.

**Why `TCandidate` stays.**

1. **It is the wiring.** `MultiMutator<TCandidate>` holds `ValueArray<IMutator<TCandidate>>`, and `GeneticAlgorithm<TCandidate>` holds a creator, crossover, mutator and selector that must agree. Erasing `TSearchSpace` kept `TCandidate` as the tie, with declared invariants covering the residue. Erasing `TCandidate` leaves nothing to tie: a permutation mutator would drop into a real vector pipeline with no compile error, moving the most common mistake from build time to run time.
2. **The erasure ratio inverts.** The search space and problem were worth removing because most operators never read them — `AlmostNoOperatorConstrainsTheProblemTypeArgument` measures exactly that. Every operator reads and returns candidates; `NoChangeMutator<TCandidate>` is generic in the candidate, not agnostic to it.
3. **There is no execution-time hand-off to move it to.** A search space and a problem arrive as method arguments at execution. Candidate values do not arrive from the run: the creator produces them and they flow between operators.
4. **The barrier was never the first argument.** `GeneticAlgorithm<RealVector>` reads fine; `GeneticAlgorithm<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>` was the friction.

**Why `TSearchState` was expected to stay, and why it left.** The argument was that it is the terminator's and the interceptor's subject rather than context they ignore, so reason 2 above applies with full force. That was the wrong test. The right one is **whether there is a source to bind against at resolution time** — and there is: an operator never originates a search state, the algorithm it runs in does, and that algorithm is what resolves it. So both roles name only the candidate, the state arrives beside the search space and problem as a third method type argument, and a mismatch is reported when the execution graph is built.

The reasoning above about interceptor invariance still holds and simply costs less than it looked: `IInterceptorInstance` is invariant in `TSearchState`, so the run-time type test succeeds only on an exact match — the same answer the compiler used to give, one step later. What the erasure bought was inference. `terminator.And(other)` could not previously infer its state, because the state sat in an extension block's type parameters with nothing to infer it from; the same held for `Or`, `CountCalls` and `MeasureDuration`. None of them names a type argument now.

**Where the state did stay: the algorithm.** `IAlgorithm<TCandidate, TSearchState>` exists alongside `IAlgorithm<TCandidate>`, and the line between them is **produced versus consumed**. An algorithm *returns* its state — `Complete` and `Stream` mention it in their return type — so a form that does not name it cannot declare them. A terminator is *asked about* a state through an instance method parameter, and nothing on that side has a return type mentioning it, so a terminator can be written without naming it. Both facts are pinned by `AlgorithmInterfaceCapabilityTests`, which compiles thirteen expressions against each interface form.

## Decided: the reduction ladder

Execution instances always carry every type argument. The question was how many of them a **configuration** has to name.

| Step | Configuration type | Loses |
| ---- | ------------------ | ----- |
| today | `GeneticAlgorithm<RealVector, BoundedRealVectorSearchSpace, IProblem<RealVector, BoundedRealVectorSearchSpace>>` | — |
| 1 and 2, taken together | `GeneticAlgorithm<RealVector>` | assignment checking against concrete problem and search space types |
| 3 — rejected | `GeneticAlgorithm` | all static compatibility checking, plus hot-path cost |

**Steps 1 and 2 are taken as one change.** They cannot be reordered, because `TProblem` is `IProblem<TCandidate, TSearchSpace>`, so a configuration naming the problem also names the search space. And stopping between them is the worst of both: `in TSearchSpace` on the configuration interface is exactly what makes a typed registry, or the instantiation carrier as a parameter, a variance error — so step 1 alone forces transitional ceremony that step 2 removes again. The migration is planned in [configuration-arity-migration.md](configuration-arity-migration.md).

The two give up different checks, and each has its own replacement. Dropping the search space gives up the distinction between `BoolVectorSearchSpace` and `FixedCardinalityBoolVectorSearchSpace`, which is a real difference now that both exist, and that is exactly what the invariant system covers. Dropping the problem gives up the check that a problem-bound operator lands in a compatible algorithm; invariants say nothing about problems, and what catches it is the cast when the execution graph is built, surfaced early by the pre-flight pass. See also the finding below on how little the search space argument protects even today.

**Step 3 is rejected.** `TCandidate` distinguishes different *types*, which C# checks exactly, for free, at compile time. The declared invariant mechanism below distinguishes different *subsets of one type*, which C# cannot check at all. Those are complementary jobs, and only the second is beyond the type system, so erasing `TCandidate` replaces a working check with a weaker one and gains nothing the invariant mechanism does not already provide.

The cost is also categorically different from steps 1 and 2. Erasing the search space or problem costs one cast per configuration when the execution graph is built, once per run. Erasing the candidate costs work on **every operator call**: `IReadOnlyList<TCandidate>` flows covariantly into an erased parameter for free, but nothing brings an erased result back, so every operator output needs casting or wrapping on the hot path. It also removes the completion filtering that does most of the discovery work for newcomers, and saves only one short, meaningful word in the type name. Step 3 would make configuration graphs deserialize from YAML more conveniently, which is a genuine benefit, but typed configurations deserialize too with type discriminators; that interacts with the open serialization-boundary item in [developer-backlog.md](developer-backlog.md) rather than deciding this.

### How much the search space argument actually protects

Measured, not assumed, and it bears directly on step 2. The search space argument states which search space an operator **reads**, not which invariant it **preserves**, and for a constrained subspace C# answers both directions the wrong way round:

- A candidate-arity mutator such as a plain bit flip declares no search space, so contravariance lets it fill **every** space slot, including `FixedCardinalityBoolVectorSearchSpace`, whose invariant one flip destroys. Accepted, and should not be.
- `BitSwapMutator` preserves cardinality, which is stronger than the unconstrained space requires, so it is valid on `BoolVectorSearchSpace` — but it names the narrower space, and contravariance runs the other way. Rejected, and should not be.

Both are pinned in `NoviceFrictionSpecs.TheSearchSpaceArgument_DoesNotProtectAConstrainedSubspace`.

Making the constrained space derive from the unconstrained one does not fix this; contravariance would then accept the general mutator into the constrained slot even more directly. Two separate parameters, one for what is read and one for what is preserved, would express it and would add arity rather than remove it.

The underlying reason: preserving an invariant is a **postcondition on the operator's output**, and a postcondition is not a type. So the check that actually matters for a constrained space cannot live in the type system at all — it belongs in validation, or in an explicit capability declaration on the operator. This bounds what step 2 would forfeit: not a working guarantee, but a partial one that already fails against the operators most likely to break a constrained space.

### The mechanism is validated

Both halves of the step 1 shape were compiled against the real library before committing to the migration.

The configuration interface drops the problem while keeping the search space contravariant, and the creation method carries the problem instead:

```csharp
public interface IMutator<TCandidate, in TSearchSpace> : IOperator
    where TSearchSpace : class, ISearchSpace<TCandidate>
{
    IMutatorInstance<TCandidate, TSearchSpace, TProblem> CreateExecutionInstance<TProblem>(ExecutionInstanceRegistry registry)
        where TProblem : class, IProblem<TCandidate, TSearchSpace>;
}
```

This satisfies C# variance rules: `TSearchSpace` sits in a contravariant slot of the returned instance type, which is a contravariant use overall and therefore legal for `in`.

**The bridge costs nothing for almost every operator.** An author who writes a problem-agnostic instance returns `IMutatorInstance<TCandidate, TSearchSpace, IProblem<TCandidate, TSearchSpace>>`, and that converts to `IMutatorInstance<TCandidate, TSearchSpace, TProblem>` **implicitly**, by the contravariance the instance interface already declares. No cast, no runtime check, no failure mode. That covers every operator that passes the problem through, which is all but one in the library.

Only an operator written against a concrete problem needs the checked cast, and only there can resolution fail. So the runtime failure this step introduces is confined to exactly the operators that were using the type argument for something, and the type system still does the work everywhere else.

This also settles how validation checks problem compatibility: by resolving, not by a second declaration. The cast is the rule, the resolve site knows the configuration, the search space and the requested problem, and a declaration alongside it would be a parallel rule able to drift. The same generic method already carries `TSearchSpace`, so nothing new is needed when step 2 later removes it from the configuration.

### The mechanism

Configuration slots would be declared at candidate arity (`IMutator<TCandidate>`), and the configuration would produce its execution instance through a generic method supplied with the run's search space and problem:

```csharp
IMutatorInstance<TCandidate, TSearchSpace, TProblem> CreateExecutionInstance<TSearchSpace, TProblem>(
    ExecutionInstanceRegistry registry, TSearchSpace searchSpace, TProblem problem)
```

Findings so far:

- **The compatibility check relocates to run start, it does not disappear.** No C# mechanism keeps the check static while the slot type omits the search space; a blind cast, a generic creation method and a type test at the slot are the same runtime check in three positions. This is the whole content of the trade.
- **The runtime performs the check precisely.** Because the instance interfaces declare `in TSearchSpace, in TProblem`, casting the created instance to the requested closed type succeeds exactly when the variance conversion is legal. No hand-written type test is needed.
- **Performance is not affected.** Resolution happens once per run at graph construction, never on the operator path.
- **The casts live in the authoring bases.** Roughly seven role base families absorb them, written once each, rather than every operator repeating them.
- **`Resolve` keeps its type safety, but changes shape.** The instance type can no longer be a parameter of `IExecutionInstanceResolvable<TExecutionInstance>`, because there is no single instance type — there is a family parameterized by `(TSearchSpace, TProblem)`, and C# has no higher-kinded types. Role-specific overloads on the registry recover full static typing at every internal call site, with the type arguments inferred from the calling algorithm. `RegisterReplacement` needs the same treatment; check whether that helps or hinders the observation merging problem recorded in [analysis-system-rework.md](analysis-system-rework.md#observation-registration-depends-on-static-generic-types).
- **Registry caching is unaffected.** The registry's contract is that one configuration reference resolves to one execution instance within one registry, which is about identity and says nothing about problems or search spaces. That contract holds unchanged. Should a caller ever request the same configuration at two incompatible closed types, the cast on retrieval fails loudly rather than returning a wrongly typed instance, so the runtime check is the same one this design already accepts everywhere else.
- **The resulting contract is a request, not a lookup.** `Resolve` means "give me an instance for this search space and problem, or fail", with pre-flight as the way to learn about failure before work starts. The throw site knows the configuration type, the requested search space and the requested problem, so its message can be considerably better than the `CS0266` the compiler emits for the same mistake today.

### What decides it

The question reduces to how strongly compile-time safety is valued against the handling cost of the type arguments, and the honest answer differs per rung. Step 1 costs almost nothing measurable. Step 2 costs a check that, as shown above, does not hold against the operators most likely to need it. Step 3 gives up static checking as a category and buys straightforward configuration deserialization.

One asymmetry to keep in view: a difference in **membership** can be re-checked at run time through `Contains`, so erasure degrades it to a later, noisier failure. A difference in **equivalence relation**, as in absolute versus relative permutations, has no run-time check to fall back on and would have to be carried some other way — see the equivalence section below, which is why that question is not independent of this one.

`FixedCardinalityBoolVectorSearchSpace` and `BitSwapMutator` exist as the first concrete second space over an existing representation, so each rung can be prototyped against a real case rather than argued about.

## Open: where candidate equivalence lives

Surfaced by the permutation discussion.

Today equality is a property of the candidate value: `Permutation` is `IEquatable<Permutation>` over its elements, `ISearchSpace<TCandidate>` declares only `Contains`, and operators route around the question — `NoSameMatesSelector` compares objective vectors rather than candidates. The one seam is `RemoveDuplicatesInterceptor`, which takes an injected `IEqualityComparer<TCandidate>` as configuration.

Three positions were considered.

- **Equality stays on the candidate**, with an injected comparer where something else is needed, as today. Cheapest; cannot express that a tour equals its own reflection.
- **Canonical representation.** Require candidates to be in canonical form, so value equality *is* solution equality. Rejected: an operator has no way to know how to canonicalize, because canonicalization is space-specific knowledge — it would have to ask the search space, at which point the search space owns the equivalence anyway and the canonical form is pure added cost. It also imposes that cost on every operator output, including for absolute permutations where every value is already its own canonical form and the work is entirely wasted.
- **Equivalence is owned above the candidate.** Chosen direction.

Chosen direction: equivalence is a concept distinct from candidate equality, resolved through the same layering that already governs operator defaults. Candidate equality is value identity and stays where it is. Equivalence answers "do these denote the same solution here", and is asked of the **problem first, the search space second, and candidate equality last**.

Both layers have real cases, which is why neither alone is enough:

- **Space-determined.** Every problem over `FixedCardinalityBoolVectorSearchSpace` shares its notion of sameness. Declaring it per problem would duplicate it and let two problems over one space disagree with no guard.
- **Problem-determined.** A tour is equivalent to its reflection only when the distance matrix is symmetric. Symmetric and asymmetric TSP share `Permutation` and `PermutationSearchSpace` and differ in equivalence, because the objective decides it. The library does not model that distinction today, so this is forward-looking, but ATSP is a standard problem class and the case is not hypothetical.

Implementation direction:

- Express equivalence as **members with default implementations**, not as a stored `IEqualityComparer<TCandidate>` property. Search spaces are records; a stored comparer would participate in their value equality, so two behaviorally identical spaces holding different comparer instances would compare unequal, and an interface-typed property would need polymorphic serialization. Default interface members on `ISearchSpace<TCandidate>` — `AreEquivalent` and `GetEquivalenceHashCode`, both delegating to `EqualityComparer<TCandidate>.Default` — add no stored state, no serialization surface and no change to record equality. Problems override the same pair.
- Provide a thin adapter exposing the resolved equivalence as `IEqualityComparer<TCandidate>` where a dictionary or set needs one. Hashing is required, not optional: duplicate detection and evaluation caching both need it.
- Make the existing injected comparer the explicit override at the top of the chain rather than a parallel mechanism. `RemoveDuplicatesInterceptor` should consult the resolved equivalence when no comparer is supplied.
- Identify consumers before changing anything: `RemoveDuplicatesInterceptor`, the `Distinct()` calls in `ParetoFront`, diversity and genealogy analysis, and any future evaluation caching. Each already has the problem and search space in scope at execution, so reading equivalence from them costs no new plumbing.

Equivalence read from a problem or search space **value** is unaffected by how many type arguments a configuration names, so this decision is independent of the reduction ladder.

## Decision: declared invariants replace what the type system cannot express

C#'s variance cannot decide whether an operator is usable on a search space, and no subspace test can either. Compatibility becomes a declared, run-time-checked property.

### Why variance cannot reach it

An operator is a function from candidate to candidate, so it carries two separate properties, and the subset relation between spaces helps them in **opposite** directions:

- **What it accepts** is a parameter position, and therefore contravariant. Accepting more makes an operator more substitutable.
- **What it returns** is a return position, and therefore covariant. Promising less makes an operator more substitutable.

To be usable on space `S` an operator must be a valid `S -> S`, so `S` occupies a contravariant and a covariant position at once and is **invariant**. No single variance annotation can express that, which is why `in TSearchSpace` cannot be made to work whichever way it is pointed.

The deeper reason is that `BoolVectorSearchSpace` and `FixedCardinalityBoolVectorSearchSpace` are not two types. They are two **subsets of one type**, and variance relates types, not subsets of a type. Expressing them as types would need refinement types, which C# does not have. Carrying the constraint in the candidate type instead would make it an ordinary typing problem, at the price of a new candidate type per constraint and a collision with the decision in §8.3 that `TCandidate` is invariant and identifies the exact shared representation. Not pursued on current evidence.

### The target table

Three operators over two search spaces sharing one candidate representation, established exhaustively in `SearchSpaceCompatibilitySpecs` by enumerating every member of both spaces:

| Operator | Needs | Promises | on `BoolVectorSearchSpace(4)` | on `FixedCardinality(4,2)` |
| --- | --- | --- | --- | --- |
| `FlipOneBitMutator` | nothing | same length | usable | **not usable** — output leaves the space |
| `BitSwapMutator` | nothing | same length, target cardinality | usable | usable |
| `SwapSecondTrueMutator` | at least two set elements | same length, same cardinality | **not usable** — rejects valid inputs | usable |

The two failures fail **different** checks, which is the case for keeping them separate: `FlipOneBitMutator` accepts everything and returns candidates outside the space, `SwapSecondTrueMutator` returns valid candidates and rejects inputs the space contains. Neither check finds the other's failure.

Note also that the two unusable cells are in different columns, so no ordering of spaces makes operators flow one way. This table is the acceptance criterion: a declared invariant mechanism has to reproduce all six cells without running an operator.

### The mechanism

Neither side names the other, which is what keeps both directions of extension open. This is the expression problem, and an operator listing compatible spaces closes the space side while a space listing compatible operators closes the operator side.

Both describe themselves against a shared vocabulary of **named invariants**:

- A search space declares what it **requires** of a candidate. `BoolVectorSearchSpace` requires `{Length}`; `FixedCardinalityBoolVectorSearchSpace` requires `{Length, Cardinality}`.
- An operator declares what it **needs** of its input and what it **guarantees** about its output. `FlipOneBitMutator` needs `{}` and guarantees `{Length}`; `BitSwapMutator` needs `{}` and guarantees `{Length, Cardinality}`; `SwapSecondTrueMutator` needs `{AtLeastTwoSet}` and guarantees `{Length, Cardinality}`.

Compatibility is then the two checks, as two subset tests: the space's requirements must lie within the operator's guarantees, and the operator's input needs must lie within what the space guarantees about its members.

The property this buys: **a new search space works with existing operators without editing them.** A user space requiring `{Length, Cardinality}` is compatible with `BitSwapMutator` immediately, because neither type knows about the other and they meet at the invariant. A space introducing a genuinely new invariant is rejected by every existing operator, which is the correct conservative default.

### What implementation settled

The mechanism is built. Three questions were open before writing it, and building it answered them.

- **Requirements are concrete, guarantees are by kind.** This was the phrasing question, and it has a forced answer: a space states `Length(4)`, but a mutator written once cannot name the value 4. It can only state that length survives it. So `RequiredInputInvariants` holds invariant instances and is matched by entailment, while `PreservedInvariantKinds` holds invariant types and is matched by exact type. The asymmetry is not a compromise; it is what the two sides can honestly say.
- **Identity is the invariant type itself.** Any type may declare a new invariant and nothing enumerates them, so users extend the vocabulary without touching the library. `Entails` carries strength between invariants, which is how a space fixing cardinality at two satisfies an operator needing at least two.
- **Composed operators need composed contracts.** Not anticipated, and found by the first validation test: a `PipelineMutator` declaring nothing was reported as preserving nothing, which is a false positive that also buried the real diagnostic on its child. Compositions now derive their contract from their children — an invariant survives only if every child preserves it, and the composition requires whatever any child requires — and validation suppresses a composition's failure when a descendant already reported the same invariant, so the diagnostic names the operator a user has to change.
- **Declaring is opt in, per operator.** An operator that declares nothing, or declares an empty contract, is not checked: the search space it is typed against already fixes which spaces it may be used over, and that answer stands until a declaration refines it. So an operator can be written without thinking about invariants, adding invariants to a search space never invalidates operators written before it, and declaring pays off exactly where the type system gives the wrong answer — an operator usable over more spaces than its type admits, or fewer.
- **Nothing enumerates operator roles.** Checking keys on whether a contract is declared, not on which role an operator fills, so a role added by a package consumer participates on the same terms as a built-in one and a role whose output is not a candidate simply never declares.
- **A contract may depend on the operator's own parameters.** It is an ordinary property, so an operator whose guarantee holds only over part of its parameter range states it inside that range and drops it outside. Producing a candidate outside the search space then becomes a declared consequence of a parameter choice rather than a defect, and validation reports it against the configuration that caused it.
- **Declarations can lie**, so `InvariantContractVerification` runs an operator over a space and confirms every invariant it claims to preserve actually survives. A test-time tool, exhaustive where a space is small enough to enumerate.

### What adopting a second encoding found

Real vectors were the first encoding not designed alongside the mechanism, and adopting them changed it twice.

- **The output half became a question, not a list.** It started as a list of invariant types, which was both untyped and reflection-flavoured, and it could not express a guarantee whose truth depends on values. `bool? Ensures(ISearchInvariant<TCandidate>)` replaced it: the search space hands the operator each invariant it states, and the operator answers for that one — `true`, `false`, or `null` for no opinion. An operator answers by pattern where it conforms to a family without knowing values, as a mutator preserving the length it was given, and by value where it makes a concrete claim, as a creator generating within bounds it was configured with. One method replaced three members, opt-in became per invariant rather than per operator, and no `Type` appears in the API.
- **A creator forced that change.** `UniformDistributedCreator` can override the search space's bounds with its own, and whether that is legitimate depends on the space it will run over. It answers `true` for length, compares its configured bounds against the space's when both are overridden, and defers otherwise. This converts an override reaching outside the space from an exception on the first created candidate into a diagnostic at validation.
- **`RealVectorSearchSpace` became `BoundedRealVectorSearchSpace`.** The old name occupied the general slot while meaning one specific constraint, an axis-aligned box, leaving no name for an unbounded or simplex-constrained sibling. The new name follows the shape already used for bool vectors, `<Qualifier><Representation>SearchSpace`, and keeps the representation name where it belongs.
- **No bounds violation exists in the current real vector operators.** Every mutator and crossover clamps, including `AlphaBetaBlendCrossover` at an alpha outside `[0, 1]`, which extrapolates beyond the parents and is then pinned to the bounds. The hypothesis that extrapolation escapes the space was wrong, and the contracts now record that as verified rather than assumed. That operator carries a commented alternative showing the contract it would declare without the clamp, which is the worked example of a guarantee that holds only over part of a parameter range.
- **One gap is accepted deliberately.** Overriding only one of the creator's bounds leaves the other taken from the space, and the pair cannot be named without knowing that space, so bounds stay a kind there. A one sided override reaching outside the space is not reported by validation and still fails at run time, as it did before. Rejecting it would mean rejecting legitimate configurations, and a missed check is the safer default for validation.

Granularity remains the open risk and can only be judged as more spaces adopt invariants.

### What is built

| Type | Role |
| --- | --- |
| `ISearchInvariant<TCandidate>` | a named candidate property, with `IsSatisfiedBy` and `Entails` |
| `IInvariantContract<TCandidate>` | `Ensures(invariant)` answering for one invariant, plus the operator's required input invariants |
| `ISearchSpace<TCandidate>.Invariants` | what a space requires of its members; empty by default |
| `SearchSpaceCompatibility` | the two checks, returning a reason and the invariant that decided it |
| `InvariantContracts.Compose` | contract of a composed operator, derived from its children |
| `SearchConfigurationValidation` | the pre-flight walk over a configuration graph, aggregating diagnostics |
| `AlgorithmValidationExtensions` | `algorithm.Validate(problem)` and `ValidateAndThrow(problem)` |
| `InvariantContractVerification` | checks a declaration against observed behavior, for tests |

`SearchSpaceCompatibilitySpecs` asserts that declarations alone reproduce all six cells of the target table without running an operator.

Keep `IsSubspaceOf` for the question it does answer: **candidate** movement between spaces. A candidate from a constrained space is usable wherever the wider space is expected, which matters for seeding, initial states and stage boundaries in meta-algorithms. It does not answer operator usability.

## Implementation direction

Roughly in order; the items are independent.

Status: the invariant mechanism and the pre-flight validation it drives are implemented. The arity reduction is largely done — all twelve operator roles, the algorithms and the experiments have landed; see [configuration-arity-migration.md](configuration-arity-migration.md). Factories, analyzers and documentation remain, and steps 1, 4 and 5 below are the parts still open.

1. **Have the built-in problems declare their defaults.** Makes `For(problem, …)` real rather than a one-problem special case and removes the worst first-contact failure. Coordinate with the existing backlog item on choosing defaults per encoding and problem, which requires evidence per choice rather than mechanical rollout.
2. **Extend the pre-flight pass to trial resolution.** The invariant half is done: `SearchConfigurationValidation` walks the configuration graph and aggregates every incompatibility. It does not yet resolve execution instances, so a failure that only resolution would surface still appears at run start rather than at validation. Resolving the graph in the same pass is the remaining half, and it is what makes validation and execution share one rule.
3. **Attempt step 1 of the ladder**, removing `TProblem` from the configuration layer while execution instances keep it. Prototype the generic resolution mechanism on one role, most likely mutators. Measure the diff the way the evaluator return-type change was measured in the backlog: how much of it is mechanical, how much reaches behavior. Do not proceed to step 2 before step 1 has been judged on that measurement.

   The migration has green intermediate states and does not have to land in one commit. Add the low-arity role interface, the registry overloads and an adapter that presents an existing full-arity operator through the new interface; move algorithm slots to the new interface one algorithm at a time, since old operators still fit through the adapter; move the authoring bases to implement the new interface natively; then delete the adapter and the old interface. The wrinkle to watch is registry identity: an adapter is a different reference from the operator it wraps, so observation anchoring needs the adapter to delegate identity or register a replacement while the transition lasts.
4. **Fix the examples and specs**, which mostly show direct construction with full arity, and land the analyzer already on the backlog that rewrites explicit construction into the inferring factory.
5. **Document closed generic aliases** as the practitioner-level mitigation. `using RealVectorGa = GeneticAlgorithm<…>;` and the project-wide `<Using Include="…" Alias="…"/>` form both work today, cost the library nothing and preserve every static guarantee. They help a codebase with repeated signatures; they do not help a newcomer reading documentation.
6. **Decide step 2 of the ladder** only after 1–4, on the evidence step 1 produces and against the two search spaces over `BoolVector` that now exist. Step 3 is rejected.
7. **Design the invariant vocabulary**, settling identity, granularity and the phrasing of guarantees before any interface is written, and validate it against the six-cell table.

## Rejected

- **Erasing configuration types to a stringly-typed or reflective layer.** This is what HeuristicLab did and the cost is not to be repeated.
- **Erasing `TCandidate`.** See step 3 of the ladder.
- **A parallel non-generic descriptor or blueprint layer** that materializes typed configurations. Same objection.
- **Per-encoding derived types** such as `RealVectorGeneticAlgorithm`, as a way to shorten names. The maintenance burden falls on algorithm authors and multiplies with encodings; closed generic aliases give the same naming benefit at no library cost.
- **Deleting `TProblem`.** See above.
- **Splitting `PermutationSearchSpace` on suitability grounds.** See above.

## Test plan

- Keep `NoviceFrictionSpecs` current. It is the record of the barrier and of the two measurements; a change to the type model should produce a visible diff there.
- Keep the two measurement specs as guard tests. A candidate type gaining a second search space, or a second operator binding a concrete problem, should fail them and reopen the discussion on evidence.
- Cover `FixedCardinalityBoolVectorSearchSpace` membership and `BitSwapMutator` cardinality preservation, including the degenerate cardinalities where no move exists.
- Any pre-flight pass needs specs showing a broken meta-algorithm stage failing at run start rather than mid-run, and reporting every failure rather than the first.
- `SearchSpaceCompatibilitySpecs` holds the six-cell target table as observed behavior. A declared invariant mechanism must reproduce every cell from declarations alone, and that equivalence is the spec its implementation has to pass.
- Equivalence needs specs for each rung of the resolution chain: a problem overriding it, a search space supplying it, and neither declaring it so candidate equality applies unchanged.
- Any arity change needs API usage specs written from the user's side, keeping the current and desired spellings side by side while the change is in progress.
