# Writing custom advice

Write advice when a runtime policy should wrap selected algorithm or operator executions without adding child configurations to the algorithm's configuration graph. Examples include recording a domain-specific event or bridging an operation to a host application's logging system.

Use a configured operator or composition when its settings and children belong in the reusable algorithm configuration. General advice can also change behavior, but predefined inspection advice observes without intentionally changing inputs or outputs.

## Wrap one role's executors

This example logs each successful mutation batch with its selected configuration, call number and offspring count. Derive from `MutatorAdvice<TCandidate>` because the advice wraps mutation executors; `EvaluatorAdvice<TCandidate>` is the evaluation counterpart. The class is runtime machinery and holds a callback; it is not a persistable configuration record.

Use these imports in a C# project with implicit system usings enabled:

<<< ../../../test/HeuristicLib.Tests.Docs/Extending/CustomAdviceDocumentationSpecs.cs#custom-advice-imports{csharp}

<<< ../../../test/HeuristicLib.Tests.Docs/Extending/CustomAdviceDocumentationSpecs.cs#custom-advice-type{csharp}

Resolution calls `Wrap` for every executor it builds for a selected mutator, passing that executor and the selected configuration. The returned executor calls the wrapped one, records the message after success and returns the offspring unchanged. If the mutator throws, no message is written; an exception from `write` propagates after the mutator has completed. Advice neither resolves its source again nor constructs a configuration wrapper.

The executor implements `IExecutorControlProvider` to pass on the wrapped executor's controls, so an algorithm that adapts its mutator, such as an evolution strategy, still finds them behind the logging wrapper. Override `Check` to reject a mutator before its executor is built, for example when the advice reads a problem type the run does not supply.

The call counter belongs to the advice object. Every executor built for its selected mutators, in every scope and run it is attached to, advances the same counter. Create one advice per run when the numbering should restart. This counter expects sequential calls; a shared logging sink must also synchronize its own writes if several executions can call it concurrently.

## Pair the advice with a pointcut and add it to a run

The pointcut selects which configurations receive the behavior; it is separate from the advice implementation. `Aspect.Create` checks that the pointcut and advice target compatible roles.

Run this code from an async method, passing your host application's `cancellationToken`:

<<< ../../../test/HeuristicLib.Tests.Docs/Extending/CustomAdviceDocumentationSpecs.cs#custom-advice-run{csharp}

The initial population is created and evaluated without mutation. The next two generations each invoke the configured mutator on four offspring, so `messages` contains call numbers one and two, each with an offspring count of four. The no-change mutator keeps the example focused on logging rather than mutation policy.

Add the aspect while the run is preparing. The run collects it; resolution constructs the advised executor when the run starts. The same aspect can be passed to `ResolutionScope.Create`, `With` or `CreateChildScope` for direct execution construction.

To cover every mutator over real vectors, replace `Pointcut.Reference(mutator)` with `Pointcut.OfType<IOperator<Mutation, RealVector>>()`. All selected mutators then share the advice's counter, and each message names the mutator that ran. Reference pointcuts use object identity, so a copy made with `with` is a separate source. See [Observability and analysis](/guide/execution/observability-and-analysis#select-configurations-by-properties-or-names) for property and name matching.

## Wrap every operation

Advice that does not depend on an operation's inputs, such as timing, derives from `OperatorAdvice` and works for every operation, including operations defined outside the library. It passes a small struct implementing `IInvocationWrapper` to the operation adapter. The wrapper receives each call as an invocation and calls `Proceed()` to run the wrapped method:

<<< ../../../test/HeuristicLib.Tests.Docs/Extending/CustomAdviceDocumentationSpecs.cs#general-advice-type{csharp}

The adapter builds one wrapping executor per selected executor and compiles the struct into it, so each call costs the same as a hand-written wrapper. The wrapper can call `Proceed()` several times, as retry does, and `invocation.CountCandidateWork(result)` returns the operation's candidate work when its adapter reports `SupportsCandidateWork`. Every operation ends with a random number generator, which the wrapper reads as `invocation.Random`; `ProceedWith(executor, random)` runs the call on another executor of the same operation or with another generator, keeping the original for an omitted argument. Executors created by `WrapInvocation` pass on the wrapped executor's controls, unless the wrapper implements `IExecutorControlProvider` and supplies them itself.

<<< ../../../test/HeuristicLib.Tests.Docs/Extending/CustomAdviceDocumentationSpecs.cs#general-advice-run{csharp}

General operator advice applies to mutation and evaluation operators; the remaining roles keep their role-specific advice bases until they move to the operation-parameterized contract.

## Keep ordering explicit

Earlier advice wraps closer to the source executor. The logging callback runs after its inner operation returns, so timing placed before this logging advice excludes the callback's work. Reuse one advice instance for one pointcut. If the same instance is paired twice, only its first pairing applies; use a combined pointcut or separate advice instances for separate selections.

See [Execution resolution](/contributing/architecture/execution-resolution#pointcuts-advice-and-join-points) for advice application and [Operator composition](/guide/extending/operator-composition#authoring-a-composition) for configured wrappers with declared children.
