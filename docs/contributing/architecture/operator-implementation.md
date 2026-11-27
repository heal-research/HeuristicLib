# Operator implementation

[Writing operators](/extend/writing-operators) is the authoring guide. This page states the rules behind the operator bases, for contributors who change an operator family or add a role.

Operator configurations build their executors in a `CreateExecutor` hook that resolution calls for every build ([Execution nodes](/contributing/architecture/execution-nodes#construction-hooks)). Each construction resolves the executor's children through its scope, initializes its private fields and reads any configured run results.

## Single-candidate bases

Some roles add a single-candidate base as a further stateless convenience, for operations that apply independently to each item of a batch: `SingleCandidateMutator<...>`, `SingleCandidateCrossover<...>`, `SingleCandidateCreator<...>`, `SingleCandidateRefiner<...>` and `SingleCandidateEvaluator<...>`. A role gets such a base only when independent per-item application is a genuine shape for it, so Selector, Replacer, Interceptor and Terminator have no equivalent. Developer guidelines [§ 8.14](/contributing/developer-guidelines#§-8-14-say-item-for-a-split-call-and-candidate-for-the-candidates) states when a name says item and when it says candidate.

The single-candidate method has its own name so it stays distinct from the batch operation it drives. The default is the role's verb followed by `Candidate`: `MutateCandidate`, `CreateCandidate`, `RefineCandidate` and `EvaluateCandidate`. `CrossParents` is the deliberate exception, because a crossover crosses one `Parents<TCandidate>` group into one candidate, so its unit is a parent group rather than a candidate. A creator receives no input at all, so `CreateCandidate` takes only the random number generator and the context it needs. The requested count belongs to the batch operation.

A single-candidate base owns its batch operation and seals it. A subclass that replaced the batch operation would no longer be the single-candidate operator its type claims to be, and the single-candidate method it must still implement would become unreachable. Batching is a configuration decision instead: set the `Concurrency` property to run the per-item calls concurrently. Each item receives a random number generator forked from its batch position, so the result is identical for every concurrency setting and the choice is a performance decision rather than a semantic one. An operator that genuinely needs a different batch-wise operation derives from the stateless base directly, where the batch operation is the authoring surface.

`SingleCandidateMutator` is the reference implementation of this rule. `SingleCandidateCrossover`, `SingleCandidateCreator`, `SingleCandidateRefiner` and `SingleCandidateEvaluator` follow it.

## Arities

Every ordinary arity of a role uses the same executor base. The arity lives in the authoring bases: the base performs the type check when a run supplies types the operator was not written for, and the stateless helper supplies the bound search space and problem to the typed author override. A consumer, field or child property never names them.

Interceptor and Terminator retain `TSearchState` in these three ordinary arities because their operations transform or inspect a produced search state. Like every other operation, both receive the call's random number generator after their primary inputs at each ordinary arity. Terminator additionally provides `Terminator<TCandidate>`, the non-generic `TerminationExecutor` and matching stateless bases for termination conditions that inspect none of the operation inputs, such as elapsed time, cancellation and external operator budgets. Interceptor cannot remove `TSearchState` because it returns that type.

The topology bases `WrappingOperator` and `MultiOperator` have no reduced arities (§ 8.5).

## Inputs and executors

A stateless operation receives everything it reads as arguments: the search space and problem at the arities that name them and, for selection and replacement, the objective at every arity. The base's executor reads them from its bound problem on each call and passes them on, so the bound problem stays their only source. An explicit executor instead holds what it reads, in its constructor.

Derive an explicit executor from its role's executor base, such as `EvaluationExecutor<TCandidate>`. Use that base at every configuration arity and for explicit wrapping and multi executors. Pass the search space or problem the executor reads to its constructor. Type the parameter to the contract the executor uses and retain it as a primary-constructor parameter or field. Name a type's sole nested executor `Executor`.

Selection and replacement executors that rank candidates read `problem.Objective` from their bound problem. To bind an operator to another fixed objective, construct a problem with that objective once in `CreateExecutor` and resolve the operator through `scope.For<TCandidate, TSearchSpace, TProblemWithObjective, TSearchState>(problemWithThatObjective)`. All operators resolved through that view use the same problem. See [execution resolution](/contributing/architecture/execution-resolution) for declaring a child's binding when its problem type differs from the parent's.

Resolve declared child operators inside `CreateExecutor`, initialize private counters and helper data in executor fields, and read run results through their configurations, such as analyzers, counters or durations. Developer guidelines § 4.3 and § 4.4 state when to retain the scope and how algorithms keep parameter-derived behavior in their execution code.

Return the most concrete accessible executor type that is useful to callers. A private nested executor is returned through its role specific executor base.

An operator that reads a value adjusted during the run resolves an `IConfigurationNode<IAdaptation>` child in `CreateExecutor` and applies it to its configured base value, as `GaussianMutator` does for `MutationStrength`. [Adaptation, failures and lifetime](/contributing/architecture/execution-resolution#adaptation-failures-and-lifetime) states how a scope resolves and shares an adaptation.

## Wrapping and multi operators

`WrappingOperator<TOperation, TCandidate>` and `MultiOperator<TOperation, TCandidate>` serve every operation. They store one child or an ordered child collection, with `ChildOperator` and `ChildOperators` configuration properties. They declare their children, so `ExecutionSignature.Supports` asks them too, and they forward candidate contracts. Their protected `CreateExecutor<TSearchSpace, TProblem, TSearchState>(scope)` validates the configuration and constructs an executor for the run's search-space, problem and state types on every build. Their executors hold the resolved children themselves: resolve current children through the typed scope inside `CreateExecutor`, keep private data in executor fields and read run results through their configurations, such as counters or durations. Existing executors keep their original children when a new context is resolved. [Writing compositions](/extend/writing-compositions) shows the authoring pattern.

`ChoosePerItem` routes through `WeightedRouter<TChoice>`. It passes a router over the resolved children to its operation's `Route`, whose routed executor assigns every item of a call with the call's random number generator. Algorithm code uses the router directly. Create it with `WeightedRouter.Create(choices)` for uniform routing or `WeightedRouter.Create(choices, weights)`, using `WeightedIndexSampler` semantics. Weights are compiled once per router. Its `Route` overloads accept either inputs or an item count, an RNG and a handler that receives one choice and its batch of inputs or item positions. It assigns every input before handling any batch, handles the batches in choice order, skips empty batches and restores results to their original positions. When every input goes to the same choice, that choice receives the original inputs. Each handler must return exactly the batch size. A mismatch throws `InvalidOperationException`. `ChoosePerInvocation` chooses once for the whole invocation and samples its child from a `WeightedItemSampler<T>`, using the call's random number generator for the choice and the child.

Algorithms use the router to apply parameter-derived behavior in their execution code. GA, NSGA2 and ALPS create `WeightedRouter.AtRate(mutator, mutationRate)` in their executor constructor. The router owns the resolved mutator and a null pass-through choice, so the executor retains only the router for mutation. It routes each offspring to the mutator with the rate's probability, and the algorithm mutates only that batch. Rates at most zero, negative infinity and NaN route every offspring to null. Rates at least one and positive infinity route every offspring to the mutator, which receives the original offspring. Neither extreme draws from the RNG. The algorithms use the step's same RNG for routing and mutation, so observers count only offspring routed to the configured mutator. `WeightedRouter.AtRate(rate)` supplies boolean choices for callers that need a decision, and `AppliedAtRate` remains a configuration helper for explicitly configured compositions using the same rate weights.

## Operations

A new operation consists of an executor interface, an operation type implementing `IOperation<TSelf>`, typed resolution extensions and, to be observable, an observation record and an `Invocations` source. [Defining operations](/extend/defining-operations) shows the parts. A built-in operation is defined the same way.

## Scaffolding roles and cross-cutting concerns

Coding agents are useful optional aids for the repetitive parts of operator authoring. Adding a concrete operator to an existing role is usually small work, because deriving from an existing authoring base is most of it. A new operator role is not: it brings a whole family with it, including a configuration arity ladder and an executor base, stateless bases, applicable single-candidate bases, wrapping and multi topologies, construction companions, tests and API usage specs. A new cross-cutting concern brings one role-specific adapter for every applicable role. That expansion is mechanical enough for an agent to draft and large enough to make agent assistance worthwhile.

Agent-produced code is normal contributor-owned source. Inspect and refactor it normally, review every public member as a deliberate API addition, and validate it with the compiler, analyzers, focused behavior tests and API usage specs. Point the agent at the current contracts and guidelines in addition to the closest accepted role family: all nine current roles follow the accepted topology, but their operation inputs, applicability and lifecycle semantics still differ.

An agent will not settle a concern's semantics for you. Implement and review the concern for one representative role first, then state its lifecycle and applicability independently of that role before expanding it. Similar signatures do not prove identical semantics: duration measurement records failed calls because it updates in `finally`, while counting updates only after a successful child call. Candidate-count conveniences apply only when the role produces an appropriate candidate collection.

## Configuration and execution rules

Developer guidelines § 4 states the ownership rules for configurations, executors and run results, and [Execution resolution](/contributing/architecture/execution-resolution) states how a scope keeps and shares executors. Do not assume stateful operation calls are serialized unless the owning execution path guarantees it.

## Roslyn analyzer guardrails

The repository's Roslyn analyzers assist with common mistakes in every construction hook:

- `HLib0001` (`ChildResolutionAnalyzer`) reports a direct call of a child configuration's `CreateExecutor` or `CreateExecutionNode` hook in construction hooks, custom resolvers and deferred execution methods. Resolve the child through the scope instead.
- `HLib0003` (`OperatorAuthoringAnalyzer`) reports direct mutation of configuration members outside construction, in operators, algorithms and consumer-defined roles alike.

These diagnostics are guardrails, not a proof that every invariant is satisfied. They cannot reliably identify every indirect mutation through a referenced collection, helper object or delegate.

## Algorithms use the explicit path

Algorithms coordinate operators and execution flow, so they always use a configuration paired with an explicitly authored executor. See [Writing algorithms](/extend/writing-algorithms) for the public authoring model.

## Related pages

- [Execution resolution](/contributing/architecture/execution-resolution) for how scopes build and share the executors an operator authors
- [Analyzers](/contributing/architecture/analyzers) for the observation records and sources of an operation
- [Operators](/guide/fundamentals/operators)
- [Operator composition](/guide/fundamentals/operator-composition)
- [Algorithm runs](/guide/execution/running-algorithms)
- [Writing algorithms](/extend/writing-algorithms)
