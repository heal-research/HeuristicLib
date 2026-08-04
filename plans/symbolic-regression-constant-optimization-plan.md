# Symbolic Regression Constant Optimization Plan

## Status

This is a design and implementation plan, not a commitment to the API shape of the current prototype.

The existing `NumericParameterExpression*` and `NumericParameterOptimization*` implementation is retained as an executable reference. It may provide algorithms, tests, performance techniques, and failure cases for the replacement, but its current naming, namespaces, public API, and responsibility boundaries are not the target design.

This plan refines Stage 3.1 of [symbolic-regression-redesign-plan.md](symbolic-regression-redesign-plan.md). The redesign plan should be updated to point here after this plan has been reviewed.

### Settled direction for the first implementation

The first implementation is a focused vertical slice for symbolic-regression constant optimization. It does not wait for a public automatic-differentiation API, a general numerical-optimization framework, L-BFGS, or generic memetic composition.

The sequence is:

1. implement an internal automatic-differentiation engine with its own execution-oriented representation;
2. lower an `ExpressionTree` into that representation while recording occurrence-safe bindings for optimizable constants;
3. expose residuals and parameter derivatives to MathNet's Levenberg-Marquardt implementation through a thin, measured adapter;
4. optimize all training rows against raw targets using least-squares mean squared error;
5. retain the best finite parameter point and reject a numerically worse result using the same raw full-data mean squared error;
6. rebuild the immutable expression through one `ExpressionTree.ReplaceMany` operation;
7. verify output behavior against the retained implementations and measure the complete pipeline before generalizing it.

MathNet is the first solver backend, not a permanent architectural commitment. The implementation should keep the backend boundary small enough to replace, but must not introduce dependency injection, reflection, virtual dispatch in hot loops, or a generalized plugin architecture merely to permit a hypothetical future solver. Direct use of MathNet storage types is acceptable where measurement shows that it avoids meaningful conversion or allocation cost.

The first automatic-differentiation API is internal. Public authoring APIs, generic local-improvement composition, additional numerical optimizers, sampling, scaling-aware optimization, and evaluator integration follow only after the direct constant-optimization path is correct and fast.

## Problem Statement

Constant optimization in symbolic regression is not one isolated solver call. The complete design combines four distinct capabilities:

1. differentiating a parameterized numeric model;
2. running a numerical optimization algorithm over that model;
3. composing local improvement with another search algorithm to form a memetic algorithm;
4. adapting an `ExpressionTree` and a symbolic-regression problem to those general capabilities.

The current prototype implements all four concerns together under `Problems/DataAnalysis/Regression`. This creates several problems:

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

Do not use a prefix such as `NumericParameterExpression` on every type. Types should be named after one clear responsibility and placed in a namespace that supplies the broader context.

## Current Prototype As Reference

The prototype is archived under `references/symbolic-regression/constant-optimization-prototype`.

Keep these files until their replacement has behavior and performance parity:

- `NumericParameterExpressionCompiler.cs`
- `NumericParameterExpressionProgram.cs`
- `NumericParameterExpressionEvaluator.cs`
- `OptimizationRowSelection.cs`
- `NumericParameterOptimizer.cs`
- `NumericParameterOptimizationOptions.cs`
- `NumericParameterOptimizationResult.cs`
- `NumericParameterOptimizerTests.cs`
- `HeuristicLib.NumericParameterOptimizationBenchmarks`

Also retain the legacy implementation as a behavioral reference:

- `SymbolicRegressionParameterOptimization.cs`
- `TreeToAutoDiffTermConverter.cs`
- the existing AutoDiff and MathNet dependencies while their replacement value is being assessed

Rules for the reference implementation:

- do not expand its public API;
- do not make it the architectural foundation of new code;
- keep its focused tests and benchmarks runnable while replacement layers are introduced;
- extract or reuse code only when the destination abstraction has already been designed;
- remove or archive it only after replacement tests compare behavior and benchmarks compare performance.

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

- a snapshot of the legacy mutable symbolic-expression system is retained under
  `references/symbolic-regression/legacy-mutable-system`;
- the first numeric-parameter-optimization prototype is archived under `references/symbolic-regression/constant-optimization-prototype`;
- maintained projects do not compile either reference snapshot, while
  still-required mutable components remain active in their established domain
  folders until their consumers migrate;
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
- legacy parameter optimization;
- reference parameter-optimization prototype.

Record which types are active, transitional, reference-only, or obsolete. Do not infer ownership from the current folder alone.

### Organization goals

- The immutable `ExpressionTree`, symbols, nodes, points, drafts, compilation, and interpretation form one coherent symbolic-expression area.
- Regression data and metrics remain data-analysis concerns rather than expression concerns.
- A symbolic-regression problem composes expression search, regression data, and evaluation without owning generic differentiation or numerical solvers.
- Old and new systems must be distinguishable by namespace and documentation while both remain in the repository.
- Folder names and namespaces should express responsibility without repeating `SymbolicExpression` on every local type.
- Reference prototypes should be clearly marked as transitional without hiding them in production-looking namespaces.

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
early. The later solver adapter owns finite-value validation.

### Execution ownership and memory

AD-2 introduces `Program.CreateExecution()` for input-free scalar programs. It
owns one pooled scalar-primal buffer and produces exactly one result.
AD-3 adds the bound-input overload described below together with batched vector
storage; the scalar convenience remains available.

`Program.CreateExecution(inputColumns, rowCount, batchCapacity)`
creates an `Execution`. The provisional default capacity is `256`.

- Input bindings are copied as `ReadOnlyMemory<double>` descriptors; numerical
  input data is not copied.
- A parameter-only scalar function uses no input columns and produces one result.
- The execution rents scalar-primal, vector-primal, and batch-adjoint buffers from
  `ArrayPool<double>` and returns them on disposal.
- The execution is mutable, non-reentrant, and not thread-safe. Concurrent runs
  create separate executions over the same program.
- Outputs and the final Jacobian are caller-owned.
- Bound input columns may overlap one another because they are read-only. Output and Jacobian buffers must not overlap parameters, bound input columns, or one another; invalid overlap throws before evaluation.
- Targets and residuals do not belong to automatic differentiation and are
  owned later by the least-squares adapter.

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

A preliminary AVX2 kernel comparison during AD-5 found explicit `Vector<double>` loops materially faster at the provisional batch capacity of `256` for trigonometric derivative accumulation, `tan`/`tanh` output-based derivatives, and vector-valued division derivatives. Those rules use a vector loop followed by a scalar tail. Increment 9 must confirm the gain end to end and across representative batch sizes rather than treating this machine-specific kernel result as final tuning evidence.

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

AD-6 ends after AD-6b. The formerly planned AD-6c through AD-6e work is postponed to Increment 9, after expression lowering, the solver adapter, constant optimization, immutable rebuilding, and behavioral comparison provide representative end-to-end workloads. The provisional capacity remains `256`; no performance specialization is accepted from isolated AD measurements alone.

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

Keep one small internal contract around the information Levenberg-Marquardt mathematically requires:

- the current parameter vector;
- residual values over an observation batch;
- parameter derivatives in the orientation required to provide the solver's Jacobian efficiently;
- explicit invalid-model, invalid-point, cancellation, and termination results.

Do not force least-squares through a generic scalar-objective contract that loses residual structure. The first contract does not need to accommodate unrelated gradient algorithms.

A future public numerical-optimization area may introduce separate value-only, value-and-gradient, and least-squares contracts. That generalization is informed by the working constant-optimization slice rather than required before it.

### Initial execution model

The first implementation is a bounded, internal, one-shot least-squares run with:

- an immutable configuration where reuse is valuable;
- run-scoped execution state and workspaces;
- explicit stopping criteria and budgets;
- a compact structured result;
- cancellation support;
- deterministic behavior when randomness is used;
- structured termination and evaluation counters;
- no hidden mutation of reusable configurations.

The design should leave room for numerical optimizers to become first-class HeuristicLib algorithms later. That algorithm model is not a gate for the first constant optimizer.

### Solver sequence

Implement in this order:

1. a thin adapter from the internal residual/Jacobian evaluation to MathNet's Levenberg-Marquardt implementation;
2. the direct symbolic-regression constant-optimization vertical slice;
3. focused benchmarks separating differentiation, Jacobian preparation, adapter overhead, solver work, and complete-pipeline cost;
4. only then decide whether to retain MathNet, implement a specialized LM runner, or add other optimizers such as L-BFGS.

MathNet is the first backend only. Keep the boundary narrow and concrete, but avoid an abstraction hierarchy whose only purpose is hypothetical backend interchangeability. If direct MathNet arrays, vectors, matrices, or column-major storage remove measurable copies or allocations, prefer them at the adapter boundary.

### Linear algebra

Do not implement or bury factorization code inside the symbolic-regression adapter while MathNet is the backend.

Measure:

- MathNet solver APIs and their required storage layout;
- whether a column-major dense Jacobian can be backed directly by parameter-major derivative storage;
- whether MathNet can consume the caller's storage without a copy for the chosen API;
- a focused span/array Cholesky implementation for small dense parameter systems;
- possible platform tensor or BLAS-backed operations for larger systems.

The last two alternatives are follow-up comparisons if MathNet's measured cost is material. Choose any replacement by correctness, failure reporting, allocation, and representative parameter sizes. If a custom kernel is retained later, give it focused numerical tests and a clear numerics owner.

### Levenberg-Marquardt requirements

The MathNet adapter and constant-optimization orchestration must make the following semantics explicit, even when MathNet implements the underlying policy:

- residual and Jacobian consumption and orientation;
- residual and mean-squared-error normalization conventions;
- option mapping to MathNet;
- function, gradient/Jacobian, and row-evaluation accounting;
- stopping criteria;
- invalid trial behavior;
- best-finite-result behavior at iteration limits.

Solver mechanics belong to MathNet and the adapter. Expression discovery, rebuilding, raw full-data acceptance, and future observation sampling do not.

### Second usable artifact

Provide focused tests that optimize ordinary least-squares problems before the expression adapter is connected:

- linear and nonlinear least squares with LM;
- analytic derivatives checked against automatically differentiated derivatives;
- invalid points, no-progress termination, iteration limits, and best-finite-result handling;
- adapter allocation and conversion cost.

The artifact is complete when the AD engine and MathNet adapter can solve an ordinary least-squares problem without referencing symbolic regression. A general public numerical-optimization API is not required.

## Stage 3: General Local Improvement And Memetic Composition (Deferred)

This stage is not a prerequisite for direct symbolic-regression constant optimization. Revisit it after the direct vertical slice establishes the correct constant-optimization contract, costs, and result semantics.

### Design question

Constant optimization is local search initialized from an existing candidate. The framework must decide how that work composes with population algorithms.

Evaluate three forms:

1. **Evaluator decoration:** improve selected candidates before final evaluation and return the authoritative improved evaluated candidate.
2. **Dedicated local-improvement operator:** transform a candidate using problem evaluation semantics, then let an evaluator score the result.
3. **Meta-algorithm composition:** coordinate a global algorithm and nested local-search algorithms explicitly.

These forms are not necessarily mutually exclusive. The design must identify one canonical low-level contract and define higher-level facades in terms of it.

### Required policies

The composition layer, not LM and not automatic differentiation, owns:

- which candidates receive local improvement;
- probability or population fraction;
- elite-only, random, all-candidate, or scheduled selection;
- local-search budget per candidate and global budget accounting;
- acceptance of equal, improved, or occasionally worse local results;
- whether local-search observations are included in algorithm analysis;
- random-source forking and reproducibility;
- parallel execution and workspace isolation;
- reuse of shared observation samples within one evaluation batch.

This directly addresses the earlier uncertainty around putting a population fraction into solver options. Solver options describe one numerical optimization run. Memetic composition decides which candidates receive such a run.

### Evaluator invariant

If local improvement is evaluator-driven, the evaluator must return the candidate it actually evaluated. It must never attach the improved objective vector to the original candidate.

The input candidate remains immutable. An unsuccessful or rejected improvement returns the original candidate and its objective vector.

### Later usable artifact

Demonstrate the composition with a non-symbolic candidate first:

- a real-vector population algorithm;
- a gradient-based local optimizer;
- configurable all-candidate and probabilistic local improvement;
- deterministic random behavior;
- accounting that distinguishes global evaluations from local-search work.

This proves the memetic design before symbolic-regression-specific lowering is added.

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

Occurrence identity follows the tree path represented by `ExpressionPoint`, not `ExpressionNode` object identity. If the same immutable node object occurs at two different paths, lowering creates two parameters. Within one symbol emission, emitted children are cached by child index: if a macro emits the same logical child more than once, lowering reuses one AD `Value`, preserving one parameter identity and explicit DAG sharing so reverse contributions accumulate.

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

EL-3 isolates the `DataFrame` dependency in a `DifferentiableExpression` extension. The compiler, retained differentiable expression, and numerical AD engine remain independent of data-analysis types. The extension resolves variables in retained AD input order, passes each `Series<double>.Values` memory directly to the AD execution without copying numerical data, and returns a structured failure for the first missing or incompatible series. Input-free expressions retain the AD engine's scalar one-result execution semantics rather than implicitly broadcasting to the data-frame row count.

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
- rebuilding accepted parameter values through one immutable `ExpressionTree.ReplaceMany` operation;
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

### Full-data objective and acceptance

The first implementation uses every training row. Observation sampling is deferred. When sampling is introduced later, it belongs to the symbolic-regression local-improvement adapter or evaluator composition, not the numerical solver.

The first symbolic integration defines:

- least-squares residuals from raw expression predictions and raw training targets;
- raw full-data mean squared error as the optimization and acceptance measure;
- retention of the best finite point observed during the LM run;
- rejection of a final replacement when its raw full-data mean squared error is non-finite or worse than the initial value beyond a small documented numerical tolerance.

Acceptance deliberately does not consult the problem's configured `IRegressionMetric`. The safeguard prevents numerical regression in the objective LM actually optimizes; it does not claim that every other problem metric must improve.

Evaluation-time linear scaling is outside the first constant-optimization objective. The optimizer neither differentiates through fitted scaling coefficients nor injects scaling terms into the expression. Normal symbolic-regression evaluation may fit or apply linear scaling after constant optimization exactly as it otherwise would. Explicit root-level scaling parameters or a jointly scaling-aware objective remain future design options.

Initially:

- optimize evolvable constants only;
- leave fixed constants unchanged;
- use least-squares residuals for LM;
- require direct full-data optimization;
- defer bounds, row sampling, sample weights, validation-driven acceptance, problem-metric acceptance, linear-scaling integration, and variable-weight optimization.

### Initial API boundary

Do not begin from `NumericParameterOptimizer.Optimize(expression, problem, options)` and work inward.

Begin with an internal direct constant-optimization entry point so the behavioral contract and performance can stabilize. Design a public symbolic-regression facade afterward. It should expose symbolic intent while allowing advanced users to select a numerical algorithm once multiple algorithms are genuinely supported. Candidate directions to evaluate later:

```csharp
// Direct local improvement.
var result = ConstantOptimization.Optimize(expression, problem, algorithm);

// Evaluator composition.
var evaluator = SymbolicExpressionEvaluator.WithConstantOptimization(
    innerEvaluator,
    algorithm,
    selectionPolicy);
```

These names are provisional. The final API must avoid exposing tape, Jacobian, row-selection, or solver-workspace details to ordinary symbolic-regression users.

### Direct vertical-slice artifact

Provide:

- a direct test fixture optimizing constants in a fixed expression;
- retained compiled/differentiable model reuse where lifecycle permits;
- tests for fixed constants, occurrence identity, structurally shared nodes, macro re-emission, unsupported operations, invalid points, immutable replacement, and raw-MSE non-regression;
- behavior-parity tests against the retained implementations using predictions and raw mean squared error within tolerance;
- performance comparison against both retained reference implementations.

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
- uses real lowered symbolic expressions and constant-optimization workloads in addition to focused synthetic kernels.

For the first MathNet backend, report at least four costs separately:

1. AD value/residual and derivative evaluation;
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

### Correctness before specialization

Every optimized kernel must have:

- a simple reference implementation or finite-difference oracle;
- invalid-value and edge-case tests;
- cross-batch consistency tests;
- benchmark evidence before replacing clearer code.

## Migration And Retirement

Replacement proceeds by behavior, not by file name:

1. establish general differentiation parity with the prototype's derivative tests;
2. establish MathNet-backed LM behavior on ordinary least-squares problems;
3. establish symbolic-expression lowering parity;
4. establish immutable replacement and raw full-data MSE non-regression;
5. compare predictions and raw MSE with retained implementations within documented tolerances;
6. compare performance on the retained benchmark corpus and decide whether MathNet remains the backend;
7. design evaluator or memetic integration only after the direct path is stable;
8. remove the prototype public API or archive it if it remains useful as a benchmark reference;
9. remove AutoDiff or MathNet dependencies only when no retained implementation or other library feature uses them.

Do not delete tests merely because the implementation changes. Adapt behavioral tests to the owning replacement layer.

## Decision Gates

The first vertical slice has settled these decisions:

1. the AD engine has internal types and a direct `ExpressionTree` lowering adapter;
2. lowering uses a mutable builder/compiler, produces an immutable reusable program, and evaluates through mutable run-scoped workspaces;
3. spans and contiguous buffers are preferred internally, while direct MathNet storage is allowed at the adapter boundary when it avoids measured cost;
4. MathNet LM is the first backend, with no broad solver-plugin architecture;
5. the first objective is raw full-data least squares over evolvable constant occurrences only;
6. acceptance uses raw full-data MSE non-regression and the best finite LM point, not the problem metric;
7. linear scaling remains an evaluation concern outside constant optimization;
8. output parity means predictions and loss within tolerance, not identical parameters or solver traces;
9. generic numerical algorithms and memetic composition are later generalizations, not delivery gates.

Remaining decisions should be made from implementation evidence:

1. the precise program instruction layout, derivative buffer layout, and Jacobian batch size;
2. which built-in expression operations are supported in the first increment and their structured failure details;
3. the exact MathNet API and storage shape with the lowest verified adapter overhead;
4. stopping defaults and the numerical tolerance used by the MSE non-regression safeguard;
5. whether MathNet performance justifies retention or replacement;
6. the eventual public facade and generic local-improvement contract.

## Incremental Delivery Sequence

| Increment | Usable result | Must not contain |
| --- | --- | --- |
| 0. Organization | Reviewed symbolic-regression ownership and namespace map | New solver or autodiff API |
| 1. AD operation model | Internal operation semantics plus mutable builder/compiler | Regression objectives, public authoring API |
| 2. Compiled AD execution | Immutable reusable program, mutable workspace, value/gradient/residual/Jacobian evaluation | `ExpressionTree`, MathNet solver policy |
| 3. AD verification | Analytic, finite-difference, invalid-domain, and allocation tests for each operation | Solver-specific derivative changes |
| 4. Expression lowering | Direct lowering with occurrence-safe optimizable-constant bindings | Optimization, mutation of the source tree |
| 5. MathNet LM adapter | Ordinary least-squares problem solved through a thin measured adapter | Generic optimizer hierarchy, memetic policy |
| 6. Direct constant optimization | Full-data LM run, best-finite retention, and raw-MSE non-regression | Sampling, problem-metric acceptance, linear-scaling internals |
| 7. Immutable rebuilding | One `ReplaceMany` operation produces the optimized expression | In-place mutation, fixed-constant replacement |
| 8. Behavioral comparison | Predictions and raw MSE match retained implementations within tolerance | Exact parameter-vector or solver-trace equivalence |
| 9. Performance decision | AD, adapter, solver, and complete pipeline benchmarked | Unmeasured backend abstraction or specialization |
| 10. Later generalization | Public API, other optimizers, sampling, evaluator and memetic composition as justified | Changes made only for hypothetical reuse |

Each increment requires:

- a small internal or public API with one clear owner;
- focused unit tests;
- an executable API usage spec or example when public;
- a named benchmark case when it introduces hot-path computation; measurement and specialization may remain deferred to Increment 9 unless performance blocks functional progress;
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
- generic local-improvement or memetic integration;
- acceptance through the problem's configured regression metric;
- differentiating through evaluation-time linear scaling or injecting root scaling parameters;
- replacing every existing HeuristicLib optimization algorithm with a gradient-based one.

These remain possible extensions. The first replacement should establish boundaries that can accommodate them without implementing them prematurely.

## First Vertical-Slice Completion Criteria

The first constant-optimization implementation is complete when:

- the internal AD program evaluates values, residuals, gradients, and the required Jacobian shape without per-operation allocation;
- analytic and finite-difference tests cover every supported operation and its invalid-domain behavior;
- `ExpressionTree` lowering preserves optimizable occurrence identity, fixed constants, macro semantics, and originating symbols;
- MathNet LM can optimize all training rows through a measured adapter;
- failures, unsupported models, no-parameter expressions, invalid trials, and termination are structured and tested;
- the input tree remains unchanged and accepted values are applied through one `ReplaceMany` call;
- raw full-data MSE cannot regress beyond the documented numerical tolerance;
- predictions and raw MSE match retained behavior within documented tolerances where the behavioral contracts overlap;
- benchmarks isolate AD, adapter, solver, and end-to-end costs and support an explicit MathNet retention or replacement decision;
- the direct capability is documented well enough to design its eventual public facade without exposing AD internals.

The broader redesign is complete later when justified public numerical-optimization APIs, generic local improvement, evaluator integration, additional solvers, and prototype retirement each have an explicit outcome. They are deliberately not conditions for completing the first vertical slice.
