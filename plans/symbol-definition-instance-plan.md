# Symbol / Expression Node Plan

## Summary

Use `Symbol` for the reusable admissibility and behavior symbol, and `ExpressionNode` for a concrete occurrence in an `ExpressionTree`.

The plan targets the final symbolic-expression redesign, where `ExpressionTree` is the operator-native genotype and `CompiledExpression` is the execution-native compact representation.

Explicitly model both numeric terminal families:

- fixed constants: named or unnamed constants such as `pi` that do not local-mutate
- evolvable constants: constants initially sampled from a distribution and locally changed through numeric perturbations according to their originating symbol. This is the library's richer ERC-style constant; the public API calls it a constant, while `FixedConstant` makes the non-evolvable case explicit.

## Key Changes

- `Symbol` defines semantic identity, arity, display metadata, compilation behavior, payload initialization policy, and local perturbation policy.
- `ExpressionNode` is the abstract immutable hierarchical genotype node. Concrete terminal and arity-specific node types own their valid payload or child references plus cached subtree metadata. They are deliberately distinct from compiled `Instruction`.
- `Symbol.CreateNode(...)` is the regular construction path. It copies/materializes the supplied children and invokes the selected variable or evolvable-constant symbol's initialization policy when local payload must be sampled.
- Unary and binary nodes store direct child references. N-ary nodes store an internal child array for arity three and above; ownership-transfer construction remains an internal optimization rather than a general public API.
- Every node retains its symbol, including no-payload operation nodes. Symbol equality is semantic value equality, not reference identity: two otherwise equal-looking nodes are unequal when their symbols differ in behavior-affecting configuration, but separately constructed equal symbols produce equal nodes.
- Keep evolvable-constant local mutation tied to the originating symbol. A numeric-value mutator delegates to the symbol that owns the value's perturbation semantics.
- Fixed constants are not evolvable constants with zero-width distributions. They are fixed terminal symbols whose local mutation is a no-op or unsupported, while replacement mutation may still replace them through normal terminal replacement rules.
- `ExpressionTreeSearchSpace` internally owns symbols and exposes compatible-symbol selection and queries for creation, replacement, containment, and local mutation.
- `ExpressionTreeSearchSpace` is an operational search-space configuration: it combines hard admissibility with default proposal guidance. Its unrestricted containment uses aggregate coverage rather than requiring one structurally equal origin symbol. It deliberately ignores evolvable-constant initialization and local-perturbation policies, while each node retains its originating symbol for future local perturbation. See [Containment Matching](#containment-matching).
- Retain duplicate symbols in their supplied order. They are separate selection entries; callers express their relative selection probability through the aligned search-space weights. During expression-draft resolution, duplicate matches remain ambiguous so accidental duplication is visible to the caller.
- Retire `SymbolicExpressionSamplingProfile`. Its current sole responsibility, constant initialization, belongs to evolvable constant symbols. Do not retain an empty profile merely for hypothetical future cross-cutting settings.

## Symbol Hierarchy

Use a shallow public hierarchy of immutable record classes. The built-in operation set is deliberately closed, so each built-in operation has a small concrete symbol type rather than requiring users to compose an operation descriptor with a separate symbol object.

```text
Symbol
|- OperationSymbol
|  |- BuiltInOperationSymbol
|  |  |- AdditionSymbol
|  |  |- SubtractionSymbol
|  |  |- MultiplicationSymbol
|  |  |- DivisionSymbol
|  |  `- other one-opcode built-ins
|  `- SigmoidSymbol and other special-lowering operations
|- ConstantSymbol
|  |- FixedConstantSymbol
|  `- EvolvableConstantSymbol
`- VariableSymbol
```

- Concrete one-opcode symbols bind exactly one internal `OpCode` through `BuiltInOperationSymbol`; they do not duplicate opcode metadata, arity, display names, or interpreter behavior.
- `SigmoidSymbol` directly owns its fixed multi-opcode lowering. Do not add a generic macro-symbol or lowering-recipe abstraction until several macros demonstrate common behavior.
- Custom macro symbols override the protected `Symbol.Emit` method and emit built-in operations through the public `IExpressionEmitter` contract. The mutable compilation session and payload-table compaction remain internal implementation details.
- The internal opcode is a compilation concern and is not exposed through the user-facing genotype API. Users query semantic symbol types, for example `FindNodesOfSymbol<AdditionSymbol>()` and `FindNodesOfSymbol<SigmoidSymbol>()`.
- The search space owns the symbol choice set and optional aligned symbol-selection weights. Creators and replacement mutators sample compatible symbols proportionally to those weights. `null` weights mean uniform selection and use the fast uniform path; weighted candidate groups are cached per terminal/arity query.

## Numeric Constant Policies

Numeric constants deliberately separate creation from local change. An initializer samples a new value; a perturbation transforms an existing value. Neither is an implicit hard bound on a constant's future value.

- An `EvolvableConstantSymbol` owns an `InitialDistribution<double>` used when a creator inserts that constant or a replacement mutation changes a terminal into that constant.
- Distributions support weighted mixtures: a mixture selects one component distribution by weight, then samples it. Mixtures may be nested to model more complex initial distributions.
- An `EvolvableConstantSymbol` owns its `NumericPerturbation`. The symbol configuration is authoritative; a generic numeric-value mutator selects nodes but does not inject a competing perturbation policy.
- `NumericPerturbation` is the umbrella term, not `MutationDistribution`, because some perturbations transform the current value rather than merely sample a new value.
- The initial perturbation set is:
  - `Additive(deltaDistribution)`: sample `delta`, then produce `value + delta`.
  - `Multiplicative(relativeDeltaDistribution)`: sample `delta`, then produce `value * (1 + delta)`. This is relative to the current value; it is not relative to an initialization range.
  - `Resample(distribution)`: discard the current value and sample the supplied distribution.
  - `ResampleInitial()`: discard the current value and sample the owning evolvable constant symbol's initial distribution.
  - `Choose(weightedPerturbations)`: select one perturbation by weight and apply it. It may be nested, for example to use additive mutation half the time and reinitialization half the time.
  - `Chain(perturbations)`: apply each perturbation sequentially to the value produced by the preceding one. It is atomic: if a stage is inapplicable, discard all temporary results and report the original node as not perturbed. A stage that executes but happens to produce the same value remains a valid no-op and does not fail the chain.
- The no-configuration default is an evolvable constant with `Uniform(-1, 1)` initialization and `Choose(80% Multiplicative(Uniform(-0.1, +0.1)), 20% ResampleInitial())` local perturbation. Default numeric-value mutation selects eligible evolvable-constant nodes and uses their symbol defaults. The resampling branch lets an exact zero escape the otherwise multiplicative local step.
- Fixed constants support no local numeric perturbation.
- Hard value bounds and numeric-optimizer domains are intentionally out of scope. An initial uniform distribution determines only fresh samples, not the set of values later mutation or optimization may reach.

## Local Perturbation Contract

Local perturbation is a common symbol capability, not a separate interface implemented only by selected symbols. A generic mutator must be able to find and change eligible nodes without type-testing each symbol kind.

- `Symbol` exposes `SupportsLocalPerturbation`, `CanPerturb(ExpressionNode)`, and `TryPerturb(...)`.
- `SupportsLocalPerturbation` is a cheap symbol-level capability filter. It is `false` for fixed constants and ordinary no-payload operations.
- `CanPerturb(ExpressionNode)` answers whether local perturbation is applicable to that concrete node. It handles instance-dependent cases such as a variable symbol with only one allowed variable.
- `TryPerturb(...)` is authoritative and applies the symbol-owned perturbation configuration. It returns `false` only when perturbation is inapplicable. It may return `true` together with an equal node because sampled no-ops are valid mutation outcomes.
- Do not add an `IPerturbableSymbol` interface. It would still require interface/type dispatch in generic mutators without providing a better performance or responsibility boundary than the common symbol contract.
- Do not introduce a generic runtime `SymbolPerturbation` request family. Each symbol owns the configuration and representation of its own perturbation policy.

## Local Perturbation Target Selection

The mutator, not the symbol, selects which perturbable nodes are targeted. It first takes a snapshot of tree-bound `ExpressionPoint` values whose symbols report `SupportsLocalPerturbation` and whose nodes satisfy `CanPerturb`; a node selected from that snapshot is perturbed at most once in one mutator call.

- `One`: select one eligible node.
- `All`: select every eligible node.
- `Each(probability)`: independently select each eligible node with the supplied probability.
- A selected node may validly remain equal after `TryPerturb`; no retry is implied to force a changed offspring.
- If no node is eligible or selected, the mutator returns an unchanged expression.

## Variable Policies

- A variable symbol owns the allowed variable references and creates nodes only from that set.
- Its default local perturbation selects uniformly from the allowed variables, including the node's current reference. A sampled unchanged variable is a valid successful no-op, consistent with numeric perturbations.
- A variable symbol also owns optional variable-selection weights aligned with its allowed-variable list. `null` weights mean uniform selection; provided weights guide both variable-node creation and local variable perturbation.
- A variable symbol requires at least one allowed variable. Its nodes remain locally perturbable even when only one variable is allowed, although perturbation then necessarily produces a no-op.

## Creation And Structural Mutation

- Tree-creation strategy owns terminal versus nonterminal selection. Full creation chooses nonterminals until the final permitted level, then chooses terminals; grow creation samples from all structurally viable symbols and can therefore produce short or unbalanced expressions.
- Symbol selection within a structurally viable candidate set uses the configured selection guidance. The search space exposes direct candidate queries by arity; arity zero naturally selects terminals, so operators do not use bounded retry loops.
- Point/node replacement is arity-preserving: a terminal is replaced by a terminal, and a nonterminal is replaced by a nonterminal with the same arity. A replacement may select the same symbol and therefore be a valid no-op.
- Terminal/nonterminal changes are structural operations, not node replacement: use subtree replacement, insertion, or shrink-style mutation when that behavior is needed.
- Node replacement initializes a fresh node through the selected symbol's creation behavior, so newly selected evolvable constants and variables carry the replacement symbol as their origin. Arity-specific edit paths reuse direct child references, while n-ary edits clone their child array once. Subtree replacement reuses the supplied immutable donor subtree.

## Macro Accounting

- A special-lowering symbol such as `SigmoidSymbol` remains exactly one `ExpressionNode` for genotype navigation, depth, length, complexity, mutation, crossover, formatting, and equality.
- Only compilation expands a macro node into multiple interpreter instructions. The compiled builder starts with one-instruction-per-node capacity and grows its buffers when macro expansion requires it.

## Storage And API Direction

- `ExpressionTree` should be optimized for generic operators: fast navigation, subtree metadata, immutable edits, and easy access to instances.
- `CompiledExpression` should be optimized for evaluation: compact opcode stream plus payload side tables. It is a public immutable advanced API so users may explicitly precompile, optimize, retain, and repeatedly execute an expression, while high-level interpretation hides this step for ordinary callers.
- `CompiledExpression` is a read-only derived artifact. It exposes no instruction-location or generic editing API; future efficient parameter updates require a dedicated parameterization design rather than mutation of compiled instructions.
- Store `ExpressionTree` as a persistent hierarchy rooted directly in `ExpressionNode`. `TerminalExpressionNode`, `VariableExpressionNode`, `NumericConstantExpressionNode`, `UnaryExpressionNode`, `BinaryExpressionNode`, and `NaryExpressionNode` eliminate invalid payload/arity states. Each node owns immutable child references and cached subtree length, depth, and hash. Immutable edits copy only affected ancestors and structurally share unchanged nodes.
- `ExpressionTree` and `ExpressionNode` are the two main genotype types. An `ExpressionNode` recursively represents the subtree rooted at that node; there is no separate `ExpressionSubtree` or internal structural-node wrapper.
- `ExpressionPoint` is an auxiliary tree-bound occurrence object for mutation, crossover, and path-copying edits. It retains the source tree, selected node, and complete parent/child path, so equal or structurally shared nodes at different positions remain distinct edit targets.
- Genotype node indexes, where an operator needs an integer selection index, follow natural root-first preorder and are resolved through cached subtree lengths. They are unrelated to the postorder instruction indexes used inside `CompiledExpression`.
- Terminal subtypes provide non-boxing payload storage and never own compiled instructions. Variable nodes store the selected variable name; numeric constant nodes store the value for both fixed and evolvable constant symbols.
- The completed representation benchmarks supersede the earlier flat-record-struct and uniform-child-array defaults. The selected layout specializes terminal payloads and stores unary/binary children directly, with an array only for n-ary nodes. See [symbolic-regression-benchmark-implications.md](symbolic-regression-benchmark-implications.md).
- Genotype storage does not mirror compiled payload-index storage. Compilation removes fixed/evolvable distinctions that are irrelevant to numeric execution and emits compact payload tables.
- No-state symbols such as `AdditionSymbol` may use singleton/flyweight values. Immutable genotype nodes may be structurally shared across related trees or multiple occurrences; `ExpressionPoint`, rather than node reference identity, identifies a concrete occurrence.
- Search-space construction exposes the documented common and weighted overloads. Exact parameter names and any convenience factory names are finalized through Stage 0 API usage specs before implementation hardens them.
- Required user ergonomics: common users must be able to configure arithmetic operations, allowed variables, `Constant(...)`, and `FixedConstant(...)` without understanding the full symbol/node model. Defaults must make evolvable constants usable without explicit distribution or perturbation configuration.
- Candidate public API directions to evaluate later: builder/factory methods, constructor overloads, or explicit symbols with convenience helpers.

## Equality And Configuration Values

- Symbols, distributions, numeric perturbations, and their composite forms must be immutable value-semantic objects. Do not permit lambdas, delegates, or other non-deterministically comparable behavior inside symbol configuration.
- Records are sufficient when their nested members are themselves value-semantic. Ordered collections used by a record require structural ordered equality rather than reference equality; use the repository's `Generator.Equals` `[Equatable]` and `[OrderedEquality]` pattern where it applies, as in `MultiMutator`.
- Mixture distributions and `Choose`/`Chain` perturbations compare their ordered components and weights structurally.

## Search-Space Authoring

- Provide overloads for the common cases: operations plus variables with default evolvable constants, and operations plus variables plus an explicit list of constant symbols.
- Provide weighted construction overloads using either aligned symbol/weight arrays or symbol/weight pairs. The paired form avoids positional misalignment; all forms normalize internally to a symbol array plus optional aligned weight array.
- Internally, the search space maintains both the canonical symbol list and pre-grouped cached candidate sets for fast terminal and arity queries. A candidate set with equal weights stores no weights and samples uniformly; weighted sets store aligned weights.
- `Contains` is a validity check, not a check of creation guidance. The unrestricted search space uses aggregate-coverage matching: its configured symbols may collectively cover an origin symbol even when no individual configured symbol is equivalent to it. It ignores search-space symbol-selection weights and evolvable-constant initialization/perturbation configuration. It validates operation semantics, every variable reachable through an origin variable symbol, fixed constants, and whether evolvable constants are permitted.
- Support both explicit operation construction such as `new AdditionSymbol()` and predefined operation groups through `Symbols`. Static operation-symbol factories may be added when their final naming is coordinated with `Symbol`, `ExpressionNode`, opcode, and `ExpressionDraft` vocabulary.
- `Symbols.MinimalOperations` contains addition, subtraction, multiplication, and division. `Symbols.DefaultOperations` adds exponential, logarithm, square root, and square as common mathematical functions. `Symbols.AllOperations` contains every predefined operation, including macro symbols. Keep default presets explicit rather than deriving them from broad categories so newly introduced operations do not silently change the default search space.

## Containment Matching

Symbol-bearing expression nodes retain their originating `Symbol`, which can contain both admissibility information and proposal guidance. Search-space containment therefore needs an explicit rule for relating a node's origin symbol to the symbols configured in a search space. Reference identity is not considered: symbols have semantic value equality and may be reconstructed independently.

Three containment models are available.

### Structural Matching

One symbol in the search space must structurally equal the node's origin symbol, including behavior-affecting configuration.

Advantages:

- The stored origin provides exactly the initialization and perturbation behavior configured by the active search space.
- Operators require no symbol resolution or rebinding.
- Symbol partitions and production identity remain significant.
- Containment and future origin-based local perturbation follow one consistent configuration.

Disadvantages:

- It is the least flexible model.
- A variable origin containing only `{x}` does not match a search-space symbol containing `{x, y}`.
- Separately configured symbols describing the same admissible values may not match.
- Changing proposal guidance can make an otherwise valid expression fail containment.
- Guidance effectively becomes part of search-space membership.

### Admissibility-Equivalence Matching

One symbol in the search space must describe the same admissible values or operation semantics as the node's origin, while proposal guidance such as selection weights, initialization distributions, and perturbation strategies is ignored.

Advantages:

- It separates mathematical admissibility from search guidance.
- Independently configured but semantically equivalent symbols can match.
- Guidance changes do not invalidate an otherwise equivalent expression.
- Symbol partitions remain meaningful because one matching search-space symbol is still required.

Disadvantages:

- It is difficult to reconcile with origin-authoritative perturbation.
- A contained node may retain guidance that is absent from the active search space.
- Operators must choose between the retained origin, a resolved search-space symbol, or rebinding the node.
- The system needs a separate containment-specific equivalence relation in addition to ordinary symbol value equality.

### Aggregate-Coverage Matching

The configured search-space symbols collectively cover the admissible outcomes of the node's origin symbol. No single search-space symbol has to be equivalent to the origin.

For example, search-space variable symbols `{x}` and `{y}` collectively cover a node whose origin allows `{x, y}`. Conversely, a search-space symbol `{x, y}` covers an origin allowing only `{x}`.

Advantages:

- It is the most flexible containment model.
- Different partitions of the same unrestricted domain remain containment-compatible.
- Draft-local and independently constructed symbols can be accepted when their outcomes remain in the aggregate domain.
- A broad origin can locally perturb across alternatives supplied by multiple search-space symbols.

Disadvantages:

- The partitioning of alternatives into search-space symbols becomes irrelevant to containment.
- A node with a narrower origin can have a narrower mutation neighborhood than the search space permits.
- Two contained nodes can explore the same search space differently because they retain different origins.
- Search-space selection guidance does not necessarily describe origin-based local perturbation behavior.
- It is unsuitable when a symbol's production or grammar origin is itself semantically significant.

### Selected Models

`ExpressionTreeSearchSpace`, the unrestricted symbolic-expression search space, uses **aggregate-coverage matching**.

- Operation and payloadless-terminal semantics must be represented by the configured symbols.
- A variable origin is covered when every variable it can produce is present in the aggregate variable domain of the search space.
- A fixed constant requires its fixed semantic constant to be configured.
- Evolvable constants are covered when the search space permits evolvable constants. Initialization distributions and numeric perturbation strategies are proposal guidance, not numeric bounds.
- Maximum length and depth remain ordinary hard containment constraints.

This rule is appropriate for unrestricted scalar GP because every scalar subtree is composition-compatible and symbol partitioning does not define a grammar. Origin symbols remain on nodes for equality and current local-perturbation behavior, but their exact configuration need not occur in the search space.

The future grammar-guided search space will probably use **structural matching**, or a grammar-specific refinement of it. The originating production, nonterminal category, and position-dependent alternatives can be semantically significant. Aggregate coverage across an entire grammar would discard those distinctions and could admit edits that no individual production permits. This grammar decision remains provisional until the grammar model is designed.

Consequences:

- Unrestricted containment is intentionally more permissive than symbol value equality.
- Search-space symbol selection weights and symbol-owned proposal guidance do not participate in unrestricted containment.
- A node with a narrower origin than the aggregate search-space domain remains contained but may have a narrower local perturbation neighborhood.
- Unrestricted operators must still guarantee that produced nodes remain within the aggregate domain.
- Grammar-aware operators and containment must not automatically reuse unrestricted aggregate matching.
- The coexistence of hard admissibility and proposal guidance in the current symbol/search-space model remains a possible future separation point.

## Expression Draft Resolution

- `ExpressionDraft.Build()` creates a standalone expression using draft-local symbols. The resulting expression supports low-level immutable editing and crossover; its local perturbation behavior is limited to those draft-local symbols.
- `ExpressionDraft.Build(searchSpace)` resolves draft terms against the supplied search-space symbols. It succeeds only when every unbound draft term has exactly one viable symbol; zero or multiple matches throw a clear resolution error rather than selecting an arbitrary origin symbol.
- `ExpressionDraft.TryBuild(symbols, out expression)` and the matching search-space overload provide the same resolution without throwing for ordinary no-match or ambiguity outcomes. `TryBuild` returns only `bool` and one expression `out` value; `Build` provides detailed failures through exceptions.
- `ExpressionDraft.Variable(name, variableSymbol: null)` accepts an optional explicit `VariableSymbol`. When supplied, that symbol is retained by the draft and used to resolve the resulting variable node during `Build` or `TryBuild`.
- Apply the same explicit-binding pattern to other draft terms when multiple viable origin symbols are meaningful, especially evolvable constants.

## Test Plan

- Fixed constants:
  - fixed `pi` can appear in an expression;
  - a named fixed constant and an unnamed fixed literal with the same numeric value remain distinct symbols;
  - local-parameter mutation leaves it unchanged or reports no local mutation;
  - replacement mutation can still replace it when terminal replacement is selected.
- Evolvable constants:
  - initialization uses the originating symbol's initial distribution;
  - local mutation uses the originating symbol and symbol-owned perturbation;
  - the default local perturbation makes multiplicative local steps most of the time and reinitializes occasionally;
  - evolvable constant symbols can configure weighted additive, multiplicative, and resampling perturbations;
  - a weighted distribution mixture samples according to its component weights, including when nested;
  - `Chain` applies its perturbations in order and atomically rolls back if a stage is inapplicable;
  - sampled no-op perturbations are successful outcomes;
  - two evolvable-constant symbols with overlapping numeric values but differing behavior-affecting configuration produce unequal nodes.
- Variables:
  - variable instances are restricted to the configured allowed names;
  - variable local mutation only selects names from the originating variable symbol and may validly retain the current name;
  - a variable symbol with one allowed name produces valid no-op local perturbations.
- Search-space behavior:
  - unrestricted containment uses aggregate coverage and validates operation semantics, all variable names reachable through a node's originating variable symbol, fixed constants, and evolvable-constant availability while ignoring sampling guidance;
  - duplicate symbols remain distinct supplied selection entries;
  - full and grow creators select structurally viable symbols according to their distinct terminal/nonterminal rules;
  - node replacement preserves terminal/nonterminal category and operation arity, while allowing no-op replacement;
  - subtree replacement is the structural mechanism for terminal/nonterminal changes;
  - replacement and creation use direct viable-candidate queries without bounded retry;
  - local mutation uses `SupportsLocalPerturbation` as a cheap filter and `CanPerturb` before calling `TryPerturb`;
  - local mutation only targets nodes whose originating symbol supports it.
  - `One`, `All`, and `Each(probability)` select from an initial snapshot of eligible node locations.
- Compilation:
  - genotype instances compile into compact opcode/payload tables;
  - a macro remains one genotype node and expands only while compiling;
  - interpreter behavior depends on semantic opcode/payload values, not mutation policy;
  - symbol references participate through semantic symbol equality in genotype equality/hash but are not required for numeric evaluation.

## Assumptions

- The settled structural vocabulary is `Symbol`, `ExpressionNode`, `ExpressionTree`, `ExpressionPoint`, `Instruction`, `OpCode`, `CompiledExpression`, and `CompiledSubExpression`.
- Default genotype equality includes semantic symbol equality where it affects future operator behavior; it does not depend on symbol object identity.
- Initial distributions and numeric perturbations are not hard numeric domains. Value bounds and numeric-optimizer domains remain out of scope for this slice.
- Numeric node payloads are not required to be finite. Do not add finite-value validation at the genotype boundary.
- Symbol-selection weights belong to the search space because it owns the symbol choice set. Variable-selection weights belong to `VariableSymbol` because it owns the allowed-variable choice set. Both store `ImmutableArray<double>.Empty` for the uniform fast path and normalized weights otherwise.
- Public API ergonomics are intentionally not settled in this plan; the implementation must not expose a clumsy symbols-only API as the only common-user path.
