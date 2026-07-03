# Symbolic Regression Redesign Plan

## Goal

Replace symbolic regression's mutable tree candidate with an immutable, compact, postorder/RPN `SymbolicExpression` genotype. Move the current mutable `SymbolicExpressionTree` to a `Legacy` namespace with `[Obsolete]` markers and keep it only for temporary migration and golden reference behavior tests until the new system fully replaces it.

HeuristicLab is the behavioral reference, not the target architecture. The first reference package is [HeuristicLab.Problems.DataAnalysis.Symbolic.Regression/3.4](https://github.com/heal-research/HeuristicLab/tree/main/HeuristicLab.Problems.DataAnalysis.Symbolic.Regression/3.4).

Implementation order:

- Stage 0: close design holes and add API usage specs.
- Stage 1: build the immutable scalar expression core and reference behavior harness.
- Stage 2: rebuild problem composition and the evaluator contract around the new genotype.
- Stage 3: add the unrestricted scalar search space and fast unrestricted search operators.
- Stage 3.1: add constant optimization with Levenberg-Marquardt and automatic differentiation after the unrestricted operators are stable.
- Stage 4: add the grammar-constrained scalar search space and grammar-preserving search operators.
- Stage 5: add extensions in sub-stages: templates, shape constraints, normalization/transformers, vectorial GP, and time-series support.

Non-goals for the scalar Stages 1-4 redesign:

- no HeuristicLab architecture/source copy
- no full data-layer redesign before Stage 1
- no first-slice implementation of templates, shape constraints, vectorial GP, or time-series support
- no long-term compatibility promise for the old mutable tree API

## Design Holes

Resolve these before or during Stage 0:

- **API specs:** add executable usage specs before hardening public APIs.
- **Legacy boundary:** decide namespace, obsolete message, in-repo migration order, and whether a temporary forwarding shim is allowed.
- **Interpreter binding:** define the Stage 1 name-first authoring contract: `ExpressionDraft.Variable(name)` interns names into the compiled expression variable table, variable instructions store payload indexes into that table, and the interpreter uses those names to fetch dataset series.
- **Reference behavior scope:** maintain a matrix for each legacy symbol/behavior: new target, reference level, test status, and intentional difference.
- **Instruction validity:** define runtime validation for non-empty code, RPN stack balance, arity, `SubtreeLength`, payload indexes, root position, max length/depth, and invalid opcodes.
- **Formatting/serialization:** decide the Stage 1 minimum for equality/hash, debug/infix formatting, optional variable names, and whether binary/JSON serialization is included or deferred.
- **Evaluator contract:** allow evaluators to return the authoritative `Solution<TGenotype>`, not only objective values, so evaluation can explicitly return a refined or repaired candidate together with its objectives.
- **Operator validity:** decide bounded retry versus repair behavior for creation, mutation, crossover, and repair failure.
- **Numeric literal metadata:** settle fixed versus optimizable literal representation and authoring names before Stage 1 hardens the genotype.
- **Buffer/cache boundary:** scratch buffers and any column caches are interpreter internals, scoped to an evaluation call or execution instance. If repeated symbolic-regression evaluation makes manual buffer handling noisy, consider a small reusable interpretation context that owns per-data scratch buffers and optional variable-column lookup caches, but do not add it before the concrete evaluation path shows that need.
- **Thread safety:** no shared mutable interpreter memory; shared state must be immutable.
- **Extension migration:** track which examples, Python interop scripts, sliding-window regression, and scenarios migrate in Stage 5 and which stay on legacy during the scalar stages.

## Core Shape

| Concept                                     | Responsibility                                                                                                                                                                                                                   |
| ------------------------------------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `SymbolicExpression`                        | Immutable genotype: RPN instructions plus side tables for numeric literals and variable references.                                                                                                                              |
| `ExpressionInstruction`                     | Opcode, arity, subtree length, and optional payload index.                                                                                                                                                                       |
| `SymbolicExpressionOpCode`                  | Stable `ushort` enum for built-in expression symbols with explicit integer values.                                                                                                                                               |
| `SymbolicExpressionOpCodes`                 | Central fast metadata companion for built-in opcodes: support checks, arity, payload kind, terminal checks, and predefined opcode groups.                                                                                          |
| `ExpressionDraft`                           | Human-friendly authoring layer; not a genotype.                                                                                                                                                                                  |
| `SymbolicSubExpression`                     | Allocation-light subtree view over an immutable expression, exposed through tree-style navigation from `SymbolicExpression.Root`.                                                                                                |
| `SymbolicExpressionSearchSpace`             | First scalar expression search space: length, depth, allowed operations, allowed variables, and numeric literal availability; all scalar subtrees are composition-compatible.                                                   |
| `GrammarSymbolicExpressionSearchSpace`      | Grammar-constrained scalar validity policy with typed operation signatures and grammar-preserving operators.                                                                                                                     |
| `SymbolicExpressionInterpreter`             | Executes opcodes over series/batch buffers and maps variable indexes to dataset columns from the supplied dataset/input-variable order.                                                                                          |
| `SymbolicRegressionProblem`                 | Composition root for data, search space, interpreter, objectives, bounds policy, and problem context needed by typed operators.                                                                                                  |
| `SymbolicExpressionEvaluator`               | Problem-specific evaluator surface for symbolic-expression regression, including optional numeric-parameter optimization before objective calculation.                                                                           |
| `Evaluator`                                 | Operator that receives a candidate and returns the authoritative `Solution<TGenotype>` that was actually evaluated. The returned genotype may be identical to the input candidate or an immutable refined/repaired replacement. |

Boundary rules:

- The genotype owns structure only.
- The current `SymbolicExpression` system is the closed, built-in-opcode implementation optimized for speed. Built-in symbols are deliberately fixed in HeuristicLib code so catalog lookup, interpretation, validation, and operator logic can use compile-time-known opcodes and fast switches in hot paths.
- A more flexible custom-symbol system may be added later if real use cases require it. That system should be designed as a separate layer or sibling implementation and must not slow down the built-in fast path.
- The interpreter owns translation from compiled variable-reference names to dataset series; Stage 1 does not add a separate public data-view or translation type.
- Search spaces and their matching operators generate, mutate, cross, repair, and validate candidates; they do not evaluate them.
- A symbolic regression problem instance has exactly one expression search-space instance. Unrestricted versus grammar-constrained behavior is selected when the problem is constructed, not switched dynamically during a run.
- The unrestricted scalar search space is not represented as a `SimpleGrammar`. Fast unrestricted operators must not call grammar predicates or enumerate grammar-derived cut points.
- Prediction bounds, penalties, and objectives stay outside expression interpretation.
- Evaluation never mutates candidates in place. Any numeric constant optimization returns a replacement `SymbolicExpression`, and the evaluator returns a `Solution<SymbolicExpression>` containing the genotype that was actually evaluated.
- `Problem.Evaluate(...)` is the batch native problem contract. `SingleSolutionProblem.Evaluate(...)` stays scalar and pure for scalar authoring, while its batch override currently adapts scalar evaluation through `BatchExecution`.
- Interceptors run after an algorithm step and are too late for candidate changes that affect offspring fitness or replacement.
- Determinism is required: the same candidate, problem data, evaluator configuration, and explicit random source must produce the same returned solution.
- Metrics compare target and prediction series positionally, like sklearn-style vectorized evaluation.
- Regression data is the first concrete data-analysis shape. Keep the current implementation regression-focused, but leave room to generalize the same `DataFrame`/named-target split into supervised-learning data shapes for classification and other target types later.

## Stage 0: Design Closure

Add API usage specs in `test/HeuristicLib.Tests.ApiUsageSpecs` for:

- building `x0 + 2 * x1` with `ExpressionDraft`
- evaluating a compiled expression against regression data
- constructing a default symbolic regression problem with an explicit RMSE metric/loss object
- running GA with the new creator, crossover, and mutator
- enabling numeric-parameter optimization through the evaluator without in-place mutation
- configuring GA with an evaluator that can return refined symbolic-expression solutions

Stage 0 specs may include commented or otherwise non-compiled "wish API" sketches when the target API depends on later stages. These sketches are allowed as design probes, but the repository must keep compiling. As implementation reaches a sketched API, comment the code back in so the usage spec compiles and fails normally if the API shape drifts. Once behavior-level invariants matter more than authoring shape, promote the relevant sketch into regular unit tests in the owning test project.

Also create the reference behavior matrix and seed it with variable, number, add, subtract, multiply, divide, log, sqrt, and linear scaling.

Stage 0 is done when the intended public flow is executable and every design hole above has a recorded outcome or owner.

## Stage 1: Immutable Scalar Core

Implement:

- `SymbolicExpression` with private instruction, numeric-literal, and variable-reference arrays plus cached length/depth/hash.
- Copying public factories and clearly named ownership-transfer factories for builders.
- Runtime validation for the instruction invariants listed above.
- `ExpressionDraft.Compile()`, `ExpressionSlice`, and formatting from compiled variable names.
- Series/batch interpretation against a supplied `Dataset` and input-variable order.
- Numeric-literal side-table entries include value plus fixed/optimizable role. The `NumericLiteral` opcode stays singular and references side-table entries by `PayloadIndex`.
- Draft/builder APIs expose fixed and optimizable literal authoring, tentatively `Fixed(value)` and `Parameter(value)`.
- Revisit the draft API after the first operators clarify authoring pressure. Consider fluent expression composition, operator overloads and static imports so common expressions can be authored without a static factory style.
- Move old mutable symbolic-expression-tree APIs under a `HEAL.HeuristicLib.Legacy...` namespace. Legacy types and methods get `[Obsolete]` markers. If a legacy member name would clash with new API names, add a `Legacy` prefix or suffix to the legacy member.

Stage 1 opcodes:

```csharp
public enum SymbolicExpressionOpCode : ushort
{
    Invalid = 0,
    Variable = 1,
    NumericLiteral = 2,
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
- The interpreter reads numeric literal values and ignores fixed/optimizable metadata.
- Equality and hashing include numeric literal metadata so evaluator caches distinguish fixed and optimizable candidates with the same literal values.
- Numeric invalid results produce `NaN` only for now. Clamping, penalties, infinity handling, and objective handling remain outside the interpreter.
- Variable coefficients lower to `Multiply(coef, variable)`.
- Linear scaling lowers to ordinary nodes, for example `Add(Multiply(scale, model), offset)`, unless Stage 0 records a dedicated-opcode decision.
- Expression complexity defaults to instruction count.
- Stage 1 supports debug/infix formatting only; fully fledged JSON/binary serialization is deferred.

Stage 1 tests: immutability, validation failures, sub-expression navigation, draft compilation, column/batch interpreter behavior, reference behavior fixtures, and formatting.

## Stage 2: Problem Composition And Evaluation Contract

`SymbolicRegressionProblem` becomes explicit composition of:

- regression data and input-variable order
- one scalar `SymbolicExpressionSearchSpace`
- interpreter
- metric/loss/objectives
- prediction bound and invalid-value policy
- problem context needed by typed evaluators, such as data, input variables, target values, interpreter, bounds policy, and numeric-optimization options

Before Stage 2 implementation, choose the concrete problem/search-space type shape. Current preferred direction is `SymbolicRegressionProblem<TSearchSpace>` with convenience factories, but this remains a Stage 2 design decision.

Defaults:

- if a `CreateDefault` convenience factory is added, it remains metric-agnostic and accepts an explicit metric/loss object; it must not encode metric names such as RMSE into method names
- provide common regression metrics through object shortcuts such as `Metrics.RMSE`
- RMSE may be the default metric only when no metric/loss is supplied
- multi-objective behavior remains explicit

Evaluation contract:

- extend the evaluator contract so evaluation returns `Solution<TGenotype>` instead of only `ObjectiveVector`
- the returned solution is authoritative: replacement, selection, logging, and analysis consume the genotype and objective vector returned by the evaluator
- evaluators may return the original genotype unchanged or an immutable refined/repaired replacement genotype
- evaluator-driven candidate changes must be explicit in the return value; evaluators must not mutate input candidates in place or update hidden genotype state through caches
- concrete evaluators can be problem-specific and extract required context from the typed problem instance
- the general evaluator call shape includes the candidate, explicit random source when needed, search space, and problem, matching other operators; it does not accept symbolic-regression-specific arguments such as input variables or target variable directly
- numeric-parameter optimization is authored as evaluator configuration, for example `SymbolicExpressionEvaluator.OptimizeNumericParameters(...)`, even if the first implementation uses Levenberg-Marquardt internally
- global budget accounting is out of scope; evaluators that perform refinement expose local counters/status where useful

Stage 2 tests: problem API specs, evaluation composition, metric fixtures, no in-place evaluation mutation, evaluator contract shape, replacement of raw candidates by returned solutions, and fixed-constant behavior.

## Stage 3: Unrestricted Search Space And Operators

Add the fast default scalar search space and operator family:

- `SymbolicExpressionSearchSpace` with size, depth, allowed operations, allowed variables, and optional numeric literals. It is the first unrestricted search-space shape without an unrestricted subtype or factory.
- creator, mutator, crossover, and optional repair for unrestricted scalar `SymbolicExpression` candidates.
- static operator methods that mirror instance entry points, following `docs/design-goals.md`.
- direct core overloads that take primitive limits and opcode/variable sets when the search space is only a container for those values.
- RPN-aware internal helpers for subtree metadata, slice selection, splicing, length/depth checks, and parent-independent candidate construction.
- shared sampling profiles for sampling choices that do not constrain the search space, such as numeric-literal distributions and later variable or operation weights.

Operator implementation order:

- First add low-level genotype operations on `SymbolicExpression` and `SymbolicSubExpression`. These operations are not search-space-aware and provide efficient immutable editing primitives for later operators.
- Then add fast unrestricted operators as the main HLib symbolic-regression GP path. These operators preserve unrestricted search-space validity by construction where practical.
- Later add restricted operators as a sibling operator family, starting with grammar-preserving operators. Restricted and unrestricted operators share low-level genotype operations but are not implicitly interchangeable.

Low-level genotype operations:

- support instruction edits when arity and payload rules stay compatible
- support numeric-literal payload edits
- support variable-reference payload edits
- support sub-expression replacement and splicing
- return new validated `SymbolicExpression` instances and never mutate existing candidates
- use copy-on-write-style array handling: opcode-only edits copy instructions only, numeric edits copy numeric-literal data only, variable edits copy variable-reference data only, and subtree edits copy affected instruction ranges while reusing unchanged side tables where possible

Rules:

- All scalar-producing subtrees are mutually composable in this search space.
- Operators must not require or call a grammar.
- Operators must not require or call a generic restriction provider.
- Operators never mutate parents.
- Operators preserve search-space validity or fail with documented bounded retry behavior.
- Numeric-literal generation belongs to the sampling profile and uses the general random-distribution abstractions, not search-space validity rules.
- Operators have a simple default constructor path. When no symbolic-expression sampling profile is supplied, they use the shared default profile.
- Shared genotype operations may be reused by grammar-aware operators, but unrestricted operators remain a separate fast path.
- Restricted operators are not implicitly reused in unrestricted contexts. A grammar-preserving operator may produce candidates that are also valid in an unrestricted search space, but it still requires grammar context and therefore belongs to the grammar operator family.
- Operator-family presets may be added later so switching from unrestricted to grammar search spaces can replace creator, mutator, crossover, and repair families together without manual one-by-one rewiring.

Stage 3 tests: genotype editing immutability and side-table reuse behavior, unrestricted search-space containment, creator validity, mutation validity, crossover validity, parent immutability, bounded failure behavior, and one GA usage spec using unrestricted operators.

## Stage 3.1: Constant Optimization During Evaluation

Add the first concrete symbolic-regression evaluator with numeric-parameter optimization:

- implement Levenberg-Marquardt numeric refinement inside a problem-specific evaluator for `SymbolicExpression` and `SymbolicRegressionProblem`; it reads input variables, target data, interpreter, and bounds/refinement configuration from the problem/search-space context passed through the evaluator contract.
- provide `SymbolicExpressionEvaluator.OptimizeNumericParameters(...)` as the user-facing factory/configuration surface for this behavior; avoid requiring users to name the optimizer algorithm in the common authoring path.
- optimize numeric literals through Levenberg-Marquardt using automatic differentiation over the immutable `SymbolicExpression`.
- distinguish optimizable numeric parameters from fixed numeric literals so constants introduced for scaling, structural templates, protected-operation thresholds, or user-authored fixed values can stay unchanged.
- keep one `NumericLiteral` opcode. Fixed versus optimizable status belongs in the genotype's numeric-literal side table, addressed by `PayloadIndex`; the interpreter reads only the value, while numeric-optimizing evaluators use the metadata.
- return a new `SymbolicExpression` with optimized numeric-literal payloads; never mutate an existing candidate.
- evaluate newly created initial candidates and all offspring through the evaluator; if numeric optimization is enabled, the evaluator returns the optimized candidate in the resulting `Solution<SymbolicExpression>`.
- store the returned solution in the population; the raw pre-optimization candidate is not the candidate associated with the fitness.
- expose function/gradient evaluation counters and numeric-optimization outcome status.
- keep optimization budget, maximum iterations, tolerances, and failure behavior explicit.
- do not add numeric literal bounds in Stage 3.1.
- expected non-convergence or numeric optimizer failure returns the original candidate with failure status; unexpected programming/configuration errors may throw.
- crossover and mutation preserve literal metadata for copied subtrees and assign deliberate metadata for newly generated literals.

Algorithm integration:

- Do not attach numeric optimization to crossover or mutation; candidate changes that affect fitness must happen through the evaluator after the complete variation pipeline.
- Keep GA authoring explicit and friendly: builders expose `Crossover`, `Mutator`, `MutationRate`, and an evaluator/problem configuration that determines whether numeric parameters are optimized.
- In Stage 3.1, GA can simply call the configured operators in the required order: select parents, cross, optionally mutate, evaluate into returned solutions, then replace.
- A shared offspring-pipeline helper (like Jenetics-style alterer chains) may be introduced later if multiple population algorithms duplicate the same variation/evaluation flow, but it is not part of the Stage 3.1 public or required internal design.

Stage 3.1 tests: no in-place evaluation mutation, immutable replacement candidate, initial-population optimization, offspring optimization after mutation, no optimization for fixed or absent constants, Levenberg-Marquardt convergence fixtures, automatic-differentiation gradient fixtures, optimization counters, failure fallback, and one GA usage spec with constant optimization enabled.

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
- Grammar logic drives option selection in the first restricted implementation. Do not add a public generic restriction-provider abstraction before the grammar implementation proves the shape.
- `Contains` is a final validation check for grammar operators, not the primary construction strategy.
- Grammar-constrained operators may enumerate viable sites, cache grammar-derived metadata, use repair, or use bounded retry.
- The grammar-aware path may pay grammar costs; the unrestricted path must not.
- Grammar operators should reuse low-level immutable genotype operations where practical, but not share the unrestricted selection logic when grammar validity changes the set of legal edits.

Stage 4 tests: typed grammar validity, grammar operator option selection, grammar-preserving operators, scalar output enforcement, grammar-specific bounded failure behavior, and one usage spec showing a restricted grammar such as `log(variable)` only.

## Stage 5: Extensions And Composability

Add later symbolic-regression features without multiplying search-space subclasses by every combination of flavor:

### Stage 5.1: Structure Templates

- Structure templates use a different genotype shape: fixed template plus one expression component per wildcard. Each wildcard chooses an unrestricted or grammar-constrained expression search space. Wildcards should not be nested symbolic-regression problems unless they truly have independent data, objectives, and evaluation semantics.

Stage 5.1 tests: template genotype composition, wildcard-specific search-space containment, template instantiation, and one usage spec showing a future template wildcard owning either unrestricted or grammar-constrained expression search space.

### Stage 5.2: Shape Constraints

- Shape constraints are evaluation components over predictions, derivatives, sampled expression behavior, or other model observations. They should not create `ShapeConstrainedUnrestricted...` and `ShapeConstrainedGrammar...` search-space pairs unless the constraint truly changes local subtree admissibility.
- Do not add a `ShapeConstrainedSymbolicRegressionProblem` or shape-constraint list on the base problem. Shape-constrained use supplies a metric/constraint evaluator that owns its constraint configuration and consumes shared evaluation context from the problem.
- Single-objective and multi-objective shape-constrained regression are evaluator configurations: aggregate violations for single-objective use or expose separate violation dimensions for multi-objective use.

Stage 5.2 tests: shape-constraint evaluator ownership, empty-normal-problem API shape, single-objective penalty aggregation, multi-objective violation dimensions, and derivative/sampling fixtures.

### Stage 5.3: Algebraic Normalization And Candidate Transformers

- Algebraic normalization, simplification, constant folding, equivalent-form comparison, and numeric refinement are candidate transformers, comparers, or evaluators, not new expression search-space flavors.

Stage 5.3 tests: constant folding, candidate immutability, equivalent-form comparison fixtures, and transformer/evaluator composition.

### Stage 5.4: Vectorial GP Value-System Support

- Vectorial GP primarily extends expression value metadata, interpreter buffers, operation signatures, and result typing. The interpreter should become value-system-aware instead of assuming only scalar `double` series. If scalar-only assumptions break, add a value-system-aware expression search-space family rather than a cross-product with every other extension.

Stage 5.4 tests: value metadata, vector/scalar operation signatures, interpreter buffer typing, result typing, and search-space containment for value-compatible expressions.

### Stage 5.5: Time-Series Expression Support

- Time-series support follows the same direction as vectorial GP when it changes value metadata or available opcodes. Lag/window semantics belong in operation signatures, interpreter binding, and data-view context before they become separate problem types.

Stage 5.5 tests: lag/window binding, time-aware operation signatures, row-window validity, interpreter behavior fixtures, and regression usage specs over time-series data.

### Stage 5 Shared Rules

- Extensions compose around the expression component. A new search-space type is justified only when the feature changes the set of structurally valid candidates or the local closure rules needed by creation, crossover, mutation, or repair.

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

- Genotype: immutable `SymbolicExpression`, explicit `SymbolicExpressionOpCode : ushort`, binary arity in Stage 1, identical-instruction equality, and `NaN` for invalid numeric results.
- Binding and execution: draft authoring is name-first, compiled variable instructions use payload indexes into an expression variable table, interpreter memory is not shared mutably, and Operon remains the postfix/contiguous-encoding reference.
- Legacy: old mutable symbolic-expression-tree APIs move under `HEAL.HeuristicLib.Legacy...`; obsolete legacy types/methods use `Legacy` prefixes or suffixes only when needed to avoid name clashes.
- Formatting/serialization: Stage 1 includes debug/infix formatting; full serialization is deferred.
- Problem and search spaces: one symbolic-regression problem concept, with unrestricted and grammar-constrained scalar search spaces as distinct operator families.
- Evaluation/refinement: evaluators return the authoritative `Solution<TGenotype>`, not only objective values. An evaluator may return the original genotype or an immutable refined/repaired replacement genotype, and algorithms must pass that returned solution into replacement, selection, logging, and analysis.
- Problem evaluation: `IProblem` and `Problem` are batch native. `SingleSolutionProblem` is the scalar authoring base and currently owns the scalar to batch adapter.
- Extensions: compose around expression components; add search-space types only when local structural admissibility changes. Shape constraints live in evaluator/evaluation-strategy objects, not the base problem.
