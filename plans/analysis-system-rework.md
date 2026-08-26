# Analysis system rework

## Summary

HeuristicLib has a sound central idea for analysis: analysis data belongs to one run and observations attach to algorithm or operator boundaries through the execution registry. The current implementation proves that model with algorithm quality curves, operator traces, nested meta-algorithms and experiment trials.

Two separate problems sit on top of that idea.

The first is mechanical. Observation merging depends on the caller's closed generic types, analyzer cleanup has lifecycle gaps, live results have no general concurrency contract and several public analyzers are incomplete or untested. Result naming, collection ownership and factory coverage also vary by type.

The second is conceptual, and it matters more. Every analyzer today is a hand-written type that hard-codes what it measures, how it summarizes, and which coordinate it records against. Because the coordinate is baked in, the same metric needs a new analyzer per axis, which is why `BestMedianWorstAnalysis` and `BestMedianWorstPerEvaluationAnalysis` both exist. HeuristicLab reached the same state from the same cause and shipped `BestAverageWorstQualityAnalyzer`, `QualityPerEvaluationsAnalyzer` and `QualityPerClockAnalyzer` as three types for one metric.

Almost the whole useful catalog is one mechanism with four independent choices: **where** to observe, **what** to measure, **how** to summarize it, and **which coordinates** to record. Quality curves, variable frequencies, tree bloat, diversity, distribution histograms and dynamic per-epoch analysis are all combinations of those four. This plan therefore does two things: it repairs the run-owned framework, and it adds `TrackSeries` as the direct composition and attachment path for most of the catalog.

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

Ignoring the domain and looking only at how observations turn into data, nearly every entry above is the same act repeated: **at a defined boundary, read something, summarize it, and record the summary against the coordinates of that moment.**

The entries differ only in which of four choices they make.

1. **Where** the reading happens. The end of an iteration, an evaluation batch, a specific operator, a named inner algorithm.
2. **What** is read. An objective value, a tree length, the set of variables a candidate uses, a pairwise similarity, a validity flag.
3. **How** many readings become one recorded value. Best, median, worst, mean, a histogram, a frequency map, a matrix, a non-dominated set, or no reduction at all.
4. **Which coordinates** the recorded value is filed under. Iteration, cumulative evaluations, elapsed time, epoch.

Choice 4 is the one the current design gets wrong, and the mistake is worth stating precisely. These coordinates are not competing x-axes to pick between. When a reading is taken at the end of iteration 7, that reading is *simultaneously* at iteration 7, at evaluation 700, at 1.4 seconds and at epoch 2. They are coordinates of one event. Treating them as alternatives is what forces one analyzer per axis and makes two series impossible to plot against each other.

### What this shape does not cover

Two classes in the catalog are not this shape, and saying so is part of the requirement.

**Provenance** — genealogy, lineage, operator success rates — needs to correlate events *across* operator boundaries and therefore needs candidate identity that survives the pipeline. `GenealogyAnalysis` carries an `IEqualityComparer<TCandidate>` precisely because no such identity exists and it must reconstruct identity by value. Operator success rate additionally needs the dispatch decision inside a `ChooseOne*` operator to become visible, which it currently is not.

Separate the two halves of that problem. The *plumbing* half — several anchors of different roles contributing to one non-series store — is covered by the accumulator model described below, and genealogy should be remodelled onto it rather than left beside it. The *identity* half is not covered and is deferred: candidate identity, a first-class lineage model and operator attribution are revisited as their own piece of work once the measurement model is in place. Genealogy must keep working throughout, and nothing in this rework may foreclose provenance later.

**Budget and cost** are already served by the caller-owned instrumentation sinks `ObservationCounter` and `ObservationDuration`, which are read live by policies such as terminators. That model stays separate. A series analyzer may sample such a sink at each coordinate, but the sink remains the source of truth.

**Terminal artifacts** are a degenerate series of length one and need no separate mechanism.

## The analysis model

### Components

| Component            | Meaning                                                                          |
| -------------------- | -------------------------------------------------------------------------------- |
| **Anchor**           | The algorithm or operator boundary where readings are taken. Already exists. An analyzer may bind several, and anchors are classified by what they supply rather than by role. |
| **Supply**           | What a firing at an anchor makes available. A small closed set, unlike the open set of operator roles. |
| **Measurement**      | What is read at that boundary. A candidate property, a pairwise property, a population property or an external value. |
| **Aggregation**      | How the readings taken at one firing become a single value, and how values carry across firings. |
| **Stamp**            | The coordinates of that firing.                                                   |
| **Recording policy** | Whether a given firing produces a contribution at all.                            |
| **Accumulator**      | How contributions fold into the analyzer's data. A series is one accumulator; a lineage graph is another. |
| **Sample**           | One stamp paired with one aggregated value.                                       |
| **Series**           | The ordered samples an analyzer produced during a run. The common accumulator and the common result. |
| **Run-bound analysis** | The stateful object returned by a `Track...` method. It collects one run's data and provides safe live reads. |

A stamp field is itself a measurement of the run — it is simply one that gets indexed by rather than plotted. That keeps extensibility to a single story: anything readable at a boundary can serve either as a value or as a coordinate. Dynamic problems already have a domain stamp in `EvaluationTiming`, though it needs the rework described below before it is a usable coordinate.

### Repeated firing is the normal case

An anchor fires many times in a run: once per generation, once per evaluation batch, once per operator call. The model must state what happens across those firings, because that is where the result shape is actually decided.

Each `TrackSeries` firing runs the same cycle: measure, aggregate, stamp, and then decide whether to record. Its collected data is therefore always a series. A best-median-worst aggregation gives a series of triples; a histogram aggregation gives a series of histograms; a pairwise aggregation gives a series of matrices. There is no separate series mechanism for a single value. A single value is a series of length one, or the last sample of a longer one. HeuristicLab confirms the shape by having needed `DataTableHistory`, `HeatMapHistory` and `ScatterPlotHistory` as three distinct types for exactly this.

#### A firing yields at most one sample

One firing, one stamp, one sample. Zero when the recording policy declines, never more.

This was challenged by dynamic problems, where `EvaluationTiming` is attached per evaluation so one evaluator firing appears to span several epochs. The conclusion is that the model is right and `DynamicProblem` is wrong, which is addressed below.

The alternative was to let an aggregation partition its readings by a coordinate and emit one sample per partition. That is rejected, because nothing needs it. Keying *within* a sample is what frequency maps already do, and transposed views read those back as one line per key. Partitioning into several samples would be a second way to express keyed data, with a second shape to store and read.

#### Aggregation works on two axes

Reducing the readings of one firing is the common case, but some analyses carry state between firings.

| Axis         | Meaning                                          | Examples                                    |
| ------------ | ------------------------------------------------ | ------------------------------------------- |
| Within firing | Many readings at one boundary visit become one value | best, median, worst; histogram; frequency map; similarity matrix |
| Across firings | Values carry state from previous firings         | best so far, cumulative count, running mean |

"Best so far" is a within-firing best followed by an across-firing running best. Keeping the two axes separate is what stops that from becoming its own analyzer type.

#### Recording policy decides what the series costs

Not every firing should produce a sample, and the difference is currently hard-coded into separate types: `BestMedianWorstAnalysis` records at every firing while `BestPerEvaluationAnalysis` records only when the best improves. That is the same one-type-per-variation mistake as the coordinate problem, so recording belongs in the model as its own choice.

At minimum the model must support recording at every firing, recording only when the value changes, recording every nth firing, and recording only the final firing.

This is also what makes expensive aggregations affordable. A similarity matrix per generation costs on the order of generations times population squared. Recording every tenth firing is the difference between a usable analyzer and one that must be labelled a memory hazard. The existing `StoreHistory` flag on `PopulationSimilarityAnalyzer` is a broken attempt at this concept and should be replaced by it rather than repaired.

#### Transposition is a read concern, not a storage concern

A series of frequency maps and a map of series carry the same information, but users want different ones. Nobody plots "the variable frequency map of generation 12"; they plot one line per variable across generations. HeuristicLab's variable frequency analyzer produces the transposed form directly, which is why that analyzer could not share machinery with the quality analyzers.

Store the series as ordered samples. Provide transposed views over it for keyed values such as frequency maps and per-dimension measurements. Do not introduce a second accumulator shape to serve the transposed reading.

### The catalog as combinations

| User need                          | Anchor            | Measurement           | Aggregation          | Stamp                     |
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

The first two rows of the main table are the point of the whole exercise. They are the *same configuration*; only the coordinate read at plot time differs. Under the current design they are two separate analyzer types, and consolidating them is listed below as a migration decision that this model dissolves entirely.

Rows four, twelve and thirteen show that "aggregation" is not limited to descriptive statistics — carrying a running best, a non-dominated set or a scalar computed from the whole reading set are the same slot.

### What is shipped

The library supplies the combinable pieces; users combine them, and only write an analyzer when they need something the model cannot express.

**Measurements.** General ones in the main package: objective value, objective vector, objective component, feasibility. Encoding-specific ones next to their encodings: symbolic expression tree length, depth, variable set, symbol set; permutation edges and positions; real-vector per-dimension values. Problem-specific ones supplied by the user.

**Aggregations, within a firing.** Best, median, worst, mean, standard deviation, min and max; count and count-matching; histogram; frequency map; distinct count; pairwise matrix; non-dominated set; and an identity aggregation that keeps every reading for callers who genuinely want the full population.

**Aggregations, across firings.** Running best, running worst, cumulative sum and cumulative count.

**Stamps.** Iteration, cumulative evaluations and elapsed time as the default set, with domain stamps such as the dynamic epoch supplied by the problem area that owns them. Elapsed time is opt-in because a clock read per evaluation is not free.

**Recording policies.** Every firing, on change, every nth firing, and final only.

**Views over a series.** Reading against a chosen coordinate, and transposition for keyed values such as frequency maps.

### Anchors are classified by supply, not by role

An anchor determines what can be measured there. A crossover has produced offspring, but nothing has evaluated them, so an objective value cannot be read at that boundary.

The obvious way to express that is a rule per operator role. It is the wrong way, because **the set of operator roles is open**. A user can introduce a role, so the analysis system cannot enumerate roles and must not name one anywhere in its API.

What is closed, and small, is the set of things a firing can supply.

| Supply               | Carries                                            | Provided by                                                    |
| -------------------- | ---------------------------------------------------- | -------------------------------------------------------------- |
| Objective readings   | objective vectors only                             | anything that has evaluated something                          |
| Candidate readings   | candidates only                                    | anything that has candidates                                   |
| Context              | search space and problem                           | every anchor                                                   |
| Evaluated population | candidates, objective vectors, context             | algorithm, interceptor, selector, replacer                     |
| Candidate batch      | candidates, context                                | creator and any role producing candidates                      |
| Derivation           | candidates, context, and the inputs they came from | crossover, mutator, refiner, creator, any user role of that shape |
| State transition     | one search state gave way to the next              | interceptor, algorithm                                         |

The first three are narrow and compose into the rest. A measurement declares the **narrowest** supply it needs, which is what keeps valid combinations wide rather than narrow: quality needs objective readings only, so it binds to an algorithm and an evaluator alike.

Search space and problem travel together in one context supply, as they already do on operator roles. Omitting them was tried and rejected because a problem-dependent measurement must read the problem belonging to the observed run.

A measurement or contribution declares the supply it requires. Binding succeeds when the anchor provides it and fails otherwise, before the run starts.

An analysis may consume several supplies and may treat some as optional. Optional does not mean ignorable: **absence has defined behavior that is part of what the analysis means.** Lineage requires derivations, because without them there is nothing to record, and optionally consumes state transitions, which separate generations. Given no state transition it produces a valid graph with every node in one generation. Given no creator it produces a valid graph whose roots have no attributed origin, which is legitimate when only crossover is of interest and is a strong hint of a misconfiguration otherwise, so the result must make unattributed roots visible rather than throwing or hiding them.

That is different from an anchor that was named but never reached, which stays a configuration error.

This single mechanism settles three separate problems.

1. An unusable combination such as an objective value at a crossover is rejected by the type system rather than by a run-time check.
2. Multiple anchors bind to one analyzer without the API naming roles, because qualification is by supply. Genealogy binds creation, crossover and mutation because all three supply derivations, creation being one with no inputs.
3. A user-defined operator role participates in analysis by declaring its supply, with no change to the analysis system and no per-role registration.

The exact taxonomy is not final. The supplies above cover the current roles and the known use cases, but whether terminators and replacers need their own, and whether derivation is one supply or splits by arity, should be settled against real measurements rather than in the abstract.

This design was verified against a compiling spike rather than argued: every valid combination compiles with no explicit type arguments, and both invalid combinations tested are compile errors. Variance is what makes it work, and it is what allows the shipped catalog to be non-generic. See [the API sketch](analysis-api-sketch.md) for the observed results.

### The stamp must be readable at every anchor firing

A stamp is only useful if it is available wherever a firing happens, including deep inside an iteration at an operator boundary. This is a requirement on the stamp, not on any individual analyzer.

The information already exists. `IterativeAlgorithm.RunStreamingAsync` drives its loop over `yieldedStateCount`, which is the iteration number, and every operator call for that iteration happens inside `ExecuteStep`, lexically nested within that loop iteration. Each algorithm execution instance runs its own loop, so a nested algorithm has its own counter and an operator inside it reports the nested iteration, which is the granularity a user wants.

What is missing is a channel from the loop to the observation callback, and today's genealogy analyzer shows what filling that gap by hand looks like: it anchors on an interceptor purely so that `AfterInterception` can tell it a generation ended. The interceptor is the last thing an iteration does, so it works, but using a transformation hook as a clock is a workaround for a missing channel rather than a design.

Two rules follow.

1. **Anchor an interceptor when analyzing what that interceptor does, never to find out what time it is.** Counting the candidates a duplicate-elimination interceptor removed per iteration is a real measurement with the interceptor as its subject. Observing an interceptor to learn the generation number is not.
2. **Generations are recovered by grouping, not by signalling.** If every contribution carries the iteration it happened in, generations are a projection over stamps. This also works for algorithms that have no interceptor and no clean generation boundary, which a boundary signal does not.

Genealogy therefore binds derivation anchors only.

Two constraints shape how the stamp can reach an operator firing, and both are recorded here because they rule out the obvious answers. Search states cannot carry it: § 4.2 forbids execution data in search states, and `SearchState` is deliberately empty. Role execution contracts cannot carry it either, because widening them is a non-goal of this rework and approaches the rejected typed-invocation design. That leaves a run-scoped ambient context that the iteration loop advances and observations read. Which mechanism is an implementation question deliberately left open.

### The series is one accumulator, not the whole system

It is tempting to treat the series model as the analysis system and everything else as an escape hatch. That is the wrong layering, and genealogy shows why.

Decompose genealogy in the vocabulary above. Its anchor is a crossover; the firing supplies parents and offspring; what it reads is the parent-to-offspring relation; the coordinates of the firing are the same iteration and evaluation counts as anywhere else. Only one thing differs: contributions fold into a graph instead of appending to an ordered list.

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

A series analysis appends stamped samples. A lineage analysis merges edges and node data. Both are stateful objects attached to one run. They share anchors, firings, stamps and the recording decision, and differ in their accumulation and reading APIs.

This layering has three consequences.

1. Genealogy is not a bypass. It is a second accumulator over the same plumbing, and remodelling it on the new system is a goal rather than a risk. It keeps its state-transition anchor: the run's iteration counter advances at root-algorithm boundaries, so for a nested or steady-state algorithm many derivation firings share one root iteration and the stamp alone cannot say which generation a derivation belongs to.
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
| Stamp           | The set of coordinates recorded with one sample.                          |
| Firing          | One visit to an anchor. An anchor fires many times per run.               |
| Sample          | One stamp and one aggregated value.                                       |
| Series          | The ordered samples collected by one run-bound series analysis.            |
| Measurement     | The value strategy read at a boundary.                                    |
| Aggregation     | The value strategy reducing readings to one value, within or across firings. |
| Recording policy | The rule deciding whether a firing produces a sample.                    |

## Goals

1. Express the mainstream catalog as combinations of anchor, measurement, aggregation, stamp and recording policy rather than as one hand-written type per metric.
2. Record every sample against all available coordinates so that one series can be read per iteration, per evaluation or per second without a second analyzer.
3. Make retention a configured choice rather than a property of which analyzer type was picked, so expensive aggregations stay affordable.
4. Make the accumulator the extension point, so that analysis which is not a series is a different fold over the same anchors, firings and stamps rather than a separate framework. Genealogy is the reference case.
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

This plan uses the glossary meanings of analyzer, analyzer result, observation, observation anchor and run. It introduces measurement, aggregation, stamp, sample and series as defined above; those are proposed glossary additions and must be settled before the specs are written.

It covers:

- the composition model of anchor, measurement, aggregation and stamp
- the measurement and aggregation catalog shipped with the main and encoding packages
- run-owned analyzer configurations and results
- observation registration and installation
- observable algorithm and operator wrappers as analyzer infrastructure
- analyzer attachment and lookup on `AlgorithmRun`
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

`TrackBestMedianWorst` avoids this for the root algorithm because it reads the anchor from the run. Named inner algorithms and operators still rely on caller-managed identity. The framework does not distinguish an anchor that resolved but had no calls from an anchor that never existed in the execution graph.

### Analyzer run state is mostly mechanical

For ordinary analyzers, `Analyzer<TResult>` creates a private `IAnalyzerRunState<TResult>` wrapper that holds only the analyzer and result references. Mutable analysis data lives in the result object.

Dynamic analysis is the only current implementation that needs custom run-state behavior. It subscribes to `DynamicProblem.OnEvaluation` and implements `IDisposable` so the run can unsubscribe later.

The public abstraction is therefore heavy for normal authoring while its exceptional cleanup behavior remains implicit.

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
- `TrackSeries`, common `Track...` shortcuts and analysis-specific attachment methods
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

1. A `Track...` call chooses the analysis behavior and anchors.
2. The returned run-bound analysis receives observations and holds mutable work data.
3. The run owns observation installation, registration lifetime and cleanup.
4. Properties expose already published immutable values without allocation. Methods publish immutable snapshots or projections and may allocate.

Do not force ordinary users through separate reusable configuration, attachment handle and result lookup objects. Do not require ordinary analyzer authors to implement a wrapper whose only job is to forward `RegisterObservations`.

### Write the desired API usage spec first

Add a desired-state API usage spec beside the current API specs before changing contracts. It should demonstrate:

1. Attaching a quality analyzer to the run without an interceptor, in one line.
2. Reading the same series against two different coordinates without a second analyzer.
3. A scalar reduction, a frequency map and a pairwise aggregation composed from the same parts.
4. An encoding-specific measurement used with a general aggregation.
5. Observing an explicitly named inner algorithm.
6. Observing an operator for data that search states do not contain.
7. A domain stamp, using the dynamic epoch.
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

**The preference is to fail as early as possible**, ideally at `Track`, on the line where the mistake was made and naming the analysis and the unreachable anchor.

How early is achievable is an implementation question and is deliberately left open. In preference order:

1. Throw at `Track`. Requires walking the configuration graph from the run's root algorithm to collect reachable configurations by reference. No such traversal exists today: `IExecutionInstanceResolvable` is a bare marker and `IOperator` exposes only `CreateExecutionInstance`. Adding declared children to every configuration is a large new public contract and would need weighing against non-goal 1 rather than assumed.
2. Throw when execution starts, if the instance graph turns out to be walkable at that point.
3. Throw at completion. `ExecutionInstanceRegistry` records whether each installed replacement was ever consumed by `Resolve`; because replacement lookup walks up the parent chain, consumption in any child registry counts.

Any of these satisfies the requirement that the failure is loud. Which one is taken should be decided when the cost of each is known, not now.

What is not open: a resolved anchor with zero invocations must not throw, and silent empty results must not remain the only behavior.

Anchors supplied at attachment already remove the most common form of this mistake, and root helpers such as `TrackBestMedianWorst` take their anchor from the run so the dominant case cannot hold a stale copy at all.

### Define live reading and publication

#### Settled: the requirement

A published result must not be a moving target. Handing out the live accumulator behind a read-only interface fails this, and the failure is general rather than a threading artifact:

1. `List<T>` invalidates any open enumerator on append, on a single thread with no concurrency involved. It throws whenever an enumeration *spans* an append, which happens as soon as a result is passed into any async pipeline.
2. Without any exception at all, the same result object answers `Count` differently on successive reads, and a deferred LINQ query over it changes its answer. A result that keeps moving cannot be compared, cached, asserted on, or handed to a report.
3. `IReadOnlyList<T>` over a `List<T>` can be downcast and mutated.

Point 2 is the one that matters most, because it is silent.

Live reads are supported. The stateful object returned by `TrackSeries` has one reading API during and after execution:

- Properties such as `Latest`, `SampleCount` and `IsCompleted` return already published values that are safe to share. They do not allocate or perform projections.
- Methods such as `Snapshot`, `ByIteration`, `ByEvaluations` and `Transpose` may allocate. Each returns an immutable value that never changes after publication.
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

### Collapse the evaluation-count analyzers into the stamp model

`BestMedianWorstAnalysis`, `BestMedianWorstPerEvaluationAnalysis`, `BestPerEvaluationAnalysis` and `BestQualityAlgorithmAnalysis` are four types for what the composition model treats as at most two configurations. The first two differ only in which coordinate they record against, which stamps remove entirely. The last two differ only in aggregation, running best against best, median and worst.

Do not migrate these four types. Replace them with the composed form and keep a convenience entry point for the dominant case so that `TrackBestMedianWorst` stays a single line.

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
| `QualityCurvePerEpochAnalysis` | quality measurement, best aggregation, epoch coordinate |
| `InvalidPerEpochAnalysis` | validity measurement, counting aggregation, epoch coordinate |
| `BestBeforeChangePerformanceAnalysis` | per-epoch best series plus an online curve model fitted across firings |

The first two are ordinary compositions. The third is not a plain series: it maintains a fitted model and derives a prediction from it. That is the second accumulator besides lineage, which is useful evidence that the accumulator is the right extension point rather than an abstraction invented for one case.

Two obligations follow. The epoch must be a stamp coordinate contributed by the problem area that owns it, not a bespoke event channel. And no dynamic analyzer may implement disposal, override analyzer state creation, or otherwise need machinery the other analyzers do not.

### Rework `EvaluationTiming` rather than bend the model around it

`EvaluationTiming(long EpochCount, int Epoch, bool Valid)` is what made dynamic analysis look like it needed several samples per firing. It conflates two unrelated things.

`EpochCount` is a free-running counter incremented once per evaluation. That is an evaluation index, and the run-owned `evaluations` coordinate supersedes it entirely. It is not an epoch and should not be named as one.

`Epoch` is the environment version, and it is the only genuine domain coordinate here. The important observation is that it is **already stable within a firing** for two of the three update policies: `ResolvePendingUpdates` runs at `AfterEvaluation` or `AfterInterception` boundaries, so every candidate in a batch was evaluated against the same environment even while the free-running counter ticked over mid-batch. Tagging those evaluations with different epoch numbers is therefore already misleading, independently of analysis.

The rework is to record the environment version an evaluation was actually made against, and to drop the redundant counter. One firing then carries one environment version and the model holds without relaxation.

`UpdatePolicy.Asynchronous` is the real exception, because it resolves updates inside `Evaluate` and the environment can genuinely change mid-batch. Three ways out, to be decided when the dynamic problems are reworked: take the environment version at the start of a firing and accept the approximation, declare that asynchronous updates do not support epoch-coordinated series at evaluator anchors, or reconsider the policy. This is a small decision and should not hold up the model.

`DynamicProblem` is experimental, so changing it is preferable to distorting a contract the whole library depends on.

## Implementation sequence

### Phase 1: Lock the desired usage

1. Settle the composition vocabulary. Confirm measurement, aggregation, stamp, firing, sample, series and recording policy as glossary terms, and confirm the default stamp coordinates.
2. Settle which measurements, aggregations, recording policies and accumulators ship, and in which package each lives.
3. Settle the supply taxonomy and how a measurement declares the supply it requires. The approach is fixed: classify anchors by supply, never by role, so that an unusable combination such as an objective value at a crossover is unrepresentable and a user-defined role participates without any analysis-side change. What is open is the exact set of supplies. This is the hardest ergonomic question in the design and the specs exist mainly to answer it.
4. Decide whether the run owns the canonical evaluation and time counters. Stamps are only possible if it does, and it is what lets four current analyzers collapse into one.
5. Add desired-state API usage specs beside the current analysis specs, covering at minimum a scalar reduction, a frequency map, a pairwise aggregation, a domain stamp and one hand-written analyzer that bypasses the composition layer.
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

1. Introduce the stamp and the run-owned counters that populate it.
2. Introduce the stateful run-bound series, its locked accumulator and its immutable snapshot and projection values.
3. Introduce the measurement, aggregation and recording contracts used directly by `TrackSeries`.
4. Support aggregation within a firing and across firings as separate choices.
5. Ship the general measurement and aggregation catalog and the recording policies.
6. Ship the encoding-specific measurements next to their encodings.
7. Provide series views: reading against a chosen coordinate, and transposition for keyed values.
8. Provide `TrackBestMedianWorst` as a convenience method over `TrackSeries`.

### Phase 5: Migrate analyzers onto the model

1. Replace the four overlapping quality analyzers with `TrackSeries` compositions and convenience methods.
2. Rework `EvaluationTiming` so the epoch is an environment version and the redundant evaluation counter is dropped, then move dynamic per-epoch analysis onto a domain stamp as ordinary compositions with no disposal and no custom analyzer state.
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
2. Duplicate analyzer attachment behavior.
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

1. The mainstream catalog is expressed as combinations of anchor, measurement, aggregation, stamp and recording policy, and adding a metric, an axis or a retention rule does not add an analyzer type.
2. One series can be read against iteration, evaluations and elapsed time without reconfiguring or rerunning.
3. A measurement that an anchor cannot supply is rejected before the run starts.
4. Every `TrackSeries` composition produces a series, and a single value is the last sample of one rather than a second result shape.
5. A keyed series such as a frequency map can be read transposed without a second accumulator.
6. Recording policy is configurable, so an expensive aggregation can be sampled rather than labelled a memory hazard.
7. Analysis that is not a series is a different accumulator over the same anchors, firings and stamps, demonstrated by genealogy remodelled onto the new system.
8. Each `Track...` call returns an independent stateful analysis object bound to that run and its chosen anchors.
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

## Decisions and trade-offs

Settled decisions, each with what was rejected and why. Reopen one only with new evidence.

### Model

| Decision | Rejected alternative | Why |
| --- | --- | --- |
| Coordinates are recorded together on every sample; the axis is chosen at read time | One analyzer per axis | HeuristicLab shipped three types for one metric and HeuristicLib was reproducing it. A reading at the end of iteration 7 is simultaneously at evaluation 700 and 1.4 seconds; these are not alternatives |
| The mainstream catalog is combinations of anchor, measurement, aggregation, stamp and recording | A hand-written type per metric | Adding a metric or an axis should not add a type |
| A series is one accumulator; the accumulator is the extension point | A series system with a hand-written escape hatch beside it | Genealogy decomposes into the same anchors, firings and stamps and differs only in the fold. Framing it as a bypass would have duplicated the plumbing |
| Anchors are classified by what they supply, never by role | A table of which roles admit which measurements | The role set is open and a user can extend it. The supply set is closed and small. One mechanism then settles compile-time rejection, role-free multi-anchor binding and user-defined roles. Verified by a compiling spike |
| Supplies carry search space and problem, in a shared context | Omitting both so measurements never spell them | Problem-dependent measurements need the run's actual context rather than captured external state. Covariance keeps the catalog non-generic either way, so there is no inference cost |
| A measurement declares the narrowest supply it needs | Declaring against the richest supply an anchor offers | Narrow declarations keep valid combinations wide and produce legible compile errors. The narrow supplies carry real members, so they are not bare markers |
| Supplies may be optional, and absence has defined behavior | Required-anchor validation | Lineage without a state transition is a valid one-generation graph; without a creator it is a valid graph with unattributed roots. Absence is part of what the analysis means |
| Generations are recovered by grouping stamps | A boundary signal from an interceptor | Grouping also works for algorithms with no interceptor and no clean generation boundary. Anchoring an interceptor to learn the time is a workaround for a missing channel |
| Recording policy is a configured choice | Retention baked into which analyzer type was picked | It is what makes an n-squared aggregation affordable, and it is the same one-type-per-variation mistake as the axis problem |
| Transposition is a read-time view | A second accumulator shape for keyed values | Same information, different reading. Storing both would double the state |

### Surface

| Decision | Rejected alternative | Why |
| --- | --- | --- |
| `TrackSeries` composes and attaches in one call | A separate universal analysis configuration followed by `Track` and result lookup | The split made a simple analysis appear as a reusable definition, a run-specific handle and a result. Most users need one run-bound analysis, so the main API returns that object directly |
| A `Track...` call returns a stateful run-bound analysis | An inert handle plus `run.GetResult(handle)` | The returned object already belongs to one run and can expose typed safe reads directly. Separate calls create independent analyses |
| One `Track...` call with several anchors feeds one analysis | A separate keyword for multi-anchor binding | Genealogy needs several derivation anchors folding into one graph. Separate calls remain the clear way to create separate analyses |
| Live reads use cheap properties and allocating methods | Completed-only reads; exposing the live collection | Streaming users need current analysis data. `Latest` can return an immutable sample without allocation, while `Snapshot` and projections make publication cost explicit and never expose moving storage |
| The initial store is a locked mutable collection copied on publication | An append-only lock-free log | The measured copy costs are acceptable, and ordinary locking has a much smaller correctness risk. Revisit only with profiling evidence |
| No partial result type | A distinct type for cancelled or failed runs | The caller already knows it cancelled |
| `ImmutableList<T>` is rejected as the series store | using it for free snapshots | Measured: 467 ms append and 1241 MB at a million samples, against 5.5 ms and 23 MB for an append-only log. It allocates on the operation a series performs most. Which store replaces it is still open |
| `TrackSeries` is the common attachment and composition verb | Keeping `WithAnalyzer`; requiring `Analyze.Series` before attachment | It says what the run will retain and returns the object that holds that run's collected data. Common shortcuts such as `TrackBestMedianWorst` preconfigure it |
| Coordinate reads are named methods | A coordinate enum | Each returns a different x type |

### Lifecycle

| Decision | Rejected alternative | Why |
| --- | --- | --- |
| No analyzer cleanup contract at all | A public registration lifetime with an `OnRelease` hook | The only analyzer needing cleanup was dynamic analysis, and only because of an event workaround. Removing the cause removes the need. The run's teardown of what it installed stays internal |
| Dynamic evaluation batches become readable state on the problem | An event, or a dedicated observation source | The event exists only because the log is cleared immediately after firing. Readable state lets any number of analyzers read the same batch with no ordering dependence and no subscription |
| Dynamic analyzers are expressed on the composition model, not merely detached from the event | Removing disposal but leaving them a parallel system | A tidier teardown around the same bespoke machinery is the same hack in better clothing. No dynamic analyzer may implement disposal or override analyzer state creation |
| One firing yields at most one sample | Letting an aggregation partition readings and emit several | Nothing needs it. Keying within a sample is what frequency maps do, and transposed views read them back. Partitioning would be a second way to express keyed data with a second shape to store |
| Rework `EvaluationTiming` rather than relax the model for it | Multi-sample firings to accommodate per-evaluation epochs | It conflates a free-running evaluation counter with the environment version. The counter is superseded by the run-owned `evaluations` coordinate, and the environment version is already stable within a firing for two of three update policies. `DynamicProblem` is experimental; the sample model is not |
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

Coordinate with these backlog items:

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
