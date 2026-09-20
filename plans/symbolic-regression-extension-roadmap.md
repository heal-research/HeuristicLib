# Symbolic Regression Extension Roadmap

## Purpose

This roadmap owns the symbolic-regression capabilities that are deliberately **out of scope for the
foundation branch**: grammar-constrained search, typed and categorical terminals, structure templates,
shape constraints, algebraic normalization, vectorial GP, time-series support, and interval-arithmetic
evaluation.

Each of these is large enough to justify its own branch, its own review, and in most cases its own
implementation plan. This document is therefore a **cross-branch roadmap, not a single implementation
plan**: it records the design already settled for each feature, states what each one depends on, and
sequences them. It does not schedule them into one delivery.

The foundation they extend is specified in
[symbolic-regression-decisions.md](symbolic-regression-decisions.md) and documented for users in
[`docs/guide/domains/symbolic-regression.md`](../docs/guide/domains/symbolic-regression.md). Read those first; this one assumes the
genotype, compiled representation, problem composition, evaluation contract, and unrestricted operator
family as given.

## Branch Model

- **One branch per feature.** Every section below is a candidate branch. None of them is a prerequisite
  for merging the foundation.
- **A shared extensibility-design branch comes first.** Several features would otherwise each invent
  their own answer to the same questions: what a value type is, where restriction lives, how a second
  evaluation backend attaches. [Extensibility design](#ext-0-extensibility-design) scopes that branch.
- **Each feature branch carries its own plan document** when its design work exceeds what is recorded
  here. This roadmap holds the settled design and the boundaries; a feature branch expands its own
  section into a full plan rather than growing this file.
- **Feature branches do not renegotiate foundation decisions** silently. A feature that requires a
  foundation change states that requirement explicitly and gets it decided on its own terms.
- **Legacy retention is not a blocker.** Retained mutable components stay in
  `HEAL.HeuristicLib.Experimental` under the `Legacy` folder for as long as their replacement does not
  exist. See [Legacy retirement dependencies](#legacy-retirement-dependencies).

Identifiers below (`EXT-n`) are branch handles. The former stage number from the redesign plan is
recorded with each one so existing references and git history stay traceable.

## Foundation Boundary

### Stays in the foundation branch

- the immutable `ExpressionTree` genotype, `ExpressionNode`, `ExpressionPoint`, and the low-level
  immutable editing operations
- `CompiledExpression`, `Instruction`, the closed built-in `Operation`/opcode catalog, and the scalar
  `double` `ExpressionInterpreter`
- `ExpressionDraft`, `InfixExpressionParser`, and the infix, C#, Python, and LaTeX formatters
- `SymbolicRegressionProblem`, `RegressionData`, the regression and expression metric families,
  `SymbolicRegressor`, `BoundedRegressor`, `LinearlyScaledRegressor`, and fitness-time linear scaling
- `ExpressionTreeSearchSpace` and the unrestricted operator family: the five creators, `SubtreeCrossover`,
  and the four mutators
- numeric parameter fitting end to end: the internal automatic-differentiation engine, the
  Levenberg-Marquardt adapter, expression lowering, `NumericParameterFitter`, the `Refiner` operator role
  with its composition topologies, and algorithm integration

### Leaves the foundation branch

Everything in [Extension branches](#extension-branches). The redesign plan retains a pointer to this
document in place of its former Stage 4 and Stage 5 bodies.

### Foundation follow-ups that are not extensions

These stay with the foundation and are not covered here, even though they touch the same types:

- JSON and binary serialization for expressions, deferred from Stage 1
- a revised invalid-value policy if `NaN` proves insufficient
- n-ary symbolic operations instead of binary-only operation arity
- the public-API review that decides how much of the internal automatic-differentiation and
  Levenberg-Marquardt surface becomes public
- immutable data snapshots, builders, versioning, and cache invalidation

Schema-based variable matching beyond Stage 1 name binding sits on the boundary: name binding is
sufficient for scalar regression, and the schema requirement is created by
[EXT-2](#ext-2-factor-variables-and-typed-terminals) and
[EXT-6](#ext-6-vectorial-gp-value-system-support). It is designed there rather than in the foundation.

## EXT-0: Extensibility Design

**Recommended first branch. Design only, no feature implementation.**

Four of the features below need an explicit value or type model, three need a restriction model, and two
need a second evaluation backend. Designing those once, against all the known consumers, is cheaper than
discovering the shared shape from whichever feature happens to land first, and much cheaper than
retrofitting it into a genotype and interpreter that are already public.

This branch should settle:

- **Value categories and shapes.** An explicit model with at least scalar and vector categories and a
  path to fixed-rank tensors. Scalar must be an explicit category, never `null`, rank zero by convention
  alone, or an untyped default. Categorical terminals and interval values must each have a recorded
  answer for whether they are value categories, evaluation domains, or terminal payload kinds.
- **The evaluation-domain boundary.** Interval arithmetic reuses the scalar genotype under a different
  numeric domain; vectorial GP changes what a valid expression is. The distinction between "same
  candidates, different evaluation" and "different candidates" must be a stated rule, because it decides
  whether a feature needs a search-space type at all.
- **Search-space families and operator families.** How a restricted search space declares its operator
  family, and the operator-family preset mechanism that lets a user switch creator, mutator, crossover,
  and repair together instead of rewiring them one by one. This was deferred from the foundation's
  Stage 3 with exactly these consumers in mind.
- **Where restriction lives.** Grammar is the first restricted family, but templates and typed values
  also constrain what is locally admissible. Decide whether they share one restriction concept or remain
  separate models, without adding a public generic restriction-provider abstraction before the grammar
  implementation proves the shape.
- **Containment per family.** The unrestricted space uses aggregate-coverage matching; the grammar space
  will probably use structural matching. See
  [Containment](symbolic-regression-decisions.md#settled-decisions). This branch decides
  whether containment is a per-family policy or a fixed rule, and records the provisional grammar answer
  as settled or still open.
- **Symbol type signatures.** How a `Symbol` carries input and output signatures so grammar productions,
  typed terminals, and vector operations describe themselves consistently.
- **Compiled-representation extension points.** How `CompiledExpression` can carry resolved-overload or
  value-shape metadata for the features that need it, and how a second interpreter backend attaches,
  both **without** adding cost to the scalar fast path.

Non-goals for this branch: implementing any feature below, a plugin or registry architecture, a source
generator, boxing or virtual per-instruction dispatch in the scalar interpreter, or any change justified
only by a hypothetical extension not listed in this roadmap.

Done when every extension section below can name the shared mechanism it will use, or explicitly record
that it needs none.

## Extension Branches

### EXT-1: Grammar-Constrained Symbolic Regression

_Formerly redesign-plan Stage 4._

Add the smallest typed grammar model needed for valid scalar symbolic regression:

- operation signatures and scalar value type first
- fluent grammar API first; EBNF authoring later as syntax over the same model
- grammar-preserving creation, mutation, crossover, and repair
- scalar final-output enforcement for standard regression
- `GrammarSymbolicExpressionSearchSpace` as a distinct search-space type, not a flag on the unrestricted
  search space
- grammar-specific creator, mutator, crossover, and repair operators

Rules:

- Grammar-constrained operators should ask for viable options at a site instead of blindly sampling edit
  points and relying on rejection.
- Grammar containment will probably use structural origin-symbol matching, or a grammar-specific
  refinement, because production and nonterminal identity can be semantically significant. This remains
  provisional until the grammar model is designed; it must not inherit unrestricted aggregate coverage by
  default.
- The first grammar implementation supports scalar expressions, but scalar must be an explicit grammar
  value category rather than the absence of type information. Operation productions have explicit input
  and output signatures so later vector and tensor value categories can extend the model without
  replacing the grammar representation.
- Grammar logic drives option selection in the first restricted implementation. Do not add a public
  generic restriction-provider abstraction before the grammar implementation proves the shape.
- `Contains` is a final validation check for grammar operators, not the primary construction strategy.
- Grammar-constrained operators may enumerate viable sites, cache grammar-derived metadata, use repair,
  or use bounded retry.
- The grammar-aware path may pay grammar costs; the unrestricted path must not.
- Grammar operators should reuse low-level immutable genotype operations where practical, but not share
  the unrestricted selection logic when grammar validity changes the set of legal edits.
- Restricted operators are not implicitly reused in unrestricted contexts. A grammar-preserving operator
  may produce candidates that are also valid in an unrestricted search space, but it still requires
  grammar context and therefore belongs to the grammar operator family.

Tests: typed grammar validity, grammar operator option selection, grammar-preserving operators, scalar
output enforcement, grammar-specific bounded failure behavior, and one usage spec showing a restricted
grammar such as `log(variable)` only.

#### Benchmarks: the last outstanding old-versus-new comparison

Parameter fitting, interpretation, evaluation, and refinement were already measured against the legacy
implementation and clearly favor the immutable one; see
[measurements](symbolic-regression-decisions.md#measurements).

**Grammar changes only creation, mutation, and crossover.** Everything else is identical whether or not a
grammar constrains the search space, so those results carry over and must not be repeated here.

This branch therefore owes exactly one comparison, and it is the last one that requires the legacy system
to still exist:

- grammar-guided **creation** against the retained mutable `ProbabilisticTreeCreator` and
  `BalancedTreeCreator`
- grammar-preserving **mutation** against its legacy counterpart
- grammar-preserving **crossover** against its legacy counterpart

Measure them the way the foundation's operator benchmarks do — one population-sized batch, across a
representative range of tree sizes, with allocation reported alongside time — so the numbers compose with
the existing runtime model rather than standing alone. Expect the grammar path to be slower than the
unrestricted one on both sides; the comparison of interest is immutable-with-grammar against
legacy-with-grammar, not against the unrestricted fast path.

Run this before proposing the grammar-dependent legacy components for deletion. Once they are gone the
comparison cannot be reconstructed.

Depends on: [EXT-0](#ext-0-extensibility-design) for signatures, families, and containment.

Unblocks: the largest legacy batch. See
[Legacy retirement dependencies](#legacy-retirement-dependencies).

### EXT-2: Factor Variables And Typed Terminals

_Formerly the deferred factor-variable row of the reference behavior matrix._

HeuristicLab's `FactorVariable` and `BinaryFactorVariable` have no replacement and are recorded as
**not started**, because they require a deliberate categorical-data and terminal-payload design rather
than another opcode.

Scope:

- a categorical value representation in the data layer, alongside the existing `Series<T>` and
  `DataFrame` model
- typed terminal symbols whose payload is a factor level or a level-to-weight mapping, not a `double`
- the compiled-side payload representation, and how the scalar interpreter resolves a factor terminal to
  a numeric contribution without a per-instruction branch on payload kind
- containment, creation, and local perturbation rules for categorical terminals, including what a
  perturbation of a factor terminal means
- how numeric parameter fitting treats factor weights: whether level weights are optimizable parameters
  and, if so, how they are lowered

Depends on: [EXT-0](#ext-0-extensibility-design) for the terminal-payload and typed-value answer. Overlaps
[EXT-6](#ext-6-vectorial-gp-value-system-support) on typed variable schema; whichever lands first should
establish the schema.

### EXT-3: Structure Templates

_Formerly redesign-plan Stage 5.1._

- Structure templates use a different composite genotype shape: one immutable fixed template plus one
  evolvable `ExpressionTree` component per wildcard slot.
- A wildcard owns or references an expression search space, not a nested symbolic-regression problem. The
  outer problem owns data, compilation, evaluation, objectives, and fitness because only the fully
  instantiated template is evaluable.
- The composite search space validates the fixed template, wildcard count, wildcard-to-component
  association, each component against its own unrestricted or grammar-constrained search space, and the
  component output type against the wildcard's expected type.
- Template structure is not exposed to ordinary expression mutation or crossover. Operators first select
  a wildcard component and then delegate creation, mutation, or crossover to the operator family
  compatible with that component's search space.
- Crossover requires compatible templates and wildcard slots. Whether repeated appearances of one named
  wildcard share one component or represent independent slots must be explicit in the template model.
- Compilation substitutes component roots while traversing the fixed template and produces the ordinary
  compact `CompiledExpression`. A simple first implementation may materialize the combined immutable
  `ExpressionTree`; a later direct compiler may avoid that temporary allocation without changing genotype
  semantics.
- Components retain normal immutable structural sharing. Composite equality and hashing include the
  template and the ordered wildcard components.
- Manually replacing placeholder nodes in an ordinary `ExpressionTree` can emulate one template instance
  today, but it does not protect the fixed structure or provide component-specific containment and
  operators. It is not the first-class template design.

Tests: template and wildcard invariants, wildcard-specific search-space containment, component output
compatibility, immutable component replacement, fixed-template preservation under mutation and crossover,
shared versus independent repeated wildcards, template instantiation and compilation, full-expression
evaluation, and one usage spec showing wildcard components with unrestricted and grammar-constrained
expression search spaces.

Depends on: [EXT-0](#ext-0-extensibility-design) for operator-family delegation. Reaches full value with
[EXT-1](#ext-1-grammar-constrained-symbolic-regression), since grammar-constrained wildcard components are
a primary use case, but an unrestricted-only first implementation is viable.

### EXT-4: Shape Constraints

_Formerly redesign-plan Stage 5.2._

- Shape constraints are evaluation components over predictions, derivatives, sampled expression behavior,
  or other model observations. They should not create `ShapeConstrainedUnrestricted...` and
  `ShapeConstrainedGrammar...` search-space pairs unless the constraint truly changes local subtree
  admissibility.
- Do not add a `ShapeConstrainedSymbolicRegressionProblem` or a shape-constraint list on the base problem.
  Shape-constrained use supplies a metric or constraint evaluator that owns its constraint configuration
  and consumes shared evaluation context from the problem.
- Single-objective and multi-objective shape-constrained regression are evaluator configurations:
  aggregate violations for single-objective use, or expose separate violation dimensions for
  multi-objective use.

Tests: shape-constraint evaluator ownership, empty-normal-problem API shape, single-objective penalty
aggregation, multi-objective violation dimensions, and derivative and sampling fixtures.

Depends on: nothing structural. This is evaluator-level work and could run early. Derivative-based
constraints can reuse the foundation's automatic-differentiation engine; interval-based constraints want
[EXT-8](#ext-8-interval-arithmetic-evaluation).

### EXT-5: Algebraic Normalization And Candidate Transformers

_Formerly redesign-plan Stage 5.3._

Algebraic normalization, simplification, constant folding, equivalent-form comparison, and numeric
refinement are candidate transformers, comparers, or evaluators, not new expression search-space flavors.
The foundation's `Refiner` role already provides the `Candidate → Candidate` mechanism these need.

Tests: constant folding, candidate immutability, equivalent-form comparison fixtures, and transformer and
evaluator composition.

Depends on: nothing structural. This is the smallest extension and a reasonable first one after
[EXT-0](#ext-0-extensibility-design).

### EXT-6: Vectorial GP Value-System Support

_Formerly redesign-plan Stage 5.4._

- Vectorial GP means that one expression value for one observation can be a vector or tensor. This value
  dimension is distinct from the current interpreter's batched `double` buffers, which contain one scalar
  value for each observation.
- The current scalar genotype and interpreter cannot directly represent vector-valued variables. The
  extension requires explicit expression value metadata, typed variable schema, typed grammar productions,
  resolved operation signatures, shape-aware compiled instructions, and shape-aware evaluation buffers.
- Introduce an explicit value category and shape model with at least scalar and vector categories and a
  path to fixed-rank tensors. Do not encode scalar as `null`, rank zero by convention alone, or an untyped
  default.
- Variable symbols bind to schema entries carrying value category and shape. Numeric constants initially
  remain scalar and participate in broadcasting.
- Grammar productions define input and output signatures such as `Vector + Vector -> Vector`,
  `Vector + Scalar -> Vector`, and `Mean(Vector) -> Scalar`. The grammar enforces that the
  symbolic-regression root returns a scalar.
- Broadcasting is part of resolved operation semantics, not an implicit interpreter guess. Initial support
  should prefer fixed per-observation shapes; general NumPy-style rank and dimension broadcasting requires
  explicit compatibility rules and runtime validation when dimensions are not statically known.
- Compilation retains compact postorder/RPN storage but records the resolved operation overload or
  equivalent value-shape metadata needed by execution. It must not rediscover grammar overloads in the
  interpreter hot loop.
- Vector and tensor interpreter buffers represent `batch size x elements per observation`; aggregation
  operations reduce only the per-observation value dimensions and never accidentally reduce the
  observation batch dimension.
- Keep the current scalar `double` interpreter as a specialized fast path. Do not force scalar execution
  through boxed values, virtual per-instruction dispatch, or a general tensor union solely to support
  vectorial GP.
- Add a value-system-aware search-space and operator family only where typed local admissibility differs.
  Avoid subclasses for every cross-product of vectorial GP with templates, shape constraints, or other
  evaluator-only extensions.

Tests: explicit scalar, vector, and tensor metadata; typed variable binding; scalar and vector operation
signatures; broadcasting compatibility and rejection; scalar aggregation roots; grammar-preserving
creation, mutation, and crossover; compiled shape metadata; interpreter buffer layout; aggregation over
the correct dimensions; result typing; and search-space containment for value-compatible expressions.

Depends on: [EXT-0](#ext-0-extensibility-design) for the value model and
[EXT-1](#ext-1-grammar-constrained-symbolic-regression) for typed productions. This is the largest
extension and the one most likely to need its own multi-checkpoint plan.

### EXT-7: Time-Series Expression Support

_Formerly redesign-plan Stage 5.5._

Time-series support follows the same direction as vectorial GP when it changes value metadata or available
opcodes. Lag and window semantics belong in operation signatures, interpreter binding, and data-view
context before they become separate problem types.

Tests: lag and window binding, time-aware operation signatures, row-window validity, interpreter behavior
fixtures, and regression usage specs over time-series data.

Depends on: [EXT-6](#ext-6-vectorial-gp-value-system-support), or at least its value model.

### EXT-8: Interval-Arithmetic Evaluation

_Formerly redesign-plan Stage 5.6._

- Interval arithmetic is initially an alternative evaluation domain for an otherwise ordinary scalar
  expression, not a new genotype. A numeric constant `c` becomes the degenerate interval `[c, c]`,
  variable bindings provide intervals, and built-in operations apply their interval-arithmetic
  definitions.
- The existing `ExpressionTree` and compact `CompiledExpression` can be reused because their structure,
  opcodes, numeric constants, and variable references are sufficient for scalar interval evaluation. What
  is missing is an interval value type, interval input binding, operation semantics, and a dedicated
  interpreter or evaluation backend.
- Keep `ExpressionInterpreter` specialized for `double`. Add a separate interval interpreter rather than
  genericizing the hot scalar interpreter through interface dispatch or boxed numeric values.
- Define interval behavior explicitly for division across zero, logarithm and square root outside their
  domains, infinities, empty and invalid intervals, and outward rounding. These rules are part of interval
  evaluation semantics and must not inherit the ordinary `double` invalid-result policy accidentally.
- Validate compiler optimizations against interval semantics. Unoptimized compilation is the reference
  path; constant folding and identity elimination may be enabled only when they preserve the selected
  interval semantics.
- Interval evaluation can support range analysis, safety checks, and shape constraints without changing GP
  containment. If intervals later become first-class expression values that mix with scalars or vectors,
  they join the typed value-system and grammar work from
  [EXT-6](#ext-6-vectorial-gp-value-system-support) instead of being handled by the scalar interval
  backend.
- Keep interval results separate from ordinary prediction series. The regression objective still consumes
  scalar predictions unless an evaluator deliberately converts interval results into penalties, bounds, or
  another objective representation.

Tests: interval construction and invariants, degenerate constants, variable binding, arithmetic enclosure,
division across zero, domain-invalid unary operations, infinities and empty intervals, outward-rounding
fixtures, optimized-versus-unoptimized semantic equivalence where optimization is enabled, range-analysis
use, and no regression in the specialized `double` interpreter.

Depends on: [EXT-0](#ext-0-extensibility-design) for the evaluation-backend attachment point. Independent
of grammar and of the value system, because it reuses the scalar genotype unchanged.

## Dependencies And Suggested Order

```text
EXT-0 Extensibility design
  |
  |-- EXT-5 Normalization and transformers      (no structural dependency)
  |-- EXT-4 Shape constraints                   (no structural dependency)
  |-- EXT-8 Interval arithmetic                 (needs the backend attachment point)
  |
  `-- EXT-1 Grammar
        |-- EXT-3 Structure templates           (usable earlier, best with grammar)
        `-- EXT-6 Vectorial GP
              `-- EXT-7 Time series

EXT-2 Factor variables shares the typed-terminal and schema question with EXT-6;
whichever branch lands first establishes the schema for the other.
```

Suggested order and why:

1. **EXT-0**, so the shared questions are answered once.
2. **EXT-1 Grammar**, because it unblocks the largest legacy batch and is a prerequisite for the typed
   work. It also owes the one remaining old-versus-new comparison — grammar-affected creation, mutation,
   and crossover — which has to run while the legacy grammar operators still exist.
3. **EXT-5** or **EXT-4** whenever convenient. Both are evaluator- and transformer-level and do not queue
   behind anything.
4. The remaining features by demand rather than by dependency, respecting the graph above.

Nothing in this order is binding. It reflects dependency and unblocking value, not commitment.

## Legacy Retirement Dependencies

Retained mutable components stay in `HEAL.HeuristicLib.Experimental` under the physical `Legacy` folder,
and that is the accepted end state for the foundation branch. Their retirement is scheduled by the
extension branch that replaces them. The full process, lifecycle states, and mandatory deletion approvals
remain in
[symbolic-regression-decisions.md](symbolic-regression-decisions.md#legacy-status).

| Retained legacy component                                                                               | Retired by                                                                                              |
| ------------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------- |
| Grammar-aware mutable `ProbabilisticTreeCreator`, `BalancedTreeCreator`, and their shared creator base  | [EXT-1](#ext-1-grammar-constrained-symbolic-regression)                                                 |
| Legacy grammars, symbols, and tree search spaces                                                        | [EXT-1](#ext-1-grammar-constrained-symbolic-regression)                                                 |
| Legacy `SymbolicRegressionProblem` and the evaluator-based regression objective layer                   | [EXT-1](#ext-1-grammar-constrained-symbolic-regression)                                                 |
| `SymbolicRegressionParameterOptimization`, `TreeToAutoDiffTermConverter`, and the `AutoDiff` dependency | [EXT-1](#ext-1-grammar-constrained-symbolic-regression), as one batch with the legacy problem           |
| `RemoveBranchManipulation` and its required base                                                        | [EXT-1](#ext-1-grammar-constrained-symbolic-regression)                                                 |
| The mutable genotype, compiler, and interpreter                                                         | last, after every consumer above has migrated                                                           |
| The `Dataset`-based data-analysis support group                                                         | with its final consumer, per [data-analysis-modernization-plan.md](data-analysis-modernization-plan.md) |
| HeuristicLab `FactorVariable` and `BinaryFactorVariable` behavior                                       | [EXT-2](#ext-2-factor-variables-and-typed-terminals)                                                    |

Two constraints carry over from the migration plan and must not be lost in the branch split:

- **Only one old-versus-new comparison is still outstanding, and it is narrow.** Parameter fitting,
  interpretation, evaluation, and refinement have all been measured and clearly favor the immutable
  system; those components can be retired as soon as their consumers have migrated, with no further
  benchmarking. What remains unmeasured is grammar-affected creation, mutation, and crossover, because no
  grammar implementation exists to measure. That comparison belongs to
  [EXT-1](#ext-1-grammar-constrained-symbolic-regression) and must run before the grammar-dependent legacy
  components are deleted, since it cannot be reconstructed afterwards.
- **No legacy source file or test is deleted without explicit approval in chat**, per component or per
  clearly enumerated batch. Introducing a replacement never implies approval to delete.

## Shared Rules

These carry over from the redesign plan and apply to every branch here:

- Extensions compose around the expression component. A new search-space type is justified only when the
  feature changes the set of structurally valid candidates or the local closure rules needed by creation,
  crossover, mutation, or repair.
- Evaluation domains that do not change expression admissibility, such as scalar interval analysis, reuse
  the genotype and compiled representation through a separate evaluator or interpreter. Value systems that
  change valid operation signatures, such as vector and tensor values, require typed grammar and
  search-space support.
- Avoid subclasses for every cross-product of features. Templates, shape constraints, vectorial values, and
  interval evaluation must not multiply into combined search-space types.
- The scalar `double` path stays the specialized fast path. No extension may add dispatch, boxing, or
  metadata cost to it.
- Evaluators measure and never transform. Refinement, repair, simplification, and normalization belong to
  the `Refiner` role.
- Determinism is required: the same candidate, problem data, operator configuration, and explicit random
  source must produce the same result.
- HeuristicLab is a behavioral reference, not an architectural target. Prefer reimplemented HeuristicLib
  fixtures over copied source; any direct source reuse requires an explicit licensing decision.

## Validation

Every branch here uses the repository-standard commands:

```powershell
dotnet restore
dotnet build --configuration Release --no-restore
dotnet test --configuration Release --no-restore
dotnet format ./HEAL.HeuristicLib.slnx --verify-no-changes --no-restore --severity error
```

Place fast invariants in `test/HeuristicLib.Tests`, workflows in `test/HeuristicLib.Tests.Scenarios`, and
public API shape in `test/HeuristicLib.Tests.ApiUsageSpecs`.
