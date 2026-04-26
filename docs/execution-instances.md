# Definition vs execution instances

This page documents an internal runtime concept.

For most users and most extension authors, the preferred model is:

- operators: author through `Creator`, `Mutator`, `Evaluator`, `Selector`, `Crossover`, `Replacer`, `Terminator`, `Interceptor`, `Wrapping*`, and `Multi*`
- algorithms: author through `IterativeAlgorithm<...>` with explicit execution state

You usually do **not** need to work with execution instances directly.

## Why execution instances still exist

HeuristicLib keeps a separation between:

- reusable configured definitions
- run-bound execution objects

That separation is still useful internally for:

- run-local mutable state
- sharing the same execution object when the same definition object is reused in one run
- giving meta algorithms control over whether execution state resets or persists

## `ExecutionInstanceRegistry`

`ExecutionInstanceRegistry` builds an execution graph from the configured definition graph.

Important properties:

- resolution is by definition object reference
- the same definition object resolves to the same execution instance within one registry
- different runs can use different registries and therefore different execution graphs

`ExecutionInstanceRegistry` also implements `IExecutionInstanceResolver`, the narrow API that high-level authoring bases receive.

That means ordinary authoring code can resolve the execution instances it needs without depending on the full registry API.

## Eager local resolution

The current intended model is eager, local resolution:

- when an algorithm creates its execution instance, it resolves the operators it will use into execution state
- when a wrapping or multi operator creates its execution instance, it resolves its declared inner operators into its execution state

This gives one-time resolution cost per execution instance and avoids per-call dictionary lookups during steady-state execution.

## When you should care

You usually only need to think about execution instances when you work on:

- meta algorithms
- advanced analyzer/observation plumbing
- very low-level custom algorithms or operators that intentionally bypass the higher-level authoring bases

If you are implementing a normal operator or a normal iterative algorithm, this page is background knowledge, not the first tool you should reach for.

## Related pages

- [Algorithm](algorithm.md)
- [Execution model](execution-model.md)
