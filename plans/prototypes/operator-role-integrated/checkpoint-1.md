# Checkpoint 1: API shape and preparation ownership

Date: 2026-10-16. Decision: continue to the next integrated feasibility checkpoint. This result does not authorize production migration or establish Package 0 completion.

## Findings

The selected consuming shape compiles and runs with the existing named execution interfaces. A candidate-only `IOperator<TCandidate, TOperation>` slot accepts generic naming directly. `typed.Resolve(source)`, optional resolution, `TryResolve` and control-returning resolution infer the operation without requiring an execution type argument. A four-argument typed scope supplies its actual state type; a three-argument scope uses `ISearchState`.

Representative external leaf authors retain domain methods. The stateless mutator overrides `Mutate`; the stateful mutator overrides `CreateInitialState` and `Mutate`; the explicit mutator supplies one protected typed construction hook. The public preparation entry on the leaf base is nonvirtual. Evaluation retains objective-vector results. This checkpoint implements representative paths, not every arity or single-item helper.

An external `Checksum` descriptor, typed resolution adapter, control, recommendation and generic forwarding topology compile in the consumer assembly. They require no registry or privileged access. This proves the forwarding and ownership subset across an assembly boundary; it does not yet prove the complete external invocation/advice protocol.

The experimental resolver retains one prepared owner per configuration reference and caches constructors by the complete requested view. One external configuration and raw execution implement mutation, evaluation, termination and the external scalar operation. Resolving views in different orders preserves the same mutable state and live control; independent roots initialize independent state.

The covariant bound interface preserves the authored factory's type. A broad terminator can be resolved through broad and narrower state views, in either order, including names prepared for the respective views. An incompatible invariant interceptor view fails before its raw constructor runs. Its successful owner preparation remains usable for a compatible view, and a subsequent incompatible view does not reinitialize it.

These experiments use the shipping resolver's scope machinery in an isolated backend build, as described in the [README](README.md). The prototype needs neither an algorithm operation nor a replacement algorithm host.

## Validation

Detailed runner output, including environment details and backend hashes, is retained locally and excluded from version control. The focused runner passed:

- 61 runtime assertions in nine groups: public API/authoring; multiple operations and construction views; state variance; ancestry/reference ownership; Fresh/Retained/Shared activation; contextual children; failure recovery; construction cycles; and collectibility.
- A separate positive compiler fixture for ordinary generic naming.
- Four expected compiler failures: candidate mismatch (`CS0029`), operation mismatch (`CS0029`), assigning an algorithm to an operator slot (`CS0266`), and passing an algorithm to generic operator naming (`CS1503`). The runner rejects unexpected additional C# error codes.

The runtime fixtures check parent-first reuse, child-first locality without hoisting, sibling-local independence, record-reference identity, unchanged earlier child bindings after reconstruction, and dependency resolution through the selected owner's construction frame. They check that failed preparation is retried, failed view binding constructs no node, successful preparation survives binding/construction failure, and the shared cycle guard rejects operation-changing and mixed legacy/new recursion.

Collectibility covers discarded child owners/configurations and fresh executions while a parent or source remains alive. Advice-key collectibility remains unvalidated because the new advice protocol is not implemented.

The backend build retains the repository analyzers. Warnings from production source and fixture code are retained in the generated command logs; this is not an analyzer-clean or whole-solution-validation claim. C# whitespace and changed Markdown are checked separately. Production source files and the historical prototype are unchanged.

## What remains uncertain

- General and typed advice preparation, view-specific advice constructors, ordering/deduplication, hook failure semantics and weak advice keys.
- The full synchronous binding protocol, external policy/lifecycle adapters, runtime metric applicability, retry and weighted choice/routing.
- Control preservation through inspection and retry, actual `EvolutionStrategy` adaptation, and native algorithm hosting integration.
- Remaining operations, complete authoring families, default/recommendation precedence coverage, and all cross-project consumers.
- Complete legacy/new coexistence and owner adaptation. Only legacy availability and mixed-graph cycle detection are tested here.
- Invocation allocation and latency budgets, preparation/rebinding costs, and the matched net source/API/extension-cost comparison. No reduction or performance claim follows from this checkpoint.

The full public contract is not frozen. In particular, the external descriptor currently implements only the forwarding subset, and naming demonstrates live controls only without operator advice.

## Next checkpoint

Add general and typed prepared advice with view-specific constructors to this same owner model. First test broad/narrow termination views and multi-operation configurations with shared general advice and operation-filtered typed advice. Preserve once-only advice state, reject unsupported bindings before child construction, and retain discarded-state/advice collectibility. That is the next significant interaction the current result does not establish.
