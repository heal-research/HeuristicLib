# Configuration arity migration

## Goal

Remove `TSearchSpace` and `TProblem` from the **configuration** layer in one step, so a user names one type argument where they name three today. Execution instances keep every type argument, exactly as now.

```csharp
GeneticAlgorithm<RealVector, BoundedRealVectorSearchSpace, IProblem<RealVector, BoundedRealVectorSearchSpace>>   // today
GeneticAlgorithm<RealVector>                                                                                     // after
```

The design record, the measurements behind it and the rejected alternatives live in [generic-arity-usability.md](generic-arity-usability.md). This plan is the migration only.

Both arguments go together rather than one at a time. `TProblem` is `IProblem<TCandidate, TSearchSpace>`, so a configuration naming the problem also names the search space, which fixes the order — and removing only the problem leaves `in TSearchSpace` on the configuration interface, which is the single thing that forces the transitional ceremony described in [generic-arity-usability.md](generic-arity-usability.md#the-mechanism-is-validated). Removing both dissolves it. Stopping halfway would leave the library in the shape with the most ceremony and the least payoff.

## The shape

Compiled against the real Contracts project before writing this plan.

**Configuration** carries the candidate only, and the instantiation carrier arrives as a parameter:

```csharp
public interface IMutator<TCandidate> : IOperator, IExecutionInstanceResolvable
{
    IMutatorInstance<TCandidate, TSearchSpace, TProblem> CreateExecutionInstance<TSearchSpace, TProblem>(
        ExecutionInstanceResolver<TCandidate, TSearchSpace, TProblem> resolver)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>;
}
```

**`ExecutionInstanceResolver<TCandidate, TSearchSpace, TProblem>`** is the type that fixes the triple. It is a typed view over the existing registry, not a replacement for it, and the split of responsibility is the point:

| | Holds | Knows the triple |
| --- | --- | --- |
| `ExecutionInstanceRegistry` | instance cache, replacements, cycle detection, parent and child chaining | no |
| `ExecutionInstanceResolver<…>` | the registry, the search space value, the problem value | yes |

All mutable state stays in the registry. The resolver is a `readonly struct` of three references, created wherever the triple is in scope and passed by value down the configuration graph, so it allocates nothing.

```csharp
public readonly struct ExecutionInstanceResolver<TCandidate, TSearchSpace, TProblem>(
    ExecutionInstanceRegistry registry, TSearchSpace searchSpace, TProblem problem)
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    public TSearchSpace SearchSpace { get; }
    public TProblem Problem { get; }

    // One pair per operator role.
    public IMutatorInstance<TCandidate, TSearchSpace, TProblem> Resolve(IMutator<TCandidate> mutator);
    public IMutatorInstance<TCandidate, TSearchSpace, TProblem>? ResolveOptional(IMutator<TCandidate>? mutator);

    // Algorithms carry their own search state, so it is a method argument and infers from the algorithm.
    public IAlgorithmInstance<TCandidate, TSearchSpace, TProblem, TSearchState> Resolve<TSearchState>(
        IAlgorithm<TCandidate, TSearchState> algorithm) where TSearchState : class, ISearchState;

    // Meta-algorithms scope their children exactly as they do today, keeping the same triple.
    public ExecutionInstanceResolver<TCandidate, TSearchSpace, TProblem> CreateChild();
}
```

Each `Resolve` does what the registry's single `Resolve` does today — cache lookup by configuration reference, replacement lookup, cycle detection, store — and then calls `CreateExecutionInstance` on whichever configuration wins. The registry keeps `RegisterInstance` and `RegisterReplacement` unchanged in spirit: they key on reference identity, which needs no type arguments, so observation continues to install against the registry at run setup where the triple is not in scope.

**Why it carries the values, not only the types.** Validation is trial resolution, so the pass that builds the graph is the pass that checks it, and the invariant checks need the search space value. Carrying it here is what lets one walk do both. Operators still receive the search space and problem per call at execution; nothing about configuration reuse changes.

**Where it is created.** At the root of a run, where the problem is supplied — `CreateRun(problem, random)` — and nowhere else except `CreateChild`.

**Known cost.** The single generic `Resolve` becomes roughly ten `Resolve`/`ResolveOptional` pairs, one per role plus algorithms. That is the price of role-specific typing without a `Type` argument or a delegate, and it is consistent with a codebase that is already role-specific throughout.

**Naming.** `ExecutionInstanceResolver` extends the vocabulary already in place: an `IExecutionInstanceResolvable` is resolved by a resolver, using the registry. It is deliberately not called a context or a scope, both of which say less than the type does.

### What does not change

- **Authoring arity.** An operator that reads a concrete search space still declares the one it binds to, and its base still takes that parameter: `Mutator<RealVector, BoundedRealVectorSearchSpace>`. What drops is the arity a **user names when declaring a slot or a variable**, not the arity an **author writes when implementing**. Leaf operators such as `GaussianMutator` do not change at all.
- **Execution instances.** `IMutatorInstance<TCandidate, TSearchSpace, TProblem>` and every other `…Instance` contract keep all their arguments.
- **`TSearchState`.** Terminators, interceptors and algorithms keep it; it is not part of this change.
- **`TCandidate`.** Stays, per the ladder decision.

### The authoring ladder

This is where most of the file count lives, so the shape is fixed here rather than discovered during the migration.

The three rungs survive, but their type parameters change meaning. Today they are *the types the operator is generic over*; afterwards they are *the types the operator binds*. Rename them accordingly, so the shift is visible at every declaration.

| Rung | Today | After | Bridge to a requested `(TSearchSpace, TProblem)` |
| --- | --- | --- | --- |
| reads nothing | `Mutator<TCandidate>` | `Mutator<TCandidate>` | implicit, no cast, cannot fail |
| reads the search space | `Mutator<TCandidate, TSearchSpace>` | `Mutator<TCandidate, TBoundSearchSpace>` | checked cast, succeeds when the requested space is the bound one or derives from it |
| binds a problem too | `Mutator<TCandidate, TSearchSpace, TProblem>` | `Mutator<TCandidate, TBoundSearchSpace, TBoundProblem>` | checked cast on both |

Every rung implements the same configuration interface, `IMutator<TCandidate>`. The bridge lives once per rung, in the base, so concrete operators are untouched: `InversionMutator : SingleCandidateMutator<Permutation>` and `GaussianMutator : Mutator<RealVector, BoundedRealVectorSearchSpace>` both keep their declarations verbatim.

The top rung is what a problem-bound operator uses. `NumericParameterFittingRefiner` stays `SingleCandidateRefiner<ExpressionTree, ExpressionTreeSearchSpace, SymbolicRegressionProblem>`, and its cast is the one place in the library where resolution can fail on the problem type.

**What the author writes on a bound rung.** The abstract member stays the simple one, expressed entirely in the bound types. The base implements the interface method, performs the check and forwards, so the generic method never reaches the author:

```csharp
protected abstract IMutatorInstance<TCandidate, TBoundSearchSpace, TBoundProblem> CreateInstance();

public IMutatorInstance<TCandidate, TSearchSpace, TProblem> CreateExecutionInstance<TSearchSpace, TProblem>(
    ExecutionInstanceResolver<TCandidate, TSearchSpace, TProblem> resolver) …
{
    if (CreateInstance() is not IMutatorInstance<TCandidate, TSearchSpace, TProblem> instance)
        throw new InvalidOperationException(
            $"{GetType().Name} is written for {typeof(TBoundSearchSpace).Name} and {typeof(TBoundProblem).Name}, " +
            $"and cannot run over {typeof(TSearchSpace).Name} with {typeof(TProblem).Name}.");

    return instance;
}
```

The type test is the check, so there is no second rule to keep in step, and the message names both sides. Pre-flight reaches it before a run does.

**Bound rungs take no resolver.** Every leaf operator in the library ignores the registry today; only wrapping and multi operators use it, and those are space agnostic. So the bound rungs' abstract member has no resolver parameter, and an operator that owns children uses a composite rung instead. That is what keeps the generic method off the authoring surface for all leaves.

**Stateless rungs are the same shape.** A stateless operator is configuration and execution instance in one type, so it implements `IMutator<TCandidate>` for the configuration side and `IMutatorInstance<TCandidate, TBoundSearchSpace, …>` for the instance side, and its bridge type-tests `this`. Only the candidate reaches the configuration interface; the bound search space is what the check is against.

**Composites keep the generic method**, because they need the resolver to resolve children, and their abstract member is generic in `TSearchSpace` and `TProblem`. This is the ergonomic cost recorded under Risks, and it is confined to wrapping and multi bases.

### The triple is fixed per resolution graph

The rule that follows from the ladder, stated once because it governs the whole migration:

> The search space and problem types are established at the root of a resolution graph and propagate unchanged. Binding is a leaf-only narrowing that affects the leaf's own instance and nothing downstream.

It holds because the instance interfaces are contravariant in the search space and problem, so every node's instance must be usable at the **requested** types. Narrowing anywhere in the middle makes some legitimately typed descendant unusable.

This binds every node that resolves children, not only composite operators: algorithms resolving their operators, and meta-algorithms resolving child algorithms, all pass the run's triple through untouched.

`CreateChild()` is the only resolver constructed after the root. It changes **registry scope** — which instances are shared rather than created fresh per cycle — and must not change the triple. That is the one place this invariant can be broken by accident, so it is worth an explicit test.

If something genuinely needs a different search space or problem, that is a nested run with its own root, not a node bending the enclosing graph. The encoded-problem adapter sketched in [developer-backlog.md](developer-backlog.md) does not need such an exception: the adapter is a *problem* presenting `IProblem<IntegerVector, IntegerVectorSearchSpace>`, and decoding happens inside its evaluation, so the graph still sees one triple.

**Do not add a bound composite rung.** It is tempting, because a rung that binds its types could narrow the resolver and hand the author a fixed one, keeping the generic method off the composite surface too. That version compiles, and it violates the rule above. A composite bound at `IProblem<TCandidate, TSearchSpace>` would resolve its children there, and a child bound to a concrete problem would then be rejected even though the run supports it. Nor can it be fixed by resolving children against the requested triple instead, because a bound-typed parent instance cannot hold requested-typed children — the conversion goes the wrong way for the same reason.

Guidelines §8.5 should carry this rule rather than leaving it to be rediscovered.

All four shapes were compiled against the real Contracts project.

§8.4 and §8.5 of the developer guidelines describe this ladder and must be updated with the changed meaning of the parameters, the new bound-rung member, and the rule that a rung which owns children is space agnostic.

### `IOperator<TExecutionInstance>` is deleted

It is `IExecutionInstanceResolvable<TExecutionInstance>` plus a marker, and a configuration that produces a family of instance types cannot implement it for any single type. The new role interfaces derive from `IOperator` and the non-generic `IExecutionInstanceResolvable` instead, so the generic form loses its only purpose.

This reverses a note in [developer-backlog.md](developer-backlog.md#discussed-tried-and-rejected), which retained it on the grounds that it "expresses which execution-instance role a configuration creates". That is no longer expressible, and the role interface itself now carries the information. Update that entry as part of package 6 rather than leaving the record contradictory.

### Where the lost checks go

A leaf operator that is problem-agnostic converts implicitly by the contravariance the instance interfaces already declare, so most of the library keeps a compile-time guarantee with no cast and no failure mode. An operator bound to a concrete search space or problem bridges by a checked cast, and that cast is the only new runtime failure. It is caught before a run by the pre-flight pass, which also runs the invariant checks that cover search-space compatibility.

## Resulting arities

| Type | Today | After |
| --- | --- | --- |
| `GeneticAlgorithm` | 3 | 1 |
| `ITerminator`, `IInterceptor` | 4 | 2 (candidate, search state) |
| `Algorithm` base | 5 | 3 (self, candidate, search state) |
| `CycleAlgorithm`, `PipelineAlgorithm` | 5 | 3 |
| `IExperiment` | 6 | 4 |

## Blast radius

- 9 role interfaces in `HeuristicLib.Contracts/Operators`, plus `IAlgorithm`.
- ~129 files across the nine operator families in `HeuristicLib/Operators`, most of them mechanical.
- ~17 algorithm records across core and experimental.
- ~120 files mention `ExecutionInstanceRegistry`, `RegisterInstance` or `RegisterReplacement`.
- 2 analyzers and 1 code fix keyed on `CreateExecutionInstance` and `ExecutionInstanceRegistry`.
- 7 documentation pages show the affected shapes.

## Spike result: the shape holds

A spike ran packages 1 and part of 2 against the real registry, real operators and the real replacement mechanism, first on xunit.v3 and again after the upgrade to xunit 4. The planned shape works, with `ExecutionInstanceResolver` as the method parameter and every call site inferring its type arguments.

Final state, with the spike present: **2044 of 2044 tests pass and the runner reports `Errors: 0`.**

### What the spike confirmed

- a candidate-only configuration resolves to a fully typed instance that runs, and the call site names nothing;
- an **open generic** operator resolves and runs — the shape that previously broke the run;
- identity caching, `ResolveOptional`, and parent and child scoping behave exactly as today;
- registered replacement is honoured, so observation still anchors — the risk flagged for package 5;
- a composite passes the run's triple through to its child rather than narrowing it;
- an agnostic leaf converts implicitly, with no cast;
- a bound leaf over the wrong search space fails with a message naming both sides.

### The tooling trap, and the rule that avoids it

Declaring a **public** open generic type in a test assembly that implements the role interface raises a `TypeLoadException` while reflection builds the method signature, because the CLR fails to re-validate `TSearchSpace : class, ISearchSpace<TCandidate>` through reflection even though the compiler accepted it and the code runs correctly when called.

```
System.TypeLoadException : GenericArguments[1], 'TSearchSpace', on
'HEAL.HeuristicLib.Operators.IMutatorInstance`3[TCandidate,TSearchSpace,TProblem]'
violates the constraint of type parameter 'TSearchSpace'.
  at System.Reflection.RuntimeMethodInfo.GetParameters()
```

On xunit.v3 through the VSTest bridge this was catastrophic and silent: `dotnet test` reported `Passed! Total: 23` where the direct runner reported 2036 with one error. The xunit 4 upgrade removed that bridge, so all tests now run; the error was reported as `Errors: 1` by the runner and swallowed by `dotnet test`.

**Making the type `internal` removes it entirely.** xUnit only reflects over public types as test class candidates, which is why the failure was labelled `[Test Class Cleanup Failure]`. Test helpers are naturally internal, so the rule costs nothing:

> An operator helper declared in a test assembly that is open in its candidate type must be `internal`, not `public`.

Existing helpers are closed rather than open — `ChooseOneOperatorTests.CountingInstanceMutator` binds `int` and `DummySearchSpace<int>` — so nothing today is at risk. An architecture test asserting that no public test-assembly type implements an operator role interface while still open in its candidate type would keep it that way. The `TypeLoadException` is worth reporting to .NET; the repro needs no HeuristicLib types.

### What survives in the tree

The registry re-key onto the non-generic `IExecutionInstanceResolvable`, the second `Resolve` overload, and the non-generic `RegisterReplacement`. All additive, all green, all needed by the migration. The state parameter is not optional: a lambda inside a struct member cannot capture primary constructor state, and a capturing lambda would allocate on every resolution.

The spike itself lives in `test/HeuristicLib.Tests/Spikes`. Package 2 has landed the shape properly, so it is now redundant and should be deleted.

## Current state

**Package 1 (registry seam): done.** The registry is re-keyed on the non-generic `IExecutionInstanceResolvable` and carries the whole resolution policy.

- **Creation methods are handed the registry, not a resolver.** `CreateExecutionInstance<TRunSearchSpace, TRunProblem>(ExecutionInstanceRegistry)` is the migrated shape, and the only difference from the original `CreateExecutionInstance(ExecutionInstanceRegistry)` is the two type arguments. An earlier draft passed an `ExecutionInstanceResolver` instead; it bought nothing, because the run's type arguments are already in scope inside the method that declares them, and three of the five implementers immediately unwrapped it back to the registry.
- `ExecutionInstanceResolver<TCandidate, TSearchSpace, TProblem>` survives as **opt-in sugar only**, built with `registry.For<…>()`, for a call site that resolves several operators and would otherwise repeat the triple on each. Every resolver overload forwards straight to the registry overload of the same name. `MultiMutator` is the one caller that earns it. Its `CreateChild` had no callers and is gone.

- **Two `Resolve` overloads, one resolution system.** `Resolve(resolvable)` for a configuration that names the single execution instance type it creates; `Resolve(resolvable, create)` for one that does not. Both run the same policy — cached instance, registered replacement, parent registry, otherwise create — and differ only in who makes the creation call.

  Why the second exists, and it is **not** that the run's type arguments are unavailable: they are threaded down from the root of the resolution graph and every caller has them. Before the migration, creation was declared once, on `IExecutionInstanceResolvable<TExecutionInstance>`, so the registry could call it for every role. After, each role declares its own creation method returning its own instance type, and to call any of them the registry would have to name one role and return that role's instance type. Writing that once for all roles needs a type parameter standing for a type constructor, which C# cannot express. The alternatives are one overload per role — making the execution machinery depend on every role contract, and leaving roles declared outside Contracts (the experimental move operators, and anything a consumer writes) unable to resolve at all — or erasing the declared return type to `IExecutionInstance`, which drops the constraints tying a search space to its candidate and puts a cast on every resolution.

  The overload is generic in the resolvable type (`TResolvable`), so neither the typed overload nor a role extension casts: `Resolve(mutator, static (creationTarget, registry) => creationTarget.CreateExecutionInstance<…>(registry))` hands the lambda an `IMutator<TCandidate>` directly. The lambda parameter is named for what it is — the resolvable to create from, which is the registered replacement when there is one, not necessarily the one passed in. The one cast left is the registry checking that a registered replacement really can stand in for what it replaces, which is where that check belongs and where it can name both sides in the message.

  The spike's `TState` parameter is gone. It existed because the spike's resolver carried the search space and problem as values, which a static lambda could not reach; the shipped resolver carries only the registry, both call sites were passing `state: 0`, and a static lambda captures nothing because the type arguments it uses come from the calling generic method.

- `RegisterReplacement` is one non-generic method. Replacement was always keyed by reference identity; the execution instance type argument only paired the two parameters, and a migrated configuration cannot supply it, so the typed overload would have become unreachable role by role. Every existing call site still binds unchanged, since `IExecutionInstanceResolvable<T>` is an `IExecutionInstanceResolvable`.
- `IOperator` now extends `IExecutionInstanceResolvable`. Every operator configuration resolves to an execution instance — before this, each role said so separately through `IOperator<TExecutionInstance>`, and nothing implemented bare `IOperator` without it. This is what lets the budget algorithms constrain `TOperator : class, IOperator`, which is what they actually mean, instead of naming the resolution mechanism.

**Package 2 (mutator role end to end): done.** Solution builds; all four suites green at 2034 / 166 / 155 / 23 = 2378 tests; whitespace, style and analyzer verification clean.

What landed:

- `IMutator<TCandidate>` declares `CreateExecutionInstance<TRunSearchSpace, TRunProblem>(ExecutionInstanceRegistry)`. The type arguments are named for the run, because an operator's own search space and problem — where it has them — are its *type* arguments and mean something different: what it was written for, rather than what it is being asked to run over.
- **The bridge is an explicit interface implementation.** `Mutator<TCandidate, TSearchSpace, TProblem>` keeps `public abstract IMutatorInstance<…> CreateExecutionInstance(ExecutionInstanceRegistry)` as the authored member, unchanged from before the migration, and implements the interface's generic method explicitly with a type test that throws naming both sides. Leaf operators did not change at all. Being able to call the bound member directly, but having to supply the run's types once the same operator is held as `IMutator<TCandidate>`, is the honest signal: they are two different operations, and the analyzer already warns on the direct call.
- **Composites are agnostic and pass the run's triple through.** `WrappingMutator<TCandidate>` and `MultiMutator<TCandidate>` expose `WrapExecutionInstance<TRunSearchSpace, TRunProblem>` / `CombineExecutionInstances<TRunSearchSpace, TRunProblem>` as the authored member. `PipelineMutator`, `ChooseOneMutator`, `CountingMutator` and `DurationMeasuringMutator` all fell from three type arguments to one, each with a nested generic `Instance<TSearchSpace, TProblem>`. `ObservableMutator` keeps its observer type arguments — its observers are typed at a search space and problem — and reconciles them against the run when it wraps.
- **The composites implement the interface publicly, unlike the leaf base.** On `Mutator<…>` the explicit implementation hides a worse creation path in favour of a better one the author already has. A composite has no second creation member, so hiding it there would remove the mechanism from view entirely and leave an author's override plugged into nothing visible. Same contract, opposite call.
- **Resolution has two spellings, and the choice is about repetition, not capability.** `registry.Resolve<TCandidate, TSearchSpace, TProblem>(mutator)` for a call site that resolves one operator; `registry.For<…>()` when it resolves several, so the triple is named once rather than per operator. Both are declared as extension blocks on `MutatorResolverExtensions`, so the resolver spelling needs no type arguments at all.
- Shared machinery came along, as the first attempt predicted: `OperatorBudgetAlgorithm` and `OperatorDurationBudgetAlgorithm` dropped `TObservedInstance` (six type arguments to five) and moved to identity-keyed replacement, with their two extension files following; `ObservationPlan.Observe` lost its instance type argument.

Guard tests that changed, and why each change is the measurement rather than an accommodation:

- `OperatorTopologyTests` split into two theories. Migrated configuration contracts name only the candidate; instance contracts and not-yet-migrated configurations keep the contravariant triple. A role moves one `InlineData` line between them as it migrates, so the file doubles as the migration ledger.
- `NoviceFrictionSpecs.TheOneArgumentAlgorithm_CannotTakeAnOperatorThatReadsTheSearchSpace` split in two. The friction it recorded still exists on unmigrated roles, so it is now stated over `ICreator` with `UniformDistributedCreator`; its migrated counterpart asserts that `GaussianMutator` — which reads `BoundedRealVectorSearchSpace` — now fills `GeneticAlgorithm<RealVector>.Mutator`. That flip from `ShouldBeFalse` to `ShouldBeTrue` is the entry-barrier result this whole plan is chasing, on one role.
- `AlmostNoOperatorConstrainsTheProblemTypeArgument` dropped `IMutator<>` from its role list: a migrated role has no problem argument to constrain, which is the answer to that measurement rather than an exception to it.

**A latent bug the migration exposed.** The registry-based `MutatorResolverExtensions.Resolve<TCandidate, TSearchSpace, TProblem>` bound to itself rather than to the resolver overload — the registry's own `Resolve` takes one type argument and was never a candidate at three. It compiled, and it would have stack-overflowed on the first genetic algorithm run. It went unnoticed only because the build was still red while the call sites were being converted. Worth remembering for the eight roles that follow: each will add a same-named pair of extension overloads with the same trap available.

**Cost, measured.** Composite authoring is heavier, as §8.5 of the developer guidelines predicted: an author of a wrapping or multi mutator now writes a generic method with a constraint clause and a nested generic instance class. Leaf authoring is unchanged — the common case pays nothing. Consumers who held an operator as a concrete type pay nothing; consumers who held it behind the interface name the triple once per call site.

**Not yet started:** the other eight roles, algorithm configurations, experiments, factories, analyzers and documentation.

## Work packages

Each package ends green: solution builds, all four suites pass, formatting verified. Nothing proceeds past a red checkpoint.

**1. Resolver and registry seam.**
Add `ExecutionInstanceResolver<TCandidate, TSearchSpace, TProblem>` with the role overloads it needs so far. Re-key the registry's three collections on the non-generic `IExecutionInstanceResolvable`; the generic argument on the key never carried information, since lookups are by reference. Keep the existing `Resolve` working untouched. Purely additive.

**2. One role end to end: mutators.**
New `IMutator<TCandidate>`; bridge the four authoring bases (`Mutator`, `StatelessMutator`, `StatefulMutator`, `SingleCandidateMutator`), the two topologies and the two instrumentation wrappers; add the resolver's mutator overloads. Convert every concrete mutator and every consumer, including `TransformedCreator` and `TransformedCrossover`, which hold mutators.

**Measurement from a first attempt (reverted).** The mutator role was converted far enough to size it, then reverted to keep the tree green. What it showed:

- The contract, the resolver and the authoring ladder all land cleanly. `IMutator<TCandidate>`, `ExecutionInstanceResolver`, the three binding rungs and the stateless/stateful bases converted without difficulty, and the bridge behaved as designed.
- **The role does not end at the role.** Converting mutators forced changes to machinery the blast radius did not account for: `OperatorBudgetAlgorithm` and `OperatorDurationBudgetAlgorithm` constrain `TOperator : IOperator<TObservedInstance>`, which a candidate-only configuration cannot satisfy. Both lose a type argument and move to identity-keyed replacement, and their two extension files follow. That is a public API change to the budget algorithms, arriving as a side effect of the first role.
- **Composites have to be restructured, not edited.** `WrappingMutator`, `MultiMutator`, `PipelineMutator`, `ChooseOneMutator`, `ObservableMutator` and the two instrumentation wrappers all drop to `<TCandidate>`, because binding is a leaf concept. That is a structural change to seven types, not a signature rewrite.
- **Volume.** 28 source files were rewritten mechanically before the cascade began; at the point of reverting, 82 compiler errors remained, overwhelmingly `CS0411` from `registry.Resolve(...)` call sites that now need a resolver. The composite restructuring had not started.

Read against the plan's estimate of "~129 files across the nine operator families, most of them mechanical", the mechanical part holds, but the per-role cascade into budget, observation and defaults machinery was missing from the estimate. Expect the first role to cost substantially more than the eight that follow, since it drags the shared machinery with it.

**Be willing to stop.** Report the diff the way the evaluator return-type change was measured in [developer-backlog.md](developer-backlog.md): how much is mechanical, how much reaches behavior, and what composite authoring actually costs.

Abandon the migration and revert the branch if any of these turns out true, rather than pressing on:

- a leaf operator needs a cast where the table above says it should not, which would mean the implicit conversion does not hold in practice and every operator pays a runtime check;
- composite authoring cannot be expressed without callers of a composite also naming type arguments, which would push the arity back out to users through a different door;
- registry replacement cannot be preserved, since observation failing silently is worse than the arity.

**3. The remaining eight roles**, one per commit, in ascending order of coupling: replacers, selectors, creators, crossovers, evaluators, refiners, terminators, interceptors.

**4. Algorithms.**
`Algorithm` and `IterativeAlgorithm` bases first, then the concrete algorithms, then the composition and control algorithms. Drop the now-redundant reduced-arity records (`GeneticAlgorithm<TCandidate, TSearchSpace>` and `GeneticAlgorithm<TCandidate>` collapse into one type).

**5. Observation and experiments.**
`ObservationPlan.Observe` is constrained on `IExecutionInstanceResolvable<TExecutionInstance>`, which the new configurations do not implement; its entry dictionary is already keyed non-generically. Rework the constraint and `RegisterReplacement` to pair same-typed resolvables. Then `IExperiment` and the experiment machinery.

**6. Factories, analyzers, documentation.**
`For(...)` and `Create(...)` companions simplify, since fewer arguments need inferring. Update both analyzers and the code fix. Update the seven documentation pages and the examples.

**7. Cleanup and exit.**
Delete every transitional overload and adapter. Verify no `Resolve` overload survives beyond the per-role ones on `ExecutionInstanceResolver`. Update `NoviceFrictionSpecs`, whose whole point is to show a visible diff when this lands.

## Risks

- **Observation is the sharpest coupling.** Registry replacement is how analyzers anchor onto operators, and it is typed on the resolvable. Getting this wrong produces silently empty analyzer results rather than a build error, which is the failure mode [execution-instances.md](../docs/contributing/architecture/execution-instances.md) already warns about. Package 5 needs its own specs, not just a green build.
- **Composite authoring gets heavier.** A wrapping or multi operator's abstract creation method becomes generic in `TSearchSpace` and `TProblem` with a constraint clause. This is the real ergonomic cost and it lands on the part of the hierarchy §8.5 of the developer guidelines already calls fiddly. Package 2's measurement should report it explicitly.
- **Experimental and Python interop trail the core.** They consume these contracts and must be converted in the same branch.
- **Reduced-arity records disappear**, which is a visible public API change beyond the arity itself.

## Test plan

- Keep all four suites green at every package boundary; the branch never merges from a red state.
- Package 2 adds specs showing a problem-bound operator failing at pre-flight rather than mid-run, with a message naming the operator and both problem types.
- Package 5 adds specs proving analyzers still anchor through registry replacement after the constraint rework, since a build passes even when this is broken.
- `NoviceFrictionSpecs` is updated last and its diff is the user-visible result: the specs that exist to record the arity should get materially shorter.
- Invariant coverage is unchanged and must stay green throughout, since it is the safety net for search-space compatibility once the configuration stops naming the space.

## Exit criteria

1. No configuration type in `src` names `TSearchSpace` or `TProblem`.
2. Every execution instance contract still names both.
3. One resolution path: the per-role overloads on `ExecutionInstanceResolver`, with no transitional overloads left.
4. All four suites, formatting, style and analyzer verification green.
5. `NoviceFrictionSpecs` and the seven documentation pages reflect the new arities.
