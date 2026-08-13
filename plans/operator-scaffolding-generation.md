# Operator Scaffolding

## Decision summary

Status: accepted on 2026-08-13.

HeuristicLib will not introduce a Roslyn source generator, another build-time code generator, or a deterministic IDE scaffolding command for operator roles or cross-cutting operator concerns at this time.

Operator types remain ordinary C# source checked into the repository. Contributors may use a coding agent to scaffold a new operator role or to adapt an accepted concern implementation across applicable roles. Agent-produced code is contributor-owned source: it must be inspectable, reviewable, refactorable, tested, and maintained in the same way as handwritten code.

There is deliberately no hidden attribute, IDE action, or command that generates the operator family. A coding agent is an optional development aid, not a compiler feature, build dependency, runtime dependency, or correctness mechanism.

This decision supersedes the source-generation alternative retained in [typed-operator-rework.md](typed-operator-rework.md).

## Migration state at this decision

The operator rework is partially complete. This status is retained here so the scaffolding decision is not mistaken for completion of the wider migration.

| Operator role | Status | Accepted scope or remaining work |
| --- | --- | --- |
| Mutator | Reworked and accepted | Role and execution-instance arity ladders, stateless and stateful paths, `SingleCandidateMutator`, wrapping and multi topology, applicable concerns, construction paths, equality, and focused authoring and behavior tests use the accepted shape. |
| Selector | Reworked and accepted | Role and execution-instance arity ladders, stateless and stateful paths, wrapping and multi topology, applicable concerns, construction paths, equality, and focused authoring and behavior tests use the accepted shape. Selection has no single-item base because it is inherently a whole-population operation. |
| Creator | Reworked, pending acceptance | Role and execution-instance arity ladders, stateless and stateful paths, `SingleCandidateCreator`, wrapping and multi topology, applicable concerns, construction paths, equality, and focused authoring and behavior tests use the accepted shape. `PredefinedCandidatesCreator` declares its fallback child directly instead of wrapping it. |
| Crossover | Reworked, pending acceptance | Role and execution-instance arity ladders, stateless and stateful paths, `SingleCandidateCrossover`, wrapping and multi topology, applicable concerns, construction paths, equality, and focused authoring and behavior tests use the accepted shape. Crossover has no pipeline because it consumes parent groups rather than candidates produced by another crossover. |
| Evaluator | Migration outstanding | Still needs a separate migration and acceptance review. |
| Replacer | Migration outstanding | Still needs a separate migration and acceptance review. |
| Interceptor | Migration outstanding | Still needs a separate migration and acceptance review, including its additional search-state shape. |
| Terminator | Migration outstanding | Still needs a separate migration and acceptance review, including its additional search-state shape. |

Some outstanding roles already received shared improvements such as `ValueArray<T>` for structural collection equality. Those changes do not complete their authoring migration: legacy role-named factories, `Inner*` topology, forwarding aliases, authoring conveniences, and concern shapes must still be reviewed role by role.

The scaffolding mechanism is nevertheless settled for both completed and future migrations: do not build a Roslyn or deterministic code generator now; use ordinary source, optionally scaffolded and bulk-evolved by coding agents under normal review and validation.

## Scope

The decision covers two kinds of scaffolding.

### New operator roles

A new operator role may need a family of related authoring types, including:

- its configuration and execution-instance arity ladders;
- stateless and stateful authoring bases;
- role-applicable single-item authoring bases;
- wrapping and multi topology bases;
- construction companions and API usage specs.

New concrete operators within an existing role normally use the existing authoring bases and do not need this family scaffolded.

### Cross-cutting concerns

A new concern such as duration measurement, counting, or observation may need one role-specific adapter for every applicable operator role. The concern semantics are defined once in an accepted reference implementation, but each adapter remains ordinary role-specific source.

Applicability and semantics must be decided explicitly. Similar method shapes do not prove identical behavior. For example:

- duration measurement records elapsed time in `finally`, including failed calls;
- counting occurs after the child succeeds, so failed calls are not counted;
- every role can count calls, but only roles with an appropriate candidate result can count processed candidates;
- public names such as `CountMutatedCandidates`, `CountSelectedCandidates`, and `CountReplacementCandidates` express different role semantics.

## Why code generation is not being adopted

The mechanical repetition is real, especially when a concern is expanded across the operator-role matrix. The measured benefit does not currently outweigh the generator's permanent development and usage cost.

- New operator roles are expected to be uncommon.
- Concern adapters contain role-specific applicability, lifecycle, result, naming, and construction decisions in addition to mechanical forwarding.
- A production generator would require a stable generator contract, semantic discovery, diagnostics, collision handling, generated-source tests, IDE verification, packaging, external consumer tests, and ongoing compiler compatibility work.
- Generated public source would be less visible in the repository and less natural to navigate, refactor, customize, and review.
- Updating a generator could reshape a broad public API without an ordinary source diff at each generated type.
- A generator defect would be reproduced across the entire generated matrix.
- A sufficiently general concern generator would need either generator-specific implementations for every known concern or a concern description language. The latter would introduce another abstraction whose complexity is not currently justified.

Coding agents change the cost comparison. Once one role establishes the accepted semantics and structure, an agent can adapt that implementation across other roles while preserving ordinary source. Agents can also help evolve or repair the expanded code later without making generation part of the product.

## Agent-assisted scaffolding workflow

Coding agents are recommended for repetitive rollout, but their output is not assumed correct.

### Scaffolding a role family

1. Define the new role's contracts and ordinary role operation explicitly.
2. Identify the closest accepted role family, while recording structural differences such as extra type parameters, variance, operation inputs, or single-item applicability.
3. Ask the agent to create ordinary source files following the accepted authoring conventions.
4. Review every generated public type and member as a deliberate API addition.
5. Add API usage specs and focused tests for every supported authoring path.

### Rolling out a concern

1. Implement and accept the concern for one representative role.
2. State its lifecycle semantics, failure behavior, retained configuration, equality behavior, and applicability independently of that role's method names.
3. Build an explicit applicability matrix covering every operator role.
4. Ask the agent to adapt the reference implementation to each applicable role as ordinary source.
5. Review role-specific names, inputs, results, factories, fluent extensions, and exceptional cases.
6. Add shared behavioral tests where possible and role-specific tests where semantics differ.
7. Confirm that non-applicable roles were omitted deliberately rather than accidentally.

An agent should inspect the current contracts and developer guidelines rather than copy the nearest file mechanically. Some roles may still contain legacy shapes while their migration is unfinished.

## Correctness guardrails

Agent assistance reduces typing and navigation work; it does not replace engineering evidence.

- The compiler, analyzers, tests, API usage specs, formatting checks, and human review remain authoritative.
- Agent-produced code must not contain generated-file markers or warnings that discourage ordinary maintenance.
- Public operator configurations retain structural value semantics. Ordered configuration collections use `ValueArray<T>`; execution instances may use `ImmutableArray<T>` for resolved children.
- Configuration inputs are snapshotted and configurations remain unchanged during execution.
- Child configurations are resolved through the execution instance registry.
- Cross-cutting concerns need success and failure tests when lifecycle placement changes observable behavior.
- Matrix-wide changes should verify that every applicable role was updated. Repository search, shared contract tests, or explicit conformance tests may provide that deterministic coverage.
- Shared runtime helpers remain preferable when they express a complete semantic rule without obscuring role behavior or adding runtime machinery. Agent replication is not a reason to duplicate a stable calculation unnecessarily.

## Retained operator decisions

These decisions were established during the Mutator and Selector migrations and remain independent of the scaffolding mechanism.

### Public factory and child topology

- Base configurations expose one public `CreateExecutionInstance(...)` method returning the exact role execution-instance type.
- Topology bases may seal that method and expose a protected natural overload with already-resolved children.
- Configurations expose canonical `ChildOperator` or role-specific child properties publicly.
- A child slot has exactly one public name. Use wrapping or multi bases only when the children are identified by being children and nothing more.
- An operator whose child has a specific responsibility derives from the role base and declares that child directly.
- Execution instances keep resolved child machinery private or protected.

### Counting concerns

The `CountingMutator` shape is the reference. A counting concern wraps its child directly and owns a counting execution instance. It does not derive from an observable concern merely to reuse a callback.

Counting occurs after the child succeeds. Structurally identical counting configurations compare equal. Equivalent failure and equality tests accompany each migrated role.

### Terminology and construction companions

- Use `SingleCandidate*`, not `SingleSolution*`, when the operation concerns unevaluated candidates.
- The single-candidate form remains a regular batch-wise role operator; only its authoring method is scalar.
- Static `Create` factories stay in the non-generic static `<Type>` companion.
- Fluent extensions stay in an explicit `<Type>Extensions` companion.
- Instrumentation concerns may use role-first names such as `MutatorDurationExtensions` when that is the accepted convention.
- XML documentation is written for non-obvious behavior and contracts rather than repeating member names or types.

## Alternatives considered

### Build-time source generation

An incremental Roslyn generator could emit consistent nominal C# with no runtime abstraction cost and automatically synchronize generated roles. It was rejected for now because its permanent implementation, packaging, debugging, inspection, refactoring, and maintenance costs are disproportionate to the expected frequency of new roles and concerns.

### Deterministic one-shot scaffolding

A Roslyn code fix, IDE action, template, or command could write ordinary source once. This avoids hidden build output but still introduces a tool and template contract that must be developed, distributed, tested, and maintained. Coding agents provide the same immediate development assistance with better adaptation to irregular role semantics and without a new product surface.

### Manual-only authoring

Requiring contributors to reproduce every family by hand would maximize determinism but spend effort on mechanical work and encourage copy drift. Coding agents are preferred as optional accelerators while all output remains normal reviewed source.

## Evidence

Mutator and Selector established the accepted common three-parameter authoring shape. Their role ladders and topology bases contain substantial mechanical similarity, while their optional conveniences and concern implementations also exposed real semantic differences.

The concern matrix strengthens the benefit of automation: one broadly applicable concern may require adapters for all eight operator roles. Duration measurement is highly uniform, while counting demonstrates capability-sensitive behavior: Interceptor and Terminator count calls but do not expose a candidate-count convenience, whereas result-producing roles do.

The migrations also found copied rate-complement behavior that differed between choose-one roles. The stable calculation now lives in `WeightedBatchDispatch.GetRateWeights`. This demonstrates both sides of agent-assisted expansion:

- an agent can reproduce a mistaken reference across several roles;
- an agent can efficiently propagate a reviewed correction;
- deterministic behavioral tests and shared semantic helpers remain necessary in either case.

## Reconsideration

This decision is intentionally reversible, but it must be revisited explicitly rather than eroded through isolated generator experiments.

Reconsider deterministic generation only if measured future work shows that agent-assisted ordinary source has become a material maintenance burden—for example, frequent new roles or concerns, recurring matrix omissions, or repeated synchronization work whose semantics have stabilized into a small declarative model.

Any future proposal must compare its measured benefit with the existing agent-assisted workflow and preserve handwritten ordinary source as a fully supported authoring path until a separate accepted decision says otherwise.
