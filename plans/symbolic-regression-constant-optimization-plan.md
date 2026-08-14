# Symbolic Regression Constant Optimization Plan

## Status

This is a living design and implementation plan. Its internal API shapes may continue to evolve as the remaining stages are implemented and reviewed.

This plan refines Stage 3.1 of [symbolic-regression-redesign-plan.md](symbolic-regression-redesign-plan.md). The redesign plan should be updated to point here after this plan has been reviewed.

### Settled direction for the first implementation

The first implementation is a focused vertical slice for symbolic-regression constant optimization. It does not wait for a public automatic-differentiation API, a general numerical-optimization framework, L-BFGS, or generic memetic composition.

The sequence is:

1. implement an internal automatic-differentiation engine with its own execution-oriented representation;
2. lower an `ExpressionTree` into that representation while recording occurrence-safe bindings for optimizable constants;
3. expose model values, raw targets, and parameter derivatives to MathNet's Levenberg-Marquardt implementation through a thin, measured adapter;
4. optimize all training rows against raw targets using least-squares mean squared error;
5. do not reject a successful numerical result because its parameter values or mean squared error are non-finite;
6. rebuild the immutable expression through one `ExpressionTree.ReplaceMany` operation;
7. verify output behavior through analytic expectations, finite-difference checks, and maintained legacy behavior where applicable, then measure the complete pipeline before generalizing it.

MathNet is the first solver backend, not a permanent architectural commitment. The implementation should keep the backend boundary small enough to replace, but must not introduce dependency injection, reflection, virtual dispatch in hot loops, or a generalized plugin architecture merely to permit a hypothetical future solver. Direct use of MathNet storage types is acceptable where measurement shows that it avoids meaningful conversion or allocation cost.

The first automatic-differentiation and Levenberg-Marquardt APIs are internal. Both are plausible standalone capabilities for HeuristicLib users, so completing constant optimization must be followed by an explicit public-API review. That review selects the useful authoring, compilation, execution, result, and failure types rather than making the complete internal implementation public by default. Public authoring APIs, generic local-improvement composition, additional numerical optimizers, sampling, scaling-aware optimization, and evaluator integration follow only after the direct constant-optimization path is correct and fast.

## Problem Statement

Constant optimization in symbolic regression is not one isolated solver call. The complete design combines four distinct capabilities:

1. differentiating a parameterized numeric model;
2. running a numerical optimization algorithm over that model;
3. composing local improvement with another search algorithm to form a memetic algorithm;
4. adapting an `ExpressionTree` and a symbolic-regression problem to those general capabilities.

Combining all four concerns in one regression-specific feature would create several problems:

- differentiation is tied directly to `ExpressionTree`, `DataFrame`, regression targets, and evolvable constants;
- Levenberg-Marquardt policy, linear algebra, sampling, metric acceptance, expression compilation, and immutable tree replacement are mixed into one feature;
- internal types are named by the complete use case rather than their actual role;
- numerical optimization is not independently usable for real-vector problems, test functions, or future differentiable models;
- memetic composition is represented as symbolic-regression evaluator behavior instead of a general algorithm/operator concept;
- the old mutable symbolic-regression system, the new immutable expression system, and two parameter-optimization implementations coexist in the same broad area without clear ownership boundaries.

The replacement must preserve clear responsibility boundaries. The first implementation may deliver a direct vertical slice through those boundaries before every layer has a generalized public API.

## Terminology

The following terms are provisional until the corresponding design stage is completed:

| Term | Meaning in this plan |
| --- | --- |
| Differentiable model | A parameterized numerical computation that can evaluate values and requested derivatives. It is independent of symbolic regression. |
| Differentiation program or tape | An immutable execution representation of a differentiable model. The final term depends on whether the representation is user-authored, compiled, or purely internal. |
| Numerical optimization problem | A parameter vector, an objective model, and the semantics needed by a numerical optimization algorithm. It is not a symbolic-regression problem. |
| Numerical optimization algorithm | A first-class HeuristicLib algorithm such as gradient descent, L-BFGS, or Levenberg-Marquardt. |
| Local improvement | Running a bounded optimization process from an existing candidate and returning an equal or improved candidate. |
| Memetic algorithm | A search process that systematically composes global search with local improvement. |
| Constant optimization | The symbolic-regression adapter that treats each evolvable constant occurrence as a numerical parameter and returns an immutable replacement `ExpressionTree`. |

Use `candidate`, `algorithm`, `operator`, `evaluator`, `configuration`, `execution instance`, and `objective vector` according to [docs/glossary.md](../docs/glossary.md).

Do not prefix every type with the complete use-case name. Types should be named after one clear responsibility and placed in a namespace that supplies the broader context.

## Target Layering

The long-term dependency direction is:

```text
Symbolic-regression evaluator and expression adapter
                    |
         Generic local-improvement composition
                    |
       Numerical optimization algorithms and problems
                    |
         Automatic differentiation and numerics
```

Dependencies must not point upward:

- automatic differentiation knows nothing about regression, metrics, expression trees, or candidates;
- numerical optimization knows nothing about symbolic regression or immutable tree replacement;
- memetic composition knows that a candidate can be locally improved, but not how expression constants are represented;
- the symbolic-regression adapter owns expression occurrence discovery, lowering, data binding, and rebuilding the improved tree.

This layering does not prescribe delivery order. In particular, the first symbolic-regression constant optimizer may call an internal least-squares runner directly. Generic local-improvement and memetic composition remain a later consumer of the same constant-optimization capability, not a prerequisite for it.

## Stage 0: Organize The Symbolic-Regression Area

Before designing new public numerical APIs, document and reorganize the current symbolic-regression implementation.

Current status:

- still-required mutable components remain active in their established domain folders until their consumers migrate;
- the maintained expression domain is organized under
  `HEAL.HeuristicLib.Genotypes.SymbolicExpressions`;
- tabular data and prediction contracts live under
  `HEAL.HeuristicLib.DataAnalysis`;
- regression data, regressors, and metrics live under
  `HEAL.HeuristicLib.DataAnalysis.Regression`;
- `SymbolicRegressionProblem` lives under
  `HEAL.HeuristicLib.Problems.DataAnalysis.Regression`.

### Ownership Map

| Concern | Maintained location | Notes |
| --- | --- | --- |
| Expression tree, nodes, points, drafts, and queries | `Genotypes/SymbolicExpressions` | Candidate structure and authoring model. |
| Symbols and local perturbation policy | `Genotypes/SymbolicExpressions` | Symbols are referenced by nodes and cannot depend upward on a search-space namespace. |
| Compilation and compiled representation | `Genotypes/SymbolicExpressions` | Shared CPU execution representation and future backend input. |
| Interpretation and compile-first convenience evaluation | `Genotypes/SymbolicExpressions` | Consumes `DataFrame` but remains independent of regression objectives. |
| Expression formatting | `Genotypes/SymbolicExpressions/Formatting` | Immutable expressions support infix and language-specific formatting directly from the authored genotype. Formatters preserve macro symbols; lowering remains exclusively a compilation concern. |
| Tabular columns, series, and prediction contracts | `DataAnalysis` | General data-analysis input and fitted prediction behavior rather than genotype state. |
| Regression data, regressors, and prediction metrics | `DataAnalysis/Regression` | Metrics compare predictions with targets and carry objective direction. |
| Symbolic-regression optimization problem | `Problems/DataAnalysis/Regression` | Binds training data, metric, and expression search space for algorithms. |
| Unrestricted expression search space | `SearchSpaces/SymbolicExpressions` | Admissibility plus current symbol-selection guidance. |
| Creation, crossover, and mutation | role-oriented `Operators/.../SymbolicExpressions` namespaces | Operator role remains the primary namespace grouping. |

Legacy prediction evaluators migrate as `IRegressionMetric` implementations. Candidate-aware measures such as tree length, variable count, and structural complexity are not regression metrics; they require a later candidate-aware objective or evaluator composition.

### Inventory

Classify every symbolic-regression-related type as one of:

- legacy mutable expression system;
- immutable expression candidate model;
- compilation and interpretation;
- unrestricted search space and variation operators;
- regression problem, data, metric, and evaluation;
- experimental or future grammar-guided functionality;
- legacy parameter optimization.

Record which types are active, transitional, or obsolete. Do not infer ownership from the current folder alone.

### Organization goals

- The immutable `ExpressionTree`, symbols, nodes, points, drafts, compilation, and interpretation form one coherent symbolic-expression area.
- Regression data and metrics remain data-analysis concerns rather than expression concerns.
- A symbolic-regression problem composes expression search, regression data, and evaluation without owning generic differentiation or numerical solvers.
- Old and new systems must be distinguishable by namespace and documentation while both remain in the repository.
- Folder names and namespaces should express responsibility without repeating `SymbolicExpression` on every local type.

### Deliverable

The ownership map above is implemented. Further formatter, metric, parser, and extension migration proceeds capability by capability with native tests.

No automatic-differentiation API is implemented until this organization is agreed.

## Stage 1: Internal Automatic Differentiation

### V1 scope and boundary

The first engine is an internal, `double`-only reverse-mode implementation under
`HEAL.HeuristicLib.Numerics.AutomaticDifferentiation`. It has no dependency on
symbolic expressions, data analysis, regression, MathNet, or solver policy.

It compiles one scalar calculation and evaluates that calculation independently
for each aligned input row. The first derivative surface contains only:

- value-only evaluation;
- values plus a dense parameter-major Jacobian;
- a single-row Jacobian as the scalar gradient.

Jacobian-vector products, vector-Jacobian products, streamed Jacobian consumers,
sparse derivatives, multiple outputs, higher derivatives, forward mode, generic
numeric types, and tensor broadcasting are follow-up capabilities. The program
representation must not depend on them in V1.

### External design reference

The design is conceptually similar to
[alexshtf/AutoDiff](https://github.com/alexshtf/autodiff): both compile an
authoring graph into a reusable reverse-mode program, preserve explicit DAG
sharing, return the forward value with derivative information, and distinguish
differentiated parameters from non-differentiated inputs.

The relevant reference points are:

- [compiled terms are intended for repeated evaluation](https://github.com/alexshtf/autodiff/blob/master/docs/autodiff-revisited.md);
- [the compiler memoizes reused source terms](https://github.com/alexshtf/autodiff/blob/master/AutoDiff/Core/AutoDiff/CompiledDifferentiator.Compiler.cs);
- [differentiation returns the already-computed forward value](https://github.com/alexshtf/autodiff/blob/master/AutoDiff/Core/AutoDiff/CompiledDifferentiator.cs);
- [parametric terms distinguish variables and fixed parameters](https://github.com/alexshtf/autodiff/blob/master/AutoDiff/Core/AutoDiff/IParametricCompiledTerm.cs).

V1 deliberately differs from that library in its execution representation.
AutoDiff stores mutable values and adjoints in polymorphic tape elements and
dispatches through virtual methods. This engine uses a closed indexed SSA
program with separate mutable execution buffers, supports row batches,
and performs no delegate or virtual dispatch in operation loops.

AutoDiff also stores local partial derivatives as mutable edge weights during
its forward sweep. V1 instead recomputes partials from retained primal values to
reduce row-dependent workspace. The end-to-end performance increment profiles
this decision, especially for `sin` and `cos`, before any selective derivative
cache is considered.

### Construction and compiled program

The internal model contains:

- `Operation`: the closed V1 operation set;
- `Value`: a builder-owned instruction handle with no numeric runtime state;
- `Builder`: the mutable authoring and validation boundary;
- `Program`: the immutable compiled SSA program;
- `Execution`: the disposable, mutable execution instance.

These types rely on the `AutomaticDifferentiation` namespace for context and do not repeat an `Ad` prefix.

`Builder` provides `Input`, `Parameter`, `Constant`, the supported unary and
binary operations, and `Build(root)`.

- Reusing a `Value` explicitly represents a shared subexpression or shared
  parameter.
- Cross-builder handles and builder use after `Build` throw immediately.
- Input and parameter indexes follow creation order.
- Every declared input and parameter must be reachable from the root.
- Unreachable intermediate calculations are discarded.
- V1 performs no constant folding, identity elimination, or automatic
  common-subexpression elimination.

`Program` stores an array-of-structs instruction sequence in topological
order. Every instruction records:

- its operation and up to two earlier operand indexes;
- its input, parameter, or constant payload index;
- its vector-primal slot or `-1`;
- its adjoint slot or `-1`.

Input and parameter dependencies are determined while compiling the graph but
are not stored separately. An input operation or nonnegative vector-primal slot
identifies input dependence; a nonnegative adjoint slot identifies parameter
dependence.

Constants live in a separate immutable array. A compact
`parameterInstructionIndices` array maps parameter order to the corresponding
instruction. Reverse propagation is therefore independent of Jacobian result
extraction. The program owns no evaluation buffers and is safe to reuse across
concurrent executions.

### Initial operation semantics

V1 supports inputs, parameters, constants, addition, subtraction,
multiplication, division, negation, `exp`, `log`, `sin`, `cos`, `tan`, and
`tanh`.

The reverse partials are:

| Operation | Left/input partial | Right partial |
| --- | --- | --- |
| `Add(left, right)` | `1` | `1` |
| `Subtract(left, right)` | `1` | `-1` |
| `Multiply(left, right)` | `right` | `left` |
| `Divide(left, right)` | `1 / right` | `-left / (right * right)` |
| `Negate(input)` | `-1` | — |
| `Exp(input)` | output | — |
| `Log(input)` | `1 / input` | — |
| `Sin(input)` | `Cos(input)` | — |
| `Cos(input)` | `-Sin(input)` | — |
| `Tan(input)` | `1 + output * output` | — |
| `Tanh(input)` | `1 - output * output` | — |

Evaluation uses ordinary IEEE 754 propagation. It does not protect operations,
clamp values, or throw for non-finite intermediate results. Invalid graph
construction, argument shape, buffer size, lifetime, and ownership errors throw
early. The first solver adapter preserves non-finite numerical results rather
than validating or sanitizing them.

### Execution ownership and memory

AD-2 introduces `Program.CreateExecution()` for one-row input-free scalar programs. It
owns one pooled scalar-primal buffer. The bound overload may give an input-free
program any positive row count; evaluation repeats its scalar value and parameter
derivatives for every requested row.
AD-3 adds the bound-input overload described below together with batched vector
storage; the scalar convenience remains available.

`Program.CreateExecution(inputColumns, rowCount, batchCapacity)`
creates an `Execution`. The provisional default capacity is `256`.

- Input bindings are copied as `ReadOnlyMemory<double>` descriptors; numerical
  input data is not copied.
- A parameter-only scalar function uses no input columns. The parameterless creation convenience produces one result, while the bound overload repeats the scalar result for its requested row count.
- The execution rents scalar-primal, vector-primal, and batch-adjoint buffers from
  `ArrayPool<double>` and returns them on disposal.
- The execution is mutable, non-reentrant, and not thread-safe. Concurrent runs
  create separate executions over the same program.
- Outputs and the final Jacobian are caller-owned.
- Bound input columns may overlap one another because they are read-only. Output and Jacobian buffers must not overlap parameters, bound input columns, or one another; invalid overlap throws before evaluation.
- Targets and residual semantics do not belong to automatic differentiation.
  Targets enter through the least-squares adapter, while MathNet computes the
  residuals for its model-plus-target formulation.

The execution surface contains two explicit operations:

```csharp
execution.Evaluate(parameters, outputs);
execution.EvaluateWithJacobian(parameters, outputs, jacobian);
```

The Jacobian layout is parameter-major:

```text
jacobian[(parameterIndex * rowCount) + rowIndex]
```

One evaluation:

1. computes scalar instructions once for the parameter vector;
2. reads bound input columns directly in row batches;
3. computes input-dependent instructions into vector-primal slots;
4. materializes the root into caller-owned output storage;
5. clears active adjoints and seeds the root with ones when derivatives are
   requested;
6. traverses instructions in reverse and recomputes local partials from primal
   values;
7. extracts parameter adjoints through `parameterInstructionIndices` directly
   into the corresponding contiguous Jacobian column ranges.

Forward and reverse processing are adjacent for each row batch, so vector primals and adjoints are retained only up to `batchCapacity`, not for all rows.

Operation dispatch occurs outside per-element loops. For every batch operation,
first consider the span-based `TensorPrimitives` kernel. Consider
`Vector<double>` or lower-level SIMD only where a suitable primitive is absent
or measurement demonstrates a material benefit. Keep clear scalar loops where
SIMD does not fit the operation or reverse-accumulation semantics. No operation
allocates during execution.

Composite reverse expressions such as `upstream * (1 + output²)` remain fused loops when expressing them with `TensorPrimitives` would require a temporary derivative vector, destructive primal reuse, or additional full-span passes. The end-to-end performance increment measures these loops before introducing scratch storage or selective in-place primal reuse.

A preliminary AVX2 kernel comparison during AD-5 found explicit `Vector<double>` loops materially faster at the provisional batch capacity of `256` for trigonometric derivative accumulation, `tan`/`tanh` output-based derivatives, and vector-valued division derivatives. Those rules use a vector loop followed by a scalar tail. The performance-decision increment must confirm the gain end to end and across representative batch sizes rather than treating this machine-specific kernel result as final tuning evidence.

### AD implementation checkpoints

Checkpoint states are `Pending`, `In progress`, `Awaiting review`, and
`Accepted`. Implementation stops at `Awaiting review`. Only explicit user
acceptance moves a checkpoint to `Accepted` and permits work on the next one.

| Checkpoint | Status | Deliverable |
| --- | --- | --- |
| AD-0 Design contract | Accepted | Record the settled design, AutoDiff comparison, operation semantics, derivative surface, ownership model, and checkpoint process. No source code. |
| AD-1 Graph and compilation | Accepted | Implement operations, handles, builder validation, immutable SSA instructions, reachability, dependency classification, slots, and parameter-instruction mapping. |
| AD-2 Scalar forward evaluation | Accepted | Implement execution ownership, pooled buffers, and parameter/constant-only single-result execution. |
| AD-3 Batched forward evaluation | Accepted | Bind input columns, evaluate scalar work once, process vector work in row batches, and materialize outputs. |
| AD-4 Scalar reverse differentiation | Accepted | Implement reverse rules, parameter-adjoint extraction, gradients, and shared-subexpression accumulation. |
| AD-5 Batched dense Jacobian | Accepted | Implement batched reverse evaluation and direct column-major Jacobian writes. |
| AD-6 Functional hardening | Accepted | Complete numerical, contract, memory, and concurrency hardening needed before integration. |
| AD-6a Numerical and contract hardening | Accepted | Test non-finite propagation, SIMD paths and scalar tails, repeated execution, and settle caller-buffer aliasing rules. |
| AD-6b Memory and concurrency | Accepted | Verify zero managed allocations after execution creation, pooled-buffer lifecycle, concurrent program reuse, and partial-batch adjoint clearing cost. |

AD-6 ends after AD-6b. The formerly planned AD-6c through AD-6e work is postponed to the performance-decision increment, after expression lowering, the solver adapter, constant optimization, immutable rebuilding, and behavioral comparison provide representative end-to-end workloads. The provisional capacity remains `256`; no performance specialization is accepted from isolated AD measurements alone.

AD-6a confirms ordinary IEEE 754 propagation without protected operations or finite-value exceptions, compares vectorized reverse rules with scalar tails at boundaries derived from `Vector<double>.Count`, and verifies that mixed forward and Jacobian evaluations retain no stale values. Writable caller buffers are required to be mutually disjoint and not overlap parameters or bound input columns; read-only input columns may overlap one another.

AD-6b confirms that warmed `Evaluate` and `EvaluateWithJacobian` calls allocate no managed memory, disposed executions do not leak stale pooled values into later executions, and separate executions can evaluate one immutable program concurrently. The allocation and concurrency stress checks live in the scenario suite to keep the ordinary unit-test loop fast; pooled-buffer correctness remains a focused unit test. Adjoint slots are packed using the active row count for each batch, so only `adjointSlotCount * batch.Count` elements are cleared. A focused microbenchmark found this materially cheaper for small final batches and approximately neutral as the batch approaches capacity, so the capacity-sized pooled allocation is retained while clearing and indexing use the active size.

### Deferred operation-model consolidation review

Do not generalize the operation model during the current AD checkpoints. At the complete vertical-slice review, inventory the repeated operation semantics and dispatch across symbolic symbols, expression lowering, scalar and batched expression evaluation, AD lowering, scalar and batched AD evaluation, reverse differentiation, formatting, and expression drafts.

The vertical-slice review must make and document an explicit decision among at least these outcomes:

- share a lower-level numerical program and forward execution engine;
- retain separate programs but generate their closed operation switches and mappings from one compile-time operation catalog;
- retain selected duplication where representations or hot-path requirements materially differ, with those differences recorded.

This review must distinguish numerical primitives from domain-level symbols and macros. It must not introduce a runtime registry, delegate dispatch, reflection, or polymorphic hot-loop architecture merely to reduce source repetition. Any consolidation deferred beyond the first constant-optimization slice requires a named follow-up increment rather than an untracked cleanup note.

### First integration-ready artifact

The V1 AD artifact is ready for expression lowering and solver integration after AD-6 when:

- analytic and central finite-difference tests cover every supported operation;
- scalar gradients and batched Jacobian rows agree;
- row counts across batch boundaries behave consistently;
- repeated evaluation retains no stale values and allocates no managed memory
  after execution creation;
- separate executions safely reuse one immutable program concurrently;
- no symbolic-regression or solver type is referenced by the differentiation
  core.

## Stage 2: First Least-Squares Backend

### Internal least-squares contract

The first backend is a thin internal adapter from an automatic-differentiation `Execution` to MathNet's Levenberg-Marquardt minimizer. It receives the differentiable model, initial parameters, target values, and the maximum number of solver iterations. The AD execution returns model values and their parameter derivatives; MathNet owns the residual convention `target - model`.

The initial non-throwing surface is:

```csharp
internal static bool TryMinimize(
    AD.Execution model,
    ReadOnlySpan<double> initialParameters,
    ReadOnlySpan<double> targets,
    int maximumIterations,
    [NotNullWhen(true)] out LevenbergMarquardtResult? result,
    [NotNullWhen(false)] out LevenbergMarquardtFailure? failure,
    CancellationToken cancellationToken = default);
```

Successful results contain only an owned copy of the final parameter vector and the final mean squared error:

```csharp
internal sealed record LevenbergMarquardtResult(double[] Parameters, double MeanSquaredError);

internal sealed record LevenbergMarquardtFailure(string Message);
```

An expected unsuccessful solve returns `false` with a small immutable `LevenbergMarquardtFailure`. Neither outcome exposes MathNet result types. Invalid caller arguments and use of a disposed AD execution remain programming errors and throw. Cancellation propagates as `OperationCanceledException`; it is not represented as a solver failure.

Do not force least squares through a generic scalar-objective contract that loses its model, target, and Jacobian structure. Do not introduce a generalized solver abstraction for the first backend. A future numerical-optimization area may add broader contracts after the complete constant-optimization slice provides implementation evidence.

Although the initial adapter is internal, standalone least-squares fitting is a credible user-facing capability. Its eventual public surface is deliberately postponed until constant optimization exercises the full lifecycle and reveals which configuration, result, failure, cancellation, and ownership details are stable.

### Initial execution model

The adapter is the numerical workhorse for one synchronous, bounded solve. MathNet stops through convergence or its maximum-iteration setting. Reaching the maximum is a successful adapter result when MathNet returns a result; the adapter does not translate this into HeuristicLib termination semantics.

This workhorse does not reference or participate in HeuristicLib algorithms, operators, terminators, execution-instance infrastructure, evaluation accounting, or search-state production. Coupling the constant optimizer to those facilities is a later orchestration task and is not part of LM-0 through LM-3.

Cancellation is checked before entering MathNet and from the model and Jacobian callbacks because the selected MathNet API has no direct `CancellationToken` parameter. The adapter introduces no randomness.

### Solver sequence

Implement in this order:

1. a thin adapter from internal model-value/Jacobian evaluation and raw targets to MathNet's Levenberg-Marquardt implementation;
2. the direct symbolic-regression constant-optimization vertical slice;
3. focused benchmarks separating differentiation, Jacobian preparation, adapter overhead, solver work, and complete-pipeline cost;
4. only then decide whether to retain MathNet, implement a specialized LM runner, or add other optimizers such as L-BFGS.

MathNet is the first backend only. Keep the boundary narrow and concrete, but avoid an abstraction hierarchy whose only purpose is hypothetical backend interchangeability. If direct MathNet arrays, vectors, matrices, or column-major storage remove measurable copies or allocations, prefer them at the adapter boundary.

### Linear algebra

Do not implement or bury factorization code inside the symbolic-regression adapter while MathNet is the backend.

The AD Jacobian uses parameter-major storage, `jacobian[(parameterIndex * rowCount) + rowIndex]`. This is the same memory order as a MathNet column-major matrix with rows representing model values and columns representing parameters. The adapter therefore wraps the existing Jacobian array directly in a `DenseMatrix` without transposition or copying.

Likewise, callback output arrays are wrapped directly in `DenseVector` instances. Dense parameter vectors use their backing arrays directly when MathNet exposes them; a non-dense vector may require a fallback copy. Targets may be copied once into MathNet-owned observed storage, and the final parameter vector is copied once so the result owns its lifetime. Repeated callback work reuses output and Jacobian buffers.

MathNet's separate model and Jacobian callbacks may cause a duplicated forward sweep because `EvaluateWithJacobian` also computes model values. Accept this for the first adapter and measure it later in the representative end-to-end benchmark. A custom `IObjectiveModel`, a specialized LM implementation, Cholesky kernels, and alternative linear algebra are deferred until that evidence shows a material cost.

### Levenberg-Marquardt requirements

The first adapter has these explicit semantics:

- MathNet receives model values, targets, and the model Jacobian `df/dp`; the adapter does not negate derivatives or allocate a residual buffer;
- the returned mean squared error is MathNet's final residual sum of squares divided by the target count;
- ordinary IEEE 754 non-finite values from AD pass through without clamping, replacement, or early rejection;
- a result returned by MathNet remains successful even when its parameters or mean squared error are non-finite;
- an exception originating from the MathNet solve is converted to `LevenbergMarquardtFailure`, except for cancellation, which propagates;
- convergence and maximum iterations are controlled by MathNet; no HeuristicLib termination or accounting concepts enter the adapter;
- the adapter does not retain a best finite point, decide whether a result is acceptable, rebuild an expression, or consult a regression metric.

Expression rebuilding belongs to the constant-optimization component. Best-point retention and acceptance are separate later policies. Expression discovery, future row sampling, and HeuristicLib operator integration also remain outside the numerical adapter.

### MathNet adapter checkpoints

Checkpoint states are `Pending`, `In progress`, `Awaiting review`, and `Accepted`. Implementation stops at `Awaiting review`; only explicit user acceptance advances to the next checkpoint.

| Checkpoint | Status | Deliverable |
| --- | --- | --- |
| LM-0 Design contract | Accepted | Document the `TryMinimize` boundary, model-plus-target formulation, Jacobian convention, result and failure semantics, non-finite behavior, cancellation, MathNet stopping behavior, storage ownership, and exclusions. No source code. |
| LM-1 Successful solve path | Accepted | Implement the adapter, expose the required AD execution dimensions, wrap output and Jacobian arrays without copying, and verify a simple linear least-squares solve. |
| LM-2 Outcome behavior | Accepted | Add argument validation, cancellation, MathNet-exception conversion, maximum-iteration behavior, and non-finite result propagation. |
| LM-3 Verification and hardening | Accepted | Add fast nonlinear, mean-squared-error, Jacobian-orientation, repeated-solve, and storage-lifetime tests to the core suite; place calculation-intensive stress cases in the scenario suite; confirm the adapter remains independent of expressions, regression, and HeuristicLib algorithm/operator infrastructure; record duplicated-forward-sweep benchmarking as deferred work. |

LM-0 through LM-3 cover only the MathNet workhorse adapter. They explicitly exclude HeuristicLib termination, evaluation accounting, algorithms, operators, general optimizer APIs, solver interchange abstractions, constant acceptance, and symbolic-expression rebuilding.

LM-3 verifies exact nonlinear fitting, non-zero MSE normalization, the parameter-major-to-column-major Jacobian mapping, repeated solves over one execution, and result ownership beyond caller-buffer and execution lifetimes. These remain fast core tests. No calculation-intensive test is needed for the focused numerical contracts; representative large-row, large-parameter, repeated-solve, and duplicated-forward-sweep measurements remain scenario and benchmark work after the complete constant-optimization workload exists. A dependency audit confirms that the AD and optimization areas do not reference symbolic expressions, data analysis, regression, or HeuristicLib algorithm/operator infrastructure.

### Second usable artifact

Provide focused tests that optimize ordinary least-squares problems before the expression adapter is connected:

- linear and nonlinear least squares with LM;
- analytic derivatives checked against automatically differentiated derivatives;
- non-finite propagation, MathNet failures, cancellation, and iteration limits;
- Jacobian orientation, mean-squared-error normalization, storage lifetime, and repeated solves.

Ordinary contract and numerical examples must remain fast unit tests in `HeuristicLib.Tests`. Larger row counts, parameter counts, repetition counts, and other calculation-intensive stress coverage belong in `HeuristicLib.Tests.Scenarios` so the normal development loop remains fast.

The artifact is complete when the AD engine and MathNet adapter can solve an ordinary least-squares problem without referencing symbolic regression. A general public numerical-optimization API is not required.

## Stage 3: Refinement And Memetic Composition

This stage was initially deferred until the direct constant-optimization slice established its transformation, failure, and cost semantics. That slice is now complete enough to settle the general integration model.

### Canonical refiner role

`Refiner` is a first-class HeuristicLib operator role with the semantic contract:

```text
Candidate → Candidate
```

The explicit algorithm refiner is the single integration mechanism for
refinement. There is no candidate-transforming evaluator and no second default
path. Objective-aware retention is available as one particular refiner, the
improvement-checking refiner described below, and therefore composes like any
other refiner.

A refiner returns a candidate and never a fitness value, and it does not promise improvement according to the problem objective. Algorithms may place refinement wherever their lifecycle requires it. The conventional placement for ordinary optimization algorithms is after initial creation and after structural or stochastic variation, immediately before evaluation:

```text
Creation → Refinement → Evaluation
Variation → Refinement → Evaluation
```

Already evaluated candidates carried forward unchanged, such as elites, are not normally refined again. Algorithms remain responsible for explicit placement because initialization, offspring production, neighborhood generation, restarts, and final postprocessing are not one universal lifecycle event. Shared orchestration may reduce repetitive refine-then-evaluate code, but it must not hide the `Refiner` operator from algorithm configuration.

An ordinary refiner performs no problem evaluation, so the algorithm evaluates its result exactly once. The general refiner contract contains no before/after objective comparison or retention guarantee. A particular refiner may use its own internal numerical acceptance information, such as the least-squares loss inside constant optimization, but that does not become a general problem-objective promise.

### Operators, evaluation, and visibility

An earlier draft of this plan claimed a refiner cannot check improvement "by
construction." That was wrong. Every operator role already receives the problem —
`IMutatorInstance.Mutate(parents, random, searchSpace, problem)` and its
counterparts — and `IProblem.Evaluate(candidates, random)` is public. Any
operator can therefore evaluate today.

The invariant that actually matters is about visibility, not capability:

> Evaluation that should be visible to budgets, termination, analysis, and
> instrumentation must go through an evaluator operator. Calling
> `problem.Evaluate` directly from an operator is legal but invisible, and is
> therefore discouraged.

This is precisely why the evaluator exists as an operator rather than as a plain
method call: it is the hook where counting, limiting, caching, and observation
attach. A refiner that needs objective information therefore takes an
`IEvaluator` as a configured dependency. That is not an exception to the role
model; it is the correct way to do something operators could otherwise do
invisibly.

### Refiner composition topologies

Refinement is composed through ordinary operator topologies rather than through
repeated placement in the algorithm lifecycle. The role provides the usual set:

- **Pipeline.** An ordered sequence such as `repair → simplification → constant
  optimization`. Ordering is semantically significant, and a stage may appear
  more than once, as in `simplification → constant optimization →
  simplification`. The composite preserves the configured order.
- **Iterated.** Feeds the refined candidate back into the same refiner for a
  configured number of rounds, mirroring
  the `IteratedEvaluator` that RF-2 removes. This is the intended way to
  express repeated refinement of one candidate.
- **Choose-one, multi, wrapping, observable, and instrumentation** variants
  follow the conventions of the other roles.

Repetition is expressed through these topologies and never by placing the same
refiner at two lifecycle points.

### Improvement-checking refiner

`ImprovementCheckingRefiner` is a wrapping refiner that adds objective-aware
retention to any inner refiner. It is an ordinary refiner: its contract is still
`Candidate → Candidate`, and it returns whichever candidate its comparison
preferred.

```text
Evaluate original candidate
→ Refine candidate with the inner refiner
→ Evaluate refined candidate
→ Return the better candidate
```

Its configuration surface is two nullable settings, following the repository
convention that a nullable setting inherits a sensible default rather than
requiring explicit configuration:

- **`Evaluator`** — the evaluator used for both comparison evaluations. A `null`
  value constructs an ordinary `ProblemEvaluator`. Because counting, limiting,
  caching, and observation are wrapper behavior rather than properties of the
  evaluator role, a default `ProblemEvaluator` is simply unwrapped and therefore
  invisible to budgets and analysis. Users who want the comparison evaluations
  counted pass the same evaluator instance the algorithm uses; see the examples
  below.
- **`Comparer`** — decides what counts as an improvement. A `null` value uses the
  problem's `ObjectiveDirections`. Supplying a comparer lets a user drive
  acceptance from one dimension of a multi-objective problem while selection
  continues to use the full objective vector.

The retention rule is a distinct decision from the comparer and must be explicit
in the implementation checkpoint. Three defensible semantics exist: retain only
on strict improvement, retain on "not worse", and, for multi-objective problems,
retain only when the refined candidate dominates the original. The first
implementation uses strict improvement for single-objective problems, so a tie
keeps the original candidate and refinement is a no-op under equality, and
dominance for multi-objective problems. A scalar improvement threshold is only
meaningful where the objective model supports it explicitly, and follows the
repository rule that configured thresholds are retained as given rather than
clamped.

Refinement failure inside the inner refiner returns the original candidate
unchanged; the improvement check never converts a failure into a worse candidate.

This refiner costs two problem evaluations, and the algorithm still evaluates the
returned candidate afterwards, so a naive configuration performs three
evaluations per refined candidate instead of one. That is one more than a
candidate-transforming evaluator would need, and it is accepted deliberately:
the cost is visible, configurable, and removable through caching, while the
transforming-evaluator alternative bought its saving by making every algorithm
responsible for using a returned candidate instead of the one it passed in. For
constant optimization the extra evaluation is small next to the Levenberg-Marquardt
solve; for cheap refiners it is not, which is what the caching composition below
addresses.

### Composition examples

These examples are the reason the improvement check belongs in a refiner taking
an evaluator rather than in a bespoke mechanism. Nothing here is a new feature;
it is all ordinary operator composition.

Evaluation accounting is decided by **reference identity**.
`ExecutionInstanceRegistry` resolves execution instances through a
`ReferenceEqualityComparer`, so the same evaluator configuration object always
resolves to one execution instance, and therefore to one counter and one cache.
Two separately constructed but structurally identical configurations resolve to
two independent instances.

**1. Default: comparison evaluations are invisible.**

```csharp
algorithm.Evaluator = new LimitEvaluator<...>(new ProblemEvaluator<...>(), maxEvaluations: 100_000);
algorithm.Refiner = new ImprovementCheckingRefiner<...>(constantOptimization);
```

Three problem evaluations per refined candidate, one of which counts against the
budget. Refinement cost is excluded from the evaluation budget.

**2. Shared evaluator: every evaluation counts.**

```csharp
var evaluator = new LimitEvaluator<...>(new ProblemEvaluator<...>(), maxEvaluations: 100_000);

algorithm.Evaluator = evaluator;
algorithm.Refiner = new ImprovementCheckingRefiner<...>(constantOptimization) { Evaluator = evaluator };
```

The same instance is handed to both, so the registry resolves one
`LimitEvaluator` execution instance holding one counter. All three evaluations
count, and the budget describes total evaluation effort including refinement.

**3. Shared cache: pay for two evaluations, not three.**

```csharp
var evaluator = new CachingEvaluator<...>(new ProblemEvaluator<...>(), keySelector);

algorithm.Evaluator = evaluator;
algorithm.Refiner = new ImprovementCheckingRefiner<...>(constantOptimization) { Evaluator = evaluator };
```

The refiner evaluates the original and the refined candidate; the algorithm's
subsequent evaluation of the returned candidate is a cache hit. Sharing one cache
requires sharing the instance, which is also what makes the hit possible.

**4. Cache and budget composed in either order.**

```csharp
new LimitEvaluator<...>(new CachingEvaluator<...>(problemEvaluator, keySelector), 100_000)  // hits count
new CachingEvaluator<...>(new LimitEvaluator<...>(problemEvaluator, 100_000), keySelector)  // hits do not count
```

Wrapper order decides whether the budget means "evaluation requests" or "actual
problem evaluations". Both are legitimate; the documentation must state which
one a preset chooses.

**5. Separate accounting for refinement.**

```csharp
algorithm.Refiner = new ImprovementCheckingRefiner<...>(constantOptimization)
{
    Evaluator = new CountingEvaluator<...>(new ProblemEvaluator<...>())
};
```

Comparison evaluations are counted and reported on their own counter, separate
from the algorithm's evaluation count, so analysis can attribute effort to
refinement.

**6. Per-round acceptance versus one final acceptance.**

```csharp
new IteratedRefiner<...>(new ImprovementCheckingRefiner<...>(constantOptimization), rounds: 5)
new ImprovementCheckingRefiner<...>(new IteratedRefiner<...>(constantOptimization, rounds: 5))
```

The first is a memetic hill-climb: each round is kept only if it improves. The
second runs five refinement rounds and accepts or rejects the final result once.
Both are useful, they are genuinely different searches, and the difference is
visible in the configuration. A candidate-transforming evaluator can only express
the second.

**7. Ordered pipeline.**

```csharp
new PipelineRefiner<...>(repair, simplification, new ImprovementCheckingRefiner<...>(constantOptimization))
```

Ordering is semantically significant, and only the stage that needs objective
information carries an evaluator.

One capability is deliberately **not** claimed here: evaluating acceptance
against a different dataset, such as a validation set, is not expressible by
configuration alone. An evaluator receives the problem as a call argument, so it
measures whatever problem the algorithm is solving. Acceptance on a subset of the
objective vector is expressible through `Comparer`; acceptance on different data
requires a separate design decision and is out of scope here.

### Evaluator contract

The evaluator returns fitness and never a candidate:

```text
Refiner:   Candidate → Candidate    (may evaluate internally through a configured evaluator)
Evaluator: Candidate → Fitness      (measures, never changes)
Algorithm: orchestrates
```

`IEvaluatorInstance.Evaluate` therefore returns `IReadOnlyList<ObjectiveVector>`,
positionally paired with its input, mirroring `IProblem.Evaluate` exactly. The
evaluator role is then describable in one sentence: the composable, instrumentable
hook around problem evaluation.

This replaces the earlier decision in which evaluators returned an authoritative
`EvaluatedCandidate<TCandidate>` so that evaluation could return a refined or
repaired replacement candidate. That decision existed to carry exactly one
workload — refinement during evaluation — and the refiner role now owns it. The
remaining candidate-transforming cases relocate rather than disappear:

| Previous transforming use | New owner |
| --- | --- |
| Constant optimization and other numeric refinement | Refiner |
| Repair | Refiner; it is already the first stage of the pipeline example above |
| Simplification and normalization | Refiner |
| `IteratedEvaluator` | `IteratedRefiner`; iterating a pure evaluator is meaningless |
| Caching, noisy-evaluation aggregation, surrogates, relative quality | Unchanged; these always wanted fitness only |

Reasons for the change:

- **A latent correctness obligation disappears.** The transforming contract
  required every algorithm to use the returned candidate rather than the one it
  passed in, and required every wrapping evaluator to preserve that. Nothing in
  the type system enforced it, and a new algorithm that dropped the returned
  candidate would fail silently.
- **`CachingEvaluator` loses a wart.** Under the transforming contract a cache hit
  returns the candidate that was cached under the key rather than the one
  supplied, which its own documentation has to warn about. With fitness-only
  results the warning is unnecessary.
- **Refinement becomes visible in algorithm configuration** instead of hidden
  inside an evaluator chain.

Accepted costs:

- **Positional pairing.** Callers pair results with inputs by position, exactly as
  `IProblem.Evaluate` already requires. The pairing happens in few places.
- **Repair discovered during evaluation.** A repair that is naturally found while
  decoding a candidate must now be performed by a refiner beforehand, which can
  mean decoding twice. If this becomes a real cost, the deferred refinement
  artifacts below are the answer, not a transforming evaluator.
- **Migration.** The evaluator instance generics appear in roughly 28 source
  files: the evaluator wrappers, the built-in algorithms, and the analysis hooks.
  The change is mechanical but not free.

The middle option of keeping `EvaluatedCandidate<TCandidate>` as the return type
while contractually forbidding transformation was considered and rejected. A type
that can express what the contract forbids invites the violation and needs a
runtime identity check to catch it. If evaluation does not transform, the
signature should say so.

### Deferred producer composition

Users may compose a refiner around candidate-producing operators, for example through a refining creator, crossover, or mutator, or adapt refinement into a probabilistic variation policy. These placements are valid but narrower: a creator wrapper affects initialization only, a crossover wrapper runs before later mutation, and a mutator wrapper does not cover initial candidates. They are not the canonical refinement integration.

HeuristicLib will not initially provide `RefiningCreator`, `RefiningCrossover`, `RefiningMutator`, generalized offspring-creation, or similar producer wrappers. Revisit those conveniences only after the explicit algorithm refiner and its composition topologies are implemented and their composition and observability behavior are understood.

### Deferred reusable refinement artifacts

A refiner may already have computed information the following evaluation repeats,
such as predictions, residuals, or a loss value. Constant optimization is the
immediate example: its least-squares objective is a full-data mean squared error.

That information is deliberately not part of the first `Refiner` contract,
because a refiner's internal objective is not generally the problem objective. It
may use a sampled row subset, a differentiable surrogate, a single objective
where the problem is multi-objective, or omit complexity penalties and linear
scaling. Reusing it unconditionally would silently change fitness.

A later increment may let a refiner return optional artifacts that an evaluator
consumes only when they are explicitly valid for the requested evaluation, or let
a refiner populate a shared evaluation cache. Both remain performance
optimizations and must not merge the refinement and evaluation responsibilities.
Refinement provenance and refinement-budget accounting are open in the same way
and are not settled by RF-0.

### Development-branch integration prerequisite

Before implementing the `Refiner` operator model, merge `dev` into the current working branch. The development branch contains operator-base improvements that may simplify or change the appropriate Refiner design. Reconcile the merge, review the resulting operator conventions, and validate the integrated code before proceeding. This is a separate integration checkpoint and must not include Refiner implementation.

### Development-branch integration outcome

`dev` is merged. The reconciliation kept `dev`'s operator structure and this
branch's evaluator semantics: `IEvaluatorInstance.Evaluate` still returns
`IReadOnlyList<EvaluatedCandidate<TCandidate>>`, while the authoring bases,
naming, and execution-instance factory come from `dev`.

That evaluator return type is superseded by the [Evaluator contract](#evaluator-contract)
decision above and is changed by RF-2. The reconciliation record is kept as
history; nothing below depends on the transforming return.

Findings that bear on the `Refiner` design:

- **Authoring model.** A role provides three paths — `Stateless<Role>`,
  `Stateful<Role, TState>`, and the explicit instance base — each in three
  arities, plus `Wrapping`/`Multi`/`Observable`/instrumentation topology pairs.
  A `Refiner` role should supply the same set rather than a single base.
- **Factory shape.** One `public abstract I<Role>Instance<...> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry)`.
  The old protected role-named factory plus explicit interface implementation is gone.
- **Single-candidate bases.** `SingleCandidate<Role>` gives the single-item
  operation its own name (`MutateCandidate`, `CreateCandidate`, `EvaluateCandidate`,
  `CrossParents`) and seals the batch method. A `SingleCandidateRefiner` should
  follow with `RefineCandidate`.
- **Concurrency.** Batching is configuration: `ExecutionConcurrency Concurrency { get; init; }`
  with per-item RNGs forked from batch position, so results do not depend on the
  concurrency setting. A refiner is a natural fit; constant optimization is
  expensive per candidate.
- **Validation placement.** Configuration invariants are validated in
  `CreateExecutionInstance` and throw `InvalidOperationException`. Note the gap
  found during this integration: stateless bases seal `CreateExecutionInstance`,
  so a stateless operator has nowhere to validate configuration and must either
  check in its role method before consuming randomness or define threshold
  semantics that need no validation. Decide which applies to `Refiner` before
  RF-2, and consider whether the stateless bases should expose a validation hook.
- **Threshold semantics.** Rates and probabilities are retained as configured,
  never clamped or rejected. If `ImprovementCheckingRefiner` grows an improvement
  threshold, it follows this rule.

Validation: Release build clean with the operator authoring analyzer at error
severity; 2001 tests pass across all four test projects; `dotnet format`
whitespace, style and analyzer checks are clean.

### Evaluator contract change outcome

RF-2 is implemented. `IEvaluatorInstance.Evaluate` returns
`IReadOnlyList<ObjectiveVector>`; `EvaluatedCandidate<TCandidate>` is retained as
the population and state pairing type, and `candidates.ToEvaluated(objectiveVectors)`
is the new batch pairing helper used at algorithm evaluation call sites.

Findings from the migration:

- **Two guards were dead code.** `RepeatingEvaluator.CandidateComparer` existed
  solely to detect repetitions disagreeing on which candidate they described, and
  `CachingEvaluator` had to document that a cache hit returns the cached candidate
  rather than the supplied one. Both disappear with a measuring evaluator.
- **`IteratedEvaluator` was removed**, together with its test. Iterating a
  measuring evaluator is meaningless; the capability returns as `IteratedRefiner`
  in RF-4, and the removed test's intent belongs there.
- **Several call sites became simpler, not more complex.** `HillClimber`,
  `PythonCorrelationAnalysis`, and `AllObjectiveVectorsAnalysis` were unpacking
  objective vectors out of evaluated candidates and now receive them directly.
  The predicted positional-pairing cost materialized in only a handful of
  algorithm and analysis call sites.
- **Two analysis hooks legitimately need pairs.** `ParetoFrontAnalysis` and
  `HyperVolumeAnalysis` store candidate/objective pairs, and now build them at the
  observer boundary. This is the intended shape: pairing happens where the pair is
  used.
- **Three refinement-behavior tests were removed** rather than adapted, because
  they tested candidate replacement during evaluation, which is deliberately no
  longer a behavior of this layer. Their coverage is owed by RF-4 and RF-7.

Validation: Release build clean across the solution; 2030 tests pass across all
four test projects; `dotnet format` whitespace and analyzer checks are clean.
`dotnet format style` reports pre-existing IDE0021 warnings in
`DataAnalysis/Statistics/Statistics.cs`, a file untouched by this change.

### Refinement checkpoints

Checkpoint states are `Pending`, `In progress`, `Awaiting review`, and `Accepted`. Implementation stops at `Awaiting review`; only explicit user acceptance advances to the next checkpoint.

| Checkpoint | Status | Deliverable |
| --- | --- | --- |
| RF-0 Design contract | Accepted | Document the `Candidate → Candidate` refiner role as the single refinement mechanism, conventional algorithm placement, the visibility rule for operator-issued evaluation, composition topologies, the improvement-checking refiner with its nullable evaluator and comparer, the fitness-only evaluator contract, the composition examples, and deferred producer wrappers and artifacts. No source code. |
| RF-1 Development-branch integration | Accepted | Merge `dev` into the working branch, reconcile and review its operator-base improvements, run appropriate validation, and stop before implementing Refiner. Done; see [Development-branch integration outcome](#development-branch-integration-outcome). |
| RF-2 Evaluator contract simplification | Awaiting review | Change `IEvaluatorInstance.Evaluate` to return `IReadOnlyList<ObjectiveVector>`, migrate the evaluator wrappers, built-in algorithms, and analysis hooks, remove `IteratedEvaluator`, and remove the candidate-substitution caveat from `CachingEvaluator`. `EvaluatedCandidate<TCandidate>` is retained as the population and state pairing type; only the evaluator stops producing it. `IEvaluatorObserver.AfterEvaluation` becomes `(IReadOnlyList<ObjectiveVector> objectiveVectors, IReadOnlyList<TCandidate> candidates, ...)`, adopting the output-first parameter order that the other seven observer interfaces already use and that evaluation was the sole exception to. |
| RF-3 Refiner operator model | Pending | Implement the general refiner configuration and execution-instance contracts, authoring bases in the three paths and three arities, `SingleCandidateRefiner.RefineCandidate`, concurrency, identity behavior, validation, and focused contract tests. |
| RF-4 Refiner composition topologies | Pending | Add the pipeline, iterated, choose-one, multi, wrapping, and observable refiner topologies, restoring iterated refinement after RF-2 removes `IteratedEvaluator`, with order-significant and repeated-stage tests. |
| RF-5 Explicit algorithm integration | Pending | Add configurable refinement to applicable built-in algorithms after creation and final variation but before evaluation, without re-refining carried evaluated candidates. |
| RF-6 Constant-optimization refiner | Pending | Adapt symbolic-regression constant optimization to the general refiner role and define its failure behavior without adding problem-objective retention. |
| RF-7 Improvement-checking refiner | Pending | Implement the wrapping refiner with nullable `Evaluator` and `Comparer`, the strict-improvement and dominance retention rules, and original-on-failure behavior. |
| RF-8 Integration hardening | Pending | Verify batching, refiner/evaluator composition, objective directions, equality and threshold behavior, failure and cancellation, iterated and pipeline refinement, observability, and each composition example including shared-instance accounting and cache/limit wrapper order. |

## Stage 4: Symbolic-Regression Constant Optimization

### Expression-lowering design

The first adapter increment lowers an `ExpressionTree` directly into the internal AD program without passing through `CompiledExpression`. Ordinary compiled expressions deliberately erase distinctions that constant optimization needs: fixed and evolvable constants share one opcode, constants may be folded, repeated variables are interned, and tree-occurrence identity is not retained.

Use the existing `Symbol.Emit` and `IExpressionEmitter` semantic boundary with a differentiation-specific emitter. This preserves built-in and external macro behavior without introducing another symbol-type switch. The emitter maintains an AD-value stack and maps the initial supported opcodes to `Builder` operations. A macro is supported when every opcode it emits is supported; an unsupported emitted opcode produces an `ExpressionCompilationFailure` attributed to the current expression point and symbol before execution begins.

The provisional internal result is a `DifferentiableExpression` produced by a `DifferentiableExpressionCompiler`. It retains:

- the source `ExpressionTree`;
- the immutable AD `Program`;
- variable names in AD input order;
- parameter bindings in AD parameter order.

Each parameter binding retains its `ExpressionPoint`, originating `EvolvableConstantSymbol`, and initial value. Parameter order follows the first semantic emission of each logical occurrence. Rebuilding receives values in parameter order and performs one `ExpressionTree.ReplaceMany` operation with new `NumericConstantExpressionNode` instances using the originating symbols.

Occurrence identity follows the tree path represented by `ExpressionPoint`, not `ExpressionNode` object identity. If the same immutable node object occurs at two different paths, those paths are separate logical occurrences and lowering creates two independent parameters. Reference sharing never implicitly ties parameter values. Within one symbol emission, emitted children are cached by child index: if a macro emits the same logical child more than once, lowering reuses one AD `Value`, preserving one parameter identity and explicit DAG sharing so reverse contributions accumulate.

Variable names are interned by ordinal name and follow first semantic-emission order. Repeated occurrences reuse one AD input handle. Fixed constants and constants emitted as part of a macro remain AD constants. Evolvable constants become parameters only while their own expression occurrence is being emitted. V1 performs no differentiation-specific constant folding or common-subexpression elimination.

Pure expression lowering does not reference `DataFrame` or regression types. A later adapter checkpoint binds the retained variable names to `double` series in a data frame and reports missing variables or series whose element type is not `double` as structured binding failures. Invalid symbol-emitter behavior, inconsistent stacks, foreign expression points, and replacement shape mismatches remain programming errors and throw early.

### Expression-lowering checkpoints

| Checkpoint | Status | Deliverable |
| --- | --- | --- |
| EL-0 Design contract | Accepted | Document the semantic emitter boundary, occurrence rules, retained lowering result, supported-operation policy, rebuilding contract, failures, and checkpoints. No source code. |
| EL-1 Semantic lowering | Accepted | Implement the differentiation-specific emitter, opcode mapping, variable interning, macro expansion, child reuse, program construction, and structured unsupported-operation results. |
| EL-2 Parameter bindings and rebuilding | Accepted | Discover evolvable constant occurrences, preserve parameter order and originating symbols, distinguish structurally shared occurrences, and rebuild through one `ReplaceMany` call. |
| EL-3 Data binding | Accepted | Bind retained variable names to `double` data-frame series in AD input order and report structured missing or incompatible-column failures. |
| EL-4 Verification | Accepted | Compare expression and AD evaluation, cover supported operations and macros, verify unsupported models fail before execution, and test occurrence-safe rebuilding without source mutation. |
| EL-5 Hardening | Accepted | Return unsupported-operation failures without exception-based control flow, enforce retained mapping invariants, and verify non-finite interpreter parity. |

As with the AD checkpoints, implementation stops at `Awaiting review`, and only one expression-lowering checkpoint is implemented per request unless explicitly expanded.

EL-1 reuses `Symbol.Emit` through a differentiation-specific stack emitter. Variables are interned by ordinal name, and each emission frame caches children by child index so repeated macro emission reuses one AD value without merging equal or reference-shared nodes at different expression points. `TryCompile` returns the differentiable expression on success or an `ExpressionCompilationFailure` containing its emitting expression point, derived symbol, and unsupported operation; null-state annotations make the two outcomes explicit to callers. During this semantic checkpoint numeric constants remain AD constants; EL-2 promotes evolvable constant occurrences to parameters and adds the retained bindings needed for rebuilding.

EL-2 maps an evolvable numeric constant to an AD parameter only while emitting that constant's own expression point. Parameter order and bindings therefore follow first semantic emission. A binding stores only its expression point; its originating symbol and initial value are derived from the immutable node. Rebuilding validates the parameter count, creates occurrence-specific replacement nodes with the originating symbols, and applies them through one `ExpressionTree.ReplaceMany` call. Reference-shared nodes at different paths remain separate parameters, while a macro's repeated emission of one child reuses one parameter and accumulates all derivative contributions.

Macro expansion exists only in the compiled AD program. If an evolvable constant occurs in a macro child or descendant, rebuilding replaces that occurrence and preserves the surrounding macro symbol in the returned expression tree. Constants emitted directly by the macro are fixed parts of its semantics because they have no expression occurrence or parameter binding. V1 does not optimize numeric state embedded directly in a macro symbol; a tunable macro value must be represented as an evolvable constant child.

EL-3 isolates the `DataFrame` dependency in a `DifferentiableExpression` extension. The compiler, retained differentiable expression, and numerical AD engine remain independent of data-analysis types. The extension resolves variables in retained AD input order, passes each `Series<double>.Values` memory directly to the AD execution without copying numerical data, and returns a structured failure for the first missing or incompatible series. Input-free expressions are bound to the data-frame row count so their scalar value and parameter derivatives are repeated for every row.

EL-4 compares every initially supported expression operation and the built-in sigmoid macro with ordinary expression-interpreter values and central finite differences over immutable parameter rebuilding. It verifies the parameter-major Jacobian layout, source-expression stability, and structured compilation failure for every currently unsupported built-in operation. Existing focused tests retain the occurrence-specific rebuilding, macro-child reuse, variable ordering, and binding-failure cases.

EL-5 makes unsupported operations a genuinely non-throwing `TryCompile` outcome. The emitter records the first `ExpressionCompilationFailure` and substitutes one builder-owned placeholder value to preserve stack shape while the symbol finishes emitting; malformed emitters and exceptions thrown by symbols still propagate. `DifferentiableExpression` rejects mismatches between program inputs and variable names or between program parameters and parameter bindings. Division-by-zero and invalid-log tests verify that non-finite values retain ordinary interpreter semantics.

### Expression adapter responsibilities

The symbolic-expression adapter owns:

- discovering `NumericConstantExpressionNode` occurrences whose symbol is `EvolvableConstantSymbol`;
- assigning one parameter identity per logical tree occurrence, even when node objects are structurally shared;
- preserving fixed constants as constants;
- compiling expression semantics into the general differentiable representation;
- binding named variables to regression input columns;
- preserving occurrence identity when a macro emits the same logical child more than once;
- rebuilding returned parameter values through one immutable `ExpressionTree.ReplaceMany` operation;
- preserving each node's originating symbol.

The adapter must not implement LM, Cholesky factorization, generic reverse mode, or memetic selection.

Lower directly from `ExpressionTree` into the immutable differentiation program where practical. A separate mutable AD tree is not required: use a mutable lowering builder, emit the compact program, and return parameter bindings that retain the expression occurrence identity needed for rebuilding. This keeps expression immutability separate from execution-oriented mutable state.

### Expression differentiation

Decide whether ordinary `CompiledExpression` and differentiable compilation share:

- one common semantic operation description;
- a common lowering pipeline with different execution backends;
- or independent compiled representations with shared symbol emission tests.

Do not change public `CompiledExpression`, `Instruction`, or `OpCode` merely to fit autodiff. The value interpreter and differentiation engine have different execution needs. Share semantic lowering where it reduces duplication without forcing one storage model onto both.

All current built-in operations must either:

- provide tested derivative semantics; or
- cause a structured unsupported-model result before optimization starts.

Macro lowering must be tested independently from the optimizer.

### Full-data objective and rebuilding

The first implementation uses every training row. Observation sampling is deferred. When sampling is introduced later, it belongs to the symbolic-regression constant-optimization adapter or a later refiner, not the numerical solver.

The first symbolic integration defines:

- least-squares residuals from raw expression predictions and raw training targets;
- raw full-data mean squared error as the numerical optimization measure;
- unconditional rebuilding with the parameter vector returned by a successful LM call;
- ordinary propagation of non-finite optimized parameter values and mean squared error.

The constant-optimization component does not retain a best finite point, compare initial and final loss, consult the problem's configured `IRegressionMetric`, or decide whether the fitted expression should be retained. A later refiner owns retention and improvement checking around this fitting capability.

Evaluation-time linear scaling is outside the first constant-optimization objective. The optimizer neither differentiates through fitted scaling coefficients nor injects scaling terms into the expression. Normal symbolic-regression evaluation may fit or apply linear scaling after constant optimization exactly as it otherwise would. Explicit root-level scaling parameters or a jointly scaling-aware objective remain future design options.

Initially:

- optimize evolvable constants only;
- leave fixed constants unchanged;
- use least-squares residuals for LM;
- require direct full-data optimization;
- defer bounds, row sampling, sample weights, best-point retention, acceptance policy, linear-scaling integration, and variable-weight optimization.

### Initial API boundary

Do not begin with a regression-problem-shaped convenience facade and work inward.

Begin with an internal direct constant-optimization entry point so the behavioral contract and performance can stabilize. Design a public symbolic-regression facade afterward. It should expose symbolic intent while allowing advanced users to select a numerical algorithm once multiple algorithms are genuinely supported. A later refiner may consume this capability, but its API is outside the current design. The final constant-optimization API must avoid exposing tape, Jacobian, row-selection, or solver-workspace details to ordinary symbolic-regression users.

The capability retains the canonical symbolic-regression name constant optimization. Its first internal component is `ConstantOptimizer`, exposing `Optimize` and `TryOptimize`. Parameter optimization describes the numerical mechanism by which evolvable constant occurrences are fitted. V1 optimizes only occurrences represented by `ParameterBinding`; fixed constants remain unchanged.

Its easy-to-use surface provides both throwing and non-throwing forms:

```csharp
internal static ExpressionTree Optimize(
    ExpressionTree expression,
    RegressionData data,
    int maximumIterations,
    CancellationToken cancellationToken = default);

internal static bool TryOptimize(
    ExpressionTree expression,
    RegressionData data,
    int maximumIterations,
    [NotNullWhen(true)] out ExpressionTree? optimizedExpression,
    CancellationToken cancellationToken = default);

internal static bool TryOptimize(
    ExpressionTree expression,
    RegressionData data,
    int maximumIterations,
    [NotNullWhen(true)] out ExpressionTree? optimizedExpression,
    [NotNullWhen(false)] out ConstantOptimizationFailure? failure,
    CancellationToken cancellationToken = default);
```

The convenience `TryOptimize` overload collapses compilation, variable-binding, and numerical-solver failures to `false`. Its detailed overload additionally returns a small closed `ConstantOptimizationFailure` hierarchy that retains the owning lower-level failure. The expression compiler, data-binding adapter, and LM adapter follow the same convention by offering convenience and detailed `Try` overloads. `Optimize` translates compilation failure to an informative `NotSupportedException`, binding failure to `ArgumentException`, and numerical-solver failure to `InvalidOperationException`. Programming errors, invalid arguments, and cancellation continue to propagate from all forms; cancellation is represented by `OperationCanceledException` rather than `false`. Constant optimization requires non-empty regression data and a non-negative maximum iteration count. `RegressionData` already owns input/target row-count consistency, and nonnullable annotations are compile-time contracts rather than reasons for redundant runtime null checks.

A successful LM result is always rebuilt through `DifferentiableExpression.WithParameterValues`, including non-finite parameter values. Input-free expressions with evolvable constants are fitted against every target row by repeating their scalar value and parameter derivatives across the data row count. Zero maximum iterations and expressions without evolvable constant occurrences succeed immediately and return the original `ExpressionTree` instance after validating the general call arguments and cancellation but before differentiable compilation, data binding, or LM. These identity shortcuts also allow unsupported operations or unbound variables that are irrelevant when no optimization is requested or no parameter can change.

The component lives under `HEAL.HeuristicLib.DataAnalysis.Regression` because it couples a symbolic expression to supervised regression data. It depends on `ExpressionTree`, `RegressionData`, expression lowering and binding, and the internal numerical optimizer. It does not depend on `SymbolicRegressionProblem`, configured regression metrics, linear scaling, HeuristicLib algorithms or operators, evaluation accounting, retention, or acceptance policy.

### Constant-optimization component checkpoints

Checkpoint states are `Pending`, `In progress`, `Awaiting review`, and `Accepted`. Implementation stops at `Awaiting review`; only explicit user acceptance advances to the next checkpoint.

| Checkpoint | Status | Deliverable |
| --- | --- | --- |
| CO-0 Component contract | Accepted | Document terminology, API shape, successful rebuilding semantics, behavior without evolvable constants, high-level failure behavior, cancellation, dependencies, and explicit exclusion of retention and acceptance. No source code. |
| CO-1 Successful bridge | Accepted | Implement compilation, data binding, initial-value extraction, LM invocation, unconditional immutable rebuilding, and one successful fitting test. |
| CO-2 Failure and identity behavior | Accepted | Verify convenience and detailed non-throwing failures, specific throwing behavior, cancellation propagation, lower-level failure retention, and identity shortcuts for zero iterations or no evolvable constants. |
| CO-3 Semantic hardening | Accepted | Verify non-finite parameter propagation, fixed constants, occurrence identity, shared nodes, macro emission, source immutability, repeated calls, and independence from problem metrics, linear scaling, algorithms, operators, retention, and acceptance. |

CO-3 verifies the complete rebuilding semantics at the direct component boundary. Reference-shared constant nodes at different expression paths are fitted as independent parameters; macro expansion remains compilation-only and the rebuilt expression preserves the macro; fixed constants and the source expression remain unchanged; repeated calls are independent; and successful non-finite results and parameter values are not sanitized. A production dependency audit confirms that `ConstantOptimizer` depends only on symbolic expressions, `RegressionData`, expression lowering and binding, and numerical optimization. It has no dependency on symbolic-regression problems, configured metrics, linear scaling, HeuristicLib algorithms or operators, retention, or acceptance policy.

### Direct vertical-slice artifact

Provide:

- a direct test fixture optimizing constants in a fixed expression;
- retained compiled/differentiable model reuse where lifecycle permits;
- tests for fixed constants, occurrence identity, structurally shared nodes, macro re-emission, unsupported operations, invalid points, identity behavior without evolvable constants, non-finite propagation, and immutable replacement;
- analytic and finite-difference correctness tests, plus behavioral comparison with the maintained legacy implementation where applicable;
- end-to-end performance measurements of the maintained constant-optimization pipeline.

Do not require identical optimized parameter vectors, iteration counts, or termination labels when different implementations produce behaviorally equivalent predictions and loss. Intentional design differences, including immutable rebuilding and exclusion of legacy variable-weight optimization, must be asserted explicitly rather than hidden by broad parity tolerances.

## Stage 5: Performance Hardening

Performance work is continuous, but final specialization happens only after the layer boundaries are stable.

### Benchmarks

Keep focused benchmark suites for:

- differentiation compilation;
- value-only evaluation;
- value-plus-gradient;
- streamed Jacobian observation batches and normal-equation accumulation;
- numerical solver iterations excluding model compilation;
- complete numerical optimization including setup;
- symbolic expression lowering and immutable replacement;
- evaluator-level memetic workloads over populations;
- retained workspace and compiled-model reuse.

Cover representative row counts, expression sizes, and parameter counts. Separate setup cost from steady-state execution and also measure the complete pipeline.

The final benchmark matrix also:

- compares AD batch capacities from `64` through `4096` against the provisional `256` default;
- compares SIMD-enabled execution with a benchmark-only scalar baseline;
- compares row-by-row and batched execution where their contracts can be held equivalent;
- profiles retained-primal derivative recomputation before considering selective local-partial caching;
- quantifies the end-to-end impact of replacing specialized scalar/vector paths with `Tensor<T>` broadcasting without making it part of V1 unless measurement justifies it;
- compares the current `ArrayPool<double>`-backed `Execution` with a benchmark-only execution-owned-array variant across repeated construction, reuse, and complete constant-optimization workloads; report throughput, allocated bytes, and garbage-collection pressure, including large-object-heap-sized workspaces;
- uses real lowered symbolic expressions and constant-optimization workloads in addition to focused synthetic kernels.

For the first MathNet backend, report at least four costs separately:

1. AD model-value and derivative evaluation;
2. Jacobian orientation, copying, and MathNet storage preparation;
3. MathNet solver iterations;
4. expression lowering, optimization, and immutable replacement as one end-to-end operation.

This evidence decides whether direct MathNet storage types, a different backend, or a specialized in-house LM implementation is warranted. Backend abstraction must not be expanded without a measured replacement need.

### Allocation and concurrency

- No per-operation allocation in differentiation execution.
- Repeated numerical iterations reuse buffers after workspace acquisition.
- Execution instances own mutable workspaces.
- Reusable configurations and compiled models remain immutable.
- Parallel candidate optimization uses isolated execution state.
- Caches require an explicit owner and lifecycle; do not introduce global weak tables or registries.
- `ArrayPool<double>` and the resulting `IDisposable` execution lifecycle are provisional until the final benchmark demonstrates a material benefit over ordinary execution-owned arrays. If the benefit is not material, prefer ordinary arrays and remove disposal complexity before the public-API review.

### Correctness before specialization

Every optimized kernel must have:

- a simple reference implementation or finite-difference oracle;
- invalid-value and edge-case tests;
- cross-batch consistency tests;
- benchmark evidence before replacing clearer code.

## Migration And Retirement

Replacement proceeds by behavior, not by file name:

1. establish general differentiation correctness with analytic and finite-difference tests;
2. establish MathNet-backed LM behavior on ordinary least-squares problems;
3. establish symbolic-expression lowering parity;
4. establish unconditional immutable replacement from successful LM results, including non-finite values;
5. compare predictions and raw MSE with the maintained legacy implementation where applicable;
6. measure representative end-to-end workloads and decide whether MathNet remains the backend;
7. design evaluator or memetic integration only after the direct path is stable;
8. remove old AutoDiff or MathNet dependencies only when no maintained library feature uses them.

Do not delete tests merely because the implementation changes. Adapt behavioral tests to the owning replacement layer.

## Decision Gates

The first vertical slice has settled these decisions:

1. the AD engine has internal types and a direct `ExpressionTree` lowering adapter;
2. lowering uses a mutable builder/compiler, produces an immutable reusable program, and evaluates through mutable run-scoped workspaces;
3. spans and contiguous buffers are preferred internally, while direct MathNet storage is allowed at the adapter boundary when it avoids measured cost;
4. MathNet LM is the first backend, with no broad solver-plugin architecture;
5. the first objective is raw full-data least squares over evolvable constant occurrences only;
6. constant optimization always rebuilds a successful LM result; best-point retention and acceptance are separate later policies;
7. linear scaling remains an evaluation concern outside constant optimization;
8. output parity means predictions and loss within tolerance, not identical parameters or solver traces;
9. generic numerical algorithms and memetic composition are later generalizations, not delivery gates.

Remaining decisions should be made from implementation evidence:

1. the precise program instruction layout, derivative buffer layout, and Jacobian batch size;
2. which built-in expression operations are supported in the first increment and their structured failure details;
3. the exact MathNet API and storage shape with the lowest verified adapter overhead;
4. stopping defaults and later retention or acceptance policy;
5. whether MathNet performance justifies retention or replacement;
6. the eventual public facade and generic local-improvement contract;
7. whether pooled AD workspaces justify the `IDisposable` execution contract over ordinary execution-owned arrays.

## Incremental Delivery Sequence

| Increment | Usable result | Must not contain |
| --- | --- | --- |
| 0. Organization | Reviewed symbolic-regression ownership and namespace map | New solver or autodiff API |
| 1. AD operation model | Internal operation semantics plus mutable builder/compiler | Regression objectives, public authoring API |
| 2. Compiled AD execution | Immutable reusable program, mutable workspace, value/gradient/Jacobian evaluation | Residuals, targets, `ExpressionTree`, MathNet solver policy |
| 3. AD verification | Analytic, finite-difference, invalid-domain, and allocation tests for each operation | Solver-specific derivative changes |
| 4. Expression lowering | Direct lowering with occurrence-safe optimizable-constant bindings | Optimization, mutation of the source tree |
| 5. MathNet LM adapter | Ordinary least-squares problem solved through a thin measured adapter | Generic optimizer hierarchy, memetic policy |
| 6. Direct constant optimization | Full-data LM run, simple high-level failure behavior, identity behavior without evolvable constants, and rebuilding after an LM run | Sampling, retention, acceptance, linear-scaling internals |
| 7. Immutable rebuilding | One `ReplaceMany` operation produces the optimized expression | In-place mutation, fixed-constant replacement |
| 8. Behavioral comparison | Predictions and raw MSE satisfy analytic expectations and match maintained legacy behavior where applicable | Exact parameter-vector or solver-trace equivalence |
| 9. Development-branch integration | Current `dev` operator-base improvements integrated, reviewed, and validated | Refiner implementation |
| 10. Evaluator contract simplification | Fitness-only evaluator results, migrated wrappers, algorithms, and analysis hooks | Candidate-transforming evaluation of any kind |
| 11. Refiner operator | Candidate-to-candidate refinement, composition topologies, plus explicit placement in applicable algorithms | Objective evaluation or retention inside the general refiner contract |
| 12. Improvement-checking refiner | Objective-aware retention as a composable wrapping refiner with configurable evaluator and comparer | A second refinement integration mechanism, or acceptance hidden from configuration |
| 13. Producer refinement composition | Deferred creator, crossover, mutator, and offspring-production conveniences after canonical mechanisms are established | Producer wrappers treated as the primary refinement integration |
| 14. Performance decision | AD, adapter, solver, refinement, evaluator, and complete pipeline benchmarked | Unmeasured backend abstraction or specialization |
| 15. Later generalization | Public APIs, other optimizers, sampling, and additional memetic composition as justified | Changes made only for hypothetical reuse |

Each increment requires:

- a small internal or public API with one clear owner;
- focused unit tests;
- an executable API usage spec or example when public;
- a named benchmark case when it introduces hot-path computation; measurement and specialization may remain deferred to the performance-decision increment unless performance blocks functional progress;
- updated documentation before the next increment builds upon it.

## Explicit Non-Goals For The First Replacement

- generic numeric types;
- Hessians and higher-order automatic differentiation;
- GPU execution;
- distributed differentiation;
- arbitrary tensor broadcasting;
- parameter bounds and constrained optimization;
- validation-set-driven local-search acceptance;
- sample weights;
- grammar-guided symbolic regression;
- vectorial symbolic regression;
- interval arithmetic;
- variable-weight optimization;
- a public automatic-differentiation authoring API;
- L-BFGS or a baseline gradient optimizer;
- a general solver-backend plugin architecture;
- producer-specific refinement wrappers before the canonical refiner and refinement-evaluator mechanisms are established;
- acceptance through the problem's configured regression metric;
- differentiating through evaluation-time linear scaling or injecting root scaling parameters;
- replacing every existing HeuristicLib optimization algorithm with a gradient-based one.

These remain possible extensions. The first replacement should establish boundaries that can accommodate them without implementing them prematurely.

## First Vertical-Slice Completion Criteria

The first constant-optimization implementation is complete when:

- the internal AD program evaluates values, gradients, and the required Jacobian shape without per-operation allocation;
- analytic and finite-difference tests cover every supported operation and its invalid-domain behavior;
- `ExpressionTree` lowering preserves optimizable occurrence identity, fixed constants, macro semantics, and originating symbols;
- MathNet LM can optimize all training rows through a measured adapter;
- compilation and binding failures, unsupported models, invalid numerical outcomes, and solver outcomes are structured and tested at their owning lower layers, while `ConstantOptimizer` deliberately exposes only success or failure;
- the input tree remains unchanged and every successful LM parameter vector is applied through one `ReplaceMany` call;
- non-finite optimized values propagate into the rebuilt expression without an implicit acceptance safeguard;
- predictions and raw MSE match retained behavior within documented tolerances where the behavioral contracts overlap;
- benchmarks isolate AD, adapter, solver, and end-to-end costs and support an explicit MathNet retention or replacement decision;
- the direct capability is documented well enough to design its eventual public facade without exposing AD internals.

The broader redesign is complete later when justified public numerical-optimization APIs, explicit refiner integration, the refinement evaluator, additional solvers, and legacy retirement each have an explicit outcome. They are deliberately not conditions for completing the first vertical slice.
