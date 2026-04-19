# Definition vs execution instances

This page documents an internal runtime concept.

For most users and most extension authors, the preferred model is:

- operators: author through the stateful/stateless, wrapping, and multi base classes
- algorithms: author through `IterativeAlgorithm<...>` or `StatefulIterativeAlgorithm<...>` with `IOperatorExecutor`

You usually do **not** need to work with execution instances directly.

## Why execution instances still exist

HeuristicLib keeps a separation between:

- reusable configured definitions
- run-bound execution objects

That separation is still useful internally for:

- run-local mutable state
- sharing the same runtime object when the same definition object is reused in one run
- giving meta algorithms control over whether runtime state resets or persists

## `ExecutionInstanceRegistry`

`ExecutionInstanceRegistry` builds an execution graph from the configured definition graph.

Important properties:

- resolution is by definition object reference
- the same definition object resolves to the same execution instance within one registry
- different runs can use different registries and therefore different execution graphs

This is the infrastructure that makes `IOperatorExecutor` and the simplified authoring bases work correctly behind the scenes.

## When you should care

You usually only need to think about execution instances when you work on:

- meta algorithms
- advanced analyzer/observation plumbing
- very low-level custom algorithms or operators that intentionally bypass the higher-level authoring bases

If you are implementing a normal operator or a normal iterative algorithm, this page is background knowledge, not the first tool you should reach for.

## Related pages

- [Algorithm](algorithm.md)
- [Execution model](execution-model.md)
