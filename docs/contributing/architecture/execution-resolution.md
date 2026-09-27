# Execution resolution

This page documents how a configuration graph becomes an execution graph, and why the lookup works the way it does. It matters because observation, budgets, racing and per-cycle execution freshness all rest on these rules, and several plausible-looking alternatives break one of them silently.

For the surrounding concepts, read [configuration vs execution nodes](/contributing/architecture/execution-nodes) first.

## Two phases, two types

Declaring decorations and resolving executions are separate types, not separate moments.

<!-- prettier-ignore -->
<figure>
<svg viewBox="0 0 620 372" role="img" aria-label="The builder declares decorations and cannot resolve; Build hands back a scope that resolves and cannot declare; CreateChildScope opens a declaration phase for a child." style="width:100%;height:auto">
<defs><marker id="reg-a1" viewBox="0 0 10 10" refX="9" refY="5" markerWidth="6" markerHeight="6" orient="auto-start-reverse"><path d="M0 0 L10 5 L0 10 z" fill="currentColor"/></marker></defs>
<rect x="50" y="36" width="520" height="112" rx="6" fill="var(--vp-c-bg-soft,#f6f6f7)" stroke="var(--vp-c-divider,#c2c2c4)"/>
<text x="68" y="64" font-family="ui-monospace,SFMono-Regular,Menlo,Consolas,monospace" font-size="14" font-weight="600" fill="var(--vp-c-brand-1,#3451b2)">ResolutionScopeBuilder</text>
<text x="68" y="92" font-family="ui-monospace,SFMono-Regular,Menlo,Consolas,monospace" font-size="13" fill="currentColor">.Decorate(…)</text>
<text x="68" y="114" font-family="ui-monospace,SFMono-Regular,Menlo,Consolas,monospace" font-size="13" fill="currentColor">.Install(module)</text>
<text x="68" y="138" font-size="12.5" fill="var(--vp-c-text-3,#8e8e93)">declares · cannot resolve</text>
<line x1="310" y1="150" x2="310" y2="190" stroke="currentColor" stroke-width="1.4" marker-end="url(#reg-a1)"/>
<text x="324" y="176" font-family="ui-monospace,SFMono-Regular,Menlo,Consolas,monospace" font-size="13" fill="var(--vp-c-brand-1,#3451b2)">declare(builder)</text>
<rect x="50" y="196" width="520" height="134" rx="6" fill="var(--vp-c-bg-soft,#f6f6f7)" stroke="var(--vp-c-divider,#c2c2c4)"/>
<text x="68" y="224" font-family="ui-monospace,SFMono-Regular,Menlo,Consolas,monospace" font-size="14" font-weight="600" fill="var(--vp-c-brand-1,#3451b2)">ResolutionScope</text>
<text x="68" y="252" font-family="ui-monospace,SFMono-Regular,Menlo,Consolas,monospace" font-size="13" fill="currentColor">.Resolve(…)</text>
<text x="68" y="274" font-family="ui-monospace,SFMono-Regular,Menlo,Consolas,monospace" font-size="13" fill="currentColor">.CreateChildScope()</text>
<text x="68" y="296" font-family="ui-monospace,SFMono-Regular,Menlo,Consolas,monospace" font-size="13" fill="currentColor">.CreateChildScope(…)</text>
<text x="68" y="320" font-size="12.5" fill="var(--vp-c-text-3,#8e8e93)">resolves · cannot declare</text>
<path d="M570 296 H 596 V 92 H 576" fill="none" stroke="var(--vp-c-text-3,#8e8e93)" stroke-width="1.2" stroke-dasharray="4 3" marker-end="url(#reg-a1)"/>
<text x="310" y="356" text-anchor="middle" font-size="12.5" fill="var(--vp-c-text-3,#8e8e93)">CreateChildScope(…) opens a declaration phase for a child</text>
</svg>
<figcaption>The two phases, and the one way back into declaration.</figcaption>
</figure>

A scope is obtained by declaring what it decorates. A meta-algorithm that needs decorations of its own declares them for a child:

```csharp
var childScope = scope.CreateChildScope(child =>
    child.Decorate(ObservedOperator, current => CountedOperatorFactory(current, counter)));

return new(childScope.Resolve<TCandidate, TRunSearchSpace, TRunProblem, TSearchState>(Algorithm), counter, MaximumCount);
```

`ResolutionScope.Create(declare)` does the same for the root. Both take a callback, then snapshot the builder's declarations into the scope. A callback can retain the builder, but later changes to it cannot affect that snapshot. Treat the builder as valid only during declaration.

**Why the split.** The scope exposes resolution and has no decoration-registration method. Its declaration snapshot stays fixed while executions are resolved, so the cache and decorations cannot diverge through changes to that scope. An escaped builder can still accept declarations, but they have no effect on an already-built scope; this is snapshot isolation, not a language restriction preventing the builder from escaping.

`ResolutionScope.Create()` and `CreateChildScope()` take no callback, for the common case of a scope that declares nothing.

## What a scope resolves

Anything implementing `IConfigurationNode` can be resolved. Decorations, the execution cache and `Decorate` all key on that non-generic interface, by reference.

Role configurations such as `IMutator<TCandidate>` name only their candidate. Their execution node type exists only once a run's search space and problem are known, so they cannot create an execution on their own. The primary entry point therefore takes the creation step as an argument:

```csharp
scope.Resolve(mutator, static (target, childScope) =>
    target.CreateExecutionInstance<TSearchSpace, TProblem>(childScope));
```

The role extensions wrap exactly that call, so an author writes `scope.Resolve<TCandidate, TRunSearchSpace, TRunProblem>(Mutator)`, or binds the types once with `scope.For<TCandidate, TRunSearchSpace, TRunProblem>()` and resolves every slot through the returned `ResolutionScope` without type arguments. A configuration whose execution type is fixed implements `IConfigurationNode<TExecution>` and resolves through the convenience `Resolve(configuration)`; `ResolveOptional` does the same for an optional slot.

The creation step runs for the configuration and then once for each decoration, innermost first, so every decoration must produce the same role type as the configuration it wraps.

A scope serves one run, and therefore one execution signature. An execution found in the cache that is not the requested type was built for a different search space or problem; resolution reports that instead of casting. Whether a configuration was written for the run's types is one question, answered by `IConfigurationNode.Fits(ExecutionSignature)` from type arguments alone, so pre-flight validation can ask it before anything is built. The authoring bases apply the same rule when they bridge to the run's types, and an observation applies it when it is resolved; a configuration that does not fit fails with `ExecutionSignature.Mismatch`, which names the types it was written for and the run's.

## Decorations

A decoration wraps whatever the scope resolves for one configuration object. Decorations **compose**: several for one configuration all apply, and a child scope's decorations apply on top of its ancestors' rather than replacing them.

Each decoration records its **origin**, which is a fact about who installed it rather than something it claims:

| Origin          | Declared by                                                      |
| --------------- | ---------------------------------------------------------------- |
| `Configuration` | the configuration being executed — a budget, a racing wrapper    |
| `Module`          | an `IExecutionModule` installed on the run — an analyzer, a logger |

`ResolutionScopeBuilder.Install(module)` stamps `Module` for the duration of the call, so a module cannot present itself as configuration.

Analyzers implement the module contract. Runs install their single attachment list in attachment order through `Install`, so analyzers and other modules share the same ordering and reference deduplication. Configuration-origin decorations still bind inside module-origin decorations.

## The lookup

`Resolve(r)` walks from the resolving scope towards the root, asking three questions at each one, and builds if the walk finds nothing.

<!-- prettier-ignore -->
<figure>
<svg viewBox="0 0 620 496" role="img" aria-label="The walk goes from the resolving scope towards the root and stops at the first scope declaring a decoration. At each scope three questions are asked in order: under construction, execution held, declares decorations." style="width:100%;height:auto">
<defs><marker id="reg-a2" viewBox="0 0 10 10" refX="9" refY="5" markerWidth="6" markerHeight="6" orient="auto-start-reverse"><path d="M0 0 L10 5 L0 10 z" fill="currentColor"/></marker></defs>
<text x="20" y="32" font-family="ui-monospace,SFMono-Regular,Menlo,Consolas,monospace" font-size="11.5" letter-spacing="0.09em" fill="var(--vp-c-text-3,#8e8e93)">THE WALK</text>
<rect x="20" y="56" width="160" height="62" rx="5" fill="var(--vp-c-bg-soft,#f6f6f7)" stroke="currentColor" stroke-width="1.4"/>
<text x="100" y="83" text-anchor="middle" font-size="14" font-weight="600" fill="currentColor">X</text>
<text x="100" y="104" text-anchor="middle" font-size="12" fill="var(--vp-c-text-3,#8e8e93)">resolving here</text>
<rect x="230" y="56" width="160" height="62" rx="5" fill="none" stroke="var(--vp-c-brand-1,#3451b2)" stroke-width="1.5"/>
<text x="310" y="83" text-anchor="middle" font-size="14" font-weight="600" fill="currentColor">parent</text>
<text x="310" y="104" text-anchor="middle" font-family="ui-monospace,SFMono-Regular,Menlo,Consolas,monospace" font-size="12" fill="var(--vp-c-brand-1,#3451b2)">declares fP</text>
<rect x="440" y="56" width="160" height="62" rx="5" fill="none" stroke="var(--vp-c-divider,#c2c2c4)" stroke-width="1.2"/>
<text x="520" y="83" text-anchor="middle" font-size="14" fill="var(--vp-c-text-3,#8e8e93)">root</text>
<text x="520" y="104" text-anchor="middle" font-size="12" fill="var(--vp-c-text-3,#8e8e93)">never reached</text>
<line x1="184" y1="88" x2="224" y2="88" stroke="currentColor" stroke-width="1.4" marker-end="url(#reg-a2)"/>
<text x="204" y="74" text-anchor="middle" font-size="11.5" fill="var(--vp-c-text-3,#8e8e93)">walk up</text>
<line x1="394" y1="88" x2="434" y2="88" stroke="var(--vp-c-text-3,#8e8e93)" stroke-width="1.2" stroke-dasharray="4 3"/>
<line x1="406" y1="80" x2="424" y2="96" stroke="var(--vp-c-danger-1,#b8272c)" stroke-width="2"/>
<line x1="424" y1="80" x2="406" y2="96" stroke="var(--vp-c-danger-1,#b8272c)" stroke-width="2"/>
<text x="415" y="72" text-anchor="middle" font-size="11.5" font-weight="600" fill="var(--vp-c-danger-1,#b8272c)">stop</text>
<text x="20" y="158" font-family="ui-monospace,SFMono-Regular,Menlo,Consolas,monospace" font-size="11.5" letter-spacing="0.09em" fill="var(--vp-c-text-3,#8e8e93)">AT EACH SCOPE, IN ORDER</text>
<rect x="20" y="176" width="580" height="48" rx="5" fill="none" stroke="var(--vp-c-divider,#c2c2c4)"/>
<text x="38" y="206" font-size="13" fill="currentColor">1 · is r under construction here?</text>
<text x="582" y="206" text-anchor="end" font-size="12" fill="var(--vp-c-text-3,#8e8e93)">return the partial</text>
<rect x="20" y="234" width="580" height="48" rx="5" fill="none" stroke="var(--vp-c-divider,#c2c2c4)"/>
<text x="38" y="264" font-size="13" fill="currentColor">2 · is an execution of r held here?</text>
<text x="582" y="264" text-anchor="end" font-size="12" fill="var(--vp-c-text-3,#8e8e93)">return it</text>
<rect x="20" y="292" width="580" height="48" rx="5" fill="none" stroke="var(--vp-c-danger-1,#b8272c)" stroke-width="1.4"/>
<text x="38" y="322" font-size="13" fill="currentColor">3 · are decorations for r declared here?</text>
<text x="582" y="322" text-anchor="end" font-size="12" font-weight="600" fill="var(--vp-c-danger-1,#b8272c)">stop the walk</text>
<rect x="20" y="362" width="580" height="58" rx="5" fill="var(--vp-c-bg-soft,#f6f6f7)" stroke="var(--vp-c-brand-1,#3451b2)" stroke-width="1.4"/>
<text x="38" y="387" font-size="13" font-weight="600" fill="var(--vp-c-brand-1,#3451b2)">otherwise</text>
<text x="38" y="409" font-size="12.5" fill="currentColor">build from the chain that applies at X, and store it at X</text>
<text x="20" y="452" font-size="12.5" fill="var(--vp-c-text-3,#8e8e93)">2 before 3: a scope's own execution already includes its own decorations.</text>
<text x="20" y="474" font-size="12.5" fill="var(--vp-c-text-3,#8e8e93)">3 makes 2 sound: reaching an ancestor without stopping proves the chains match.</text>
</svg>
<figcaption>The walk, and the three questions asked at every scope along it.</figcaption>
</figure>

No runtime chain comparison is needed. The walk establishes the equality structurally: if it never stopped, the chains are the same by construction.

## When two resolves share an execution

> A resolve reuses the first eligible execution found in its own scope or an ancestor. Identical decoration chains permit ancestor reuse; they do not by themselves guarantee a shared execution.

A decoration does not by itself prevent reuse. An intervening declaration for `r` blocks reuse from above it because the chains differ. Declarations for other configurations do not block that lookup. If no eligible execution exists yet, the resolving scope builds and stores its own; resolution order therefore matters.

<!-- prettier-ignore -->
<figure>
<svg viewBox="0 0 620 364" role="img" aria-label="A child that adds no decoration for the target reuses the parent's cached decorated execution; a child that adds one builds its own. Siblings cannot read each other's caches." style="width:100%;height:auto">
<defs><marker id="reg-a3" viewBox="0 0 10 10" refX="9" refY="5" markerWidth="6" markerHeight="6" orient="auto-start-reverse"><path d="M0 0 L10 5 L0 10 z" fill="var(--vp-c-text-3,#8e8e93)"/></marker></defs>
<rect x="170" y="28" width="280" height="88" rx="6" fill="var(--vp-c-bg-soft,#f6f6f7)" stroke="var(--vp-c-brand-1,#3451b2)" stroke-width="1.5"/>
<text x="310" y="54" text-anchor="middle" font-size="14" font-weight="600" fill="currentColor">parent</text>
<text x="310" y="78" text-anchor="middle" font-family="ui-monospace,SFMono-Regular,Menlo,Consolas,monospace" font-size="13" fill="var(--vp-c-brand-1,#3451b2)">declares [ fP ]</text>
<text x="310" y="100" text-anchor="middle" font-family="ui-monospace,SFMono-Regular,Menlo,Consolas,monospace" font-size="12" fill="var(--vp-c-text-3,#8e8e93)">holds execution fP(r)</text>
<line x1="250" y1="116" x2="165" y2="186" stroke="var(--vp-c-text-3,#8e8e93)" stroke-width="1.2" marker-end="url(#reg-a3)"/>
<line x1="370" y1="116" x2="465" y2="186" stroke="var(--vp-c-text-3,#8e8e93)" stroke-width="1.2" marker-end="url(#reg-a3)"/>
<rect x="10" y="192" width="280" height="112" rx="6" fill="none" stroke="var(--vp-c-success-1,#18794e)" stroke-width="1.5"/>
<text x="150" y="219" text-anchor="middle" font-size="14" font-weight="600" fill="currentColor">child A</text>
<text x="150" y="243" text-anchor="middle" font-family="ui-monospace,SFMono-Regular,Menlo,Consolas,monospace" font-size="12.5" fill="var(--vp-c-text-3,#8e8e93)">declares nothing</text>
<text x="150" y="266" text-anchor="middle" font-size="12" fill="currentColor">applies [ fP ] — same as parent</text>
<text x="150" y="290" text-anchor="middle" font-size="13" font-weight="600" fill="var(--vp-c-success-1,#18794e)">reuses the parent's execution</text>
<rect x="340" y="192" width="280" height="112" rx="6" fill="none" stroke="var(--vp-c-divider,#c2c2c4)" stroke-width="1.4"/>
<text x="480" y="219" text-anchor="middle" font-size="14" font-weight="600" fill="currentColor">child B</text>
<text x="480" y="243" text-anchor="middle" font-family="ui-monospace,SFMono-Regular,Menlo,Consolas,monospace" font-size="12.5" fill="var(--vp-c-brand-1,#3451b2)">declares [ fC ]</text>
<text x="480" y="266" text-anchor="middle" font-size="12" fill="currentColor">applies [ fP, fC ] — differs</text>
<text x="480" y="290" text-anchor="middle" font-size="13" font-weight="600" fill="currentColor">builds its own execution</text>
<path d="M150 308 V 328 H 480 V 308" fill="none" stroke="var(--vp-c-text-3,#8e8e93)" stroke-width="1.2" stroke-dasharray="4 3"/>
<line x1="306" y1="320" x2="324" y2="336" stroke="var(--vp-c-danger-1,#b8272c)" stroke-width="2"/>
<line x1="324" y1="320" x2="306" y2="336" stroke="var(--vp-c-danger-1,#b8272c)" stroke-width="2"/>
<text x="315" y="356" text-anchor="middle" font-size="12.5" font-weight="600" fill="var(--vp-c-danger-1,#b8272c)">no lookup across sibling caches</text>
</svg>
<figcaption>Existing ancestor executions can be reused. Executions built in a child stay in that child.</figcaption>
</figure>

Representative cases (resolve order is left to right):

| Decorations      | Resolves      | Outcome                                                    |
| ---------------- | ------------- | ---------------------------------------------------------- |
| none             | parent, child | child reuses the parent's execution                         |
| none             | two siblings, no ancestor execution | each sibling builds its own execution |
| none             | parent, then two siblings | both siblings reuse the parent's execution |
| none             | child, then parent | each builds its own execution; the child retains its cached execution |
| parent only      | parent, child | child reuses the parent's **decorated** execution           |
| parent only      | child only    | child builds and keeps it                                  |
| child only       | parent, child | parent gets an undecorated execution, child a decorated one |
| parent and child | child         | both decorations apply; the child builds its own execution  |

**Executions are stored where they are built, never hoisted.** Hoisting a child's new execution to an ancestor would expose it to later sibling resolves. Keeping it local is what lets `CycleAlgorithm.NewExecutionInstancesPerCycle` build fresh executions when the parent has not already resolved those configurations. A fresh child scope can still reuse an eligible execution already held by an ancestor.

Reusing a cached composite also reuses the children it already holds. The resolver does not traverse those children again under the requesting scope. Scope nesting describes lookup ancestry; it does not identify which caller invokes a shared execution.

For example, a parent scope observes mutator M and constructs algorithm G, which retains its resolved M execution. A child scope adds another observation of M and resolves G. If G is reused from the parent, its stored M reference is unchanged: the parent's observation still receives calls through G, but the child's additional observation does not. Resolving M directly is a different operation from invoking G's already-bound child. Child registrations do not modify a reused composite's dependencies.

This limitation does not hide deferred descendants from parent observers. `CycleAlgorithm` and `PipelineAlgorithm` retain their creating scope and use it to create scopes for delayed child resolution. Observations inherited from that creating scope apply to those later resolutions. Reusing the algorithm from a different child scope does not replace its retained scope or add that requesting scope's observations to its future children. The distinction is which scope binds the dependencies, not whether execution has started.

## Ordering

Decorations are sorted innermost to outermost by three keys, in this order:

| Key               | Rule                      | Reason                                                        |
| ----------------- | ------------------------- | ------------------------------------------------------------- |
| 1. Origin         | configuration before module | a wrapper that measures must never measure the observer       |
| 2. Scope depth | deeper before shallower   | the most local budget is the least disturbed                  |
| 3. Declaration sequence | earlier before later | a trace installs the clocks it reads before installing itself |

Origin outranks depth: configuration declared in an ancestor still binds tighter than a module declared below it.

At the same origin and depth, declarations A then B produce `B(A(target))`. Entry work runs B then A; successful exit callbacks run A then B. This is the current wrapper order. General before, throwing, finally and around advice APIs are proposed work and need their own documented contracts.

### Why origin comes first

A duration budget times its call to whatever it wraps:

```csharp
var startTimestamp = timeProvider.GetTimestamp();
try { return ChildEvaluator.Evaluate(...); }
finally { duration.AddDuration(timeProvider.GetElapsedTime(startTimestamp)); }
```

Modules are installed when the run is created, before anything resolves; configuration decorations are installed during resolution. Ordering purely by install time therefore puts every module _inside_ every configuration wrapper.

<!-- prettier-ignore -->
<figure>
<svg viewBox="0 0 620 478" role="img" aria-label="Under install order the duration wrapper encloses the analyzer, so the timed span includes the analyzer's work. Under origin order the analyzer is outermost and the timed span covers only the evaluator." style="width:100%;height:auto">
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
<text x="20" y="272" font-family="ui-monospace,SFMono-Regular,Menlo,Consolas,monospace" font-size="11.5" letter-spacing="0.09em" fill="var(--vp-c-text-3,#8e8e93)">ORDERED BY ORIGIN</text>
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

## Building the chain

A decoration produces a _configuration_, and that wrapper resolves what it wraps through the scope, so building a chain is re-entrant. Each link is published as **under construction** while the chain is built, so a wrapper receives the execution already created for its child instead of starting over.

<!-- prettier-ignore -->
<figure>
<svg viewBox="0 0 620 424" role="img" aria-label="Building a chain of two decorations: the raw execution is created and pinned, then each wrapper is created and pinned in turn, with each wrapper's own resolve answered by the pin below it." style="width:100%;height:auto">
<rect x="20" y="40" width="580" height="88" rx="6" fill="var(--vp-c-bg-soft,#f6f6f7)" stroke="var(--vp-c-divider,#c2c2c4)"/>
<text x="38" y="66" font-family="ui-monospace,SFMono-Regular,Menlo,Consolas,monospace" font-size="11.5" letter-spacing="0.08em" fill="var(--vp-c-brand-1,#3451b2)">STEP 1</text>
<text x="38" y="92" font-family="ui-monospace,SFMono-Regular,Menlo,Consolas,monospace" font-size="13" fill="currentColor">creates execution₀ from r</text>
<text x="38" y="116" font-family="ui-monospace,SFMono-Regular,Menlo,Consolas,monospace" font-size="12" fill="var(--vp-c-brand-1,#3451b2)">pinned:  r → execution₀</text>
<rect x="20" y="146" width="580" height="104" rx="6" fill="var(--vp-c-bg-soft,#f6f6f7)" stroke="var(--vp-c-divider,#c2c2c4)"/>
<text x="38" y="172" font-family="ui-monospace,SFMono-Regular,Menlo,Consolas,monospace" font-size="11.5" letter-spacing="0.08em" fill="var(--vp-c-brand-1,#3451b2)">STEP 2</text>
<text x="38" y="198" font-family="ui-monospace,SFMono-Regular,Menlo,Consolas,monospace" font-size="13" fill="currentColor">creates execution₁ from fInner(r)</text>
<text x="38" y="218" font-family="ui-monospace,SFMono-Regular,Menlo,Consolas,monospace" font-size="11.5" fill="var(--vp-c-text-3,#8e8e93)">its Resolve(r) is answered by the pin → execution₀</text>
<text x="38" y="240" font-family="ui-monospace,SFMono-Regular,Menlo,Consolas,monospace" font-size="12" fill="var(--vp-c-brand-1,#3451b2)">pinned:  r → execution₀ ,  fInner(r) → execution₁</text>
<rect x="20" y="268" width="580" height="104" rx="6" fill="var(--vp-c-bg-soft,#f6f6f7)" stroke="var(--vp-c-divider,#c2c2c4)"/>
<text x="38" y="294" font-family="ui-monospace,SFMono-Regular,Menlo,Consolas,monospace" font-size="11.5" letter-spacing="0.08em" fill="var(--vp-c-brand-1,#3451b2)">STEP 3</text>
<text x="38" y="320" font-family="ui-monospace,SFMono-Regular,Menlo,Consolas,monospace" font-size="13" fill="currentColor">creates execution₂ from fOuter(fInner(r))</text>
<text x="38" y="340" font-family="ui-monospace,SFMono-Regular,Menlo,Consolas,monospace" font-size="11.5" fill="var(--vp-c-text-3,#8e8e93)">its Resolve(fInner(r)) is answered by the pin → execution₁</text>
<text x="38" y="362" font-family="ui-monospace,SFMono-Regular,Menlo,Consolas,monospace" font-size="12" fill="var(--vp-c-brand-1,#3451b2)">pinned:  + fOuter(…) → execution₂</text>
<text x="20" y="396" font-size="12.5" fill="currentColor">Every pin is then removed, and execution₂ is stored under r.</text>
<text x="20" y="416" font-size="12.5" fill="var(--vp-c-text-3,#8e8e93)">The undecorated execution is created exactly once.</text>
</svg>
<figcaption>Building a two-decoration chain. The pins exist only for the duration of the build.</figcaption>
</figure>

## What the alternatives break

Each rule above exists because a simpler-looking rule fails somewhere:

| Alternative rule                                          | What it breaks                                                                                  |
| --------------------------------------------------------- | ----------------------------------------------------------------------------------------------- |
| Nearest scope's decorations win, ignore ancestors      | An analyzer at the root vanishes as soon as any child scope decorates the same configuration |
| Check ancestor executions before decorations               | A child's own decoration is skipped — a budget resolves an undecorated operator and never fires |
| Check decorations before ancestor executions, always build | Parent and child build two identical decorated executions and split any state they hold          |
| Hoist new executions to the ancestor owning the chain      | Siblings start sharing, so recreating execution nodes per cycle stops working               |
| Order decorations purely by install time                  | Budgets measure the analyzers observing them                                                    |
| One type for declaring and resolving                      | Decorating after resolving is accepted and silently ignored                                     |

## Related pages

- [Configuration vs execution nodes](/contributing/architecture/execution-nodes)
- [Analyzers](/contributing/architecture/analyzers)
- [Operator implementation](/contributing/architecture/operator-implementation)
