# Operator implementation

::: info Advanced extension
Most applications only configure built-in operators. Use this page when your candidate representation or domain needs a new operation.
:::

Operator configurations are reusable descriptions. Operator executors perform the work and own their private mutable fields. HeuristicLib provides two authoring paths so an operator can use the smallest execution model that fits its responsibilities.

Operator configurations build their executors in a `CreateExecutor` hook that resolution calls for every build. Each construction binds an executor to contextual children and observations, initializes its private fields and reads any configured run results.

## Choose an authoring path

| Path              | Typical base              | Use when                                                                                     |
| ----------------- | ------------------------- | -------------------------------------------------------------------------------------------- |
| Stateless         | `StatelessEvaluator<...>` | The operation needs configuration values but no mutable execution data                       |
| Explicit executor | `Evaluator<...>`          | The operator resolves child operators or needs complete control over its execution structure |

The same pattern exists for creators, crossovers, mutators, selectors, replacers, interceptors and terminators.

The unprefixed role base is the common base of both paths. Stateless bases derive from it. Derive directly from the unprefixed base when authoring an explicit executor.

Some roles add a single-item base as a further stateless convenience, for operations that apply independently to each item of a batch: `SingleCandidateMutator<...>`, `SingleCandidateCrossover<...>`, `SingleCandidateCreator<...>` and `SingleCandidateEvaluator<...>`. Implement the single-item method for one item; the inherited batch-wise role operation handles deterministic per-item random forks and batching. Callers holding the specialized base may invoke the single-item method directly, while ordinary role consumers continue to use the batch role operation. A role gets such a base only when independent per-item application is a genuine shape for it, so Selector, Replacer, Interceptor and Terminator have no equivalent.

The single-item method has its own name so it stays distinct from the batch operation it drives. The default is the role's verb followed by `Candidate`: `MutateCandidate`, `CreateCandidate` and `EvaluateCandidate`. `CrossParents` is the deliberate exception, because a crossover crosses one `Parents<TCandidate>` group into one candidate, so its single-item unit is a parent group rather than a candidate. A creator receives no input at all, so `CreateCandidate` takes only the random number generator and the context it needs; the requested count belongs to the batch operation.

A single-item base owns its batch operation and seals it. A subclass that replaced the batch operation would no longer be the single-item operator its type claims to be, and the single-item method it must still implement would become unreachable. Batching is a configuration decision instead: set the `Concurrency` property to run the per-item calls concurrently. Each item receives a random number generator forked from its batch position, so the result is identical for every concurrency setting and the choice is a performance decision rather than a semantic one. An operator that genuinely needs a different batch-wise operation derives from the stateless base directly, where the batch operation is the authoring surface.

`SingleCandidateMutator` is the reference implementation of this rule; `SingleCandidateCrossover`, `SingleCandidateCreator` and `SingleCandidateEvaluator` follow it.

## Choose an arity

Each role configuration base is available at three ordinary arities. Reduced arities exist so an author who needs neither the problem nor the search space does not have to name them. Every ordinary arity of a role uses the same executor base.

Interceptor and Terminator retain `TSearchState` in these three ordinary arities because their operations transform or inspect a produced search state. Like every other operation, both receive the call's random number generator after their primary inputs at each ordinary arity. Terminator additionally provides `Terminator<TCandidate>`, the non-generic `TerminationExecutor` and matching stateless bases for termination conditions that inspect none of the operation inputs, such as elapsed time, cancellation and external operator budgets. Interceptor cannot remove `TSearchState` because it returns that type.

| Declared as                                                  | Operation receives                    | Use when                                       |
| ------------------------------------------------------------ | ------------------------------------- | ---------------------------------------------- |
| `SingleCandidateMutator<TCandidate, TSearchSpace, TProblem>` | parent, random, search space, problem | The operation depends on the problem           |
| `SingleCandidateMutator<TCandidate, TSearchSpace>`           | parent, random, search space          | The operation depends on the search space only |
| `SingleCandidateMutator<TCandidate>`                         | parent, random                        | The operation depends on the candidate only    |

```csharp
public record SwapMutator : SingleCandidateMutator<Permutation>
{
    public override Permutation MutateCandidate(Permutation parent, IRandomNumberGenerator random) =>
        parent.SwapRandomIndices(random);
}
```

Reduction changes what the author declares, not where the operator fits. **Candidate-operation slots name the operation and candidate, without run types.** `IOperator<Mutation, Permutation>` is the whole contract, so the `SwapMutator` above fills any mutator slot over permutations, and so does an operator authored at the fullest rung against a specific problem.

The arity lives in the authoring bases: the base performs the type check when a run supplies types the operator was not written for, and the stateless helper supplies the bound search space and problem to the typed author override. A consumer, field or child property never names them.

`WrappingOperator<TOperation, TCandidate>` and `MultiOperator<TOperation, TCandidate>` store one child or an ordered child collection. They declare their children, so `ExecutionSignature.Supports` asks them too, and they forward candidate contracts. Their protected `CreateExecutor<TSearchSpace, TProblem, TSearchState>(scope)` validates the configuration and constructs an executor for the run's search-space, problem and state types on every build. Resolve children through the typed scope it receives, and initialize private data on the executor. Read run results through `scope.Results.Result(configuration)`; share configurations explicitly to retain their executors across child scopes.

Use `.Named("name")` for a transparent selectable boundary and `.Retry(3)` to allow up to three attempts after a directly thrown `RetryableOperationException`. `ChoosePerInvocation` chooses one child per call. A generic wrapper of your own returns `TOperation.Wrap<TCandidate, TSearchState, TWrapper>(scope.ResolveOperator(ChildOperator), wrapper)` with a struct `IInvocationWrapper`, which works for every operation, including external ones. The wrapper can read the call's random number generator and proceed with another one or on another executor of the same operation. The [stateful composition walkthrough](/guide/extending/operator-composition#a-complete-stateful-composition) shows the complete authoring pattern.

## Stateless operators

A stateless operator configuration implements its operation logic. Its configuration values and referenced collections must remain unchanged during execution.

A stateless operation receives everything it reads as arguments: the search space and problem at the arities that name them and, for selection and replacement, the objective at every arity. The base's executor reads them from its bound problem on each call and passes them on, so the bound problem stays their only source. An explicit executor instead holds what it reads, as [explicit executors](#explicit-executors) describes.

```csharp
private sealed record FirstValueEvaluator
    : StatelessEvaluator<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>
{
    public override IReadOnlyList<ObjectiveVector> Evaluate(
        IReadOnlyList<RealVector> candidates,
        IRandomNumberGenerator random,
        BoundedRealVectorSearchSpace searchSpace,
        TestFunctionProblem problem) =>
        candidates.Select(candidate => new ObjectiveVector(candidate[0])).ToArray();
}
```

Use specialized helpers such as `SingleCandidateEvaluator` when the role offers one that exactly matches the operation shape.

## Private fields and shared executors

Mutable operators author an executor with ordinary fields. A counter or cursor is private by default. Different runs, problem objects or fresh child activations construct independent fields. Repeated resolution of the same configuration/problem pair in one scope reuses its fields.

Keep data such as a cache or an allowance in executor fields too. To share it, users share the configuration through `Sharing`, which gives every use within the sharing node one executor and therefore one set of fields. Structure the configuration so that data shared independently of other behavior is its own node. Counters and durations are configuration nodes whose executors hold the run's count or time. `AfterOperatorCountTerminator` and `AfterOperatorDurationTerminator` resolve them.

The [tested composition and cache examples](/guide/extending/operator-composition#authoring-a-composition) show private fields and a shared cache. Shared executors retain their resolved children. Authors own concurrency correctness across all participants. Executors have no automatic disposal lifecycle; dependencies requiring cleanup remain caller owned.

## Explicit executors

Derive an explicit executor from its role's executor base, such as `EvaluationExecutor<TCandidate>`. Use that base at every configuration arity and for explicit wrapping and multi executors. Pass the search space or problem the executor reads to its constructor. Type the parameter to the contract the executor uses and retain it as a primary-constructor parameter or field. Name a type's sole nested executor `Executor`.

Selection and replacement executors that rank candidates read `problem.Objective` from their bound problem. To bind an operator to another fixed objective, construct a problem with that objective once in `CreateExecutor` and resolve the operator through `scope.For<TCandidate, TSearchSpace, TProblemWithObjective, TSearchState>(problemWithThatObjective)`. All operators resolved through that view use the same problem. See [execution resolution](/contributing/architecture/execution-resolution) for declaring a child's binding when its problem type differs from the parent's.

The configuration describes reusable parameters and graph structure. For creation, crossover, mutation, evaluation, selection, refinement and replacement, the protected typed `CreateExecutor` runs for every build and exposes the problem on its scope. It resolves child configurations through the supplied scope and creates a node for that context. The executor owns typed operation logic and resolved child references; private data belongs to executor fields and `Sharing` retains selected executors across child scopes.

```csharp
private sealed record ForwardingEvaluator(IOperator<Evaluation, RealVector> Inner)
    : Evaluator<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>
{
    public static Parameter<ForwardingEvaluator, IOperator<Evaluation, RealVector>> InnerParameter { get; } =
        new(nameof(Inner), static evaluator => evaluator.Inner, static (evaluator, value) => evaluator with { Inner = value });

    public static IReadOnlyList<IParameter> Parameters { get; } = [InnerParameter];

    public override IReadOnlyList<IParameter> GetParameters() => [.. base.GetParameters(), .. Parameters];

    protected override IEvaluationExecutor<RealVector> CreateExecutor<TSearchState>(ResolutionScope<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem, TSearchState> scope)
    {
        var inner = scope.Resolve(Inner);
        return new Executor(inner);
    }

    private sealed class Executor(IEvaluationExecutor<RealVector> inner)
        : EvaluationExecutor<RealVector>
    {
        public override IReadOnlyList<ObjectiveVector> Evaluate(
            IReadOnlyList<RealVector> candidates,
            IRandomNumberGenerator random) =>
            inner.Evaluate(candidates, random);
    }
}
```

Resolve declared child operators inside `CreateExecutor`. Initialize private counters and helper data in executor fields. Read run results through their configurations, such as analyzers, counters or durations; share configurations to retain their executors. Retain the scope only when deferred graph construction is an intentional part of the operator. Algorithms resolve their configured parameters and keep parameter-derived behavior in their execution code rather than constructing hidden operators.

Return the most concrete accessible executor type that is useful to callers. A private nested executor is returned through its role specific executor base.

## Adaptable parameters

<<< ../../../test/HeuristicLib.Tests.Docs/Extending/AdaptationDocumentationSpecs.cs#adaptation-wiring{csharp}

`GaussianMutator.MutationStrength` remains the configured base value. Its optional `MutationStrengthAdaptation` child is an `IConfigurationNode<IAdaptation>`: the executor resolves it and calls `Apply(baseValue)` once per mutation invocation. `ScaleAdaptation` starts with factor 1 and `OffsetAdaptation` with offset 0. A rule writes their runtime objects through the settable `Factor` and `Offset` properties.

The strategy's slot is `ScaleAdaptation?`, the form its 1/5 success rule writes. Both slots must name the same instance. An unset strategy slot means no adaptation. Wrappers forward nothing: `Timed`, `Counted`, `PipelineMutator` and `ChoosePerInvocation` can surround mutators that read the same link. Several mutators can read one adaptation with different base values. External nodes may implement `IConfigurationNode<IAdaptation>` directly, including as records.

A scope resolves one runtime adaptation per configuration reference. Independent runs remain independent. To retain adaptation across Cycle turns, share the adaptation on the Cycle. Sharing the mutator also requires sharing the adaptation: otherwise the retained mutator reads its original adaptation while each fresh strategy writes a different one. The resolver does not detect unused links.

## Wrapping and multi operators

Every operation uses `WrappingOperator<TOperation, TCandidate>` and `MultiOperator<TOperation, TCandidate>`, with `ChildOperator` and `ChildOperators` configuration properties. Their executors hold the resolved children themselves. Keep private data in executor fields, read run results through their configurations, such as counters or durations, and resolve current children inside `CreateExecutor`. Existing executors keep their original children when a new context is resolved.

When a child plays a specific part in the operation, derive from `Operator<TOperation, TCandidate>` and declare that child yourself. This configuration excerpt omits the executor body:

```csharp
public record EliteSelector<TCandidate> : Operator<Selection, TCandidate>
{
    public required IOperator<Selection, TCandidate> SelectorForRemaining { get; init; }
    public int Elites { get; init; } = 1;

    public static Parameter<EliteSelector<TCandidate>, IOperator<Selection, TCandidate>> SelectorForRemainingParameter { get; } =
        new(nameof(SelectorForRemaining), static selector => selector.SelectorForRemaining, static (selector, value) => selector with { SelectorForRemaining = value });

    public static Parameter<EliteSelector<TCandidate>, int> ElitesParameter { get; } =
        new(nameof(Elites), static selector => selector.Elites, static (selector, value) => selector with { Elites = value });

    public static IReadOnlyList<IParameter> Parameters { get; } = [SelectorForRemainingParameter, ElitesParameter];

    public override IReadOnlyList<IParameter> GetParameters() => [.. base.GetParameters(), .. Parameters];

    protected override IExecutor CreateExecutor<TSearchSpace, TProblem, TSearchState>(
        ResolutionScope<TCandidate, TSearchSpace, TProblem, TSearchState> scope)
    {
        var selectorForRemaining = scope.Resolve(SelectorForRemaining);
        return new Executor(selectorForRemaining, Elites, scope.Problem);
    }

    // Executor derives from SelectionExecutor<TCandidate> and reads the bound problem's objective when ranking elites.
}
```

`EliteSelector` takes its elites first and asks `SelectorForRemaining` only for the places that are left, so calling that child `ChildSelector` would hide what it does. Wrapping it and adding a second, better-named property is not the fix. The two properties return the same object and a reader cannot tell which one to use. Declaring the child directly also lets the constructor fix how many children there are. `GenderSpecificSelector` needs exactly one `FemaleSelector` and one `MaleSelector`, which a multi base cannot express.

Like every composite, these name only the candidate. A child slot is `IOperator<Selection, TCandidate>`, and the run's search space and problem arrive as method type arguments on `CreateExecutor`. See [Choose an arity](#choose-an-arity) for what that costs the author and why it costs the consumer nothing.

Composition helpers such as choosing one child, applying a transformation and running a pipeline are covered in [Operator composition](/guide/extending/operator-composition).

`ChoosePerItem` routes through `WeightedRouter<TChoice>`. It passes a router over the resolved children to its operation's `Route`, whose routed executor assigns every item of a call with the call's random number generator; algorithm code uses the router directly. Create it with `WeightedRouter.Create(choices)` for uniform routing or `WeightedRouter.Create(choices, weights)`, using `WeightedIndexSampler` semantics; weights are compiled once per router. Its `Route` overloads accept either inputs or an item count, an RNG and a handler that receives one choice and its batch of inputs or item positions. It assigns every input before handling any batch, handles the batches in choice order, skips empty batches and restores results to their original positions. When every input goes to the same choice, that choice receives the original inputs. Each handler must return exactly the batch size; a mismatch throws `InvalidOperationException`. `ChoosePerInvocation` chooses once for the whole invocation and samples its child from a `WeightedItemSampler<T>`, using the call's random number generator for the choice and the child.

Algorithms use the router to apply parameter-derived behavior in their execution code. GA, NSGA2 and ALPS create `WeightedRouter.AtRate(mutator, mutationRate)` in their executor constructor. The router owns the resolved mutator and a null pass-through choice, so the executor retains only the router for mutation. It routes each offspring to the mutator with the rate's probability, and the algorithm mutates only that batch. Rates at most zero, negative infinity and NaN route every offspring to null; rates at least one and positive infinity route every offspring to the mutator, which receives the original offspring. Neither extreme draws from the RNG. The algorithms use the step's same RNG for routing and mutation, so observers count only offspring routed to the configured mutator. `WeightedRouter.AtRate(rate)` supplies boolean choices for callers that need a decision, and `AppliedAtRate` remains a configuration helper for explicitly configured compositions using the same rate weights.

## Define a new operation

When no existing role fits, define an operation. It has an executor interface with one named method, an operation type that implements `IOperation<TSelf>` and wraps that executor, and typed resolution extensions. Operators then implement `IOperator<TOperation, TCandidate>` for it, and naming, retry, choice, counting, timing and custom wrappers work without further code. A built-in operation is defined the same way.

The named method ends with the call's random number generator. The executor received its problem during construction, so the call carries neither search space nor problem:

<<< ../../../test/HeuristicLib.Tests.Docs/Architecture/OperationDefinitionDocumentationSpecs.cs#operation-executor{csharp}

The operation implements the static members of `IOperation<TSelf>` explicitly. It recognizes its executor interface, wraps an executor in a private wrapping executor, and holds a private `Invocation` struct for one call. An item operation, marked with `IItemOperation<TSelf>`, also routes a call's items to several executors through a private routed executor. The library bases `WrappingExecutor<TExecutor, TWrapper>` and `RoutingExecutor<TExecutor>` hold the executors, the wrapper and the router, type them as the operation's executor interface:

<<< ../../../test/HeuristicLib.Tests.Docs/Architecture/OperationDefinitionDocumentationSpecs.cs#operation-type{csharp}

The invocation is what every wrapper receives: its `Random` and `Proceed`. An operation whose calls do not split into items implements `IOperation<TSelf>` instead and needs no routed executor. An operation with candidate work returns true from `SupportsCandidateWork` and implements `CountCandidateWork` on its invocation. The static members, the invocation and the two executors stay per operation: C# needs a nominal class for each executor interface, a struct has no base class, and the operation's static members are the only place that names the executor interface for the run's types.

Resolution extensions cast the resolved executor to the operation's interface. Creation, crossover, evaluation, mutation, refinement, replacement and selection each declare one block for the untyped scope with an explicit problem and one for `ResolutionScope<TCandidate, TSearchSpace, TProblem>`. The typed block also serves four-argument scopes through their base class. Resolution uses the scope's search state even when a four-argument scope is held as a three-argument scope; a scope without a search-state argument uses `ISearchState`. Termination and interception retain their four-argument blocks because their executor interfaces name the search state.

`TryResolve` calls `TryResolveOperator`, which takes the interface from the executor variable and reports a failure as a reason:

<<< ../../../test/HeuristicLib.Tests.Docs/Architecture/OperationDefinitionDocumentationSpecs.cs#operation-resolution{csharp}

### Make the operation observable

An operation is observable once it declares two parts beside the operation, as each built-in operation does: an observation record, which implements `IInvocationObservation<TOperation, TCandidate, TSelf>` to make an executor report it, and an `Invocations` source. The record carries the call's inputs and outputs together with the search space and problem its executor was bound to, so a trace can rank by that problem's objective:

<<< ../../../test/HeuristicLib.Tests.Docs/Architecture/OperationDefinitionDocumentationSpecs.cs#operation-observation{csharp}

`Analyzer.Trace(estimator.Invocations, observation => observation.Estimates.Count)` and `estimator.Invocations.Observing(…)` then work as they do for built-in operations. The `Observed` form takes an `IObservationReceiver<TObservation>` in its `receiver` slot and binds that receiver's recorder when constructed. `Wrap` casts because it receives the executor untyped, and resolution has already checked that the executor performs the operation.

## Scaffolding roles and cross-cutting concerns

Coding agents are useful optional aids for the repetitive parts of operator authoring. Adding a concrete operator to an existing role is usually small work, because deriving from an existing authoring base is most of it. A new operator role is not: it brings a whole family with it, including a configuration arity ladder and an executor base, stateless bases, applicable single-item bases, wrapping and multi topologies, construction companions, tests and API usage specs. A new cross-cutting concern brings one role-specific adapter for every applicable role. That expansion is mechanical enough for an agent to draft and large enough to make agent assistance worthwhile.

Agent-produced code is normal contributor-owned source. Inspect and refactor it normally, review every public member as a deliberate API addition, and validate it with the compiler, analyzers, focused behavior tests and API usage specs. Point the agent at the current contracts and guidelines in addition to the closest accepted role family: all nine current roles follow the accepted topology, but their operation inputs, applicability and lifecycle semantics still differ.

An agent will not settle a concern's semantics for you. Implement and review the concern for one representative role first, then state its lifecycle and applicability independently of that role before expanding it. Similar signatures do not prove identical semantics: duration measurement records failed calls because it updates in `finally`, while counting updates only after a successful child call; candidate-count conveniences apply only when the role produces an appropriate candidate collection.

## Configuration and execution rules

- Keep configuration values unchanged during execution. Retained collection inputs are immutable snapshots.
- Keep randomness explicit in operation calls and bind the problem explicitly during executor construction.
- Store mutable execution data in executor fields or run results; use `Sharing` to retain selected executors across child scopes.
- Use `Sharing` when child scopes should retain a selected configuration's executor and its data.
- Each scope keeps one executor per configuration/problem reference pair for algorithms and operators, and per configuration reference for configurations whose executor needs no problem, such as counters. Child scopes create independent executors except for explicitly shared configurations; all child scopes use the root's run-bound results. Separate roots have independent results.
- Do not assume stateful operation calls are serialized unless the owning execution path guarantees it.

## Roslyn analyzer guardrails

The `OperatorAuthoringAnalyzer` Roslyn analyzer assists with common mistakes across operator roles:

- `HLib0003` reports direct mutation of configuration members outside construction, in operators, algorithms and consumer-defined roles alike.

These diagnostics are guardrails, not a proof that every invariant is satisfied. They cannot reliably identify every indirect mutation through a referenced collection, helper object or delegate.

## Algorithms use the explicit path

Algorithms coordinate operators and execution flow, so they always use a configuration paired with an explicitly authored executor. See [Writing algorithms](/guide/extending/writing-algorithms) for the public authoring model.

## Related pages

- [Operators](/guide/fundamentals/operators)
- [Operator composition](/guide/extending/operator-composition)
- [Running algorithms](/guide/execution/running-algorithms)
- [Writing algorithms](/guide/extending/writing-algorithms)
