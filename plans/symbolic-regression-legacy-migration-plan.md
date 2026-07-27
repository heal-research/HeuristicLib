# Symbolic Regression Legacy Migration

## Purpose

Phase out the mutable symbolic-expression system incrementally while keeping every
legacy capability operational until its consumers have migrated or the capability
is explicitly removed.

The immutable `ExpressionTree` system is the target genotype. Still-required
mutable source remains compiled inside `HeuristicLib` in its established domain
folders until its consumers migrate. Migration state is tracked in this plan and
the parity matrix rather than through a physical legacy source directory.

## Migration Rules

- Replace small, coherent dependency groups rather than rewriting the complete
  mutable system at once.
- Keep a component in its established domain folder until a complete working
  replacement, including behavior relied upon by maintained consumers, exists.
  Age, architectural preference, or dependence on another old component is not
  sufficient reason to remove it.
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

1. **Active**: maintained in its existing location.
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
`BalancedTreeCreator`, and their shared creator base remain compiled in their
operator folders because grammar-guided creation does not yet have a complete
replacement.

The first replacement group intentionally excludes:

- legacy grammars and symbols;
- legacy search spaces;
- legacy regression models and problems;
- formatters;
- Python interop, experimental workflows, and scenarios.

Those consumers continue referencing the unchanged legacy namespaces.

### Current Status

The mutable genotype and its remaining dependent components remain compiled and
operational in their established genotype, operator, and data-analysis folders.
Modern types use distinct namespaces; `.Legacy` is retained only for regression
types whose names collide with the modern API.

The concrete mutable symbolic-expression compiler and interpreter implementations
remain under `Problems/DataAnalysis/Symbolic`. Supporting operation catalogs,
instruction models, dispatch helpers, state types, and interfaces remain active
wherever their behavior is not yet completely replaced. Parameter optimization
and AutoDiff conversion also remain active until working replacements exist.

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

The Python genealogy, extended callback, and inter-objective workflows remain
legacy consumers because they explicitly install a linear-scaling root. The
legacy automatic-differentiation and parameter-optimization tests also remain
on the mutable system until their maintained capability has a replacement.

The evaluator-based regression objective layer remains under
`Problems/DataAnalysis/Regression` and uses the
`Problems.DataAnalysis.Regression.Legacy` namespace where names collide. Modern
symbolic regression uses `IRegressionMetric` and `IExpressionMetric` collections
on `SymbolicRegressionProblem`. Explicit-grammar workflows and the unreplaced
automatic-differentiation parameter optimizer continue to import the legacy
evaluators until those complete capabilities migrate.

The complete still-operational `Dataset`-based support group remains under
`Problems/DataAnalysis`. This includes the old data containers, problem-data
base classes, regression model contracts, and CSV loader. Python workflows,
parameter optimization, and their tests remain valid consumers.

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
