# Symbolic Regression Benchmark Implications

## Status

This document records the architectural implications of the expression-tree representation benchmarks. It supersedes the earlier provisional assumption that the operator-native genotype should itself use contiguous postorder/RPN storage.

The decision is:

- use a persistent hierarchical `ExpressionTree` as the authoritative genotype;
- retain `CompiledExpression` as the compact opcode/RPN execution representation;
- expose compilation and `CompiledExpression` as an advanced public API so callers can explicitly precompile, optimize, retain, and repeatedly execute a plan;
- retain direct hierarchical evaluation as a supported CPU path;
- do not use flat class RPN or chunked RPN as the production genotype;
- keep genotype and execution storage as deliberately different representations with a compilation boundary between them.

## Question Investigated

The benchmarks compared whether one representation could efficiently serve both genetic operators and numeric evaluation, or whether the genotype and execution engine should use different layouts.

Four immutable genotype prototypes were tested:

1. contiguous RPN storage of `readonly record struct ExpressionNode` values;
2. contiguous RPN storage of class-based nodes;
3. a persistent hierarchical object tree with cached metadata and path-copying edits;
4. a persistent chunked RPN rope with 32-node leaf chunks.

Each prototype retained equivalent semantic node information: the originating `Symbol`, arity, variable payload, numeric payload, subtree metadata, equality, immutable edits, compilation, and direct batched evaluation. The benchmarks used realistic balanced trees of 31, 55, and 95 nodes, controlled node and subtree edit locations, mixed operations and terminals, and approximately 10-15% foldable constant or identity subexpressions.

The final population workloads covered:

- 1,000 individuals evolved for 25 generations to isolate genetic-operator behavior;
- the resulting aged populations for direct evaluation, compilation, and compile-plus-interpret workloads;
- a complete 256-individual, 10-generation pipeline evaluated on 1,000 rows;
- retained-memory diagnostics before and after 25 generations.

The detailed result files remain under [`benchmarks/results`](../benchmarks/results/) and [`benchmarks/results/pop-based`](../benchmarks/results/pop-based/).

## Main Results

### Population Workloads

| Workload | Struct RPN | Class RPN | Hierarchy | Chunked RPN |
| --- | ---: | ---: | ---: | ---: |
| Evolution time | 81.70 ms | 74.99 ms | **13.22 ms** | 115.40 ms |
| Evolution allocation | 85.89 MB | 73.77 MB | **17.55 MB** | 109.87 MB |
| Aged direct evaluation | 3.921 ms | 4.006 ms | **3.332 ms** | 4.445 ms |
| Aged compilation | **2.323 ms** | 2.373 ms | 2.371 ms | 4.330 ms |
| Aged compile plus interpret | **5.009 ms** | 5.323 ms | 5.097 ms | 7.726 ms |
| Full GP pipeline | 25.57 ms | 25.11 ms | **20.48 ms** | 33.54 ms |
| Full pipeline allocation | 28.18 MB | 27.28 MB | **21.04 MB** | 34.47 MB |

For the evolution-only workload, the hierarchy was approximately 6.2 times faster than struct RPN and allocated about 80% less. In the complete pipeline, common compilation, interpretation, and fitness work reduced the overall advantage, but the hierarchy remained approximately 20% faster and allocated 25% less than struct RPN.

### Genetic Edits

Controlled and end-to-end operator benchmarks consistently favored the hierarchy:

- end-to-end node replacement: about 140 ns for hierarchy versus 1,780 ns for struct RPN;
- end-to-end local perturbation: about 320 ns versus 1,994 ns;
- end-to-end crossover: about 71 ns versus 2,183 ns;
- controlled node replacement: 27-72 ns versus approximately 1.7-1.9 us;
- controlled subtree replacement: typically 16-59 ns versus approximately 1.3-3.1 us.

The reason is structural sharing. A hierarchical edit copies only the changed node and its ancestor path. A flat immutable RPN edit must allocate and copy most or all of a contiguous node array, even when the logical change is small. GP repeatedly performs exactly these small immutable edits, so this difference dominates the genotype workload.

### Evaluation And Compilation

Precompiled opcode RPN remained the fastest isolated execution path in many cases, especially for small row counts where semantic node dispatch and variable-name resolution were visible. At 55 nodes and 1,000 rows, for example, precompiled optimized interpretation took about 2.88 us, while direct hierarchy evaluation took about 3.75 us.

As row counts increased, arithmetic over the data dominated traversal overhead. At 55 nodes and 100,000 rows, precompiled unoptimized interpretation took about 366 us and direct hierarchy evaluation about 374 us. The layouts were therefore much closer once both used the same batching, spans, pooling, and numeric kernels.

Direct hierarchy evaluation was faster than direct struct RPN evaluation in the fresh and aged-population tests. The aged population result, 3.332 ms versus 3.921 ms, shows that structural sharing did not produce a measurable locality collapse in the tested workload.

Compilation from struct RPN, class RPN, and hierarchy was effectively tied for aged populations: 2.323 ms, 2.373 ms, and 2.371 ms. Traversing the object hierarchy was therefore not a material compilation penalty. Chunked RPN was substantially slower because accumulated rope fragmentation and navigation overhead worked against both edits and sequential processing.

Optimization gains depended on how much code could actually be removed. Constant folding and identity reduction produced useful improvements for some larger trees, but little or no gain where the optimized instruction count stayed close to the original or where data processing dominated. Optimization is valuable, but it is not a universal guarantee of faster one-shot CPU evaluation.

### Retained Memory

After 25 generations, all representations held the same 42,070 logical nodes with an average tree length of 42.07 and average depth of 6.471.

| Representation | Initial retained | Final retained | Allocated during evolution | GC collections |
| --- | ---: | ---: | ---: | ---: |
| Struct RPN | 1.640 MB | 1.262 MB | 83.899 MB | 8 / 5 / 1 |
| Class RPN | 2.960 MB | **0.784 MB** | 68.464 MB | 6 / 0 / 0 |
| Hierarchy | 3.992 MB | 0.893 MB | **9.842 MB** | **0 / 0 / 0** |
| Chunked RPN | 1.976 MB | 1.752 MB | 107.541 MB | 10 / 6 / 4 |

The hierarchy did not have the smallest initial retained footprint, because an independently built object tree has per-object overhead. Its final retained population was nevertheless compact because descendants were shared across related immutable individuals. More importantly, its allocation churn during evolution was dramatically lower and triggered no collections in the retained-population run.

Retained population size alone is not a sufficient genotype metric. A persistent representation deliberately trades additional object identity for subtree sharing, while a flat representation owns contiguous arrays. Evolution time, allocation rate, collection pressure, and the lifetime pattern of actual populations are the relevant combined measures.

## Interpretation Of The Representations

### Persistent Hierarchy

The hierarchy matches the semantic and operational shape of tree-based GP. Node replacement, local perturbation, subtree mutation, and crossover all affect a path or subtree. Direct children, cached length/depth/hash, and path-copying provide cheap immutable edits and natural tree navigation.

Its expected weaknesses, pointer chasing and object overhead, did not outweigh those benefits in the measured GP workloads. They were small for compilation and direct batched evaluation, and structural sharing prevented aged populations from becoming prohibitively fragmented.

### Flat Struct RPN

Contiguous struct storage is compact for an isolated tree and gives predictable sequential access. It remains a good execution layout after semantic nodes have been lowered to compact instructions. It is a poor immutable genotype layout because every structural edit copies a large contiguous region and recreates metadata.

### Flat Class RPN

Class RPN reduced some copying costs because arrays copied references rather than full node values, but it retained the fundamental whole-array edit cost while adding per-node object allocation and indirection. Its population evolution result was modestly better than struct RPN but far behind the hierarchy.

### Chunked RPN

The rope attempted to combine sharing with RPN ordering. In practice, balancing, splitting, concatenation, boundary fragments, and navigation costs outweighed the sharing benefit for GP-sized trees. Aging made sequential compilation and evaluation worse as fragments accumulated. The tested 32-node chunk design should not become the production genotype.

## Genotype Decision

`ExpressionTree` becomes an immutable persistent hierarchy:

- `ExpressionTree` is rooted directly in an immutable hierarchical `ExpressionNode` that owns its symbol, local payload, and child array;
- each node caches metadata justified by common operators, initially length, depth, and hash;
- edits rebuild only the affected ancestor path and reuse unchanged subtrees;
- macros remain one genotype node even when compilation lowers them to multiple instructions;
- equality remains semantic and includes the originating `Symbol` where it affects future operator behavior;
- public navigation uses `ExpressionTree` and `ExpressionNode` and does not expose opcode layout.

Operators represent a concrete edit target with a tree-bound `ExpressionPoint`. The point retains the selected node and complete parent/child path, allowing path-copying without repeated global integer-index lookup and distinguishing structurally shared occurrences.

Compiled artifacts are not part of genotype equality, hashing, search-space containment, mutation, or crossover. They are derived execution data, but they are not purely internal. Advanced callers may explicitly create and retain them when they need control over compilation, optimization, or repeated execution.

## Evaluation Decision

The compilation and interpreter system should remain. The benchmark does not show that opcode RPN is unnecessary; it shows that opcode RPN belongs after the genotype boundary.

```text
                              +-> direct hierarchical CPU evaluator
ExpressionTree ---------------+
                              +-> compiler/optimizer
                                      |
                                      v
                              CompiledExpression
                                      |
                                      +-> batched CPU interpreter
                                      +-> future backend-specific packing
                                              |
                                              v
                                       GPU population evaluator
```

### CPU Execution

Retain two CPU paths:

1. `ExpressionCompiler` lowers a hierarchy to `CompiledExpression`, optionally optimizing it, and `ExpressionInterpreter` executes the compact opcode stream.
2. A direct hierarchical evaluator executes semantic nodes with the same batched kernels and workspace discipline.

The CPU interpreter hot loop uses direct opcode-specific scalar/span paths and `TensorPrimitives` calls. Avoid delegates and per-instruction callback abstraction in this path unless benchmarks demonstrate an improvement.

Compilation is the canonical complete semantic path. A symbol that implements `IExpressionEmitter`, including a macro that lowers to several built-in opcodes, is executable without also supplying a second evaluation implementation. A direct evaluator must therefore either support the encountered symbol explicitly or fall back to compilation. Do not require every custom symbol to maintain equivalent emitter and direct-evaluator logic merely to enable this optimization.

The compiled path remains the primary general execution representation because it provides:

- compact sequential instructions and payload side tables;
- variable names resolved once to integer payload indexes;
- macro lowering independent of genotype shape;
- constant folding and identity reduction;
- a stable boundary for specialized CPU, SIMD, native, or future GPU backends;
- cheap repeated evaluation when the compiled plan is reused.

Expose this path publicly through an API equivalent to `ExpressionCompiler.Compile(tree, optimize)` or `tree.Compile(optimize)`, returning a public immutable `CompiledExpression`. Users must be able to compile once and invoke the interpreter repeatedly without hidden recompilation. The exact factory placement remains an API-shape decision, but public precompilation is a requirement.

Keep the compiled artifact read-only. Do not expose generic instruction locations or compiled-expression editing; a future need for efficient parameter updates should introduce an explicit parameterized execution-plan design.

The direct hierarchy path remains useful for built-in-only one-shot evaluations, small datasets where compilation cannot amortize, debugging, and reference validation. For example, with a 55-node tree, direct hierarchy evaluation beat compile-plus-interpret at 100 and 1,000 rows, while compiled execution recovered the advantage at 10,000 rows in the measured fixture. The results do not justify making either path universal, nor do they establish a stable automatic crossover threshold for production expressions.

For ordinary interpretation, compilation remains an implementation detail of the high-level evaluator. Most callers should be able to pass an `ExpressionTree` and data without understanding execution representations. The evaluator may use direct execution, compile and execute, or reuse an evaluator-owned compiled plan when all relevant context is known.

For advanced use, compiled execution is an explicit public path as well as the correctness-complete fallback. Direct execution should be selected only when all symbols support it. Do not put automatic dispatch policy on `ExpressionTree`; its explicit `Compile` convenience, if provided, only requests compilation. After the production hierarchy is implemented, benchmark a high-level evaluator policy over realistic combinations of tree length, row count, repeat count, optimization opportunity, and backend. Only then consider automatic selection.

### Compilation Lifetime And Caching

Compilation should occur at most once per immutable tree within one evaluation operation and the resulting plan should be reused for all batches and outputs in that operation. Recompiling inside each batch would invalidate the intended architecture.

Do not add a mutable compiled-plan cache directly to the immutable genotype by default. Such a cache introduces thread-safety, backend selection, optimization-level, and retention concerns into a structural value. If repeated evaluation of elites or duplicate trees makes caching valuable, use an explicit evaluator- or run-owned cache keyed by the tree and compilation options. Independently, an advanced caller may retain the public immutable `CompiledExpression` directly and control its lifetime.

Optimized compilation should be favored when a plan will be reused, when meaningful instruction reduction is available, or before transferring/executing programs on an accelerator. Unoptimized compilation remains useful for one-shot execution, diagnostics, and cases where optimization cost cannot amortize.

### GPU Direction

Compact opcode RPN is a plausible source representation for GPU evaluation, but `CompiledExpression` should not be frozen as the final GPU ABI without a dedicated benchmark. GPU execution will likely require a population-level packed plan rather than one independently allocated object per expression:

- concatenated instruction streams with per-individual offsets, or buckets by program length;
- packed payload and variable-index tables;
- batched input and output buffers;
- explicit handling of different program lengths and resulting warp divergence;
- backend-specific opcode support and memory alignment.

The intended future boundary is therefore either:

```text
ExpressionTree -> CompiledExpression -> GPU packer -> GPU expression batch
```

or, if measurements justify it:

```text
ExpressionTree -> GPU-specific compiler -> GPU expression batch
```

Both preserve the important decision: GPU-oriented linear storage is a derived execution artifact, not the genotype layout.

## Implementation Consequences

1. Replace the current contiguous `ExpressionTree` node storage with an immutable hierarchical implementation while preserving the public logical tree vocabulary.
2. Adapt node replacement, local perturbation, subtree replacement, and crossover to path-copying and subtree sharing.
3. Make `ExpressionNode` the immutable hierarchical node carrying its originating `Symbol`, local payload, children, and cached metadata; do not put `Instruction` or `OpCode` into the user-facing tree API.
4. Consolidate traversal, symbol emission, instruction construction, online optimization, and payload compaction behind `ExpressionCompiler` while retaining public `IExpressionEmitter` for custom symbols.
5. Keep `CompiledExpression`, payload compaction, macro lowering, constant folding, and the batched opcode interpreter.
6. Keep `CompiledExpression` and its compilation/optimization entry point public so advanced callers can precompile once and repeatedly interpret the result.
7. Provide a high-level interpretation API over `ExpressionTree` that does not require ordinary callers to know whether compilation occurred.
8. Keep compiled batch execution cohesive inside `ExpressionInterpreter`. Extract shared kernels only when a direct hierarchy evaluator creates demonstrated reuse.
9. Add production tests for hierarchy navigation, metadata, equality, path-copy edits, structural sharing, public precompilation, repeated compiled evaluation, direct evaluation, and semantic parity.
10. Re-run the focused and population benchmarks against the production hierarchy after migration. Prototype results justify the direction but do not replace regression measurements on final code.
11. Defer GPU implementation and automatic CPU execution dispatch to separate, benchmark-driven work.

## Deferred Node Layout Benchmark

The initial production `ExpressionNode` should use one child array for every arity, with an empty array for terminals. This keeps the hierarchy uniform and the implementation straightforward while the larger representation change settles.

After the production hierarchy and operator workloads are stable, compare this baseline against specialized terminal, unary, and binary node implementations behind the same public node API. The specialized variants should store unary and binary children in dedicated reference fields and retain equivalent metadata, immutability, structural sharing, equality, compilation, and evaluation behavior.

Measure at least tree construction, node replacement, subtree replacement, crossover, direct evaluation, compilation, allocation, and the multi-generation population workload. Adopt specialized node layouts only if they provide a material whole-workload improvement; isolated object-size or microbenchmark gains are insufficient.

## Benchmark Caveats

- The alternative representations are benchmark prototypes with parity for measured behavior, not complete production APIs.
- The primary tree fixtures used scalar binary operations and excluded macros. Macro lowering was tested elsewhere but did not influence this representation comparison.
- Population runs were single-process CPU workloads and did not measure parallel GP execution or GPU transfer/execution.
- The final retained populations became smaller on average than the initial 55-node fixtures. This is realistic for unconstrained subtree operations but should not be treated as the only possible bloat regime.
- Retained-population semantic checksums differ across separately launched processes because they use process-randomized .NET hash codes. Logical counts, shape statistics, and in-process correctness checks are the relevant comparisons. A deterministic semantic checksum should be added if future diagnostics compare runs across processes.
- The measured hardware and runtime were a 12th-generation Intel Core i5-12600KF on .NET 10. Results should be rechecked when runtime, hardware, batching, or operator behavior changes materially.

## Final Verdict

Use the representation best suited to each phase. The genotype should be a persistent hierarchical tree because GP spends substantial time creating structurally related immutable offspring, and the hierarchy is decisively better at that workload. Evaluation should continue to support compilation into compact opcode RPN because it remains an efficient CPU execution plan and provides the correct architectural boundary for optimization and future accelerator backends.

The result is not a compromise layout. It is an explicit two-stage design: semantic hierarchy for evolution, linear instructions for execution.
