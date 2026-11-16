# Writing wrappers

Write a wrapper when a runtime policy should surround the executors of selected operators or algorithms. Examples include recording a domain-specific event or bridging an operation to a host application's logging system. A wrapper is a configuration node that holds the configuration it wraps as its declared child. Place it with `Substitute`, which returns a new configuration with the wrapper in place, or write it into the configuration by hand.

Library wrappers such as `Counted`, `Timed` and the observations behind `Observing` observe without changing inputs or outputs. A wrapper of your own can also change behavior. When its settings and children belong in the reusable algorithm configuration, it is an ordinary [composition](/guide/extending/operator-composition).

## Wrap one operation's executors

This example logs each successful mutation batch with the mutator it wraps, a call number and the offspring count. Derive from `WrappingOperator<Mutation, TCandidate>` because the wrapper wraps mutation executors; `WrappingOperator<Evaluation, TCandidate>` is the evaluation counterpart. The record holds a callback, so it cannot be serialized.

Use these imports in a C# project with implicit system usings enabled:

<<< ../../../test/HeuristicLib.Tests.Docs/Extending/CustomWrapperDocumentationSpecs.cs#custom-wrapper-imports{csharp}

<<< ../../../test/HeuristicLib.Tests.Docs/Extending/CustomWrapperDocumentationSpecs.cs#custom-wrapper-type{csharp}

`CreateExecutor` resolves the child through the scope and returns an executor that calls the child's executor, records the message after success and returns the offspring unchanged. If the mutator throws, no message is written; an exception from `write` propagates after the mutator has completed.

The executor implements `IExecutorControlProvider` to pass on the wrapped executor's controls, so an algorithm that adapts its mutator, such as an evolution strategy, still finds them behind the logging wrapper.

A wrapper checks its child where the check belongs. A requirement that its operation decides, such as candidate work, can go into the constructor, where it fails as the wrapper is written, or into `CreateExecutor` before the child is resolved, as `Counted` does. A requirement on the run, such as a problem type the wrapper reads, goes into `Supports(ExecutionSignature)`, which rejects the run before anything is built and leaves the scope usable. A failure in `CreateExecutor` ends the scope tree, like any construction failure.

The call counter is private executor data. A scope builds one executor per configuration/problem reference pair, so every request for the wrapper in one scope uses the same logging executor, and a child scope or another run that builds the wrapper again numbers its calls from one. Keep a count across runs in the object `write` writes to. This counter expects sequential calls; a shared logging sink must also synchronize its own writes if several executions can call it concurrently.

## Place the wrapper

`Substitute` replaces the configuration it is given and returns the new algorithm. Create the run from the result; the original stays unchanged.

Run this code from an async method, passing your host application's `cancellationToken`:

<<< ../../../test/HeuristicLib.Tests.Docs/Extending/CustomWrapperDocumentationSpecs.cs#custom-wrapper-run{csharp}

The initial population is created and evaluated without mutation. The next two generations each invoke the configured mutator on four offspring, so `messages` contains call numbers one and two, each with an offspring count of four. The no-change mutator keeps the example focused on logging rather than mutation policy.

A target such as `mutator` matches by reference, so a copy made with `with` is a separate configuration, and a target the configuration does not hold is an error. To cover every mutator over real vectors, select by the type of the lambda's parameter instead: `algorithm.Substitute((IOperator<Mutation, RealVector> mutator) => new LoggedMutator<RealVector>(mutator, write))`. A predicate passed before the replacement selects by properties or names. A selection by type or predicate that matches nothing changes nothing. See [Observability and analysis](/guide/execution/observability-and-analysis#select-configurations-by-properties-or-names) for selections by property and name.

A substitution reaches the children a configuration declares, including a cycle's inner algorithms. It does not reach configurations that a delegate creates at run time; whoever writes the delegate can call `Substitute` inside it.

## Wrap every operation

A wrapper that does not depend on an operation's inputs, such as timing, can be written once for every operation, including operations defined outside the library. Make the record generic in its operation, ask `OperationInfo.For<TOperation, TCandidate, TSearchState>()` for the operation it resolves, and pass a small struct implementing `IInvocationWrapper` to `operation.Wrap`. The wrapper receives each call as an invocation and calls `Proceed()` to run the wrapped method:

<<< ../../../test/HeuristicLib.Tests.Docs/Extending/CustomWrapperDocumentationSpecs.cs#general-wrapper-type{csharp}

`Wrap` builds one wrapping executor around the child's executor and compiles the struct into it, so each call costs the same as a hand-written wrapper. The wrapper can call `Proceed()` several times, as retry does, and `invocation.CountCandidateWork(result)` returns the operation's candidate work when the operation reports `SupportsCandidateWork`. Every operation ends with a random number generator, which the wrapper reads as `invocation.Random`; `Proceed(executor, random)` runs the call on another executor of the same operation, or with another generator, keeping the original for an omitted argument. Each executor runs against the problem it was bound to. A wrapper that needs the problem reads the typed `scope.Problem` in its own `CreateExecutor` and retains it in its wrapper struct or executor. Executors created by `Wrap` pass on the wrapped executor's controls, unless the wrapper implements `IExecutorControlProvider` and supplies them itself.

A wrapper record serves one operation, so place one for each operator, typed by that operator's operation:

<<< ../../../test/HeuristicLib.Tests.Docs/Extending/CustomWrapperDocumentationSpecs.cs#general-wrapper-run{csharp}

An algorithm wrapper derives from `WrappingAlgorithm<TCandidate, TSearchState>` and returns its executor from `CreateExecutor`. A wrapper of a consumer-defined role implements that role and declares the child it holds, so that later substitutions can reach through it.

## Keep ordering explicit

The configuration states the order: a wrapper written inside another is closer to the executor. Substitutions apply one after another, and each matches the original node inside the wrappers placed before it, so a later substitution, in the same call or a later one, sits closer to the node it matches. A wrapper changes only the configuration it holds. Building it changes nothing for the other configurations of a scope.

See [Execution resolution](/contributing/architecture/execution-resolution) for how scopes build executors and [Operator composition](/guide/extending/operator-composition#authoring-a-composition) for configured wrappers with declared children.

## Runtime dependencies

Supply concrete runtime dependencies through the wrapper's properties and pass them to its executor. A wrapper resolves only its declared children and has no resource lookup. Create a resource-dependent wrapper for each run so independent runs receive independent data.

Use `mutator.Invocations.Count()`, `CountCandidates()` or `Duration()` to define a counter or a duration. Attach it with `AnalyzedBy` or place it through `Counted` or `Timed`, and read its result with `run.Result(counter)`. A terminator receives the same definition and reads that run's result. Reusing a definition across runs gives independent accumulators. See [shared accounting](/guide/execution/observability-and-analysis#share-accounting-with-a-terminator).
