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

### `IOperator<TExecutionInstance>` is deleted — done

**Deleted.** With the nine roles, the three experimental move roles and `IVariableStrengthMutator` migrated, it had no implementers left; the only remaining uses were two analyzer test fixtures that exist to describe an operator authored without a library base, and they say `IOperator` now. The analyzer itself was never affected: it keys on the non-generic `HEAL.HeuristicLib.Operators.IOperator`.

`IExecutionInstanceResolvable<TExecutionInstance>` survives it, because `IAlgorithm` still implements it. That one is package 4's exit criterion, not package 3's.

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

**Operator defaults moved to what declares them.** `IEncodingDefault*` is now `HEAL.HeuristicLib.SearchSpaces` and `IProblemDefault*` is `HEAL.HeuristicLib.Problems`, beside `ISearchSpace` and `IProblem` respectively; the `Contracts/Algorithms/Defaults` folder is gone.

The case against leaving them under `Algorithms` is concrete rather than taxonomic. `IProblemDefaults<TSelf, TCandidate, TSearchSpace>` *extends* `IProblem`, so an `IProblem` was living in the algorithms namespace; and `PermutationSearchSpace.cs` — a pure encoding type — opened with `using HEAL.HeuristicLib.Algorithms;` for no reason other than to describe itself. A default is what the encoding or problem says about itself, and it holds whether or not any algorithm ever reads it; algorithm factories are merely today's only reader.

The move needed **no new using anywhere**, and removed a dead one, which is the test that it went to the right place. An earlier idea of a neutral `Contracts/Defaults/` namespace was worse: it would have moved the interfaces away from both the declarer and the reader. `AssemblyDependencyTests.ConceptFolders_UseTheirIntendedNamespaces` pins the new mapping — it is what caught the move.

**The search state leaves the configuration too: terminators and interceptors now name only the candidate.**

The state looked like it had to stay, on the grounds that it is what these two operators are written about. It does not. The test is not "is it meaningful to the operator" but **"is there a source to bind against at resolution time"** — and there is: an operator never originates a search state, the algorithm it runs in does, and that algorithm is what resolves it. So `ITerminator<TCandidate, TSearchState>` became `ITerminator<TCandidate>`, with the state arriving alongside the search space and problem as a third method type argument, and a mismatch reported when the execution graph is built.

What this bought, beyond the arity:

- **Composite factories became inferable.** `terminator.And(other)` previously could not infer its state — it sat in the extension block's type parameters with nothing to infer it from, so callers wrote `And<Permutation, PopulationState<Permutation>>(...)`. The same held for `Or`, `CountCalls` and `MeasureDuration`. All of them now take no explicit type arguments at all.
- **The pass-through composites lost a type argument each.** `AllTerminator`, `AnyTerminator`, `PipelineInterceptor` and the four instrumentation wrappers never read the state; they only forwarded it. They are agnostic in it now, like the rest of the composite layer. `ObservableTerminator` and `ObservableInterceptor` keep one, renamed `TObserverSearchState`, because their observers really are written for a state — the `ObservableMutator` pattern.
- **The state-aware bucket in `OperatorTopologyTests` is gone.** All nine roles are now checked by one theory: a migrated configuration names the candidate and nothing else.

### The resolver carries the binding, so the quadruple is just a longer binding

A first attempt gave the two state-aware roles no resolver form at all, on the grounds that `resolver.Resolve<TSearchState>(terminator)` does not compile — **an extension member declared in a generic extension block merges the block's type parameters into its own**, so an explicit type argument list must supply all four or none. That reasoning was right about the mechanism and wrong about the conclusion. The state does not have to be named on the *call*; it can be named on the *resolver*.

`ExecutionInstanceResolver<TCandidate, TSearchSpace, TProblem, TSearchState>` now exists and **derives from** the triple resolver. Extension members declared for a base type apply to a derived one, so the nine triple-role blocks serve it unchanged and only the two state-aware roles needed new blocks — whose members take no method type parameters at all, because all four come from the receiver. An algorithm therefore uses one resolver for everything, and no call site names anything:

```csharp
var resolver = instanceRegistry.For<TCandidate, TSearchSpace, TProblem, PopulationState<TCandidate>>();
return new Instance(resolvedInterceptor, resolver.Resolve(Evaluator), resolver.Resolve(Creator), resolver.Resolve(Crossover),
    resolver.Resolve(effectiveMutator), resolver.Resolve(Selector), resolver.ResolveOptional(Terminator),
    resolver.ResolveOptional(Refiner), PopulationSize, MaximumGenerations, Elites);
```

The one cost is that `ExecutionInstanceResolver` became a class: structs cannot inherit, and the inheritance is what avoids duplicating nine blocks that would then drift. It is one small allocation per creation method, and a creation method runs once per run rather than once per iteration. The alternative — a second, unrelated struct carrying twenty-seven copied members — is worse for a saving that does not show up anywhere.

`RoleBindingTests` covers all nine roles through both forms again, including the two state-aware ones through the quadruple resolver.

### Cost: one more inference loss, recorded

`ObservableTerminator.Create(terminator, (bool _) => { })` and the matching `ObserveWith` overload took their state from the terminator's own type. With that gone, an `Action<bool>` observer carries nothing to infer from, so both now name the candidate and state explicitly. This one is a genuine loss — the information left the argument types — unlike the algorithm factories, where the type parameters turned out to be supplying nothing. It is recorded where it happens, in `InferenceConstructionSpecs` and `ObservableOperatorCounterTests`.

**Result.** Solution builds; all four suites pass at 2510 tests; whitespace, style and analyzer verification clean.

### The algorithms: settled

`TSearchState` on `IAlgorithm` was a different question, because an algorithm *does* originate its state.

- `TSearchSpace` and `TProblem` leave the configuration, as for the operators.
- `TCandidate` stays.
- `TSelf` stays on the authoring base. It never appears on `IAlgorithm`, so it costs a user nothing.
- The concrete algorithms already fix their state — `GeneticAlgorithm` at `PopulationState<TCandidate>`, `HillClimber` at `SingleSolutionState<TCandidate>` — so after the migration `GeneticAlgorithm<Permutation>` is one type argument and `Complete` still returns a fully typed state.

**Answered: it keeps the state, on a second interface.** Dropping it outright was tried first, and moved the typed run methods to the authoring base. That worked and was wrong: it made every execution-shaped call unreachable for anyone holding an algorithm they had not constructed, which is the ordinary case for a factory return or a stored configuration. A compile probe held one genetic algorithm both ways and confirmed the size of the loss — on `IAlgorithm<RealVector>` all of `Complete`, `WithMaxIterations`, `ObserveWith`, `AsGrid`, `Repeat` and `Validate` fail; on the concrete type all six compile.

So `IAlgorithm<TCandidate, TSearchState> : IAlgorithm<TCandidate>` exists, and membership is decided by **produced versus consumed**. `Complete` returns `TSearchState`, so its signature cannot be written on a type that does not name it — forced. The erased form keeps everything that does not need it: heterogeneous collections, composite children, and the whole `Resolve` / `ResolveOptional` / `TryResolve` family, which names all four run types at the call site regardless.

**Terminators and interceptors do not follow it back.** Their state is a parameter of an *instance* method, and nothing on that side has a return type mentioning it — `terminator.And(other)` yields `AllTerminator<TCandidate>`, `CountCalls` yields `CountingTerminator<TCandidate>`. Restoring it would buy only an earlier report of a mismatch that pre-flight already catches, at the price of making two of the twelve roles state what they are written for while the other ten do not. Revisit only if the pre-flight message proves to be a common stumble in practice.

**Package 3 stragglers: complete, and green.** Four types held `IOperator<TExecutionInstance>` after the nine roles landed, and each was a different kind of leftover.

- **`IVariableStrengthMutator` was a deletion, not a migration.** The configuration-side interface had no consumers at all: `EvolutionStrategy` adapts strength by testing its *resolved* mutator, which is an execution contract and rightly keeps the triple. So the config interface went, along with `GaussianMutator`'s explicit second resolvable. The two tests that resolved the instance directly now resolve an ordinary mutator and test for the capability — the same thing the algorithm does, so the tests describe the real mechanism rather than a shortcut only they used.

  **Decided: it does not come back, and the instance is renamed to say why.** A restored configuration interface could no longer name its instance type — that is exactly what `IOperator<TExecutionInstance>` was for, and a configuration produces a family of instance types, one per run triple. It would degrade to `IMutator<TCandidate>` plus a `MutationStrength` property, and worse, it would be a promise the type system cannot keep: nothing would force an implementer to return an instance that actually adapts. The one caller that matters has to test the instance regardless.

  The asymmetry that made this look wrong was the *name*. `IVariableStrengthMutatorInstance` parses as "the instance form of `IVariableStrengthMutator`", so the missing partner reads as an omission. It is now `IAdaptableMutationStrengthInstance`: a **capability** of an execution instance rather than a role, which any mutator may offer by returning an instance that implements it. `Instance` stays in the name deliberately — only an execution instance can be adaptable, and the name should say so.
- **The three experimental move roles took the standard shape.** `IMoveCreator<TCandidate, TMove>`, `IMoveApplier<TCandidate, TMove>`, `IMoveEvaluator<TCandidate, TMove>`, each with the generic `CreateExecutionInstance<TRunSearchSpace, TRunProblem>`, the leaf ladder with its explicit bridge, and the full `Resolve` / `ResolveOptional` / `TryResolve` pair of extension blocks. **`TMove` stays on the configuration** for the same reason `TSearchState` did: it is part of what the operator is written about, not what it is being asked to run over.
- **`INeighborhood` fell out of it, and is the visible win.** It exposes the three move roles, so it dropped from `INeighborhood<TCandidate, TSearchSpace, TProblem, TMove>` to `INeighborhood<TCandidate, TMove>`. Authoring is unchanged: `Neighborhood<TCandidate, TSearchSpace, TProblem, TMove>` still names what it is written for, and the three bound operators it hands out satisfy the agnostic interface — the `ObservableMutator` pattern again.
- `TGenotype` was renamed to `TCandidate` throughout the move roles, per the glossary.

The move roles had no tests before. `MoveRoleBindingTests` in the experimental suite now pins the same binding property the nine roles have.

Solution builds; all four suites pass at 2160 / 169 / 159 / 23 = 2511 tests; whitespace, style and analyzer verification clean.

**Package 3 (all nine roles): complete, and green.** Creator, crossover, selector, replacer, refiner and evaluator followed the mutator, then terminator and interceptor. Every operator configuration in the library now names only what the operator is written about. Solution builds; all four suites pass at 2130 / 166 / 155 / 23 = 2474 tests; whitespace, style and analyzer verification clean.

The blocker below is resolved for now by naming the triple at the affected `Create` call sites — the decision it needs is still open, and lands with the algorithm package.

**Package 3a (creator and crossover): mechanically complete, blocked on one design decision.** Both roles are migrated end to end — contracts, base ladders, composites, instrumentation, observation, the experimental composite search space, and every call site. The solution builds with zero errors; `HeuristicLib.Tests` (2034) and `HeuristicLib.Tests.Experimental` (155) are green. Four tests across the spec and scenario suites fail, all from one root cause.

### Blocker: the `Create` factories inferred the algorithm's search space from its creator

`GeneticAlgorithm.Create(new UniformDistributedCreator(...), crossover, mutator, …)` used to infer `TSearchSpace = BoundedRealVectorSearchSpace`, because the creator's own type named it. A creator now names only its candidate, so nothing in the call carries a search space and inference falls back to the widest `ISearchSpace<RealVector>`. The algorithm is then typed at that, resolution asks the creator for an instance over it, and the bound creator's bridge correctly refuses:

> `UniformDistributedCreator is written for BoundedRealVectorSearchSpace and IProblem`2, and cannot run over ISearchSpace`1 with IProblem`2.`

This is not a typing nuisance. It is working user code that now throws at pre-flight — `PractitionerUsageSpecs.GeneticAlgorithm_BenchmarkExample_RunsToCompletion` and `SymbolicRegressionRedesignSpecs.GeneticAlgorithm_AuthoringShape_RunsWithNewCreatorCrossoverAndMutator` are both real authoring paths. The information the factory inferred from no longer exists in the argument types, so no overload can recover it.

**What this shows about the plan.** Roles are not independently migratable after all. An algorithm configuration names a search space and problem, and the only things that used to pin them were its operators. Removing that from the operators means the algorithm layer has to supply it instead — so package 4 is a precondition for the operator roles that fill an algorithm slot, not a follow-up.

**It resolves once the algorithms migrate.** The run's triple is established at the root, and today the root is the algorithm's own type, pinned at construction by whatever the factory could infer. Once an algorithm configuration names only its candidate, the root moves to `Complete(problem, …)`: a Rastrigin problem supplies `BoundedRealVectorSearchSpace`, resolution happens there, and a creator bound to that space matches. Same mechanism as the operators, one level up.

One prerequisite, already solved in the tree for a different reason. A parameter typed `TProblem problem` leaves the candidate and search space in constraint position, where C# inference cannot reach them — the XML doc on `IProblemDefaults<TSelf, TCandidate, TSearchSpace>` states exactly this, which is why that interface exists and why `GeneticAlgorithm.For` takes it rather than a bare problem. `Complete` needs the same shape. Today only problems declaring a role default reach `IProblemDefaults` (`TravelingSalesmanProblem` does, `TestFunctionProblem` does not), so extending it to every problem is part of the algorithm package.

**Resolved by package 4, and the resolution was subtraction.** The algorithm configurations migrated, so the run's triple is established at `Complete(problem, …)` and a bound creator meets the problem's own search space there. That left `Create`'s `TSearchSpace` and `TProblem` naming nothing the method uses — not a parameter, not the return type, not the body, only their own `where` clauses. A type parameter in that position cannot be inferred at all, which is why the interim spelling had to name all three; it was never a widening to be pinned. Deleting them leaves `Create<TCandidate>(creator, …)`, inferred from the operators, at `GeneticAlgorithm`, `EvolutionStrategy`, `NSGA2` and `HillClimber`.

No anchor parameter was added. The considered alternative — `Create(problem.SearchSpace, creator, …)` — would have reintroduced at the factory exactly what the run now supplies.

### Compatibility is now measured twice

`OperatorCompatibilityTests` carries one table of 96 algorithm and crossover pairings with two expectation columns: what the compiler accepts, and what trial resolution accepts. They were the same measurement before the role migrated. The compile-time column now answers on the candidate alone; the validation column still holds the answers the compiler used to give, and passes. Every row where the two differ is a check that moved from build time to pre-flight rather than one that disappeared — twelve of ninety-six.

The check is `registry.TryResolve(operator, out instance, out reason)`, declared next to each migrated role alongside `Resolve`. It reports instead of throwing, and on success hands back the very instance the run will use — the registry has already stored it — so validating and creating are one step and a pairing that validates cannot fail later for this reason. That guarantee is asserted in the test itself: a true result must carry an instance and no reason, a false result a reason and no instance.

There is deliberately no separate rule engine beside it. An operator states the search space and problem it was written for on its own type, so whether it can serve a run is answered by asking it; a parallel predicate could only drift from what creation actually does. This is the *validation is trial resolution* decision in [generic-arity-usability.md](generic-arity-usability.md), now with an API that does not require catching an exception to use it.

`algorithm.Validate(problem)` now answers both halves, and the binding half turned out not to need a per-slot walk at all. Resolving the algorithm *is* the check: it reaches every operator, and it is typed at the run's problem, so problem compatibility comes for free rather than needing a mechanism of its own. The instances it builds are discarded with their registry.

The two halves fail differently, and the doc says so: invariants report every violation, because the walk continues; binding reports the first, because building stops there. `InvariantContractVerification` remains a testing tool — it runs operators on sample candidates, which is not something pre-flight should do — and is called only from tests.

**Settled: validation is opt in.** Nothing in the library calls `Validate`; a user calls it. A configuration that would be reported still runs, and a run performs no check of its own — so the cost of not asking is real and the specs say so rather than only asserting the policy. `PreflightValidationSpecs` shows the workflow (`Validate` then `Complete`), a run leaving the search space unchecked, and the binding half catching an operator written for another problem.

Still open, for the documentation package: validation appears in no user-facing docs page. An opt-in feature nobody is told about is the same as no feature.

### What the later roles added

- **The algorithm factories name only the candidate.** Once no argument carried a search space, `Create`'s `TSearchSpace` and `TProblem` appeared in nothing but its own constraint clauses — a type parameter nothing can supply, so every call site had to spell all three. They are gone; `Create<TCandidate>(creator, …)` infers from the operators and the dozen call sites across specs, scenarios and the Python interop write no type arguments at all. `CreateFactories_InferEverythingTheyNameFromTheirOperators` records the shape.
- **A composite can read a search space or problem, and keeps its base while doing it.** Staying agnostic in the base and binding on the operator are not alternatives: the operator names what it needs on its own type and reconciles it with the run's inside the override. `ObservableMutator` has always done this with its observers, and `DynamicCachingEvaluator`, `DynamicRelativeQualityEvaluator` and the `PrefixingWrappingCreator` spec now do it with their problem. An earlier draft had them implement the role directly instead, which lost the base for nothing.
- **A composite with a shape of its own implements the role interface directly, and that is right.** `Wrapping` covers one child and `Multi` a uniform list, because in both the base can do the resolving and hand the author instances. A composite with its own shape — a named child plus settings, two children, a child plus an evaluator — has to resolve its own children, so a base could only re-declare the interface method. A `Composing<Role>` base was tried and removed for exactly that: with the registry as its parameter, which the settled rule requires, it added nothing. `EliteSelector`, `GenderSpecificSelector`, `PredefinedCandidatesCreator`, `TransformedCreator`, `TransformedCrossover`, `RefinementEvaluator` and `ImprovementCheckingRefiner` implement the role interface.

  These operators cannot use the leaf ladder instead: `Creator<TCandidate, TSearchSpace, TProblem>` binds the triple, and `Creator<TCandidate>` binds it to the widest — either way the authored member never sees the run's types, so a child would be resolved at the wrong triple and a bound child refused. Owning children and passing the run's triple through is what the composite shape is for.
- **Composites hide in the leaf ladder.** Those same seven derived from the *leaf* base while owning a child. Worth checking for when the state aware roles migrate: the tell is a `Resolve` call inside a class whose base is the bound one.
- **The registry diagnosed a blind cast.** An instance it already held was cast to whatever the caller asked for. A stateless operator returns itself as its own execution instance, so the stored object is the operator; asking the same registry for it at a second triple found that object and cast it to the second triple's instance type, which failed as `InvalidCastException` from inside resolution. A registry serves one run, so that is a misuse rather than an incompatible operator, and `RequireInstanceOf` now says so, naming both types.
- **`RegisterInstance` joined `RegisterReplacement` in going non-generic**, for the same reason: a migrated configuration does not name an execution instance type.
- **`TryResolve` and `ResolveOptional` are declared for every migrated role**, on both the registry and the resolver, so the resolver is a complete alternative rather than a partial one. An algorithm that resolves several operators now names the triple once — `var resolver = instanceRegistry.For<…>()` — instead of repeating it per operator.
- **A state aware role names nothing at all through the resolver.** Its resolver overloads are generic in the search state only, so `resolver.ResolveOptional(Terminator)` takes the triple from the resolver and the state from the operator. The registry form still needs all four, because C# cannot infer type arguments partially; that is exactly the repetition the resolver exists to remove, and every algorithm now uses it.
- **Problem bound operators keep their own spec.** `AlmostNoOperatorConstrainsTheProblemTypeArgument` counted operators constraining the role's problem argument; the roles no longer have one, so the count stopped being the question. `AnOperatorThatReadsItsProblem_RunsOverThatProblemAndIsRefusedOverAnother` replaces it with the capability — an operator declared in the spec itself, standing for one a consumer writes, runs over the problem it names and is refused over another.

### Settled: inference ergonomics

The reduced arity moved type arguments from declarations to call sites, and the tests showed where. `Resolve` was never the concern — a user of an algorithm never resolves. What mattered was anything a user writes: the algorithm factories, the operator combinators, the run entry point.

Walking those call sites found one pattern behind all of them, and it is not a trade-off: a type parameter that appears only in a `where` clause. Nothing can supply it, so it is not merely hard to infer but impossible, and the compiler's only recourse is to demand the whole list. It hit `GeneticAlgorithm.Create` and its three siblings, and — with the same symptom of a call site naming types no argument mentions — `Repeat`, `CycleWith`, `Then` and `AsGrid`, where the extension block's own parameters were the uninferable ones. The fix in both places was deletion, not an anchor parameter: the information was not being carried badly, it was not needed.

`IProblemDefaults<TSelf, TCandidate, TSearchSpace>` remains the shape for the genuine case, where a value in hand does mention all three and `For(problem)` reads it off that value.

### The two state aware roles

Both keep their search state on the configuration, as decided, and the variance difference the decision predicted survives contact with the code:

- `ITerminator<TCandidate, in TSearchState>` — contravariant. The state appears in the *return* type of the creation method, but at a contravariant slot of `ITerminatorInstance`, so the positions cancel and `in` stays legal. That is what keeps the existing ladder working: `AfterIterationsTerminator<TCandidate>` reaches `ITerminatorInstance<…, ISearchState>` and still fills a `PopulationState<TCandidate>` slot.
- `IInterceptor<TCandidate, TSearchState>` — invariant, because `Transform` returns the state. No ladder rung below "names its state".

Blast radius matched the seven that preceded them: the mechanical 4→2 collapse (keeping the first and last argument) closed 80 of 98 core errors for terminators, and the residue was the same three classes — factories and extension blocks carrying now-unused parameters, the two composite bases, and resolution call sites.

**Where it stands.** Nine roles migrated, four suites green at 2130 / 166 / 155 / 23 = 2474 tests, build and formatting clean.

- `OperatorTopologyTests` is now three theories with no "not yet migrated" bucket: six stateless configurations at one type argument, two state aware ones at two, and the instance contracts still at the full triple.
- `NoviceFrictionSpecs` lost its subject. The friction was always stated over whichever role had not migrated — creator, then selector, then terminator, then interceptor — and there is no such role left. `EveryOperatorSlot_NamesOnlyWhatTheOperatorIsWrittenAbout` replaces it: every operator slot on `GeneticAlgorithm<RealVector>` names the candidate, and the two state aware slots add the state and nothing else.

**Not yet started:** factories, analyzers and documentation. Algorithm configurations and experiments are done; see *Package 4* and *Package 5* below.

**Settled before starting them:** only `TSearchSpace` and `TProblem` move. `TCandidate` and `TSearchState` stay on configurations, so every role lands on one of two shapes — `IMutator<TCandidate>` for the seven stateless roles, `ITerminator<TCandidate, TSearchState>` for the two state aware ones. See *What can leave the configuration layer* in [generic-arity-usability.md](generic-arity-usability.md).

## Package 4a: the problem anchor (prerequisite, done and green)

The algorithm arity reduction cannot start without this, which the plan predicted but under-weighted.

`Stream`, `Complete` and `CreateRun` work today because the search space comes from the **receiver**: they are declared on `IAlgorithm<TCandidate, TSearchSpace, TProblem, TSearchState>`. Once an algorithm names only its candidate, the receiver carries nothing, and a plain `TProblem problem` parameter cannot supply the rest — the search space sits in constraint position, where C# inference does not reach. So the run call itself is blocked, not just the `For` factories.

**The self type lives on the authoring base, not on the contract.** `Problem<TSelf, TCandidate, TSearchSpace>` now names the problem's own type, exactly as `Algorithm<TSelf, …>` does while `IAlgorithm` stays clean. A first attempt put it on a second interface, `IProblem<TSelf, TCandidate, TSearchSpace>`, which every problem declared; that was rejected, and rightly — it is an inference device, and a contract should not carry one. `IProblemDefaults`, which was that same device under a name about defaults, is gone too; the `IProblemDefault*` role interfaces keep their `TSelf` because their static members take it, which is a use rather than a trick.

`TSelf` threads through ten abstract bases (`Problem`, `SingleSolutionProblem`, `RealVectorProblem`, `PermutationProblem`, `DynamicProblem`, the three partial-problem bases, `DataAnalysisProblem`, `RegressionProblem`) and is named by each concrete problem: `TestFunctionProblem : RealVectorProblem<TestFunctionProblem>`. That is shorter than the interface line it replaces and is the CRTP shape the algorithm bases already use.

### What the base-class form costs

**An abstract problem base can no longer stand for "any problem over this encoding".** `PermutationEncodingSpecificAlgorithm<PermutationProblem>` used to mean an algorithm over any permutation problem; with a self type, `PermutationProblem<TSelf>` cannot be written unbound. That role passes to the interface — `IProblem<Permutation, PermutationSearchSpace>` — which is where it belonged. `OperatorCompatibilityTests` and `PythonCorrelationAnalysis` moved over, and a scenario helper that needed `EpochClock` off the base took the self type as a parameter instead.

**Quality-of-life methods reach only problems typed on the base**, which is the accepted trade: the four types implementing `IProblem` directly (`EmptyMetaOptProblem`, `NoProblem`, and two test doubles) do not get them. Every problem a user runs derives from `Problem<…>`.

`InferenceConstructionSpecs.OneProblemArgument_InfersTheProblemTheCandidateAndTheSearchSpace` pins the mechanism over `TestFunctionProblem`, a problem that declares no operator defaults at all — the case the old `IProblemDefaults` could not serve.

One harness fix fell out: `OperatorCompatibilityTests.GetCompilableName` rendered generic type arguments by `FullName`, which emits backtick metadata names. It is recursive now, so a generic argument compiles as C# source.

Solution builds; all four suites pass at 2513 tests; whitespace, style and analyzer verification clean.

## Package 4 (algorithms and experiments): done, and green

`GeneticAlgorithm<RealVector, BoundedRealVectorSearchSpace, IProblem<RealVector, BoundedRealVectorSearchSpace>>` is now `GeneticAlgorithm<RealVector>`, and the reduced-arity records that existed to hide the old arity collapsed away with it. `IExperiment` fell from six type arguments to four.

**Inference had to be restored structurally, not by naming types.** The first pass left call sites spelling type arguments that used to be inferred, which trades one entry barrier for another. Four devices fixed it, and each is reusable:

- **Anchor on the value that mentions everything.** `Complete`/`Stream`/`CreateRun` take `Problem<TProblem, TCandidate, TSearchSpace>` rather than a bare `TProblem`, so the candidate and search space sit in *parameter* position where inference reaches them, with a second overload on `IProblem<TCandidate, TSearchSpace>` for a problem held behind its interface.
- **Declare the block on the type that knows the most.** Builders moved to the authoring base, where `TSelf` is available.
- **Delete what cannot bind.** An extension block declaring a type parameter nothing supplies is not merely awkward to call — it is never a candidate for overload resolution, so it silently loses to something else. The experiment-level `Repeat` declared unused `TSearchSpace`/`TProblem` and `.Repeat(2)` was quietly binding to LINQ's `Enumerable.Repeat`. Three further blocks — `CycleWith`, `Then`, `AsGrid` on `IAlgorithm<TCandidate>` — were dead the same way, and the second interface revived rather than removed them: with the receiver at `IAlgorithm<TCandidate, TSearchState>` both arguments come from the receiver.
- **Bridge with a type test, never a type comparison.** A bound rung comparing `typeof(TRunProblem) != typeof(TProblem)` broke contravariance — an algorithm written for `IProblem<…>` genuinely does serve a run over a concrete problem — and refused seven legitimate runs.

**Wrapper and composite algorithm slots name the state.** `StateTerminatedAlgorithm<TCandidate, TSearchState>` held its child as `IAlgorithm<TCandidate>` and then resolved it at `TSearchState` three lines later, so the requirement was known at configuration time and discarded anyway. The slot is now `IAlgorithm<TCandidate, TSearchState>` across the four control wrappers, both composites, `ObservableAlgorithm`, `AlgorithmRun`, the experiment types and `DynamicRacingAlgorithm`.

This is *not* the operator rule reversed. The test is whether the holder already knows the constraint: a search space arrives with the run, so an operator must defer; a wrapper names its own state, so deferring throws information away. And because `IAlgorithmInstance` is invariant in `TSearchState`, the compile-time constraint is exactly as strict as the runtime check it replaces — nothing expressible was lost, the identical rejection just moved earlier. Two latent defects fell out: `IExperiment` and `Experiment` had **no constraint at all** on `TSearchState`, which could therefore have been instantiated with `int`.

**The capability split is pinned by tests, both halves.** `AlgorithmInterfaceCapabilitySpecs` shows the usage; `AlgorithmInterfaceCapabilityTests` drives a Roslyn compilation over thirteen expressions, asserting each compiles on the two-argument form and fails on the one-argument form. Asserting both directions is what makes it a measurement — the positive half proves the probe is well formed, so the negative half cannot pass because of a typo. It caught one immediately.

**`IExecutionInstanceResolvable<TExecutionInstance>` is deleted.** Package 5 planned to rework its constraint; it turned out to have no implementers in `src` at all, which made its two `ExecutionInstanceRegistry` overloads and `ExecutionInstanceResolvableExtensions` unreachable from production. Everything real goes through the factory form `Resolve<TResolvable, TExecutionInstance>(resolvable, create)`. Only two test doubles used it, and they now resolve through the factory form like the library does.

**One pre-existing test-runner defect fixed in passing.** `dotnet test` exited 5 despite zero failures: xUnit reflects over every public class, and `GetParameters()` on an *open* generic fixture throws `TypeLoadException` because a method type parameter cannot be validated against a constraint mentioning a still-free class type parameter. Seven fixtures are now `internal`, with `[assembly: InternalsVisibleTo("TestCompilation")]` so the Roslyn harness in `OperatorCompatibilityTests` — which consumes them as an external API — still sees them.

Solution builds; all four suites pass at 2176 / 173 / 159 / 23 = 2531 tests; whitespace, style and analyzer verification clean.

### Bound algorithm rungs: kept, but they are the exception

`Algorithm<TSelf, TCandidate, TSearchSpace, TProblem, TSearchState>` and its iterative sibling name the two type arguments everything else shed, which looks like a contradiction. It is not, for a reason worth stating plainly: **the rung adds no arity to any configuration.** `DynamicRacingAlgorithm<TCandidate, TSearchSpace, TProblem, TSearchState, TAlgorithm>` declared all five while sitting on the *unbound* base; it named them because it reads `problem.EpochClock`, which exists on `DynamicProblem` and not on `IProblem`. There is no way to write that algorithm with fewer. The rung's only job is to own the bridge, and moving it there deleted eighteen lines of hand-written bridge including a wrong variance check and two unchecked casts. Without the rung, every bound algorithm re-writes that bridge and can get it wrong the same way.

Same rule as the operators, and the operator ladder has more real users of its bound rung than the algorithm ladder does: three symbolic-regression refiners that genuinely need `SymbolicRegressionProblem`. The migration's thesis was never that nothing names a problem, but that a configuration names only what it is *about* — and this algorithm is about dynamic problems.

What was wrong was the **presentation**, and that is fixed:

- `Algorithm<…5…>` had zero users in `src` and seven test doubles that all named `IProblem<int, DummySearchSpace<int>>` — the widest problem, so binding to nothing while paying full arity. `AdditiveStepAlgorithm` spelled the pair five times in a thirty-two-line file to use only `problem.SearchSpace`. All seven moved to the three-argument base with a generic nested instance, the shape `GeneticAlgorithm` already used.
- Both bound rungs now carry the selection rule in their own remarks. Only the unbound `Algorithm` had it, which is the one place it is not needed.

Deliberately left bound: `BatchEvaluationAlgorithm` (reads `IntegerDynamicProblem`), the five `OperatorCompatibilityTests` fixtures (the matrix exists to prove bound operators get refused by incompatible runs), and `AlgorithmAuthoringSpecs`' `SingleCreateAlgorithm`/`DoubleCreateAlgorithm` — those two read no concrete problem member, but their specs are *about* the bound rung's surface, including a reflection assertion for the non-generic single-parameter `CreateExecutionInstance` overload that exists only there.

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

**3. The remaining eight roles**, one per commit, in ascending order of coupling: replacers, selectors, creators, crossovers, evaluators, refiners, terminators, interceptors. Then the stragglers: the three experimental move roles and `IVariableStrengthMutator`, which are what let `IOperator<TExecutionInstance>` be deleted.

**4. Algorithms. Done** — see *Package 4* above.
`Algorithm` and `IterativeAlgorithm` bases first, then the concrete algorithms, then the composition and control algorithms. Drop the now-redundant reduced-arity records (`GeneticAlgorithm<TCandidate, TSearchSpace>` and `GeneticAlgorithm<TCandidate>` collapse into one type).

**5. Observation and experiments. Done** — see *Package 4* above, which absorbed it.
The constraint rework this package anticipated did not happen: `IExecutionInstanceResolvable<TExecutionInstance>` was deleted instead, having turned out to have no production implementers. `RegisterReplacement` had already become non-generic in package 1.

**6. Factories, analyzers, documentation. Factories done; documentation deliberately deferred.**
(The defaults namespace move that was parked here is done; see above.)
`For(...)` and `Create(...)` companions simplify, since fewer arguments need inferring — done, see *The algorithm factories name only the candidate* above. Update both analyzers and the code fix. Update the seven documentation pages and the examples.

Documentation is held until the reworked API is judged stable, so it is not a gap to close yet — but two pages are known to be wrong now, and both mislead in the same direction, toward more type arguments rather than fewer:

- [writing-algorithms.md](../docs/guide/extending/writing-algorithms.md) teaches the *bound* five-argument `IterativeAlgorithm` as the way to write an algorithm, with `ICreator<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>` operator slots. Its example reads no concrete problem member, so it should be on the three-argument base. As the first page an algorithm author reads, it currently presents maximum arity as normal.
- [writing-meta-algorithms.md](../docs/guide/extending/writing-meta-algorithms.md) still shows the pre-migration `IAlgorithm<TCandidate, TSearchSpace, TProblem, TSearchState>`, which no longer exists.

The bound rungs themselves stay — see *Bound algorithm rungs* below.

**7. Cleanup and exit. Partly done.**
Delete every transitional overload and adapter. Verify no `Resolve` overload survives beyond the per-role ones on `ExecutionInstanceResolver`. Update `NoviceFrictionSpecs`, whose whole point is to show a visible diff when this lands.

Done: `IOperator<TExecutionInstance>` and `IExecutionInstanceResolvable<TExecutionInstance>` are gone, `NoviceFrictionSpecs` records the new shape, and the three uncallable extension blocks are re-anchored. `CreateExecutionInstanceAnalyzer` is narrowed to require exactly one `ExecutionInstanceRegistry` parameter, dropping the zero-argument spelling that no longer exists.

### HLib0001 had no tests, and writing them found two defects

The rule is `DiagnosticSeverity.Error` and is the only mechanical enforcement of resolution going through the registry, yet it identified what it guards by two literal names with no test holding it in place. The asymmetry is the argument for testing it: a false positive fails a build loudly, while the analyzer quietly ceasing to fire looks exactly like success. `CreateExecutionInstanceAnalyzerTests` now covers five cases — the generic interface shape every migrated role actually has, the non-generic bound shape, the correct `registry.Resolve` spelling, a caller that is not itself a creation method, and a call to the type's own overload.

Two defects the five cases did not cover, found by probing and since fixed:

- **The analyzer was blind inside explicit interface implementations.** `containingMethodSymbol.Name` is the *qualified* name there (`HEAL.HeuristicLib.Operators.IMutator<int>.CreateExecutionInstance`), so the name comparison failed and analysis returned before looking at anything. Every role base writes its bridge as exactly such a method, so a consumer writing a composite the same way had no guard at all. `IsCreationMethod` now matches `ExplicitInterfaceImplementations` as well as `Name`.
- **A `base.CreateExecutionInstance(registry)` call was reported**, though the code comment claimed base calls were ignored. The exclusion compared containing types, and for a base call the target's containing type is the base, not the caller. A derived operator wanting its base's instance has no other spelling, since `registry.Resolve(this)` would resolve back to itself.

**The receiver, not the containing type, is what identifies a self call.** Replacing the type comparison with a check for a `this`, `base` or implicit receiver fixed the base call and closed a third hole in the same line: a child that happens to share its holder's type was waved through as if it were a self call, when it is owned and must be resolved. All three cases are covered; eight specs in total.

Both fixes shipped together because the risky direction turned out not to be risky: the widened rule reaches every role base's bridge, and those bridges call their own overload on an implicit receiver, so nothing in `src` newly fails.

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

### Tested and false: block versus method placement of extension type parameters

`ObservableMutator` declares `TSearchSpace`/`TProblem` on the member and only `TCandidate` on the extension block,
while the other nine observable roles declare all of them on the block. That looked like an inference win worth
copying — one fewer argument at a call site that has to name any of them.

It is not. A C# extension block's type parameters **merge** into the emitted static method, so an explicit
type-argument list must supply the whole merged set regardless of where each one is written. Moving the refiner's two
onto the member and updating its call sites to the shorter list fails to compile: the merged arity is still three.
Placement is a source-organisation choice, not an inference one.

Worse, the failure is confusing. Explicit lists on `ObserveWith` are matched by arity across all ten roles, so a
two-argument list on an `IRefiner` receiver reports a constraint violation against
`ObservableAlgorithmExtensions.extension<TCandidate, TSearchState>(IAlgorithm<TCandidate, TSearchState>)` — an
unrelated role. Only the count is wrong, and the diagnostic names the wrong type entirely.

What did help was the same widest-instantiation move used for the analyzers. `ObservableTerminator`'s `Action<bool>`
overload reads nothing but the terminal flag, so it is written at the widest state as well as the widest search space
and problem, and `terminator.ObserveWith(_ => …)` now names nothing. That brought it in line with the five roles whose
equivalent overload already inferred everything; the terminator had been the only one requiring two arguments.

Of 67 `ObserveWith` calls in the tree, 61 already infer fully. The rest pass a lambda, which carries no types to infer
from — the floor for those, and not something placement can lower.

### The analysis layer, and the two blocks that look like it but are not

The analyzer factories were the last place a caller wrote a type-argument list the compiler could have supplied. Nine
of ten forced three or four, because `TS`, `TP` and `TR` appeared only in the return type. No analyzer reads a search
space at all, and those reading the problem use `problem.Objective` or `problem.Evaluate` — both on the widest
`IProblem`. Every observer contract is contravariant in the search space, the problem *and* the search state, so an
analysis written at the widest of all three serves any run.

Each factory now has a form naming only the candidate, delegating to the bound form that stays for an analysis that
genuinely reads a concrete problem. Twenty call sites dropped their lists, including
`Analyzer.BestMedianWorst<SymbolicExpressionTree, SymbolicExpressionTreeSearchSpace, IProblem<…>, PopulationState<…>>`.
The `PythonInterop` helper that existed only to forward those arguments lost two of its four type parameters as a
result. One call site in `AlgorithmObservationTests` keeps the bound spelling on purpose: it is the only place a
narrowed analysis is shown running.

**Two nearby blocks look like the same defect and are not.** Both were surveyed as mechanical wins; neither is.

- `DynamicCachedEvaluatorExtension.WithCache` — **resolved by putting `EpochClock` on `IDynamicProblem`.** `TSearchSpace` had appeared only in the `where TProblem : DynamicProblem<TProblem, TCandidate, TSearchSpace>` clause, so all four arguments had to be written. With the clock reachable through the interface, the evaluator holds `IDynamicProblem<TCandidate, TSearchSpace>` instead of a concrete `TProblem`, drops that parameter, and takes its search space from a parameter's type rather than a constraint. Eight call sites went from four type arguments to none.

  `EvaluationClock` is exposed rather than `IEpochClock`, deliberately: `AdvanceEpoch`, `PendingEpochs` and `ResolvePendingEpochs` are on the class and not the interface, so narrowing the property would have pulled three more members onto a contract that dynamic problems may still reshape. The smaller change is the right one while this area is experimental.

  Two neighbours keep their arity for good reasons. `DynamicRelativeQualityEvaluator` hands the problem to a user supplied `IBestKnownObjectiveProvider<TCandidate, TSearchSpace, TProblem>`, whose `GetBestKnown` may read concrete members. `ReevaluationInterceptor` sits on the bound interceptor rung, so its `Transform` signature names the problem.
- **Encoding defaults for the remaining search spaces are postponed by decision**, not by oversight. `For(problem)` and `For(searchSpace)` work only where a search space declares `IEncodingDefault*` and a problem declares `IProblemDefault*` — today `PermutationSearchSpace` and `TravelingSalesmanProblem`. Adding them for the other encodings is a couple of static methods each, but it is a product decision to be taken once the architecture and API settle, and before the documentation is rewritten. Until then the restriction is a documentation entry, not a code gap.
- `CompositeSearchSpace.WithSearchSpace` — `T1` and `T2` are likewise constraint-only, but the suggested fix of re-parameterising on the candidate pair would erase `TS1`/`TS2` to `ISearchSpace<T1>`. The composite resolves its child operators at those types, and the meta-optimization search space is built from creators bound to `BoundedRealVectorSearchSpace`, which would then be refused. The arity is holding a real capability.

## Before the documentation rewrite: is the overload surface necessary?

Answered. Recorded here so the documentation rewrite can start, and so the next role added does not re-litigate
it. The question was posed because documentation written against a surface that is about to be unified would be
rewritten twice; the conclusion is that the surface stays as it is, with one naming fix already landed.

The impression that prompted this: the migration added a great many extension-method overloads, and a surface that
grows that way is usually a sign that the design is expressing something the type system should express once.

Measured on the current tree, so the discussion starts from numbers rather than the impression:

| | Count |
| --- | ---: |
| Files declaring at least one `extension` block | 105 |
| `extension` blocks | 146 |
| Public members declared in those files | 396 |

The surface is not spread evenly. It is dominated by a handful of names repeated once per operator role, which is the
shape worth interrogating:

| Member | Overloads | What varies |
| --- | ---: | --- |
| `ObserveWith` | 40 | 10 observable roles x ~4 observer shapes each |
| `Resolve` / `ResolveOptional` / `TryResolve` | 78 | one triple per role, plus the quadruple for the two state aware ones |
| `Observe` | 20 | one per role |
| `Measure*Duration` | 28 | one family per role, 4 shapes each |

That is roughly 166 of the 396 members in four families. None of them is arbitrary — each exists because a role
contract is nominal and C# cannot abstract over the role — but the repetition is mechanical enough that it should be
re-derived rather than assumed.

Questions to settle:

1. **Can the resolver triple collapse?** `Resolve`, `ResolveOptional` and `TryResolve` are declared once per role
   because each returns that role's own instance type, which is the same obstacle recorded on
   `ExecutionInstanceRegistry.Resolve`: naming the role generically would need a type parameter standing for a type
   constructor. Confirm that this is still the binding constraint, and that no interface-level restructuring
   (for example a role marker carrying its instance type as an associated type) removes it.
2. **Is `ObserveWith` at 40 overloads the minimum?** The terminator work showed one of them was removable outright by
   writing the observer at the widest state. Check the other nine roles for the same, and check whether the
   observer-shape axis (observer, params observers, full lambda, narrow lambda) needs all four everywhere or only
   where a role actually has a narrow lambda worth offering.
3. **Do `Observe` and `Measure*Duration` need a per-role spelling at all**, or can they anchor on something the roles
   already share?
4. **Which of these are load-bearing for inference** and which are only convenience? An overload that exists purely so
   a call site reads better is a different decision from one that exists so a type argument can be inferred, and the
   two should not be defended with the same argument.

Method: count first, then group by why each overload exists, then attack the largest group whose reason does not
survive scrutiny. Anything that stays should be stated once as a rule in the developer guidelines, so the next role
added does not re-litigate it.

### The answers

**1. The resolver triple does not collapse, and the constraint is not the one the question assumed.** It is stronger.
`ICreator<TCandidate>.CreateExecutionInstance<TSearchSpace, TProblem>` is a *generic method*: it is universally
quantified over the run's types, and the caller chooses them at the call. A type parameter on the interface — the
"role marker carrying its instance type as an associated type" the question proposed — can only capture a type, not a
type constructor applied later. So no interface-level restructuring reaches it; the per-role declaration is forced by
what the method quantifies over, not merely by nominal role contracts.

**2. Two receivers per role is the right count, and both are load-bearing.**
`registry.Resolve<TCandidate, TSearchSpace, TProblem>(creator)` is the canonical form: the registry is generic-less, so
it cannot infer the triple and the call site names it. `resolver.Resolve(creator)` binds that same registry to a
resolver that already knows the triple, so the call infers everything and names nothing — and under the hood it relays
straight to the registry. They are one mechanism with two spellings, not two mechanisms. Keeping only the canonical
form would put the triple on every call site inside a creation method; keeping only the resolver would remove the form
that the resolver is defined in terms of. Both stay.

This is what makes the family's scaling acceptable under the axis rule below: adding a role costs a fixed, known set of
declarations that the role's author writes once, and it costs nothing to any existing role.

**3. Instrumentation x role is justified, for the same reason.** Adding an operator role does not touch any existing
instrumentation, and adding an instrumentation concern does not touch any existing role — each new cell is written
explicitly by whoever adds the row or the column. Generalising instrumentation to a role-agnostic "operator" was tried
again here and does not work without paying for it on the hot path: a uniform `Invoke` erases the per-role signature
and violates the zero-overhead rule in the developer guidelines (§ 7.1 / § 8.1), and `DispatchProxy` violates § 7.1
outright. A source generator is the only route that keeps both the arity and the performance, and it is not worth
opening on this branch.

What *was* wrong was the naming: the instrumentation members were spelled inconsistently across roles. That is fixed,
and it is the only change this question produced.

**The rule, so the next role does not re-litigate it.** Per-role duplication is acceptable when a role's author writes
it once and existing roles are unaffected. Multiplicative scaling over an axis the role's author does not know exists
is not. The resolve family and the instrumentation family are both the first kind. Call sites stay minimal regardless:
a user writes `resolver.Resolve(creator)` or `registry.TryResolve(config, out var instance)` and nothing more.

**Consequence for the exit criteria.** Criterion 5 is met with no unification landing, so the documentation rewrite
(criterion 6) is unblocked.

## Documentation backlog

Deferred until the API settles, recorded here so nothing is rediscovered. Four of six guide pages that were compiled
against the library had a snippet that does not build, so the entries below are grouped by whether they are broken or
merely stale.

### Snippets that do not compile

| Page | Line | Failure |
| --- | --- | --- |
| `docs/guide/execution/observability-and-analysis.md` | 57 | `Analyzer.BestMedianWorst(interceptor)` — CS0411, no inferring overload for the interceptor anchor |
| `docs/contributing/architecture/analyzers.md` | 302 | `Analyzer.BestQuality(algorithm.Evaluator)` — CS0411; this factory has never had a compiling form |
| `docs/examples/custom-problem.md` | 68, 72 | `SingleSolutionProblem<TCandidate, TSearchSpace>` — CS0305, the base takes a self type first |
| `docs/examples/multi-objective.md` | 26, 31 | `RealVectorSearchSpace` — CS0246, the type is `BoundedRealVectorSearchSpace` |
| `docs/guide/extending/operator-composition.md` | 176, 177, 185, 186, 196, 197, 213, 214, 238 | Nine assignments to `init`-only properties; needs `with`. Also `CountCandidates` lives in `HEAL.HeuristicLib.Operators.Evaluators` while every sibling helper is in `HEAL.HeuristicLib.Operators`, and the page shows no `using` block |

The first two stop compiling for the reason the analysis layer still carries `TSearchSpace`/`TProblem`; they are written
against the shape that layer should have, so they start compiling on their own once it is fixed rather than needing an
edit.

### Stale against the current shape

- `docs/guide/extending/writing-algorithms.md:28` teaches the bound five argument `IterativeAlgorithm` as the default. The unbound rung is the default; the bound one is for an algorithm that reads its problem.
- `docs/guide/extending/writing-meta-algorithms.md:38, 44, 46` still shows the pre-migration four argument `IAlgorithm`.
- `docs/guide/fundamentals/algorithms.md:26, 49, 62` presents `For(problem)` as one of three peer construction forms without saying it requires a search space declaring `IEncodingDefault*` and a problem declaring `IProblemDefault*` — today only permutations and the traveling salesman problem.
- `docs/guide/getting-started.md:38` passes `selector: TournamentSelector.For(problem, tournamentSize: 2)`, which reconstructs `GeneticAlgorithmDefaults.Selector<T>()` exactly. It is the only line mentioning the problem before the run and teaches a coupling that does not exist.
- `docs/guide/execution/running-algorithms.md:54` describes `MaximumGenerations` without stating its default, which is now `GeneticAlgorithmDefaults.MaximumGenerations` rather than unbounded.
- `docs/guide/fundamentals/problems.md` never shows a problem base class signature, so the self type — which `custom-problem.md` gets wrong — is documented nowhere a reader would look.
- `README.md:36`, `docs/guide/fundamentals/search-spaces.md:11` and `docs/guide/fundamentals/operators.md:37` advertise boolean vectors. A creator, crossover and bit flip mutator now exist for `BoolVectorSearchSpace`, so these can name them.
- Eight operator `For(problem, …)` factory mentions across nine pages now take one type argument rather than two. The call sites are unchanged, so this is a check rather than an edit.

### Absent rather than wrong

- `Validate` and `ValidateAndThrow` appear in no user facing page. They are the pre-flight answer to an operator written for another search space, which otherwise fails once the run starts.
- The invariant system (`IInvariantContract`, `ISearchInvariant`, `RealVectorBounds`, `BoolVectorCardinality`) is unmentioned, while `search-spaces.md:49` warns in prose to preserve validity.

### Structural

- `GenerateDocumentationFile` **is now set in `Directory.Build.props`**, so a `<see cref>` naming a deleted type can no longer rot silently. CS1591 is suppressed and the nine diagnostics that report a comment contradicting the code are escalated to errors; the rule and its rationale are § 9.7 of the developer guidelines. Before the flag went on, building the four source projects with it reported 28 XML warnings, not the five previously recorded here: seven rotted crefs (five from the arity migration naming the pre-migration three-argument types, plus `BoundsChecker`, a type that no longer exists, and a generic-method cref), five malformed-XML errors in `RandomEnumerable`, and six `<param>` tags that no longer matched their signature. Enabling it across the whole solution then caught one further rotted cref, in `SearchSpaceCompatibilityTests.cs:63`, which named `SatisfiedInvariantKinds`, a member that exists nowhere in the tree; it now points at `Ensures`. The full solution builds clean with documentation generation on, and the generated XML ships in the package. Note that an *incremental* build does not re-emit these warnings, so verification needs `--no-incremental`.
- `SymbolicRegressionRedesignSpecs.cs:215-244` and `:252-287` are green tests whose bodies are comment blocks followed by `typeof(T).ShouldNotBeNull()`. They claim coverage of numeric optimization authoring that does not exist.
- Doc snippets are not compiled by anything. `HeuristicLib.Tests.ApiUsageSpecs` is the mechanism that would catch every entry in the first table.

## Exit criteria

1. No configuration type in `src` names `TSearchSpace` or `TProblem`.
2. Every execution instance contract still names both.
3. One resolution path: the per-role overloads on `ExecutionInstanceResolver`, with no transitional overloads left.
4. All four suites, formatting, style and analyzer verification green.
5. **Done.** The overload surface question above is answered: the surface stays as it is, the instrumentation naming fix has landed, and no unification is called for. The documentation rewrite is unblocked.
6. `NoviceFrictionSpecs` and the seven documentation pages reflect the new arities.
