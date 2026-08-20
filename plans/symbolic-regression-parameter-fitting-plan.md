# Symbolic Regression Numeric Parameter Fitting Plan

## Status

This is a living design and implementation plan. Its internal API shapes may continue to evolve as the remaining stages are implemented and reviewed.

This plan refines Stage 3.1 of [symbolic-regression-redesign-plan.md](symbolic-regression-redesign-plan.md). The redesign plan should be updated to point here after this plan has been reviewed.

### Settled direction for the first implementation

The first implementation is a focused vertical slice for symbolic-regression numeric parameter fitting. It does not wait for a public automatic-differentiation API, a general numerical-optimization framework, L-BFGS, or generic memetic composition.

The sequence is:

1. implement an internal automatic-differentiation engine with its own execution-oriented representation;
2. lower an `ExpressionTree` into that representation while recording occurrence-safe bindings for optimizable constants;
3. expose model values, raw targets, and parameter derivatives to MathNet's Levenberg-Marquardt implementation through a thin, measured adapter;
4. optimize all training rows against raw targets using least-squares mean squared error;
5. do not reject a successful numerical result because its parameter values or mean squared error are non-finite;
6. rebuild the immutable expression through one `ExpressionTree.ReplaceMany` operation;
7. verify output behavior through analytic expectations, finite-difference checks, and maintained legacy behavior where applicable, then measure the complete pipeline before generalizing it.

MathNet is the first solver backend, not a permanent architectural commitment. The implementation should keep the backend boundary small enough to replace, but must not introduce dependency injection, reflection, virtual dispatch in hot loops, or a generalized plugin architecture merely to permit a hypothetical future solver. Direct use of MathNet storage types is acceptable where measurement shows that it avoids meaningful conversion or allocation cost.

The first automatic-differentiation and Levenberg-Marquardt APIs are internal. Both are plausible standalone capabilities for HeuristicLib users, so completing numeric parameter fitting must be followed by an explicit public-API review. That review selects the useful authoring, compilation, execution, result, and failure types rather than making the complete internal implementation public by default. Public authoring APIs, generic local-improvement composition, additional numerical optimizers, sampling, scaling-aware optimization, and evaluator integration follow only after the direct parameter-fitting path is correct and fast.

## Problem Statement

Numeric parameter fitting in symbolic regression is not one isolated solver call. The complete design combines four distinct capabilities:

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
| Numeric parameter fitting | The symbolic-regression adapter that treats each evolvable constant occurrence as a numerical parameter and returns an immutable replacement `ExpressionTree`. |

Use `candidate`, `algorithm`, `operator`, `evaluator`, `configuration`, `execution instance`, and `objective vector` according to [docs/glossary.md](../docs/glossary.md).

### Why parameter rather than constant

This capability was called *constant optimization* until RF-6, and the surrounding field still uses several
terms for it: PySR and HeuristicLab say *constant*, Operon says *coefficient*, and the research literature says
*parameter* — including [Kommenda et al., *Parameter identification for symbolic regression using nonlinear least
squares*](https://link.springer.com/article/10.1007/s10710-019-09371-3), whose method of Levenberg-Marquardt with
automatic differentiation as local search in tree-based GP is what Stages 1 through 4 implement.

*Parameter* was chosen for three reasons:

- **It stays correct as the capability grows.** *Constant* describes a leaf node, but the values being fitted need not
  be leaves: variable-weight optimization is deferred rather than rejected here, and Operon has already moved from
  leaf-only to all-node fitting.
- **Coefficient is strictly narrower.** A coefficient is a multiplicative factor. Fitted values also appear as
  exponents, phase offsets and divisors, where nothing is a coefficient of anything — which is exactly why the method
  is *nonlinear* least squares rather than a closed-form solve. Linear scaling, which does fit true coefficients,
  keeps that word.
- **It matches the layer.** The genotype is right to say *constant*, because the value is constant within one
  evaluation; the numerics are right to say *parameter*, because it is a free variable of the fitted model.
  `EvolvableConstantSymbol` and `FixedConstantSymbol` therefore keep their names.

*Numeric* qualifies the term because HeuristicLib also has operator and configuration parameters, and because
`ParameterFitting` without a qualifier is the name a general, problem-independent capability would want.

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

This layering does not prescribe delivery order. In particular, the first symbolic-regression parameter fitter may call an internal least-squares runner directly. Generic local-improvement and memetic composition remain a later consumer of the same parameter-fitting capability, not a prerequisite for it.

## Stage 0: Organize The Symbolic-Regression Area

Before designing new public numerical APIs, document and reorganize the current symbolic-regression implementation.

Current status:

- still-required mutable components remain active in the Experimental `Legacy` folder until their consumers migrate;
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

AD-6 ends after AD-6b. The formerly planned AD-6c through AD-6e work is postponed to the performance-decision increment, after expression lowering, the solver adapter, numeric parameter fitting, immutable rebuilding, and behavioral comparison provide representative end-to-end workloads. The provisional capacity remains `256`; no performance specialization is accepted from isolated AD measurements alone.

AD-6a confirms ordinary IEEE 754 propagation without protected operations or finite-value exceptions, compares vectorized reverse rules with scalar tails at boundaries derived from `Vector<double>.Count`, and verifies that mixed forward and Jacobian evaluations retain no stale values. Writable caller buffers are required to be mutually disjoint and not overlap parameters or bound input columns; read-only input columns may overlap one another.

AD-6b confirms that warmed `Evaluate` and `EvaluateWithJacobian` calls allocate no managed memory, disposed executions do not leak stale pooled values into later executions, and separate executions can evaluate one immutable program concurrently. The allocation and concurrency stress checks live in the scenario suite to keep the ordinary unit-test loop fast; pooled-buffer correctness remains a focused unit test. Adjoint slots are packed using the active row count for each batch, so only `adjointSlotCount * batch.Count` elements are cleared. A focused microbenchmark found this materially cheaper for small final batches and approximately neutral as the batch approaches capacity, so the capacity-sized pooled allocation is retained while clearing and indexing use the active size.

### Deferred operation-model consolidation review

Done; see [Operation-model consolidation review outcome](#operation-model-consolidation-review-outcome).

Do not generalize the operation model during the current AD checkpoints. At the complete vertical-slice review, inventory the repeated operation semantics and dispatch across symbolic symbols, expression lowering, scalar and batched expression evaluation, AD lowering, scalar and batched AD evaluation, reverse differentiation, formatting, and expression drafts.

The vertical-slice review must make and document an explicit decision among at least these outcomes:

- share a lower-level numerical program and forward execution engine;
- retain separate programs but generate their closed operation switches and mappings from one compile-time operation catalog;
- retain selected duplication where representations or hot-path requirements materially differ, with those differences recorded.

This review must distinguish numerical primitives from domain-level symbols and macros. It must not introduce a runtime registry, delegate dispatch, reflection, or polymorphic hot-loop architecture merely to reduce source repetition. Any consolidation deferred beyond the first parameter-fitting slice requires a named follow-up increment rather than an untracked cleanup note.

### Operation-model consolidation review outcome

The review the AD stage deferred to the complete vertical-slice review is done. It decides for **one shared
operation identity and one declaration site per operation**, dispatched through a table rather than through
repeated switches. This is the second of the three outcomes the deferred review named — generating the
consumers' dispatch from one operation catalog — with the catalog written by hand rather than by a source
generator, because the measurements below make a generator unnecessary.

#### Inventory

Three operation identities exist, with no compile-time link: `OpCode` (public, `ushort`, genotype side, 22
members), `Operation` (internal, `byte`, AD engine, 22 members) and the legacy byte constants under
`Problems/DataAnalysis/Symbolic`. The first two are near-identical sets with different numbering, bridged by
a hand-written map in `DifferentiableExpressionCompiler`.

Sixteen sites carry per-operation knowledge on the maintained side: the opcode metadata switch; the built-in
symbol records; two constant-folding switches in `ExpressionCompiler`; the interpreter's switch and its 21
kernels; `CompiledExpression.ToInfixString`; the AD compiler's unary and binary maps and its supported-set
predicate; the AD `Builder`; six switches across AD forward execution; the AD reverse derivative rules; four
formatters; and the infix parser.

The same forward math is written four times — constant fold, interpreter kernel, AD forward scalar, AD
forward vector — plus once more as a derivative. Adding an operation touches twelve to sixteen places and
**not one of them fails to compile if a place is missed**, because C# does not check enum switch
exhaustiveness and every site has a discard arm. The failure surfaces at runtime.

Three things are deliberately *not* duplication and must survive consolidation: span kernels genuinely differ
from scalar kernels, derivative rules are different information rather than copies, and formatter output is
target-specific. What repeats in the formatters is the twenty-way dispatch shape, not the content.

#### Measured basis for the decision

Nothing in the maintained hot paths dispatches per element. The interpreter, AD forward and AD reverse all
dispatch once per instruction and then process a whole span or batch, so dispatch cost is amortised over the
row count. A throwaway spike measured five dispatch mechanisms over a ten-instruction program, in that
amortised regime and in a row-major scalar regime that amortises nothing:

| Mechanism | Span regime | Scalar regime |
| --- | ---: | ---: |
| Switch | 1.00 | 1.00 |
| Static abstract through a generic type parameter | 1.00 | 1.00 |
| Function-pointer table | 1.00 | 0.96 |
| Delegate table | 0.98–1.01 | 1.02–1.03 |
| Interface singleton table | 0.99–1.01 | 1.13–1.17 |

In the span regime every mechanism is indistinguishable at 200, 2 000 and 20 000 rows. In the scalar regime
an ordering appears — function pointers beat a switch by about four percent, delegates trail it by three, and
interface dispatch trails by thirteen to seventeen — but no maintained path is in that regime today. **The
performance objection to consolidation does not survive measurement**, which is why the decision is made on
design grounds.

#### The decision

1. **One operation identity, named `OpCode`.** `Operation` is internal and disappears into it. `OpCode` keeps
   its name because it is already the public one, and because in this design its role is exactly the value
   stored in an instruction and shipped to a backend. Keeping it also means the merge carries no rename, so
   the namespace move is the only break. The name is provisional: it is understood, and it is cheap to
   revisit once the catalog exists and its use is visible. `IOperation` and `AddOperation` name the types
   carrying semantics, which the retained `OpCode` leaves free.
2. **Opcode values are append-only.** Genotypes are persistable and store symbols, so an opcode value is a
   compatibility contract: never reordered, never reused after retirement. The value space is expected to go
   sparse over time; the dispatch table is sized to the largest value and stays small under disciplined
   numbering, with a `byte` remap to a dense index available if it ever does not.
3. **One struct per built-in operation, with `static abstract` members** carrying every facet: span kernel,
   scalar kernel, adjoint rule, constant fold, arity, payload kind and differentiability. The compiler then
   enforces that an operation defines everything, which is the property the current design lacks entirely,
   and static members make it impossible for an operation to carry state.
4. **One catalog binds opcode to operation type**, projected into a table indexed by opcode. Consumers index
   the table; no consumer holds a switch. Declaration and dispatch are deliberately separable: the operation
   structs do not know how they are dispatched.
5. **Delegates are the first table form.** Bound from the static abstract members, which is verified to
   compile and to bind correctly per instantiation. This avoids `unsafe` at no measured cost in any
   maintained path. Swapping to a function-pointer table later, should a row-major path make four percent
   matter, changes the catalog file alone and leaves every operation type byte-identical.
6. **Primitives stay closed; extension is by symbol.** A consumer defines an `OperationSymbol` whose `Emit`
   expands into built-in opcodes, which `SigmoidSymbol` already demonstrates and which is already public API.
   Such a symbol works across interpreter, AD, formatters and parser without touching the catalog. This is
   also what keeps every user expression portable to a future GPU backend: a user cannot introduce a kernel
   that only exists on the host.
7. **The instruction stream is data; the catalog is behavior.** Instructions stay blittable value types
   holding an opcode and integer operands, with variable names in a side table — which is already true today.
   No reference, string, delegate or function pointer ever enters an instruction. Opcode numeric values are
   explicit and stable, because a separately compiled device kernel switching on them makes them a
   cross-language contract. The dispatch table stays host-side and keyed by opcode; a device backend supplies
   its own.

Deliberately out of scope: the four formatters and the infix parser keep their own dispatch. They map
*symbols* to target languages rather than opcodes to kernels, and a macro symbol such as `SigmoidSymbol` has
no opcode at all, so their dispatch cannot key on the catalog. The legacy opcode set under
`Problems/DataAnalysis/Symbolic` is untouched and leaves with the legacy system.

Not chosen, with reasons: a source generator produces the same code the hand-written catalog does and adds a
build-time surface to maintain; retaining the duplication with a conformance test fixes only the silent-miss
symptom and leaves the same math written four times; and an interface singleton table was the slowest
mechanism measured and would have permitted an operation to hold state.

#### Where the shared identity lives

The differentiation core currently has no dependency on symbolic expressions, which the AD scope statement
requires: "It has no dependency on symbolic expressions, data analysis, regression, MathNet, or solver
policy." Merging into the genotype's `OpCode` where it stands would break that, so **the merged identity and
its catalog move to a neutral home under `HEAL.HeuristicLib.Numerics`**, which both the differentiation core
and the genotype depend on. A numerics layer that the symbolic-regression genotype builds on is the right
direction of dependency; the reverse is not.

This adds no translation step. One shared type means the genotype's instruction stores it directly rather
than carrying its own. The only translation that exists today is the hand-written opcode-to-operation map
inside `DifferentiableExpressionCompiler`, and the merge deletes it, so the change removes an indirection
rather than introducing one.

Moving a public enum is a source-breaking namespace change for consumers. That break is accepted, and any
rename is folded into it so that consumers absorb one break rather than two.

#### Resolved details

- **Non-differentiable operations carry `IsDifferentiable = false` and throw if their adjoint is requested.**
  One interface and one table, no second tier. The throw is a programming-error guard rather than a control
  path: the real check stays where `IsSupported` sits today, at lowering time, so a non-differentiable
  operation is rejected before any adjoint is asked for.
- **Terminals are `Variable`, `Constant` and `Parameter`.** `Variable` replaces the AD engine's `Input`,
  being the more understandable of two names for one concept. `Parameter` stays a distinct member because
  forward evaluation genuinely fetches from a different array than `Constant` does. The genotype emits only
  `Variable` and `Constant`; the distinction between an evolvable and a fixed constant is a property of the
  symbol, and it is AD lowering that turns an evolvable occurrence into a `Parameter`. A shared instruction
  set having members that one producer never emits is ordinary, and expression-program validation rejects
  any operation whose payload kind is invalid for that program.
- **The catalog also drives the AD `Builder` and `CompiledExpression.ToInfixString`**, removing two further
  hand-maintained switches.
- **The catalog and the operation types are public.** Users extend through symbols rather than kernels, but
  nothing is gained by hiding the vocabulary they emit into.
- **The two program representations stay separate.** Merging the identity does not merge the genotype and AD
  instruction layouts, which keep their own operand encodings and slot assignments.
- **The enum merge lands first**, then the catalog with metadata facets replacing `OpCodes.TryGetMetadata`
  and the AD supported-set predicate, then one consumer at a time: interpreter, AD forward, AD reverse,
  constant folding, builder, display.
- **Behavioral equivalence is the expectation, not an absolute gate.** The 2 236 tests across four suites and
  the nine best-objective values the profile harness reports are the check. Should the refactor change a
  result, the change is examined on its merits rather than assumed to be a defect.

#### A seventeenth site the inventory missed

The interpreter sizes its workspace from a hardcoded list of the operations needing a scratch column:
`opCode is OpCode.Root or OpCode.AnalyticQuotient`. Nothing connected that list to the kernels that use the
scratch, so the two could disagree silently. It is now a `ScratchColumns` facet declared with the operation,
which is the clearest case yet for the catalog: the knowledge was invisible until an operation had to be
described completely.

#### Open items carried into the implementation

These are recorded rather than settled, because they are easier to judge against working code than in advance.

- **Naming is provisional.** A later pass should consider `Operation` for the enum and `OperationDefinition`
  for the interface an operation implements, which would leave today's `OperationDefinition` record needing a
  name of its own, such as `OperationEntry`. Deferred so that the names are chosen while reading the code
  that uses them.
- **`PayloadKind` is genotype-shaped and redundant.** It is fully determined by the opcode — the three
  terminals map one to one onto its three values and everything else is `None` — and only the genotype layer
  reads it. It sits on `ITerminalOperation` for now so that it is at least declared with the operation rather
  than typed into a list. Whether it belongs in the shared layer at all is open.
- **Two things per operation look like one thing twice.** An operation declares `static abstract Apply`, and
  the table holds a delegate bound to it. The delegate is a handle rather than a second declaration, because
  an array slot needs a value, but the pair is easy to misread and the names should make the distinction
  obvious.
- **A dispatch interface would replace the delegates and is worth revisiting.** A generic adapter written
  once — `BinaryDispatch<TOperation> : IBinaryDispatch` whose instance methods forward to the operation's
  static ones — would let the table hold an object with methods instead of a row of delegate handles, while
  the operation itself stays entirely static, so state remains impossible and the compiler still enforces
  every member. It costs interface dispatch instead of a delegate call, which the spike measured at 1.13 to
  1.17 in the scalar regime and 1.00 in the span regime that every current consumer uses. Delegates were kept
  because they measured fastest and add no type; the adapter reads better and is a contained change, since
  the operation types do not move either way.
- **Arity is deliberately not declared.** It follows from which interface an operation implements, so
  declaring it would allow an operation to contradict itself.
- **Arity two is the declared ceiling and stays that way until an operation needs more.** The consumers'
  arity dispatch is the cheap part of the assumption; the binding constraints are that `Instruction` encodes
  exactly two operand slots and that `IBinaryOperationDefinition` names four kernel shapes for the
  scalar/span combinations, which a ternary operation would take to eight. Generalizing now would mean either
  losing that specialization or maintaining an n-ary path with no consumer. It is deferred rather than
  ignored because every arity switch ends in a throw naming the operation and its arity, so an undeclared
  arity fails loudly at its own site instead of silently. When one is needed, the contained change is a
  span-only `ITernaryOperationDefinition` — skipping the scalar specializations rather than doubling them —
  plus a third operand slot in `Instruction`. It is contained precisely because the catalog centralizes what
  the sixteen sites used to each restate.

#### AD reverse sweep

The reverse sweep was the one site increment 16 initially left alone. `Execution.Reverse.cs` switched twenty
ways on `Operation` and dispatched to twenty hand-written `AccumulateAdjointsFor*` methods, so it was the last
place that had to be edited by hand when an operation was added, and the last place that failed at runtime
rather than at compile time when that edit was forgotten. Removing that switch was the entire prize; no math
was deleted, because adjoint rules are different information rather than copies, as the inventory above
records.

An adjoint rule does not fit the forward kernel shape. It needs the upstream adjoints, both operand primals,
the instruction's own forward result for the rules that reuse it rather than recompute it, and up to *two*
destinations that it accumulates into rather than assigns, either of which may be dead because its operand
does not depend on a parameter. Three shapes were considered for handing a rule that much: a
`BinaryAdjointContext` ref struct bundling it, a wide parameter list, and leaving the switch alone.

**Decided: the wide parameter list.** Two signatures, matching the forward unary/binary split:

```csharp
// IUnaryOperationDefinition
static abstract void Adjoint(
    ReadOnlySpan<double> upstream, Operand operand, Operand result,
    Span<double> operandAdjoints);

// IBinaryOperationDefinition
static abstract void Adjoint(
    ReadOnlySpan<double> upstream, Operand left, Operand right, Operand result,
    Span<double> leftAdjoints, bool leftIsActive,
    Span<double> rightAdjoints, bool rightIsActive);
```

Unary carries no activity flag, because with one operand the driver skips the call instead. Binary carries one
per side, because either side can be active alone. **Active** and **passive** are the activity-analysis terms
from the automatic-differentiation literature, chosen over the borrowed compiler-dataflow sense of live and
dead: an operand is active when it depends on a parameter, and a passive one has no derivative to receive. The
flags are redundant with the adjoint span being empty, and that redundancy is deliberate: encoding activity as
an empty span would be an implicit rule the reader has to already know, which is the class of cleverness this
codebase has been removing. The rule bodies move across unchanged, and `count` comes from `upstream.Length`
rather than a parameter.

The context struct was rejected as the more general answer to a generality that is not needed yet. It wins
only when a seventh input arrives, since it absorbs that as one field where the wide form edits twenty
signatures. Leaving the switch alone was rejected because the compile-time-safety gap it preserves is the
specific defect this whole consolidation exists to close.

**The seventh input arrived, and wide was kept.** Scratch spans were added to both adjoint signatures shortly
after the sweep landed, for the reason recorded under [Adjoint scratch](#adjoint-scratch) rather than the
`Tanh`/`Sqrt`/`CubeRoot` recomputation this section had anticipated. The decision was re-taken deliberately and
came out the same way: one more parameter is tolerable, the forward path already establishes how scratch is
declared and reserved, and that precedent is worth more than the parameter it saves.

Two consequences are preconditions rather than side effects, and belong to whichever increment does the work:

- Five helpers currently private to `Execution` — `AccumulateScaled`, `AccumulateWithPrimalFactor`,
  `AccumulateDividedByPrimal`, `ValueAt` and `GetVectorizedElementCount` — must become a shared internal
  static class, because the rules that call them leave that class. It belongs at the end of the definitions
  file rather than in one of its own, since only the rules in that file use it.
- The twenty bodies average roughly twenty-eight lines and land in `OperationDefinitions.cs`, which already
  holds twenty-two structs, so that file roughly doubles.

Both preconditions were met as part of the work. `AdjointAccumulation`, at the end of the definitions file, holds the three
accumulation shapes the rules share — `AddScaled`, `AddProduct` and `AddQuotient` — plus the SIMD tail split; the division denominator
partial went to `DivideDefinition` as a private member, being specific to one operation rather than shared;
and `Operand.ValueAt` replaced the private broadcast reader, since reading either shape at an index is what
the two shapes exist for. `OperationDefinitions.cs` stayed one file, ordered by arity — terminals, then unary,
then binary — rather than being split per arity as this section originally anticipated.

**Outcome.** `Execution.Reverse.cs` went from 663 lines to 116: the backward loop, the two arity branches that
gather forward values and decide liveness, and the three lookups they need. The twenty rule bodies moved
across unchanged. No `case Operation.` remains anywhere in `src`, so every consumer now reads the catalog and
the compiler requires each operation to supply its own adjoint rule. The two switches that remain are on
`PayloadKind`, which has three values and is read from the catalog. All nineteen differentiable operations are
covered individually by the per-operation verification theory in `ExpressionAdapterVerificationTests`, which
passes unchanged.

#### Adjoint scratch

`Power`, `Root` and `AnalyticQuotient` were the three rules that could not be composed from primitives. Every
piece they need — `Pow`, `Log`, `Multiply`, `Divide`, `Sqrt` — exists in `TensorPrimitives`; what was missing
was somewhere to put the intermediates, so all three fell back to element loops reading operands through a
broadcast accessor on `Operand`. That accessor branched on operand shape once per element.

Both adjoint signatures now take `ScratchSpans`, declared through `AdjointScratchSpanCount` on the operation.
It is declared apart from the forward `ScratchSpanCount` because a derivative builds different intermediates
than the value does: the three rules that need it declare two spans each and every other operation declares
none, whereas forward `Root` and `AnalyticQuotient` declare one. `Execution` reserves a second buffer sized the
same way as the forward one.

`TensorPrimitivesEx` holds what `TensorPrimitives` does not: forms taking an `Operand`, which cannot use its
overload-per-shape approach because an operand carries its shape at run time, and an accumulating divide to
match its `MultiplyAdd`. The three rules are now primitive compositions, `Operand.ValueAt` is gone, and shape
is decided once per span instead of once per element.

The rewrite replaced all the broadcast handling in those rules, and the existing per-operation theory only
covered one shape combination each, so `BinaryOperationMatchesFiniteDifferencesForEveryOperandShape` covers
all four combinations of span and parameter-only operands for the three of them. Mutating the scalar branches
fails six of the twelve cases, so the coverage is real rather than nominal.

#### Broadcast spike

> **This finding must outlive this document.** This plan is deleted when the branch merges, and the result below
> is the kind that is expensive to lose: the duplication it justifies *looks* like something to clean up, so a
> later reader who does not know these numbers will propose exactly this unification again. Without the
> measurements they either re-derive them or, far worse, carry the change through and silently give up a factor
> of 45 to 80 on the hottest path in the library — a regression that no test fails on and that end-to-end timings
> would show only as "the evaluator got slower." Relocate this section, its numbers and its re-test trigger to a
> durable home before this plan is removed. `BroadcastSpikeBenchmarks` must move with it or be kept, because the
> numbers are only re-checkable while the harness exists.

The shape duplication that survives consolidation is real and visible: fourteen mixed-shape forward overloads
across seven binary operations, nine adjoint rules branching on operand shape, and eight shape branches inside
`TensorPrimitivesEx`. The fourteen are the ones that recur, because every new binary operation pays them.

The proposal was to delete all of it by replacing `Operand` — which is a hand-rolled broadcasting tensor
restricted to stride zero and stride one — with the general `Tensor` API, which broadcasts. `Tensor<T>`,
`TensorSpan<T>` and `ReadOnlyTensorSpan<T>` are present and no longer experimental in the referenced
`System.Numerics.Tensors` 10.0.8, and a stride-zero view over a one-element buffer broadcasts correctly with no
materialisation, so the idea was sound on its face. `Tensor<T>` itself was excluded before measuring, being a
class that would allocate per operand per instruction.

Measured on .NET 10.0.11, Intel Core Ultra 7 265, at the row counts the whole-run profile used. Costs are per
element, and are flat across row counts to within a few percent, which is the important part: this is a
per-element cost, not a startup cost that a larger batch amortises.

| Variant | Multiply ns/element | Pow ns/element |
| --- | ---: | ---: |
| `TensorPrimitives`, span and single value | 0.08 | 6.63 |
| `TensorPrimitives`, span and span | 0.11 | 6.66 |
| `Tensor`, span and single value | 4.01 | 10.89 |
| `Tensor`, span and stride-zero broadcast | 6.43 | 15.31 |
| `Tensor`, span and span | 6.35 | 15.47 |
| `Tensor<T>` object, span and span | 6.41 | — |
| `Tensor<T>` object, stride-zero broadcast | 6.46 | — |

Nothing here is an allocation artifact, and the comparison does not charge the tensor path for buffers the
baseline gets free. Every variant reads and writes the *same* arrays, allocated once in setup; `TensorPrimitives`
takes them as spans and the tensor variants wrap them, since both `Tensor.Create` and the ref struct views alias
existing memory rather than copying it. Every variant allocated zero bytes under the memory diagnoser.

Two controls establish where the cost is. Building the three views costs **8.3 ns per call**, flat across all
three row counts, which at twenty thousand rows is six thousandths of one percent of the operation. And
`Tensor<T>` held in fields, built once in setup so that the measurement contains no construction at all, comes
out the same as the ref struct views to within noise. Removing construction entirely changes nothing: the cost
is inside the operation, per element, and identical whether the tensor is a class or a view, dense or
broadcast.

**Broadcasting is not what costs.** The `Tensor` operation layer costs about 6.4 ns per element whether it
broadcasts or not, against 0.08 to 0.15 ns for `TensorPrimitives`, so the penalty is the abstraction rather
than the stride-zero view. A tensor multiply over 20 000 rows takes 128 986 ns; a `TensorPrimitives` **pow**
over the same rows takes 133 331 ns. The abstraction costs about as much per element as computing a power
function.

A flat per-element cost that survives every attempt to remove overhead around it is the signature of a scalar
loop. `TensorPrimitives` at 0.039 ns per element is roughly four doubles per cycle, which is a vectorised one.
Both span views report `IsDense` true, so a dense fast path had every opportunity to apply and evidently is not
there for these operations at this version.

The penalty is smaller for expensive operations, as expected — 2.3 times for `Pow` against 45 to 80 times for
`Multiply` — because per-element math dilutes fixed overhead. That does not rescue it: `Multiply` and `Add`
dominate evolved expressions, and the profile puts the evaluator above half of run time. Every variant
allocated nothing, so the ref struct views behave as intended; throughput alone decides this.

**Decision: `Operand` and the shape overloads stay, and the shape duplication is deliberate rather than
unfinished.** The duplication buys a factor of 45 to 80 on the hottest path in the library, which is not a trade
worth making for deleting fourteen one-line overloads. Anyone proposing to unify the shapes — through the
`Tensor` API or any other single-entry-point abstraction — should re-run the spike first and be required to show
that the unified form holds the per-element costs in the table above. This measures
one implementation at one version, and the `Tensor` API is young; the spike is kept as
`BroadcastSpikeBenchmarks` and runs with `dotnet run -- broadcast`, so the question can be reopened cheaply
when the runtime changes rather than re-argued from first principles.

Implementation was increment 16 in the delivery sequence. It is deliberately not part of the first vertical
slice, which is complete.

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

Do not force least squares through a generic scalar-objective contract that loses its model, target, and Jacobian structure. Do not introduce a generalized solver abstraction for the first backend. A future numerical-optimization area may add broader contracts after the complete parameter-fitting slice provides implementation evidence.

Although the initial adapter is internal, standalone least-squares fitting is a credible user-facing capability. Its eventual public surface is deliberately postponed until numeric parameter fitting exercises the full lifecycle and reveals which configuration, result, failure, cancellation, and ownership details are stable.

### Initial execution model

The adapter is the numerical workhorse for one synchronous, bounded solve. MathNet stops through convergence or its maximum-iteration setting. Reaching the maximum is a successful adapter result when MathNet returns a result; the adapter does not translate this into HeuristicLib termination semantics.

This workhorse does not reference or participate in HeuristicLib algorithms, operators, terminators, execution-instance infrastructure, evaluation accounting, or search-state production. Coupling the parameter fitter to those facilities is a later orchestration task and is not part of LM-0 through LM-3.

Cancellation is checked before entering MathNet and from the model and Jacobian callbacks because the selected MathNet API has no direct `CancellationToken` parameter. The adapter introduces no randomness.

### Solver sequence

Implement in this order:

1. a thin adapter from internal model-value/Jacobian evaluation and raw targets to MathNet's Levenberg-Marquardt implementation;
2. the direct symbolic-regression parameter-fitting vertical slice;
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

Expression rebuilding belongs to the parameter-fitting component. Best-point retention and acceptance are separate later policies. Expression discovery, future row sampling, and HeuristicLib operator integration also remain outside the numerical adapter.

### MathNet adapter checkpoints

Checkpoint states are `Pending`, `In progress`, `Awaiting review`, and `Accepted`. Implementation stops at `Awaiting review`; only explicit user acceptance advances to the next checkpoint.

| Checkpoint | Status | Deliverable |
| --- | --- | --- |
| LM-0 Design contract | Accepted | Document the `TryMinimize` boundary, model-plus-target formulation, Jacobian convention, result and failure semantics, non-finite behavior, cancellation, MathNet stopping behavior, storage ownership, and exclusions. No source code. |
| LM-1 Successful solve path | Accepted | Implement the adapter, expose the required AD execution dimensions, wrap output and Jacobian arrays without copying, and verify a simple linear least-squares solve. |
| LM-2 Outcome behavior | Accepted | Add argument validation, cancellation, MathNet-exception conversion, maximum-iteration behavior, and non-finite result propagation. |
| LM-3 Verification and hardening | Accepted | Add fast nonlinear, mean-squared-error, Jacobian-orientation, repeated-solve, and storage-lifetime tests to the core suite; place calculation-intensive stress cases in the scenario suite; confirm the adapter remains independent of expressions, regression, and HeuristicLib algorithm/operator infrastructure; record duplicated-forward-sweep benchmarking as deferred work, since closed by the [solver cost attribution outcome](#solver-cost-attribution-outcome). |

LM-0 through LM-3 cover only the MathNet workhorse adapter. They explicitly exclude HeuristicLib termination, evaluation accounting, algorithms, operators, general optimizer APIs, solver interchange abstractions, constant acceptance, and symbolic-expression rebuilding.

LM-3 verifies exact nonlinear fitting, non-zero MSE normalization, the parameter-major-to-column-major Jacobian mapping, repeated solves over one execution, and result ownership beyond caller-buffer and execution lifetimes. These remain fast core tests. No calculation-intensive test is needed for the focused numerical contracts; representative large-row, large-parameter, repeated-solve, and duplicated-forward-sweep measurements remain scenario and benchmark work after the complete parameter-fitting workload exists. A dependency audit confirms that the AD and optimization areas do not reference symbolic expressions, data analysis, regression, or HeuristicLib algorithm/operator infrastructure.

### Second usable artifact

Provide focused tests that optimize ordinary least-squares problems before the expression adapter is connected:

- linear and nonlinear least squares with LM;
- analytic derivatives checked against automatically differentiated derivatives;
- non-finite propagation, MathNet failures, cancellation, and iteration limits;
- Jacobian orientation, mean-squared-error normalization, storage lifetime, and repeated solves.

Ordinary contract and numerical examples must remain fast unit tests in `HeuristicLib.Tests`. Larger row counts, parameter counts, repetition counts, and other calculation-intensive stress coverage belong in `HeuristicLib.Tests.Scenarios` so the normal development loop remains fast.

The artifact is complete when the AD engine and MathNet adapter can solve an ordinary least-squares problem without referencing symbolic regression. A general public numerical-optimization API is not required.

## Stage 3: Refinement And Memetic Composition

This stage was initially deferred until the direct parameter-fitting slice established its transformation, failure, and cost semantics. That slice is now complete enough to settle the general integration model.

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

An ordinary refiner performs no problem evaluation, so the algorithm evaluates its result exactly once. The general refiner contract contains no before/after objective comparison or retention guarantee. A particular refiner may use its own internal numerical acceptance information, such as the least-squares loss inside numeric parameter fitting, but that does not become a general problem-objective promise.

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
  more than once, as in `simplification → numeric parameter fitting →
  simplification`. The composite preserves the configured order.
- **Iterated.** Feeds the refined candidate back into the same refiner for a
  configured number of rounds, mirroring
  the `IteratedEvaluator` that RF-2 removes. This is the intended way to
  express repeated refinement of one candidate.
- **Choose-one, multi, wrapping, observable, and instrumentation** variants
  follow the conventions of the other roles.
- **Refinement evaluation.** Applies a refiner temporarily, evaluates the refined
  candidate, and associates its objective vector with the original candidate.
  Its public name is settled with the other composition types in RF-4.

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
- **`Criterion`** — decides what counts as an improvement. A `null` value uses
  `ImprovementChecking.Default`. Supplying a criterion lets a user drive acceptance
  from one dimension of a multi-objective problem while selection continues to use
  the full objective vector.

The criterion is an explicit strategy rather than an `IComparer<ObjectiveVector>`.
Two findings settled this during RF-3/RF-4:

- **A comparer cannot see the objective directions.** An improvement threshold has
  to move one operand by a delta, and whether that means adding or subtracting
  depends on the direction of each objective. A comparer constructed without the
  directions cannot apply the shift correctly, so the threshold would silently
  invert on a maximization objective.
- **A threshold breaks the comparer contract.** Shifting one operand makes
  `Compare(a, b)` and `-Compare(b, a)` disagree. `IComparer<ObjectiveVector>` is an
  ordering device, and the repository rule that a type must not be able to express
  what its contract forbids applies directly: such an object reaching a sort is a
  latent defect with no compile-time guard.

The criterion therefore names its asymmetry and receives the directions:

```csharp
public interface IImprovementCriterion
{
    bool IsImprovement(ObjectiveVector refined, ObjectiveVector original, ObjectiveDirections objectiveDirections);
}
```

The interface, its built-in implementations and the `ImprovementChecking` companion
live in `ImprovementCheckingRefiner.cs` beside their only consumer. They follow the
`IObjectiveVectorAggregator` /
`ObjectiveVectorAggregation` convention: immutable records behind a static companion
exposing named instances. The retention rule and the criterion are therefore one
concept rather than two:

| `ImprovementChecking` member | Semantics |
| --- | --- |
| `Default` | Strictly better by the problem's total objective order where one exists, dominance where it does not. |
| `StrictlyBetter` | Strictly better by the total objective order; throws where none is defined. |
| `NotWorse` | Not worse by the total objective order; throws where none is defined. |
| `Dominance` | Refined dominates original; works at any arity and needs no total order. |
| `MinimumImprovement(delta)` | Every objective improves by at least `delta` in its own direction. |
| `MinimumRelativeImprovement(fraction)` | Every objective improves by at least `abs(original) * fraction` in its own direction. |

`Default` keys off whether `ObjectiveDirections.TotalOrderComparer` is a
`NoTotalOrderComparer` rather than off the objective count, so a multi-objective
problem configured with a weighted-sum or lexicographic order uses the order its
author deliberately chose instead of falling back to dominance. Single-objective
problems always define a total order, so they get strict improvement as specified.

Threshold margins are direction-correct by construction — `original - refined` for a
minimized objective and `refined - original` for a maximized one — so a positive
threshold always means "better by at least this much". Every objective must clear the
margin, which is strict for multi-objective problems; `Dominance` is the criterion to
use where objectives trade off. Thresholds are retained as given rather than clamped,
so zero accepts anything not worse and a negative value deliberately tolerates a
bounded worsening. A `NaN` objective value is never an improvement.

This criterion belongs to `ImprovementCheckingRefiner` alone. `RefinementEvaluator`
refines and evaluates without comparing anything, so it needs neither an evaluation
of the original candidate nor a criterion.

Dominance and a total order coincide on every single-objective problem, so the choice
only matters once objectives can trade off. They diverge in three ways: dominance
abstains on an incomparable pair while a total order still decides; a total order
accepts a trade-off that dominance refuses; and dominance's `Equivalent` means
componentwise identical, whereas a total order can rate two different vectors equal.
Dominance is otherwise the stricter criterion, but only while the total order is
consistent with it — nothing validates weights, so a negative weight reverses the
implication. Each case is pinned by a test.

**Blocker for RF-7 — resolved.** `ObjectiveVector.CompareTo` opened with
`if (ReferenceEquals(this, other)) return 0;`, and `0` was `DominanceRelation.Dominates`,
so a vector reported that it dominated itself. `Dominance` and `Default` therefore
accepted a non-improvement whenever both evaluations returned the same `ObjectiveVector`
instance, which a shared `CachingEvaluator` makes likely — exactly the configuration the
composition examples above recommend. It also reached `DominationCalculator`;
`ParetoFront` happened to guard against it by excluding self-comparisons first.

The fast path was unnecessary: for a self-comparison the loop already reaches
componentwise equality on its own, including for `NaN`, and skipping it also skipped the
argument validation. It now sits below the argument checks and returns the named
`DominanceRelation.Equal`. See [Objective ordering outcome](#objective-ordering-outcome).

**Second blocker for RF-7 — resolved.** Every order-based criterion ranked through
`double.CompareTo`, which orders `NaN` below every number, so `StrictlyBetter`,
`NotWorse`, `Dominance` and `Default` all accepted a refinement that produced a `NaN`
objective value as the best possible outcome. The threshold criteria were already safe,
because a comparison against a `NaN` margin is false.

Fixed at its root rather than in the criteria: ordering is now owned by
`ObjectiveValue.Compare`, which ranks `NaN` worst *before* the objective direction is
applied. Applying the direction first was the actual defect — it flipped `NaN` along with
everything else, making it best when minimizing and worst when maximizing. That
asymmetry is why the behavior was never deliberate.

Refinement failure inside the inner refiner returns the original candidate
unchanged; the improvement check never converts a failure into a worse candidate.

This refiner costs two problem evaluations, and the algorithm still evaluates the
returned candidate afterwards, so a naive configuration performs three
evaluations per refined candidate instead of one. That is one more than a
candidate-transforming evaluator would need, and it is accepted deliberately:
the cost is visible, configurable, and removable through caching, while the
transforming-evaluator alternative bought its saving by making every algorithm
responsible for using a returned candidate instead of the one it passed in. For
numeric parameter fitting the extra evaluation is small next to the Levenberg-Marquardt
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
algorithm.Refiner = new ImprovementCheckingRefiner<...>(parameterFitting);
```

Three problem evaluations per refined candidate, one of which counts against the
budget. Refinement cost is excluded from the evaluation budget.

**2. Shared evaluator: every evaluation counts.**

```csharp
var evaluator = new LimitEvaluator<...>(new ProblemEvaluator<...>(), maxEvaluations: 100_000);

algorithm.Evaluator = evaluator;
algorithm.Refiner = new ImprovementCheckingRefiner<...>(parameterFitting) { Evaluator = evaluator };
```

The same instance is handed to both, so the registry resolves one
`LimitEvaluator` execution instance holding one counter. All three evaluations
count, and the budget describes total evaluation effort including refinement.

**3. Shared cache: pay for two evaluations, not three.**

```csharp
var evaluator = new CachingEvaluator<...>(new ProblemEvaluator<...>(), keySelector);

algorithm.Evaluator = evaluator;
algorithm.Refiner = new ImprovementCheckingRefiner<...>(parameterFitting) { Evaluator = evaluator };
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
algorithm.Refiner = new ImprovementCheckingRefiner<...>(parameterFitting)
{
    Evaluator = new CountingEvaluator<...>(new ProblemEvaluator<...>())
};
```

Comparison evaluations are counted and reported on their own counter, separate
from the algorithm's evaluation count, so analysis can attribute effort to
refinement.

**6. Per-round acceptance versus one final acceptance.**

```csharp
new IteratedRefiner<...>(new ImprovementCheckingRefiner<...>(parameterFitting), rounds: 5)
new ImprovementCheckingRefiner<...>(new IteratedRefiner<...>(parameterFitting, rounds: 5))
```

The first is a memetic hill-climb: each round is kept only if it improves. The
second runs five refinement rounds and accepts or rejects the final result once.
Both are useful, they are genuinely different searches, and the difference is
visible in the configuration. A candidate-transforming evaluator can only express
the second.

**7. Ordered pipeline.**

```csharp
new PipelineRefiner<...>(repair, simplification, new ImprovementCheckingRefiner<...>(parameterFitting))
```

Ordering is semantically significant, and only the stage that needs objective
information carries an evaluator.

One capability is deliberately **not** claimed here: evaluating acceptance
against a different dataset, such as a validation set, is not expressible by
configuration alone. An evaluator receives the problem as a call argument, so it
measures whatever problem the algorithm is solving. Acceptance on a subset of the
objective vector is expressible through `Criterion`; acceptance on different data
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
| Numeric parameter fitting and other numeric refinement | Refiner |
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

#### Transformed operators must be reconsidered as a whole

`TransformedCreator` and `TransformedCrossover` already exist and predate the refiner role. Each takes a source operator plus a `TransformationMutator` and always invokes that mutator on the produced batch. Adding the refiner does not simply extend them with a second accepted child type; it invalidates the concept they were designed around, so increment 13 must re-decide what a transformed operator is rather than bolt a refiner onto the current shape.

Three findings drive that reconsideration.

**The motivating use case moved out from under the feature.** Transformed composition was introduced for postprocessing such as repair and normalization, and [operator-composition.md](../docs/operator-composition.md) still teaches it with a `repairMutator`. Repair, simplification and normalization are refiner work under the role model settled by RF-0, and the [Evaluator contract](#evaluator-contract) relocation table assigns them to the refiner explicitly. The remaining honest mutator case is cross-then-mutate, which the algorithm lifecycle already sequences on its own; the composition earns its keep there only when the pairing must be local, such as one branch of a `ChooseOneCrossover` that needs the mutation while the others do not. Whether that narrow case justifies a general mechanism is itself part of the decision.

**No union type is needed, and none would help.** `IMutatorInstance.Mutate` and `IRefinerInstance.Refine` have identical signatures, and `IOperator<out TExecutionInstance>` is covariant. A shape-only supertype over the two instance interfaces therefore makes both `IMutator` and `IRefiner` usable as one child type by ordinary variance, with no adapter object, no wrapper allocation and no per-call dispatch. A future C# union of the two configuration interfaces would be strictly worse: it would still force a discriminating switch when the child is resolved and again when its instance is invoked. Do not wait for the language feature to decide this.

**A shape-defined operator role is rejected.** Introducing `Transformer` as a tenth role was considered and decided against. It would be the only role defined by its signature rather than its intent, it would need the full topology family that every role carries, and it would create a third answer to the question of what to derive from for a candidate-to-candidate operator, which is precisely the question the role model exists to answer. Adapters between the existing roles, such as `refiner.AsMutator()`, are rejected for a related reason: an adapted refiner would be assignable to `algorithm.Mutator` and would misrepresent its intent at that slot.

The remaining choice is between two shapes:

| Option | Shape | Cost |
| --- | --- | --- |
| Intent-named composites | Keep `TransformedCreator` taking a mutator and add `RefiningCreator` and `RefiningCrossover` taking a refiner | Four near-identical composites; matches the existing convention that roles are deliberately parallel rather than abstracted, as the `Operators/Mutators` to `Operators/Refiners` copy already established |
| Shape-typed child | One composite per producer whose child is typed as the shape supertype, with role-typed entry points such as `TransformWith(mutator)` and `WithRefinement(refiner)` | Adds a member to two public instance contracts, which is breaking for direct implementers unless a default interface member forwards it; renames `TransformationMutator` to `Transformation`; intent moves from the composite's name to the child's type |

The question that decides between them is whether cross-role composition is wanted at all — whether `mutate, then simplify` should be expressible as one operator. `PipelineMutator` accepts mutators only and `PipelineRefiner` refiners only, so it currently is not. If producer composition remains the only need, intent-named composites are sufficient and cheaper; if cross-role pipelines are also wanted, the shape supertype is what makes them expressible.

Answering this needs evidence rather than analysis. RF-5 places refinement in the algorithm lifecycle and RF-6 makes numeric parameter fitting a real refiner, and only then does it become visible whether users reach for producer-attached refinement or whether the algorithm refiner slot already covers it. Increment 13 therefore begins by re-deciding the concept, and only then chooses between the two shapes above. Neither option is foreclosed by waiting: intent-named composites are purely additive, and the shape supertype stays available through a forwarding default interface member.

### Transient-refinement evaluation

The RF-4 evaluator composition refines a candidate temporarily, evaluates the refined candidate through a child evaluator, discards it, and associates its objective vector with the original candidate. This provides Baldwinian refinement without changing the evaluator contract; the same refiner is Lamarckian when an algorithm retains its result. RF-4 settles the public name as `RefinementEvaluator`. Whether it should also accept a mutator or a generic candidate transformation is part of the same open question as the transformed operators; see [Transformed operators must be reconsidered as a whole](#transformed-operators-must-be-reconsidered-as-a-whole).

### Deferred reusable refinement artifacts

A refiner may already have computed information the following evaluation repeats,
such as predictions, residuals, or a loss value. Numeric parameter fitting is the
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
  concurrency setting. A refiner is a natural fit; numeric parameter fitting is
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

### Refiner operator model outcome

RF-3 and RF-4 are implemented as one step. The role's shape was already settled by the
mutator slice, which [developer-guidelines](../docs/developer-guidelines.md) names as the
reference role and [operator-authoring](../docs/operator-authoring.md) explicitly sanctions
copying, so the bulk of the work was a directory copy of `Operators/Mutators` into
`Operators/Refiners` with a `Mutator → Refiner` / `Mutate → Refine` rename. Splitting the
two checkpoints would have separated the copy from the topologies it copies.

Delivered:

- `IRefiner` and `IRefinerInstance` in `HeuristicLib.Contracts`, both contravariant in
  `TSearchSpace` and `TProblem`. The operation is
  `Refine(candidates, random, searchSpace, problem)`; the random number generator is
  retained for parity with the other eight roles, because stochastic refiners exist and
  `SingleCandidateRefiner` forks per-item generators from batch position.
- Authoring bases in all three paths at all three arities: `Refiner`/`RefinerInstance`,
  `StatelessRefiner`, `StatefulRefiner`, plus `SingleCandidateRefiner.RefineCandidate` with
  its `ExecutionConcurrency Concurrency` setting, and `NoChangeRefiner` for identity
  behavior.
- Topologies `WrappingRefiner`, `MultiRefiner`, `PipelineRefiner` (with `Then`),
  `ChooseOneRefiner` (with `WithRate`), `ObservableRefiner` with `IRefinerObserver`, and the
  `CountingRefiner`/`DurationMeasuringRefiner` instrumentation pair.
- `IteratedRefiner`, which restores the capability RF-2 removed with `IteratedEvaluator`. It
  applies its child exactly `Iterations` times with a per-iteration forked generator and
  validates a positive count in the protected post-resolution factory. There is deliberately
  no early exit on an unchanged candidate: candidate equality is not generally meaningful and
  a fixed count keeps the result reproducible.
- `RefinementEvaluator` is the settled name for the transient Baldwinian composition. It
  derives from the `Evaluator` role base rather than `WrappingEvaluator`, because the
  wrapping base's protected overload receives only the resolved child evaluator and cannot
  resolve a second child. It declares `Refiner` as its required constructor child and an
  `Evaluator` setting default-initialized to an ordinary `ProblemEvaluator`, and resolves
  both in `CreateExecutionInstance`. Its XML documentation states explicitly that the
  refined candidates are discarded and never written back, and that the default evaluator
  is unwrapped and therefore invisible to budgets and analysis.
- `ImprovementCheckingRefiner` together with `IImprovementCriterion`, the
  `ImprovementChecking` companion and the six built-in criteria described above, delivered as
  RF-7 in the same pass. See [Improvement-checking refiner outcome](#improvement-checking-refiner-outcome).
- Cross-role plumbing: `ObservationPlanExtensions.Observe` overloads,
  `WithMaxRefinerCalls`/`WithMaxRefinedCandidates`, and both `WithMaxRefinerDuration`
  overloads.

Findings:

- **RF-2 made the Baldwinian composition trivial.** Associating the objective vector with the
  original candidate needs no code at all now that evaluation is fitness-only and positionally
  paired. The refined copies simply go out of scope. Under the previous transforming contract
  this composition would have needed an explicit guard against returning the refined candidate.
- **No analyzer change was needed.** `OperatorAuthoringAnalyzer` keys off `IOperator` and a
  `CreateInitialState` type parameter rather than a role list, so `StatefulRefiner` is covered
  by `HLib0002` and `HLib0004` without modification.
- **The stateless validation gap is inherited unchanged.** `StatelessRefiner` seals
  `CreateExecutionInstance` exactly as `StatelessMutator` does, so a stateless refiner still
  has nowhere to validate configuration. Adding a validation hook would change all nine roles
  at once and remains its own increment. The refiner topologies that need validation
  (`ChooseOneRefiner`, `IteratedRefiner`) validate in their protected overload.
- **No concrete refiner ships in this step.** The encoding-specific mutators were dropped from
  the copy; RF-6 adds the first concrete refiner.
- **`Iterated` is retained deliberately.** It is the exact term for repeated self-composition,
  `refine(refine(refine(x)))` being the n-th iterate of the refiner, and it preserves the
  contrast with `RepeatingEvaluator`, which applies a child to the *same* input repeatedly and
  aggregates. `Recursive` was considered and rejected: the refiner never calls itself, so the
  name would suggest self-reference and a base case that do not exist. The summary states the
  mechanism explicitly so the name does not have to carry it alone.
- **The improvement criterion has exactly one consumer.** `RefinementEvaluator` refines and
  evaluates without comparing, so it needs no criterion and costs one evaluation. The criteria
  therefore live with `ImprovementCheckingRefiner`, which is why RF-7 was pulled into this pass.
- **`ObjectiveVector` reports that it dominates itself.** Found while implementing
  `DominanceCriterion`; recorded as an RF-7 blocker above. It is a pre-existing defect that
  is not fixed here because it also reaches `DominationCalculator` and therefore NSGA-II
  fronts, which is its own change with its own validation.

Validation: Release build clean across the solution; 2134 tests pass across all four test
projects; `dotnet format` whitespace and analyzer checks are clean. `dotnet format style`
reports the same pre-existing IDE0021 warnings in `DataAnalysis/Statistics/Statistics.cs`
recorded for RF-2, in a file untouched by this change.

### Improvement-checking refiner outcome

RF-7 is implemented in the same pass as RF-3 and RF-4, because the improvement
criterion has exactly one consumer and splitting the two would have left a public
strategy interface in the library with nothing using it.

`ImprovementCheckingRefiner` derives from the `Refiner` role base rather than
`WrappingRefiner`, for the same reason `RefinementEvaluator` does not use
`WrappingEvaluator`: the topology base's protected overload receives only the resolved
child of its own role and cannot resolve the evaluator. Its three children are named for
the part they play: `Refiner` as the required constructor child, plus the `Evaluator` and
`Criterion` settings. `Refiner` rather than `ChildRefiner`, because the type is not a
topology base and the name should say what the child is rather than restate that it is a
child; this mirrors `RefinementEvaluator.Refiner`.

Both settings are default-initialized rather than nullable, matching the decision made
for `RefinementEvaluator.Evaluator`: `Evaluator` defaults to an ordinary
`ProblemEvaluator` and `Criterion` to `ImprovementChecking.Default`. This supersedes the
"two nullable settings" wording above. A default value is a value, so structural
equality still distinguishes configurations correctly, and there is no null branch to
document.

Findings:

- **Original-on-failure needs no code.** A refiner that cannot improve a candidate
  returns it unchanged, so its objective vector is unchanged and the criterion keeps the
  original. The plan's requirement that failure never produces a worse candidate falls out
  of the comparison rather than needing a failure channel on the refiner contract.
- **The batch contract is checked.** A child refiner returning a different number of
  candidates is a programming error and throws `InvalidOperationException` from the role
  method, which is where operation-input validation belongs.
- **Composition order is observably different.** A refiner that reaches a better candidate
  only by passing through a worse one settles the plan's claim that the two nestings are
  genuinely different searches: accepting each round rejects the uphill step and never
  arrives, while accepting once at the end keeps the better result. Pinned by a test.
- **`NotWorse` versus `StrictlyBetter` needs a lateral move to be observable.** The test
  minimizes an absolute value and negates the candidate, so the objective vector is
  unchanged while the candidate is not; only `NotWorse` takes the move.
- **Evaluation accounting is verified, not assumed.** `OperatorBudgetAlgorithm` registers its
  counting replacement against the observed operator *instance* in a child registry, and
  `ExecutionInstanceRegistry` resolves replacements by reference. A refiner holding the same
  evaluator instance therefore resolves the counted replacement too, so its comparison
  evaluations count against the budget that decides when the run stops. A refiner left on its
  inherited default holds a different instance and stays outside the budget. Both directions
  are pinned by tests, and [operator-composition.md](../docs/operator-composition.md) documents them.
- **The two blockers recorded above are now resolved.** Both were fixed in the objective
  ordering layer rather than in the criteria; see
  [Objective ordering outcome](#objective-ordering-outcome).

### Objective ordering outcome

The two RF-7 blockers were both defects in how objective values are ordered, so both were
fixed at that layer and the criteria were left unchanged.

- **One ordering primitive.** `ObjectiveValue.Compare(left, right, direction)` is now the
  single place objective values are ordered; a negative result means `left` is better.
  `ObjectiveVector.CompareTo`, `ObjectiveValue.CompareTo`, `SingleObjectiveComparer`,
  `LexicographicComparer`, `WeightedSumComparer` and `HyperVolumeCalculator.DimensionComparer`
  all delegate to it. Five of those six previously ranked `NaN` best when minimizing and
  worst when maximizing.
- **`NaN` is the worst value in both directions**, ranked before the direction is applied.
  The infinities remain ordinary participants and therefore swap roles with the direction:
  negative infinity is the best value of a minimized objective and the worst ordered value
  of a maximized one. Only `NaN` sits outside the order.
- **`NaN` was deliberately not made `Incomparable`.** `IComparer<ObjectiveVector>` has no
  incomparable result, so that choice would give `NaN` one meaning under dominance and
  another under a total order. It would also make a `NaN` candidate non-dominated, so
  NSGA-II would preserve it in the first front, and it would make repairing a `NaN`
  candidate back to a finite value invisible to `ImprovementCheckingRefiner` — which is
  exactly the case RF-6 numeric parameter fitting needs to work.
- **`DominanceRelation` reordered and renamed.** `Incomparable` is now the zero value, so a
  defaulted value neither promotes nor eliminates a candidate; `Equivalent` became `Equal`,
  which states the componentwise equality the code actually checks. Members are declared in
  order of increasing strength of verdict and carry the literature notation (`‖`, `∼`, `≺`)
  in their documentation. Note that `Equal` is deliberately *not* covered by `Incomparable`,
  unlike HeuristicLab's single `IsNonDominated`.
- **`BestValue`/`WorstValue` are now `∓∞`/`±∞`** rather than `double.MinValue`/`MaxValue`, so
  they are genuine bounds of the ordered range — an objective value can legitimately be
  infinite, and an extreme finite sentinel is beaten by one. They had no production callers.
  `RegressionMetricExtensions.ToFinite` needs a finite replacement and now derives one
  privately, guarded by the existing `double.IsFinite` check.

Findings:

- **The self-dominance fast path was pure overhead.** For a self-comparison the loop reaches
  componentwise equality unaided, `NaN` included, so the shortcut only skipped work — and
  skipped the argument validation with it.
- **`MaxValue` never offered the arithmetic safety it appeared to.** `MaxValue - MinValue`
  is `3.595e308`, which overflows to `+∞`, so range and normalization computations break
  identically under both choices. The infinities also already enter through real objective
  values, so a finite sentinel protects nothing.
- **Three pre-existing arithmetic hazards were found and fixed alongside.** None was caused
  by the ordering changes; all three were reachable from a non-finite objective value, which
  symbolic regression produces.
  - `WeightedSumComparer` turned a zero weight on an infinite objective into `0 × ∞ = NaN`
    and ranked the vector last, so excluding an objective could decide the ranking. A zero
    weight now contributes nothing. Its `directedWeights` are also hoisted into the
    constructor: they are loop-invariant but were rebuilt per comparison, and with the
    `RealVector` operators that cost six allocations on every call inside a sort.
  - `ProportionalSelector` propagated `NaN` through `Math.Min`/`Math.Max` into both window
    bounds, so one unusable fitness made every selection weight `NaN`. Bounds are now taken
    over the ordered values only, a `NaN` fitness gets no share, an infinitely bad fitness is
    clamped to zero instead of a negative share, and an infinite share restricts the draw to
    the infinite entries uniformly, which is its limit.
  - `CrowdingDistance` sorted `NaN` first through `IndexedComparer`, making it a boundary
    point with infinite crowding distance — so NSGA-II preferred it in a tie — while the
    resulting `NaN` range defeated the `range <= 0.0` guard and produced `NaN` distances for
    the whole dimension. `NaN` now sorts last and is excluded from the dimension, and a
    non-finite range skips it.
- **The roulette wheel had two defects of its own**, both surfaced by the tests for the
  above. Its cumulative comparison was exclusive, so a candidate with no share was selected
  whenever the draw landed exactly on its cumulative sum — including a draw of zero against a
  leading zero share. Shares are also rescaled by the largest one before the draw, because
  shares derived from very large fitnesses summed to infinity, and `0 × ∞` then made the draw
  itself `NaN`.

`HyperVolumeCalculator` seeded its region bound with a magic `1E15`, leaving any objective
value above it untracked. It is the first consumer of the new sentinel and now seeds with
`ObjectiveValue.WorstValue(ObjectiveDirection.Minimize)`; that path is minimization-only and
already returns early for an empty front, so the seed is always overwritten.

#### Deferred infinity arithmetic

Two places still produce `NaN` from arithmetic on genuinely infinite objective values. Both
are cases where the arithmetic gives the answer it should, so neither is a defect on the
terms above, and both are deferred to their own branch rather than widening this one:

- `ObjectiveVector.Add` computes `∞ + (−∞)`, which is undefined.
- `OpenEndedRelevantAllelesPreservingGeneticAlgorithm.Combine` interpolates two objective
  vectors; its `0 × ∞` endpoints are already short-circuited, but an infinite objective at an
  intermediate strictness still propagates.

Validation: Release build clean across the solution; 2169 tests pass across all four test
projects; `dotnet format` whitespace and analyzer checks are clean.

### Numeric parameter-fitting refiner outcome

RF-6 is implemented. `NumericParameterFittingRefiner` lives in
`Operators/Refiners/SymbolicRegressionRefiners` and derives from the three-arity
`SingleCandidateRefiner<ExpressionTree, ExpressionTreeSearchSpace, SymbolicRegressionProblem>`.
It is bound to the problem type because it needs `SymbolicRegressionProblem.TrainingData`; the
namespace is named for symbolic regression rather than symbolic expressions for the same reason,
while the operator role stays the primary grouping as the ownership map requires.

Its whole body delegates to the existing internal component, which is the point: RF-6 adapts
numeric parameter fitting to the role rather than reimplementing it.

```csharp
public override ExpressionTree RefineCandidate(ExpressionTree candidate, IRandomNumberGenerator random, ExpressionTreeSearchSpace searchSpace, SymbolicRegressionProblem problem) =>
    NumericParameterFitter.TryFit(candidate, problem.TrainingData, MaximumIterations, out var fittedExpression)
        ? fittedExpression
        : candidate;
```

Decisions:

- **Failure is split by whether it describes the candidate or the configuration.** An unsuccessful
  numerical solve is an ordinary per-candidate outcome, so that candidate is returned unchanged, as
  RF-7 established for a refiner that cannot improve its input. An undifferentiable operation or an
  unbound variable throws instead: both follow from the search space and the training data, so every
  affected candidate fails identically and returning them unchanged would leave the refiner a silent
  no-op for a whole run. `NumericParameterFitter.CreateException` is now shared with the throwing
  `Optimize` form so the two do not restate the mapping. The identity shortcuts inside
  `NumericParameterFitter` mean an expression with no evolvable constants, and a zero iteration count,
  return the *same instance* rather than a copy.
- **No problem-objective retention**, as the checkpoint requires. The fit minimizes mean squared
  error against raw training targets, which need not be the problem objective, so the fitted
  expression is returned without comparison. A test pins this using linear scaling, where the
  unfitted expression already evaluates perfectly: the refiner still replaces it, and the same
  configuration under `ImprovementCheckingRefiner` keeps the original instead.
- **`MaximumIterations` defaults to `5`.** Zero is a documented identity, and a negative value throws
  from the role method — the stateless bases seal `CreateExecutionInstance`, so there is nowhere to
  validate at configuration time. This is the "check in the role method" branch of the gap RF-1
  recorded.
- **Linear scaling stays an evaluation concern.** The fit runs against raw targets even where the
  problem applies scaling when it evaluates, exactly as Stage 4 specifies.
- **Concurrency is inherited** from `SingleCandidateRefiner`, defaulting to sequential. A
  Levenberg-Marquardt solve per candidate is expensive, so this is the setting users will reach for.

Findings:

- **The random generator is unused.** Numeric parameter fitting is deterministic. The parameter is
  retained for parity across the nine roles, as RF-3 decided.
- **Cancellation is not reachable.** `NumericParameterFitter` accepts a `CancellationToken`, but no
  operator role signature carries one, so the refiner passes `default`. Recorded in the
  [developer backlog](../docs/developer-backlog.md) as a general operator question rather than
  solved here, because it interacts with the run-scoped execution-policy item already open there.
- **No problem-bound construction helper.** One was written and removed: the repository's
  `For(problem, ...)` convention exists to infer generic arguments, and `NumericParameterFittingRefiner`
  is not generic, so the helper took a problem it never used. Direct construction is clearer.
- **Fitting a proportion of candidates needed no new API.** `refiner.WithRate(0.25)` already composes a
  `ChooseOneRefiner` against `NoChangeRefiner`, so the HeuristicLab option for refining only some
  candidates is expressible without a setting on this refiner.
- **Row sampling is an optional dataset rather than a percentage.** HeuristicLab fits constants on a
  configurable percentage of rows. A percentage cannot state *which* rows — resampled per candidate or
  fixed, stratified, contiguous, driven by which generator — so `FittingData` takes a
  `RegressionData` instead, with `null` meaning the problem's training data. Any sampling policy is
  then expressible by the caller, the fitted rows stay explicit, and a percentage convenience remains
  available later on top of it. The subset is fixed for the configuration, so every candidate is
  fitted to the same rows; a fresh draw per candidate is deliberately not offered, because it costs a
  binding per candidate for a benefit a fixed representative sample already provides. Being a
  dataset, the setting takes part in configuration equality by reference rather than by value.
  Sampling *candidates* rather than rows needs nothing new: `ChooseOneRefiner.WithRate` against
  `NoChangeRefiner` already refines a proportion of a batch.
- **Placement is unresolved and recorded in the backlog.** `Operators/Refiners` now holds both
  general role machinery and a leaf only symbolic-regression users can instantiate. The three-tier
  taxonomy — general, candidate-specific, problem-specific — and its options are in the
  [developer backlog](../docs/developer-backlog.md); the current location follows the existing
  role-first ownership rule until that decision is made.

Validation: Release build clean across the solution; 2178 tests pass across all four test projects;
`dotnet format` whitespace and analyzer checks are clean.

### Algorithm integration outcome

RF-5 is implemented. Six built-in algorithms gained a nullable
`IRefiner<TCandidate, TSearchSpace, TProblem>? Refiner` setting: `GeneticAlgorithm`, `NSGA2`,
`EvolutionStrategy`, `AlpsGeneticAlgorithm`,
`OpenEndedRelevantAllelesPreservingGeneticAlgorithm`, and `HillClimber`. Each resolves it in
`CreateExecutionInstance` and applies it as `refiner?.Refine(...) ?? candidates` immediately before
evaluation, at every point where candidates are produced.

Decisions:

- **Nullable rather than a default `NoChangeRefiner`.** `null` means no refinement, matching the
  existing `Terminator` setting, and avoids resolving an execution instance for an operator that
  would do nothing. A test pins that an unset refiner leaves the run bit-identical, which matters
  because refinement must not perturb reproducibility of existing configurations.
- **Every production site is covered, not only the last one.**
  `OpenEndedRelevantAllelesPreservingGeneticAlgorithm` creates candidates at three points — the
  initial population, a repopulation branch when the population empties, and offspring — and all
  three refine. `HillClimber` refines its initial candidate and each neighbor batch.
- **Carried candidates are never re-refined.** Elites reach `ElitismReplacer` from the previous
  population and never pass through the refiner, and `HillClimber`'s incumbent is likewise
  untouched. This falls out of placing refinement on the production path rather than the
  replacement path, and is pinned by a counting test with two elites.
- **`ParameterlessPopulationPyramid` is excluded** because its implementation is entirely commented
  out; it is a stub rather than a working algorithm.
- **The algorithm builders are deliberately not extended.** Whether builders should gain full
  feature parity or be phased out is an open backlog decision, so adding a refiner step to them
  would prejudge it. `Refiner` is set through object initializers and `with` expressions.

Findings:

- **`GeneticAlgorithm` had a misnamed local, now removed.** `offspringSize` held
  `populationSize * 2` but was passed to the selector as the number of candidates to select, making
  it the mating pool size. `ToParentPairs` then halved it (`parents.Count / 2`) and crossover
  produced one offspring per pair, so a generation yields `populationSize` offspring, not
  `offspringSize`. The counting refiner measured 20 refined candidates over four generations at a
  population size of five, which is `5 + (3 × 5)`, and that is what exposed the name. The local is
  gone and `populationSize * 2` is now inline at the selection call, matching `NSGA2`; behavior is
  unchanged.
- **Refinement placement is easy to get wrong at the creation sites.** Where creation and refinement
  are combined in one expression, it is simple to write `creator.Create(...)` on both sides of a
  null check and consume randomness twice, which would silently break reproducibility for runs with
  no refiner configured. Every site binds the created batch to a local first and refines it in a
  separate statement.

The complete workflow is demonstrated by `NumericParameterFittingSpecs` in the API usage specs: a
`GeneticAlgorithm` over a `SymbolicRegressionProblem` with the refiner in its `Refiner` slot, plus
the improvement-check, rate-limited and row-subset compositions. The first spec runs the same seed
with and without the refiner and asserts the fitted run reaches a lower mean squared error, so it
fails if parameter fitting stops contributing rather than merely stops throwing.

Validation: Release build clean across the solution; 2195 tests pass across all four test projects;
`dotnet format` whitespace and analyzer checks are clean.

### Complete operation coverage outcome

The AD engine originally supported addition, subtraction, multiplication, division, negation, `exp`,
`log`, `sin`, `cos`, `tan` and `tanh`, and reported every other built-in operation as an unsupported
model. That gap was found through the Python interop: all three symbolic-regression entry points put
`Symbols.SquareRoot` in their symbol set, so with the refiner throwing on undifferentiable
operations, a run would have failed as soon as any candidate used it. The mixed-search-space case
that RF-6 recorded as speculative was in fact the first real consumer.

Every remaining built-in operation now has a differentiation rule:

| Operation | Partials |
| --- | --- |
| `Sqrt(x)` | `0.5 / output` |
| `Abs(x)` | `sign(x)` |
| `Square(x)` | `2x` |
| `Cube(x)` | `3x²` |
| `CubeRoot(x)` | `1 / (3 · output²)` |
| `Power(a, b)` | `b · a^(b-1)` and `output · ln(a)` |
| `Root(a, b)` | `(1/b) · a^(1/b - 1)` and `-output · ln(a) / b²` |
| `AnalyticQuotient(a, b)` | `1 / sqrt(1 + b²)` and `-a · b / (1 + b²)^(3/2)` |

Decisions:

- **`Abs` uses the zero subgradient at the origin.** The derivative does not exist there; zero is the
  choice the established automatic-differentiation frameworks also make, and it keeps the value finite
  where the one-sided limits disagree.
- **`Power` and `Root` propagate `NaN` for a nonpositive base** in their exponent partial, because
  `ln(a)` is undefined there. This is the engine's ordinary IEEE behavior rather than a special case.
- **The three binary operations use scalar loops** rather than `TensorPrimitives`. `Pow` is
  transcendental and the analytic quotient carries a square root, so neither maps onto a clean vector
  kernel; the AD design contract allows a clear scalar loop where SIMD does not fit.
- **Output-based rules reuse the retained primal** for `Sqrt` and `CubeRoot` rather than recomputing a
  root, matching the existing `Tanh` shape.

Findings:

- **The unsupported-operation failure path is now unreachable through built-in symbols.** Every opcode
  carrying arity metadata has a rule, and `OpCode.Invalid` throws from `OpCodes.GetArity` before the
  differentiability check, so it cannot stand in for one. Five tests existed only to drive that path
  and were removed; `EveryBuiltInOperationIsDifferentiable` replaces them by enumerating `OpCode` and
  asserting each compiles, which fails as soon as an opcode is added without a rule.
- **Coverage was lost and is not replaced.** The refiner's throw-on-undifferentiable-operation
  behavior no longer has a test, because nothing built-in can trigger it. The behavior still matters
  for a user's custom symbol emitting a rule-less opcode.
- **The Python demonstrator's blocker closed as a side effect.** Its default symbol set is fully
  differentiable, so the throw policy no longer fires there.

### Python interop outcome

Numeric parameter fitting is re-activated on the two Python entry points that can host it.

- **`InteractiveSymbolicRegression.Run`** builds its algorithm over `SymbolicRegressionProblem`
  directly, so the refiner drops into the `Refiner` slot with `MaximumIterations` taken from the
  existing `ParameterOptimizationIterations`. Its guard is gone. The demonstrator shipped a default of
  `5` in three places while the guard rejected anything above zero, so it could not run with its own
  defaults; that is now correct rather than fatal.
- **`PythonGenealogyAnalysis.RunAlgorithmConfigurable`** gained a `TProblem` type parameter and an
  optional refiner argument. `ExperimentParameters` was deliberately *not* changed: its operators stay
  declared over `IProblem`, and operator-role contravariance lets them fill the more specific slots.
  The builders take explicit type arguments so `TProblem` comes from the problem rather than being
  inferred from the operators, and the refiner is attached after `Build()`. The other three callers
  needed no edits. `ProblemGeneration`'s guard is gone; a problem factory could not have owned a
  refiner in any case.

Two entry points remain blocked. `ExtendedSymbolicRegressionProblem` and
`PythonInterOptEquationScoring` *compose* a `SymbolicRegressionProblem` as an inner problem rather
than deriving from it, so a refiner declared over `SymbolicRegressionProblem` does not fit their
algorithms and contravariance does not bridge a composition boundary. Their guards were restored as
`NotSupportedException` with the reason stated, rather than accepting the parameter and ignoring it.
Numeric parameter fitting is a symbolic-regression capability, so this is a limitation of those two
wrapper problems rather than a reason to loosen the refiner. Revisit only if a real need appears.

### Integration hardening plan

RF-8 is the closing checkpoint of the refinement track. It is verification and
hardening of what RF-3 through RF-7 delivered, not new capability. It adds no
refiner type, changes no operator contract, and does no performance work;
producer composition remains increment 13 and measurement remains increment 14.
Where a check finds a genuine defect or an unsettled rule, RF-8 fixes it in the
existing code or records an explicit decision, and states which.

The refinement track already carries substantial focused coverage:
`RefinerCompositionTests`, `RefinerConfigurationEqualityTests`,
`SingleCandidateRefinerTests`, `ImprovementCheckingRefinerTests`,
`ImprovementCriterionTests`, `RefinementEvaluatorTests`,
`NumericParameterFittingRefinerTests`, `AlgorithmRefinementTests`, and
`NumericParameterFittingSpecs`. RF-8 therefore targets the seams between those
units rather than restating them: the batch contract as a shared rule, the
accounting behavior the composition examples promise, the algorithms RF-5
touched but did not pin, and the failure and cancellation behavior that only
appears in a run.

Work groups are ordered so that the cheap structural checks run first and the
decision-bearing ones run once the surrounding behavior is pinned.

#### H-1 Batch semantics across every topology

The batch an operator receives is a population, not a positional tuple. An
operator maps an input population to an output population, and how it pairs
inputs or how many inputs contribute to one output is that operator's own
semantics. A crossover may recombine three parents into one offspring or two
parents into two; a mutator normally returns one candidate per input but may
conceptually return a differently sized population; a refiner in its general
form takes a population and returns a refined population.

Equal input and output size is therefore **not** a general constraint of the
refiner role, and RF-8 does not add a uniform guard. An operator that genuinely
depends on positional pairing checks it locally, at the point where the
dependency exists and where the message can name what it needed.

`ImprovementCheckingRefiner` is such an operator and already checks: its
accept-or-reject loop indexes candidates, refined candidates, and both objective
vector lists together, so a differently sized result would mispair or overrun.
Its existing `InvalidOperationException` stays as the model for a local check.

`RefinementEvaluator` is the second such operator, and does not check today. It
returns `evaluator.Evaluate(refined, ...)` directly, so a child refiner that
changes the size makes the evaluator return a different number of objective
vectors than the caller supplied candidates. The evaluator contract *is*
positional, so this is a genuine local requirement rather than a general one:
add a check with a message naming the refined size, the supplied size, and the
reason the evaluator needs them equal.

The algorithms need no such check. They apply `refiner?.Refine(...) ?? candidates`
on the production path, and a refiner that returns a differently sized population
is an ordinary outcome there; the replacer already decides the surviving
population size.

The work:

- Pin the general semantics: a size-changing refiner passes through
  `PipelineRefiner`, `IteratedRefiner`, `ChooseOneRefiner`, `WrappingRefiner`,
  `ObservableRefiner`, `CountingRefiner` and `DurationMeasuringRefiner` without
  any of them objecting, and the counting and duration instrumentation reports
  the sizes it actually saw.
- Pin the local requirements: `ImprovementCheckingRefiner` keeps its check, and
  `RefinementEvaluator` gains an equivalent one.
- Pin that a size-changing refiner inside a real algorithm run is accepted and
  that the run continues with the population the refiner returned.
- Cover empty, single-element and multi-element batches for every topology,
  since the empty batch is the case each topology handles separately.
- Pin that a refiner which changes nothing returns reference-identical
  candidates, because elitism, caching keys and improvement checking all read
  identity rather than value equality.

#### H-2 Refiner and evaluator composition accounting

Turn the seven composition examples into executable tests. Examples 3, 5, and 6
have coverage today; the rest do not.

- Example 1: a refiner with its own evaluator keeps comparison evaluations out
  of an algorithm's `LimitEvaluator` budget. Today only the operator-budget
  observer form is covered.
- Example 2: one shared `LimitEvaluator` instance yields one counter, so a run
  with refinement reaches the budget in fewer generations than the same run
  without it.
- Example 4: `LimitEvaluator(CachingEvaluator(...))` and
  `CachingEvaluator(LimitEvaluator(...))` differ in whether cache hits consume
  budget. Pin both.
- Example 7: an ordered pipeline in which only the stage that needs objective
  information carries an evaluator.
- The accounting rule itself: `ExecutionInstanceRegistry` resolves by reference
  identity, so one shared configuration object gives one counter and one cache,
  while two structurally equal but separately constructed configurations give
  two. Every example depends on this; pin it directly.
- `RefinementEvaluator` inside a real run: the population keeps the original
  candidates while their objective vectors come from the transient refined
  copies, and sharing the algorithm's evaluator instance merges the counters.

#### H-3 Algorithm integration completeness

RF-5 added the `Refiner` setting to six algorithms; `AlgorithmRefinementTests`
pins `GeneticAlgorithm` and `HillClimber` in detail and only checks that
`EvolutionStrategy` and `NSGA2` accept a refiner.

- Add `AlpsGeneticAlgorithm` and
  `OpenEndedRelevantAllelesPreservingGeneticAlgorithm`. The latter refines at
  three production sites; the repopulation branch needs a configuration that
  actually empties the population, which is the site most likely to be missed.
- Extend the carried-candidate rule beyond `GeneticAlgorithm` elites to the
  plus-strategy parents of `EvolutionStrategy` and to candidates carried between
  ALPS layers.
- Parameterize the unset-refiner reproducibility check over all six algorithms.
  It guards the property that made the creation sites delicate: refinement must
  not consume randomness when no refiner is configured.

#### H-4 Objective directions and acceptance criteria in composition

`ImprovementCriterionTests` covers the criteria in isolation. RF-8 covers their
wiring.

- `ImprovementCheckingRefiner` on a maximized objective keeps the higher
  objective vector, through a run rather than a direct call.
- Default criterion resolution from the problem, single objective to strict
  order, Pareto to dominance, and a configured total order to that order,
  observed through the refiner.
- `MinimumImprovement` and `MinimumRelativeImprovement` through the refiner, one
  case each, including a rejected marginal improvement.
- One multi-objective run under NSGA2 where acceptance is dominance-based.

#### H-5 Failure behavior

- A refiner that throws inside a run propagates out of `Complete` and of the
  streaming enumeration, and instrumentation wrappers neither swallow it nor
  lose their recorded state. The operator-level halves of this are covered; the
  run-level halves are not.
- `NumericParameterFittingRefiner` throws on configuration-level failures, an
  undifferentiable operation or a variable the fitting data does not supply, on
  the first affected candidate rather than after a full generation. RF-6
  recorded that no built-in symbol can reach the undifferentiable path any more,
  so this needs a custom symbol emitting a rule-less opcode. This restores the
  coverage RF-6 recorded as lost.
- A non-converging solve stays a per-candidate no-op when it happens inside a
  run, not only at the component boundary.
- Decide and pin how an exception from one candidate surfaces when `Concurrency`
  is parallel, so that the failure a user sees is deterministic rather than
  dependent on scheduling.

#### H-6 Topology round-out

- `IteratedRefiner` at its boundaries, and nested pipelines containing wrapping
  refiners.
- Observer semantics for a refiner nested inside a pipeline or an iterated
  refiner: pin whether observers fire once per outer call or once per inner
  stage, since both are defensible and only one is implemented.
- `ChooseOneRefiner` reproducibility, where one seed gives one selection
  sequence and `WithRate(1.0)` behaves as the bare refiner.

#### Out of scope for RF-8

**Cancellation.** `IRefinerInstance.Refine` takes no `CancellationToken`, so a
run's token is honored at iteration boundaries through `RunStreamingAsync` and
`CancellationTokenTerminator`, never inside a refiner call.
`NumericParameterFitter.TryFit` accepts a token, but
`NumericParameterFittingRefiner` has no way to supply one, so a long
Levenberg-Marquardt solve cannot be interrupted once it starts. This is recorded
rather than addressed: adding a token to the operator contract would touch every
operator role, and a per-refiner setting is a separate decision. Neither belongs
in a hardening checkpoint.

**Documentation and usage specs.** The user-facing explanation of refinement is
tracked separately as RF-9, so that RF-8 stays a behavioral checkpoint. RF-8
still updates this plan's outcome section and the parity-matrix row, which are
checkpoint bookkeeping rather than user documentation.

#### Validation

Run `HeuristicLib.Tests` throughout, and the API usage, experimental and
scenario suites once at the end. Formatting, style, and analyzer checks must be
clean before review.

### Integration hardening outcome

RF-8 is implemented. The refinement track's units were already well covered, so the work landed on the
seams between them: batch semantics, evaluator accounting, the algorithms RF-5 wired but did not pin,
acceptance wiring, and failure behavior inside a run.

Decisions:

- **Batch size is not a refiner contract.** An operator maps an input population to an output
  population; how it pairs inputs and how many outputs it returns is that operator's own semantics, as
  it already is for a three-parent crossover or a two-offspring one. RF-8 therefore added no uniform
  guard. `IRefiner` now says this, and the composition topologies pass a resized population through
  unchanged.
- **Positional requirements are local and stated where they exist.** Three operators have one.
  `ImprovementCheckingRefiner` indexes candidates against objective vectors and already checked.
  `ChooseOneRefiner` restores input order through `WeightedBatchDispatcher`, which already checked and
  documents the requirement. `RefinementEvaluator` did not check and now does: it owes its caller one
  objective vector per supplied candidate, so a resizing refiner throws there instead of returning a
  miscounted result that the algorithm would have tripped over later.
- **Algorithms need no such check.** A refiner that returns a differently sized population on the
  production path is an ordinary outcome; the replacer decides the surviving population. This is
  pinned rather than merely assumed.
- **Parallel batching keeps the execution layer's exception convention.** A candidate failure under
  `ExecutionConcurrency.Concurrent` surfaces as an `AggregateException` wrapping the original, which is
  the TPL behavior `BatchExecutionTests.Parallel_FollowsTplExceptionBehavior` already pins for every
  batching operator. Refiners inherit it rather than introducing a second convention; sequential
  batching throws the original exception. Both are deterministic, which was the actual requirement.

Findings:

- **`RefinementEvaluator` was the one composition that could silently break a contract.** Everything
  else either checked already or had no positional requirement. Fixed with a check and a remark.
- **The undifferentiable-operation failure is unreachable by construction, not merely by built-in
  symbols.** RF-6 recorded the lost coverage as still mattering "for a user's custom symbol emitting a
  rule-less opcode". It cannot: `OpCodes.GetArity` throws for any opcode without metadata, including
  undefined enum values, before the differentiability check runs, and RF-6 gave every opcode with
  metadata a rule. The guard in `DifferentiableExpressionCompiler` stays as future-proofing for an
  opcode added without a rule, and `EveryBuiltInOperationIsDifferentiable` fails the moment that
  happens. RF-8 covers the configuration failure that *is* reachable instead: a variable the fitting
  data does not supply, at the refiner and through a run. Whether the design should change to make the
  path reachable was reviewed separately; see
  [Undifferentiable operations: reachability decision](#undifferentiable-operations-reachability-decision).
- **A misconfigured fitting dataset is invisible without evolvable constants.** The refiner's identity
  shortcut returns candidates untouched when an expression has no evolvable constant, so a search space
  configured with only fixed constants never reaches the solver and never reports the mismatch. This
  surfaced while writing the run-level failure test, which needed `EvolvableConstantSymbol` in the
  search space to fail at all. It is correct behavior, but worth stating where the refiner is
  documented.
- **`GeneticAlgorithm` was the only algorithm whose refinement sites were pinned in detail.** ALPS, the
  open-ended algorithm, and the plus-strategy carry rule of `EvolutionStrategy` were unverified. The
  open-ended algorithm's repopulation branch needed a refiner that keeps every offspring from
  dominating its parents before the branch could be reached at all.

#### Undifferentiable operations: reachability decision

The unreachable failure path raised the question of whether the design should change to make it testable.
It should not, for a reason that only became clear on inspection: the path is dormant rather than dead.
Stages 5.4 and 5.6 add vectorial and interval operations, and the first opcode that lands without a
differentiation rule makes the branch live. `EveryBuiltInOperationIsDifferentiable` fails at that moment,
which forces the author to either add the rule or exclude the opcode and write the test that is possible
by then.

Three layers were involved, and only one of them was actually untestable:

| Layer | Reachable |
| --- | --- |
| The compiler recording an `ExpressionCompilationFailure` | No. An opcode without metadata throws from `OpCodes.GetArity` first, and every opcode with metadata has a rule. |
| `NumericParameterFitter.CreateException` mapping it to a `NotSupportedException` | Yes, from a constructed failure. It was simply untested. |
| The refiner reporting it instead of skipping the candidate | Only because the decision was inline behind a static call. |

The exception mapping is now covered from a constructed compilation failure, which is the one layer that
carries real risk: it asserts the exception type and the symbol and operation the message names.

The refiner's classification stays an inline type test. Extracting it into a named member was tried and
reverted: with one call site, a test over the extracted member restates its own one-line body and can
only fail when that body is edited, while both branches the refiner can actually reach are already
covered by behavior tests, a non-converging solve that skips a candidate and an unbound variable that
throws. The branch that remains untested is the one no expression can produce.

Rejected alternatives:

- **Delete the path.** Everything involved is internal, so removal would cost nothing externally, but
  Stage 5 reactivates it and re-adding structured failure handling later costs more than keeping it.
- **Give operations extensible identity** so a custom symbol could emit an operation the engine does not
  know. This would make the failure genuinely reachable from user code, but it contradicts the AD design
  contract, which excludes runtime registries and dispatch in hot loops, and the closed opcode set is what
  keeps the emission switch fast. Worth revisiting only if third-party primitive operations become a
  product goal, which is not a testing decision.
- **Report an undefined opcode as a compilation failure** instead of an argument exception. This conflates
  a malformed symbol, which also breaks the ordinary interpreter, with a model limitation, and it cannot
  work mechanically: the placeholder substitution that keeps the value stack balanced needs the arity that
  an unknown opcode does not have.

Deriving the compiler's supported-operation list from one operation catalog, rather than maintaining it
beside the emission switch, belongs to the deferred operation-model consolidation review. It would make
the drift this branch guards against impossible by construction.

Coverage added:

| Group | Where |
| --- | --- |
| H-1 batch semantics | `RefinerBatchSemanticsTests`, plus a resizing case in `AlgorithmRefinementTests` and `RefinementEvaluatorTests` |
| H-2 evaluator accounting | `RefinerEvaluatorAccountingTests`, plus the Baldwinian run case in `AlgorithmRefinementTests` |
| H-3 algorithm integration | `AlgorithmRefinementTests`: ALPS, the open-ended algorithm including repopulation, plus-strategy carry, and a six-algorithm reproducibility check |
| H-4 acceptance wiring | `ImprovementCheckingCompositionTests` |
| H-5 failure behavior | `RefinerFailureTests`, `NumericParameterFittingRefinerTests`, and the failure-classification and exception-mapping tests in `NumericParameterFitterTests` |
| H-6 topology round-out | `RefinerCompositionTests` |

Validation: Release build clean across the solution; 2022 core, 120 API usage, 64 experimental and 23
scenario tests pass; `dotnet format` whitespace, style and analyzer checks report nothing in the
touched files.

### Refinement documentation outcome

RF-9 is implemented. Its scope turned out to be much narrower than the checkpoint described, because most
of what it planned to write already existed. `docs/operator-composition.md` carried the refiner
topologies, the improvement-checking criteria table, both nesting orders, the reference-identity
accounting rule with its shared-counter and shared-cache configurations, the terminator arrangement, and
transient refinement evaluation. The glossary carried entries for both refiner and numeric parameter
fitting, and the developer backlog already recorded the open cancellation and validation-phase decisions.
The RF-8 outcome's claim that the accounting story was explained only in this plan was wrong.

What was genuinely missing was the behavior RF-8 settled, plus the places refinement had never been
described at all:

- **Batch semantics.** `docs/operators.md` gains a section stating that a batch is a population, that
  returning one output per input is the common case rather than a rule of the operator model, and which
  operators hold a positional requirement of their own.
- **Budget wrapper order.** The accounting section documented counting relative to a cache but not
  limiting relative to one, which decides whether a refiner's repeat evaluation spends from the budget.
- **Algorithm placement.** `docs/algorithm.md` had no refinement content at all. It now names the six
  algorithms carrying a `Refiner`, states that refinement sits on the production path and therefore never
  reaches carried elites, plus-strategy parents or a hill climber's incumbent, and that an unset refiner
  consumes no randomness.
- **Instrumentation position.** `docs/observability-and-analysis.md` gains the rule that a counter reports
  what its own position sees, with the iterated-refiner example, and that item counters report the
  population an operator returned rather than the one it received.
- **The fitting-data caveat.** `NumericParameterFittingRefiner.FittingData` documents that a mismatch is
  reported when the first affected candidate is refined, and stays unreported entirely while the search
  space offers no evolvable constant.
- **The accounting spec.** `NumericParameterFittingSpecs` gains a spec that runs the same search twice,
  sharing the evaluator with the improvement check in one and not the other, and asserts the difference in
  the counters.

Deliberately not written: any statement about what refinement costs in time. That belongs with the
measurements from the performance increment rather than with an estimate. Since written: the [whole-run
profile outcome](#whole-run-profile-outcome) supplies those measurements, and
`docs/operator-composition.md` now carries the cost statement they support.

Not taken in review: a cancellation-boundary paragraph in `docs/execution-model.md` and a batch-semantics
sentence in the glossary refiner entry. `docs/algorithm.md` states the boundary where refinement is
configured, and `docs/operators.md` carries the batch semantics, so both subjects are documented; whether
the general execution page should also state the operator-level cancellation boundary remains open
alongside the backlog decision about how cancellation should reach operators.

Validation: Release build clean across the solution; 2023 core and 121 API usage tests pass; `dotnet
format` whitespace and analyzer checks are clean.

### Refinement checkpoints

Checkpoint states are `Pending`, `In progress`, `Awaiting review`, and `Accepted`. Implementation stops at `Awaiting review`; only explicit user acceptance advances to the next checkpoint.

| Checkpoint | Status | Deliverable |
| --- | --- | --- |
| RF-0 Design contract | Accepted | Document the `Candidate → Candidate` refiner role as the single refinement mechanism, conventional algorithm placement, the visibility rule for operator-issued evaluation, composition topologies, transient-refinement evaluation, the improvement-checking refiner with its nullable evaluator and comparer, the fitness-only evaluator contract, the composition examples, and deferred producer wrappers and artifacts. No source code. |
| RF-1 Development-branch integration | Accepted | Merge `dev` into the working branch, reconcile and review its operator-base improvements, run appropriate validation, and stop before implementing Refiner. Done; see [Development-branch integration outcome](#development-branch-integration-outcome). |
| RF-2 Evaluator contract simplification | Accepted | Change `IEvaluatorInstance.Evaluate` to return `IReadOnlyList<ObjectiveVector>`, migrate the evaluator wrappers, built-in algorithms, and analysis hooks, remove `IteratedEvaluator`, and remove the candidate-substitution caveat from `CachingEvaluator`. `EvaluatedCandidate<TCandidate>` is retained as the population and state pairing type; only the evaluator stops producing it. `IEvaluatorObserver.AfterEvaluation` becomes `(IReadOnlyList<ObjectiveVector> objectiveVectors, IReadOnlyList<TCandidate> candidates, ...)`, adopting the output-first parameter order that the other seven observer interfaces already use and that evaluation was the sole exception to. |
| RF-3 Refiner operator model | Accepted | Implement the general refiner configuration and execution-instance contracts, authoring bases in the three paths and three arities, `SingleCandidateRefiner.RefineCandidate`, concurrency, identity behavior, validation, and focused contract tests. Delivered together with RF-4; see [Refiner operator model outcome](#refiner-operator-model-outcome). |
| RF-4 Refiner composition topologies | Accepted | Add the pipeline, iterated, choose-one, multi, wrapping, and observable refiner topologies; design and add the evaluator composition for transient Baldwinian refinement; restore iterated refinement after RF-2 removes `IteratedEvaluator`; and cover order-significant, repeated-stage, no-write-back, and evaluation-accounting behavior. Delivered together with RF-3; see [Refiner operator model outcome](#refiner-operator-model-outcome). |
| RF-5 Explicit algorithm integration | Accepted | Add configurable refinement to applicable built-in algorithms after creation and final variation but before evaluation, without re-refining carried evaluated candidates. See [Algorithm integration outcome](#algorithm-integration-outcome). |
| RF-6 Numeric parameter-fitting refiner | Accepted | Adapt symbolic-regression numeric parameter fitting to the general refiner role and define its failure behavior without adding problem-objective retention. See [Numeric parameter-fitting refiner outcome](#numeric-parameter-fitting-refiner-outcome). |
| RF-7 Improvement-checking refiner | Accepted | Implement the refiner with its `Evaluator` and `Criterion` settings, the `IImprovementCriterion` strategy with its strict-improvement, not-worse, dominance and threshold implementations, and original-on-failure behavior. Delivered alongside RF-3/RF-4; see [Improvement-checking refiner outcome](#improvement-checking-refiner-outcome). |
| RF-8 Integration hardening | Accepted | Verify batch semantics across every refiner topology, refiner/evaluator composition and accounting, objective directions and acceptance criteria in composition, failure behavior in a run, and the remaining topology semantics, including each composition example with its shared-instance accounting and cache/limit wrapper order. Cancellation and user documentation are out of scope. See [Integration hardening plan](#integration-hardening-plan) and [Integration hardening outcome](#integration-hardening-outcome). |
| RF-9 Refinement documentation | Accepted | Document refinement for users: the evaluator-accounting rules including shared instances and cache/limit wrapper order, the refiner topologies, and the cancellation boundary; extend `NumericParameterFittingSpecs` with the accounting compositions; and reconcile `docs/operators.md`, `docs/operator-composition.md`, `docs/execution-model.md` and `docs/observability-and-analysis.md` with the behavior RF-8 pins. |

## Stage 4: Symbolic-Regression Numeric Parameter Fitting

### Expression-lowering design

The first adapter increment lowers an `ExpressionTree` directly into the internal AD program without passing through `CompiledExpression`. Ordinary compiled expressions deliberately erase distinctions that numeric parameter fitting needs: fixed and evolvable constants share one opcode, constants may be folded, repeated variables are interned, and tree-occurrence identity is not retained.

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

The first implementation uses every training row. Observation sampling is deferred. When sampling is introduced later, it belongs to the symbolic-regression parameter-fitting adapter or a later refiner, not the numerical solver.

The first symbolic integration defines:

- least-squares residuals from raw expression predictions and raw training targets;
- raw full-data mean squared error as the numerical optimization measure;
- unconditional rebuilding with the parameter vector returned by a successful LM call;
- ordinary propagation of non-finite optimized parameter values and mean squared error.

The parameter-fitting component does not retain a best finite point, compare initial and final loss, consult the problem's configured `IRegressionMetric`, or decide whether the fitted expression should be retained. A later refiner owns retention and improvement checking around this fitting capability.

Evaluation-time linear scaling is outside the first parameter-fitting objective. The optimizer neither differentiates through fitted scaling coefficients nor injects scaling terms into the expression. Normal symbolic-regression evaluation may fit or apply linear scaling after numeric parameter fitting exactly as it otherwise would. Explicit root-level scaling parameters or a jointly scaling-aware objective remain future design options.

Initially:

- optimize evolvable constants only;
- leave fixed constants unchanged;
- use least-squares residuals for LM;
- require direct full-data optimization;
- defer bounds, row sampling, sample weights, best-point retention, acceptance policy, linear-scaling integration, and variable-weight optimization.

### Initial API boundary

Do not begin with a regression-problem-shaped convenience facade and work inward.

Begin with an internal direct parameter-fitting entry point so the behavioral contract and performance can stabilize. Design a public symbolic-regression facade afterward. It should expose symbolic intent while allowing advanced users to select a numerical algorithm once multiple algorithms are genuinely supported. A later refiner may consume this capability, but its API is outside the current design. The final parameter-fitting API must avoid exposing tape, Jacobian, row-selection, or solver-workspace details to ordinary symbolic-regression users.

The capability retains the canonical symbolic-regression name numeric parameter fitting. Its first internal component is `NumericParameterFitter`, exposing `Optimize` and `TryFit`. Parameter optimization describes the numerical mechanism by which evolvable constant occurrences are fitted. V1 optimizes only occurrences represented by `ParameterBinding`; fixed constants remain unchanged.

Its easy-to-use surface provides both throwing and non-throwing forms:

```csharp
internal static ExpressionTree Optimize(
    ExpressionTree expression,
    RegressionData data,
    int maximumIterations,
    CancellationToken cancellationToken = default);

internal static bool TryFit(
    ExpressionTree expression,
    RegressionData data,
    int maximumIterations,
    [NotNullWhen(true)] out ExpressionTree? fittedExpression,
    CancellationToken cancellationToken = default);

internal static bool TryFit(
    ExpressionTree expression,
    RegressionData data,
    int maximumIterations,
    [NotNullWhen(true)] out ExpressionTree? fittedExpression,
    [NotNullWhen(false)] out NumericParameterFittingFailure? failure,
    CancellationToken cancellationToken = default);
```

The convenience `TryFit` overload collapses compilation, variable-binding, and numerical-solver failures to `false`. Its detailed overload additionally returns a small closed `NumericParameterFittingFailure` hierarchy that retains the owning lower-level failure. The expression compiler, data-binding adapter, and LM adapter follow the same convention by offering convenience and detailed `Try` overloads. `Optimize` translates compilation failure to an informative `NotSupportedException`, binding failure to `ArgumentException`, and numerical-solver failure to `InvalidOperationException`. Programming errors, invalid arguments, and cancellation continue to propagate from all forms; cancellation is represented by `OperationCanceledException` rather than `false`. Numeric parameter fitting requires non-empty regression data and a non-negative maximum iteration count. `RegressionData` already owns input/target row-count consistency, and nonnullable annotations are compile-time contracts rather than reasons for redundant runtime null checks.

A successful LM result is always rebuilt through `DifferentiableExpression.WithParameterValues`, including non-finite parameter values. Input-free expressions with evolvable constants are fitted against every target row by repeating their scalar value and parameter derivatives across the data row count. Zero maximum iterations and expressions without evolvable constant occurrences succeed immediately and return the original `ExpressionTree` instance after validating the general call arguments and cancellation but before differentiable compilation, data binding, or LM. These identity shortcuts also allow unsupported operations or unbound variables that are irrelevant when no optimization is requested or no parameter can change.

The component lives under `HEAL.HeuristicLib.DataAnalysis.Regression` because it couples a symbolic expression to supervised regression data. It depends on `ExpressionTree`, `RegressionData`, expression lowering and binding, and the internal numerical optimizer. It does not depend on `SymbolicRegressionProblem`, configured regression metrics, linear scaling, HeuristicLib algorithms or operators, evaluation accounting, retention, or acceptance policy.

### Numeric parameter-fitting component checkpoints

Checkpoint states are `Pending`, `In progress`, `Awaiting review`, and `Accepted`. Implementation stops at `Awaiting review`; only explicit user acceptance advances to the next checkpoint.

| Checkpoint | Status | Deliverable |
| --- | --- | --- |
| PF-0 Component contract | Accepted | Document terminology, API shape, successful rebuilding semantics, behavior without evolvable constants, high-level failure behavior, cancellation, dependencies, and explicit exclusion of retention and acceptance. No source code. |
| PF-1 Successful bridge | Accepted | Implement compilation, data binding, initial-value extraction, LM invocation, unconditional immutable rebuilding, and one successful fitting test. |
| PF-2 Failure and identity behavior | Accepted | Verify convenience and detailed non-throwing failures, specific throwing behavior, cancellation propagation, lower-level failure retention, and identity shortcuts for zero iterations or no evolvable constants. |
| PF-3 Semantic hardening | Accepted | Verify non-finite parameter propagation, fixed constants, occurrence identity, shared nodes, macro emission, source immutability, repeated calls, and independence from problem metrics, linear scaling, algorithms, operators, retention, and acceptance. |

PF-3 verifies the complete rebuilding semantics at the direct component boundary. Reference-shared constant nodes at different expression paths are fitted as independent parameters; macro expansion remains compilation-only and the rebuilt expression preserves the macro; fixed constants and the source expression remain unchanged; repeated calls are independent; and successful non-finite results and parameter values are not sanitized. A production dependency audit confirms that `NumericParameterFitter` depends only on symbolic expressions, `RegressionData`, expression lowering and binding, and numerical optimization. It has no dependency on symbolic-regression problems, configured metrics, linear scaling, HeuristicLib algorithms or operators, retention, or acceptance policy.

### Direct vertical-slice artifact

Provide:

- a direct test fixture optimizing constants in a fixed expression;
- retained compiled/differentiable model reuse where lifecycle permits;
- tests for fixed constants, occurrence identity, structurally shared nodes, macro re-emission, unsupported operations, invalid points, identity behavior without evolvable constants, non-finite propagation, and immutable replacement;
- analytic and finite-difference correctness tests, plus behavioral comparison with the maintained legacy implementation where applicable;
- end-to-end performance measurements of the maintained parameter-fitting pipeline.

Do not require identical optimized parameter vectors, iteration counts, or termination labels when different implementations produce behaviorally equivalent predictions and loss. Intentional design differences, including immutable rebuilding and exclusion of legacy variable-weight optimization, must be asserted explicitly rather than hidden by broad parity tolerances.

## Stage 5: Performance Hardening

Performance work is continuous, but final specialization happens only after the layer boundaries are stable.

### Legacy comparison outcome

Numeric parameter fitting was compared between the maintained immutable implementation and the legacy
mutable one through a throwaway measurement harness that is not part of the repository. Both call the same
MathNet Levenberg-Marquardt, so the solver is held constant and the measurement isolates what this
replacement changed: the internal automatic-differentiation engine against the AutoDiff package and its
tree conversion.

The harness fitted three expressions over 500 rows with ten solver iterations, taking fifty measured fits
after five warmup fits. Fitting data covers every row on both sides, and the legacy tree is rebuilt outside
the measured region because it
is fitted in place.

| Expression | Implementation | Fitted parameters | ms / fit | KB / fit | MSE |
| --- | --- | ---: | ---: | ---: | ---: |
| `c*x + c` | immutable | 2 | 0.49 | 383 | 45.36 |
| `c*x + c` | legacy | 2 | 4.07 | 3653 | 45.36 |
| `c*x + c` | legacy, with variable weights | 3 | 5.52 | 4801 | 45.36 |
| `c*x*x + c*x + c` | immutable | 3 | 0.76 | 708 | 8.193E-06 |
| `c*x*x + c*x + c` | legacy | 3 | 4.38 | 3976 | 8.193E-06 |
| `c*x*x + c*x + c` | legacy, with variable weights | 6 | 3.87 | 7094 | 8.193E-06 |
| `(c*x + c) * (c*x + c) + c` | immutable | 5 | 1.16 | 1495 | 8.193E-06 |
| `(c*x + c) * (c*x + c) + c` | legacy | 5 | 4.93 | 6601 | 8.193E-06 |
| `(c*x + c) * (c*x + c) + c` | legacy, with variable weights | 7 | 3.37 | 7584 | 8.193E-06 |

Outcome:

- **The immutable implementation is faster on every case**, by roughly 4x to 8x at equal parameter counts,
  and allocates roughly a fifth to a tenth as much. A second run reproduced every figure within ordinary
  run-to-run variance.
- **Both reach the same optimum.** Mean squared error agrees to four significant figures on all three
  expressions, including the linear expression that cannot fit the quadratic target and where both settle
  on the same least-squares line. This is the behavioral parity the migration plan asks for, measured
  rather than assumed.
- **Part of the legacy gap is its own acceptance policy**, not its differentiation. It evaluates the
  expression before and after the solve to decide whether to keep the result, work the immutable
  implementation does not do because acceptance belongs to the improvement-checking refiner. The remaining
  difference is the AutoDiff conversion and its allocation behavior.
- **Allocation per fit is dominated by setup and the solver, not by our marshalling.** Even the immutable
  path allocates several hundred kilobytes per fit while the AD engine itself allocates nothing after
  execution creation, so compilation, binding and MathNet's own per-iteration storage are where that
  memory goes. Attributing it across those three is work for the performance increment.

This closes the behavioral and performance comparison the migration sequence asks for before the legacy
optimizer can be proposed for deletion. Retiring it would remove `AutoDiff` from the Experimental package,
whose only remaining consumer is `TreeToAutoDiffTermConverter`.

These figures are a single-machine smoke comparison, not a statistical benchmark. Their purpose is to show
the direction and order of magnitude; the performance increment owns proper measurement.

### Evaluation throughput outcome

Evaluating an expression over a dataset was compared the same way, which is like for like: the same
expressions produce the same predictions over the same rows and only the implementation differs. Equal
checksums in every row confirm that. Three expressions over 5000 rows, one hundred measured runs after
five warmup runs.

| Expression | Implementation | ms / run | KB / run |
| --- | --- | ---: | ---: |
| `c*x + c` | compile | 0.0014 | 1.5 |
| `c*x + c` | compile, optimized | 0.0013 | 1.5 |
| `c*x + c` | immutable | 0.0199 | 39.1 |
| `c*x + c` | immutable, optimized | 0.0202 | 39.1 |
| `c*x + c` | immutable, optimized, caller buffers | 0.0171 | 0.0 |
| `c*x + c` | legacy scalar | 0.0944 | 1.6 |
| `c*x + c` | legacy batched | 0.1063 | 120.6 |
| `c*x*x + c*x + c` | immutable, optimized | 0.0341 | 39.1 |
| `c*x*x + c*x + c` | legacy scalar | 0.1654 | 2.5 |
| `c*x*x + c*x + c` | legacy batched | 0.2016 | 124.1 |
| `(c*x + c) * (c*x + c) + c` | immutable, optimized | 0.0394 | 39.1 |
| `(c*x + c) * (c*x + c) + c` | legacy scalar | 0.1824 | 2.6 |
| `(c*x + c) * (c*x + c) + c` | legacy batched | 0.1823 | 125.2 |

Outcome:

- **The compiled interpreter evaluates about 4.5 to 5 times faster** than the legacy scalar interpreter,
  with identical predictions.
- **Compilation is cheap relative to evaluation**, roughly six percent of the interpretation it enables at
  5000 rows. Compiling every candidate once per generation is therefore not a cost worth engineering
  around, which also means the interpret-only rows are close to the per-candidate reality.
- **Compiler optimization makes no measurable difference on these expressions.** Constant folding and
  identity elimination have nothing to fold where constants sit at leaves multiplied by variables, which
  is the shape genetic programming produces. It costs nothing either, so the default stands; expressions
  carrying foldable constant subtrees are where it would pay.
- **The allocating interpret overload allocates the result array**, 39 KB for 5000 rows, where the legacy
  interpreter returns a lazy sequence and allocates almost nothing. The overload taking caller-owned
  destination and workspace buffers allocates nothing at all and is the one to use in a hot loop. This is
  the one axis where the naive comparison favours the legacy implementation.

- **The batched legacy interpreter is not faster than the scalar one**, and allocates roughly fifty times
  as much. The comparison above therefore holds against the better of the two legacy implementations.

The harness also surfaced a defect in `SymbolicDataAnalysisExpressionTreeBatchInterpreter`, which is now
fixed. It compiled a numeric constant into a buffer of `BatchSize` values but then indexed that buffer by
absolute dataset row, so it threw on any dataset longer than one batch that contained a constant. The only
test covering it used eleven rows, where the batched loop never runs and only the remainder path executes,
which is why the defect had never been exercised. `LoadData` now fills the batch from the instruction value
for numbers and constants and indexes a dataset column only for variables, and the constant no longer
carries a redundant buffer. `SymbolicDataAnalysisExpressionTreeBatchInterpreterTests` covers row counts on
both sides of the batch boundary and pins agreement with the scalar interpreter; those tests fail against
the previous implementation.

### Solver cost attribution outcome

This closes the duplicated-forward-sweep item that LM-3 deferred, and attributes per-fit allocation.

**The duplicated forward sweep is real and immaterial.** Instrumenting the adapter for one fit of
`c*x*x + c*x + c` over 500 rows recorded six model callbacks and six Jacobian callbacks, with all six
Jacobian calls at the same parameter point as the model call immediately preceding them. MathNet asks for
model values and the Jacobian through separate callbacks at one point, so the forward sweep that producing
the Jacobian performs anyway is repeated by the value-only call every time.

Removing it was implemented and reverted. Serving both callbacks from one combined evaluation cached by
parameter vector produced no measurable improvement at 500 rows, and none at 20 000 rows either, where the
three expressions measured 5.86, 8.27 and 35.3 milliseconds per fit against 6.07, 8.24 and 32.9 without the
change: differences inside run-to-run variance and pointing in both directions. The change was therefore
reverted rather than kept, because a specialization that measurement does not support is exactly what this
increment is supposed to exclude. The redundant sweep costs a few percent at these sizes because a
value-only sweep is small next to the Jacobian evaluation and the solver's own linear algebra.

**Per-fit allocation is dominated by the solver, not by our marshalling.** Allocation scales linearly with
row count and grows with parameter count: 0.38 MB per fit at 500 rows and two parameters, 15.1 MB at 20 000
rows and two parameters, and 48 MB at 20 000 rows and five parameters. Raising the iteration cap from ten
to forty barely moved it, so the solver converges well before either cap and the figures describe the
iterations actually taken.

The adapter's own allocation is bounded and one-off per solve: model values, the Jacobian, a target copy
and the unused independent-variable vector, which is `(3 + parameterCount) * rowCount` doubles. At 20 000
rows and five parameters that is roughly 1.3 MB against the 48 MB measured, so under three percent of the
allocation belongs to this library. The remainder is MathNet's per-iteration dense storage.

Consequence for the retained-MathNet decision: the solver's allocation behavior is now a quantified cost
rather than a suspicion, and it is not addressable by adapter changes or solver configuration. It is
nevertheless not sufficient to reopen the decision, because the only remedy is a Levenberg-Marquardt
implementation of our own, and the complete path is already four to eight times faster than the
implementation it replaces. The cost is recorded so that a whole-run profile showing garbage-collection
pressure has something to point at.

### Whole-run profile plan

Done; see [Whole-run profile outcome](#whole-run-profile-outcome).

This is the last open piece of the performance increment and the last completion criterion of the first
vertical slice. Every measurement so far is per fit or per evaluation; none of them says whether those
costs matter inside a run.

Three questions depend on it:

1. **Does the interpreter advantage matter?** Evaluating four to five times faster is worth little if
   evaluation is a small share of a generation.
2. **What does refinement cost per generation?** The refinement documentation deliberately carries no cost
   statement, because there was no measurement to base one on.
3. **Does the solver's allocation matter?** Fitting allocates 15 to 48 MB per fit at 20 000 rows. The
   solver-backend gate names garbage-collection pressure in a whole run as the condition that would
   reopen the retained-MathNet decision.

#### Approach

Assemble the profile from the library's own duration instrumentation rather than from anything added for
measurement. Every operator role has a duration-measuring wrapper in
`src/HeuristicLib/Operators/<Role>/Instrumentation/DurationMeasuring<Role>.cs`, exposed as
`Measure<Role>Duration(ObservationDuration)` and an `out` overload in the role's own namespace.
`ObservationDuration.CurrentDuration` accumulates across calls, so one instance per role covers a run.

Using the public instrumentation is deliberate: it exercises that API for the purpose it exists for. If
assembling the profile is awkward, that is a finding about the API and belongs in the outcome rather than
being worked around.

The harness is throwaway and stays out of the repository, like the earlier comparisons. Findings are
recorded here.

#### API traps when rebuilding a harness

Three details cost time while the earlier comparison harnesses were built and are not evident from the API
surface:

- **Duration wrappers live in the role namespace rather than an instrumentation one.**
  `MeasureSelectorDuration` and its siblings are declared in `HEAL.HeuristicLib.Operators.Selectors` even
  though their files sit in an `Instrumentation` folder, so the ordinary role using directive is enough and
  a nested one does not exist. This one affects the whole-run profile.
- **A legacy `SymbolicExpressionTree` needs its program-root and start wrapper.** Handing the AutoDiff
  converter or either legacy interpreter a bare operation node fails; the shape is `ProgramRootSymbol`,
  then `StartSymbol`, then the expression. This affects any rebuilt comparison harness.
- **The legacy and immutable `Symbol` types collide by name.** A file touching both needs an alias such as
  `using LegacySymbol = HEAL.HeuristicLib.SearchSpaces.Trees.SymbolicExpressionTree.Symbols.Symbol;`, and
  without it the compiler reports a confusing argument-type mismatch rather than an ambiguity. This also
  affects any rebuilt comparison harness.

#### Configuration

One `GeneticAlgorithm` over a `SymbolicRegressionProblem`, using the shape of
`SymbolicRegressionVerticalSliceScenario`: population 80, 30 generations, `RampedHalfAndHalfTreeCreator`,
`SubtreeCrossover`, a `ChooseOneMutator` over the three symbolic mutators at rate 0.3, tournament
selection of size 3, one elite, and a search space carrying an `EvolvableConstantSymbol` so that
refinement has parameters to fit. Two synthetic inputs with a target that rewards fitted values, for
example `2.5*x0 + 1.5*x0*x1 - 0.75`.

Three configurations at one seed, so the search path is comparable:

| Configuration | Purpose |
| --- | --- |
| No refiner | Baseline share per role |
| `NumericParameterFittingRefiner` | Refinement cost per generation |
| The same under `WithImprovementCheck` | Adds two comparison evaluations per candidate |

Give the improvement check **its own** evaluator instance rather than the algorithm's, so its comparison
evaluations are attributable separately. Sharing the algorithm's evaluator merges both into one duration;
that is the shared-accounting arrangement rather than a measurement problem, and the outcome should say
which one the figures describe.

Sweep row counts of 200, 2 000 and 20 000 with everything else fixed. Row count moves evaluation and
fitting cost while leaving variation cost alone, which is what separates "evaluation dominates" from
"variation dominates".

#### Reporting

Per run: duration for creator, crossover, mutator, evaluator, selector, refiner and the check's evaluator;
total wall-clock; the **unattributed remainder** as its own row; allocated bytes for the run; and
`GC.CollectionCount(0/1/2)` deltas, which is the figure the allocation question actually needs.

#### Caveats that belong in the outcome

- **`GeneticAlgorithm` has no `Replacer` slot.** It calls `ElitismReplacer.Replace(...)` directly, so
  replacement cannot be instrumented and lands in the remainder. `NSGA2` does have a configurable
  replacer, and that asymmetry is worth recording as an API observation.
- **The remainder is part of the run, not error.** State construction, elitism, randomness forking and
  streaming overhead all sit outside operator calls.
- **The algorithm wraps the mutator** in `WithRate` when `MutationRate` is below one, so a wrapper on the
  configured mutator measures actual mutation calls rather than the rate dispatch around them. Crossover
  has no such wrapper, so the two are not directly comparable without saying so.
- **Instrumentation is not free.** Run one configuration with and without the wrappers and report the
  difference, so the shares are known to be shares of a lightly perturbed run.
- **Refinement changes the search trajectory.** Time per generation is comparable across configurations;
  time to reach a given quality is a different question this profile does not answer.

#### Verification

Run the sweep twice and check that the shares agree within ordinary variance. Three checks must hold
before any conclusion is drawn, because they catch a broken harness before it produces a plausible answer:
the configuration without a refiner must report zero refiner duration; roles plus remainder must equal the
measured total; and evaluation duration must grow roughly linearly with row count while crossover and
mutation stay flat.

Confirm that production is untouched with a release build and the core test suite.

#### Done when

The three questions above have numbers, a `Whole-run profile outcome` section records them in the same
shape as the legacy comparison and cost attribution outcomes, and the first vertical slice's benchmark
criterion can be marked complete. Any hotspot the profile reveals is written down as a candidate for
separate work rather than fixed in the same pass.

### Whole-run profile outcome

A complete symbolic-regression run was profiled through the library's own duration instrumentation, in the
configuration the plan above describes: one `GeneticAlgorithm` over a `SymbolicRegressionProblem` with
population 80, 30 generations, `RampedHalfAndHalfTreeCreator`, `SubtreeCrossover`, a `ChooseOneMutator`
over the three symbolic mutators at rate 0.3, tournament selection of size 3, one elite, and a search
space carrying an `EvolvableConstantSymbol`, against `2.5*x0 + 1.5*x0*x1 - 0.75` at 200, 2 000 and 20 000
rows. `NumericParameterFittingRefiner` runs at its default five iterations. The algorithm refines the
initial population and each generation's offspring, so one run refines 30 batches of 80 candidates.

Every role is wrapped in its `Measure<Role>Duration` wrapper. The improvement check holds its own
evaluator, so its comparison evaluations are attributable separately; because that evaluator sits inside
the refiner, its duration is a nested subtotal of the refiner rather than a further addend, and adding it
to the role sum would count it twice.

Shares below are sweep 1, which the second sweep reproduced within 0.2 points on every refining
configuration.

| Rows | Configuration | creator | crossover | mutator | evaluator | selector | refiner | remainder | s / run |
| ---: | --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| 200 | no refinement | 1.0% | 22.8% | 6.1% | 54.6% | 6.7% | 0.0% | 8.8% | 0.004 |
| 200 | refinement | 0.0% | 0.9% | 0.3% | 1.8% | 0.2% | 96.4% | 0.4% | 0.164 |
| 200 | refinement, improvement check | 0.0% | 0.8% | 0.3% | 1.6% | 0.2% | 96.7% | 0.4% | 0.166 |
| 2 000 | no refinement | 0.4% | 10.3% | 2.9% | 79.1% | 2.7% | 0.0% | 4.7% | 0.010 |
| 2 000 | refinement | 0.0% | 0.1% | 0.1% | 0.9% | 0.0% | 98.7% | 0.1% | 1.001 |
| 2 000 | refinement, improvement check | 0.0% | 0.2% | 0.1% | 0.8% | 0.0% | 98.9% | 0.1% | 0.974 |
| 20 000 | no refinement | 0.1% | 21.4% | 6.4% | 71.1% | 0.4% | 0.0% | 0.7% | 0.100 |
| 20 000 | refinement | 0.0% | 0.1% | 0.0% | 0.7% | 0.0% | 99.3% | 0.0% | 11.967 |
| 20 000 | refinement, improvement check | 0.0% | 0.1% | 0.0% | 0.6% | 0.0% | 99.3% | 0.0% | 12.603 |

Two of these shares are not what they look like, and the section below on cost per candidate corrects them.
The crossover and mutator shares of the unrefined 20 000-row run — 21.4 and 6.4 percent, for operators that
do not touch a data row — are collection pause charged to whoever was executing rather than work.
Evaluation's real share of that run is around 96 percent of the work it performs, not the 71 percent shown.

| Rows | Configuration | MB / run | gen0 | gen1 | gen2 |
| ---: | --- | ---: | ---: | ---: | ---: |
| 200 | no refinement | 11.7 | 0 | 0 | 0 |
| 200 | refinement | 682 | 46 | 7 | 0 |
| 2 000 | no refinement | 44.7 | 2 | 0 | 0 |
| 2 000 | refinement | 5 510 | 436 | 231 | 143 |
| 20 000 | no refinement | 375 | 97 | 97 | 97 |
| 20 000 | refinement | 51 292 | 11 349 | 11 330 | 11 324 |

The improvement-checking rows allocate within two percent of the plain refining rows at every row count
and are omitted from the second table.

#### Cost per candidate

A share cannot be checked for plausibility on its own, so each role was also divided by the candidates it
actually processed, counted through the matching `Count<Role>Candidates` wrapper rather than assumed from
the population arithmetic. This is the durable statistic; the shares above are what it produces in one
particular configuration.

| Rows | creator | crossover | mutator | evaluator | selector |
| ---: | ---: | ---: | ---: | ---: | ---: |
| 200 | 1.56 | 0.48 | 0.97 | 1.07 | 0.065 |
| 2 000 | 1.21 | 0.40 | 0.79 | 2.78 | 0.063 |
| 20 000 | 1.01 | 9.35 † | 0.94 | 29.07 | 0.098 |

Microseconds per candidate, unrefined runs, five runs aggregated per row count. Candidate counts per run
are 80 for the creator, 2 320 for crossover, 690 for the mutator — the rate wrapper sits outside the
duration wrapper, so this is the mutated subset rather than the whole offspring population — 2 400 for the
evaluator and 4 640 for the selector. † is not a real cost; see below.

Mean tree length in the final population is 10.1, 10.0 and 10.3 nodes at the three row counts, with a
depth limit reached in every case, so tree size is effectively constant across the sweep and cannot explain
any difference between row counts.

Three things follow, and the first two answer the question a reader is likeliest to ask about the share
table — why does crossover take a fifth of an unrefined run when evaluation supposedly does so much more
work, and why does it stay a fifth when the rows grow a hundredfold?

- **Evaluation over 200 rows is genuinely cheap, because the interpreter is vectorized.**
  `ExpressionInterpreter` evaluates column-wise through `TensorPrimitives`, so a ten-node tree over 200 rows
  is about ten SIMD kernel calls over 200-element spans. The intuition that evaluation does far more work
  than crossover is right in scalar operation count and wrong in time.
- **Crossover is not cheap, because it allocates.** Measured outside any algorithm on five-node parents,
  one `SubtreeCrossover.Cross` costs 0.22 microseconds and 690 bytes. `SelectDonor` walks the second parent
  through `ExpressionPoint.TraversePreOrder`, a recursive iterator that allocates a point object per node
  and a nested state machine per subtree, before `Replace` rebuilds the path. At the ten-node trees the run
  evolves that comes to the 0.4 microseconds the table shows. Crossover at a fifth of an unrefined 200-row
  run is therefore real.
- **Evaluation's per-candidate cost is linear in rows above a fixed floor.** It grows 2.78 to 29.07
  microseconds from 2 000 to 20 000 rows, a factor of 10.4 for ten times the rows, but only 1.07 to 2.78
  from 200 to 2 000, a factor of 2.6. Compiling each candidate costs the same at every row count and
  dominates at 200 rows. This is the sublinearity the row-count check reports, with the mechanism attached.

**† The 20 000-row crossover figure is measurement contamination, and the trend you would expect is real
underneath it.** Crossover does no row-dependent work and the trees are the same size, yet it reads 9.35
microseconds per candidate there against 0.40 at 2 000 rows, and it read 5.42 and 0.91 in other repetitions
of the same measurement. The cause is directly measurable: `GC.GetTotalPauseDuration` reports 11.6
milliseconds of pause in a 94-millisecond run at 20 000 rows across roughly 100 full blocking collections,
against 0.36 milliseconds and no full collection at 2 000 rows. Wall-clock instrumentation charges that
pause to whichever operator is executing when it happens, and the roles that do almost nothing absorb it
disproportionately. Correcting crossover and the mutator to their row-independent costs leaves evaluation
at essentially all of the run's actual work at 20 000 rows, which is the clean shift towards evaluation the
share table appeared to contradict.

Answers to the three questions:

- **The interpreter advantage matters, but only where refinement is off.** Without a refiner, evaluation is
  the dominant role at every row count and becomes more dominant as rows grow: 55 percent at 200 rows, 79
  percent at 2 000, and about 96 percent of the work at 20 000 once the collection pause the share table
  charges to other roles is put back. A four-to-five-times faster interpreter is therefore close to a
  four-to-five-times faster run for ordinary genetic programming, which is what the evaluation-throughput
  outcome could not say on its own.
  Turn refinement on and the same evaluator falls to between 0.6 and 1.8 percent, where the interpreter
  could be made free without changing the run. Both readings are true at once and the configuration decides
  which one applies.
- **Refinement costs about 70 microseconds per candidate at 200 rows, 440 at 2 000 and 5.1 milliseconds at
  20 000**, which is 5.6, 35 and 407 milliseconds per generation of 80 candidates. As a share it is 96 to
  99 percent of the run at every row count, and enabling it multiplies total run time by roughly 45 times
  at 200 rows and around a hundredfold at 2 000 and 20 000. Refinement is not one cost among several: at
  the default five iterations it is the run. The refinement documentation can now carry that statement.
- **The improvement check costs what its documentation says and little more.** Its evaluator takes almost
  exactly twice the algorithm's own evaluator at every row count — two comparison evaluations against
  one — and lands at 3.3, 1.6 and 1.2 percent of the run. Adding the check changed total run time by less
  than the difference between two sweeps of the same configuration, because it doubles a role that
  refinement has already reduced to under two percent.
- **The solver's allocation is real, large, and does not reopen the retained-MathNet decision.** At 20 000
  rows a refining run allocates 51 GB and triggers 11 300 collections, against 375 MB and 97 without a
  refiner: 137 times the allocation and 118 times the collections. Per refined candidate that is 21 MB and
  4.7 full collections. The shape of the pressure is more informative than its size: at 20 000 rows the
  gen0, gen1 and gen2 counts are equal, so nearly every collection is a full blocking one, while at 2 000
  rows gen2 is a third of gen0 and at 200 rows it is zero. A row-length `double[]` is 160 KB at 20 000
  rows and 16 KB at 2 000, so the large-object-heap threshold is crossed between those two row counts and
  that crossing, not the row count itself, is what turns ordinary allocation into full collections.

The gate asks for a quantified problem that neither adapter changes nor solver configuration can address.
This one is addressable by both. `MaximumIterations` scales the per-iteration storage directly, and
`FittingData` fits on a row subset, which is the setting that moves the arrays back below the large-object
threshold; both are ordinary configuration of the shipped refiner. Of the 21 MB per candidate, the
adapter's own marshalling is `(3 + parameterCount) * rowCount` doubles, under one megabyte at these
parameter counts, which agrees with the under-three-percent attribution the solver cost outcome measured.
**MathNet is retained**, and the figure is recorded so that the decision rests on a measurement rather than
on an absence of one.

Instrumentation cost: at 2 000 rows the instrumented and uninstrumented runs differed by between −25 and
+22 percent across four comparisons, changing sign between sweeps. The wrappers therefore cost less than
the run-to-run variance of the configuration they measure, and the shares above are shares of a run that is
perturbed by less than the noise floor.

Harness checks, all of which hold: a run without a refiner reports exactly zero refiner duration across all
40 such runs; no role sum exceeds its run, so the remainder is non-negative everywhere; evaluation grows 34
times over a hundredfold row increase, sublinearly because compiling each candidate costs the same at every
row count; crossover and mutation stay flat to within 12 percent up to 2 000 rows; and the two sweeps agree
within 0.2 points on every refining configuration.

Three findings came out of building the harness rather than out of the figures:

- **Short runs charge collection pauses to whichever operator is executing.** This is the single largest
  threat to a profile assembled this way, and it is quantified above: 11.6 milliseconds of measured pause in
  a 94-millisecond run at 20 000 rows, inflating crossover by a factor of 23 on work that cannot depend on
  the row count. Duration instrumentation measures wall clock, which is the right thing for attributing a
  run and the wrong thing for attributing an operator, and nothing in the API says so. A role whose real
  cost is small next to the pause budget cannot be measured this way at all; it needs a per-candidate cost
  taken where the heap is quiet, which is what the isolated crossover measurement provides. The harness
  reduces but does not remove the effect by aggregating five runs per point; an earlier version reporting
  one median run put the same evaluator share at 97 percent in one sweep and 60 percent in the next.
- **Assembling the profile is mildly awkward in one specific way**, which the plan asked to be recorded
  rather than worked around. A file that declares a variable of a role interface, constructs a concrete
  operator and wraps it needs three namespaces: `HEAL.HeuristicLib.Operators` for `IRefiner<,,>`,
  `HEAL.HeuristicLib.Operators.Refiners` for the duration extension, and
  `HEAL.HeuristicLib.Operators.Refiners.SymbolicRegressionRefiners` for the operator. Missing the middle
  one produces `does not contain a definition for 'MeasureCreatorDuration'` rather than anything naming a
  namespace. This is the same shape as the trap already recorded for the role namespace, one level deeper.
- **Nesting is not expressible in the instrumentation.** Wrapping a composed refiner measures the whole
  composition, so the improvement check's evaluator appears both inside the refiner duration and in its own
  counter, and anyone summing role durations has to know the topology to avoid counting it twice. That is
  inherent to wrapper-based measurement rather than a defect, but it is not visible from the API and the
  arithmetic silently produces a plausible wrong answer for whoever does not know.

Hotspots, recorded as candidates for separate work rather than fixed here:

1. **Evaluation allocates a prediction array per candidate.** `SymbolicRegressionProblem.Evaluate` calls the
   allocating `Evaluate(DataFrame)` overload, so each candidate allocates a row-length array — 160 KB and
   therefore large-object-heap traffic at 20 000 rows, which is where the unrefined run's 375 MB and 100
   full collections come from. `Evaluate(DataFrame, Span<double>, Span<double>)` already exists and the
   evaluation-throughput outcome already measured it at zero allocation. This is the largest cost in a run
   without refinement, the change is local to the problem, and it would additionally remove the pause that
   currently makes every other role at 20 000 rows unmeasurable.
2. **Crossover allocates a point object per node of the second parent.** One `SubtreeCrossover.Cross` costs
   690 bytes and 0.22 microseconds on five-node parents, because `SelectDonor` enumerates
   `ExpressionPoint.TraversePreOrder`, which allocates an `ExpressionPoint` per node and a nested iterator
   state machine per subtree. Crossover only needs each candidate donor's node, length and depth, all of
   which are already stored on `ExpressionNode`, so a non-allocating traversal would not change what the
   operator selects. At a fifth of an unrefined run this is the second-largest cost in a run without
   refinement, and it is invisible in a refining run.
3. **Fitting allocates 21 MB per refined candidate at 20 000 rows.** Most of it is MathNet's per-iteration
   dense storage, but the adapter's own buffers are allocated per solve and could be reused across
   candidates. Worth attempting only with a measurement showing the reuse is visible in a whole run, since
   the adapter is under five percent of the total.
4. **Nothing else is worth optimizing while refinement is enabled.** Creator, selector and the remainder are
   each under one percent of a refining run, and crossover and mutation together are under half a percent.
   The configuration lever that matters is how many candidates get refined at all; `refiner.WithRate(...)`
   changes the run by more than any implementation change to the roles measured here would.

These are single-machine figures from a throwaway harness that is not part of the repository, taken in
`Release` with five runs per point and two complete sweeps. Refinement also changes the search trajectory,
so time per generation is comparable across configurations while time to reach a given quality is a
different question this profile does not answer. Production is untouched: the release build is clean and
the 2028 core tests pass.

### Operator benchmark and runtime model outcome

The whole-run profile divides a run across roles from the inside. This asks the opposite question: measure each
operator on its own with BenchmarkDotNet, compose those costs into a predicted run, and see whether the
prediction survives contact with a real one.

Every operator is measured on one population-sized batch of 80 candidates, because that is how an algorithm
calls it, so batch dispatch and allocation sit inside the measurement in the same proportion as in a run.
Nominal tree sizes of 10, 30 and 60 nodes produce benchmarked populations averaging 7.3, 31.0 and 56.6 nodes,
with 3.2, 15.0 and 27.8 operations and 1.5, 5.5 and 9.6 evolvable constants.

| Operator | 7.3 nodes | 31.0 nodes | 56.6 nodes | KB / call at 7.3 nodes |
| --- | ---: | ---: | ---: | ---: |
| Creator | 17.4 µs | 28.6 µs | 46.9 µs | 68 |
| Crossover | 18.7 µs | 74.7 µs | 137.7 µs | 83 |
| Mutator | 7.9 µs | 14.5 µs | 21.7 µs | 26 |
| Selector | 5.75 µs | 5.88 µs | 5.79 µs | 15 |

| Operator | Rows | 7.3 nodes | 31.0 nodes | 56.6 nodes |
| --- | ---: | ---: | ---: | ---: |
| Evaluator | 200 | 0.038 ms | 0.126 ms | 0.233 ms |
| Evaluator | 2 000 | 0.117 ms | 0.302 ms | 0.518 ms |
| Evaluator | 20 000 | 5.04 ms | 6.57 ms | 8.46 ms |
| Refiner | 200 | 14.1 ms | 54.0 ms | 63.3 ms |
| Refiner | 2 000 | 49.7 ms | 192.0 ms | 268.5 ms |
| Refiner | 20 000 | 272.1 ms | 741.7 ms | 1358.4 ms |

Three results stand on their own, independently of the model:

- **Selection does not depend on tree size**, 5.75 to 5.88 microseconds across an eightfold size range. It
  compares objective vectors and never walks an expression, and the measurement says so.
- **Crossover is close to linear in tree length**, 7.4 times the cost for 7.8 times the nodes, which is what an
  operator that traverses one parent and rebuilds one path should do.
- **Evaluation at 20 000 rows barely responds to tree size**, growing 1.7 times while the tree grows 7.8 times.
  At that row count the per-candidate fixed cost — the 160 KB prediction array and the data columns behind it —
  dominates the vectorized kernels that the expression itself contributes. This is independent evidence for the
  allocation hotspot the whole-run profile recorded, arriving from the opposite direction.

#### The model and what it got wrong

The predicted run is `creator + 30 × evaluator + 29 × (selector + crossover + mutator)`, plus the refiner at
every evaluation point when refinement is on. Those call counts come from the algorithm's structure and were
confirmed exactly against the counting wrappers. Costs are interpolated between the benchmarked populations on
the mean tree length the run actually evolved, which is not the length its search space allows: a search space
capped at 65 nodes evolves populations averaging 8.5 to 18.2 nodes, because nothing in the objective rewards a
larger expression once the target is fitted.

| Configuration | Rows | mean length | predicted run | measured run | predicted / measured |
| --- | ---: | ---: | ---: | ---: | ---: |
| no refinement | 200 | 6.8 | 2.1 ms | 4.1 ms | 52% |
| no refinement | 2 000 | 6.8 | 4.5 ms | 10.1 ms | 44% |
| no refinement | 20 000 | 7.4 | 152.2 ms | 68.1 ms | 223% |
| no refinement | 20 000 | 12.4 | 162.3 ms | 96.3 ms | 169% |
| refinement | 200 | 9.8 | 551.9 ms | 175.0 ms | 315% |
| refinement | 2 000 | 9.5 | 1891.2 ms | 999.0 ms | 189% |
| refinement | 200 | 25.7 | 1360.4 ms | 313.7 ms | 434% |

**The model's structure is right and its inputs do not transfer.** It lands between 44 and 55 percent of
measured wherever the run is cheap, and between 137 and 434 percent wherever allocation is heavy. Both errors
are systematic rather than noisy, and neither is the unattributed remainder the model deliberately omits: that
would produce a constant modest underprediction, not a sign change.

Holding the population and the operator fixed and changing only the harness locates the cause:

| Rows | BenchmarkDotNet | plain loop | inside a run |
| ---: | ---: | ---: | ---: |
| 200 | 0.038 ms | 0.069 ms | 0.079 ms |
| 2 000 | 0.117 ms | 0.215 ms | 0.240 ms |
| 20 000 | 5.04 ms | 1.79 ms | 2.10 ms |

The plain loop and the in-run wrapper agree with each other within 18 percent at every row count. BenchmarkDotNet
is the outlier in both directions, and its 20 000-row figure is not noise: its engine log shows 4.99, 5.00 and
5.21 milliseconds per operation across iterations of 128 operations each.

- **Below the large-object threshold it reads low**, by about half. Thousands of back-to-back operations on one
  population keep those trees and columns in cache and train the branch predictors, which a run that touches
  fresh trees every generation never gets. This is ordinary microbenchmark optimism.
- **Above it, it reads high**, by two and a half times, because evaluation allocates 12.6 MB per call at 20 000
  rows. Running that back to back allocates 1.6 GB per iteration and the collector never catches up, so every
  call pays collection cost. A run calls it thirty times with other work in between and pays much less per call.
  **For an allocation-bound operator, cost per call is a function of the call rate**, so an isolated
  steady-state measurement describes a duty cycle the algorithm does not have.
- **Refinement compounds this with a second error.** The benchmark refits the same unfitted population on every
  operation, which is the worst case; after the first generation a run refits candidates whose parameters are
  already near a fit, where Levenberg-Marquardt converges sooner. The refining predictions are the worst in the
  table for both reasons at once.

Consequences worth carrying forward:

1. **Per-operator benchmarks answer "did this change make this operator faster", not "how long will a run take".**
   For the first question they are the right instrument and the profile is not; for the second the ordering
   reverses. Neither replaces the other, which is the concrete version of the conclusion the whole-run profile
   reached about instrumentation.
2. **A predicted run should be believed only for operators that are not allocation-bound.** Selection, crossover
   and mutation transfer within the optimism factor; evaluation above the large-object threshold and refinement
   anywhere do not. Since measured: the
   [allocation hotspot fixes outcome](#allocation-hotspot-fixes-outcome) removed evaluation's allocation and
   evaluation now transfers like the others, which confirms this rule by satisfying it. Refinement still does
   not.
3. **The allocation hotspots are now indicted twice.** The evaluator's per-candidate array is what makes its cost
   rate-dependent, and the refiner's per-candidate megabytes do the same at a larger scale. Fixing the first
   would make evaluation predictable as well as faster.

These figures come from BenchmarkDotNet 0.15.8 on one machine, with five iterations after three warmup
iterations for the throughput jobs and three after one for the monitoring job used for refinement. The harness
is not part of the repository, and `docs/developer-guidelines.md` requires a separate accepted decision before
any of it becomes standing infrastructure.

### Allocation hotspot fixes outcome

The two allocation hotspots the profile and the operator benchmarks both indicted are fixed. Neither needed an
architectural change, and neither changes a single result: every recorded best objective across the nine
profile configurations is bit-identical to the figures above, and the core, API usage, experimental and
scenario suites pass unchanged.

**Evaluation no longer allocates a prediction array per candidate.**
`SymbolicRegressionProblem.Evaluate` called the allocating `Evaluate(DataFrame)` overload, which returns a
fresh row-length array. It now borrows one from `ArrayPool<double>.Shared` and evaluates into it through the
span overload. The scratch workspace was already pooled inside `ExpressionInterpreter`, and every consumer of
the predictions — `LinearScaling.Fit` and `Apply`, `IPredictionMetric.Evaluate` — already took spans, so the
change is confined to one method. A field would be faster still and is not available: `SingleSolutionProblem`
evaluates a batch under a configurable `Concurrency`, so two candidates may be in flight at once and would
share it.

**Crossover no longer allocates an expression point per node of the second parent.** `SubtreeCrossover`
selected its donor by walking `ExpressionPoint.TraversePreOrder` over the whole second parent, but used
nothing from those points except `.Node`: the replacement takes a node, and the length and depth its
eligibility test needs are stored on `ExpressionNode` already. Donor selection is now a recursive walk over
nodes, visiting the same nodes in the same order and drawing the same random numbers.

| Measurement | Before | After |
| --- | ---: | ---: |
| Unrefined run, 20 000 rows | 100 ms | 45 ms |
| Unrefined run allocation, 20 000 rows | 375 MB | 6.7 MB |
| Unrefined run full collections, 20 000 rows | 97 | 0 |
| Unrefined run allocation, 2 000 rows | 44.7 MB | 6.3 MB |
| One crossover, 5.2-node parents | 0.222 µs, 690 B | 0.118 µs, 150 B |

Run allocation is now effectively independent of row count — 6.3, 6.3 and 6.9 MB across a hundredfold row
range, against 11.7, 44.7 and 375 MB before — because nothing in the unrefined path scales its allocation
with the data any more.

**The measurement defect the profile spent most of its effort on is gone with it.** The 20 000-row unrefined
role shares were untrustworthy because roughly a fifth of that run was collection pause charged to whichever
operator was executing. With no collections left to charge, the same table now reads:

| Role | Before | After |
| --- | ---: | ---: |
| crossover | 21.4% | 1.5% |
| mutator | 6.4% | 0.8% |
| evaluator | 71.1% | 95.9% |

That 95.9 percent is the corrected reading this plan predicted from the row-independent costs before the fix
existed, now measured directly rather than reconstructed. The harness check that reports variation flatness at
the largest row count passes for the first time, at exactly 1.0 in both operators.

Consequences for the earlier outcomes, which remain accurate records of what was true when they were taken:

- The whole-run profile's first answer is strengthened rather than changed. Evaluation was already the
  dominant role in an unrefined run; it is now measurably 96 percent of one at 20 000 rows, so the
  interpreter's advantage carries almost the whole run at that size.
- The refining configurations barely move, exactly as their shares predict: refinement is 96 to 99 percent of
  those runs and the evaluator it dwarfs got cheaper. Full collections at 20 000 rows drop about a quarter,
  from roughly 11 300 to 8 500, which is the evaluator's share of the pressure leaving.
- The solver-backend gate is unaffected. The remaining allocation at 20 000 rows is the fitting path's, which
  the gate already considered and which configuration rather than adapter work addresses.

Re-running the operator benchmarks measures both operators directly. Costs are per population-sized batch.

| Operator | Population | Before | After | Allocation before | after |
| --- | ---: | ---: | ---: | ---: | ---: |
| Crossover | 7.3 nodes | 18.7 µs | 6.3 µs | 82.7 KB | 23.6 KB |
| Crossover | 31.0 nodes | 74.7 µs | 18.0 µs | 286.8 KB | 35.0 KB |
| Crossover | 56.6 nodes | 137.7 µs | 33.1 µs | 499.4 KB | 39.5 KB |
| Evaluator | 7.3 nodes, 20 000 rows | 5 037 µs | 718 µs | 12 648 KB | 143.6 KB |
| Evaluator | 31.0 nodes, 20 000 rows | 6 566 µs | 2 201 µs | 12 808 KB | 303.0 KB |
| Evaluator | 56.6 nodes, 20 000 rows | 8 459 µs | 3 799 µs | 12 971 KB | 467.0 KB |

Crossover is three to four times faster, and its allocation is now nearly flat in tree size — 23.6 to 39.5 KB
across an eightfold size range, against 82.7 to 499.4 KB before — because the part that scaled with node
count was the discarded points. Evaluation is two to seven times faster at 20 000 rows, and its allocation no
longer depends on the row count at all: 143.6 KB whether the data has 200 rows or 20 000.

**This also settles the diagnosis the operator-benchmark outcome could only argue for.** That outcome found
BenchmarkDotNet disagreeing with in-run measurement in opposite directions either side of the large-object
threshold, and attributed the over-reading to allocation saturating the collector under back-to-back
execution. Removing the allocation removes the over-reading:

| Rows | BenchmarkDotNet, before | in a run, before | BenchmarkDotNet, after | in a run, after |
| ---: | ---: | ---: | ---: | ---: |
| 200 | 0.038 ms | 0.079 ms | 0.037 ms | 0.068 ms |
| 2 000 | 0.117 ms | 0.240 ms | 0.091 ms | 0.173 ms |
| 20 000 | 5.04 ms | 2.10 ms | 0.718 ms | 1.248 ms |

The 20 000-row row was the outlier, reading 2.4 times the in-run cost where every other row read about half
of it. It now reads 0.58 of the in-run cost, in line with the others. What remains is one uniform effect in
one direction — the ordinary optimism of a benchmark that reuses one population with warm caches — and the
regime-dependent reversal is gone.

The runtime model inherits that. Its unrefined predictions were 44 to 55 percent of measured below the
threshold and 137 to 223 percent above it; they are now **37 to 52 percent everywhere**. A model that is
uniformly optimistic by about half is usable with one correction factor, which one that changes sign with row
count is not. The refining predictions are unchanged at 199 to 419 percent, as expected: the refiner was not
touched, it is still allocation-bound, and the benchmark still refits an unfitted population on every
operation where a run refits mostly-converged ones.

#### Two follow-up decisions

**Donor selection no longer preserves the random draw sequence, deliberately.** The first version of the fix
kept reservoir sampling so that the same draws were made in the same order. Draw-order stability is not a
property this operator promises, and giving it up buys a simpler algorithm: count the eligible donors, draw
one index, walk to it. That is one random draw per crossover instead of one per eligible node, at the cost of
a second walk over cached node fields, and it took an isolated crossover from 0.118 to 0.106 microseconds.
Cumulatively the operator went from 0.222 microseconds and 690 bytes to 0.106 and 150.

A single eligible donor is special-cased to draw nothing, which the reservoir version did implicitly and the
index version otherwise would not. Three `SubtreeCrossoverTests` script an exact random sequence and were
updated: two now select the first eligible donor rather than the second, and one had three of its five
scripted draws left dead by the change and was reduced to the two the operator now makes. The behavior each
test pins — that a donor exceeding the length or depth limit is rejected — is unchanged.

**The prediction buffer stays on `ArrayPool<double>.Shared` rather than moving to a per-operator pool.** The
concern was that renting thousands of times per run, potentially in parallel, makes the rent itself the
bottleneck and merely moves work from the allocator to the pool. Measuring one generation's worth of
acquisitions, eighty per operation, says otherwise:

| Strategy | 200 rows | 2 000 rows | 20 000 rows | Allocated |
| --- | ---: | ---: | ---: | ---: |
| Allocate per candidate | 3 185 ns | 29 457 ns | 394 767 ns | up to 12.8 MB |
| `ArrayPool.Shared` | 400 ns | 401 ns | 401 ns | none |
| Buffer retained per thread | 35 ns | 38 ns | 35 ns | none |
| `ArrayPool.Shared`, in parallel | 5 881 ns | 5 960 ns | 5 640 ns | 4.6 KB |
| Retained per thread, in parallel | 4 235 ns | 4 377 ns | 4 364 ns | 4.2 KB |

- **The pool costs about 5 nanoseconds per candidate and does not depend on the row count**, because
  `ArrayPool.Shared` serves a rent and return on one thread from a thread-local slot without locking.
  Evaluating one candidate at 20 000 rows costs about 9 microseconds, so acquisition is under a tenth of a
  percent of the operation it serves.
- **There is no contention to remove.** Both parallel rows sit near the 4.2-microsecond floor that
  `Parallel.For` itself costs for eighty items, and the batch is partitioned into contiguous ranges, so each
  worker rents and returns repeatedly on its own thread — the access pattern the thread-local slot exists for.
- **A thread-retained buffer is genuinely faster to acquire**, 0.44 nanoseconds against 5, but that is 0.05
  percent of a candidate's evaluation instead of 0.06. It buys nothing measurable and gives up the pool's
  trimming: a retained buffer holds its memory for the life of the thread, 160 KB per worker at 20 000 rows,
  after the problem that needed it is gone.

One sharp edge is worth recording rather than acting on: `ArrayPool.Shared` keeps one array per size bucket
per thread, and `SymbolicRegressionProblem.Evaluate` rents the destination while `ExpressionInterpreter`
rents its workspace inside it. Should both ever land in one bucket, the inner rent misses the thread-local
slot and takes the per-core locked path. The flat 401 nanoseconds says this is not happening at these sizes.

Remaining hotspot candidates: the adapter's per-solve buffers could be reused across candidates, and the
refiner is now the only allocation-bound operator left. Nothing else in a refining run is worth optimizing
while refinement is 99 percent of it.

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
- compares the current `ArrayPool<double>`-backed `Execution` with a benchmark-only execution-owned-array variant across repeated construction, reuse, and complete parameter-fitting workloads; report throughput, allocated bytes, and garbage-collection pressure, including large-object-heap-sized workspaces;
- uses real lowered symbolic expressions and parameter-fitting workloads in addition to focused synthetic kernels.

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
6. measure representative end-to-end workloads and attribute their cost across differentiation, adapter and solver;
7. design evaluator or memetic integration only after the direct path is stable;
8. remove old AutoDiff or MathNet dependencies only when no maintained library feature uses them.

Do not delete tests merely because the implementation changes. Adapt behavioral tests to the owning replacement layer.

### Solver-backend decision gate

The original gate asked whether MathNet should be retained or replaced. That fork is not actionable as
written, because no alternative Levenberg-Marquardt implementation exists to switch to and writing one is
not work this replacement wants to take on. Inspection of the current code also settles part of the
question outright:

- **The legacy implementation calls the same MathNet Levenberg-Marquardt.**
  `SymbolicRegressionParameterOptimization` invokes it directly, so old and new differ in their
  differentiation and marshalling layers rather than in their solver. A comparison between them measures
  exactly what this replacement changed, with the solver held constant on both sides.
- **Dropping the solver would not drop the dependency.** `HyperVolumeCalculator` uses MathNet statistics
  and the legacy optimizer uses its linear algebra, so the package reference stays regardless of what the
  fitting path does.
- **The dependency actually up for elimination is `AutoDiff`.** Its only remaining consumer is
  `TreeToAutoDiffTermConverter`, reached solely through the legacy optimizer, so retiring that optimizer
  removes the dependency from Experimental while replacing MathNet removes none.

The gate is therefore restated: **MathNet is retained.** Measurement attributes cost across the
differentiation engine, the adapter and the solver, and only a specific quantified problem that neither
adapter changes nor solver configuration can address would reopen the question. Writing a Levenberg-
Marquardt implementation trades a maintained one for our own damping, trust-region and convergence-test
edge cases, for a dependency that stays either way.

One suspected cost is already recorded and is fixable without touching the solver. MathNet's calling
convention asks for the model values and the Jacobian through separate callbacks, so the adapter computes
the primal twice per iteration, once in `EvaluateModel` and once inside `EvaluateWithJacobian`. If
measurement shows that duplicated forward sweep is material, the fix is to retain the primal computed
during the Jacobian call inside our own adapter.

## Decision Gates

The first vertical slice has settled these decisions:

1. the AD engine has internal types and a direct `ExpressionTree` lowering adapter;
2. lowering uses a mutable builder/compiler, produces an immutable reusable program, and evaluates through mutable run-scoped workspaces;
3. spans and contiguous buffers are preferred internally, while direct MathNet storage is allowed at the adapter boundary when it avoids measured cost;
4. MathNet LM is the first backend, with no broad solver-plugin architecture;
5. the first objective is raw full-data least squares over evolvable constant occurrences only;
6. numeric parameter fitting always rebuilds a successful LM result; best-point retention and acceptance are separate later policies;
7. linear scaling remains an evaluation concern outside numeric parameter fitting;
8. output parity means predictions and loss within tolerance, not identical parameters or solver traces;
9. generic numerical algorithms and memetic composition are later generalizations, not delivery gates.

Remaining decisions should be made from implementation evidence:

1. the precise program instruction layout, derivative buffer layout, and Jacobian batch size;
2. which built-in expression operations are supported in the first increment and their structured failure details;
3. the exact MathNet API and storage shape with the lowest verified adapter overhead;
4. stopping defaults and later retention or acceptance policy;
5. whether any measured solver-side cost is large enough to reopen the retained-MathNet decision;
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
| 6. Direct numeric parameter fitting | Full-data LM run, simple high-level failure behavior, identity behavior without evolvable constants, and rebuilding after an LM run | Sampling, retention, acceptance, linear-scaling internals |
| 7. Immutable rebuilding | One `ReplaceMany` operation produces the optimized expression | In-place mutation, fixed-constant replacement |
| 8. Behavioral comparison | Predictions and raw MSE satisfy analytic expectations and match maintained legacy behavior where applicable | Exact parameter-vector or solver-trace equivalence |
| 9. Development-branch integration | Current `dev` operator-base improvements integrated, reviewed, and validated | Refiner implementation |
| 10. Evaluator contract simplification | Fitness-only evaluator results, migrated wrappers, algorithms, and analysis hooks | Candidate-transforming evaluation of any kind |
| 11. Refiner operator | Candidate-to-candidate refinement, composition topologies including transient-refinement evaluation, plus explicit placement in applicable algorithms | Objective evaluation or retention inside the general refiner contract |
| 12. Improvement-checking refiner | Objective-aware retention as a composable wrapping refiner with configurable evaluator and comparer | A second refinement integration mechanism, or acceptance hidden from configuration |
| 13. Producer refinement composition | A re-decided transformed-operator concept, then the deferred creator, crossover, mutator, and offspring-production conveniences after canonical mechanisms are established | Producer wrappers treated as the primary refinement integration, or a refiner accepted by the existing transformed operators without revisiting the concept |
| 14. Performance decision | AD, adapter, solver, refinement, evaluator, and complete pipeline benchmarked, with cost attributed across the layers and compared against the legacy implementation | Unmeasured backend abstraction or specialization, or a solver written to replace one that measurement has not indicted |
| 15. Later generalization | Public APIs, other optimizers, sampling, and additional memetic composition as justified | Changes made only for hypothetical reuse |
| 16. Operation-model consolidation | One `Operation`, one struct per operation carrying every facet through static abstract members, one catalog projected into a delegate table, and consumers reading that table instead of holding switches. Done, the AD reverse sweep included. See [Operation-model consolidation review outcome](#operation-model-consolidation-review-outcome) and [AD reverse sweep](#ad-reverse-sweep) | A source generator, a runtime registry, open primitive kernels, behavior inside the instruction stream, or reordered opcode values |

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
- producer-specific refinement wrappers before the canonical refiner and improvement-checking mechanisms are established;
- acceptance through the problem's configured regression metric;
- differentiating through evaluation-time linear scaling or injecting root scaling parameters;
- replacing every existing HeuristicLib optimization algorithm with a gradient-based one.

These remain possible extensions. The first replacement should establish boundaries that can accommodate them without implementing them prematurely.

## First Vertical-Slice Completion Criteria

The first parameter-fitting implementation is complete when:

- the internal AD program evaluates values, gradients, and the required Jacobian shape without per-operation allocation;
- analytic and finite-difference tests cover every supported operation and its invalid-domain behavior;
- `ExpressionTree` lowering preserves optimizable occurrence identity, fixed constants, macro semantics, and originating symbols;
- MathNet LM can optimize all training rows through a measured adapter;
- compilation and binding failures, unsupported models, invalid numerical outcomes, and solver outcomes are structured and tested at their owning lower layers, while `NumericParameterFitter` deliberately exposes only success or failure;
- the input tree remains unchanged and every successful LM parameter vector is applied through one `ReplaceMany` call;
- non-finite optimized values propagate into the rebuilt expression without an implicit acceptance safeguard;
- predictions and raw MSE match retained behavior within documented tolerances where the behavioral contracts overlap;
- benchmarks isolate AD, adapter, solver, and end-to-end costs, attribute them across those layers, and compare the complete path against the legacy implementation;
- the direct capability is documented well enough to design its eventual public facade without exposing AD internals.

The benchmark criterion is met. [Legacy comparison outcome](#legacy-comparison-outcome) compares the
complete path against the legacy implementation, [Evaluation throughput
outcome](#evaluation-throughput-outcome) isolates evaluation, [Solver cost attribution
outcome](#solver-cost-attribution-outcome) attributes per-fit cost and allocation across differentiation,
adapter and solver, and [Whole-run profile outcome](#whole-run-profile-outcome) places all of it inside a
run and gives refinement its cost statement. Together they close the performance-decision increment:
MathNet is retained on measured grounds, and the hotspots the profile revealed are recorded as separate
work rather than folded into this slice.

The broader redesign is complete later when justified public numerical-optimization APIs, explicit refiner integration, transient-refinement evaluation, additional solvers, and legacy retirement each have an explicit outcome. They are deliberately not conditions for completing the first vertical slice.
