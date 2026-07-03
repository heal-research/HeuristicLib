# Symbolic Regression Reference Behavior Matrix

This matrix tracks Stage 1 reference behavior while the mutable tree API is phased out. HeuristicLab and the current mutable tree implementation are behavioral references only; new code should use HeuristicLib-native fixtures and terminology. Do not add a legacy-to-new converter. Rows are closed by reimplementing the relevant behavior as native tests that can remain after the old system is removed.

| Behavior | HeuristicLab reference | Current legacy type | New target | Reference level | Test status | Intentional differences |
| --- | --- | --- | --- | --- | --- | --- |
| Variable | `Variable` symbol | `Variable` / `VariableTreeNode` | `Variable` opcode with payload index into the compiled variable-reference table | Exact for name-bound scalar evaluation | Covered by primitive operation tests | Stage 1 draft authoring is name-first; compiled instructions use indexes only as compact table references. |
| Number | `Number` symbol | `Number` / `NumberTreeNode` | `NumericLiteral` opcode plus numeric-literal side table | Exact value evaluation | Covered by primitive operation tests | Fixed vs optimizable metadata participates in equality/hash but is ignored by the interpreter. |
| Add | `Addition` symbol | `Addition` | Binary `Add` opcode | Exact for binary and deterministic n-ary lowering | Covered by primitive operation tests | Legacy n-ary add is represented through explicit left-associative binary expressions. |
| Subtract | `Subtraction` symbol | `Subtraction` | Binary `Subtract` opcode | Exact for binary and deterministic n-ary lowering | Covered by primitive operation tests | Legacy n-ary subtract is represented through explicit left-associative binary expressions. |
| Multiply | `Multiplication` symbol | `Multiplication` | Binary `Multiply` opcode | Exact for binary and deterministic n-ary lowering | Covered by primitive operation tests | Legacy n-ary multiply is represented through explicit left-associative binary expressions. |
| Divide | `Division` symbol | `Division` | Binary `Divide` opcode | Ordinary `double` behavior | Covered by primitive operation tests | Invalid numeric results produce normal `double` results such as `NaN` or infinity; bounds and penalties remain outside the interpreter. |
| Log | `Logarithm` symbol | `Logarithm` | Unary `Log` opcode | Ordinary `double` behavior | Covered by primitive operation tests | Invalid numeric results produce `NaN`. |
| Sqrt | `SquareRoot` symbol | `SquareRoot` | Unary `Sqrt` opcode | Ordinary `double` behavior | Covered by primitive operation tests | Invalid numeric results produce `NaN`. |
| Linear scaling | Regression model scaling | `PredictAndAdjustScaling` / grammar linear scaling | Ordinary `Add(Multiply(scale, model), offset)` expression nodes | Behavioral equivalence after lowering | Not started | No dedicated opcode in Stage 1. Scaling constants can be authored as fixed or optimizable literals depending on the caller. |

## Stage 0 Design-Hole Outcomes

| Topic | Outcome or owner |
| --- | --- |
| API specs | Initial executable specs live in `test/HeuristicLib.Tests.ApiUsageSpecs/Regression/SymbolicRegressionRedesignSpecs.cs`; future implementation should replace commented sketches with real API calls as each stage lands, then promote behavior-heavy checks into regular unit tests in the owning project. |
| Problem metric API | Settled direction: problem convenience factories stay metric-agnostic, for example `CreateDefault(..., loss: Metrics.RMSE)`; metric names such as RMSE should be object shortcuts, not factory method names. |
| Search-space API | Current first draft: `SymbolicExpressionSearchSpace` represents the unrestricted search space directly. No unrestricted factory or subtype is part of the first API. |
| Legacy boundary | Settled in the redesign plan: move mutable tree APIs under `HEAL.HeuristicLib.Legacy...`, mark legacy types/members obsolete, and add `Legacy` prefixes/suffixes only for clashes. |
| Interpreter binding | Settled: `ExpressionDraft.Variable(name)` interns names into the compiled expression variable table; variable instructions store payload indexes into that table, and the interpreter resolves the referenced names once against the supplied dataset. |
| Reference behavior scope | Seeded by the matrix above; add rows before extending Stage 1 behavior coverage. |
| Instruction validity | Stage 1 owner: `SymbolicExpression` factories and ownership-transfer factories validate non-empty RPN, stack balance, arity, subtree length, payload indexes, root position, limits, and invalid opcodes. |
| Formatting/serialization | Settled: Stage 1 includes equality/hash and debug/infix formatting with default `x0` names plus optional supplied names; JSON/binary serialization is deferred. |
| Evaluator contract | Settled and implemented: `IProblem` and `Problem` are batch native, while `SingleSolutionProblem` is the scalar authoring base and currently owns scalar to batch execution. Evaluator operators return the authoritative evaluated `Solution<TGenotype>`. Numeric-parameter optimization is evaluator behavior, not a separate refiner operator. |
| Numeric optimization factory API | Settled direction: expose common symbolic-expression evaluator configurations through `SymbolicExpressionEvaluator`, for example `SymbolicExpressionEvaluator.OptimizeNumericParameters(...)`; Levenberg-Marquardt can be the first implementation without being required in the common authoring name. |
| Operator validity | Stage 3 owner: unrestricted operators document bounded retry/failure behavior; grammar operators may use grammar-aware repair later. |
| Numeric literal metadata | Settled: keep one `NumericLiteral` opcode; side-table entries carry value plus fixed/optimizable role, authored as `Fixed(value)` and `Parameter(value)`. |
| Buffer/cache boundary | Settled: scratch buffers and column caches are interpreter internals scoped to an evaluation call or execution instance. |
| Thread safety | Settled: no shared mutable interpreter memory; shared state must be immutable. |
| Extension migration | Stage 5 owner: examples, Python interop, sliding-window regression, and scenarios either migrate in extension stages or stay on legacy during scalar Stages 1-4. |
