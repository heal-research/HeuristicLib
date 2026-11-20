# Writing wrappers

Write a wrapper when a runtime policy should surround the executors of selected operators or algorithms. Examples include recording a domain-specific event or bridging an operation to a host application's logging system. A wrapper is a configuration node that holds the configuration it wraps as its declared child. Place it with `Substitute`, which returns a new configuration with the wrapper in place, or write it into the configuration by hand.

Library wrappers such as `Counted`, `Timed` and the observations behind `Observing` observe without changing inputs or outputs. A wrapper of your own can also change behavior. When its settings and children belong in the reusable algorithm configuration, it is an ordinary [composition](/guide/extending/operator-composition). This page builds on [Write an operator](/guide/extending/writing-operators) and [Compose operators](/guide/extending/operator-composition#authoring-a-composition).

## Wrap one operation's executors

This example logs each successful mutation batch with the mutator it wraps, a call number and the offspring count. Derive from `WrappingOperator<Mutation, TCandidate>` because the wrapper wraps mutation executors; `WrappingOperator<Evaluation, TCandidate>` is the evaluation counterpart. The record holds a callback, so it cannot be serialized and its equality depends on the callback; [Configurations](/guide/fundamentals/configurations#configurations-that-hold-delegates) explains what that costs.

Use these imports in a C# project with implicit system usings enabled:

<<< ../../../test/HeuristicLib.Tests.Docs/Extending/CustomWrapperDocumentationSpecs.cs#custom-wrapper-imports{csharp}

<<< ../../../test/HeuristicLib.Tests.Docs/Extending/CustomWrapperDocumentationSpecs.cs#custom-wrapper-type{csharp}

`CreateExecutor` resolves the child through the scope and returns a private nested `Executor` that derives from the role's executor base, here `MutationExecutor<TCandidate>`. It calls the child's executor, records the message after success and returns the offspring unchanged. If the mutator throws, no message is written; an exception from `write` propagates after the mutator has completed.

A wrapper checks its child where the check belongs. A requirement that its operation decides, such as candidate work, can go into the constructor, where it fails as the wrapper is written, or into `CreateExecutor` before the child is resolved, as `Counted` does. A requirement on the run, such as a problem type the wrapper reads, goes into `Supports(ExecutionSignature)`, which rejects the run before anything is built and leaves the scope usable. A failure in `CreateExecutor` ends the scope tree, like any construction failure.

The call counter is private executor data, so each executor numbers its own calls. Every request for the wrapper in one scope uses the same logging executor, and a child scope or another run that builds the wrapper again numbers its calls from one, unless the wrapper is [shared](/guide/fundamentals/configurations#sharing-executors). Keep a count across runs in the object `write` writes to. This counter expects sequential calls; a shared logging sink must also synchronize its own writes if several executions can call it concurrently.

## Place the wrapper

`Substitute` replaces the configuration it is given and returns the new algorithm. Create the run from the result; the original stays unchanged.

Run this code from an async method, passing your host application's `cancellationToken`:

<<< ../../../test/HeuristicLib.Tests.Docs/Extending/CustomWrapperDocumentationSpecs.cs#custom-wrapper-run{csharp}

The initial population is created and evaluated without mutation. The next two generations each invoke the configured mutator on four offspring, so `messages` contains call numbers one and two, each with an offspring count of four. The no-change mutator keeps the example focused on logging rather than mutation policy.

A target such as `mutator` selects that configuration by reference. To cover every mutator over real vectors, select by the type of the lambda's parameter instead: `algorithm.Substitute((IOperator<Mutation, RealVector> mutator) => new LoggedMutator<RealVector>(mutator, write))`. [Configurations](/guide/fundamentals/configurations#rewriting-a-graph-with-substitute) states what each selection reaches and when `Substitute` throws, and [Observability and analysis](/guide/execution/observability-and-analysis#select-configurations-by-properties-or-names) shows selections by property and name. Whoever writes a delegate that creates configurations at run time can call `Substitute` inside it, because the outer substitution cannot reach them.

## Wrap every operation

A wrapper that does not depend on an operation's inputs, such as timing, can be written once for every operation, including operations defined outside the library. Make the record generic in its operation, ask `OperationInfo.For<TOperation, TCandidate, TSearchState>()` for the operation it resolves, and pass a small struct implementing `IInvocationWrapper` to `operation.Wrap`. The wrapper receives each call as an invocation and calls `Proceed()` to run the wrapped method:

<<< ../../../test/HeuristicLib.Tests.Docs/Extending/CustomWrapperDocumentationSpecs.cs#general-wrapper-type{csharp}

`Wrap` builds one wrapping executor around the child's executor and compiles the struct into it, so each call costs the same as a hand-written wrapper. The wrapper can call `Proceed()` several times, as retry does, and `invocation.CountCandidateWork(result)` returns the operation's candidate work when the operation reports `SupportsCandidateWork`. Every operation ends with a random number generator, which the wrapper reads as `invocation.Random`; `Proceed(executor, random)` runs the call on another executor of the same operation, or with another generator, keeping the original for an omitted argument. Each executor runs against the problem it was bound to. A wrapper that needs the problem reads the typed `scope.Problem` in its own `CreateExecutor` and retains it in its wrapper struct or executor.

A wrapper record serves one operation, so place one for each operator, typed by that operator's operation:

<<< ../../../test/HeuristicLib.Tests.Docs/Extending/CustomWrapperDocumentationSpecs.cs#general-wrapper-run{csharp}

An algorithm wrapper derives from `WrappingAlgorithm<TCandidate, TSearchState>` and returns its executor from `CreateExecutor`; its executor derives from `WrappingAlgorithmExecutor<TSearchState>`. A wrapper of a consumer-defined role implements that role and declares the child it holds, so that later substitutions can reach through it.

## Read only an interface of the problem

A wrapper that needs one capability of the problem, not one particular problem, stays agnostic in the search space and the problem. Its configuration is written for the candidate alone, like every wrapper above. Three parts carry the pattern:

- `Supports` additionally requires that the run's problem implements the interface, so a run whose problem does not is rejected before anything is built.
- `CreateExecutor` reads `scope.Problem` as that interface. `Supports` has already accepted the run, so a problem that is not the interface can only be a mismatch, which `ExecutionSignature.Mismatch` reports.
- The private nested `Executor` receives the interface as its own constructor parameter and keeps nothing else of the problem.

This `OptimumGapEvaluator` subtracts the known optimum of the problem from the objective its child evaluator returns. The optimum is one number, so the wrapper serves single-objective problems only and returns a one-component vector for each candidate. `Supports` sees types, not the objective count of the problem, so `CreateExecutor` checks the count before it resolves the child and throws `InvalidOperationException` for a problem with several objectives. The wrapper works with any single-objective problem that implements `IHasKnownOptimum`, whatever its search space. Use these imports in a C# project with implicit system usings enabled:

<<< ../../../test/HeuristicLib.Tests.Docs/Extending/AgnosticWrapperDocumentationSpecs.cs#agnostic-wrapper-imports{csharp}

<<< ../../../test/HeuristicLib.Tests.Docs/Extending/AgnosticWrapperDocumentationSpecs.cs#agnostic-wrapper-type{csharp}

The example problem is a sphere shifted by its optimum, and it implements the interface beside its problem base:

<<< ../../../test/HeuristicLib.Tests.Docs/Extending/AgnosticWrapperDocumentationSpecs.cs#agnostic-wrapper-problem{csharp}

Pass the problem by its own type. The run shortcuts come in two overloads that differ in the run's `TProblem`. The overload taking `Problem<TProblem, TCandidate, TSearchSpace>` infers `TProblem` as the concrete type of the object you pass, so a problem that derives from that base and implements the interface reaches `Supports` as itself. The overload taking `IProblem<TCandidate, TSearchSpace>` types the run at the interface, which erases the capability: the same object held in a variable of that type reaches `Supports` as `IProblem<TCandidate, TSearchSpace>`, and validation rejects the run. What `Supports` sees is the signature that the parent declares for its children. An agnostic parent such as the genetic algorithm passes the run's signature on. A parent written for fixed types, such as an algorithm on the five-argument base, hands its declared problem type to its children, so the wrapper sees that type, and a declared type that lacks the interface hides it even when the run's problem has it.

Run this code from an async method, passing your host application's `cancellationToken`:

<<< ../../../test/HeuristicLib.Tests.Docs/Extending/AgnosticWrapperDocumentationSpecs.cs#agnostic-wrapper-run{csharp}

Experimental's `DynamicCachingEvaluator` uses the same pattern with `IDynamicProblem<TCandidate, ISearchSpace<TCandidate>>`. It also shows what such a wrapper keeps consistent when the problem changes under it. The cache rechecks the problem's epoch before it publishes results, so results from a child evaluation during which the epoch changed are returned but not cached. A batch with any miss resets its count of consecutive all-hit batches, and the cache requests at most one update per epoch, however many all-hit batches follow.

## Keep ordering explicit

The configuration states the order: a wrapper written inside another is closer to the executor. [Configurations](/guide/fundamentals/configurations#what-substitute-guarantees) states how the substitutions of one call and of several calls order their wrappers. A wrapper changes only the configuration it holds. Building it changes nothing for the other configurations of a scope.

## Runtime dependencies

A wrapper measures time with `scope.Results.TimeProvider`, as the library's timing wrappers do.

Supply other concrete runtime dependencies through the wrapper's properties and pass them to its executor. A wrapper resolves only its declared children and has no resource lookup. Create a resource-dependent wrapper for each run so independent runs receive independent data.

Counters and durations are configurations that wrapper placements add to. [Observability and analysis](/guide/execution/observability-and-analysis#count-time-and-observe-an-operator) shows how to create, place and read them, and [Share accounting with a terminator](/guide/execution/observability-and-analysis#share-accounting-with-a-terminator) shows how a terminator reads the same counter.

## Next steps

- [Write an algorithm](/guide/extending/writing-algorithms) when the state transition or control flow is new.
- [Execution resolution](/contributing/architecture/execution-resolution) describes how scopes build executors, and [Operator composition](/guide/extending/operator-composition#authoring-a-composition) shows configured wrappers with declared children.
