# Execution resolution

Execution resolution selects persistent state by configuration reference and binds typed execution nodes to the children and observations supplied by a resolution context. Typed factories separate state preparation from node binding.

For the surrounding concepts, read [configuration vs execution nodes](/contributing/architecture/execution-nodes).

## What the components do

The resolver answers two different questions: **which prepared state does this configuration use?** and **which nodes should this caller receive?** `ResolutionScope` routes those requests; it does not own the preparation state machines, node cache or wrapper-construction algorithm.

| Component | Responsibility |
| --- | --- |
| `ExecutionSharingScope` | Finds existing state in this scope or its ancestors, reserves missing preparations locally, and remembers direct selections. |
| `ExecutionPreparation` | Owns one configuration's preparation attempt and factory, including captured persistent state, pinned child selections and retained child scopes. It is not an execution node. |
| `ExecutionBindings` | Constructs and caches raw/wrapped nodes for the current observation path. Owns binding failures, wrapper ordering and contextual retained children. |
| `WrapperRegistration` | Owns a wrapper recipe and its separate preparation for each selected source, using weak source keys. |

These are internal storage owners with different lifetimes. A single preparation can supply nodes to several observation contexts. Each context needs its own node cache, while the shared factory and chosen dependencies survive. The sharing scope remembers which preparations belong together; it does not own their bound-node caches. A registration owns per-source wrapper preparation so another context does not reset the wrapper's state.

`WrappedNodes`, nested in `ExecutionBindings`, holds the original source and its already-built inner chain for one wrapper. `RetainedChildScope`, nested in `ExecutionSharingScope`, holds a retained child's once-only declaration snapshot. Construction and preparation failures are private records within their owners. These helpers are implementation details; users and factory authors use `ResolutionScope`.

## Declaration, preparation and binding

`ResolutionScopeBuilder.Wrap(source, recipe)` registers a wrapper and `Install` installs a module. `ResolutionScope.Create(declare)` and `CreateChildScope(declare)` snapshot those registrations before returning a scope. Keeping the builder and changing it later cannot change that snapshot. The scope resolves configurations and exposes no registration method.

A selected configuration prepares an `ExecutionFactory<TExecution>` once, without receiving a scope. Preparation allocates persistent state and validates configuration values. The returned factory receives a construction frame, resolves children and returns the typed execution node. It can run again under another observation context while retaining the prepared state.

For a configuration with a fixed execution contract:

```csharp
IConfigurationNode<TExecution> configuration = ...;
TExecution execution = scope.Resolve(configuration);
// The generic route uses this preparation adapter:
execution = scope.Resolve(configuration, static target => target.CreateExecutionFactory());
```

Run-typed roles use the same generic resolver with their own preparation adapter. Every generated wrapper must satisfy that adapter's configuration contract. The selected execution contract is recorded before preparation; an incompatible later request fails before binding another node.

`IConfigurationNode.Fits(ExecutionSignature)` is a type-only check for pre-flight validation and authoring bridges. The scope is keyed by configuration reference, with explicit child resolution. It does not discover dependencies by type or invoke operations.

<!-- prettier-ignore -->
<figure>
<svg viewBox="0 0 720 634" role="img" aria-labelledby="resolve-flow-title" style="width:100%;height:auto">
<title id="resolve-flow-title">Resolution selects a persistent preparation, then obtains execution nodes for the requesting observation path.</title>
<defs><marker id="resolve-flow-arrow" viewBox="0 0 10 10" refX="9" refY="5" markerWidth="6" markerHeight="6" orient="auto-start-reverse"><path d="M0 0 L10 5 L0 10 z" fill="var(--vp-c-text-2,#67676c)"/></marker></defs>
<g font-family="system-ui,sans-serif" fill="currentColor">
<rect x="140" y="20" width="440" height="56" rx="7" fill="var(--vp-c-bg-soft,#f6f6f7)" stroke="var(--vp-c-brand-1,#3451b2)" stroke-width="1.5"/>
<text x="360" y="45" font-size="17" text-anchor="middle" font-weight="600">Resolve(configuration)</text>
<text x="360" y="65" font-size="13" text-anchor="middle" >same public entry point for callers and factories</text>
<path d="M 360 76 V 108 H 185 V 132" fill="none" stroke="var(--vp-c-text-2,#67676c)" stroke-width="1.5" marker-end="url(#resolve-flow-arrow)"/>
<path d="M 360 108 H 535 V 132" fill="none" stroke="var(--vp-c-text-2,#67676c)" stroke-width="1.5" marker-end="url(#resolve-flow-arrow)"/>
<rect x="25" y="132" width="320" height="134" rx="7" fill="var(--vp-c-bg-soft,#f6f6f7)" stroke="var(--vp-c-divider,#c2c2c4)" stroke-width="1.5"/>
<text x="185" y="157" font-size="16" text-anchor="middle" font-weight="600">Direct request</text>
<text x="185" y="184" font-size="14" text-anchor="middle" >Use the selection in this sharing scope.</text>
<text x="185" y="205" font-size="14" text-anchor="middle" >Otherwise look in ancestors;</text>
<text x="185" y="226" font-size="14" text-anchor="middle" >if absent, reserve a new preparation here.</text>
<text x="185" y="250" font-size="13" text-anchor="middle" fill="var(--vp-c-brand-1,#3451b2)">ExecutionSharingScope</text>
<rect x="375" y="132" width="320" height="134" rx="7" fill="var(--vp-c-bg-soft,#f6f6f7)" stroke="var(--vp-c-divider,#c2c2c4)" stroke-width="1.5"/>
<text x="535" y="157" font-size="16" text-anchor="middle" font-weight="600">A factory resolves its child</text>
<text x="535" y="184" font-size="14" text-anchor="middle" >Use this preparation's pinned child.</text>
<text x="535" y="205" font-size="14" text-anchor="middle" >Otherwise select through its original</text>
<text x="535" y="226" font-size="14" text-anchor="middle" >sharing scope and pin that selection.</text>
<text x="535" y="250" font-size="13" text-anchor="middle" fill="var(--vp-c-brand-1,#3451b2)">ExecutionPreparation</text>
<path d="M 185 266 V 292 H 360 V 316" fill="none" stroke="var(--vp-c-text-2,#67676c)" stroke-width="1.5" marker-end="url(#resolve-flow-arrow)"/>
<path d="M 535 266 V 292 H 360" fill="none" stroke="var(--vp-c-text-2,#67676c)" stroke-width="1.5" marker-end="url(#resolve-flow-arrow)"/>
<rect x="140" y="316" width="440" height="68" rx="7" fill="var(--vp-c-bg-soft,#f6f6f7)" stroke="var(--vp-c-brand-1,#3451b2)" stroke-width="1.5"/>
<text x="360" y="342" font-size="16" text-anchor="middle" font-weight="600">One selected preparation</text>
<text x="360" y="365" font-size="14" text-anchor="middle" >Same factory and persistent state across observation paths.</text>
<path d="M 360 384 V 418" fill="none" stroke="var(--vp-c-text-2,#67676c)" stroke-width="1.5" marker-end="url(#resolve-flow-arrow)"/>
<rect x="70" y="418" width="580" height="141" rx="7" fill="var(--vp-c-bg-soft,#f6f6f7)" stroke="var(--vp-c-brand-1,#3451b2)" stroke-width="1.5"/>
<text x="360" y="446" font-size="16" text-anchor="middle" font-weight="600">Obtain nodes for this observation path</text>
<text x="360" y="472" font-size="14" text-anchor="middle" >Prepare the factory once; reuse a completed binding if available.</text>
<text x="360" y="495" font-size="14" text-anchor="middle" >Otherwise bind children, construct the raw node, apply wrappers,</text>
<text x="360" y="516" font-size="14" text-anchor="middle" >then publish the completed chain. Cache a fault on failure.</text>
<text x="360" y="542" font-size="13" text-anchor="middle" fill="var(--vp-c-brand-1,#3451b2)">ExecutionBindings</text>
<path d="M 360 559 V 587" fill="none" stroke="var(--vp-c-text-2,#67676c)" stroke-width="1.5" marker-end="url(#resolve-flow-arrow)"/>
<text x="360" y="612" font-size="16" text-anchor="middle" font-weight="600">Return the typed execution node</text>
</g>
</svg>
<figcaption>Selection decides which state survives. Binding decides which children and wrappers a caller reaches. Generated wrapper frames have one narrow override, shown in the wrapper diagram below.</figcaption>
</figure>

## Sharing state while constructing different nodes

A sharing scope keeps the selected preparation for each configuration reference. Each preparation keeps its factory and chosen dependencies. Equal configuration values remain independent when their references differ.

Execution bindings keep the callable nodes and their wrapper chains for one observation path. Each factory gets a `ResolutionScope` combining its preparation's original sharing scope and dependencies with the requesting bindings.

Direct resolution first uses a local selection, then searches ancestors, and otherwise reserves a new preparation locally. Wrappers do not block this search. A descendant never publishes its local selection to an ancestor.

| Resolution order | State ownership |
| --- | --- |
| Parent, then child | Child shares the selected parent state, including when it adds observations. |
| Two siblings before any ancestor selection | Each sibling owns independent state. |
| Child, then parent | Both own state; the child keeps its earlier selection. |
| Parent, then two siblings | Both siblings share the selected parent state. |

A factory's scope resolves each ordinary child through its preparation's original sharing scope and pins the selected preparation. Rebinding a composite preserves these selections. If C selected M locally before P constructed G with another M, resolving G through C still uses G's pinned M while C's direct M remains separate.

The bindings cache each completed node. A child with no new declarations may reuse an existing ancestor binding. Any added declaration causes contextual rebinding, even if it targets only a composite's descendant or an unrelated configuration. This conservative rule makes newly added child observations reachable without a dependency-discovery API. It does not prepare fresh state.

In the following diagrams, G is an algorithm, M its mutator, A a parent observer and B an additional child observer.

<!-- prettier-ignore -->
<figure>
<svg viewBox="0 0 720 405" role="img" aria-labelledby="shared-state-title" style="width:100%;height:auto">
<title id="shared-state-title">Parent and child have different G and M bindings but share G state and M state; the child alone reaches its additional observer B.</title>
<defs><marker id="shared-state-arrow" viewBox="0 0 10 10" refX="9" refY="5" markerWidth="6" markerHeight="6" orient="auto-start-reverse"><path d="M0 0 L10 5 L0 10 z" fill="var(--vp-c-text-2,#67676c)"/></marker></defs>
<g font-family="system-ui,sans-serif" fill="currentColor">
<text x="120" y="33" font-size="17" text-anchor="middle" font-weight="600">Parent P</text>
<text x="360" y="33" font-size="16" text-anchor="middle" font-weight="600">Persistent state</text>
<text x="600" y="33" font-size="16" text-anchor="middle" font-weight="600">Child C adds observer B</text>
<rect x="30" y="67" width="180" height="60" rx="7" fill="var(--vp-c-bg-soft,#f6f6f7)" stroke="var(--vp-c-brand-1,#3451b2)" stroke-width="1.5"/>
<text x="120" y="94" font-size="16" text-anchor="middle" font-weight="600">G parent node</text>
<text x="120" y="115" font-size="12" text-anchor="middle" >keeps its original children</text>
<rect x="270" y="67" width="180" height="60" rx="7" fill="var(--vp-c-bg-soft,#f6f6f7)" stroke="var(--vp-c-success-1,#18794e)" stroke-width="1.5"/>
<text x="360" y="94" font-size="16" text-anchor="middle" font-weight="600">G state</text>
<text x="360" y="115" font-size="13" text-anchor="middle" >prepared once</text>
<rect x="510" y="67" width="180" height="60" rx="7" fill="var(--vp-c-bg-soft,#f6f6f7)" stroke="var(--vp-c-brand-1,#3451b2)" stroke-width="1.5"/>
<text x="600" y="94" font-size="16" text-anchor="middle" font-weight="600">G child node</text>
<text x="600" y="115" font-size="12" text-anchor="middle" >children bound for C</text>
<path d="M 210 97 H 268" fill="none" stroke="var(--vp-c-text-2,#67676c)" stroke-width="1.5" stroke-dasharray="5 4" marker-end="url(#shared-state-arrow)"/>
<path d="M 510 97 H 452" fill="none" stroke="var(--vp-c-text-2,#67676c)" stroke-width="1.5" stroke-dasharray="5 4" marker-end="url(#shared-state-arrow)"/>
<path d="M 120 127 V 202" fill="none" stroke="var(--vp-c-text-2,#67676c)" stroke-width="1.5" marker-end="url(#shared-state-arrow)"/>
<path d="M 600 127 V 202" fill="none" stroke="var(--vp-c-text-2,#67676c)" stroke-width="1.5" marker-end="url(#shared-state-arrow)"/>
<text x="120" y="167" font-size="13" text-anchor="middle" >typed child call</text>
<text x="600" y="167" font-size="13" text-anchor="middle" >typed child call</text>
<rect x="30" y="202" width="180" height="89" rx="7" fill="var(--vp-c-bg-soft,#f6f6f7)" stroke="var(--vp-c-brand-1,#3451b2)" stroke-width="1.5"/>
<text x="120" y="229" font-size="16" text-anchor="middle" font-weight="600">M parent binding</text>
<text x="120" y="253" font-size="17" text-anchor="middle" >A → M</text>
<text x="120" y="277" font-size="12" text-anchor="middle" >parent observations</text>
<rect x="270" y="217" width="180" height="60" rx="7" fill="var(--vp-c-bg-soft,#f6f6f7)" stroke="var(--vp-c-success-1,#18794e)" stroke-width="1.5"/>
<text x="360" y="244" font-size="16" text-anchor="middle" font-weight="600">M state</text>
<text x="360" y="265" font-size="13" text-anchor="middle" >one advancing counter</text>
<rect x="510" y="202" width="180" height="89" rx="7" fill="var(--vp-c-bg-soft,#f6f6f7)" stroke="var(--vp-c-brand-1,#3451b2)" stroke-width="1.5"/>
<text x="600" y="229" font-size="16" text-anchor="middle" font-weight="600">M child binding</text>
<text x="600" y="253" font-size="17" text-anchor="middle" >A → B → M</text>
<text x="600" y="277" font-size="12" text-anchor="middle" >parent + child observations</text>
<path d="M 210 247 H 268" fill="none" stroke="var(--vp-c-text-2,#67676c)" stroke-width="1.5" stroke-dasharray="5 4" marker-end="url(#shared-state-arrow)"/>
<path d="M 510 247 H 452" fill="none" stroke="var(--vp-c-text-2,#67676c)" stroke-width="1.5" stroke-dasharray="5 4" marker-end="url(#shared-state-arrow)"/>
<text x="360" y="329" font-size="15" text-anchor="middle" font-weight="600">3 calls through P + 2 calls through C</text>
<text x="360" y="353" font-size="15" text-anchor="middle" >M advances 5 times · A observes 5 · B observes 2</text>
<text x="360" y="385" font-size="13" text-anchor="middle" >Solid arrows: operation calls. Dashed arrows: shared persistent state.</text>
</g>
</svg>
<figcaption>Rebuilding a node does not reset its state. C's G also retains the same selected M even if C previously resolved a different local M directly. The diagram assumes the parent already selected G and M.</figcaption>
</figure>

For example, P owns G -> M. C adds an observer of M. Resolving G through C creates a G binding whose M binding includes C's observer; the original G and M bindings remain unchanged. The G state and pinned M state survive reconstruction. Ordinary typed operations run directly on these nodes without a resolver lookup per call.

## Fresh and retained children

`CreateChildScope()` creates a fresh sharing scope. Existing ancestor state can still be shared; missing state is selected locally. Fresh children do not reset ancestor state or share newly selected state with siblings.

`GetOrCreateChildScope(key, declare)` retains a child sharing scope under a reference-identity key. A factory's scope keeps the retained child on its preparation; a standalone scope keeps it on its sharing scope. Allocate the key during preparation when it must survive rebinding.

The first call snapshots the declarations once. Later calls ignore the callback and return a scope combining that child's shared state with the requesting observations. Distinct keys and owners remain independent. A declaration failure is retained, and a recursive request for an unfinished declaration fails.

A deferred execution may retain its immutable construction frame and use it later. The frame continues to use its original dependencies and observation context. Rebinding elsewhere does not modify an active iterator or its children.

## Wrappers and controls

A declaration targets an original source configuration reference. Its callback receives that source, once per selected source execution and declaration, even when other declarations also apply. Each pair has its own generated configuration and prepared state, owned by the declaration's private `PreparedWrapper` helper.

For each context, the raw source binds first. Each wrapper then receives a `WrappedNodes` through its factory's scope. Resolving the supplied source returns that target's already-built inner chain. The target remains available for deferred resolution. It belongs only to that wrapper's scope; an additional child resolves through its own scope.

<!-- prettier-ignore -->
<figure>
<svg viewBox="0 0 720 553" role="img" aria-labelledby="wrap-chain-title" style="width:100%;height:auto">
<title id="wrap-chain-title">Construct the raw source M, then B around M, then A around B; both wrappers resolve original M but receive their own immutable inner target.</title>
<defs><marker id="wrap-chain-arrow" viewBox="0 0 10 10" refX="9" refY="5" markerWidth="6" markerHeight="6" orient="auto-start-reverse"><path d="M0 0 L10 5 L0 10 z" fill="var(--vp-c-text-2,#67676c)"/></marker></defs>
<g font-family="system-ui,sans-serif" fill="currentColor">
<text x="360" y="28" font-size="16" text-anchor="middle" font-weight="600">Both wrapper callbacks receive the original configuration M</text>
<rect x="25" y="60" width="670" height="96" rx="7" fill="var(--vp-c-bg-soft,#f6f6f7)" stroke="var(--vp-c-divider,#c2c2c4)" stroke-width="1.5"/>
<text x="46" y="87" font-size="21" fill="var(--vp-c-brand-1,#3451b2)" font-weight="600">1</text>
<text x="80" y="87" font-size="16" font-weight="600">Bind the source</text>
<text x="80" y="115" font-size="14" >The raw M node is ready to be wrapped.</text>
<rect x="532" y="83" width="131" height="48" rx="7" fill="var(--vp-c-bg-soft,#f6f6f7)" stroke="var(--vp-c-brand-1,#3451b2)" stroke-width="1.5"/>
<text x="597" y="113" font-size="19" text-anchor="middle" font-weight="600">M</text>
<path d="M 597 156 V 176" fill="none" stroke="var(--vp-c-text-2,#67676c)" stroke-width="1.5" marker-end="url(#wrap-chain-arrow)"/>
<rect x="25" y="176" width="670" height="128" rx="7" fill="var(--vp-c-bg-soft,#f6f6f7)" stroke="var(--vp-c-divider,#c2c2c4)" stroke-width="1.5"/>
<text x="46" y="204" font-size="21" fill="var(--vp-c-brand-1,#3451b2)" font-weight="600">2</text>
<text x="80" y="204" font-size="16" font-weight="600">Bind inner wrapper B</text>
<text x="80" y="233" font-size="14" >B's factory resolves M → raw M node.</text>
<text x="80" y="260" font-size="13" >Its WrappedNodes keeps that inner node.</text>
<rect x="475" y="193" width="188" height="94" rx="7" fill="var(--vp-c-bg-soft,#f6f6f7)" stroke="var(--vp-c-brand-1,#3451b2)" stroke-width="1.5"/>
<text x="490" y="216" font-size="18" font-weight="600">B</text>
<rect x="530" y="230" width="113" height="41" rx="7" fill="var(--vp-c-bg-soft,#f6f6f7)" stroke="var(--vp-c-divider,#c2c2c4)" stroke-width="1.5"/>
<text x="587" y="256" font-size="17" text-anchor="middle" >M</text>
<path d="M 597 304 V 324" fill="none" stroke="var(--vp-c-text-2,#67676c)" stroke-width="1.5" marker-end="url(#wrap-chain-arrow)"/>
<rect x="25" y="324" width="670" height="155" rx="7" fill="var(--vp-c-bg-soft,#f6f6f7)" stroke="var(--vp-c-divider,#c2c2c4)" stroke-width="1.5"/>
<text x="46" y="352" font-size="21" fill="var(--vp-c-brand-1,#3451b2)" font-weight="600">3</text>
<text x="80" y="352" font-size="16" font-weight="600">Bind outer wrapper A</text>
<text x="80" y="381" font-size="14" >A's factory resolves M → B(M).</text>
<text x="80" y="408" font-size="13" >Its own target keeps this longer chain.</text>
<text x="80" y="441" font-size="13" >Calls through the completed chain: A → B → M</text>
<rect x="448" y="341" width="215" height="121" rx="7" fill="var(--vp-c-bg-soft,#f6f6f7)" stroke="var(--vp-c-brand-1,#3451b2)" stroke-width="1.5"/>
<text x="462" y="364" font-size="18" font-weight="600">A</text>
<rect x="480" y="377" width="165" height="69" rx="7" fill="var(--vp-c-bg-soft,#f6f6f7)" stroke="var(--vp-c-divider,#c2c2c4)" stroke-width="1.5"/>
<text x="493" y="399" font-size="16" font-weight="600">B</text>
<rect x="544" y="403" width="83" height="31" rx="7" fill="var(--vp-c-bg-soft,#f6f6f7)" stroke="var(--vp-c-divider,#c2c2c4)" stroke-width="1.5"/>
<text x="586" y="425" font-size="15" text-anchor="middle" >M</text>
<text x="360" y="510" font-size="15" text-anchor="middle" font-weight="600">An existing parent A(M) keeps its original target.</text>
<text x="360" y="534" font-size="14" text-anchor="middle" >Rebound A reuses its prepared state and receives the new B(M) target.</text>
</g>
</svg>
<figcaption>A target belongs only to its wrapper's factory frame, including deferred calls. It does not leak into additional children or new child scopes. No already-created wrapper is modified.</figcaption>
</figure>

Generated wrappers are not additional wrapper targets. Explicit configured wrappers remain ordinary selectable source nodes. Independent declarations keep independent state even when they use the same callback.

Additional wrapper dependencies are selected in a private sharing scope. Its parent is the deeper of the source and declaring sharing scopes when they are comparable; otherwise it is the declaring sharing scope. Existing ancestors can supply dependencies, but missing children stay private and their selections remain pinned.

A source's operation-free control can be projected through the advanced `Resolve(..., selectControl, out control)` overload. The projection sees the same source's raw binding only after the full chain succeeds. Invoke operations through the returned wrapped execution. An explicit configured wrapper must deliberately expose its own control; the resolver does not unwrap it or discover arbitrary interfaces on its children.

Each registration records internally whether it was made during `Install(module)`. The builder deduplicates module instances by reference and restores the previous installation state after nested installations. Registrations outside module installation belong to configuration setup. There is no public origin or precedence setting.

## Ordering

Wrappers are sorted innermost to outermost by three keys, in this order:

| Key               | Rule                      | Reason                                                        |
| ----------------- | ------------------------- | ------------------------------------------------------------- |
| 1. Registration kind | configuration before module | a wrapper that measures must never measure the observer       |
| 2. Contextual depth | deeper before shallower   | the most local budget is the least disturbed                  |
| 3. Declaration sequence | earlier before later | a trace installs the clocks it reads before installing itself |

Registration kind outranks depth: configuration declared in an ancestor still binds tighter than a module declared below it.

For the same registration kind and depth, declarations A then B produce `B(A(target))`. Entry work runs B then A; successful exit callbacks run A then B.

Depth follows the current observation path. A retained child D viewed below a new child C orders as `P -> C -> D`, even when D's sharing scope was first created under P.

### Why module wrappers sit outside configuration wrappers

A duration budget times its call to whatever it wraps:

```csharp
var startTimestamp = timeProvider.GetTimestamp();
try { return ChildEvaluator.Evaluate(...); }
finally { duration.AddDuration(timeProvider.GetElapsedTime(startTimestamp)); }
```

Modules are installed when the run is created, before anything resolves; configuration wrappers are installed during resolution. Ordering purely by install time therefore puts every module _inside_ every configuration wrapper.

<!-- prettier-ignore -->
<figure>
<svg viewBox="0 0 620 478" role="img" aria-label="Under install order the duration wrapper encloses the analyzer, so the timed span includes the analyzer's work. With module wrappers outside configuration wrappers, the analyzer is outermost and the timed span covers only the evaluator." style="width:100%;height:auto">
<text x="20" y="30" font-family="ui-monospace,SFMono-Regular,Menlo,Consolas,monospace" font-size="11.5" letter-spacing="0.09em" fill="var(--vp-c-text-3,#8e8e93)">ORDERED BY INSTALL TIME</text>
<rect x="20" y="44" width="430" height="132" rx="6" fill="none" stroke="var(--vp-c-danger-1,#b8272c)" stroke-width="1.6"/>
<text x="36" y="67" font-family="ui-monospace,SFMono-Regular,Menlo,Consolas,monospace" font-size="13" fill="var(--vp-c-danger-1,#b8272c)">DurationMeasuring</text>
<rect x="52" y="76" width="366" height="84" rx="5" fill="none" stroke="currentColor" stroke-width="1.2"/>
<text x="68" y="99" font-family="ui-monospace,SFMono-Regular,Menlo,Consolas,monospace" font-size="13" fill="currentColor">Observing (analyzer)</text>
<rect x="86" y="108" width="298" height="40" rx="4" fill="var(--vp-c-bg-soft,#f6f6f7)" stroke="var(--vp-c-divider,#c2c2c4)"/>
<text x="235" y="134" text-anchor="middle" font-family="ui-monospace,SFMono-Regular,Menlo,Consolas,monospace" font-size="13" fill="currentColor">evaluator</text>
<path d="M460 76 h10 v84 h-10" fill="none" stroke="var(--vp-c-danger-1,#b8272c)" stroke-width="1.5"/>
<text x="480" y="123" font-size="12.5" font-weight="600" fill="var(--vp-c-danger-1,#b8272c)">timed span</text>
<text x="20" y="204" font-size="13" font-weight="600" fill="var(--vp-c-danger-1,#b8272c)">the analyzer's work is charged to the budget</text>
<text x="20" y="226" font-size="12.5" fill="var(--vp-c-text-3,#8e8e93)">attaching a trace shortens the run</text>
<text x="20" y="272" font-family="ui-monospace,SFMono-Regular,Menlo,Consolas,monospace" font-size="11.5" letter-spacing="0.09em" fill="var(--vp-c-text-3,#8e8e93)">MODULE WRAPPERS OUTSIDE</text>
<rect x="20" y="286" width="430" height="132" rx="6" fill="none" stroke="currentColor" stroke-width="1.2"/>
<text x="36" y="309" font-family="ui-monospace,SFMono-Regular,Menlo,Consolas,monospace" font-size="13" fill="currentColor">Observing (analyzer)</text>
<rect x="52" y="318" width="366" height="84" rx="5" fill="none" stroke="var(--vp-c-success-1,#18794e)" stroke-width="1.6"/>
<text x="68" y="341" font-family="ui-monospace,SFMono-Regular,Menlo,Consolas,monospace" font-size="13" fill="var(--vp-c-success-1,#18794e)">DurationMeasuring</text>
<rect x="86" y="350" width="298" height="40" rx="4" fill="var(--vp-c-bg-soft,#f6f6f7)" stroke="var(--vp-c-divider,#c2c2c4)"/>
<text x="235" y="376" text-anchor="middle" font-family="ui-monospace,SFMono-Regular,Menlo,Consolas,monospace" font-size="13" fill="currentColor">evaluator</text>
<path d="M460 350 h10 v40 h-10" fill="none" stroke="var(--vp-c-success-1,#18794e)" stroke-width="1.5"/>
<text x="480" y="375" font-size="12.5" font-weight="600" fill="var(--vp-c-success-1,#18794e)">timed span</text>
<text x="20" y="446" font-size="13" font-weight="600" fill="var(--vp-c-success-1,#18794e)">the budget measures the operator only</text>
<text x="20" y="468" font-size="12.5" fill="var(--vp-c-text-3,#8e8e93)">traced and untraced runs are identical</text>
</svg>
<figcaption>The one edge that moves, and what it costs. The measuring wrapper times its call to whatever it wraps.</figcaption>
</figure>

Under install order, the same configuration run with and without a trace performs a different number of evaluations and returns a different answer. Observation would be changing the search, which it must not do. The same applies to `DynamicRacingAlgorithm`, where it could change which contender wins.

Counting budgets are unaffected, because counting is order-independent. That is why the problem stayed invisible.

### Why depth comes second

Two nested duration budgets on one operator, an outer 10s and an inner 2s. Whichever binds tighter measures only the operator; the other also measures the tighter wrapper's overhead. Putting the deeper one innermost leaves the overhead in the budget with the most headroom, rather than in the one actually governing the run.

## Failure and lifetime ownership

| Failure | Retained result |
| --- | --- |
| Factory preparation | Faulted preparation; no later context repeats preparation. |
| Wrapper callback or preparation | Faulted occurrence for that source/declaration pair. |
| Raw or wrapper binding | Faulted binding in that context; no partially completed node is returned. A different observation context can try binding again with the same prepared state. |
| Dependency construction | Its selection remains pinned; already completed sibling dependencies remain valid. |
| Retained child declaration | Faulted slot; the callback is not rerun. |
| Control projection | Completed binding remains valid; the projection exception propagates. |
| Operation or observer callback | Normal invocation failure; resolution is not rolled back or restarted. |

`TryResolve` converts `InvalidOperationException` into a reason and propagates other exception types. It does not retry faulted construction. Active synchronous construction is checked across factory frames and fresh sharing scopes; creating another child scope cannot evade a recursive dependency.

Node caches belong to their execution bindings. Declarations hold prepared wrappers through weak source keys. Preparations refer weakly to their sharing owners, and a prepared wrapper avoids a strong path back into the declaration that owns it. Live scopes and deferred factory frames retain the sharing scopes they need. This supports collection of both fresh child state beneath a long-lived declaration and discarded observing contexts around a long-lived source.

The resolver supplies no general disposal contract and does not make shared state safe for concurrent operations. Iterators and problem subscriptions have explicit lifecycle owners.

## Related pages

- [Configuration vs execution nodes](/contributing/architecture/execution-nodes)
- [Analyzers](/contributing/architecture/analyzers)
- [Operator implementation](/contributing/architecture/operator-implementation)
