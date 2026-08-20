# Symbolic Regression Legacy Migration

## Purpose

Phase out the mutable symbolic-expression system incrementally while keeping every
legacy capability operational until its consumers have migrated or the capability
is explicitly removed.

The immutable `ExpressionTree` system is the target genotype. Still-required
mutable source is compiled in `HeuristicLib.Experimental` under its physical
`Legacy` folder until its consumers migrate. Existing namespaces remain stable
except where a name collision already requires `.Legacy`.

## Migration Rules

- Replace small, coherent dependency groups rather than rewriting the complete
  mutable system at once.
- Keep an operational component in the Experimental `Legacy` folder until a
  complete working replacement, including behavior relied upon by maintained
  consumers, exists. Age, architectural preference or dependence on another old
  component is not sufficient reason to remove it.
- Preserve existing namespaces unless an actual type-name collision requires a
  `.Legacy` namespace. A `Legacy` filename qualifier may distinguish two source
  files without changing their public type names.
- Migrate consumers before deleting their dependencies. Delete the mutable
  genotype only after its final consumer has migrated or been explicitly removed.
- Production code, examples, API specifications, unit tests, experimental tests,
  scenario tests, and interop projects all count as consumers.
- A legacy test may be removed only when the new test suite covers the same
  behavior or the tested behavior is intentionally removed.
- Do not replace working legacy APIs with implementations that throw at runtime.
  Keep them functional or remove them so unresolved dependencies fail at compile
  time.
- Use `[Obsolete]` only when a usable replacement exists and the diagnostic can
  identify a concrete migration path. Do not mark the mutable root types obsolete
  while doing so would only create warnings throughout still-supported legacy
  subsystems.
- Do not delete any legacy source file or test without explicit approval in chat.
  Approval is required for each proposed component or clearly enumerated batch.
- Once an approved component has no remaining value as compiled code, remove it
  instead of retaining dead source. Git history remains the reference for deleted
  implementations.

## Migration State

Each component progresses through these states:

1. **Active**: maintained in the Experimental `Legacy` folder.
2. **Retained operational**: superseded for some use cases but still compiled,
   functional, and required by at least one maintained consumer.
3. **Replacement verified**: all consumers and relevant behavioral tests use the new
   implementation.
4. **Deletion proposed**: remaining references and test parity have been reviewed.
5. **Deleted with approval**: removed only after explicit approval in chat.

Introducing a replacement never implies approval to delete the retained component.

## Initial Migration Boundary

The first replacement group consists of:

- mutable `SymbolicExpressionTree` and `SymbolicExpressionTreeNode`;
- legacy tree crossovers and mutators whose maintained behavior has an immutable
  replacement.

Only genotype-specific operators with complete immutable replacements are
eligible for consumer migration and eventual deletion.
The unrestricted PTC2 behavior now has an immutable `ProbabilisticTreeCreator`.
The unrestricted PTC2 and balanced target-length behaviors now have immutable
creators. The grammar-aware mutable `ProbabilisticTreeCreator`,
`BalancedTreeCreator` and their shared creator base remain compiled in the
Experimental `Legacy` folder because grammar-guided creation does not yet have a
complete replacement.

The first replacement group intentionally excludes:

- legacy grammars and symbols;
- legacy search spaces;
- legacy regression models and problems;
- formatters;
- Python interop, experimental workflows, and scenarios.

Those consumers continue referencing the unchanged legacy namespaces.

### Current Status

The mutable genotype and its remaining dependency closure are compiled in
`HEAL.HeuristicLib.Experimental` under the physical `Legacy` folder. Modern types
remain in `HEAL.HeuristicLib`. Public namespaces did not change as part of the
assembly move. `.Legacy` remains only where regression type names collide with
the modern API.

The concrete mutable symbolic-expression compiler and interpreter implementations
remain under `Legacy/Problems/DataAnalysis/Symbolic`. Supporting operation catalogs,
instruction models, dispatch helpers, state types, and interfaces remain active
wherever their behavior is not yet completely replaced.

`SymbolicRegressionParameterOptimization` and the `AutoDiff` package have reached
replacement-verified state for the immutable system. Measurement shows the
immutable implementation matching
their mean squared error while fitting four to eight times faster and allocating
a fifth to a tenth as much, and evaluating four to five times faster; the figures
are recorded in
[`symbolic-regression-parameter-fitting-plan.md`](symbolic-regression-parameter-fitting-plan.md).

They are nevertheless retained, because the legacy `SymbolicRegressionProblem`
calls `OptimizeParameters` from its own evaluation path. Consumers that cannot
migrate until grammar-guided immutable GP exists would lose parameter fitting
with no replacement available to them, since `NumericParameterFittingRefiner` is
typed over the immutable problem and genotype. A later budgeted quality
comparison between the two systems would also need parameter fitting enabled on
both sides.

Their deletion therefore belongs with the retirement of the legacy
`SymbolicRegressionProblem` rather than as a separate batch, and the budgeted
quality comparison between the two systems is a precondition of that batch rather
than something that follows it. Once these components are gone, no comparison
between the old and new systems can be run again. `AutoDiff` leaves
with them: its only consumer is `TreeToAutoDiffTermConverter`, which only
`SymbolicRegressionParameterOptimization` uses.

Legacy source is deleted only after reference and test-parity checks and explicit
approval. Operator tests are removed only after their behavior has been migrated
or verified as redundant.

The redundant Grow, Full, and ramped half-and-half assertions have been removed
from the mixed legacy creator test; its unmatched Balanced and probabilistic
creator coverage remains. The legacy subtree-crossover depth-limit behavior has
been migrated to the immutable crossover suite, and the superseded dedicated
legacy crossover test has been removed with approval.

## Candidate Replacement Mapping

| Legacy capability | Immutable replacement | Current direction |
| --- | --- | --- |
| Grow creation | `SymbolicExpressionCreators.GrowTreeCreator` | Replacement implemented; unused legacy implementation removed |
| Full creation | `SymbolicExpressionCreators.FullTreeCreator` | Replacement implemented; unused legacy implementation removed |
| Ramped half-and-half creation | `SymbolicExpressionCreators.RampedHalfAndHalfTreeCreator` | Replacement implemented; unused legacy implementation removed |
| Subtree crossover | `SymbolicExpressionCrossovers.SubtreeCrossover` | Replacement implemented |
| Same-arity symbol replacement | `SymbolicExpressionMutators.NodeReplacementMutator` | Replacement implemented |
| Branch replacement | `SymbolicExpressionMutators.SubtreeMutator` | Replacement implemented |
| Minimal-tree branch removal | `SymbolicExpressionMutators.ShrinkSubtreeMutator` | Not a complete replacement; `RemoveBranchManipulation` and its required base remain active |
| One-point parameter shaking | `LocalPerturbationMutator(One)` | Replacement implemented |
| Full-tree parameter shaking | `LocalPerturbationMutator(All)` | Replacement implemented |
| Random mutator composition | Generic `ChooseOneMutator` | Replacement implemented through composition; unused legacy wrapper removed |
| Bounded prediction | `BoundedRegressor` wrapping a `SymbolicRegressor` | General immutable regressor decorator implemented |
| Balanced creation | `SymbolicExpressionCreators.BalancedTreeCreator` | Unrestricted replacement implemented; grammar-aware legacy implementation remains |
| Probabilistic PTC2 creation | `SymbolicExpressionCreators.ProbabilisticTreeCreator` | Unrestricted replacement implemented; grammar-aware legacy implementation remains |

Implemented replacements do not authorize deletion. Actual consumers and
behavioral test parity must be checked first.

## Approved Deletions

The following components were removed after reference and test-parity checks and
explicit approval:

- `MultiSymbolicExpressionTreeManipulator`, replaced by `ChooseOneMutator`;
- the unused mutable-tree Grow, Full, and ramped half-and-half creators;
- `SymbolicExpressionTreeLinearCompiler` and `LinearInstruction`, which referenced
  only each other and had no consumers or tests;
- `EmptySymbolicExpressionTreeGrammar`, which had no consumers or behavior;
- `BoundedSymbolicRegressionModel`, replaced by `BoundedRegressor` composed
  with `SymbolicRegressor` for the immutable genotype;
- `CachedRegressionSolution`, which had no consumers or tests.
- `RegressionCsvInstanceProvider` and `TableFileParser`, which had no consumers
  or tests after the Python migration.

The legacy creator base remains because the grammar-aware `BalancedTreeCreator`
and `ProbabilisticTreeCreator` still depend on it.

## Consumer Migration

Consumers migrate only when their complete symbolic-expression requirements are
covered by the immutable system. A fully connected or default legacy grammar is
treated as an unrestricted search space and should become an
`ExpressionTreeSearchSpace`. Consumers that install meaningful productions,
special roots, linear scaling, or other grammar constraints remain on the
legacy stack until grammar-guided immutable GP exists.

`SlidingWindowSymbolicRegressionProblem` now uses `ExpressionTree`,
`ExpressionTreeSearchSpace`, `RegressionData`, and `IRegressionMetric`. Its
bound training data already defines the eligible rows, and each dynamic window
materializes the selected rows for evaluation.

The Python genealogy, extended callback, and inter-objective workflows now use
the immutable expression tree, unrestricted search space, modern metrics, and
fitness-time linear scaling. Their parameter-optimization controls remain on
the public interop API and report that the capability is not implemented when
enabled. The legacy automatic-differentiation and parameter-optimization tests
remain on the mutable system until their maintained capability has a
replacement.

The migrated default workflows retain the former add, subtract, multiply,
divide, square-root, and logarithm symbol set; the `[-20, 20]` constant
initialization; the former additive and multiplicative constant perturbations;
the `0.9` internal crossover-point probability; and both one-point and all-point
local perturbation choices. The immutable variable node does not carry the
legacy occurrence-local numeric weight, so that behavior cannot be preserved
without a separate weighted-variable design. The old program, start, and
linear-scaling wrapper nodes also no longer count against expression length.

The evaluator-based regression objective layer remains under the Experimental
`Legacy/Problems/DataAnalysis/Regression` folder and uses the
`Problems.DataAnalysis.Regression.Legacy` namespace where names collide. Modern
symbolic regression uses `IRegressionMetric` and `IExpressionMetric` collections
on `SymbolicRegressionProblem`. Explicit-grammar tests and the unreplaced
automatic-differentiation parameter optimizer continue to import the legacy
evaluators until those complete capabilities migrate.

The complete still-operational `Dataset`-based support group remains under the
Experimental `Legacy/Problems/DataAnalysis` folder. This includes the old data
containers, problem-data base classes and regression model contracts used by
parameter optimization and its tests.

The maintained data-analysis layer now provides numeric perturbation feature
importance for modern predictors and supervised data. The superseded legacy
`RegressionVariableImpactsCalculator` and its tests were removed with explicit
approval. Categorical feature perturbation is outside the current scope.

## Planned Order

1. Inventory mutable-genotype operator tests and compare each behavior with the immutable
   operator tests.
2. Migrate complete consumers that only require the implemented unrestricted
   symbolic-expression feature set.
3. Propose small deletion batches after reference and test-parity checks.
4. Migrate compilation, evaluation, regression, formatting, and grammar features
   as separate later stages.
5. Delete the mutable genotype last.
