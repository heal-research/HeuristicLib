# Symbolic Regression Foundation Decisions

Why the symbolic-regression foundation is built the way it is: decisions that could
otherwise be reopened, and the measurements that settled them.

Usage documentation is in [`docs/guide/domains/symbolic-regression.md`](../docs/guide/domains/symbolic-regression.md)
and [`docs/guide/domains/symbolic-expressions.md`](../docs/guide/domains/symbolic-expressions.md). Planned
extensions are in [symbolic-regression-extension-roadmap.md](symbolic-regression-extension-roadmap.md).
Symbol, constant, and perturbation semantics are documented for users in
[`docs/guide/domains/symbolic-regression.md`](../docs/guide/domains/symbolic-regression.md#symbols-and-numeric-terminals).

Measurements are single-machine and establish direction and order of magnitude, not
statistical precision.

## Genotype Representation

**`ExpressionTree` is an immutable persistent hierarchy; compact opcode RPN lives
after the genotype boundary as `CompiledExpression`.**

Four immutable prototypes were benchmarked: contiguous struct RPN, contiguous class
RPN, persistent hierarchy, and chunked RPN (a rope).

| Workload                           | Struct RPN | Class RPN |     Hierarchy | Chunked RPN |
| ---------------------------------- | ---------: | --------: | ------------: | ----------: |
| Evolution time                     |   81.70 ms |  74.99 ms |  **13.22 ms** |   115.40 ms |
| Evolution allocation               |   85.89 MB |  73.77 MB |  **17.55 MB** |   109.87 MB |
| Full GP pipeline                   |   25.57 ms |  25.11 ms |  **20.48 ms** |    33.54 ms |
| GC collections over 25 generations |  8 / 5 / 1 | 6 / 0 / 0 | **0 / 0 / 0** |  10 / 6 / 4 |

- **Structural sharing is the whole story.** A hierarchical edit copies the changed
  node and its ancestor path; a flat immutable RPN edit copies most of a contiguous
  array. End-to-end crossover measured ~71 ns against ~2 183 ns. GP does nothing but
  these small immutable edits.
- **The predicted weaknesses did not materialize.** Compilation from hierarchy tied
  with both flat layouts (2.371 vs 2.323 and 2.373 ms on aged populations), and aged
  populations showed no locality collapse.
- **Chunked RPN loses on both counts** — rope balancing and fragmentation outweigh the
  sharing benefit at GP tree sizes. Do not revive it.
- **Retained size alone is a misleading metric.** The hierarchy starts larger per
  object and ends compact through sharing; allocation churn and collection pressure
  are what matter.

Consequences: nodes are specialized by terminal payload and arity (unary/binary hold
direct references, n-ary an array); each node caches length, depth, and hash; macros
stay one node and expand only during compilation; compiled artifacts take no part in
equality, hashing, containment, mutation, or crossover.

## Architectural Boundaries

Invariants. Breaking one is a design change.

- The genotype owns structure only. Genotype storage follows the semantic tree,
  compiled storage follows the execution engine, and opcode layout is never exposed
  through the tree API.
- Ordinary interpretation takes an `ExpressionTree`; explicit compilation is an
  advanced opt-in for repeated execution.
- The built-in opcode set is closed, so interpretation, validation, and operators can
  use compile-time-known opcodes and fast switches. A custom-symbol system, if ever
  justified, is a separate layer that must not slow this path.
- The interpreter owns name-to-`DataFrame`-series resolution. No separate public
  data-view type.
- Search spaces and operators produce and validate candidates; they never evaluate.
  Problems evaluate; they never transform. Refiners are the only place a candidate
  changes.
- Operators never mutate parents, and evaluation never mutates or replaces candidates.
- One problem instance has exactly one search space, chosen at construction.
- The unrestricted search space is not a grammar; its operators must not call grammar
  predicates or enumerate grammar-derived cut points.
- Bounds, penalties, and objectives stay outside interpretation.
- Interceptors run after an algorithm step, too late to affect offspring fitness or
  replacement.
- Determinism: same candidate, data, configuration, and random source gives the same
  result.
- Evaluation that must count toward budgets, termination, analysis, or instrumentation
  goes through an evaluator operator.

## Settled Decisions

- **Genotype.** Immutable `ExpressionTree`, `OpCode : ushort`, binary operation arity,
  identical-instruction equality, `NaN` for invalid numeric results.
- **Binding.** Name-first authoring; compiled variable instructions carry payload
  indexes into an interned table; interpreter memory is never shared mutably. Operon
  is the postfix-encoding reference.
- **Constants.** One `Constant` opcode plus a `double` side table. `FixedConstant` and
  `Constant` differ only in the tree.
- **Containment.** `ExpressionTreeSearchSpace` uses **aggregate-coverage matching**:
  the configured symbols collectively cover a node's origin symbol, and no single one
  need be equivalent to it. Variable symbols `{x}` and `{y}` together cover an origin
  allowing `{x, y}`, and `{x, y}` covers an origin allowing only `{x}`. Selection
  weights, initial distributions, and perturbation policies are proposal guidance and
  play no part. Two rejected alternatives: _structural matching_ (an equal origin
  symbol must be configured) is inflexible enough that changing guidance can
  invalidate a valid expression; _admissibility-equivalence matching_ needs a second
  equivalence relation beside symbol equality and does not reconcile with
  origin-authoritative perturbation. Aggregate coverage suits unrestricted scalar GP
  because every scalar subtree composes and symbol partitioning carries no meaning.
  **A grammar search space probably cannot reuse it** — production and nonterminal
  identity are semantically significant, and aggregate coverage would admit edits no
  individual production permits. That remains provisional until the grammar model is
  designed. Consequence: two contained nodes with different origins may explore the
  same search space differently, and the coexistence of hard admissibility with
  proposal guidance in one symbol model is a possible future separation point.
- **Problems.** `IProblem`/`Problem` are batch native; `SingleSolutionProblem` is the
  scalar authoring base and owns the scalar-to-batch adapter.
- **Objectives.** Prediction metrics computed once from one shared prediction vector,
  ordered before expression metrics. Both collections empty is rejected. Lexicographic
  total order by default; individual directions govern dominance.
- **Linear scaling.** Opt-in fitness-time prediction transformation, never a genotype
  node. `LinearlyScaledRegressor` retains fitted coefficients for later prediction.
- **Evaluation and refinement.** Evaluators return objective vectors and never replace
  candidates. `Refiner` (`Candidate → Candidate`) is the single refinement mechanism;
  `ImprovementCheckingRefiner` adds retention as an ordinary composable refiner.
  Repeated refinement uses iterated or pipeline topologies, not two lifecycle points.
- **Parameter fitting is a refiner, not evaluator configuration.** A successful fit
  returns a replacement tree built through one `ReplaceMany` call. Non-finite results
  propagate without an implicit safeguard.
- **Thread safety.** No shared mutable interpreter memory.
- **Buffers.** Scratch buffers and column caches are interpreter internals scoped to
  one evaluation call or execution instance.

## Measurements

### Versus the legacy implementation

Both sides call the same MathNet Levenberg-Marquardt, so the solver is held constant.

- **Parameter fitting: 4x to 8x faster at equal parameter counts, a fifth to a tenth
  the allocation, same optimum.** MSE agrees to four significant figures across three
  expressions, including one that cannot fit its target and where both settle on the
  same least-squares line. Part of the legacy gap is its own pre/post acceptance
  evaluation, work the immutable path does not do because acceptance belongs to
  `ImprovementCheckingRefiner`.
- **Interpretation: about 4.5x to 5x faster** than the better of the two legacy
  interpreters, with identical predictions. The legacy batched interpreter is not
  faster than its scalar one and allocates ~50x as much.
- **Compilation costs about 6% of the interpretation it enables** at 5 000 rows, so
  compiling once per candidate per generation needs no engineering around.
- **Compiler optimization changes nothing measurable on GP-shaped expressions** —
  constant folding has nothing to fold where constants sit at leaves multiplied by
  variables. It costs nothing either, so it stays on by default.

Comparing against legacy also exposed a real defect in
`SymbolicDataAnalysisExpressionTreeBatchInterpreter`: it compiled a constant into a
`BatchSize` buffer but indexed it by absolute dataset row, throwing on any dataset
longer than one batch containing a constant. Its only test used eleven rows, where the
batched loop never runs. Fixed and covered on both sides of the boundary.

### Where run time goes

- **Refinement dominates any run that enables it**: 96 to 99 percent of wall clock at
  every size from 200 to 20 000 rows, multiplying total run time by 45 to 120 times.
  Every other role including evaluation falls below two percent. User-facing guidance
  is in [`docs/guide/extending/operator-composition.md`](../docs/guide/extending/operator-composition.md#refiner-composition).
- **In unrefined runs, evaluation dominates at scale** — 95.9 percent at 20 000 rows —
  but is genuinely cheap at small row counts because the interpreter is vectorized
  through `TensorPrimitives`. The intuition that evaluation does far more work than
  crossover is right in operation count and wrong in time.
- **Selection is independent of tree size** (5.75 to 5.88 µs per batch across an
  eightfold size range); it only compares objective vectors.

### Allocation fixes

Two hotspots were removed without architectural change and without altering a single
result:

|                                       |          Before |           After |
| ------------------------------------- | --------------: | --------------: |
| Unrefined run, 20 000 rows            |          100 ms |           45 ms |
| Unrefined run allocation, 20 000 rows |          375 MB |          6.7 MB |
| Full GC collections, 20 000 rows      |              97 |               0 |
| One crossover, 5.2-node parents       | 0.222 µs, 690 B | 0.106 µs, 150 B |

`SymbolicRegressionProblem.Evaluate` now rents its prediction buffer from
`ArrayPool<double>.Shared` instead of allocating per candidate — a field is not
available because a batch evaluates under a configurable `Concurrency`.
`SubtreeCrossover` walks nodes directly instead of allocating an `ExpressionPoint` per
node of the second parent. Run allocation is now independent of row count.

Donor selection deliberately **no longer preserves the random draw sequence**: count
eligible donors, draw one index, walk to it. Draw-order stability was never promised.

## Solver Backend: MathNet Retained

Reopening this needs a specific quantified problem that neither adapter changes nor
solver configuration can address.

- The legacy implementation calls the same MathNet solver, so old and new differ in
  differentiation and marshalling only.
- Dropping it would not drop the dependency: `HyperVolumeCalculator` uses MathNet
  statistics and the legacy optimizer its linear algebra. The dependency actually up
  for elimination is `AutoDiff`.
- **Per-fit allocation is the solver's, not ours**: 48 MB at 20 000 rows and five
  parameters, of which the adapter's `(3 + parameterCount) * rowCount` doubles is
  roughly 1.3 MB — under three percent. Quantified, not addressable by adapter
  changes, and still not enough to justify writing our own damping, trust-region, and
  convergence-test edge cases for a dependency that stays either way.

## Tried And Reverted

- **Caching the duplicated forward sweep.** MathNet requests model values and the
  Jacobian through separate callbacks at the same parameter point, so the forward
  sweep runs twice per iteration (confirmed: six model and six Jacobian callbacks at
  matching points). Serving both from one cached evaluation produced no measurable
  gain at 500 or 20 000 rows — 5.86 / 8.27 / 35.3 ms per fit against 6.07 / 8.24 /
  32.9 without it, inside variance and pointing both ways. Reverted.
- **A thread-retained prediction buffer** instead of `ArrayPool.Shared`. Renting costs
  ~5 ns per candidate regardless of row count, under a tenth of a percent of the
  evaluation it serves; a retained buffer acquires in 0.44 ns and holds its memory for
  the life of the thread. Not worth it.

Two wider rejected approaches — typed operator invocation and generated operator
families — are recorded in
[`developer-backlog.md`](developer-backlog.md#discussed-tried-and-rejected).

## Do Not Add

Abstractions considered during the symbol/node design and deliberately left out. Each
needs new evidence, not a fresh opinion.

- **`IPerturbableSymbol`.** Local perturbation is a common `Symbol` capability
  (`SupportsLocalPerturbation`, `CanPerturb`, `TryPerturb`), not an opt-in interface.
  An interface would still force type dispatch in generic mutators without a better
  responsibility or performance boundary.
- **A generic runtime `SymbolPerturbation` request family.** Each symbol owns the
  configuration and representation of its own perturbation policy; a mutator selects
  nodes and must not inject a competing policy.
- **A generic macro-symbol or lowering-recipe abstraction.** `SigmoidSymbol` owns its
  own multi-opcode lowering directly. Wait until several macros demonstrate genuinely
  common behavior. Custom macros already work by overriding `Symbol.Emit` against the
  public `IExpressionEmitter`.
- **Hard value bounds on numeric symbols.** Initial distributions and perturbations
  are sampling guidance, never domains. Bounded and constrained optimization is a
  separate design.
- **Editing APIs on `CompiledExpression`.** It is a read-only derived artifact with no
  instruction-location or mutation surface. Efficient parameter updates need a
  dedicated parameterization design, not mutable compiled instructions.

## Known Follow-Up

- JSON and binary expression serialization.
- A public-API review deciding how much of the internal automatic-differentiation and
  Levenberg-Marquardt surface to expose, rather than publishing internals by default.
- N-ary symbolic operations instead of binary-only arity.
- A revised invalid-value policy if `NaN` proves insufficient.
- Cancellation cannot reach operators: `NumericParameterFitter` takes a token,
  `NumericParameterFittingRefiner` has none to give and passes `default`.
- No problem-dependent validation phase, so a search space allowing operations the
  differentiable compiler cannot lower fails per candidate at runtime rather than
  before the run starts.

The last two are tracked with their design context in
[`developer-backlog.md`](developer-backlog.md).

## Legacy Status

The mutable symbolic-expression system still ships in `HEAL.HeuristicLib.Experimental`
under its physical `Legacy` folder. Namespaces are unchanged except where a collision
forces `.Legacy`.

### Replacement Mapping

| Legacy capability                                      | Replacement                                                          | State                                                                                                     |
| ------------------------------------------------------ | -------------------------------------------------------------------- | --------------------------------------------------------------------------------------------------------- |
| Grow / Full / ramped half-and-half creation            | `GrowTreeCreator`, `FullTreeCreator`, `RampedHalfAndHalfTreeCreator` | Legacy removed                                                                                            |
| Balanced creation                                      | `BalancedTreeCreator`                                                | Unrestricted done; grammar-aware legacy remains                                                           |
| Probabilistic PTC2 creation                            | `ProbabilisticTreeCreator`                                           | Unrestricted done; grammar-aware legacy remains                                                           |
| `SubtreeCrossover`                                     | `SubtreeCrossover`                                                   | Done                                                                                                      |
| `ChangeNodeTypeManipulation`                           | `NodeReplacementMutator`                                             | Done                                                                                                      |
| `ReplaceBranchManipulation`                            | `SubtreeMutator`                                                     | Done                                                                                                      |
| `RemoveBranchManipulation`                             | `ShrinkSubtreeMutator`                                               | Not equivalent; legacy remains                                                                            |
| `OnePointShaker` / `FullTreeShaker`                    | `LocalPerturbationMutator(One)` / `(All)`                            | Done                                                                                                      |
| `MultiSymbolicExpressionTreeManipulator`               | `ChooseOneMutator` composition                                       | Legacy removed                                                                                            |
| `BoundedSymbolicRegressionModel`                       | `BoundedRegressor` over `SymbolicRegressor`                          | Legacy removed                                                                                            |
| Regression evaluators                                  | `IRegressionMetric` + `IExpressionMetric`                            | Legacy layer remains for grammar consumers                                                                |
| `SymbolicRegressionParameterOptimization` + `AutoDiff` | `NumericParameterFitter` / `NumericParameterFittingRefiner`          | Verified faster and equal-quality; retained, see below                                                    |
| `RegressionVariableImpactsCalculator`                  | `FeatureImportance` (numeric perturbation only)                      | Legacy removed; categorical out of scope                                                                  |
| `FactorVariable` / `BinaryFactorVariable`              | None                                                                 | Not started, [EXT-2](symbolic-regression-extension-roadmap.md#ext-2-factor-variables-and-typed-terminals) |

Built-in operation symbols (arithmetic, `Log`, `Sqrt`, trigonometric, `Abs`,
`Square`, `Cube`, `CubeRoot`, `Power`, `Root`, `AnalyticQuotient`) all map 1:1 to
opcodes and are covered by primitive-operation tests. Legacy n-ary arithmetic is
represented as left-associative binary expressions.

Deferred and optional, with no legacy equivalent: hoist mutation, size-fair and
homologous crossover.

### What Still Ships And Why

One dependency closure remains, all of it blocked on grammar-guided immutable GP
([EXT-1](symbolic-regression-extension-roadmap.md#ext-1-grammar-constrained-symbolic-regression)):
legacy grammars, symbols and search spaces; the grammar-aware mutable creators and
their shared base; `RemoveBranchManipulation`; the legacy `SymbolicRegressionProblem`
and its evaluator layer; and the mutable genotype, compiler and interpreter they
need.

`SymbolicRegressionParameterOptimization` and `AutoDiff` are replacement-verified but
retained, because the legacy problem calls `OptimizeParameters` from its own
evaluation path and `NumericParameterFittingRefiner` is typed over the immutable
problem. They leave as one batch with that problem; `AutoDiff` follows, its only
consumer being `TreeToAutoDiffTermConverter`.

The `Dataset`-based data-analysis support group remains for the same consumers; see
[data-analysis-modernization-plan.md](data-analysis-modernization-plan.md).

### Retirement Rules

These bind every branch that retires legacy code.

- **Deleting any legacy source file or test needs explicit approval in chat**, per
  component or per clearly enumerated batch. Implementing a replacement never implies
  approval.
- Migrate consumers before deleting their dependencies. Production code, examples,
  API specs, all test projects, and interop count as consumers.
- Keep a component until a replacement covers the behavior maintained consumers rely
  on. Age or architectural preference is not sufficient reason.
- Never leave a working API replaced by one that throws. Keep it functional or remove
  it so broken dependencies fail at compile time.
- Remove a legacy test only when new tests cover the same behavior, or the behavior is
  intentionally gone.
- Use `[Obsolete]` only where a usable replacement exists and the diagnostic can name
  the migration path.
- Once approved and unused, delete rather than retain dead source. Git history is the
  record.

### Consumer Migration

A fully connected or default legacy grammar is an unrestricted search space and should
become an `ExpressionTreeSearchSpace`. Consumers installing meaningful productions,
special roots, or grammar-level linear scaling stay on the legacy stack until EXT-1.

Migrated: `SlidingWindowSymbolicRegressionProblem`, and the Python genealogy, extended
callback and inter-objective workflows.

### Permanent Behavior Differences

Deliberate divergences from the mutable system that will not be restored:

- The immutable variable node carries no occurrence-local numeric weight. Preserving
  that needs a separate weighted-variable design.
- Program, start, and linear-scaling wrapper nodes no longer exist and no longer count
  against expression length.
- Linear scaling is a fitness-time prediction transformation, not a genotype node.
- Fixed-versus-evolvable constant identity lives in the genotype and is absent from
  compiled evaluation.

### Known Gaps

- Legacy automatic-differentiation and parameter-optimization tests remain because the
  retained mutable symbolic-regression problem still invokes the legacy optimization
  path. They leave with that problem after grammar-guided immutable GP replaces its
  remaining consumers under EXT-1.
