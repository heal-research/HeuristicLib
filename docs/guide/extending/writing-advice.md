# Writing custom advice

Write advice when a runtime policy should wrap selected algorithm or operator executions without adding child configurations to the algorithm's configuration graph. Examples include recording a domain-specific event or bridging an operation to a host application's logging system.

Use a configured operator or composition when its settings and children belong in the reusable algorithm configuration. General advice can also change behavior, but predefined inspection advice observes without intentionally changing inputs or outputs. Mutation and evaluation advice preserves the selected executor's control provider.

## Select a role and implement its advice

This example logs each successful mutation batch with its selected configuration, call number and offspring count. Derive from `OperatorAdvice<Mutation, TCandidate>` because the advice wraps mutation executions. The class is runtime machinery and holds a callback; it is not a persistable configuration record.

Use these imports in a C# project with implicit system usings enabled:

<<< ../../../test/HeuristicLib.Tests.Docs/Extending/CustomAdviceDocumentationSpecs.cs#custom-advice-imports{csharp}

<<< ../../../test/HeuristicLib.Tests.Docs/Extending/CustomAdviceDocumentationSpecs.cs#custom-advice-type{csharp}

`CreateWrapperFactory` receives the selected configuration once per advice instance and configuration owner. Its returned `Factory` retains the call count. For each run signature, `GetWrapperFactory` selects `adapter.WrapInvocation(this)`. During executor construction, `Wrap` receives the typed child-call function and the operation's candidate-count function. It returns a function that calls the child, calculates the count and records the message after success. Advice neither resolves its source again nor constructs a configuration wrapper.

The prepared call counter survives rebuilding the execution binding. Each executor retains its own child. The mutation adapter forwards parents, random source, search space and problem unchanged and defines candidate work as the returned offspring count. The logging factory chooses to calculate that count after the child succeeds. If the child throws, no message is written; an exception from `write` propagates after the child has completed.

This counter expects sequential calls for a join point. A shared logging sink must also synchronize its own writes if several executions can call it concurrently. Advice preparation and shared state do not provide synchronization.

## Pair the advice with a pointcut and add it to a run

The pointcut selects which configurations receive the behavior; it is separate from the advice implementation. `Aspect.Create` checks that the pointcut and advice target compatible roles.

Run this code from an async method, passing your host application's `cancellationToken`:

<<< ../../../test/HeuristicLib.Tests.Docs/Extending/CustomAdviceDocumentationSpecs.cs#custom-advice-run{csharp}

The initial population is created and evaluated without mutation. The next two generations each invoke the configured mutator on four offspring, so `messages` contains call numbers one and two, each with an offspring count of four. The no-change mutator keeps the example focused on logging rather than mutation policy.

Add the aspect while the run is preparing. The run collects it; resolution constructs the advised executor when the run starts. The same aspect can be passed to `ResolutionScope.Create`, `With` or `CreateChildScope` for direct execution construction.

To cover every mutator over real vectors, replace `Pointcut.Reference(mutator)` with `Pointcut.OfType<IOperator<Mutation, RealVector>>()`. Each selected configuration has its own prepared call counter, while all messages go to the supplied sink. Reference pointcuts use object identity, so a copy made with `with` is a separate source. See [Observability and analysis](/guide/execution/observability-and-analysis#select-configurations-by-properties-or-names) for property and name matching.

## Keep ownership and ordering explicit

Rebinding the same selected configuration in the same state retains its call counter. Resolving it in an independent root prepares a new counter, even when the advice object is reused. The callback's sink is caller-owned: reusing `messages` intentionally retains messages from every run using it. Create a fresh sink for independent logs.

Earlier advice wraps closer to the source executor. The logging callback runs after its inner operation returns, so timing placed before this logging advice excludes the callback's work. Reuse one advice instance for one pointcut. If the same instance is paired twice, only its first pairing applies; use a combined pointcut or separate advice instances for separate selections.

See [Execution resolution](/contributing/architecture/execution-resolution#pointcuts-advice-and-join-points) for join-point preparation and [Operator composition](/guide/extending/operator-composition#authoring-a-composition) for configured wrappers with declared children.
