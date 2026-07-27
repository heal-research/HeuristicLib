# Symbolic Regression Constant Optimization Plan

## Status

This is a design and implementation plan, not a commitment to the API shape of the current prototype.

The existing `NumericParameterExpression*` and `NumericParameterOptimization*` implementation is retained as an executable reference. It may provide algorithms, tests, performance techniques, and failure cases for the replacement, but its current naming, namespaces, public API, and responsibility boundaries are not the target design.

This plan refines Stage 3.1 of [symbolic-regression-redesign-plan.md](symbolic-regression-redesign-plan.md). The redesign plan should be updated to point here after this plan has been reviewed.

## Problem Statement

Constant optimization in symbolic regression is not one isolated solver call. It combines four distinct capabilities:

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

The replacement must build reusable layers in order. Every layer must produce a usable, tested artifact before the next layer depends on it.

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

The intended dependency direction is:

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

## Stage 1: General Automatic Differentiation

### Required use cases

The first differentiation layer must support:

- a scalar function of a parameter vector, returning its value and gradient;
- a residual model evaluated over observations, returning values and derivative information suitable for least-squares algorithms;
- reusable compiled structure with varying parameter values;
- caller-owned or pooled workspaces for repeated evaluation;
- deterministic evaluation with no hidden mutable global state;
- explicit reporting of unsupported operations and invalid numeric results;
- finite-difference verification independent of symbolic regression.

The initial scope is `double`. Generic numeric types, higher derivatives, sparse derivatives, complex numbers, and GPU execution are follow-up capabilities, but the design must not prevent a later backend.

### Differentiation modes

Design the public capability around requested derivative products rather than around one implementation:

- value only;
- value and gradient;
- Jacobian blocks for vector/residual outputs;
- vector-Jacobian products where reverse mode can avoid materialization;
- Jacobian-vector products if a later algorithm benefits from forward mode.

The first engine may specialize in batched reverse mode, but the public model must not claim that a full Jacobian is always available cheaply.

For least squares, support streaming blocks that let an algorithm accumulate `J^T r` and `J^T J` without retaining the full observation-by-parameter Jacobian. This is a consumer of derivative data, not a symbolic-regression-specific feature.

### Data representation decisions

Run a focused design and benchmark spike before selecting public data types:

1. `ReadOnlySpan<double>` and `Span<double>` for hot-path parameter, value, gradient, and workspace access;
2. repository-owned `RealVector` only if its candidate semantics also make sense for numerical linear algebra;
3. `System.Numerics.Tensors` tensor types for owning multidimensional data;
4. plain arrays or memory owners at public lifetime boundaries;
5. MathNet vectors and matrices only where a mature linear-algebra operation justifies their conversion and allocation costs.

`TensorPrimitives` is already useful for contiguous element-wise kernels, but it is not by itself an automatic-differentiation graph, tape, workspace model, or nonlinear solver. Use it as an implementation backend where benchmarks show a benefit. Do not expose tensor types merely because tensor kernels are used internally.

The first usable artifact only requires scalar parameters and a one-dimensional observation batch. Shape metadata should nevertheless be explicit enough that future vectorial symbolic regression and broadcasting are not silently represented as scalar operations.

### Operation model

Define how differentiable operations are registered or authored without delegates hidden inside value-semantic configuration objects.

The design must answer:

- whether users build a differentiable expression, implement an interface, or compile another domain representation into a tape;
- how an operation supplies value, reverse derivative, and optional forward derivative behavior;
- whether the operation set is closed like symbolic-expression opcodes or extensible by users;
- how scalar values, batched values, constants, and parameters are represented;
- how common subexpressions and repeated logical parameter occurrences are distinguished;
- how invalid-domain behavior for `log`, `sqrt`, division, overflow, and `NaN` is reported.

Avoid a public API that requires one type per low-level tape instruction. Avoid runtime dictionaries or reflection in the hot path.

### Execution architecture

Separate immutable model structure from execution state:

- immutable compiled differentiation representation;
- reusable execution workspace containing buffers, liveness/slot information, and batch state;
- explicit parameter and observation inputs;
- result written into caller-provided storage where appropriate.

Precompute operand indexes, liveness, storage slots, scalar/batch classification, and derivative dependencies during compilation. Evaluation must not rediscover graph structure or allocate per operation.

### First usable artifact

Provide a small public example unrelated to symbolic regression:

```csharp
// Provisional authoring shape, not a committed API.
var model = ...; // Rosenbrock or another two-parameter function
var compiled = model.Compile();

Span<double> gradient = stackalloc double[2];
var value = compiled.Evaluate([1.2, 0.8], gradient);
```

The artifact is complete when:

- users can define and differentiate at least one nonlinear multivariate function;
- analytic and finite-difference tests cover every built-in differentiable operation;
- repeated evaluation has a documented workspace and allocation model;
- benchmarks cover value-only, value-plus-gradient, and Jacobian-block evaluation;
- no symbolic-regression type is referenced by the differentiation assembly area.

## Stage 2: First-Class Numerical Optimization

### Problem contracts

Design numerical optimization around the information an algorithm mathematically requires:

- value-only objective;
- value-and-gradient objective;
- least-squares residual model;
- optional bounds or constraints later, without pretending they exist in the first slice.

Do not force least-squares algorithms through a generic scalar-objective contract that loses residual structure. Do not force general gradient algorithms to depend on Jacobians.

An objective model may be backed by automatic differentiation, analytic derivatives, or a user implementation. Automatic differentiation is one producer of derivative information, not a mandatory part of every algorithm.

### Algorithm model

Numerical optimizers are first-class HeuristicLib algorithms:

- reusable algorithm configurations;
- run-scoped execution instances and workspaces;
- explicit stopping criteria and budgets;
- observable search states or a deliberately justified compact numerical state;
- cancellation support;
- deterministic behavior when randomness is used;
- structured termination and evaluation counters;
- no hidden mutation of reusable configurations.

Provide a concise one-shot convenience API only after the algorithm model exists. The convenience call must be a facade over the same implementation, not a second solver.

### Algorithms

Implement in small increments:

1. one transparent baseline gradient algorithm to validate the objective and execution contracts;
2. Levenberg-Marquardt over the least-squares contract;
3. at least one additional practical gradient-based algorithm, such as L-BFGS, to prove that the abstraction is not LM-specific.

The exact order of the baseline and L-BFGS is a design decision. Symbolic-regression integration does not begin until both a general gradient objective and a least-squares objective can be optimized through the same numerical-optimization area.

### Linear algebra

Do not bury factorization code inside a symbolic-regression optimizer.

Compare:

- MathNet factorization and solver APIs;
- a focused span/array Cholesky implementation for small dense parameter systems;
- possible platform tensor or BLAS-backed operations for larger systems.

Choose by correctness, failure reporting, allocation, and measured parameter sizes. Keep factorization workspaces reusable. If a custom kernel is retained, give it focused numerical tests and a clear numerics owner.

### Levenberg-Marquardt requirements

The LM implementation must independently define:

- residual and Jacobian-block consumption;
- normalization convention;
- damping matrix and initial damping;
- gain-ratio acceptance;
- damping updates;
- factorization retry policy;
- function, gradient/Jacobian, and row-evaluation accounting;
- stopping criteria;
- invalid trial behavior;
- best-finite-result behavior at iteration limits.

These belong to the LM algorithm and its configuration. Regression metrics, expression rebuilding, observation sampling, and full-dataset acceptance do not.

### Second usable artifact

Provide examples and tests that optimize ordinary numerical problems:

- Rosenbrock with a gradient algorithm;
- linear and nonlinear least squares with LM;
- an analytic-derivative model and an automatically differentiated model using the same optimizer;
- iteration streaming or final result consumption through normal HeuristicLib algorithm authoring.

The artifact is complete when numerical optimization is useful without referencing symbolic regression.

## Stage 3: General Local Improvement And Memetic Composition

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

### Third usable artifact

Demonstrate the composition with a non-symbolic candidate first:

- a real-vector population algorithm;
- a gradient-based local optimizer;
- configurable all-candidate and probabilistic local improvement;
- deterministic random behavior;
- accounting that distinguishes global evaluations from local-search work.

This proves the memetic design before symbolic-regression-specific lowering is added.

## Stage 4: Symbolic-Regression Constant Optimization

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

### Sampling and acceptance

Observation sampling is part of the symbolic-regression local-improvement adapter or its evaluator composition, not the numerical solver.

The symbolic integration defines:

- full-data versus sampled optimization rows;
- deterministic sample construction for direct calls;
- run-RNG sampling for evaluator composition;
- whether one sample is shared across a batch of candidates;
- optimization loss used by the local optimizer;
- full-training metric evaluation before accepting the replacement candidate;
- objective-direction-aware non-regression.

Initially:

- optimize evolvable constants only;
- leave fixed constants unchanged;
- use least-squares residuals for LM;
- permit direct full-data optimization;
- defer bounds, sample weights, validation-driven acceptance, and variable-weight optimization.

### Public API

Do not begin from `NumericParameterOptimizer.Optimize(expression, problem, options)` and work inward.

Design the public symbolic-regression facade after the reusable layers exist. It should expose symbolic intent while allowing advanced users to select a numerical algorithm. Candidate directions to evaluate:

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

### Fourth usable artifact

Provide:

- a direct example optimizing constants in a fixed expression;
- a GA example with configurable memetic constant optimization;
- retained compiled/differentiable model reuse where lifecycle permits;
- tests for fixed constants, occurrence identity, macros, sampling, immutable replacement, and full-metric acceptance;
- performance comparison against both retained reference implementations.

## Stage 5: Performance Hardening

Performance work is continuous, but final specialization happens only after the layer boundaries are stable.

### Benchmarks

Keep focused benchmark suites for:

- differentiation compilation;
- value-only evaluation;
- value-plus-gradient;
- streamed Jacobian blocks and normal-equation accumulation;
- numerical solver iterations excluding model compilation;
- complete numerical optimization including setup;
- symbolic expression lowering and immutable replacement;
- evaluator-level memetic workloads over populations;
- retained workspace and compiled-model reuse.

Cover representative row counts, expression sizes, and parameter counts. Separate setup cost from steady-state execution and also measure the complete pipeline.

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
2. establish LM parity on ordinary least-squares problems;
3. establish symbolic-expression lowering parity;
4. establish immutable replacement and full-metric acceptance parity;
5. compare performance on the retained benchmark corpus;
6. switch the symbolic-regression evaluator/facade to the new layers;
7. remove the prototype public API or archive it if it remains useful as a benchmark reference;
8. remove AutoDiff or MathNet dependencies only when no retained implementation or other library feature uses them.

Do not delete tests merely because the implementation changes. Adapt behavioral tests to the owning replacement layer.

## Decision Gates

Resolve these questions in order:

1. What is the symbolic-regression folder and namespace ownership map while old and new systems coexist?
2. What is the minimal public differentiable-model contract?
3. Which data types own parameters, gradients, residuals, Jacobian blocks, and workspaces?
4. Is the first differentiation operation set closed or user-extensible?
5. How are scalar, batched, and future tensor shapes represented?
6. Which numerical optimization problem contracts are required by gradient and least-squares algorithms?
7. How do numerical algorithms participate in the HeuristicLib configuration/execution/search-state model?
8. Which local-improvement contract is canonical for memetic composition?
9. Where do selection probability, budget, sampling, and acceptance policies live?
10. How does an expression symbol expose derivative semantics without coupling genotype nodes to one differentiation backend?
11. Which prototype code is reusable after these boundaries are fixed?

Do not implement a later gate by inventing a private side contract before the earlier gate is settled.

## Incremental Delivery Sequence

| Increment | Usable result | Must not contain |
| --- | --- | --- |
| 0. Organization | Reviewed symbolic-regression ownership and namespace map | New solver or autodiff API |
| 1. Differentiation core | General nonlinear function with value and gradient examples | Regression, `ExpressionTree`, LM |
| 2. Residual differentiation | General batched residual/Jacobian-block model | Symbolic constants, regression metrics |
| 3. Gradient algorithm | First-class optimizer for an ordinary numerical objective | Symbolic-regression adapter |
| 4. Levenberg-Marquardt | First-class least-squares optimizer | Expression rebuilding, population fraction |
| 5. Memetic composition | Generic local improvement inside a population search | Expression-specific logic |
| 6. Expression adapter | Differentiable model compiled from `ExpressionTree` | Population selection policy |
| 7. Direct constant optimization | Usable direct symbolic-regression facade | Hidden in-place mutation |
| 8. Evaluator integration | Configurable memetic symbolic regression | Solver-owned candidate selection |
| 9. Hardening | Benchmarked, documented replacement | Unmeasured specialization |

Each increment requires:

- a small public or internal API with one clear owner;
- focused unit tests;
- an executable API usage spec or example when public;
- a benchmark when it introduces hot-path computation;
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
- replacing every existing HeuristicLib optimization algorithm with a gradient-based one.

These remain possible extensions. The first replacement should establish boundaries that can accommodate them without implementing them prematurely.

## Completion Criteria

The constant-optimization redesign is complete when:

- automatic differentiation is independently useful and documented;
- numerical optimization algorithms are first-class HeuristicLib algorithms;
- at least one general gradient algorithm and LM use the shared objective infrastructure;
- local improvement can be composed generically into a memetic algorithm;
- symbolic regression contributes only an adapter, policies, and immutable candidate rebuilding;
- ordinary users can enable constant optimization without understanding autodiff internals;
- advanced users can select and configure the numerical algorithm;
- the retained prototype behavior is covered by replacement tests;
- benchmarks show that the structured implementation remains competitive with the prototype;
- obsolete prototype and legacy code have an explicit retirement outcome.
