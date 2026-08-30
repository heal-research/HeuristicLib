# Analysis system rework

## Summary

HeuristicLib has a sound central idea for analysis: analysis data belongs to one run and observations attach to algorithm or operator boundaries through the execution registry. The current implementation proves that model with algorithm quality curves, operator traces, nested meta-algorithms and experiment trials.

Two separate problems sit on top of that idea.

The first is mechanical. Observation merging depends on the caller's closed generic types, analyzer cleanup has lifecycle gaps, live results have no general concurrency contract and several public analyzers are incomplete or untested. Result naming, collection ownership and factory coverage also vary by type.

The second is conceptual, and it matters more. Every analyzer today is a hand-written type that hard-codes what it measures, how it summarizes, and which clock it records against. Because the clock is baked in, the same metric needs a new analyzer per axis, which is why `BestMedianWorstAnalysis` and `BestMedianWorstPerEvaluationAnalysis` both exist. HeuristicLab reached the same state from the same cause and shipped `BestAverageWorstQualityAnalyzer`, `QualityPerEvaluationsAnalyzer` and `QualityPerClockAnalyzer` as three types for one metric.

Almost the whole useful catalog is one mechanism with four independent choices: **where** to observe, **what** to measure, **how** to summarize it, and **which clocks** to record. Quality curves, variable frequencies, tree bloat, diversity, distribution histograms and dynamic per-epoch analysis are all combinations of those four. This plan therefore does two things: it repairs the run-owned framework, and it adds `Analyzer.Trace` as the direct composition path for most of the catalog. `CreateRun` binds the resulting analyzers before it materializes the execution graph.

The backlog phrase "desired-state analysis API" means an executable API usage spec for the intended replacement beside the current API specs. It does not mean analysis of a desired search state.

## What users need from analysis

### How users actually ask for it

Analysis requests are almost always one of these sentences:

- How did quality progress?
- Is that progress fair to compare against another configuration?
- What is my population doing — converging, diversifying, bloating?
- Which parts of the search space is the population using?
- What did this specific operator or inner algorithm do?
- Show me the final answer.
- Show me progress while it runs.

The first five are analysis. The last two are not: the final answer already comes from `Complete()`, and live progress already comes from `Stream()`. Both belong in the catalog only as boundary markers so the documentation can point away from analyzers.

### Use case catalog

| Class                   | Concrete examples                                                                                                          |
| ----------------------- | -------------------------------------------------------------------------------------------------------------------------- |
| Quality progress        | best, median and worst per iteration; the same per evaluation or per second; quality distribution                          |
| Population structure    | genotypic and phenotypic diversity; pairwise similarity matrix per generation; entropy; cluster count                       |
| Encoding-specific       | symbolic regression tree length, depth, variable frequency, symbol frequency, bloat; permutation edge frequency; real-vector per-dimension variance |
| Problem-specific        | constraint violation rate, feasible fraction, validation against training quality, objective component breakdown            |
| Multiobjective          | Pareto front over time, hypervolume, front size, crowding                                                                   |
| Dynamic problems        | best per epoch, invalid evaluations per epoch, performance before each change                                               |
| Budget and cost         | evaluations spent, time per operator, cache hit rate                                                                        |
| Cross-run comparison    | the same series collected per experiment trial, seed or configuration                                                       |
| Terminal artifacts      | best solution, final front, final model                                                                                     |
| Provenance              | genealogy, lineage depth, which operator produced the eventual best, operator success rates                                 |

### What the catalog has in common

Ignoring the domain and looking only at how observations turn into data, nearly every entry above is the same act repeated: **at a defined boundary, read something, summarize it, and record the summary against the clocks of that moment.**

The entries differ only in which of four choices they make.

1. **Where** the reading happens. The end of an iteration, an evaluation batch, a specific operator, a named inner algorithm.
2. **What** is read. An objective value, a tree length, the set of variables a candidate uses, a pairwise similarity, a validity flag.
3. **How** many readings become one recorded value. Best, median, worst, mean, a histogram, a frequency map, a matrix, a non-dominated set, or no reduction at all.
4. **Which explicitly selected clocks** the recorded value is filed under. Iteration, cumulative evaluations, elapsed time, epoch.

Choice 4 is the one the current design gets wrong. Clocks are not competing x-axes to pick between. When selected, iteration, evaluations, elapsed time and epoch describe the same event and can all be read from one series. None is automatic: each clock names the algorithm, evaluator, problem or time source that defines it. Treating axes as separate analyzer types forces reruns; attaching every possible source would add observation cost and silently choose among ambiguous inner algorithms or evaluators.

### What this shape does not cover

Two classes in the catalog are not this shape, and saying so is part of the requirement.

**Provenance** — genealogy, lineage, operator success rates — needs to correlate events *across* operator boundaries and therefore needs candidate identity that survives the pipeline. `GenealogyAnalysis` carries an `IEqualityComparer<TCandidate>` precisely because no such identity exists and it must reconstruct identity by value. Operator success rate additionally needs the dispatch decision inside a `ChooseOne*` operator to become visible, which it currently is not.

Separate the two halves of that problem. The *plumbing* half — several anchors of different roles contributing to one non-series store — is covered by the accumulator model described below, and genealogy should be remodelled onto it rather than left beside it. The *identity* half is not covered and is deferred: candidate identity, a first-class lineage model and operator attribution are revisited as their own piece of work once the measurement model is in place. Genealogy must keep working throughout, and nothing in this rework may foreclose provenance later.

**Budget and cost** are already served by the caller-owned instrumentation sinks `ObservationCounter` and `ObservationDuration`, which are read live by policies such as terminators. That model stays separate. A trace analyzer may sample such a sink when it records an entry, but the sink remains the source of truth.

**Terminal artifacts** are a degenerate trace of length one and need no separate mechanism.

## The analysis model

### Components

| Component            | Meaning                                                                          |
| -------------------- | -------------------------------------------------------------------------------- |
| **Anchor**           | The algorithm or operator boundary where readings are taken. Already exists. An analyzer may bind several. |
| **Measurement**      | What is read at that boundary. A candidate property, a pairwise property, a population property or an external value. |
| **Aggregation**      | How the readings taken at one firing become a single value, and how values carry across firings. |
| **Clock**            | One typed notion of time and the source from which it progresses.                         |
| **Moment**           | The simultaneous times read from the clocks selected for one trace entry.                 |
| **Recording policy** | Whether a given firing produces a contribution at all.                            |
| **Accumulator**      | How contributions fold into the analyzer's data. A series is one accumulator; a lineage graph is another. |
| **Trace**            | The ordered entries a trace analyzer recorded during a run. The common accumulator and result.       |
| **Run-bound analysis** | The stateful object returned by an analyzer factory. It collects one run's data and provides safe live reads. |

A clock defines one typed notion of time. The same clock object installs any observation it needs and provides typed
readback through `entry.At(clock)` and `trace.By(clock)`. Dynamic problems already have a domain clock in
`EvaluationTiming`, though it needs the rework described below before it is usable.

### Repeated firing is the normal case

An anchor fires many times in a run: once per generation, once per evaluation batch, once per operator call. The model must state what happens across those firings, because that is where the result shape is actually decided.

Each `Trace` observation runs the same cycle: measure, aggregate, moment, and then decide whether to record. Its collected data is therefore always a trace. A best-median-worst aggregation gives a trace of triples; a histogram aggregation gives a trace of histograms; a pairwise aggregation gives a trace of matrices. There is no separate mechanism for a single value. A single value is a trace of length one, or the last entry of a longer trace. HeuristicLab confirms the shape by having needed `DataTableHistory`, `HeatMapHistory` and `ScatterPlotHistory` as three distinct types for exactly this.

#### A firing yields at most one sample

One firing, one moment, one sample. Zero when the recording policy declines, never more.

This was challenged by dynamic problems, where `EvaluationTiming` is attached per evaluation so one evaluator firing appears to span several epochs. The conclusion is that the model is right and `DynamicProblem` is wrong, which is addressed below.

The alternative was to let an aggregation partition its readings by a clock and emit one sample per partition. That is rejected, because nothing needs it. Keying *within* a sample is what frequency maps already do, and transposed views read those back as one line per key. Partitioning into several samples would be a second way to express keyed data, with a second shape to store and read.

#### Aggregation works on two axes

Reducing the readings of one firing is the common case, but some analyses carry state between firings.

| Axis         | Meaning                                          | Examples                                    |
| ------------ | ------------------------------------------------ | ------------------------------------------- |
| Within firing | Many readings at one boundary visit become one value | best, median, worst; histogram; frequency map; similarity matrix |
| Across firings | Values carry state from previous firings         | best so far, cumulative count, running mean |

"Best so far" is a within-firing best followed by an across-firing running best. Keeping the two axes separate is what stops that from becoming its own analyzer type.

#### Recording policy decides what the series costs

Not every firing should produce a sample, and the difference is currently hard-coded into separate types: `BestMedianWorstAnalysis` records at every firing while `BestPerEvaluationAnalysis` records only when the best improves. That is the same one-type-per-variation mistake as the clock problem, so recording belongs in the model as its own choice.

At minimum the model must support recording at every firing, recording only when the value changes, recording every nth firing, and recording only the final firing.

This is also what makes expensive aggregations affordable. A similarity matrix per generation costs on the order of generations times population squared. Recording every tenth firing is the difference between a usable analyzer and one that must be labelled a memory hazard. The existing `StoreHistory` flag on `PopulationSimilarityAnalyzer` is a broken attempt at this concept and should be replaced by it rather than repaired.

#### Transposition is a read concern, not a storage concern

A series of frequency maps and a map of series carry the same information, but users want different ones. Nobody plots "the variable frequency map of generation 12"; they plot one line per variable across generations. HeuristicLab's variable frequency analyzer produces the transposed form directly, which is why that analyzer could not share machinery with the quality analyzers.

Store the trace as ordered entries. Provide transposed views over it for keyed values such as frequency maps and per-dimension measurements. Do not introduce a second accumulator shape to serve the transposed reading.

### The catalog as combinations

| User need                          | Anchor            | Measurement           | Aggregation          | Moment                     |
| ---------------------------------- | ----------------- | --------------------- | -------------------- | ------------------------- |
| Quality curve per iteration        | root algorithm    | objective value       | best, median, worst  | iteration, evaluations    |
| Quality curve per evaluation       | root algorithm    | objective value       | best, median, worst  | iteration, evaluations    |
| Quality distribution               | root algorithm    | objective value       | histogram            | iteration                 |
| Best so far                        | root algorithm    | objective value       | best                 | evaluations               |
| Tree bloat over time               | root algorithm    | tree length           | min, mean, max       | iteration, evaluations    |
| Variable frequency                 | root algorithm    | variables used        | frequency map        | iteration                 |
| Symbol frequency                   | root algorithm    | symbols used          | frequency map        | iteration                 |
| Population diversity               | root algorithm    | pairwise similarity   | min, mean, max       | iteration                 |
| Similarity matrix per generation   | root algorithm    | pairwise similarity   | matrix               | iteration                 |
| Feasible fraction                  | root algorithm    | feasibility           | mean                 | iteration                 |
| Validation against training        | root algorithm    | validation quality    | best                 | iteration                 |
| Pareto front over time             | root algorithm    | objective vector      | non-dominated set    | iteration, evaluations    |
| Hypervolume over time              | root algorithm    | objective vector      | hypervolume          | iteration, evaluations    |
| Inner algorithm quality            | named algorithm   | objective value       | best, median, worst  | iteration, evaluations    |
| Selection quality profile          | selector          | objective value       | mean                 | evaluations               |
| Best per epoch                     | evaluator         | objective value       | best                 | epoch                     |
| Invalid evaluations per epoch      | evaluator         | validity              | count                | epoch                     |

Every row above produces a series, one sample per recorded firing. The recording policy column is omitted because most rows use every firing; the ones that do not are worth naming, because each is currently a separate analyzer type for no other reason:

| User need                        | Recording           | Why                                                     |
| -------------------------------- | ------------------- | ------------------------------------------------------- |
| Best so far                      | on change           | a monotone series with repeats carries no extra information |
| Similarity matrix per generation | every nth firing    | cost is generations times population squared            |
| Full population capture          | every nth firing    | the only thing that makes it affordable at all          |
| Final best solution              | final only          | a series of length one                                   |

The first two rows of the main table are the point of the whole exercise. They are the *same configuration*; only the clock read at plot time differs. Under the current design they are two separate analyzer types, and consolidating them is listed below as a migration decision that this model dissolves entirely.

Rows four, twelve and thirteen show that "aggregation" is not limited to descriptive statistics — carrying a running best, a non-dominated set or a scalar computed from the whole reading set are the same slot.

### What is shipped

The library supplies the combinable pieces; users combine them, and only write an analyzer when they need something the model cannot express.

**Measurements.** General ones in the main package: objective value, objective vector, objective component, feasibility. Encoding-specific ones next to their encodings: symbolic expression tree length, depth, variable set, symbol set; permutation edges and positions; real-vector per-dimension values. Problem-specific ones supplied by the user.

**Aggregations, within a firing.** Best, median, worst, mean, standard deviation, min and max; count and count-matching; histogram; frequency map; distinct count; pairwise matrix; non-dominated set; and an identity aggregation that keeps every reading for callers who genuinely want the full population.

**Aggregations, across firings.** Running best, running worst, cumulative sum and cumulative count.

**Clocks.** Explicit iteration, cumulative-evaluation, elapsed-time and domain sources. Nothing is included by default. Only selected sources install observers or contribute values to moments.

**Recording policies.** Every firing, on change, every nth firing, and final only.

**Views over a series.** Typed `By(source)` reads against a selected clock, and transposition for keyed values such as frequency maps.

### Measurements read typed observations

The supply-interface hierarchy was tried and rejected. Types such as `ICandidateReadings`, `IObjectiveReadings`,
`IEvaluatedPopulation` and `IDerivation` existed mainly to make contravariance select valid anchor and measurement
combinations. They exposed type-system machinery as if it were domain language.

Each role-specific `Analyzer.Trace` overload maps its anchor directly to that role's typed observation. A target-typed
measurement lambda receives the complete observation and selects the data it needs. This is a runtime-only convenience;
an analyzer created with a lambda does not have serializable measurement configuration. Common and reusable measurements
are immutable classes implementing `IMeasurement<TObservation, TValue>`. Their settings are ordinary serializable
properties rather than captured delegate state.

No separate derivation value exists. `CrossoverObservation` already contains offspring, parent pairs, search space and
problem. A measurement that needs any combination reads it directly rather than receiving a second, less precise carrier.

This design accepts one typed installation overload per observable role. Those overloads establish the actual
anchor-to-observation relationship and are easier to discover than a public hierarchy of superficial capability
interfaces. Both the algorithm and crossover spikes now use this direct path.

Internally, a `TraceSource` retains the typed anchor, measurement and aggregation behind the simpler
`TraceAnalyzer<TResult>` type. A role-specific source installs the recorder that turns its observations into trace
entries. The source is retained configuration; the recorder is the execution-time callback.

### Clocks use observation, not ambient execution state

A caller explicitly selects each clock. Iterations observe a chosen algorithm, evaluations observe a chosen
evaluator, an epoch observes or reads a chosen dynamic problem, and elapsed time reads a chosen `TimeProvider`. This choice
is semantic: a nested algorithm has a different iteration count from the root, and several evaluator boundaries may count
different work.

Clocks use one of three acquisition modes. A source reads from the current immutable boundary context when the
analyzer and clock share a boundary. A source on another boundary uses the replacement and observer machinery to
retain its latest immutable value. A source such as elapsed time reads its value when the analyzer captures the moment.
Algorithms, operators and problems do not receive or write into an analysis context, and search states remain free of
execution data as required by § 4.2.

All clock and analyzer registrations are collected before the execution registry resolves the root instance. Only
selected sources install observable replacements. Child registries inherit those replacements as they do for existing
analyzer observations.

At a shared anchor, the observable wrapper creates an immutable observation before it dispatches analyzers. The observation
contains intrinsic facts about that occurrence, such as the algorithm iteration number. The analyzer passes that observation
to its selected clocks while capturing the moment. No clock observer or callback ordering is needed at
that boundary. Cross-boundary retained values have their ordinary temporal meaning: the latest value observed before the
entry. Built-in retained sources have an initial value, such as zero evaluations, so every selected clock is present
in every moment.

Two rules follow.

1. Anchor an interceptor when analyzing what that interceptor does, never to learn an iteration number.
2. Recover generations by grouping derivation samples with a selected iteration clock rather than using an interceptor as a clock.

Genealogy therefore binds derivation anchors and selects an iteration clock when generation grouping is wanted.

### The series is one accumulator, not the whole system

It is tempting to treat the series model as the analysis system and everything else as an escape hatch. That is the wrong layering, and genealogy shows why.

Decompose genealogy in the vocabulary above. Its anchor is a crossover; the firing supplies parents and offspring; what it reads is the parent-to-offspring relation; the clocks of the firing are the same iteration and evaluation counts as anywhere else. Only one thing differs: contributions fold into a graph instead of appending to an ordered list.

So the general mechanism is:

```text
a set of anchor bindings
        ↓ each firing produces
a contribution
        ↓ folded into
an accumulator
        ↓ published as
a safe read
```

A trace analyzer appends momented entries. A lineage analyzer merges edges and node data. Both are stateful objects created before a run and bound to exactly one run by `CreateRun`. They share anchors, observations, moments and the recording decision, and differ in their accumulation and reading APIs.

This layering has three consequences.

1. Genealogy is not a bypass. It is a second accumulator over the same plumbing, and remodelling it on the new system is a goal rather than a risk. It keeps its state-transition anchor: the run's iteration counter advances at root-algorithm boundaries, so for a nested or steady-state algorithm many derivation firings share one root iteration and the moment alone cannot say which generation a derivation belongs to.
2. **One run-bound analysis may bind several anchors.** Genealogy needs creation, crossover and mutation folding into one graph. Separate `Track...` calls create independent analyses.
3. Analysis-specific accumulation remains an extension point. A user who needs something that is neither a series nor a graph can provide custom accumulation and safe reading behavior without duplicating observation plumbing.

What genealogy still does not get from this is identity. Folding edges requires knowing which offspring came from which parent, and candidate identity across operator boundaries does not exist, which is why `GenealogyAnalysis` carries an `IEqualityComparer<TCandidate>` today. The accumulator generalization fixes the plumbing, not the provenance, and provenance stays deferred.

### Terminology

These terms are proposed additions to the glossary and should be settled before the specs are written.

| Term            | Meaning                                                                  |
| --------------- | ------------------------------------------------------------------------ |
| Iteration       | The algorithm's own step. `Generation` is a population-algorithm alias.   |
| Evaluations     | Cumulative candidate evaluations in the run. The comparable axis.        |
| Elapsed         | Wall-clock time since the run started.                                    |
| Moment           | The collective time recorded with one trace entry.                        |
| Firing          | One visit to an anchor. An anchor fires many times per run.               |
| Trace entry     | One moment and one aggregated value.                                       |
| Trace           | The ordered entries collected by one trace analyzer.                      |
| Measurement     | The value strategy read at a boundary.                                    |
| Aggregation     | The value strategy reducing readings to one value, within or across firings. |
| Recording policy | The rule deciding whether a firing produces a sample.                    |

## Goals

1. Express the mainstream catalog as combinations of anchor, measurement, aggregation, moment and recording policy rather than as one hand-written type per metric.
2. Record every sample against its explicitly selected clocks so that one series can be read on several chosen axes without a second analyzer.
3. Make retention a configured choice rather than a property of which analyzer type was picked, so expensive aggregations stay affordable.
4. Make the accumulator the extension point, so that analysis which is not a series is a different fold over the same anchors, firings and moments rather than a separate framework. Genealogy is the reference case.
5. Let one analyzer bind several anchors into a single result, qualifying them by supply rather than by role, and keep that distinct from attaching one analysis to several anchors for several results.
6. Preserve one fresh analysis result per analyzer and run.
7. Keep observations read only and separate from algorithm behavior.
8. Make analyzer setup, execution lifetime and result availability explicit.
9. Merge observations by anchor identity and observation role without depending on the caller's static generic view.
10. Detect observation anchors that never participate in the run instead of silently returning misleading empty results.
11. Give analyzer results a consistent ownership and immutability model.
12. Make custom analyzer authoring smaller than the current `IAnalyzerRunState` plus `ObservationPlan` model.
13. Preserve analysis across nested algorithms and recreated child execution registries.
14. Keep experiment trial results independent and strongly typed.
15. Retain only analyzers whose current contract is honest, observable and tested.

## Non-goals

1. Do not redesign algorithms, operator role contracts or the execution registry as a whole.
2. Do not combine run-owned analyzers with direct callbacks or caller-owned instrumentation.
3. Do not settle the complete `AlgorithmRun` and `ExperimentRun` lifecycle redesign unless analysis needs a bounded part of it.
4. Do not redesign multiobjective comparison inside this work. Pareto and hypervolume analysis should follow the separate objective-system decisions.
5. Do not perform namespace or folder moves in the same branch.
6. Do not preserve incomplete public types only for compatibility. HeuristicLib is still in early alpha.
7. Do not design provenance in this pass. Candidate identity across operator boundaries, a first-class lineage model and operator attribution are revisited once the measurement model is in place. Genealogy must keep working as a hand-written analyzer throughout, and the composition layer must not foreclose provenance.
8. Do not make measured data steer the search. An adaptive operator that reweights its children from measured success belongs to the caller-owned instrumentation model and its policy contract, not to run-owned analysis.
9. Do not force every analyzer through the composition layer.

## Terminology and scope

This plan uses the glossary meanings of analyzer, analyzer result, observation, observation anchor and run. It introduces measurement, aggregation, moment, sample and series as defined above; those are proposed glossary additions and must be settled before the specs are written.

It covers:

- the composition model of anchor, measurement, aggregation and moment
- the measurement and aggregation catalog shipped with the main and encoding packages
- stateful analyses bound to one run
- observation registration and installation
- observable algorithm and operator wrappers as analyzer infrastructure
- analysis binding during `CreateRun`
- analyzer fan-out across `ExperimentRun` trials
- concrete quality, population, diversity and dynamic analyzers
- private analysis adapters in Python interop

It does not cover:

- Roslyn analyzers
- provenance, candidate identity across operator boundaries and operator success rates
- adaptive operators that consume measured data to steer the search
- pure calculators and metrics that do not observe a run
- direct search-state streaming
- counting and duration wrappers except where their boundary with analyzers needs clarification

## Current architecture

The current execution path is:

```text
Analyzer configuration
        ↓ creates
Analyzer run state and result
        ↓ registers
ObservationPlan
        ↓ installs replacements into
ExecutionInstanceRegistry
        ↓ resolves
Observable algorithm and operator wrappers
        ↓ invoke callbacks that update
Analyzer result
        ↓ retrieved through
AlgorithmRun.GetResult(analyzer)
```

`ExperimentRun` creates one concrete analyzer per trial through `TrialAnalyzer`. Each trial keeps its own `AlgorithmRun`, observation registrations and result object.

### Systems in the current implementation

| System                   | Responsibility                                                | Current assessment                                  |
| ------------------------ | ------------------------------------------------------------- | --------------------------------------------------- |
| Search-state streaming   | Direct progress inspection through `Stream()`                 | Strong and simple                                   |
| Observable wrappers      | Callback hooks for algorithms and all nine operator roles     | Strong mechanism with repetitive authoring API      |
| Run-owned analyzers      | Reusable configuration with one result per run                | Sound ownership model with weak lifecycle contracts |
| `ObservationPlan`        | Combines analyzer requests and installs registry replacements | Useful design with brittle generic merging          |
| Experiment analysis      | Creates independent analyzers for every trial                 | Useful and reasonably well tested                   |
| Instrumentation wrappers | Caller-owned counters and duration measurements               | Useful but distinct from run-owned analysis         |
| Experimental analysis    | Genealogy, rank and dynamic-problem analysis                  | Useful research code with unresolved ownership      |
| Concrete core analyzers  | Quality, population, Pareto and diversity analysis            | Uneven quality and coverage                         |

### Observation and analysis must not remain competing user APIs

The current public API offers two ways to react to the same execution boundary. A caller can wrap an algorithm or
operator with `ObserveWith`, or create an analyzer and pass it to `CreateRun`. Sharing the observable wrappers underneath
the analysis implementation would remove some duplicated machinery, but it would not remove this user-facing choice.

The preferred direction is one public analysis system. `CreateRun` installs analyzers before resolving the execution
graph, and analyzers are the ordinary way to inspect execution. The existing observable configurations, public observer
interfaces and `ObserveWith` authoring API should not remain as a parallel feature merely because analysis can implement
those interfaces internally.

Typed observations can still be useful as the data passed to custom analysis code. Internal role-specific trace
recorders can consume them, for example `AlgorithmTraceRecorder` and `CrossoverTraceRecorder`. Those recorders are
implementation details and are not glossary terms. The remaining design question is the smallest custom-analyzer API
that exposes typed observations without recreating a second observer framework under another name.

This direction also separates two performance decisions. Installing no analyzer must leave the ordinary operator path
unchanged. When analysis is installed, observation delivery should still avoid a mandatory heap allocation per operator
call, particularly for evaluators. The mutator observation prototype is useful for settling its data shape, but its
current class representation is not yet a performance commitment.

The crossover spike tests this direction. Crossover traces install an internal `CrossoverAnalysisHook`; the internal
`CrossoverTraceRecorder` receives a typed `CrossoverObservation` directly and does not implement `ICrossoverObserver`.
The former `ObservableCrossover`, `ICrossoverObserver`, action observer and `ObserveWith` API are obsolete. They remain
temporarily so the spike can coexist with existing tests and consumers, but the new trace path does not use them.

Custom crossover trace authoring targets its typed observation directly. For example, a custom
`IMeasurement<CrossoverObservation<...>, TValue>` can analyze offspring and parents while reusing the standard trace
lifecycle, clocks, aggregation and safe reads. The `Trace` lambda overload provides the same runtime-only path without
spelling the long observation type. Persisted or reusable analyzer configuration uses a concrete measurement class. This
needs no public observer or observation-installation API. The custom accumulator design
remains open and is the next useful test case.

The spike keeps `CrossoverObservation` as a regular record. Before this representation is copied to every role, benchmark
the class allocation and garbage-collection cost against a readonly-struct representation, including the copying and
stack costs of the larger value. Optimize from those measurements rather than assuming either representation wins.

### What should be preserved

#### Run ownership

`AlgorithmRun` creates fresh analyzer data when execution starts. Reusing an analyzer configuration therefore does not mix results from separate runs.

#### Stable scope across nested execution

Observation replacements propagate through child execution registries. Analyzer results can span short-lived algorithm and operator execution instances inside a meta-algorithm.

#### Read-only observation

Algorithms and all nine operator roles expose post-operation observer contracts. Observation records data without changing an operation's inputs, outputs or computation.

#### Typed result lookup

`run.GetResult(analyzer)` connects the analyzer configuration to its result type without an untyped public key.

#### Experiment isolation

`TrialAnalyzer` creates a separate analyzer and result for every materialized trial. Typed trial keys remain attached to the collected results.

## Concrete analyzer inventory

### Main package

| Analyzer                               | Observes                    | Result                                                            | Assessment                                                            |
| -------------------------------------- | --------------------------- | ----------------------------------------------------------------- | --------------------------------------------------------------------- |
| `BestMedianWorstAnalysis`              | Algorithms and interceptors | Best, median and worst candidate per observation                  | Best current reference implementation                                 |
| `BestMedianWorstPerEvaluationAnalysis` | Evaluators and interceptors | Best, median and worst candidate with cumulative evaluation count | Useful but retains old collection storage and lacks algorithm anchors |
| `BestQualityAlgorithmAnalysis`         | Evaluators                  | Best objective vector                                             | Misnamed and inconsistent with its documentation                      |

### Experimental package

| Analyzer                              | Observes                                   | Result                                | Assessment                                                                           |
| ------------------------------------- | ------------------------------------------ | ------------------------------------- | ------------------------------------------------------------------------------------ |
| `BestPerEvaluationAnalysis`           | Evaluators                                 | Best candidate improvements by evaluation count | Coherent but exposed under the mismatched factory name `QualityCurve`      |
| `AllPopulationsAnalysis`              | One interceptor                            | Full population history               | Honest but potentially very memory intensive                                         |
| `AllObjectiveVectorsAnalysis`         | Evaluators and optional reset interceptors | Objective-vector collection           | The optional clearing behavior conflicts with the name "All"                         |
| `PopulationSimilarityAnalyzer`        | Interceptors                               | Similarity matrices and aggregate history | Has correctness gaps and no direct tests                                         |
| `ParetoFrontAnalysis`                 | Evaluators                                 | Intended Pareto front                 | Result front is not publicly readable and initial result has the wrong concrete type |
| `HyperVolumeAnalysis`                 | Evaluators                                 | Pareto state plus hypervolume         | Coupled to unsettled multiobjective semantics                                        |
| `GenealogyAnalysis`                   | Crossovers, mutators and interceptors      | Genealogy graph                       | Useful experimental analyzer and a good composition case                             |
| `RankAnalysis`                        | Genealogy hooks and interceptors           | Genealogy graph plus rank history     | Useful composition over `GenealogyAnalysis`                                          |
| `QualityCurvePerEpochAnalysis`        | Dynamic-problem evaluation events          | Best candidate per epoch              | Useful but depends on custom event lifetime                                          |
| `BestBeforeChangePerformanceAnalysis` | Dynamic-problem evaluation events          | Per-change performance and prediction | Useful research analysis with custom lifetime                                        |
| `InvalidPerEpochAnalysis`             | Dynamic-problem evaluation events          | Invalid evaluation counts per epoch   | Small and coherent but tied to the same event model                                  |

### Integration adapters

Python interop has a private `CallbackAnalysis` that derives from `Analyzer<object>` only to invoke a callback at an interceptor. It still requires a placeholder interceptor. This should become a direct observation adapter or use an algorithm anchor after the replacement API is settled.

## Findings

### Observation registration depends on static generic types

`ObservationPlan` keys entries by anchor reference, which is correct. It merges entries only when their complete closed generic entry types match.

Operator roles are contravariant in problem and search-space types. The same operator may therefore be observed legally through two different static interface views. The current plan treats those registrations as incompatible and throws `InvalidOperationException: Observation conflict` even though both refer to the same object and operation boundary.

The public authoring facade contains twenty overloads, two for every observable role. Every new role or anchor kind adds another hand-written pair.

### Unresolved anchors fail silently

An observation anchor is matched by reference. A configuration copied with `with` is a different anchor. An analyzer attached to the old object records nothing when the copy runs.

The obsolete run-level `TrackBestMedianWorst` avoids this for the root algorithm because it reads the anchor from the run. Named inner algorithms and operators still rely on caller-managed identity. The framework does not distinguish an anchor that resolved but had no calls from an anchor that never existed in the execution graph.

### Analyzer run state is mostly mechanical

For ordinary analyzers, `Analyzer<TResult>` creates a private `IAnalyzerRunState<TResult>` wrapper that holds only the analyzer and result references. Mutable analysis data lives in the result object.

Dynamic analysis is the only current implementation that needs custom run-state behavior. It subscribes to `DynamicProblem.OnEvaluation` and implements `IDisposable` so the run can unsubscribe later.

The public abstraction is therefore heavy for normal authoring while its exceptional cleanup behavior remains implicit.

The replacement API removes `IAnalyzerRunState` and `CreateAnalyzerState`. A stateful analyzer registers observations
directly and owns its accumulator. Run completion and cleanup need one explicit lifecycle contract rather than a second
object whose ordinary implementation only forwards to the analyzer. The production spike temporarily returns the analyzer
itself from `CreateAnalyzerState` while the existing analyzer catalog still depends on the old contract.

`DynamicAnalysis` copies the private `RunState` wrapper from `Analyzer<TResult>` verbatim and adds `IDisposable` to it. That duplication is the concrete cost of having no cleanup contract, and it disappears once a registration lifetime exists.

### Cleanup has lifecycle gaps

`AlgorithmRun.Stream()` creates analyzer states and registers observations before the returned stream is enumerated. Analyzer states are disposed only from the asynchronous iterator's `finally` block.

This leaves several failure paths:

1. A caller obtains a stream but never enumerates it.
2. One analyzer subscribes successfully and a later analyzer fails during registration.
3. Observation installation fails after an analyzer acquired a resource.
4. Cleanup depends on an optional `IDisposable` check that the analyzer contracts do not describe.

The run needs one explicit ownership mechanism for subscriptions and other cleanup work acquired during analysis setup.

### Result lifecycle is unclear

The current API allows `GetResult` as soon as execution starts. API usage specs deliberately query a result during streaming.

Most analyzer results are mutable lists or mutable classes. The framework has no general synchronization rule for a callback writing while user code reads. There is also no finalization step that turns an accumulator into an immutable completed result.

Result shapes vary between raw `List<T>`, classes named `State`, classes named `Result`, `QualityCurve` and mutable graphs. Some expose raw mutable collections while others expose read-only interfaces backed by mutable collections.

The redesign must decide whether live inspection remains supported. If it does, it needs an explicitly named snapshot or view API and a concurrency contract. Completed results should not expose mutable implementation collections.

### Attachment behavior is inconsistent

`AlgorithmRun` accepts the same analyzer object more than once and fails later when its reference-keyed state dictionary is built. `ExperimentRun` rejects the same trial analyzer at attachment time.

Both run types should apply one rule and report configuration errors before execution side effects begin.

### Configuration equality and collection ownership drifted

`BestMedianWorstAnalysis` uses `ValueArray` for retained anchor collections and has structural equality tests.

Other analyzer records retain `ImmutableArray` or use arrays in positional record parameters. `ImmutableArray` compares backing storage identity and belongs on execution instances under the repository guidelines. Array-valued record parameters also compare by reference.

Every retained analyzer configuration collection should accept `IReadOnlyList<T>`, snapshot with `ToValueArray()` and expose `ValueArray<T>`.

### Naming and factory coverage have no rule

Configuration types alternate between `Analysis` and `Analyzer`. Results alternate between `State`, `Result`, descriptive names and raw collections. The public construction factory is named `Analyzer` but exposes only part of the catalog.

`ExperimentalAnalyzers.QualityCurve` returns a `BestPerEvaluationAnalysis`, so the factory name and the type it produces do not agree. `AllObjectiveVectorsAnalysis`, `PopulationSimilarityAnalyzer` and `ParetoFrontAnalysis` have no factory entry at all.

Use these names consistently:

1. `...Analyzer` for reusable analyzer configuration.
2. `...Result` for the user-facing completed or snapshot result.
3. Accumulator or run state only for private mutable execution data.

### Concrete correctness defects

#### `ParetoFrontAnalysis`

`CreateInitialResult` returns `HyperVolumeState<T>` even though the analyzer promises `ParetoState<T>`. `ParetoState.Front` is protected, so callers cannot inspect the Pareto front returned by the analyzer. This violates the basic requirement that a public analyzer expose an observable result.

#### `BestQualityAlgorithmAnalysis`

The type observes evaluators rather than an algorithm. Its result contains only an objective vector, while the architecture document says it stores the best evaluated candidate. It also uses the problem's total-order comparer without the fallback used by the best, median and worst analyzers.

#### `PopulationSimilarityAnalyzer`

Empty populations cause aggregate operations on empty sequences. A one-member population divides by zero while calculating average similarity. `StoreHistory = false` clears similarity matrices but continues appending aggregate history. The type has no direct tests.

#### `AllObjectiveVectorsAnalysis`

When reset mode is enabled, interceptors clear the collected vectors. The resulting object is no longer "all objective vectors" and may be empty after the final interception. Split this into an honest current-window analyzer or remove the reset behavior.

### Test coverage is concentrated on the framework

The main framework has useful coverage for typed result lookup, multiple analyzers on one anchor, algorithm and operator anchors, nested meta-algorithms and experiment trial isolation.

`BestMedianWorstAnalysis` has representative API usage coverage. Genealogy has scenario coverage and dynamic analysis has focused experimental tests.

Most other public analyzers have no direct result tests. This includes Pareto front, hypervolume, population similarity, allele frequency, all objective vectors and best quality. Several current defects would be caught by one basic result assertion.

### Direct instrumentation is a separate model

`ObservationCounter` and `ObservationDuration` are caller-owned mutable sinks passed into counting and duration wrappers. They are useful for local instrumentation and execution budgets, but their data does not belong automatically to one run. Reusing a configuration with the same sink can mix measurements across runs.

Keep this model separate and document its external ownership. Do not force these wrappers through the analyzer result system.

## Proposed design

### Keep a small analyzer framework in core

An earlier boundary proposal would have moved the complete run-owned analyzer layer to Experimental. That question is settled the other way: the analyzer framework stays in the main package, and only analyzers whose result, retention or objective semantics are unsettled live in Experimental, with their entry points on `ExperimentalAnalyzers`. The package admission and promotion policy behind that decision is [developer guidelines § 9.6](../docs/contributing/developer-guidelines.md#-96-keep-package-placement-deliberate).

The current recommendation is to keep a small mature analyzer framework in the main package. Run-wide quality analysis is a normal optimization workflow and now appears in the README. Moving the whole concept would make an ordinary feature look provisional.

Keep in core:

- observable algorithm and operator contracts
- observation installation needed by runs
- run-bound analysis and safe snapshot contracts
- `Analyzer.Trace`, common `Track...` shortcuts and analysis-specific factories
- experiment trial integration if its delegate configuration remains acceptable
- a small set of mature general analyzers, starting with best, median and worst quality

Already moved to Experimental and staying there until mature:

- genealogy and rank analysis
- dynamic-problem analysis
- population diversity analysis
- Pareto and hypervolume analysis until the objective model is settled
- research-specific or high-memory trace analyzers without a common user story

The move itself is done. What remains is deciding whether the two weakest main-package analyzers, `BestMedianWorstPerEvaluationAnalysis` and `BestQualityAlgorithmAnalysis`, meet the § 9.6 bar for the main package or should follow them.

### Keep ownership clear without exposing every layer

The implementation still has separate responsibilities:

1. An `Analyzer.Track...` call chooses the analysis behavior, anchors and clocks.
2. `CreateRun` binds the returned stateful analysis exactly once.
3. The bound analysis receives observations and holds mutable work data.
4. The run owns observation installation, registration lifetime and cleanup.
5. Properties expose already published immutable values without allocation. Methods publish immutable snapshots or projections and may allocate.

Do not force ordinary users through separate reusable configuration, attachment handle and result lookup objects. Do not require ordinary analyzer authors to implement a wrapper whose only job is to forward `RegisterObservations`.

### Write the desired API usage spec first

Add a desired-state API usage spec beside the current API specs before changing contracts. It should demonstrate:

1. Creating a quality analysis and passing it directly to `CreateRun` without an interceptor or setup type.
2. Reading the same series against two different clocks without a second analyzer.
3. A scalar reduction, a frequency map and a pairwise aggregation composed from the same parts.
4. An encoding-specific measurement used with a general aggregation.
5. Observing an explicitly named inner algorithm.
6. Observing an operator for data that search states do not contain.
7. A domain moment, using the dynamic epoch.
8. A recorded-on-change series beside a recorded-every-firing one, from the same measurement.
9. A transposed read of a frequency series, one line per key.
10. Reading the same typed analysis during and after execution.
11. Cheap `Latest` and count properties beside allocating snapshot and projection methods.
12. One hand-written analyzer that bypasses the composition layer entirely.
13. One experiment that creates an independent analyzer result per trial.
14. A configuration error for an anchor that never resolves, or the explicit diagnostic model chosen instead.
15. A combination that must not compile, such as an objective-value measurement at a crossover boundary.

This spec is the target for the redesign. Keep the current API specs until the migration is complete so reviewers can compare the two usage models.

### Replace opportunistic disposal with registration lifetime

An earlier draft of this plan proposed a public cleanup contract so that analyzers could register teardown work. That was the wrong fix. The only analyzer that ever needed cleanup was dynamic analysis, and it needed it only because of the event workaround described above. Once the evaluation batch becomes readable state, **no analyzer acquires anything that needs releasing**, so the right move is to remove the concept rather than give it a nicer shape.

What remains is the run's own teardown of what *it* installed, which is internal and invisible to analyzer authors.

Required behavior:

1. Setup is transactional. If one analyzer fails during setup, the run undoes everything it already installed.
2. A stream that is created but never enumerated cannot leak installed observations.
3. Cancellation, early disposal, completion and failure all release exactly once.
4. No analyzer implements disposal, and `state is IDisposable` disappears entirely.
5. `ExperimentRun` gets the same teardown, which it has never had.

The current design fails 1, 2, 3 and 5. `StartExecution` sets `ExecutionStarted = true` before doing any analyzer work, so a failure leaves the run permanently unusable with half-built state; setup runs eagerly in `Stream()` while release happens only in the async iterator's `finally`; and `ExperimentRun` prepares every trial up front with no release path at all.

### Redesign observation registration around typed events

The public authoring API should provide typed event data for each role while the internal plan merges registrations independently of their static generic view.

The exact type shape needs a prototype. The accepted design must satisfy these constraints:

1. Analyzer authors do not spell three otherwise inferable type arguments.
2. Adding an observation role does not require two new overloads in one central facade.
3. Multiple analyzers observing one anchor create one replacement rather than a deep wrapper chain.
4. The same contravariant anchor observed through compatible static types does not conflict.
5. One configuration that genuinely exposes different operation roles can distinguish them.
6. Public callbacks remain strongly typed.
7. The internal implementation may erase callback types after public registration if that is the simplest way to merge compatible observers safely.

Do not introduce a generic operator invocation path. The rejected typed-invocation design in [developer-backlog.md](developer-backlog.md#typed-operator-invocation) remains rejected. Observation plumbing may use internal type erasure because it does not replace role execution contracts.

### Track anchor resolution separately from invocation count

Three situations must stay distinguishable, and today they are not:

1. An anchor that was never part of the execution graph.
2. An anchor that resolved but whose operation was never invoked.
3. An anchor that produced zero items in a valid invocation.

Only the first is an error. The other two are valid empty analyses.

**The preference is to fail as early as possible**, during `CreateRun`, naming the analysis and the unreachable anchor. This is the point where the root algorithm, requested analyses and materialized execution graph first meet.

How much validation can finish before instance resolution remains an implementation question. The registry can at least track whether each installed replacement was consumed by `Resolve`. Replacement lookup walks up the parent chain, so consumption in any child registry counts. `CreateRun` must finish binding and validation before it returns a runnable `AlgorithmRun`.

Any of these satisfies the requirement that the failure is loud. Which one is taken should be decided when the cost of each is known, not now.

What is not open: a resolved anchor with zero invocations must not throw, and silent empty results must not remain the only behavior.

Every analysis names its anchors before run creation. `CreateRun` validates those anchors against the execution graph while binding the analysis.

### Define live reading and publication

#### Settled: the requirement

A published result must not be a moving target. Handing out the live accumulator behind a read-only interface fails this, and the failure is general rather than a threading artifact:

1. `List<T>` invalidates any open enumerator on append, on a single thread with no concurrency involved. It throws whenever an enumeration *spans* an append, which happens as soon as a result is passed into any async pipeline.
2. Without any exception at all, the same result object answers `Count` differently on successive reads, and a deferred LINQ query over it changes its answer. A result that keeps moving cannot be compared, cached, asserted on, or handed to a report.
3. `IReadOnlyList<T>` over a `List<T>` can be downcast and mutated.

Point 2 is the one that matters most, because it is silent.

Live reads are supported. The stateful object returned by `Trace` has one reading API during and after execution:

- Properties such as `Latest`, `SampleCount` and `IsCompleted` return already published values that are safe to share. They do not allocate or perform projections.
- Methods such as `Snapshot`, `By(clock)` and `Transpose` may allocate. Each returns an immutable value that never changes after publication.
- Separate property reads are individually safe but are not transactional. Callers use one snapshot when several values must agree.
- The analysis object never exposes its mutable accumulator through `IReadOnlyList<T>` or a deferred query.

There is no partial result type. Cancellation leaves the analysis readable with whatever it accumulated, and the caller already knows it cancelled.

#### Initial store

Use a plain mutable collection under a lock and copy when a snapshot or projection method is called. This is the simplest implementation with an obvious concurrency contract.

Measured, at one million samples unless stated:

- `List.Add` 1.9 ns; under lock 17.3 ns, so 17 ms per million appends.
- Copying to publish: 1.4 ms for a thousand samples polled a thousand times; 111 ms for a hundred thousand samples polled a thousand times.
- `ImmutableList<T>` is rejected outright: 467 ms append and 1241 MB allocated, against 5.5 ms and 23 MB for an append-only log. Its snapshot is free but it allocates rebalancing nodes on the operation a series performs most.
- Append-only log: 5.5 ms append, 23 MB, constant-time publish. Faster than plain `List<T>`, which pays for doubling copies.

The costs are small enough in realistic ranges that correctness and an obvious implementation matter more than avoiding copies. A spike of the append-only log had a bug in its barrier-sensitive code. Revisit the store only if profiling shows snapshot copying is a real cost.

### Make results immutable at the public boundary

Analyzer callbacks may update private mutable accumulators. Every collection published during or after execution must be an immutable value or snapshot.

Apply these rules:

1. Do not return raw `List<T>`.
2. Snapshot historical collections before publication.
3. Keep mutable graphs private while they are being built, then publish an immutable graph view or value.
4. Use descriptive result entry types instead of public tuples when field meaning matters.
5. Include observation count or other metadata only when it has defined semantics.
6. Do not store reporting concerns such as chart labels or units unless the analyzer owns that meaning.

### Normalize retained analysis settings

Measurements, aggregations, recording policies and other retained settings should follow the same ownership rules as algorithm and operator configuration values.

1. Accept finite ordered anchor collections as `IReadOnlyList<T>`.
2. Snapshot retained collections with `ToValueArray()`.
3. Expose retained collections as `ValueArray<T>`.
4. Keep mutable accumulators out of retained setting values.
5. Use records only where structural equality is the intended contract.
6. Use classes for runtime adapters that hold identity-bearing delegates or external sinks.

### Make composition a supported authoring case

`RankAnalysis` currently calls `GenealogyAnalysis.RegisterObservations` with the outer result's graph. The idea is useful: one analyzer can reuse another analyzer's observation logic while publishing a different result.

The new authoring model should allow reusable observation components without creating a hidden second run-owned result. Add one desired-state spec for this case before migrating genealogy and rank.

### Clarify the boundary with direct callbacks

Keep these choices distinct:

| Need                                                        | Use                                              |
| ----------------------------------------------------------- | ------------------------------------------------ |
| Inspect every yielded search state                          | `Stream()`                                       |
| Run one local callback at an algorithm or operator boundary | Observable wrapper or direct observation adapter |
| Collect one coherent typed analysis across a run            | A run-bound object returned by a `Track...` method |
| Supply an externally owned counter or timer                 | Instrumentation wrapper                          |
| Collect independent results for experiment trials           | Trial analyzer integration                       |

The Python callback adapter should use the direct callback path. It should not create `Analyzer<object>` unless Python needs a run-owned result.

## Concrete migration decisions

### Use best, median and worst quality as the reference

Migrate this analyzer first because its common run-level usage is already documented and tested.

The replacement should:

1. Anchor on the run's root algorithm through a convenience method.
2. Support named inner algorithms and exact operator boundaries.
3. Publish an immutable quality history.
4. Use the problem's defined total order and preserve the documented lexicographic fallback when none exists.
5. Reject an empty observed population with a clear error.
6. Preserve structural equality for retained anchor collections if anchors remain part of analyzer configuration.

### Collapse the evaluation-count analyzers into the moment model

`BestMedianWorstAnalysis`, `BestMedianWorstPerEvaluationAnalysis`, `BestPerEvaluationAnalysis` and `BestQualityAlgorithmAnalysis` are four types for what the composition model treats as at most two configurations. The first two differ only in which clock they record against, which moments remove entirely. The last two differ only in aggregation, running best against best, median and worst.

Do not migrate these four types. Replace them with the composed form and keep a convenience entry point for the dominant case so that `TraceBestMedianWorst` stays a single line.

The one behavior worth carrying over deliberately is the retention difference: recording only on improvement produces a much smaller series than recording at every boundary. That is a property of the aggregation and recording policy, not a reason for a separate analyzer type.

### Split full-history and current-window collection

`AllObjectiveVectorsAnalysis` should not switch between full history and a clearing window through one boolean.

Either provide two explicit analyzers or keep only the use case with a demonstrated consumer. Apply the same standard to full population capture. High-memory analyzers must say what they retain and should not be the default recommendation.

### Remove analyzers that nothing consumes

`AllObjectiveVectorsAnalysis` has no reference anywhere in source, tests, examples or documentation. `AllPopulationsAnalysis`, `InvalidPerEpochAnalysis` and `RankAnalysis` have no direct result test. Delete an analyzer with no consumer and no test rather than migrating it, and add a representative result test for anything retained.

### Repair population similarity before retaining it

If retained, define behavior for populations of size zero and one, clarify whether diagonal similarity participates and make `StoreHistory` apply to every history collection consistently. Add focused unit tests for each case and for invalid similarity values.

Keep it Experimental until those semantics are settled.

### Defer Pareto and hypervolume promotion

Fix the inaccessible Pareto result and wrong initial result immediately if the types remain public during migration. Do not finalize their replacement API until the objective-system rework defines dominance, objective directions and total ordering clearly.

These analyzers should remain Experimental during that work.

### Rework dynamic analysis subscriptions

`DynamicProblem.Evaluate` appends each evaluation with its timing to a log. The problem registers *itself* as the evaluator observer, and in `AfterEvaluation` raises `OnEvaluation` with the sorted log and then clears it. An analyzer's only way in is to be subscribed at that instant, which is why `DynamicAnalysis` needs a custom run state and `IDisposable`.

The event is therefore not a design choice. It is a workaround for the log being destroyed immediately after it is raised.

The replacement makes the batch **readable state on the problem**, cleared lazily at the start of the next batch rather than immediately after firing. Each analyzer then registers an ordinary evaluator observation and reads it. Any number of analyzers can read the same batch, there is no ordering dependence between them, and there is no subscription.

The problem keeps its own observer registration for `UpdatePolicy` work, which is genuine problem behavior unrelated to analysis. `EvaluationTiming` already carries the epoch and validity the analyzers need, subject to the rework below. This also resolves the analysis half of the existing `// ToDo` on `DynamicProblem`, which questions whether a problem should be an observer at all.

The consequence is larger than dynamic analysis. **No analyzer needs cleanup**, which is why the cleanup contract is removed rather than redesigned.

### Dynamic analysis runs on the new model, not beside it

Removing the subscription is necessary but not sufficient. The dynamic analyzers must be expressible in the composition model, otherwise they remain a parallel system with a tidier teardown, which is the same hack in better clothing.

Checked against the three of them:

| Analyzer | Expression on the new model |
| --- | --- |
| `QualityCurvePerEpochAnalysis` | quality measurement, best aggregation, epoch clock |
| `InvalidPerEpochAnalysis` | validity measurement, counting aggregation, epoch clock |
| `BestBeforeChangePerformanceAnalysis` | per-epoch best series plus an online curve model fitted across firings |

The first two are ordinary compositions. The third is not a plain series: it maintains a fitted model and derives a prediction from it. That is the second accumulator besides lineage, which is useful evidence that the accumulator is the right extension point rather than an abstraction invented for one case.

Two obligations follow. The epoch must be a moment clock contributed by the problem area that owns it, not a bespoke event channel. And no dynamic analyzer may implement disposal, override analyzer state creation, or otherwise need machinery the other analyzers do not.

### Rework `EvaluationTiming` rather than bend the model around it

`EvaluationTiming(long EpochCount, int Epoch, bool Valid)` is what made dynamic analysis look like it needed several samples per firing. It conflates two unrelated things.

`EpochCount` is a free-running counter incremented once per evaluation. That is an evaluation index, and an explicitly selected evaluator clock supersedes it entirely. It is not an epoch and should not be named as one.

`Epoch` is the environment version, and it is the only genuine domain clock here. The important observation is that it is **already stable within a firing** for two of the three update policies: `ResolvePendingUpdates` runs at `AfterEvaluation` or `AfterInterception` boundaries, so every candidate in a batch was evaluated against the same environment even while the free-running counter ticked over mid-batch. Tagging those evaluations with different epoch numbers is therefore already misleading, independently of analysis.

The rework is to record the environment version an evaluation was actually made against, and to drop the redundant counter. One firing then carries one environment version and the model holds without relaxation.

`UpdatePolicy.Asynchronous` is the real exception, because it resolves updates inside `Evaluate` and the environment can genuinely change mid-batch. Three ways out, to be decided when the dynamic problems are reworked: take the environment version at the start of a firing and accept the approximation, declare that asynchronous updates do not support epoch-clockd series at evaluator anchors, or reconsider the policy. This is a small decision and should not hold up the model.

`DynamicProblem` is experimental, so changing it is preferable to distorting a contract the whole library depends on.

## Implementation sequence

### Phase 1: Lock the desired usage

1. Settle the composition vocabulary. Confirm measurement, aggregation, clock, moment, firing, sample, series and recording policy as glossary terms. No clock is included by default.
2. Settle which measurements, aggregations, recording policies and accumulators ship, and in which package each lives.
3. Pass each role's typed observation directly to its measurements. Add one role-specific `Trace` overload to establish
   each anchor-to-observation mapping. Do not reintroduce a public supply hierarchy.
4. Implement typed clocks over existing observers, with explicit source selection and typed `By(source)` reads.
5. Add desired-state API usage specs beside the current analysis specs, covering at minimum a scalar reduction, a frequency map, a pairwise aggregation, a domain moment and one hand-written analyzer that bypasses the composition layer.
6. Confirm the live reading API: cheap immutable properties, allocating snapshot and projection methods, and a locked mutable accumulator for the first implementation.
7. Confirm that unresolved anchors fail loudly, and defer the point of failure to implementation.
8. Decide whether `BestMedianWorstPerEvaluationAnalysis` and `BestQualityAlgorithmAnalysis` stay in the main package. The rest of the boundary is already executed: the main package now holds only those two and `BestMedianWorstAnalysis`.
9. Record the accepted public naming scheme.

### Phase 2: Replace lifecycle and observation internals

1. Introduce the explicit registration lifetime.
2. Make analyzer setup transactional.
3. Replace static-type-dependent observation merging.
4. Track whether each anchor resolves.
5. Preserve replacement inheritance through child registries.
6. Add tests for stream creation without enumeration, setup failure, cancellation, early disposal and normal completion.

### Phase 3: Replace the analyzer authoring contract

1. Remove the mechanical run-state wrapper from the ordinary authoring path.
2. Keep a deliberate advanced path for custom lifetime behavior if still needed.
3. Add typed observation event data and inference-friendly registration.
4. Add custom analyzer specs for one anchor, multiple roles and composed observation logic.
5. Remove the duplicated `RunState` wrapper from `DynamicAnalysis` once the registration lifetime replaces it.

### Phase 4: Build the composition layer

1. Introduce typed clocks, immutable boundary contexts and moments containing only selected clocks.
2. Introduce the stateful run-bound trace, its locked accumulator and its immutable snapshot and projection values.
3. Introduce the measurement, aggregation and recording contracts used directly by `Analyzer.Trace`.
4. Support aggregation within a firing and across firings as separate choices.
5. Ship the general measurement and aggregation catalog and the recording policies.
6. Ship the encoding-specific measurements next to their encodings.
7. Provide typed `By(source)` trace views and transposition for keyed values.
8. Provide `Analyzer.TraceBestMedianWorst` as a convenience method over `Analyzer.Trace`.
9. Add `CreateRun(..., params IAnalyzer[] analyzers)` as the only analyzer-binding boundary and keep attachment methods off `AlgorithmRun`.

### Phase 5: Migrate analyzers onto the model

1. Replace the four overlapping quality analyzers with `Trace` compositions and convenience methods.
2. Rework `EvaluationTiming` so the epoch is an environment version and the redundant evaluation counter is dropped, then move dynamic per-epoch analysis onto a domain moment as ordinary compositions with no disposal and no custom analyzer state.
3. Express Pareto front and hypervolume as aggregations once the objective-system work allows.
4. Migrate experiment trial integration.
5. Remodel genealogy as a lineage accumulator with several bound anchors, keeping its current identity comparer until provenance is designed.
6. Update README and user documentation only after the new path is stable.

### Phase 6: Triage specialized analyzers

1. Repair population similarity and keep it Experimental until tested.
2. Delete `AllObjectiveVectorsAnalysis` and any other analyzer left without a consumer or a test.
3. Migrate genealogy and rank using the supported composition pattern.
4. Confirm no dynamic analyzer retains bespoke machinery once it is on the model.
5. Keep Pareto and hypervolume analysis aligned with the objective-system branch.
6. Replace the Python callback analyzer with direct observation.
7. Rename `ExperimentalAnalyzers.QualityCurve` to agree with the analyzer it creates.

### Phase 7: Documentation and namespace work

1. Rewrite the analyzer architecture page to describe the implemented contracts.
2. Update the glossary only if settled terminology changed.
3. Update observability, experiment and meta-algorithm guides.
4. Remove current-state API specs after equivalent replacement coverage exists.
5. Reconcile any analysis-specific namespace changes required by the accepted replacement API without reopening the broader folder restructuring.

## Validation plan

### Contract and lifecycle tests

Cover:

1. One fresh result per analyzer and run.
2. Rejection when the same analysis is supplied twice or reused for another run.
3. Result access before start, during execution, after completion, after cancellation and after failure.
4. Cleanup when a stream is never enumerated.
5. Cleanup after partial analyzer setup failure.
6. Cleanup after observation installation failure.
7. Cleanup on early stream disposal and cancellation.
8. One cleanup action executed exactly once.

### Observation tests

Cover:

1. Multiple analyzers on one anchor.
2. One anchor observed through compatible contravariant static types.
3. Algorithm and operator anchors in one run.
4. An unresolved anchor.
5. A resolved anchor with zero invocations.
6. Root algorithm helpers that cannot hold a stale copy.
7. Named inner algorithm observation.
8. Child registry recreation and reuse.
9. A meta-algorithm that bypasses `Resolve`, if the existing authoring guard remains documentation only.

### Result tests

Every public analyzer retained after the rework needs:

1. One representative result assertion.
2. Empty-input behavior where applicable.
3. Single-item behavior where applicable.
4. Multiobjective behavior where applicable.
5. Proof that published history cannot be mutated through the public API.
6. Structural equality tests for configuration collections.
7. A memory-retention test or documented bound for analyzers that store full populations or evaluation histories.

### Project validation

During implementation, run the narrowest relevant tests first. Before merging the branch, run:

1. `HeuristicLib.Tests`
2. `HeuristicLib.Tests.ApiUsageSpecs`
3. `HeuristicLib.Tests.Experimental`
4. Relevant genealogy, dynamic-problem and meta-algorithm scenarios
5. Release build
6. Whitespace, style and analyzer verification
7. Documentation build
8. Package creation for main, Experimental and Python interop

## Acceptance criteria

The rework is complete when:

1. The mainstream catalog is expressed as combinations of anchor, measurement, aggregation, moment and recording policy, and adding a metric, an axis or a retention rule does not add an analyzer type.
2. One series can be read against every clock selected when it was attached, without rerunning.
3. A measurement and anchor combination without a matching `Trace` overload is rejected at compile time.
4. Every `Trace` composition produces a trace, and a single value is the last entry of one rather than a second result shape.
5. A keyed series such as a frequency map can be read transposed without a second accumulator.
6. Recording policy is configurable, so an expensive aggregation can be sampled rather than labelled a memory hazard.
7. Analysis that is not a series is a different accumulator over the same anchors, firings and moments, demonstrated by genealogy remodelled onto the new system.
8. Each `Track...` call returns an independent stateful analysis object that `CreateRun` binds once to that run and its chosen anchors.
9. The desired API usage specs are the normal documented path.
10. Analyzer setup cannot leak registrations on any tested exit path.
11. Observation merging no longer depends on compatible callers choosing identical closed generic types.
12. Unresolved anchors fail loudly and cannot masquerade silently as a successful empty analysis.
13. No analyzer implements disposal or overrides analyzer state creation, dynamic analysis included.
14. Run-bound analyses never expose mutable implementation collections, during or after execution.
15. Properties return safe already published values without allocation; snapshot and projection methods return immutable values that remain stable while execution continues.
16. Configuration records use structural collection values consistently.
17. Every retained public analyzer has focused tests and an observable result.
18. `ParetoFrontAnalysis` no longer returns an unreadable front, or the type is removed.
19. Analyzer, result and factory naming follows one recorded rule.
20. Experiment trial analysis preserves independent typed results.
21. Nested and recreated execution registries preserve run-wide analysis.
22. Python interop no longer uses `Analyzer<object>` solely as a callback carrier.
23. The analyzer architecture document describes the implementation rather than an earlier design.
24. The rework preserves the accepted broad namespace layout and limits any further moves to analysis concepts that genuinely changed.
25. `AlgorithmRun` exposes no way to attach analysis after creation.
26. The analysis overload of `CreateRun` accepts `params IAnalyzer[]`, and the overload without analyzers remains available.

## Decisions and trade-offs

Settled decisions, each with what was rejected and why. Reopen one only with new evidence.

### Model

| Decision | Rejected alternative | Why |
| --- | --- | --- |
| Explicitly selected clocks are recorded together on every sample | One analyzer per axis; attaching every known clock automatically | One series can support several axes, but observing an unused source has a cost and nested algorithms or evaluators make automatic selection ambiguous |
| The mainstream catalog is combinations of anchor, measurement, aggregation, moment and recording | A hand-written type per metric | Adding a metric or an axis should not add a type |
| A series is one accumulator; the accumulator is the extension point | A series system with a hand-written escape hatch beside it | Genealogy decomposes into the same anchors, firings and moments and differs only in the fold. Framing it as a bypass would have duplicated the plumbing |
| Measurements receive typed observations through role-specific overloads | A public supply-interface hierarchy or binding-selected intermediate collections | `ICandidateReadings`, `IObjectiveReadings`, `IEvaluatedPopulation` and `IDerivation` were type-system adapters rather than useful domain concepts. Binding-selected collections hid the actual anchor-to-observation relationship and made identity measurements appear necessary |
| Crossover parent and offspring data stays on `CrossoverObservation` | A separate flattened derivation carrier | The observation already contains both values and preserves parent pairs. The extra carrier duplicated data and lost structure |
| Generations are recovered by grouping moments with a selected iteration source | A boundary signal from an interceptor | The source identifies which algorithm's iterations define generations. Anchoring an interceptor to learn the time is a workaround for a missing observation |
| Recording policy is a configured choice | Retention baked into which analyzer type was picked | It is what makes an n-squared aggregation affordable, and it is the same one-type-per-variation mistake as the axis problem |
| Transposition is a read-time view | A second accumulator shape for keyed values | Same information, different reading. Storing both would double the state |

### Surface

| Decision | Rejected alternative | Why |
| --- | --- | --- |
| `Analyzer.Trace` creates the stateful analyzer and `CreateRun` binds it | Attaching through `AlgorithmRun`; a universal definition followed by result lookup | The caller declares anchors and clocks before execution and keeps the typed object used for live reads. `AlgorithmRun` cannot enter an unsupported late-attachment state |
| `CreateRun` has a `params IAnalyzer[]` overload | `AlgorithmRunSetup`; storing analyzers on every algorithm configuration; an optional collection parameter | Run creation is already where configuration and runtime dependencies become one execution. An overload keeps analysis visible without adding another lifecycle type or polluting algorithm configurations |
| An analyzer is used with one run by convention | Global ownership tracking and runtime reuse checks | The ordinary API does not need machinery for accidental cross-run sharing. Intentional shared accumulation can define its own semantics later |
| A `Track...` call returns a stateful analysis with typed reads | An inert handle plus `run.GetResult(handle)` | The object can collect data after binding and expose safe reads directly. Separate calls create independent analyses |
| One `Track...` call with several anchors feeds one analysis | A separate keyword for multi-anchor binding | Genealogy needs several derivation anchors folding into one graph. Separate calls remain the clear way to create separate analyses |
| Live reads use cheap properties and allocating methods | Completed-only reads; exposing the live collection | Streaming users need current analysis data. `Latest` can return an immutable sample without allocation, while `Snapshot` and projections make publication cost explicit and never expose moving storage |
| The initial store is a locked mutable collection copied on publication | An append-only lock-free log | The measured copy costs are acceptable, and ordinary locking has a much smaller correctness risk. Revisit only with profiling evidence |
| No partial result type | A distinct type for cancelled or failed runs | The caller already knows it cancelled |
| `ImmutableList<T>` is rejected as the series store | using it for free snapshots | Measured: 467 ms append and 1241 MB at a million samples, against 5.5 ms and 23 MB for an append-only log. It allocates on the operation a series performs most. Which store replaces it is still open |
| `Analyzer.Trace` is the common composition factory | Keeping `WithAnalyzer`; attaching through the runnable run | It returns the object that will hold one run's collected data. Common shortcuts such as `Analyzer.TraceBestMedianWorst` preconfigure it |
| A typed clock-source object is also the `By(source)` read key | Dedicated methods; a clock enum | The source preserves the x type, distinguishes two sources of the same kind and makes unavailable clocks explicit |
| Clock observers update before analysis observers capture moments | Depending on registration order | A source and an analysis may observe the same boundary. Correct moments cannot depend on which attachment happened first |

### Lifecycle

| Decision | Rejected alternative | Why |
| --- | --- | --- |
| No analyzer cleanup contract at all | A public registration lifetime with an `OnRelease` hook | The only analyzer needing cleanup was dynamic analysis, and only because of an event workaround. Removing the cause removes the need. The run's teardown of what it installed stays internal |
| Dynamic evaluation batches become readable state on the problem | An event, or a dedicated observation source | The event exists only because the log is cleared immediately after firing. Readable state lets any number of analyzers read the same batch with no ordering dependence and no subscription |
| Dynamic analyzers are expressed on the composition model, not merely detached from the event | Removing disposal but leaving them a parallel system | A tidier teardown around the same bespoke machinery is the same hack in better clothing. No dynamic analyzer may implement disposal or override analyzer state creation |
| One firing yields at most one sample | Letting an aggregation partition readings and emit several | Nothing needs it. Keying within a sample is what frequency maps do, and transposed views read them back. Partitioning would be a second way to express keyed data with a second shape to store |
| Rework `EvaluationTiming` rather than relax the model for it | Multi-sample firings to accommodate per-evaluation epochs | It conflates a free-running evaluation counter with the environment version. An evaluator clock owns the count, and the environment version is already stable within a firing for two of three update policies. `DynamicProblem` is experimental; the sample model is not |
| No run lifecycle state machine for reading analysis | An execution state enum | The same properties and snapshot methods work during and after execution. This avoids colliding with the separate run-lifecycle backlog item |
| Unresolved anchors fail loudly; how early is an open implementation choice | Silent empty results; a queryable diagnostic | A silently empty curve reads as "the algorithm did not improve" rather than "your instrumentation is detached". Earlier is better, but `Track`-time detection needs a configuration graph walk that does not exist, so the point of failure is decided when its cost is known |

### Scope

| Decision | Rejected alternative | Why |
| --- | --- | --- |
| Provenance is deferred, genealogy is remodelled now | Designing candidate identity in this rework; or leaving genealogy untouched | The plumbing half is covered by the accumulator model. The identity half needs candidate identity across operator boundaries, which does not exist. Genealogy keeps an explicit comparer until then |
| Measured data must not steer the search | Adaptive operator selection as an analyzer | An adaptive operator reweighting its children from measured success is control, not analysis. It belongs to the caller-owned instrumentation model, which policies such as terminators already read live |
| Caller-owned instrumentation stays separate | Folding counters and timers into analyzer results | Their data is not owned by one run. A series may sample such a sink, but the sink remains the source of truth |

## Sequencing with other work

Both prerequisites are cleared. The mechanical namespace and folder restructuring is complete, and the release gate no longer applies. Do this rework on its own branch.

The namespace work may have to move or rename some analysis files a second time if the replacement API changes their concepts. That cost is acceptable because the namespace work was bounded and release-facing, while the analysis redesign needs more time.

Clock with these backlog items:

- the `AlgorithmRun` and `ExperimentRun` lifecycle backlog item
- the objective-system backlog item before finalizing Pareto and hypervolume analysis

The algorithm observation anchor this rework builds on is already implemented and documented in [observability and analysis](../docs/guide/execution/observability-and-analysis.md). One defect it left behind is in scope here: only `BestMedianWorstAnalysis` was converted off a `params IInterceptor<…>[]` constructor to `ValueArray`, so the remaining interceptor-anchored analyses still take reference equality over their anchors.

Keep the implementation on its own branch. Avoid mixing it with namespace moves, broad glossary renaming or unrelated algorithm work.

## Source material

- [Analysis API sketch](analysis-api-sketch.md)
- [Analyzer architecture](../docs/contributing/architecture/analyzers.md)
- [Observability and analysis](../docs/guide/execution/observability-and-analysis.md)
- [Configuration and execution ownership](../docs/contributing/developer-guidelines.md#-4-configuration-and-execution-ownership)
- [Collection ownership](../docs/contributing/developer-guidelines.md#-5-immutability-and-collection-ownership)
- [Analyzer glossary entries](../docs/guide/glossary.md#analyzer)
- [Developer backlog](developer-backlog.md)
