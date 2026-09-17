# Execution instance resolver

This page documents how the resolver turns a configuration graph into an execution graph, and why the lookup works the way it does. It matters because observation, budgets, racing and per-cycle instance freshness all rest on these rules, and several plausible-looking alternatives break one of them silently.

For the surrounding concepts, read [configuration vs execution instances](/contributing/architecture/execution-instances) first.

## Two phases, two types

Declaring decorations and resolving instances are separate types, not separate moments.

<!-- prettier-ignore -->
<figure>
<svg viewBox="0 0 620 372" role="img" aria-label="The builder declares decorations and cannot resolve; Build hands back a resolver that resolves and cannot declare; CreateChildResolver opens a declaration phase for a child." style="width:100%;height:auto">
<defs><marker id="reg-a1" viewBox="0 0 10 10" refX="9" refY="5" markerWidth="6" markerHeight="6" orient="auto-start-reverse"><path d="M0 0 L10 5 L0 10 z" fill="currentColor"/></marker></defs>
<rect x="50" y="36" width="520" height="112" rx="6" fill="var(--vp-c-bg-soft,#f6f6f7)" stroke="var(--vp-c-divider,#c2c2c4)"/>
<text x="68" y="64" font-family="ui-monospace,SFMono-Regular,Menlo,Consolas,monospace" font-size="14" font-weight="600" fill="var(--vp-c-brand-1,#3451b2)">ExecutionInstanceResolverBuilder</text>
<text x="68" y="92" font-family="ui-monospace,SFMono-Regular,Menlo,Consolas,monospace" font-size="13" fill="currentColor">.Decorate(…)</text>
<text x="68" y="114" font-family="ui-monospace,SFMono-Regular,Menlo,Consolas,monospace" font-size="13" fill="currentColor">.Install(hook)</text>
<text x="68" y="138" font-size="12.5" fill="var(--vp-c-text-3,#8e8e93)">declares · cannot resolve</text>
<line x1="310" y1="150" x2="310" y2="190" stroke="currentColor" stroke-width="1.4" marker-end="url(#reg-a1)"/>
<text x="324" y="176" font-family="ui-monospace,SFMono-Regular,Menlo,Consolas,monospace" font-size="13" fill="var(--vp-c-brand-1,#3451b2)">declare(builder)</text>
<rect x="50" y="196" width="520" height="134" rx="6" fill="var(--vp-c-bg-soft,#f6f6f7)" stroke="var(--vp-c-divider,#c2c2c4)"/>
<text x="68" y="224" font-family="ui-monospace,SFMono-Regular,Menlo,Consolas,monospace" font-size="14" font-weight="600" fill="var(--vp-c-brand-1,#3451b2)">ExecutionInstanceResolver</text>
<text x="68" y="252" font-family="ui-monospace,SFMono-Regular,Menlo,Consolas,monospace" font-size="13" fill="currentColor">.Resolve(…)</text>
<text x="68" y="274" font-family="ui-monospace,SFMono-Regular,Menlo,Consolas,monospace" font-size="13" fill="currentColor">.CreateChildResolver()</text>
<text x="68" y="296" font-family="ui-monospace,SFMono-Regular,Menlo,Consolas,monospace" font-size="13" fill="currentColor">.CreateChildResolver(…)</text>
<text x="68" y="320" font-size="12.5" fill="var(--vp-c-text-3,#8e8e93)">resolves · cannot declare</text>
<path d="M570 296 H 596 V 92 H 576" fill="none" stroke="var(--vp-c-text-3,#8e8e93)" stroke-width="1.2" stroke-dasharray="4 3" marker-end="url(#reg-a1)"/>
<text x="310" y="356" text-anchor="middle" font-size="12.5" fill="var(--vp-c-text-3,#8e8e93)">CreateChildResolver(…) opens a declaration phase for a child</text>
</svg>
<figcaption>The two phases, and the one way back into declaration.</figcaption>
</figure>

A resolver is obtained by declaring what it decorates. A meta-algorithm that needs decorations of its own declares them for a child:

```csharp
var childResolver = resolver.CreateChildResolver(child =>
    child.Decorate(ObservedOperator, current => CountedOperatorFactory(current, counter)));

return new(childResolver.Resolve(Algorithm), counter, MaximumCount);
```

`ExecutionInstanceResolver.Create(declare)` does the same for the root. Both take a callback, so the builder never outlives the declaration it belongs to and cannot be reached once resolving starts.

**Why the split.** With one type carrying both operations, decorating something already resolved was accepted and then silently ignored — the instance cache answered first and never consulted decorations again. Reordering a decoration after a resolve disabled a budget with no exception and no failing test. Separate types, handed out only inside a declaration callback, make that impossible to write rather than an error to detect.

`ExecutionInstanceResolver.Create()` and `CreateChildResolver()` take no callback, for the common case of a resolver that declares nothing.

## Decorations

A decoration wraps whatever the resolver resolves for one configuration object. Decorations **compose**: several for one configuration all apply, and a child resolver's decorations apply on top of its ancestors' rather than replacing them.

Each decoration records its **origin**, which is a fact about who installed it rather than something it claims:

| Origin          | Declared by                                                      |
| --------------- | ---------------------------------------------------------------- |
| `Configuration` | the configuration being executed — a budget, a racing wrapper    |
| `Hook`          | an `IExecutionHook` installed on the run — an analyzer, a logger |

`ExecutionInstanceResolverBuilder.Install(hook)` stamps `Hook` for the duration of the call, so a hook cannot present itself as configuration.

## The lookup

`Resolve(r)` walks from the resolving resolver towards the root, asking three questions at each one, and builds if the walk finds nothing.

<!-- prettier-ignore -->
<figure>
<svg viewBox="0 0 620 496" role="img" aria-label="The walk goes from the resolving resolver towards the root and stops at the first resolver declaring a decoration. At each resolver three questions are asked in order: under construction, instance held, declares decorations." style="width:100%;height:auto">
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
<text x="20" y="158" font-family="ui-monospace,SFMono-Regular,Menlo,Consolas,monospace" font-size="11.5" letter-spacing="0.09em" fill="var(--vp-c-text-3,#8e8e93)">AT EACH REGISTRY, IN ORDER</text>
<rect x="20" y="176" width="580" height="48" rx="5" fill="none" stroke="var(--vp-c-divider,#c2c2c4)"/>
<text x="38" y="206" font-size="13" fill="currentColor">1 · is r under construction here?</text>
<text x="582" y="206" text-anchor="end" font-size="12" fill="var(--vp-c-text-3,#8e8e93)">return the partial</text>
<rect x="20" y="234" width="580" height="48" rx="5" fill="none" stroke="var(--vp-c-divider,#c2c2c4)"/>
<text x="38" y="264" font-size="13" fill="currentColor">2 · is an instance of r held here?</text>
<text x="582" y="264" text-anchor="end" font-size="12" fill="var(--vp-c-text-3,#8e8e93)">return it</text>
<rect x="20" y="292" width="580" height="48" rx="5" fill="none" stroke="var(--vp-c-danger-1,#b8272c)" stroke-width="1.4"/>
<text x="38" y="322" font-size="13" fill="currentColor">3 · are decorations for r declared here?</text>
<text x="582" y="322" text-anchor="end" font-size="12" font-weight="600" fill="var(--vp-c-danger-1,#b8272c)">stop the walk</text>
<rect x="20" y="362" width="580" height="58" rx="5" fill="var(--vp-c-bg-soft,#f6f6f7)" stroke="var(--vp-c-brand-1,#3451b2)" stroke-width="1.4"/>
<text x="38" y="387" font-size="13" font-weight="600" fill="var(--vp-c-brand-1,#3451b2)">otherwise</text>
<text x="38" y="409" font-size="12.5" fill="currentColor">build from the chain that applies at X, and store it at X</text>
<text x="20" y="452" font-size="12.5" fill="var(--vp-c-text-3,#8e8e93)">2 before 3: a resolver's own instance already includes its own decorations.</text>
<text x="20" y="474" font-size="12.5" fill="var(--vp-c-text-3,#8e8e93)">3 makes 2 sound: reaching an ancestor without stopping proves the chains match.</text>
</svg>
<figcaption>The walk, and the three questions asked at every resolver along it.</figcaption>
</figure>

No runtime chain comparison is needed. The walk establishes the equality structurally: if it never stopped, the chains are the same by construction.

## When two resolves share an instance

> Two resolvers share an instance of `r` exactly when the decorations applying to them are identical.

A decoration does not by itself prevent reuse. Only a _difference_ in decorations does.

<!-- prettier-ignore -->
<figure>
<svg viewBox="0 0 620 364" role="img" aria-label="A child that declares no decoration of its own reuses the parent's decorated instance; a child that adds one builds its own. Two sibling resolvers never share with each other." style="width:100%;height:auto">
<defs><marker id="reg-a3" viewBox="0 0 10 10" refX="9" refY="5" markerWidth="6" markerHeight="6" orient="auto-start-reverse"><path d="M0 0 L10 5 L0 10 z" fill="var(--vp-c-text-3,#8e8e93)"/></marker></defs>
<rect x="170" y="28" width="280" height="88" rx="6" fill="var(--vp-c-bg-soft,#f6f6f7)" stroke="var(--vp-c-brand-1,#3451b2)" stroke-width="1.5"/>
<text x="310" y="54" text-anchor="middle" font-size="14" font-weight="600" fill="currentColor">parent</text>
<text x="310" y="78" text-anchor="middle" font-family="ui-monospace,SFMono-Regular,Menlo,Consolas,monospace" font-size="13" fill="var(--vp-c-brand-1,#3451b2)">declares [ fP ]</text>
<text x="310" y="100" text-anchor="middle" font-family="ui-monospace,SFMono-Regular,Menlo,Consolas,monospace" font-size="12" fill="var(--vp-c-text-3,#8e8e93)">holds instance fP(r)</text>
<line x1="250" y1="116" x2="165" y2="186" stroke="var(--vp-c-text-3,#8e8e93)" stroke-width="1.2" marker-end="url(#reg-a3)"/>
<line x1="370" y1="116" x2="465" y2="186" stroke="var(--vp-c-text-3,#8e8e93)" stroke-width="1.2" marker-end="url(#reg-a3)"/>
<rect x="10" y="192" width="280" height="112" rx="6" fill="none" stroke="var(--vp-c-success-1,#18794e)" stroke-width="1.5"/>
<text x="150" y="219" text-anchor="middle" font-size="14" font-weight="600" fill="currentColor">child A</text>
<text x="150" y="243" text-anchor="middle" font-family="ui-monospace,SFMono-Regular,Menlo,Consolas,monospace" font-size="12.5" fill="var(--vp-c-text-3,#8e8e93)">declares nothing</text>
<text x="150" y="266" text-anchor="middle" font-size="12" fill="currentColor">applies [ fP ] — same as parent</text>
<text x="150" y="290" text-anchor="middle" font-size="13" font-weight="600" fill="var(--vp-c-success-1,#18794e)">reuses the parent's instance</text>
<rect x="340" y="192" width="280" height="112" rx="6" fill="none" stroke="var(--vp-c-divider,#c2c2c4)" stroke-width="1.4"/>
<text x="480" y="219" text-anchor="middle" font-size="14" font-weight="600" fill="currentColor">child B</text>
<text x="480" y="243" text-anchor="middle" font-family="ui-monospace,SFMono-Regular,Menlo,Consolas,monospace" font-size="12.5" fill="var(--vp-c-brand-1,#3451b2)">declares [ fC ]</text>
<text x="480" y="266" text-anchor="middle" font-size="12" fill="currentColor">applies [ fP, fC ] — differs</text>
<text x="480" y="290" text-anchor="middle" font-size="13" font-weight="600" fill="currentColor">builds its own instance</text>
<path d="M150 308 V 328 H 480 V 308" fill="none" stroke="var(--vp-c-text-3,#8e8e93)" stroke-width="1.2" stroke-dasharray="4 3"/>
<line x1="306" y1="320" x2="324" y2="336" stroke="var(--vp-c-danger-1,#b8272c)" stroke-width="2"/>
<line x1="324" y1="320" x2="306" y2="336" stroke="var(--vp-c-danger-1,#b8272c)" stroke-width="2"/>
<text x="315" y="356" text-anchor="middle" font-size="12.5" font-weight="600" fill="var(--vp-c-danger-1,#b8272c)">siblings never share</text>
</svg>
<figcaption>Sharing follows the decoration set, and flows from parent to child only.</figcaption>
</figure>

The full set of cases:

| Decorations      | Resolves      | Outcome                                                    |
| ---------------- | ------------- | ---------------------------------------------------------- |
| none             | parent, child | child reuses the parent's instance                         |
| none             | two siblings  | two instances — siblings never share                       |
| parent only      | parent, child | child reuses the parent's **decorated** instance           |
| parent only      | child only    | child builds and keeps it                                  |
| child only       | parent, child | parent gets an undecorated instance, child a decorated one |
| parent and child | child         | both decorations apply; the child builds its own instance  |

**Instances are stored where they are built, never hoisted.** Hoisting a newly built instance up to the ancestor that owns the decorations would let siblings share — and sibling isolation is exactly how `CycleAlgorithm.NewExecutionInstancesPerCycle` produces fresh instances per cycle.

## Ordering

Decorations are sorted innermost to outermost by three keys, in this order:

| Key               | Rule                      | Reason                                                        |
| ----------------- | ------------------------- | ------------------------------------------------------------- |
| 1. Origin         | configuration before hook | a wrapper that measures must never measure the observer       |
| 2. Resolver depth | deeper before shallower   | the most local budget is the least disturbed                  |
| 3. Install order  | earlier before later      | a trace installs the clocks it reads before installing itself |

Origin outranks depth: configuration declared in an ancestor still binds tighter than a hook declared below it.

### Why origin comes first

A duration budget times its call to whatever it wraps:

```csharp
var startTimestamp = timeProvider.GetTimestamp();
try { return ChildEvaluator.Evaluate(...); }
finally { duration.AddDuration(timeProvider.GetElapsedTime(startTimestamp)); }
```

Hooks are installed when the run is created, before anything resolves; configuration decorations are installed during resolution. Ordering purely by install time therefore puts every hook _inside_ every configuration wrapper.

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

A decoration produces a _resolvable_, and that wrapper resolves what it wraps through the resolver, so building a chain is re-entrant. Each link is published as **under construction** while the chain is built, so a wrapper receives the instance already created for its child instead of starting over.

<!-- prettier-ignore -->
<figure>
<svg viewBox="0 0 620 424" role="img" aria-label="Building a chain of two decorations: the raw instance is created and pinned, then each wrapper is created and pinned in turn, with each wrapper's own resolve answered by the pin below it." style="width:100%;height:auto">
<rect x="20" y="40" width="580" height="88" rx="6" fill="var(--vp-c-bg-soft,#f6f6f7)" stroke="var(--vp-c-divider,#c2c2c4)"/>
<text x="38" y="66" font-family="ui-monospace,SFMono-Regular,Menlo,Consolas,monospace" font-size="11.5" letter-spacing="0.08em" fill="var(--vp-c-brand-1,#3451b2)">STEP 1</text>
<text x="38" y="92" font-family="ui-monospace,SFMono-Regular,Menlo,Consolas,monospace" font-size="13" fill="currentColor">creates instance₀ from r</text>
<text x="38" y="116" font-family="ui-monospace,SFMono-Regular,Menlo,Consolas,monospace" font-size="12" fill="var(--vp-c-brand-1,#3451b2)">pinned:  r → instance₀</text>
<rect x="20" y="146" width="580" height="104" rx="6" fill="var(--vp-c-bg-soft,#f6f6f7)" stroke="var(--vp-c-divider,#c2c2c4)"/>
<text x="38" y="172" font-family="ui-monospace,SFMono-Regular,Menlo,Consolas,monospace" font-size="11.5" letter-spacing="0.08em" fill="var(--vp-c-brand-1,#3451b2)">STEP 2</text>
<text x="38" y="198" font-family="ui-monospace,SFMono-Regular,Menlo,Consolas,monospace" font-size="13" fill="currentColor">creates instance₁ from fInner(r)</text>
<text x="38" y="218" font-family="ui-monospace,SFMono-Regular,Menlo,Consolas,monospace" font-size="11.5" fill="var(--vp-c-text-3,#8e8e93)">its Resolve(r) is answered by the pin → instance₀</text>
<text x="38" y="240" font-family="ui-monospace,SFMono-Regular,Menlo,Consolas,monospace" font-size="12" fill="var(--vp-c-brand-1,#3451b2)">pinned:  r → instance₀ ,  fInner(r) → instance₁</text>
<rect x="20" y="268" width="580" height="104" rx="6" fill="var(--vp-c-bg-soft,#f6f6f7)" stroke="var(--vp-c-divider,#c2c2c4)"/>
<text x="38" y="294" font-family="ui-monospace,SFMono-Regular,Menlo,Consolas,monospace" font-size="11.5" letter-spacing="0.08em" fill="var(--vp-c-brand-1,#3451b2)">STEP 3</text>
<text x="38" y="320" font-family="ui-monospace,SFMono-Regular,Menlo,Consolas,monospace" font-size="13" fill="currentColor">creates instance₂ from fOuter(fInner(r))</text>
<text x="38" y="340" font-family="ui-monospace,SFMono-Regular,Menlo,Consolas,monospace" font-size="11.5" fill="var(--vp-c-text-3,#8e8e93)">its Resolve(fInner(r)) is answered by the pin → instance₁</text>
<text x="38" y="362" font-family="ui-monospace,SFMono-Regular,Menlo,Consolas,monospace" font-size="12" fill="var(--vp-c-brand-1,#3451b2)">pinned:  + fOuter(…) → instance₂</text>
<text x="20" y="396" font-size="12.5" fill="currentColor">Every pin is then removed, and instance₂ is stored under r.</text>
<text x="20" y="416" font-size="12.5" fill="var(--vp-c-text-3,#8e8e93)">The undecorated instance is created exactly once.</text>
</svg>
<figcaption>Building a two-decoration chain. The pins exist only for the duration of the build.</figcaption>
</figure>

## What the alternatives break

Each rule above exists because a simpler-looking rule fails somewhere:

| Alternative rule                                          | What it breaks                                                                                  |
| --------------------------------------------------------- | ----------------------------------------------------------------------------------------------- |
| Nearest resolver's decorations win, ignore ancestors      | An analyzer at the root vanishes as soon as any child resolver decorates the same configuration |
| Check ancestor instances before decorations               | A child's own decoration is skipped — a budget resolves an undecorated operator and never fires |
| Check decorations before ancestor instances, always build | Parent and child build two identical decorated instances and split any state they hold          |
| Hoist new instances to the ancestor owning the chain      | Siblings start sharing, so recreating execution instances per cycle stops working               |
| Order decorations purely by install time                  | Budgets measure the analyzers observing them                                                    |
| One type for declaring and resolving                      | Decorating after resolving is accepted and silently ignored                                     |

## Related pages

- [Configuration vs execution instances](/contributing/architecture/execution-instances)
- [Analyzers](/contributing/architecture/analyzers)
- [Operator implementation](/contributing/architecture/operator-implementation)
