# Configuration vs execution instances

This page documents an advanced execution concept.

For most users and most extension authors, the preferred model is:

- operators: author through `Creator`, `Mutator`, `Evaluator`, `Selector`, `Crossover`, `Replacer`, `Terminator`, `Interceptor`, `Wrapping*`, and `Multi*`
- algorithms: author through `IterativeAlgorithm<...>` with explicit execution state

You usually do **not** need to work with execution instances directly.

## Why execution instances still exist

HeuristicLib keeps a separation between:

- reusable configurations
- run-bound execution instances

That separation is still useful internally for:

- run-local mutable state
- sharing the same execution instance when the same configuration object is reused in one run
- giving meta-algorithms control over whether execution state resets or persists

## `ExecutionInstanceRegistry`

`ExecutionInstanceRegistry` builds an execution graph from the configuration graph.

Important properties:

- resolution is by configuration object reference
- the same configuration object resolves to the same execution instance within one registry
- different runs can use different registries and therefore different execution graphs

`ExecutionInstanceRegistry` also implements `IExecutionInstanceResolver`, the narrow API that high-level authoring bases receive.

That means ordinary authoring code can resolve the execution instances it needs without depending on the full registry API.

## Registry replacements

Advanced execution plumbing can register explicit registry entries before resolving a configuration.

`RegisterInstance(...)` stores an already created execution instance for a configuration identity. This is useful when infrastructure code already owns the instance that should be reused by later resolution.

`RegisterReplacement(...)` stores a replacement configuration for a configuration identity. When the original configuration is resolved, the registry creates the execution instance from the replacement configuration and stores it under the original identity.

Replacement configurations may resolve the original configuration while they are being created. This supports wrapper scenarios such as observable operators, counted operators and measured operators. The registry detects that the original is already being replaced and creates the original execution instance directly for the wrapper.

Most users should not call these methods directly. They are intended for meta-algorithms, observation installation and other advanced execution infrastructure.

## Eager local resolution

The current intended model is eager, local resolution:

- when an algorithm creates its execution instance, it resolves the operators it will use into execution state
- when a wrapping or multi operator creates its execution instance, it resolves its declared inner operators into its execution state

This gives one-time resolution cost per execution instance and avoids per-call dictionary lookups during steady-state execution.

## When you should care

You usually only need to think about execution instances when you work on:

- meta-algorithms
- advanced analyzer/observation plumbing
- very low-level custom algorithms or operators that intentionally bypass the higher-level authoring bases

If you are implementing a normal operator or a normal iterative algorithm, this page is background knowledge, not the first tool you should reach for.

## Related pages

- [Algorithm](algorithm.md)
- [Execution model](execution-model.md)
