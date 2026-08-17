# Symbolic Regression Reference Behavior Matrix

This matrix tracks Stage 1 reference behavior while the mutable tree API is phased out. HeuristicLab and the current mutable tree implementation are behavioral references only; new code should use HeuristicLib-native fixtures and terminology. Do not add a legacy-to-new converter. Rows are closed by reimplementing the relevant behavior as native tests that can remain after the old system is removed.

The operational source-migration process, lifecycle states, test-retention rules,
and mandatory deletion approvals are defined in
[`symbolic-regression-legacy-migration-plan.md`](symbolic-regression-legacy-migration-plan.md).

The maintained immutable vertical slice now uses the modern data-analysis
foundation described in
[`data-analysis-modernization-plan.md`](data-analysis-modernization-plan.md):

- `DataFrame` and `Series<T>` are general immutable columnar inputs;
- `RegressionData` pairs an input frame with a target series;
- `SymbolicRegressor` adapts an immutable expression tree to `IRegressor` and
  retains its name-based compiled expression;
- `BoundedRegressor` decorates any `IRegressor` with output bounds;
- `LinearlyScaledRegressor` decorates any `IRegressor` with fitted affine output
  scaling;
- `SymbolicRegressionProblem` binds training data plus ordered prediction and
  expression metrics for algorithm evaluation and can apply linear scaling
  transiently without changing the expression genotype;
- the legacy mutable problem remains in the `.Legacy` namespace until its
  remaining consumers migrate.

| Behavior | HeuristicLab reference | Current legacy type | New target | Reference level | Test status | Intentional differences |
| --- | --- | --- | --- | --- | --- | --- |
| Variable | `Variable` symbol | `Variable` / `VariableTreeNode` | `Variable` opcode with payload index into the compiled variable-reference table | Exact for name-bound scalar evaluation | Covered by primitive operation tests | Stage 1 draft authoring is name-first; compiled instructions use indexes only as compact table references. |
| Number | `Number` symbol | `Number` / `NumberTreeNode` | `Constant` opcode plus `double` side table | Exact value evaluation | Covered by primitive operation tests | Fixed versus evolvable identity belongs to the genotype and is not represented in compiled evaluation. |
| Add | `Addition` symbol | `Addition` | Binary `Add` opcode | Exact for binary and deterministic n-ary lowering | Covered by primitive operation tests | Legacy n-ary add is represented through explicit left-associative binary expressions. |
| Subtract | `Subtraction` symbol | `Subtraction` | Binary `Subtract` opcode | Exact for binary and deterministic n-ary lowering | Covered by primitive operation tests | Legacy n-ary subtract is represented through explicit left-associative binary expressions. |
| Multiply | `Multiplication` symbol | `Multiplication` | Binary `Multiply` opcode | Exact for binary and deterministic n-ary lowering | Covered by primitive operation tests | Legacy n-ary multiply is represented through explicit left-associative binary expressions. |
| Divide | `Division` symbol | `Division` | Binary `Divide` opcode | Ordinary `double` behavior | Covered by primitive operation tests | Invalid numeric results produce normal `double` results such as `NaN` or infinity; bounds and penalties remain outside the interpreter. |
| Log | `Logarithm` symbol | `Logarithm` | Unary `Log` opcode | Ordinary `double` behavior | Covered by primitive operation tests | Invalid numeric results produce `NaN`. |
| Sqrt | `SquareRoot` symbol | `SquareRoot` | Unary `Sqrt` opcode | Ordinary `double` behavior | Covered by primitive operation tests | Invalid numeric results produce `NaN`. |
| Trigonometric functions | `Sine`, `Cosine`, `Tangent`, and `HyperbolicTangent` symbols | Same symbols | Unary `Sin`, `Cos`, `Tan`, and `Tanh` opcodes | Ordinary `double` behavior | Covered by primitive operation and compiler tests | No protected variants are introduced in the interpreter. |
| Absolute and integer powers | `Absolute`, `Square`, and `Cube` symbols | Same symbols | Unary `Abs`, `Square`, and `Cube` opcodes | Ordinary `double` behavior | Covered by primitive operation and compiler tests | Square and cube are explicit operations rather than power aliases in the genotype. |
| Cube root | `CubeRoot` symbol | `CubeRoot` | Unary `CubeRoot` opcode | Ordinary `double` behavior | Covered by primitive operation and compiler tests | Uses signed `Math.Cbrt` semantics. |
| Power and root | `Power` and `Root` symbols | Same symbols | Binary `Power` and `Root` opcodes | `pow(x, y)` and `pow(x, 1 / y)` | Covered by primitive operation and compiler tests | Exponents and root degrees are not rounded. |
| Analytic quotient | `AnalyticQuotient` symbol | `AnalyticQuotient` | Binary `AnalyticQuotient` opcode | `x / sqrt(1 + y²)` | Covered by primitive operation and compiler tests | Implemented as an ordinary numeric operation. |
| Factor variables | `FactorVariable` and `BinaryFactorVariable` | Same symbols | Deferred typed terminal design | Intermediate-term migration | Not started | Requires a deliberate categorical-data and terminal-payload design. |
| Linear scaling | Regression model scaling | `PredictAndAdjustScaling` / grammar linear scaling | Fitness-time least-squares scaling plus `LinearlyScaledRegressor` for the fitted predictor | Equivalent scaled predictions without genotype mutation | Implemented first draft | Scaling is opt-in on `SymbolicRegressionProblem`, is fitted once per candidate evaluation, and does not affect expression equality, length, depth, containment, mutation, or crossover. Final predictor construction fits and retains the coefficients separately. |

## Operator Parity

The new immutable expression-tree operator family targets behavioral coverage rather than one-to-one preservation of legacy type names. Operators that are compositions or parameterizations of the new generic operators should not be reintroduced as separate compatibility types.

| Behavior | Legacy operator | New operator or decision | Status | Notes |
| --- | --- | --- | --- | --- |
| Grow initialization | `GrowTreeCreator` | `GrowTreeCreator` | Implemented; legacy removed | Samples any structurally viable symbol within the remaining length and depth budgets. |
| Full initialization | `FullTreeCreator` | `FullTreeCreator` | Implemented; legacy removed | Produces trees whose leaves occur at one selected depth. |
| Ramped half-and-half initialization | Ramped full/grow population initialization | `RampedHalfAndHalfTreeCreator` | Implemented; legacy removed | Distributes creation across an inclusive depth range and alternates full and grow creation. |
| Balanced target-length initialization | `BalancedTreeCreator` | `BalancedTreeCreator` | Implemented for unrestricted GP | Expands randomly selected positions within each shallowest level before proceeding deeper. `Irregularity` controls whether weighted terminal selection may occur before the requested length is reached. The creator preserves hard search-space limits and shares the internal construction builder with PTC2. The grammar-aware mutable variant remains beside the old grammar system until that system is redesigned. |
| Probabilistic target-size initialization | `ProbabilisticTreeCreator` / PTC2 | `ProbabilisticTreeCreator` | Implemented for unrestricted GP | Implements Luke's random-frontier PTC2 algorithm over fixed-arity symbols. A requested-length distribution is optional; the default is uniform over the search-space length range. Arity may make the result slightly larger or smaller than the request, but hard search-space length and depth limits are always preserved. The grammar-aware mutable variant remains available until grammar-guided GP is redesigned. |
| Subtree crossover | `SubtreeCrossover` | `SubtreeCrossover` | Implemented | Selects a destination and a valid donor without bounded retry loops. |
| Same-arity node replacement | `ChangeNodeTypeManipulation` | `NodeReplacementMutator` | Implemented | Replaces a symbol while retaining the existing children when arity is unchanged. |
| Random subtree replacement | `ReplaceBranchManipulation` | `SubtreeMutator` | Implemented | Generates one structurally viable replacement subtree within the exact remaining budgets. |
| One-point local-parameter mutation | `OnePointShaker` | `LocalPerturbationMutator(One)` | Implemented | Perturbation behavior belongs to the originating symbol. |
| All-point local-parameter mutation | `FullTreeShaker` | `LocalPerturbationMutator(All)` | Implemented | Also supports independent per-point selection through `Each(probability)`. |
| Random choice among mutators | `MultiSymbolicExpressionTreeManipulator` | `ChooseOneMutator` | Implemented through generic composition; legacy removed | Symbolic expressions do not require a dedicated multi-mutator type. |
| Strict structural shrinking | `RemoveBranchManipulation` | `ShrinkSubtreeMutator` | Implemented | Replaces a selected operation occurrence with a terminal sampled from the search space, guaranteeing a strict length reduction without retries. |
| Hoist mutation | No direct legacy equivalent | Deferred | Optional | Replaces a selected subtree with one of its descendants. Consider only after measured need; shrink mutation already provides a simple anti-bloat structural operator. |
| Size-fair or homologous crossover | No direct legacy equivalent | Deferred | Optional | These are alternative crossover policies, not prerequisites for numeric-parameter optimization. |
| Evolvable-constant optimization | Levenberg-Marquardt numeric parameter fitting | Reusable differentiation, numerical optimization, and refinement composition | Direct parameter fitter implemented; refiner and algorithm integration implemented | The implementation and remaining integration stages are specified in `symbolic-regression-parameter-fitting-plan.md`. |

## Stage 0 Design-Hole Outcomes

| Topic | Outcome or owner |
| --- | --- |
| API specs | Initial executable specs live in `test/HeuristicLib.Tests.ApiUsageSpecs/Regression/SymbolicRegressionRedesignSpecs.cs`; future implementation should replace commented sketches with real API calls as each stage lands, then promote behavior-heavy checks into regular unit tests in the owning project. |
| Problem metric API | Settled and implemented: the simple problem constructor defaults to `Metrics.MSE`; an explicit single prediction metric remains concise; advanced construction accepts ordered prediction and expression metric collections. Prediction values are calculated once for all prediction metrics, prediction objectives precede expression objectives, and zero total metrics is rejected. Multiple objectives use lexicographic total order by default while their declared directions govern dominance. |
| Legacy regression evaluators | Superseded for the immutable problem by `IRegressionMetric` and `IExpressionMetric`. The old evaluator interfaces, implementations, evaluator-based regression problem, and evaluator-specific data extensions compile under `Problems.DataAnalysis.Regression.Legacy` for remaining explicit-grammar and parameter-optimization consumers. |
| Search-space API | Current first draft: `ExpressionTreeSearchSpace` represents the unrestricted search space directly. No unrestricted factory or subtype is part of the first API. |
| Legacy migration | Incremental replacement: still-required mutable components stay in their domain folders and are removed only after replacement coverage and explicit approval. `.Legacy` namespaces are used only where old and new public type names would otherwise collide. |
| Interpreter binding | Settled: `ExpressionDraft.Variable(name)` interns names into the compiled expression variable table; variable instructions store payload indexes into that table, and the interpreter resolves the referenced names once against the supplied dataset. |
| Reference behavior scope | Seeded by the matrix above; add rows before extending Stage 1 behavior coverage. |
| Instruction validity | Stage 1 owner: `ExpressionTree` factories and ownership-transfer factories validate non-empty RPN, stack balance, arity, subtree length, payload indexes, root position, limits, and invalid opcodes. |
| Formatting/parsing/serialization | Immutable expressions format through `IExpressionFormatter`. Infix, C#, Python, and LaTeX formatters support the maintained symbol set and preserve authored macros. Default infix output treats all constants as numeric literals; `InfixConstantNotation` can opt into `param(value)` and/or `fixed(value)` annotations. `InfixExpressionParser` interprets bare literals according to `NumericLiteralInterpretation` (fixed by default), while explicit annotations override it. JSON/binary serialization remains separate follow-up work. |
| Evaluator contract | Settled: `IProblem` and `Problem` are batch native, while `SingleSolutionProblem` is the scalar authoring base and currently owns scalar to batch execution. Evaluator operators return objective vectors and never replace candidates. Numeric-parameter optimization is a `Refiner`, not evaluator behavior. The evaluator return type is implemented as `EvaluatedCandidate<TCandidate>` until RF-2 changes it; see [symbolic-regression-parameter-fitting-plan.md](symbolic-regression-parameter-fitting-plan.md). |
| Numeric optimization factory API | Settled direction: expose common symbolic-expression evaluator configurations through `SymbolicExpressionEvaluator`, for example `SymbolicExpressionEvaluator.OptimizeNumericParameters(...)`; Levenberg-Marquardt can be the first implementation without being required in the common authoring name. |
| Operator validity | Stage 3 owner: unrestricted operators document bounded retry/failure behavior; grammar operators may use grammar-aware repair later. |
| Constant payloads | Settled: keep one `Constant` opcode with a `double` side table. `FixedConstant(value)` and `Constant(value)` are distinguished only in the expression tree. |
| Buffer/cache boundary | Settled: scratch buffers and column caches are interpreter internals scoped to an evaluation call or execution instance. |
| Thread safety | Settled: no shared mutable interpreter memory; shared state must be immutable. |
| Extension migration | Python interop symbolic-regression workflows and sliding-window regression use the immutable system, including fitness-time linear scaling. The remaining explicit-linear-scaling-grammar consumers use maintained legacy core, experimental, and scenario-test components until they migrate. |
