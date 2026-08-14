# Symbolic Regression Redesign Plan

## Goal

Replace symbolic regression's mutable tree candidate with an immutable persistent hierarchical `ExpressionTree` genotype. Compile that genotype into a compact postorder/RPN `CompiledExpression` for execution. Move the current mutable `SymbolicExpressionTree` to a `Legacy` namespace with `[Obsolete]` markers and keep it only for temporary migration and golden reference behavior tests until the new system fully replaces it.

HeuristicLab is the behavioral reference, not the target architecture. The first reference package is [HeuristicLab.Problems.DataAnalysis.Symbolic.Regression/3.4](https://github.com/heal-research/HeuristicLab/tree/main/HeuristicLab.Problems.DataAnalysis.Symbolic.Regression/3.4).

Implementation order:

- Stage 0: close design holes and add API usage specs.
- Stage 1: build the immutable scalar expression core and reference behavior harness.
- Stage 2: rebuild problem composition and the evaluator contract around the new genotype.
- Stage 3: add the unrestricted scalar search space and fast unrestricted search operators.
- Stage 3.1: add constant optimization with Levenberg-Marquardt and automatic differentiation after the unrestricted operators are stable.
- Stage 4: add the grammar-constrained scalar search space and grammar-preserving search operators.
- Stage 5: add extensions in sub-stages: templates, shape constraints, normalization/transformers, vectorial GP, time-series support, and interval-arithmetic evaluation.

Non-goals for the scalar Stages 1-4 redesign:

- no HeuristicLab architecture/source copy
- no full data-layer redesign before Stage 1
- no first-slice implementation of templates, shape constraints, vectorial GP, time-series support, or interval-arithmetic evaluation
- no long-term compatibility promise for the old mutable tree API

## Design Holes

Resolve these before or during Stage 0:

- **API specs:** add executable usage specs before hardening public APIs.
- **Legacy migration:** keep still-required mutable components in their domain
  folders, use `.Legacy` namespaces only for actual name collisions, and remove
  components only after consumer and test parity plus explicit approval.
- **Interpreter binding:** define the Stage 1 name-first authoring contract: `ExpressionDraft.Variable(name)` interns names into the compiled expression variable table, variable instructions store payload indexes into that table, and the interpreter uses those names to fetch dataset series.
- **Reference behavior scope:** maintain a matrix for each legacy symbol/behavior: new target, reference level, test status, and intentional difference.
- **Instruction validity:** define runtime validation for non-empty code, RPN stack balance, arity, `SubtreeLength`, payload indexes, root position, max length/depth, and invalid opcodes.
- **Formatting/serialization:** decide the Stage 1 minimum for equality/hash, debug/infix formatting, optional variable names, and whether binary/JSON serialization is included or deferred.
- **Evaluator contract:** evaluators measure and never transform. They return objective vectors positionally paired with their input candidates, mirroring `IProblem.Evaluate`. Refinement and repair belong to the `Refiner` operator role.
- **Operator validity:** decide bounded retry versus repair behavior for creation, mutation, crossover, and repair failure.
- **Numeric literal metadata:** settle fixed versus optimizable literal representation and authoring names before Stage 1 hardens the genotype.
- **Symbol definition versus instance:** split search-space admissibility from concrete genotype occurrences, including fixed constants and ephemeral random constants. See [symbol-definition-instance-plan.md](symbol-definition-instance-plan.md).
- **Genotype representation:** use a persistent hierarchical genotype and retain compact opcode RPN as a derived execution representation. This supersedes the provisional flat-genotype direction; see [symbolic-regression-benchmark-implications.md](symbolic-regression-benchmark-implications.md).
- **Buffer/cache boundary:** scratch buffers and any column caches are interpreter internals, scoped to an evaluation call or execution instance. If repeated symbolic-regression evaluation makes manual buffer handling noisy, consider a small reusable interpretation context that owns per-data scratch buffers and optional variable-column lookup caches, but do not add it before the concrete evaluation path shows that need.
- **Thread safety:** no shared mutable interpreter memory; shared state must be immutable.
- **Extension migration:** track which examples, Python interop scripts, sliding-window regression, and scenarios migrate in Stage 5 and which stay on legacy during the scalar stages.

## Core Shape

| Concept                                     | Responsibility                                                                                                                                                                                                                   |
| ------------------------------------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `ExpressionTree`                        | Immutable persistent hierarchical genotype with symbol-owned node payload semantics, cached subtree metadata, and structurally shared edits.                                                                                   |
| `CompiledExpression`                    | Public immutable derived execution representation: compact postorder/RPN instructions plus payload side tables, available for explicit precompilation, optimization, and repeated execution.                                  |
| `Instruction`                     | Opcode, arity, subtree length, and optional payload index.                                                                                                                                                                       |
| `OpCode`                  | Stable `ushort` enum for built-in expression symbols with explicit integer values.                                                                                                                                               |
| `OpCodes`                 | Central fast metadata companion for built-in opcodes: support checks, arity, payload kind, terminal checks, and predefined opcode groups.                                                                                          |
| `ExpressionDraft`                           | Human-friendly construction-only authoring layer and static construction vocabulary; not a genotype.                                                                                                                             |
| `ExpressionNode`                        | Immutable recursive node containing its symbol, local payload, children, and cached subtree metadata. A node also represents the subtree rooted at that node.                                                                |
| `ExpressionPoint`                       | Tree-bound occurrence path used when an operator must identify and replace one specific occurrence, including when structurally shared nodes appear more than once.                                                          |
| `ExpressionTreeSearchSpace`             | First scalar expression search space: length, depth, and allowed symbols; all scalar subtrees are composition-compatible.                                                                                                        |
| `GrammarSymbolicExpressionSearchSpace`      | Grammar-constrained scalar validity policy with typed operation signatures and grammar-preserving operators.                                                                                                                     |
| `ExpressionInterpreter`             | Executes opcodes over series/batch buffers and maps variable indexes to dataset columns from the supplied dataset/input-variable order.                                                                                          |
| `SymbolicRegressionProblem`                 | Composition root for data, search space, interpreter, objectives, bounds policy, and problem context needed by typed operators.                                                                                                  |
| `SymbolicExpressionEvaluator`               | Problem-specific evaluator surface for symbolic-expression regression, including optional numeric-parameter optimization before objective calculation.                                                                           |
| `Evaluator`                                 | Operator that receives candidates and returns their objective vectors. It measures and never replaces a candidate; it is the composable hook where counting, limiting, caching, and observation attach. |
| `Refiner`                                   | Operator that receives a candidate and returns a candidate. Refinement, repair, simplification, and numeric constant optimization live here. A refiner may consult a configured evaluator when it needs objective information. |

Boundary rules:

- The genotype owns structure only.
- Genotype storage follows the semantic tree; compiled storage follows the execution engine. Do not expose opcode/RPN layout through the user-facing tree API or force either representation to serve both responsibilities.
- Ordinary interpretation accepts an `ExpressionTree` without requiring users to manage compilation. Advanced users may explicitly compile and optimize a tree, retain the resulting public `CompiledExpression`, and execute it repeatedly without recompilation.
- The current `ExpressionTree` system is the closed, built-in-opcode implementation optimized for speed. Built-in symbols are deliberately fixed in HeuristicLib code so catalog lookup, interpretation, validation, and operator logic can use compile-time-known opcodes and fast switches in hot paths.
- A more flexible custom-symbol system may be added later if real use cases require it. That system should be designed as a separate layer or sibling implementation and must not slow down the built-in fast path.
- The interpreter owns translation from compiled variable-reference names to
  `DataFrame` series; it does not add a separate public data-view or
  translation type.
- Search spaces and their matching operators generate, mutate, cross, repair, and validate candidates; they do not evaluate them.
- A symbolic regression problem instance has exactly one expression search-space instance. Unrestricted versus grammar-constrained behavior is selected when the problem is constructed, not switched dynamically during a run.
- The unrestricted scalar search space is not represented as a `SimpleGrammar`. Fast unrestricted operators must not call grammar predicates or enumerate grammar-derived cut points.
- Prediction bounds, penalties, and objectives stay outside expression interpretation.
- Evaluation never mutates candidates in place and never replaces them. Numeric constant optimization returns a replacement `ExpressionTree` from a refiner, which the algorithm then evaluates.
- `Problem.Evaluate(...)` is the batch native problem contract. `SingleSolutionProblem.Evaluate(...)` stays scalar and pure for scalar authoring, while its batch override currently adapts scalar evaluation through `BatchExecution`.
- Interceptors run after an algorithm step and are too late for candidate changes that affect offspring fitness or replacement.
- Determinism is required: the same candidate, problem data, evaluator configuration, and explicit random source must produce the same returned solution.
- Metrics compare target and prediction series positionally, like sklearn-style vectorized evaluation.
- Regression data is the first concrete data-analysis shape. Keep the current implementation regression-focused, but leave room to generalize the same `DataFrame`/named-target split into supervised-learning data shapes for classification and other target types later.

## Stage 0: Design Closure

Add API usage specs in `test/HeuristicLib.Tests.ApiUsageSpecs` for:

- building `x0 + 2 * x1` with `ExpressionDraft`
- evaluating a compiled expression against regression data
- constructing a symbolic regression problem with either the default MSE or an
  explicit regression metric
- running GA with the new creator, crossover, and mutator
- enabling numeric-parameter optimization through the evaluator without in-place mutation
- configuring GA with an evaluator that can return refined symbolic-expression solutions

Stage 0 specs may include commented or otherwise non-compiled "wish API" sketches when the target API depends on later stages. These sketches are allowed as design probes, but the repository must keep compiling. As implementation reaches a sketched API, comment the code back in so the usage spec compiles and fails normally if the API shape drifts. Once behavior-level invariants matter more than authoring shape, promote the relevant sketch into regular unit tests in the owning test project.

Also create the reference behavior matrix and seed it with variable, number, add, subtract, multiply, divide, log, sqrt, and linear scaling.

Stage 0 is done when the intended public flow is executable and every design hole above has a recorded outcome or owner.

## Stage 1: Immutable Scalar Core

Implement:

- `ExpressionTree` rooted in an immutable `ExpressionNode` hierarchy with specialized payloadless, variable, numeric, unary, binary, and n-ary nodes, structurally shared path-copy edits, and cached length/depth/hash.
- Regular construction uses `Symbol.CreateNode(...)` or the public concrete node constructors. Unary and binary nodes store direct references; n-ary nodes materialize a private child array, with ownership transfer restricted to internal construction and edit paths.
- Runtime validation for the instruction invariants listed above.
- `ExpressionDraft.Compile()`, `ExpressionSlice`, and formatting from compiled variable names.
- Series/batch interpretation against a supplied `DataFrame`.
- Constant side-table entries are plain `double` values. The `Constant` opcode references the table by `PayloadIndex`; fixed versus evolvable status remains genotype-side symbol information and is irrelevant to compiled evaluation.
- Draft authoring APIs expose fixed and optimizable literal authoring through `FixedConstant(value)` and `Constant(value)`.
- Revisit the draft API after the first operators clarify authoring pressure. Consider additional fluent expression composition and static-import helpers so common expressions can be authored without a static factory style.
- Move old mutable symbolic-expression-tree APIs under a `HEAL.HeuristicLib.Legacy...` namespace. Legacy types and methods get `[Obsolete]` markers. If a legacy member name would clash with new API names, add a `Legacy` prefix or suffix to the legacy member.

Stage 1 opcodes:

```csharp
public enum OpCode : ushort
{
    Invalid = 0,
    Variable = 1,
    Constant = 2,
    Add = 10,
    Subtract = 11,
    Multiply = 12,
    Divide = 13,
    Log = 20,
    Sqrt = 21
}
```

Rules:

- `Invalid = 0` is rejected.
- Instructions are postorder/RPN; root is `Instructions[^1]`.
- `SubtreeLength` includes the instruction itself.
- `PayloadIndex == -1` means no side-table payload.
- Arithmetic factories produce binary operations; n-ary arithmetic behavior is represented with deterministic left-associative binary expression fixtures when old behavior is used as a reference.
- Expression drafts author variables by name, for example `ExpressionDraft.Variable("x0")`.
- Compiled variable references are interned into a variable side table. Variable instructions store a `PayloadIndex` into that table, not the raw name.
- Missing dataset variables and non-double variables in scalar regression fail during interpreter setup.
- The interpreter resolves each used variable reference name to a dataset series once per evaluation setup. Operators consume one or more input buffers and produce a result buffer for the batch.
- The interpreter reads compiled constant values only. Fixed versus evolvable identity remains on the expression-tree node symbol and participates in tree equality and hashing.
- Numeric invalid results produce `NaN` only for now. Clamping, penalties, infinity handling, and objective handling remain outside the interpreter.
- Variable coefficients lower to `Multiply(coef, variable)`.
- Linear scaling is an optional fitness-time prediction transformation. It fits
  slope and intercept against the training target without changing or lowering
  additional nodes into the expression genotype.
- Expression complexity defaults to instruction count.
- Stage 1 supports debug/infix formatting only; fully fledged JSON/binary serialization is deferred.

Stage 1 tests: immutability, validation failures, sub-expression navigation, draft compilation, column/batch interpreter behavior, reference behavior fixtures, and formatting.

## Stage 2: Problem Composition And Evaluation Contract

`SymbolicRegressionProblem` composes one `RegressionData` training set, one
`IRegressionMetric`, and one `ExpressionTreeSearchSpace`. It evaluates only its
bound training data and exposes no arbitrary-data prediction API.

`SymbolicRegressor` is a fitted `IRegressor`. It owns an `ExpressionTree`, its
name-based compiled representation, and a prediction name. `BoundedRegressor`
decorates any `IRegressor` with numeric output bounds. Prediction on validation
or test data is performed through these predictors rather than through the
problem. `ExpressionTree.ToRegressor(...)` is the concise conversion path and
performs the one-time compilation owned by the returned regressor.
`IRegressor.ToBounded(...)` adds output bounds through normal predictor
composition.

Linear scaling is opt-in problem evaluation behavior. The problem evaluates an
expression once, fits least-squares slope and intercept against its training
target, applies the scaled predictions to all prediction metrics, and leaves
the genotype unchanged. `LinearScaling` exposes the reusable span-based
calculation, while `LinearlyScaledRegressor` retains fitted coefficients for
validation, test, and production prediction. The future symbolic-regression
estimator must fit this wrapper once for the selected final expression when
linear scaling was enabled during search.

The maintained data-analysis foundation and its migration sequence are
specified in
[data-analysis-modernization-plan.md](data-analysis-modernization-plan.md).

Defaults:

- the simple `SymbolicRegressionProblem(data, searchSpace)` constructor uses
  `Metrics.MSE`
- the explicit single-metric constructor remains the concise path for another
  prediction metric
- advanced construction takes separate ordered prediction-metric and
  expression-metric collections
- prediction metrics are evaluated from one shared prediction vector and
  appear before expression metrics in the objective vector
- either metric collection may be empty, but both cannot be empty
- multiple objectives use lexicographic total order by default; their
  individual directions continue to govern dominance
- common metric implementations are exposed through `Metrics` and
  `ExpressionMetrics` shortcuts

Evaluation contract:

- evaluators return objective vectors paired positionally with their input candidates; they never return a replacement candidate
- evaluators must not mutate input candidates in place or update hidden genotype state through caches
- concrete evaluators can be problem-specific and extract required context from the typed problem instance
- the general evaluator call shape includes the candidate, explicit random source when needed, search space, and problem, matching other operators; it does not accept symbolic-regression-specific arguments such as input variables or target variable directly
- numeric-parameter optimization is a refiner, not evaluator configuration. The algorithm applies it after variation and before evaluation; see [symbolic-regression-constant-optimization-plan.md](symbolic-regression-constant-optimization-plan.md)
- evaluation that must be visible to budgets, termination, analysis, or instrumentation goes through an evaluator operator; an operator calling `problem.Evaluate` directly is legal but invisible and therefore discouraged

Stage 2 tests: problem API specs, evaluation composition, metric fixtures, no in-place evaluation mutation, evaluator contract shape, and fixed-constant behavior.

## Stage 3: Unrestricted Search Space And Operators

Add the fast default scalar search space and operator family:

- `ExpressionTreeSearchSpace` with size, depth, and allowed symbols. It is the first unrestricted search-space shape without an unrestricted subtype or factory.
- creator, mutator, crossover, and optional repair for unrestricted scalar `ExpressionTree` candidates.
- static operator methods that mirror instance entry points, following `docs/design-goals.md`.
- direct core overloads that take primitive limits and opcode/variable sets when the search space is only a container for those values.
- Hierarchy-aware helpers for subtree metadata, occurrence selection, structurally shared replacement, and length/depth checks.
- symbol-owned initialization and local-perturbation policies, plus search-space selection weights.

Operator implementation order:

- First add low-level genotype operations on `ExpressionTree`, `ExpressionNode`, and tree-bound `ExpressionPoint` occurrences. These operations are not search-space-aware and provide efficient immutable editing primitives for later operators.
- Then add fast unrestricted operators as the main HLib symbolic-regression GP path. These operators preserve unrestricted search-space validity by construction where practical.
- Later add restricted operators as a sibling operator family, starting with grammar-preserving operators. Restricted and unrestricted operators share low-level genotype operations but are not implicitly interchangeable.

Low-level genotype operations:

- support same-arity node-symbol edits when payload rules stay compatible
- support constant payload edits
- support variable-reference payload edits
- support sub-expression replacement and splicing
- return new validated `ExpressionTree` instances and never mutate existing candidates
- use ancestor path copying, direct unary/binary child reuse, and single-clone n-ary child arrays for edits and subtree replacement

Rules:

- All scalar-producing subtrees are mutually composable in this search space.
- Unrestricted containment uses aggregate coverage across configured symbols rather than requiring a structurally equal origin symbol. This keeps different partitions of the same unrestricted variable or constant domain containment-compatible; see [Containment Matching](symbol-definition-instance-plan.md#containment-matching).
- Operators must not require or call a grammar.
- Operators must not require or call a generic restriction provider.
- Operators never mutate parents.
- Operators preserve search-space validity or fail with documented bounded retry behavior.
- Evolvable-constant generation belongs to the symbol's initialization distribution and uses the general random-distribution abstractions.
- Operators have a simple default constructor path; symbol-owned defaults provide common initialization and local perturbation behavior.
- Shared genotype operations may be reused by grammar-aware operators, but unrestricted operators remain a separate fast path.
- Restricted operators are not implicitly reused in unrestricted contexts. A grammar-preserving operator may produce candidates that are also valid in an unrestricted search space, but it still requires grammar context and therefore belongs to the grammar operator family.
- Operator-family presets may be added later so switching from unrestricted to grammar search spaces can replace creator, mutator, crossover, and repair families together without manual one-by-one rewiring.

Stage 3 tests: genotype editing immutability and side-table reuse behavior, unrestricted search-space containment, creator validity, mutation validity, crossover validity, parent immutability, bounded failure behavior, and one GA usage spec using unrestricted operators.

Current executable vertical slice:

- `SymbolicRegressionProblem` implements the standard single-solution problem contract and owns one explicit `ExpressionTreeSearchSpace`.
- `GrowTreeCreator` samples from all symbols that are structurally viable under the remaining length and depth budget. Its optional maximum depth defaults to the search space maximum. Its bounded subtree-generation operation is shared with subtree mutation.
- `FullTreeCreator` keeps every leaf at one selected depth. Its optional exact depth defaults to the deepest depth feasible under both the search space's length and depth limits. Operation arities are constrained during creation so full trees do not require rejection retries.
- `RampedHalfAndHalfTreeCreator` initializes populations with paired full and grow trees across a configurable inclusive depth range. The minimum defaults to two and the nullable maximum defaults to the deepest feasible search-space depth. Larger populations repeat that deterministic depth/method ramp while each tree retains independent randomness.
- `SubtreeCrossover` selects one destination and one valid donor without retry loops. Its nullable internal-node probability defaults to uniform selection across all nodes; setting it to `0.9` enables the conventional Koza-style function-versus-terminal bias.
- `NodeReplacementMutator`, `SubtreeMutator`, `ShrinkSubtreeMutator`, and `LocalPerturbationMutator` provide complementary same-arity, structural replacement, strict shrinking, and symbol-local mutation paths. `ShrinkSubtreeMutator` replaces a selected operation occurrence with a terminal sampled from the search space and therefore reduces tree length without retries. The mutators compose through the general `ChooseOneMutator` with explicit weights.
- `SubtreeMutator` selects one occurrence, derives the exact remaining global length and depth budgets at that point, creates one viable subtree, and replaces the occurrence without rejection retries.
- Nullable creator depth settings inherit the search-space limits. Explicit depth settings must remain feasible within those limits; users who need a larger candidate domain construct a correspondingly larger search space.
- The ordinary problem evaluator compiles and interprets an expression at most
  once per evaluation, then scores the shared predictions with every configured
  prediction metric and evaluates configured expression metrics directly on
  the genotype. Expression-only objectives do not compile the expression.
- The API usage specs run this path through the generic genetic algorithm for multiple generations with all three mutation paths and verify that the final population remains inside the search space.
- A deterministic synthetic-regression scenario exercises creation, crossover, all mutation forms, evaluation, best-solution selection, explicit optimized compilation, and repeated interpretation of the retained compiled best expression. It asserts a real improvement over a constant-mean baseline.
- Population-integrated numeric parameter optimization, a named symbolic-regression operator preset, protected-operation policy, convenience problem factories, and grammar-constrained operators remain follow-up work. Direct numeric optimization is available independently of evaluator composition.

## Stage 3.1: Constant Optimization During Evaluation

The detailed design and incremental implementation sequence now live in
[symbolic-regression-constant-optimization-plan.md](symbolic-regression-constant-optimization-plan.md).

The replacement proceeds through independently usable layers:

1. organize the old and new symbolic-regression systems;
2. provide general automatic differentiation;
3. provide first-class numerical optimization algorithms;
4. provide generic local-improvement and memetic composition;
5. adapt immutable symbolic expressions to those capabilities;
6. integrate constant optimization into symbolic-regression evaluation.

The immutable-candidate invariant remains settled: any accepted constant optimization returns a replacement `ExpressionTree`.

Refinement integration is settled as well:

- A `Refiner` operator role with the contract `Candidate → Candidate` is the
  single refinement mechanism. Algorithms place it explicitly after creation and
  after final variation, immediately before evaluation.
- Evaluators measure and never transform. `IEvaluatorInstance.Evaluate` returns
  objective vectors, mirroring `IProblem.Evaluate`. There is no
  candidate-transforming evaluator.
- Objective-aware retention is `ImprovementCheckingRefiner`, a wrapping refiner
  that takes a nullable evaluator and a nullable comparer and returns the better
  of the original and refined candidate. Because it is an ordinary refiner, it
  composes with every other refiner topology.
- Evaluation visibility, not capability, is the invariant: any operator can
  reach `problem.Evaluate`, but evaluation that should count towards budgets,
  termination, analysis, or instrumentation must go through an evaluator
  operator.
- Sharing one evaluator configuration instance between the algorithm and a
  refiner makes their evaluations share one execution instance, and therefore
  one counter and one cache. This is how refinement cost is included in or
  excluded from an evaluation budget.
- Repeated refinement is expressed through iterated or pipeline refiner
  topologies, not by placing the same refiner at two lifecycle points.

## Stage 4: Grammar Search Space And Operators

Add the smallest typed grammar model needed for valid scalar symbolic regression:

- operation signatures and scalar value type first
- fluent grammar API first; EBNF authoring later as syntax over the same model
- grammar-preserving creation, mutation, crossover, and repair
- scalar final-output enforcement for standard regression
- `GrammarSymbolicExpressionSearchSpace` as a distinct search-space type, not a flag on the unrestricted search space.
- grammar-specific creator, mutator, crossover, and repair operators.

Rules:

- Grammar-constrained operators should ask for viable options at a site instead of blindly sampling edit points and relying on rejection.
- Grammar containment will probably use structural origin-symbol matching, or a grammar-specific refinement, because production and nonterminal identity can be semantically significant. This remains provisional until the grammar model is designed; it must not inherit unrestricted aggregate coverage by default.
- The first grammar implementation supports scalar expressions, but scalar must be an explicit grammar value category rather than the absence of type information. Operation productions have explicit input and output signatures so later vector/tensor value categories can extend the model without replacing the grammar representation.
- Grammar logic drives option selection in the first restricted implementation. Do not add a public generic restriction-provider abstraction before the grammar implementation proves the shape.
- `Contains` is a final validation check for grammar operators, not the primary construction strategy.
- Grammar-constrained operators may enumerate viable sites, cache grammar-derived metadata, use repair, or use bounded retry.
- The grammar-aware path may pay grammar costs; the unrestricted path must not.
- Grammar operators should reuse low-level immutable genotype operations where practical, but not share the unrestricted selection logic when grammar validity changes the set of legal edits.

Stage 4 tests: typed grammar validity, grammar operator option selection, grammar-preserving operators, scalar output enforcement, grammar-specific bounded failure behavior, and one usage spec showing a restricted grammar such as `log(variable)` only.

## Stage 5: Extensions And Composability

Add later symbolic-regression features without multiplying search-space subclasses by every combination of flavor:

### Stage 5.1: Structure Templates

- Structure templates use a different composite genotype shape: one immutable fixed template plus one evolvable `ExpressionTree` component per wildcard slot.
- A wildcard owns or references an expression search space, not a nested symbolic-regression problem. The outer problem owns data, compilation, evaluation, objectives, and fitness because only the fully instantiated template is evaluable.
- The composite search space validates the fixed template, wildcard count, wildcard-to-component association, each component against its own unrestricted or grammar-constrained search space, and the component output type against the wildcard's expected type.
- Template structure is not exposed to ordinary expression mutation or crossover. Operators first select a wildcard component and then delegate creation, mutation, or crossover to the operator family compatible with that component's search space.
- Crossover requires compatible templates and wildcard slots. Whether repeated appearances of one named wildcard share one component or represent independent slots must be explicit in the template model.
- Compilation substitutes component roots while traversing the fixed template and produces the ordinary compact `CompiledExpression`. A simple first implementation may materialize the combined immutable `ExpressionTree`; a later direct compiler may avoid that temporary allocation without changing genotype semantics.
- Components retain normal immutable structural sharing. Composite equality and hashing include the template and the ordered wildcard components.
- Manually replacing placeholder nodes in an ordinary `ExpressionTree` can emulate one template instance today, but it does not protect the fixed structure or provide component-specific containment and operators. It is not the first-class template design.

Stage 5.1 tests: template and wildcard invariants, wildcard-specific search-space containment, component output compatibility, immutable component replacement, fixed-template preservation under mutation and crossover, shared versus independent repeated wildcards, template instantiation/compilation, full-expression evaluation, and one usage spec showing wildcard components with unrestricted and grammar-constrained expression search spaces.

### Stage 5.2: Shape Constraints

- Shape constraints are evaluation components over predictions, derivatives, sampled expression behavior, or other model observations. They should not create `ShapeConstrainedUnrestricted...` and `ShapeConstrainedGrammar...` search-space pairs unless the constraint truly changes local subtree admissibility.
- Do not add a `ShapeConstrainedSymbolicRegressionProblem` or shape-constraint list on the base problem. Shape-constrained use supplies a metric/constraint evaluator that owns its constraint configuration and consumes shared evaluation context from the problem.
- Single-objective and multi-objective shape-constrained regression are evaluator configurations: aggregate violations for single-objective use or expose separate violation dimensions for multi-objective use.

Stage 5.2 tests: shape-constraint evaluator ownership, empty-normal-problem API shape, single-objective penalty aggregation, multi-objective violation dimensions, and derivative/sampling fixtures.

### Stage 5.3: Algebraic Normalization And Candidate Transformers

- Algebraic normalization, simplification, constant folding, equivalent-form comparison, and numeric refinement are candidate transformers, comparers, or evaluators, not new expression search-space flavors.

Stage 5.3 tests: constant folding, candidate immutability, equivalent-form comparison fixtures, and transformer/evaluator composition.

### Stage 5.4: Vectorial GP Value-System Support

- Vectorial GP means that one expression value for one observation can be a vector or tensor. This value dimension is distinct from the current interpreter's batched `double` buffers, which contain one scalar value for each observation.
- The current scalar genotype and interpreter cannot directly represent vector-valued variables. The extension requires explicit expression value metadata, typed variable schema, typed grammar productions, resolved operation signatures, shape-aware compiled instructions, and shape-aware evaluation buffers.
- Introduce an explicit value category/shape model with at least scalar and vector categories and a path to fixed-rank tensors. Do not encode scalar as `null`, rank zero by convention alone, or an untyped default.
- Variable symbols bind to schema entries carrying value category and shape. Numeric constants initially remain scalar and participate in broadcasting.
- Grammar productions define input and output signatures such as `Vector + Vector -> Vector`, `Vector + Scalar -> Vector`, and `Mean(Vector) -> Scalar`. The grammar enforces that the symbolic-regression root returns a scalar.
- Broadcasting is part of resolved operation semantics, not an implicit interpreter guess. Initial support should prefer fixed per-observation shapes; general NumPy-style rank and dimension broadcasting requires explicit compatibility rules and runtime validation when dimensions are not statically known.
- Compilation retains compact postorder/RPN storage but records the resolved operation overload or equivalent value-shape metadata needed by execution. It must not rediscover grammar overloads in the interpreter hot loop.
- Vector/tensor interpreter buffers represent `batch size x elements per observation`; aggregation operations reduce only the per-observation value dimensions and never accidentally reduce the observation batch dimension.
- Keep the current scalar `double` interpreter as a specialized fast path. Do not force scalar execution through boxed values, virtual per-instruction dispatch, or a general tensor union solely to support vectorial GP.
- Add a value-system-aware search-space and operator family only where typed local admissibility differs. Avoid subclasses for every cross-product of vectorial GP with templates, shape constraints, or other evaluator-only extensions.

Stage 5.4 tests: explicit scalar/vector/tensor metadata, typed variable binding, scalar/vector operation signatures, broadcasting compatibility and rejection, scalar aggregation roots, grammar-preserving creation/mutation/crossover, compiled shape metadata, interpreter buffer layout, aggregation over the correct dimensions, result typing, and search-space containment for value-compatible expressions.

### Stage 5.5: Time-Series Expression Support

- Time-series support follows the same direction as vectorial GP when it changes value metadata or available opcodes. Lag/window semantics belong in operation signatures, interpreter binding, and data-view context before they become separate problem types.

Stage 5.5 tests: lag/window binding, time-aware operation signatures, row-window validity, interpreter behavior fixtures, and regression usage specs over time-series data.

### Stage 5.6: Interval-Arithmetic Evaluation

- Interval arithmetic is initially an alternative evaluation domain for an otherwise ordinary scalar expression, not a new genotype. A numeric constant `c` becomes the degenerate interval `[c, c]`, variable bindings provide intervals, and built-in operations apply their interval-arithmetic definitions.
- The existing `ExpressionTree` and compact `CompiledExpression` can be reused because their structure, opcodes, numeric constants, and variable references are sufficient for scalar interval evaluation. What is missing is an interval value type, interval input binding, operation semantics, and a dedicated interpreter/evaluation backend.
- Keep `ExpressionInterpreter` specialized for `double`. Add a separate interval interpreter rather than genericizing the hot scalar interpreter through interface dispatch or boxed numeric values.
- Define interval behavior explicitly for division across zero, logarithm and square root outside their domains, infinities, empty/invalid intervals, and outward rounding. These rules are part of interval evaluation semantics and must not inherit the ordinary `double` invalid-result policy accidentally.
- Validate compiler optimizations against interval semantics. Unoptimized compilation is the reference path; constant folding and identity elimination may be enabled only when they preserve the selected interval semantics.
- Interval evaluation can support range analysis, safety checks, and shape constraints without changing GP containment. If intervals later become first-class expression values that mix with scalars or vectors, they join the typed value-system and grammar work from Stage 5.4 instead of being handled by the scalar interval backend.
- Keep interval results separate from ordinary prediction series. The regression objective still consumes scalar predictions unless an evaluator deliberately converts interval results into penalties, bounds, or another objective representation.

Stage 5.6 tests: interval construction and invariants, degenerate constants, variable binding, arithmetic enclosure, division across zero, domain-invalid unary operations, infinities and empty intervals, outward-rounding fixtures, optimized-versus-unoptimized semantic equivalence where optimization is enabled, range-analysis use, and no regression in the specialized `double` interpreter.

### Stage 5 Shared Rules

- Extensions compose around the expression component. A new search-space type is justified only when the feature changes the set of structurally valid candidates or the local closure rules needed by creation, crossover, mutation, or repair.
- Evaluation domains that do not change expression admissibility, such as scalar interval analysis, reuse the genotype and compiled representation through a separate evaluator/interpreter. Value systems that change valid operation signatures, such as vector/tensor values, require typed grammar and search-space support.

Follow-up details stay outside this plan unless separately scheduled:

- schema-based variable matching beyond Stage 1 name binding
- revised invalid-value policy if `NaN` is not sufficient for later objectives or operators
- n-ary symbolic operators instead of binary-only operator arity
- immutable data snapshots, builders, versioning, and cache invalidation

## Compatibility

- Use HeuristicLab as behavioral reference only.
- Prefer reimplemented HeuristicLib fixtures over copied source.
- Any direct source-code reuse requires an explicit licensing decision.
- Maintain a matrix with: HeuristicLab source, current legacy type, new target, reference level, porting status, and intentional differences. Do not add a legacy-to-new converter; reference behavior is checked through independently authored fixtures for the new implementation.
- Put fast invariants in `test/HeuristicLib.Tests`, workflows in `test/HeuristicLib.Tests.Scenarios` and public API shape in `test/HeuristicLib.Tests.ApiUsageSpecs`.

## Validation

```powershell
dotnet restore
dotnet build --configuration Release --no-restore
dotnet test --configuration Release --no-restore
dotnet format ./HEAL.HeuristicLib.sln --verify-no-changes --no-restore --severity error
```

## Settled Decisions

- Genotype: immutable `ExpressionTree`, explicit `OpCode : ushort`, binary arity in Stage 1, identical-instruction equality, and `NaN` for invalid numeric results.
- Binding and execution: draft authoring is name-first, compiled variable instructions use payload indexes into an expression variable table, interpreter memory is not shared mutably, and Operon remains the postfix/contiguous-encoding reference.
- Legacy migration: still-required mutable symbolic-expression APIs remain in
  their established domain folders until complete replacements exist. Use a
  `.Legacy` namespace, or a `Legacy` filename qualifier, only where old and new
  names would otherwise collide.
- Formatting/serialization: Stage 1 includes debug/infix formatting; full serialization is deferred.
- Problem and search spaces: one symbolic-regression problem concept, with unrestricted and grammar-constrained scalar search spaces as distinct operator families.
- Evaluation/refinement: evaluators return objective vectors and never replace candidates. The `Refiner` operator role, `Candidate → Candidate`, owns refinement, repair, simplification, and constant optimization, and `ImprovementCheckingRefiner` adds objective-aware retention as an ordinary composable refiner.
- Problem evaluation: `IProblem` and `Problem` are batch native. `SingleSolutionProblem` is the scalar authoring base and currently owns the scalar to batch adapter.
- Extensions: compose around expression components; add search-space types only when local structural admissibility changes. Shape constraints live in evaluator/evaluation-strategy objects, not the base problem.
