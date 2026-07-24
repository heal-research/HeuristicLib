# Symbolic Regression Reference Behavior Matrix

This matrix tracks Stage 1 reference behavior while the mutable tree API is phased out. HeuristicLab and the current mutable tree implementation are behavioral references only; new code should use HeuristicLib-native fixtures and terminology. Do not add a legacy-to-new converter. Rows are closed by reimplementing the relevant behavior as native tests that can remain after the old system is removed.

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
| Linear scaling | Regression model scaling | `PredictAndAdjustScaling` / grammar linear scaling | Ordinary `Add(Multiply(scale, model), offset)` expression nodes | Behavioral equivalence after lowering | Not started | No dedicated opcode in Stage 1. Scaling constants can be authored as fixed or optimizable literals depending on the caller. |

## Operator Parity

The new immutable expression-tree operator family targets behavioral coverage rather than one-to-one preservation of legacy type names. Operators that are compositions or parameterizations of the new generic operators should not be reintroduced as separate compatibility types.

| Behavior | Legacy operator | New operator or decision | Status | Notes |
| --- | --- | --- | --- | --- |
| Grow initialization | `GrowTreeCreator` | `GrowTreeCreator` | Implemented | Samples any structurally viable symbol within the remaining length and depth budgets. |
| Full initialization | `FullTreeCreator` | `FullTreeCreator` | Implemented | Produces trees whose leaves occur at one selected depth. |
| Ramped half-and-half initialization | Ramped full/grow population initialization | `RampedHalfAndHalfTreeCreator` | Implemented | Distributes creation across an inclusive depth range and alternates full and grow creation. |
| Balanced target-length initialization | `BalancedTreeCreator` | Deferred `BalancedTreeCreator` design | Deferred | A useful initialization policy, but not required by the unrestricted vertical slice. Its target-length, breadth-first expansion, mixed-arity, depth-limit, and irregularity-bias semantics must be specified before implementation. |
| Probabilistic target-size initialization | `ProbabilisticTreeCreator` / PTC2 | Deferred probabilistic creator design | Deferred | Keep separate from balanced creation. A future implementation should expose the intended size distribution and symbol-probability contract explicitly. |
| Subtree crossover | `SubtreeCrossover` | `SubtreeCrossover` | Implemented | Selects a destination and a valid donor without bounded retry loops. |
| Same-arity node replacement | `ChangeNodeTypeManipulation` | `NodeReplacementMutator` | Implemented | Replaces a symbol while retaining the existing children when arity is unchanged. |
| Random subtree replacement | `ReplaceBranchManipulation` | `SubtreeMutator` | Implemented | Generates one structurally viable replacement subtree within the exact remaining budgets. |
| One-point local-parameter mutation | `OnePointShaker` | `LocalPerturbationMutator(One)` | Implemented | Perturbation behavior belongs to the originating symbol. |
| All-point local-parameter mutation | `FullTreeShaker` | `LocalPerturbationMutator(All)` | Implemented | Also supports independent per-point selection through `Each(probability)`. |
| Random choice among mutators | `MultiSymbolicExpressionTreeManipulator` | `ChooseOneMutator` | Implemented through generic composition | Symbolic expressions do not require a dedicated multi-mutator type. |
| Strict structural shrinking | `RemoveBranchManipulation` | `ShrinkSubtreeMutator` | Implemented | Replaces a selected operation occurrence with a terminal sampled from the search space, guaranteeing a strict length reduction without retries. |
| Hoist mutation | No direct legacy equivalent | Deferred | Optional | Replaces a selected subtree with one of its descendants. Consider only after measured need; shrink mutation already provides a simple anti-bloat structural operator. |
| Size-fair or homologous crossover | No direct legacy equivalent | Deferred | Optional | These are alternative crossover policies, not prerequisites for numeric-parameter optimization. |
| Evolvable-constant optimization | Levenberg-Marquardt parameter optimization | `NumericParameterOptimizer.Optimize(...)` | Direct solver implemented | Uses a parameter-aware compiler and batched reverse mode without a materialized Jacobian. Evaluator/population composition remains follow-up work. |

## Stage 0 Design-Hole Outcomes

| Topic | Outcome or owner |
| --- | --- |
| API specs | Initial executable specs live in `test/HeuristicLib.Tests.ApiUsageSpecs/Regression/SymbolicRegressionRedesignSpecs.cs`; future implementation should replace commented sketches with real API calls as each stage lands, then promote behavior-heavy checks into regular unit tests in the owning project. |
| Problem metric API | Settled direction: problem convenience factories stay metric-agnostic, for example `CreateDefault(..., loss: Metrics.RMSE)`; metric names such as RMSE should be object shortcuts, not factory method names. |
| Search-space API | Current first draft: `ExpressionTreeSearchSpace` represents the unrestricted search space directly. No unrestricted factory or subtype is part of the first API. |
| Legacy boundary | Settled in the redesign plan: move mutable tree APIs under `HEAL.HeuristicLib.Legacy...`, mark legacy types/members obsolete, and add `Legacy` prefixes/suffixes only for clashes. |
| Interpreter binding | Settled: `ExpressionDraft.Variable(name)` interns names into the compiled expression variable table; variable instructions store payload indexes into that table, and the interpreter resolves the referenced names once against the supplied dataset. |
| Reference behavior scope | Seeded by the matrix above; add rows before extending Stage 1 behavior coverage. |
| Instruction validity | Stage 1 owner: `ExpressionTree` factories and ownership-transfer factories validate non-empty RPN, stack balance, arity, subtree length, payload indexes, root position, limits, and invalid opcodes. |
| Formatting/serialization | Settled: Stage 1 includes equality/hash and debug/infix formatting with default `x0` names plus optional supplied names; JSON/binary serialization is deferred. |
| Evaluator contract | Settled and implemented: `IProblem` and `Problem` are batch native, while `SingleSolutionProblem` is the scalar authoring base and currently owns scalar to batch execution. Evaluator operators return authoritative `EvaluatedCandidate<TCandidate>` values. Numeric-parameter optimization is evaluator behavior, not a separate refiner operator. |
| Numeric optimization factory API | Settled direction: expose common symbolic-expression evaluator configurations through `SymbolicExpressionEvaluator`, for example `SymbolicExpressionEvaluator.OptimizeNumericParameters(...)`; Levenberg-Marquardt can be the first implementation without being required in the common authoring name. |
| Operator validity | Stage 3 owner: unrestricted operators document bounded retry/failure behavior; grammar operators may use grammar-aware repair later. |
| Constant payloads | Settled: keep one `Constant` opcode with a `double` side table. `FixedConstant(value)` and `Constant(value)` are distinguished only in the expression tree. |
| Buffer/cache boundary | Settled: scratch buffers and column caches are interpreter internals scoped to an evaluation call or execution instance. |
| Thread safety | Settled: no shared mutable interpreter memory; shared state must be immutable. |
| Extension migration | Stage 5 owner: examples, Python interop, sliding-window regression, and scenarios either migrate in extension stages or stay on legacy during scalar Stages 1-4. |
