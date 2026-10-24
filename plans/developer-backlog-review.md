# Developer backlog review before issue migration

Review date: 2026-10-23. Status: cleanup and structural review applied on 2026-10-24 with developer approval; feature selection and unresolved designs remain undecided. No issues have been published.

## Cleanup applied on 2026-10-24

This is local editorial cleanup, not a migration handover or implementation approval. The backlog remains authoritative. Original B/R identifiers and source line numbers below remain tied to the reviewed snapshot, not the edited file.

- Removed the completed README-content deliverable B14 and ownership-taking factories B60. README example validation remains under B21; broader candidate immutability remains under B11.
- Merged explanatory B17 into B11 and the unbounded scenario placeholder B42 into B03, retaining the audit distinction and requiring named scenario gaps.
- Narrowed B07/B20 to additional controls and configured-composition semantics, retaining the historical Gaussian regression as an explicit verification follow-up. Narrowed B45 to the unresolved need for a broader workflow beyond the existing implementation.
- Preserved all rejected-decision records and unrelated working-tree edits, including the call-context item added after the original review. At that checkpoint, other recommendations remained unapplied; the subsequent restructuring below records the next pass.

Rechecked the current README, ownership-taking factory declarations and real-vector random-generation adoption, `MetaOptimizationProblem` and its scenario, experiment specs, legacy tree mutation methods, `ExecutorControls`, `Named`, `WrappingExecutor`, composition documentation and `ControlsAndContexts` test source. This pass used source inspection; it did not rerun those examples or tests. The test results in the original evidence section below belong to the 2026-10-23 review.

## Restructuring applied on 2026-10-24

The developer authorized consolidation, factual refresh and grouping by next action. The backlog now separates concrete problems and bounded follow-ups, requirements/design questions, and conditional ideas. These are review categories, not implementation approval or a commitment to publish issues. Vague and conditional entries remain for later selection; none was discarded in this pass.

- **Merged:** B01/B48/B50 into run lifecycle/results/naming, with setup/running types retained as an option; B06/B54 into execution policy/batching ownership; B27/B28 into pipeline stage transitions, including same-type transformations.
- **Kept independently scoped:** B25 genealogy correctness, B35 provenance and B23 equivalence share context without making a general lineage framework prerequisite to a focused fix. B65 cancellation remains separate from concurrency policy. B32/B33/B62 share benchmark infrastructure, not conclusions or acceptance gates.
- **Split:** B34 into matching conveniences, advice/lifecycle semantics, advice-level retry, interceptor replacement, caller-path selection and persisted aspects; B56 into convention cleanup, expensive-test policy and coverage reporting. B22 remains a bounded fix within convention cleanup. Splitting descriptions does not select separate issues.
- **Refreshed:** B02 persistence premises; B08 mating/count reconciliation; B15/B16 existing enforcement; B18 unverified package release status; B22 stale evaluator examples; B24 current API conveniences; B30/B31 objective/analysis terminology; B35 routing names; B36 numerical coverage wording; B51/B52 current authoring checks; B53/B55 caller-driven inference/naming questions; B57 the stale operator-migration inventory; B59 dependency-import wording; and B67 the remaining boilerplate question.
- **Preserved evidence:** B67's evaluator-change account moved verbatim, apart from list indentation/formatting, to the historical evidence subsection below. Its counts remain historical and its commit still needs identifying. The newer call-context entry is reviewed as B68 and retained conditionally under the accepted sixth-review constraints.
- **Reconciled source status:** R33/R34's removals and R35's operator state-access outcome are present in current source. Algorithms and own roles still use factories, consistent with the accepted step boundary. Related rows R05/R11/R24/R27/R31/R32 and obsolete source links were refreshed. Rejected decisions and reopening conditions are preserved; only R33's present-tense ranking description in the backlog was corrected to explicit directions supplied by the trace function.

Source checks included the GA's parent-count and pairing code, `PipelineAlgorithm`, scalar problem/operator batching, `Retry`, objective/ranking APIs, observation sources and trace functions, expression metrics, Boolean operators, the migration inventory, architecture tests/analyzer hooks, current contributor/test guidance, and the operator plan's accepted fifth/sixth reviews. The mating plan requests `PopulationSize - Elites` offspring while current GA code selects `2 * PopulationSize` parents; acceptance and the intended reconciliation remain open. Searches of production C# sources (excluding build output) found no `Readings<T>`, `IProblemObservation`, operation-argument interfaces, `OperationExecutorAdapter` or `OperatorExecutorFactory`. These are source-inspection findings, not new runtime or performance results.

Every pre-pass active entry is accounted for by relocation, merge or split. Formatting and repository-link checks validate the edited Markdown; the historical runtime test results below were not rerun for this prose-only pass.

## Scope and evidence

Reviewed [developer-backlog.md](developer-backlog.md) against the current working tree, relevant contributor rules, implementation, tests and related plans. Base commit: `9c567b9e2e7bcf38c0ce7a59b644c69cd956db9d`. The checkout includes staged and unstaged changes; this is a working-tree review, not a claim about that commit or a released package.

The source backlog's SHA-256 at the start of review was `82AF9B2AFE54A81E086E0C4E4CBE7B6F81DC6357D8F1BC56E6AFAEDA00F9DE71`. Source line numbers below refer to that version. B01–B67 cover all 66 open-work bullets plus the explanatory immutability bullet B17. R01–R40 cover every heading under “Discussed, tried, and rejected.” The multi-paragraph advice entry B34 is reviewed by subtopic.

Disposition and issue recommendation are separate: a real unresolved concern can be worth retaining without creating an actionable issue yet. **Keep** means substantially accurate; **Rewrite** means the concern survives but its wording, scope or premise needs correction; **Resolved** means the stated deliverable is present; **Obsolete** means the entry is redundant or its premise no longer applies; **Needs decision** means code cannot establish the intended requirement. None of these recommendations grants implementation approval.

Evidence is primarily source inspection. Three focused existing test selections passed with the SDK selected by `global.json` (.NET SDK 10.0.402):

- `dotnet test --project test/HeuristicLib.Tests/HeuristicLib.Tests.csproj --configuration Release --no-restore --filter-class '*OperatorAdviceTests'`: 8 passed.
- The same command with `--filter-class '*VectorTests'`: 244 passed.
- The same command with `--filter-class '*ExperimentMaterializationTests'`: 8 passed.

The first test attempt was blocked by sandbox access to the installed SDK directory; the permitted retry succeeded. Other cited tests were inspected, not executed. No whole-solution validation, performance measurements or reproduction of the historical genealogy scenario was performed. Search results establish what was found in the inspected checkout, not the absence of every possible implementation elsewhere.

## Main findings

1. **Do not migrate the backlog verbatim.** It mixes actionable defects, proposed architecture, conditional ideas, completed work and historical explanations.
2. **Two deliverables are already present:** the README front-door content (B14) and ownership-taking vector factories with random-generation adoption (B60). Meta-optimization (B45) also already exists as an Experimental implementation, although its intended broader workflow is unspecified.
3. **The wrapper-control bug needs narrowing.** Transparent names and observation/invocation wrappers now forward controls, with passing coverage. Whether a configured pipeline should expose a meaningful mutation-strength control is a separate design question (B20).
4. **Several short entries cannot become useful issues without a concrete missing scenario:** graph invariants, multi-objective short-path usage, richer experiments, dynamic extension and population metadata (B05, B26, B42, B44, B47).
5. **The original R33–R35 discrepancies have been rechecked:** the 2026-10-24 source implements the removals and operator state access. Algorithms and own roles retain factories under the accepted staged plan. See the refreshed rows; do not carry the original mismatch forward as a current finding.
6. **Keep conditional ideas conditional.** Hierarchical trial paths, experimental annotations, extra controls, dependency splitting and output buffers should not automatically become implementation issues.
7. **Consolidate overlapping work.** Result objects and stop reasons belong together; scenario placeholders belong with their domain work; observation, batching and resolver performance should share the benchmarking infrastructure without sharing an unproven performance conclusion.

## Open-work entries

### B01 — Setup types and run lifecycle (source line 11)

**Restructure applied 2026-10-24:** Merged with B48/B50 into one lifecycle/result/naming investigation; setup/running types remain an option, not a required redesign.

**Rewrite; design issue only.** “Single use” obscures the difference between a resumable algorithm run and a terminal experiment run. [AlgorithmRun tests][run-tests] cover pause/resume, competing streams and rejected late attachments; [ExperimentRun][experiment-run] has its own terminal scheduling lifecycle. Replacement: “Evaluate whether separate setup/running types improve misuse prevention while preserving algorithm pause/resume, experiment terminal behavior and direct fluent setup.” Link B48/B50; do not present working runtime guards as absent.

### B02 — Experiment serialization (line 12)

**Restructure applied 2026-10-24:** Reworded persistence around recipes, materialized cases or application-owned setup; no round-trip contract inferred.

**Rewrite; retain a decision issue.** [Grid][grid] immediately applies configurators; [materialization tests][materialization-tests] verify immutable materialized configurations, keys and fork paths. Runtime factories are not proof that a stored grid retains delegates, and serialization friendliness is not a tested round-trip contract. Replacement: “Decide whether to persist construction recipes, materialized cases, or application-owned setup, including reproducibility inputs and per-trial analysis.”

### B03 — Experiment composition extensions (line 13)

**Cleanup applied 2026-10-24:** merged B42 into this entry and required a named gap and expected outcome against existing scenarios. Deferred composition candidates remain undecided.

**Rewrite; deferred scenarios, not a bundle of implementation issues.** [ExperimentRun][experiment-run] supplies a common problem/initial state and schedules materialized trials. Existing composition does not establish which proposed per-trial variation a user needs. Replacement: “Select one concrete experiment requiring per-trial problem/state, heterogeneous algorithms or individual cancellation; document the gap against current materialization and scheduling.” Merge B42 into this scenario inventory.

### B04 — Hierarchical trial paths (line 14)

**Keep; defer an open issue until the trigger exists.** Typed keys and immutable `RandomForkPath` are exercised by [materialization tests][materialization-tests]. Preserve the condition that tuple keys must first prove insufficient. This is a design constraint for B03, not an independently justified migration task.

### B05 — Execution-graph invariants (line 15)

**Needs decision; not ready for an issue.** [ResolutionScope][scope], [resolution tests][scope-tests] and [factory lifetime tests][factory-lifetimes] already cover substantial execution ownership and resolution behavior. The entry names no missing invariant. Replacement: “Identify the specific graph invariant not enforced or tested, with a minimal failing graph and expected behavior.” Do not invent a general graph-validation subsystem from this phrase.

### B06 — Run-level execution policy (line 16)

**Restructure applied 2026-10-24:** Merged B54 into execution policy/batching ownership; cancellation B65 remains independently scoped.

**Rewrite; one architecture investigation.** [SingleCandidateMutator][single-mutator] and [SingleSolutionProblem][single-problem] still own concurrency settings; [ExperimentRun][experiment-run] separately controls trial concurrency. The gap remains, but “execution registry” should be replaced by the current scope/preparation vocabulary. Replacement: “Define run-scoped concurrency policy, capability constraints and budget ownership across operator batches and trials, preserving explicit state and deterministic RNG.” Include B54 and coordinate B65.

### B07 — Additional run-scoped controls (line 17)

**Cleanup applied 2026-10-24:** removed the obsolete raw-projection-only and blanket wrapper-bug wording; retained the concrete-consumer requirement and link to the composition-control question.

**Rewrite; retain as a conditional requirement.** [ExecutorControls][controls] now exposes explicit control-provider forwarding. The assertion that raw-control projection is the only mechanism is outdated. Replacement: “Require a concrete algorithm consumer and authoring example before adding controls beyond mutation strength; apply the current control-provider contract and state composition semantics explicitly.” Link the remaining B20 question; do not create a generic parameter framework issue.

### B08 — Role-aware mating (line 18)

**Refreshed and applied 2026-10-24; reconcile with the existing plan.** [Mating contract](mating-contract.md) describes ordered parent roles and `PopulationSize - Elites` offspring. Current [GA source](../src/HeuristicLib/Algorithms/Evolutionary/GeneticAlgorithm.cs) selects `2 * PopulationSize` parents, pairs them through `ToParentPairs`, evaluates the offspring and then performs elitist replacement. The backlog now names that discrepancy and requires acceptance evidence before introducing an arrangement role. Source inspection does not settle which count policy should be implemented.

### B09 — Record configuration style (line 19)

**Keep; contributor-policy decision.** Current records use both forms, for example [GridExperiment][grid-experiment] and [composite genotype][composite-genotype]. The [guidelines][guidelines] prescribe immutable configuration and naming but do not settle this stylistic choice globally. A small issue should show representative examples and the resulting rule; broad mechanical conversion is a separate scope decision.

### B10 — Operator recommendations (line 20)

**Keep; choose concrete encodings before implementation.** [PermutationSearchSpace][permutation-space] and [TravelingSalesmanProblem][tsp] implement recommendations. The proposed wider coverage and reproducibility policy remain useful. Separate evidence-backed recommendations from the decision about recording their origin; do not make every encoding a child issue merely because it exists.

### B11 — Candidate immutability audit (line 21)

**Cleanup applied 2026-10-24:** merged B17's policy-versus-audit distinction and narrowed the task to identifying representation-specific risks before deciding migration or retirement. B60's completed factories do not close this audit.

**Rewrite; bounded audit issue.** Core vectors have immutable APIs and explicit ownership transfer, while [legacy tree nodes][legacy-nodes] still expose mutable tree operations and parent links. A whole-library claim cannot be closed from vector tests. Replacement: “Inventory maintained and legacy candidate representations, identify mutation/aliasing risks and decide migration or retirement per representation.” Preserve existing legacy-retirement approval boundaries; do not silently rewrite every experimental type.

### B12 — Merge core and Experimental unit-test projects (line 22)

**Needs decision; keep deferred unless there is a concrete cost.** [The test guide][test-guide] explicitly assigns separate suite responsibilities, and [assembly tests][assembly-tests] enforce package boundaries. Existing organization is a settled working baseline. Before opening a redesign issue, identify a measurable reporting, dependency or maintenance problem that merging would solve. Namespace stability alone is not evidence that merging is needed.

### B13 — ExperimentalAttribute (line 23)

**Keep; defer until a stable baseline exists.** The [guidelines][guidelines] still describe early alpha and provisional public APIs. No current need to convert this conditional reconsideration into annotation work. Preserve it with packaging/release policy.

### B14 — README front door (line 24)

**Cleanup applied 2026-10-24:** removed the completed content-deliverable bullet after rechecking the README. B21 already includes guide and README example validation; no execution or compactness acceptance is claimed.

**Resolved as a content deliverable; no new feature issue.** [README](../README.md) contains install commands, TSP and symbolic-regression examples, package overview, guide links and the demonstrator GIF. Example execution and compactness are different acceptance questions; this review did not execute the README. Fold remaining example validation into B21 rather than reopening the original “add these sections” task.

### B15 — Formatting baseline and enforcement (line 25)

**Restructure applied 2026-10-24:** Retained both open wrapping choices and settled conventions; corrected enforcement wording and historical-count status.

**Rewrite; policy/enforcement issue.** [.editorconfig](../.editorconfig) exists; [CI][ci] still makes formatting, style and analyzer checks non-blocking. The historical “roughly 990” count is not a current measurement, and “none of these rules is enforced” is too broad alongside analyzer/style rules. Replacement: “Choose the remaining wrapping conventions, identify which tools enforce each rule, measure the current violations, then make the agreed checks blocking.” Preserve the two explicitly deferred wrapping decisions.

### B16 — Warning debt (line 26)

**Restructure applied 2026-10-24:** Described extension of the existing warning policy, not absent enforcement.

**Rewrite; baseline and ratchet issue.** [Directory.Build.props](../Directory.Build.props) already treats nullable and selected XML-documentation diagnostics as errors. Replacement: “Measure remaining warnings by diagnostic and suite, fix or justify them, and extend the existing staged policy.” No warning-clean conclusion follows from the focused incremental tests in this review; do not migrate historical counts as current debt.

### B17 — Immutability explanatory bullet (line 30)

**Cleanup applied 2026-10-24:** removed the separate bullet and its empty introductory section; preserved its rationale in B11.

**Obsolete as a separate work item; merge into B11.** This is rationale for the audit, not a second deliverable. Retain the distinction between a documented rule and verified repository coverage.

### B18 — Deprecate the Contracts package (line 36)

**Restructure applied 2026-10-24:** Changed the backlog to verification followed by conditional release action; published deprecation remains unverified.

**Needs external verification; release checklist item.** The [main project][core-project] owns the package and the solution no longer has a Contracts project. That cannot prove the status of an already published package. The attempted [NuGet page](https://www.nuget.org/packages/HEAL.HeuristicLib.Contracts) lookup was inaccessible through the web tool. Check deprecation and the replacement release before retaining or closing this item; do not publish the assertion “still needs deprecation” as verified current fact.

### B19 — ZDT file casing (line 37)

**Keep; small actionable cleanup issue.** Git still tracks [ZDT1.cs](../src/HeuristicLib/Problems/TestFunctions/ZDT/ZDT1.cs) and [ZDT5.cs](../src/HeuristicLib/Problems/TestFunctions/ZDT/ZDT5.cs), alongside `Zdt2.cs`–`Zdt6.cs`. The requested casing change remains applicable. Its execution still requires explicit authorization for any necessary Git/index operations.

### B20 — Capabilities through wrappers (line 39)

**Cleanup applied 2026-10-24:** replaced the blanket wrapper-bug claim with the configured-composition decision and corrected the documentation link. Current source uses `WrappingExecutor` to forward controls through invocation wrappers. The specific historical Gaussian reproduction remains an explicit verification follow-up, not a freshly verified fix.

Historical reproduction retained from the original backlog: a deterministic one-dimensional `EvolutionStrategy` with initial Gaussian strength 3 reportedly produced `0, -1.5, -2.5` without child inspection and `0, -1.5, -3` with counting advice on the Gaussian beneath `gaussian.Named("exploration")`, including selection through a broad mutator pointcut. These values were not rerun during cleanup.

**Rewrite; split resolved transparent forwarding from the remaining composition decision.** [ExecutorControls][controls], [mutation observation advice][mutation-observation] and [Named][named] support forwarding. `ControlsAndContexts` in [OperatorAdviceTests][advice-tests] passes for nested names, broad advice, shared controls and rebinding. [OperationBindingTests][binding-tests] also contains control forwarding/override coverage. [Composition documentation][composition-doc] says configured pipelines expose only controls they deliberately support.

Replacement: “Decide which controls, if any, a configured composition can meaningfully expose; preserve the existing transparent-wrapper forwarding contract.” Retire the old blanket “no general solution selected” and direct/child-advice failure claims after checking the exact Gaussian regression if it is to be marked closed. The passing generic test is not a fresh execution of the historical numeric example. Do not assume forwarding one child's strength through an arbitrary pipeline is semantically correct.

### B21 — Executable documentation imports (line 41)

**Keep; one scoped documentation-quality issue.** [The test guide][test-guide] establishes source imports and the Docs suite; [experiments documentation][experiments-doc] and README still contain fenced examples. Inventory remaining runnable examples, choose imports versus explicitly illustrative snippets, and validate them from one source. Include README follow-up from B14 and avoid parallel copies in API specs.

### B22 — Comment-only numeric optimization specs (line 42)

**Restructure applied 2026-10-24:** Narrowed to removing misleading claims or distinct current refiner specs; coordinated with B56 convention cleanup.

**Rewrite; actionable test cleanup.** [SymbolicRegressionRedesignSpecs][sr-specs] still has two commented examples ending in `typeof(...).ShouldNotBeNull()`. Their evaluator-oriented design is stale: current fitting belongs to [NumericParameterFittingRefiner][fitting-refiner], and [NumericParameterFittingSpecs][numeric-specs] already demonstrates current usage. Replacement: “Remove misleading placeholder claims or replace them with distinct current refiner workflows not already covered.” Do not revive the obsolete optimizing-evaluator design simply to make the comments executable.

### B23 — Solution equivalence (line 43)

**Restructure applied 2026-10-24:** Preserved the proposed resolution order and API shape while making acceptance uncertainty explicit.

**Keep, with acceptance status explicit; design issue.** [RemoveDuplicatesInterceptor][duplicates] takes an explicit comparer, while [IProblem][problem-contract] and [ISearchSpace][space-contract] do not establish the proposed equivalence-resolution chain. The concept remains distinct from value equality. Preserve the intended override ordering as a proposal unless acceptance evidence is located. Coordinate B25/B35; occurrence identity must not be collapsed into equivalence.

### B24 — Equivalent API entry points (line 44)

**Restructure applied 2026-10-24:** Reframed as a current caller-workflow audit preserving deliberate inference conveniences.

**Rewrite; user-flow audit, not blanket API removal.** [Guidelines § 8.6][guidelines] explicitly justify constructors, inference factories and some fluent helpers. [Analyzer][analyzer] and [run trace helpers][run-traces] have changed substantially. Replacement: “Identify concrete ambiguous workflows in the current API and remove only redundant spellings that add no inference or setup value.” Begin with a current caller example and preserve deliberate inference conveniences.

### B25 — Repeated genealogy values (line 45)

**Restructure applied 2026-10-24:** Retained a focused correctness task without requiring completion of a general provenance/equivalence design.

**Keep; bug plus design decision.** [GenealogyGraph][genealogy] still keys each generation by candidate value and calls `current.Add(child, childNode)` except when an equal child is also a parent. The collision mechanism remains visible. Preserve the reported NSGA-II scenario as historical reproduction evidence, not a rerun result. Specify occurrence/derivation semantics before changing insertion behavior; connect B23 and B35.

### B26 — Multi-objective short-path usage (line 47)

**Needs decision; replace the placeholder before drafting an issue.** [Objective APIs][objectives] and [aggregation implementations][aggregations] already distinguish objective directions from total-order requirements. Replacement: “Name the shortest multi-objective workflow that is currently missing or awkward, with desired construction, result access and analysis.” Link B30 and B24; do not infer an entire new API from “short-path.”

### B27 — Heterogeneous pipeline state types (line 48)

**Restructure applied 2026-10-24:** Merged B28 into typed pipeline transitions with both heterogeneous and same-type examples.

**Keep; design issue together with B28.** [PipelineAlgorithm][pipeline] has one `TSearchState` for all stages. Supporting different state types remains unimplemented in that contract. Define typed stage transitions and what the composed stream exposes before implementation.

### B28 — Explicit pipeline state transformations (line 49)

**Restructure applied 2026-10-24:** Merged into B27; no independent deliverable discarded.

**Keep; companion requirement within B27.** [PipelineAlgorithm][pipeline] forwards the previous stage's state directly; it has no explicit transition function. Include same-type transformations in the examples so heterogeneous types do not become the only supported use case. Split into child work only if the accepted design yields independently reviewable deliverables.

### B29 — Probability-based selection abstraction (line 50)

**Needs decision; retain as a design candidate.** [ProportionalSelector][proportional] and [LinearRankSelector][rank-selector] still own their mappings. Existing weighted sampling does not itself establish a composable selector contract. Require a concrete need to interchange mappings and compare it with small helpers before accepting another public abstraction.

### B30 — Objective-system rework (line 51)

**Restructure applied 2026-10-24:** Reframed around remaining semantics and promotion criteria rather than restarting terminology changes.

**Rewrite; audit remaining semantics rather than restart terminology work.** [ObjectiveDirections][objectives], [MultiObjective][multi-objective] and [the objective guide][objective-doc] already use the intended vocabulary; `RequireTotalOrder` rejects missing ordering for relevant [aggregations][aggregations]. Replacement: “Identify remaining inconsistencies in single-objective, dominance and total-order requirements, then define promotion criteria for Pareto/hypervolume analysis.” Existing names are progress, not proof that every semantic question is settled or every failure case has regression coverage.

### B31 — Measurement and aggregation catalog (line 52)

**Refreshed and applied 2026-10-24; inventory current functions and missing workflows.** The original review described a readings-based intermediate state. Current [observation sources][observations] deliver records and [Analyzer][analyzer] reads them through functions; [Ranking][aggregations] and [BestSoFar](../src/HeuristicLib/Analysis/Tracing/BestSoFar.cs) supply ranking and accumulation. [Expression metrics](../src/HeuristicLib/Encodings/SymbolicExpressions/Metrics/ExpressionMetrics.cs) already provide some encoding analysis. Preserve the requested workflow inventory and numeric-type inference decision, but do not recreate removed measurement strategies or `Readings` source types. Each additional helper needs a concrete gap beyond ordinary functions.

### B32 — Observation cost (line 53)

**Restructure applied 2026-10-24:** Linked common benchmark infrastructure and kept semantics and conclusions specific to observation.

**Keep; benchmark issue linked to infrastructure.** [MutationObservation][mutation-observation] is a class record allocated by observation advice; each subscription builds advice. The source supports the allocation/wrapper concern, but does not establish its measured impact or a universal zero-cost claim. Compare records/structs and observer aggregation with matched semantics, including clocks and failure behavior; do not change representation merely because allocation exists.

### B33 — Resolver/advice performance (line 54)

**Restructure applied 2026-10-24:** Retained conditional profiling and distinguished historical checkpoints from a current baseline.

**Rewrite; distinguish historical measurements from the current baseline.** [The operator plan][operator-plan] contains measured Package 2a/2b checkpoints; “no comparative cost claim” is too broad. Those results do not benchmark every current resolver or later working-tree revision. Replacement: “Select a concrete setup, deferred-activation or advice workload; establish the current revision's baseline and compare matched alternatives.” Link the [benchmarking plan][benchmark-plan] and retain explicit acceptance boundaries.

### B34 — Matching and advice follow-ups (lines 55–63)

**Restructure applied 2026-10-24:** Split into six conditional concerns; existing configured retry is no longer described as absent. Original detailed deferral constraints remain.

**Rewrite and split by responsibility; no single omnibus implementation issue.** [Pointcut][pointcut], [Advice][advice], [Retry][retry] and [iterative execution][iterative] are the current baseline.

- **Matching conveniences:** keep conditional. Existing reference/type/predicate/Boolean/name matching is not missing work. Require a concrete wildcard or attribute-selection example.
- **Advice kinds and failure semantics:** retain an investigation only for demanded before/after/finally APIs; ordinary wrapper control flow already exists.
- **Retry helpers:** remove the blanket deferred claim. Configured `Retry` and [retry tests][retry-tests] exist. A distinct advice-level retry convenience still needs a demonstrated use case.
- **Replacing Interceptor:** preserve the deferral and exact internal iteration boundary. A wrapper of yielded states still cannot replace the inner loop's retained next-step state or prior termination decision.
- **Caller-path selection:** retain as its own conditional design question about execution occurrences and shared state, not a pointcut syntax task.
- **Persisted aspect descriptions:** retain as conditional and link B02; no accepted persistable runtime-advice contract was found.

### B35 — Provenance and operator attribution (line 65)

**Restructure applied 2026-10-24:** Updated routing names and separated provenance/attribution from the focused genealogy defect.

**Rewrite; parent design question with concrete scenarios.** [GenealogyGraph][genealogy] still reconstructs identity by value. The old `ChooseOne*` names have become [ChoosePerCall][choose-call] and [ChoosePerItem][choose-item]. Replacement: “Define occurrence identity and derivation across operator boundaries, including routing attribution where success rates need it.” Use B25's defect as a concrete requirement; distinguish this from B23's equivalence semantics.

### B36 — Population similarity semantics (line 66)

**Restructure applied 2026-10-24:** Distinguished missing numerical behavior coverage from existing construction coverage.

**Rewrite; small high-value correctness issue.** [PopulationSimilarityAggregation][similarity] still skips the diagonal, initializes minima to 1/maxima to 0 and divides by `count - 1`; a singleton therefore exposes the stated undefined mean. [ObservationSourceRequirementTests][observation-requirements] covers constructing a similarity trace, but does not exercise the numerical aggregation. Replacement: “Define empty/singleton and diagonal semantics, validate the similarity contract and add focused numerical behavior tests before promotion.” Distinguish missing numerical coverage from the existing construction/package-boundary checks.

### B37 — Exact epoch work (line 67)

**Keep; conditional design issue.** [EpochWorkTrace][epoch-work] explicitly assumes every evaluation reaches the problem and documents the caching limitation. Decide whether exact problem work is required before adding problem-owned counters. Link B32 only for observation-cost evidence; neither issue automatically blocks the other.

### B38 — Mutable trace values and shared retention (line 68)

**Keep; deferred guardrails.** [TraceAnalyzer][trace] stores aggregate results and [TraceRetention][retention] can carry mutable sampling state. [AnalysisUsabilityTests][analysis-tests] explicitly covers shared versus independent retention. The documented hazards remain distinct from a universal bug: intentional accumulation/sharing exists. Preserve “add checks when either bites,” and specify an actual misuse case before introducing runtime restrictions or mandatory copying.

### B39 — Island workflow (line 70)

**Rewrite; scenario-led feature candidate.** [IslandPopulation][islands] and its tests already exist, but a population container is not a migration/scheduling algorithm. Replacement: “Specify and demonstrate one island workflow, including topology, migration, RNG and termination, reusing the existing population model.” Do not migrate this as “add island population.”

### B40 — Full ALPS workflow (line 71)

**Keep; substantial algorithm work.** [ALPSGeneticAlgorithm][alps] updates `Population[0]` and returns one layer; ages alone do not implement age-layered evolution. A real issue needs layer creation, admission/aging, replacement and scheduling semantics with acceptance examples. Existing mutation-composition tests do not establish full ALPS behavior.

### B41 — Offspring-selection GA (line 72)

**Keep as a feature candidate; define its contract before implementation.** The inspected algorithm inventory contains GA, NSGA-II, evolution strategy and ALPS but no dedicated offspring-selection GA. Specify success criteria, selection pressure, budgets and objective assumptions. Absence of a class is not authorization to choose those semantics.

### B42 — Richer experiment scenarios (line 73)

**Cleanup applied 2026-10-24:** removed the standalone placeholder and incorporated scenario-gap selection into B03.

**Obsolete as a standalone placeholder; merge into B03.** [Experiment specs][experiment-specs] and materialization tests already supply scenarios. Preserve only named gaps with expected behavior, rather than a second unbounded “more examples” issue.

### B43 — Symbolic-regression validation scenarios (line 74)

**Rewrite; identify residual coverage.** [SymbolicRegressionVerticalSliceScenario][sr-scenario] exercises fitting synthetic data and retaining the compiled best expression, and [numeric specs][numeric-specs] cover fitting usage. Replacement: “List missing symbolic-regression validation workflows beyond the existing vertical slice, especially held-out-data semantics if that is the intended meaning of validation.” Coordinate with [data-analysis modernization](data-analysis-modernization-plan.md); do not claim scenario coverage is absent.

### B44 — Dynamic-problem extension workflow (line 75)

**Needs decision; scope a missing authoring scenario.** [DynamicProblem][dynamic-problem], [AutoEC scenario][dynamic-scenario] and [dynamic analysis tests][dynamic-tests] already provide implementation and coverage. Specify whether the gap is a custom-problem walkthrough, update-policy semantics, or missing runtime behavior. Link B37 if exact work measurement is the actual need.

### B45 — Meta-optimization inside Evaluate (line 76)

**Cleanup applied 2026-10-24:** replaced the implementation-existence task with a requirement clarification for any broader workflow. The Experimental implementation and scenario were inspected again; their generality and execution were not newly validated.

**Resolved for existence; rewrite only if a broader requirement remains.** [MetaOptimizationProblem][meta-problem] already constructs and executes an inner algorithm inside `Evaluate`; [MetaOptimizationTests][meta-tests] contains a scenario. It remains Experimental and specialized. Do not create an “implement meta-optimization” issue; ask for a concrete usability, cancellation, generality or documentation gap before proposing further work. The scenario was inspected, not rerun.

### B46 — Apply glossary terminology (line 77)

**Rewrite; bounded naming inventory.** [The glossary][glossary] and current operator terminology have changed substantially. Replacement: “List specific remaining public names that conflict with canonical terms, with a proposed mapping and callers affected.” Keep namespace/folder changes out of that issue unless deliberately included. Avoid a blanket rename license.

### B47 — Population topology versus metadata example (line 78)

**Needs decision; candidate example under B39/B40.** [IslandPopulation][islands] and [AlpsState][alps] already model topology and ages in different structures. Name the example's teaching goal and desired user operations before drafting it. It may be one acceptance example for island/ALPS work rather than another issue.

### B48 — Completion-result object (line 79)

**Restructure applied 2026-10-24:** Merged into B01/B50 lifecycle/results/naming; no completion-result type is preapproved.

**Keep; merge with B50 into one design investigation.** [AlgorithmRun][run] exposes lifecycle separately from yielded/final states. A typed result/stop-reason model remains a real question, but cancellation, failure, pause and terminal completion must retain distinct semantics. Compare plain states plus run metadata with a separate result object; no new result type is preapproved.

### B49 — Encoded-problem adapter (line 80)

**Keep as a conditional design candidate.** Existing [problem contracts][problem-contract] associate a candidate with its search space, and the inspected code does not establish the proposed general decoding adapter. Retain the grammatical-evolution example as the requirement to develop. Do not add a phenotype parameter throughout the library as an incidental implementation choice.

### B50 — Run/resume naming and typed stop reasons (line 81)

**Restructure applied 2026-10-24:** Merged into B01/B48; lifecycle semantics precede naming choices.

**Rewrite; merge result semantics with B48, then review names.** [Run tests][run-tests] already distinguish paused continuation from terminal behavior. Replacement: “Settle lifecycle and result semantics with B48, then assess whether current public names miscommunicate them.” Naming alone should not drive new execution machinery.

### B51 — Cross-cutting authoring architecture tests (line 82)

**Refreshed and applied 2026-10-24; inventory unenforced accepted rules.** [RoleContractArityTests][arity-tests], [layer tests][layer-tests] and existing analyzers already enforce parts of API shape and ownership. Operators build with `CreateExecutor`, while [PipelineAlgorithm][pipeline] retains public `CreateExecutorFactory`; the historical protected-creation test is not a current rule to restore mechanically. The backlog retains reflection for compiled shape, Roslyn for source ownership and a demonstrated-need threshold for another test library, without duplicating the proposed inference-helper analyzer.

### B52 — Inference-helper analyzer (line 83)

**Restructure applied 2026-10-24:** Marked analyzer generation as a proposal requiring inferability, false-positive and safe-output criteria.

**Keep as proposed tooling; requirements need acceptance.** [OperatorAuthoringAnalyzer][authoring-analyzer] checks state and mutation rules; [ExecutorFactoryAnalyzer][factory-analyzer] checks child resolution. Neither implements the requested inference-helper rule. Define when a helper is inferable and useful, false-positive cases and safe fix output before implementation. Respect current generic operation slots and § 8.6 rather than generating companions mechanically.

### B53 — Composite construction inference (line 84)

**Restructure applied 2026-10-24:** Required current caller examples and comparison with inference helpers before a representation redesign.

**Keep; focused public-API design issue.** [CompositeSearchSpace][composite-space] still spells four candidate/space parameters and [CompositeGenotype][composite-genotype] spells its part types. Start with a representative call site and compare inference helpers against a larger representation redesign. Preserve static relationships; no untyped collection workaround is justified by this entry.

### B54 — Scalar problem batching ownership (line 85)

**Restructure applied 2026-10-24:** Merged into B06 as its scalar-problem batching case; no relocation to ProblemEvaluator selected.

**Keep, but consolidate with B06.** [SingleSolutionProblem][single-problem] still has `Concurrency` and delegates scalar evaluation through `BatchExecution`. It is a concrete case for the run-policy investigation, not an independent mandate to move batching into `ProblemEvaluator`.

### B55 — Operator authoring-base names (line 86)

**Restructure applied 2026-10-24:** Required a concrete confusion against the current hierarchy; historical names remain proposals.

**Rewrite; reassess against the accepted authoring hierarchy.** [Guidelines § 8.4][guidelines] and [SingleCandidateMutator][single-mutator] define current reduced-arity and single-candidate bases. Replacement: “Identify a concrete authoring confusion between the batch role and single-candidate convenience base, then compare names with the current hierarchy.” Do not publish the old vague “problem operator base classes” phrase as a settled naming direction.

### B56 — Test-suite organization and conventions (line 87)

**Restructure applied 2026-10-24:** Split into conventions, expensive-test policy and coverage reporting, preserving settled suite responsibilities and B22 as a bounded fix.

**Rewrite and split.** [The test guide][test-guide] already settles unit/scenario/API/docs placement; [CI][ci] already runs testing after build. Remaining concerns include inconsistent assertions, commented tests (B22), expensive-test policy and coverage reporting. Replacement: “Audit concrete convention violations; separately decide expensive-test execution and establish useful coverage reporting.” Do not restart the settled suite split or mandate a tool without a use case. A fresh inventory is needed for explicit-test counts.

### B57 — HeuristicLab operator migration (line 88)

**Restructure applied 2026-10-24:** Made inventory refresh precede migration batches; existing Boolean operators and maintained expression types invalidate old absence/mapping claims.

**Rewrite; retain the program, refresh its ledger first.** [The overview](heuristiclab_operator_migration_overview.md) says Boolean encoding operators are absent, while [RandomBoolVectorCreator][bool-creator] and other Boolean operators exist. Its symbolic-expression mapping also predates the maintained expression model. Replacement: “Reconcile operator/test mappings with current code and deliberate omissions, then select the next migration batch.” Do not translate stale table rows directly into issues or treat class existence as behavior parity.

### B58 — Python package bootstrap (line 89)

**Keep; packaging design/implementation candidate.** [Python samples README][python-readme] describes local builds and integrations; the inspected Python tree has no package bootstrap implementing the proposed NuGet publish/cache flow. Record version matching, prerequisites, cache invalidation and wheel scope before implementation. Keep the bootstrap and later wheel distribution independently deliverable.

### B59 — Optional dependency extraction (line 90)

**Restructure applied 2026-10-24:** Retained the consumer-demand trigger and corrected the Parlot-import obstacle.

**Rewrite; defer absent consumer demand.** [The core project][core-project] still references CsvHelper, Parlot and MathNet; [HyperVolumeCalculator][hypervolume] still uses MathNet. However, [InfixExpressionParser][parser] now imports Parlot locally, so the project-wide-import obstacle is stale. Replacement: “When a consumer needs a lighter package, inventory current dependency ownership and compare a coherent split with the status quo.” Preserve the warning against making internal fitting APIs public merely to satisfy a package split.

### B60 — Ownership-taking candidate factories (line 91)

**Cleanup applied 2026-10-24:** removed the completed deliverable after rechecking all four factory declarations and real-vector random-generation adoption. The broader B11 audit remains open.

**Resolved; no new issue for the stated deliverable.** [RealVector][real-vector], [IntegerVector][integer-vector], [BoolVector][bool-vector] and [Permutation][permutation] expose `FromOwnedArray`; random generation uses it, for example [RealVectorRandomExtensions][real-random]. The vector test selection passed 244 tests, including ownership tests; permutation ownership tests were inspected. Any additional container must be named as a new residual gap. This does not close B11's broader immutability audit.

### B61 — Caller-provided operator output storage (line 92)

**Keep; conditional design question, not immediate implementation.** Current named operation contracts such as [IMutationExecutor][mutation-contract] return result collections. Existing `Span<T>` helpers elsewhere do not establish a library-wide ownership contract. Require a measured allocation-sensitive operator workflow first, and preserve immutable candidate ownership semantics.

### B62 — Lightweight batching overhead (line 93)

**Restructure applied 2026-10-24:** Replaced the captured-delegate premise with current batching/RNG/allocation measurements; preserved determinism requirements.

**Rewrite; benchmark current mechanisms.** [SingleCandidateMutator][single-mutator] already uses static callbacks with explicit state, so the captured-delegate premise is outdated on that path. Per-item RNG forks and result allocation still need measurement through [BatchExecution][batch]. Replacement: “Measure lightweight batching and RNG-fork cost on the current implementation; evaluate fixed-width planning only for proven draw-count contracts with identical deterministic semantics.” Link [benchmark infrastructure][benchmark-plan] and B63; do not claim a measured regression here.

### B63 — Raw RNG primitives and bounded integers (line 94)

**Keep; concrete design/algorithm issue.** [IRandomNumberGenerator][rng-contract] exposes `NextInt`/`NextDouble`, while [bounded integer helpers][rng-extensions] still scale `NextDouble`. Engines have additional private primitives, which are not a public raw-width contract. Preserve statistical correctness, overflow/range behavior and reproducibility requirements when choosing integer-domain sampling.

### B64 — Problem-dependent validation before execution (line 95)

**Keep; architecture investigation with a concrete example.** [NumericParameterFittingRefiner][fitting-refiner] attempts fitting when each candidate is encountered and reports unsupported expressions then. Define the validation boundary and what can be checked before draws/evaluation, including delayed child graphs. A broad lifecycle hook is not automatically warranted by this one consumer.

### B65 — Cancellation reaching operators (line 96)

**Restructure applied 2026-10-24:** Updated resolution vocabulary and retained cancellation separately from concurrency policy.

**Keep; concrete architecture issue.** [Mutation][mutation-contract] and [refinement][refinement-contract] contracts represent operations without cancellation tokens; [NumericParameterFitter][fitter] accepts a token, but [the refiner][fitting-refiner] calls it without supplying one. Preserve this as the motivating long-running operation. Coordinate B06's run-state transport decision without assuming every short operation needs a token parameter.

### B66 — Terminator random streams across placements (line 97)

**Keep; reproducibility contract issue.** [IterativeAlgorithm][iterative] passes the iteration generator after step/interception, while [StateTerminatedAlgorithm][state-terminated] forks a dedicated termination stream and then per-state streams. The mismatch remains visible. Require the drawing-terminator equivalence example, nested-operator policy and preserved independence before accepting a general stream scheme. Built-in draw-free terminators do not establish placement equivalence for external terminators.

### B67 — Operator boilerplate and historical evaluator measurement (lines 98–115)

**Refreshed and applied 2026-10-24; rebaseline residual work.** [IOperator][operator-contract], per-operation invocations such as [Mutation][mutation-operation], and the accepted [operator plan][operator-plan] implement substantial unification. Old role/instrumentation counts cannot describe the current maintenance burden. The active backlog now requests a representative current change-surface measurement. The historical account below is preserved as evidence, not a fresh benchmark or redesign authorization.

### Historical evaluator-change evidence (B67)

Moved from the backlog on 2026-10-24. The following account predates the current operator unification; its counts were not remeasured and its source commit remains to be identified. The original wording is retained as historical evidence.

The evaluator contract change from `EvaluatedCandidate<TCandidate>` to `ObjectiveVector` (RF-2 of the symbolic-regression parameter-fitting work; see [symbolic-regression decisions](symbolic-regression-decisions.md)) is the clearest measurement available so far. It changed the single most fundamental property of an operator role — what its executor returns — and is worth reading from two directions at once, because the pair is the actual finding.

**How far it spread.** The change touched 55 files, +255/-393. Split by whether a file has any semantic stake in what an evaluator does:

| Area                                                                  | Files | Lines     |
| --------------------------------------------------------------------- | ----- | --------- |
| Evaluator role: contract, arity ladders, topologies                   | 14    | +68/-174  |
| Surrounding algorithms, analysis hooks, experimental operators, tests | 38    | +147/-213 |

About three quarters of the affected files changed only because a signature moved.

**How little it reached.** Inside the evaluator role itself, roughly 6% of the source changed. The role spans about 1080 lines; the return-type change touched 68 added and 106 removed lines, and about 30 of those were the identical one-line signature edit repeated across `Evaluator`, `StatelessEvaluator`, `StatefulEvaluator`, `SingleCandidateEvaluator`, the wrapping and multi topologies, and the instrumentation pair. Per file it is starker still: `CountingEvaluator` and `DurationMeasuringEvaluator` changed one line each out of 70 and 86, and `RelativeQualityEvaluator` two out of 108.

Genuine design work amounted to four edits: removing `RepeatingEvaluator.CandidateComparer`, deleting `IteratedEvaluator`, dropping `CachingEvaluator`'s candidate-substitution behavior, and adding the batch pairing helper. Everything else was mechanical.

Taken together: the change was broad but shallow. The ~94% of role source that survived untouched — constructors, generic constraint lists, arity re-declarations, factory companions, fluent helpers, nested execution-node classes — is exactly the code that never expressed what an evaluator does, and the 38 outside files are coupled to the role's signature rather than its behavior. Any future proposal should be measured against this shape: it must shrink the mechanical fraction without reintroducing a generic invocation path or a generated public API, and handwritten source must remain fully supported. Record the commit hash here once this work is committed so the measurement can be re-derived.

### B68 — Repeated invocation call context (added after the original review)

**Reviewed 2026-10-24; conditional follow-up.** The accepted sixth review in the [operator plan][operator-plan] keeps `Random`, `SearchSpace` and `Problem` on each operation's private invocation struct. [Mutation][mutation-operation], [IInvocation](../src/HeuristicLib/Execution/IInvocation.cs) and developer guidelines § 4.19 match that decision. The backlog retains the possible class-invocation measurement as evidence gathering only: it does not approve a context carrier, allocation on every call or a new role invocation mechanism. Keep its duplication concern related to B67 without treating the accepted implementation as unfinished migration work.

## Rejected and superseded decisions

These records should normally survive as decision history, not become open implementation issues. A current name or implementation paragraph may need rewriting without invalidating the original rejection. Historical benchmark numbers and compiler probes below were not rerun; retain their scope and link original evidence before publication.

| ID / source line | Entry                                                   | Recommendation and current evidence                                                                                                                                                                                                                                                                                                                                                                                                                                                                              |
| ---------------- | ------------------------------------------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| R01 / 119        | Binding as public runtime family                        | **Rewrite current terminology; retain rejection.** `IExecutor` remains, but role contracts now use names such as `IMutationExecutor`, not the entry's `...Execution` family. Link the current [glossary][glossary].                                                                                                                                                                                                                                                                                              |
| R02 / 123        | Prohibiting child-scope module installation             | **Keep rationale; modernize modules to aspects where describing current behavior.** [ResolutionScope][scope] supports child-local aspects and shared prepared ownership.                                                                                                                                                                                                                                                                                                                                         |
| R03 / 129        | Fixed composite bindings with rejection                 | **Keep as superseded.** [Advice tests][advice-tests] verify new contextual children with preserved shared state. Do not restore rejection of legitimate contextual resolution.                                                                                                                                                                                                                                                                                                                                   |
| R04 / 135        | Implicit source resolution for generated wrappers       | **Keep historical.** [Named][named] and invocation factories use explicit child wrapping; historical source-redirection code is not the current authoring model.                                                                                                                                                                                                                                                                                                                                                 |
| R05 / 141        | Supplying inner execution through every factory         | **Refreshed 2026-10-24; keep historical.** Preserve rejection of the optional supplied-execution protocol. Operators now use [CreateExecutor][operator-contract]; algorithms/own roles retain factories under the accepted staged [operator plan][operator-plan]. Avoid universal claims about all factory paths.                                                                                                                                                                                                |
| R06 / 147        | Separate ordinary/supplied callbacks                    | **Keep historical.** Preserve why dual callbacks were rejected; do not turn the selected callback shape at that date into a permanent contract across later operator factories.                                                                                                                                                                                                                                                                                                                                  |
| R07 / 153        | Separate advice declarations and aspects                | **Keep.** [Aspect][aspect] stores pointcut/advice pairs; no extra public declaration type is required by the current model.                                                                                                                                                                                                                                                                                                                                                                                      |
| R08 / 159        | Separate direct-advice/installed-aspect collections     | **Keep.** [Aspect][aspect], scope collection and [ordering tests][advice-tests] support explicit attachment order and deduplication.                                                                                                                                                                                                                                                                                                                                                                             |
| R09 / 165        | Composite aspects                                       | **Keep.** Current aspects expose a flat list; no evidence here justifies a public hierarchy with recursive flattening.                                                                                                                                                                                                                                                                                                                                                                                           |
| R10 / 171        | Advice as wrapper configurations                        | **Keep, with precise scope.** [Advice][advice] wraps executions; configured [Retry][retry] and other wrappers remain ordinary operators. The rejection is not a ban on all configured wrappers.                                                                                                                                                                                                                                                                                                                  |
| R11 / 177        | Static advice hooks and factory objects                 | **Refreshed 2026-10-24; partly superseded history.** `OperatorExecutorFactory` belonged to an intermediate implementation and is absent from current production source. Operators now build directly; algorithms/own roles retain factories. Preserve static variant-interface objections separately and use the accepted [operator plan][operator-plan] for remaining gates.                                                                                                                                    |
| R12 / 185        | Advice-based configured wrappers                        | **Keep historical.** There is no basis here to reopen the proposed `AdvisedMutator` API. Avoid treating the historical instrumentation/operator division as proof that no future behavior could ever need both forms.                                                                                                                                                                                                                                                                                            |
| R13 / 191        | Resolver-owned keyed child retention                    | **Keep.** [ChildScopeSelector][child-selector] owns Fresh/Retained/Shared policy; preserve separation between retained state and current requesting aspects.                                                                                                                                                                                                                                                                                                                                                     |
| R14 / 197        | Generated wrappers with extra children                  | **Keep.** No new need for advice-owned child resolution was established; configured operators remain the explicit composition route.                                                                                                                                                                                                                                                                                                                                                                             |
| R15 / 203        | Per-context node caches                                 | **Keep, qualify scope.** [ResolutionScope][scope] retains preparation/state rather than a general cache of bound nodes/faults. Other preparation caches do not contradict this narrower rejection. Reopening needs a current measurement.                                                                                                                                                                                                                                                                        |
| R16 / 209        | Shared configurable RunHost                             | **Keep.** [AlgorithmRun][run] and [ExperimentRun][experiment-run] retain different lifecycle responsibilities; generic state names do not justify a host.                                                                                                                                                                                                                                                                                                                                                        |
| R17 / 219        | Renaming Interceptor for AOP                            | **Keep.** [IterativeAlgorithm][iterative] still owns the step/intercept/termination/yield boundary. Link the separate B34 replacement question.                                                                                                                                                                                                                                                                                                                                                                  |
| R18 / 225        | Internally derived selectable sources                   | **Rewrite names; keep decision.** Replace historical `ChooseOneMutator` as a current example with current generic choice/router terminology. [MutatorSelectionTests][selection-tests] covers configured-source selection and rates. Preserve historical names within the dated account.                                                                                                                                                                                                                          |
| R19 / 239        | Node-selector Where                                     | **Rewrite current terminology; keep reasoning.** Selection is now [Pointcut][pointcut]; Boolean composition remains the relevant concept. Do not imply an active public NodeSelector API is still the baseline.                                                                                                                                                                                                                                                                                                  |
| R20 / 243        | Policy engine and retry-specific adapter hooks          | **Keep.** [IInvocationWrapper][invocation] and [Retry][retry] express the selected mechanism. “RetryWrapper” in a current implementation is not itself the rejected adapter-hook architecture.                                                                                                                                                                                                                                                                                                                   |
| R21 / 247        | Delegate wrappers/carriers/advice preparation           | **Rewrite with scoped current claims.** Struct invocation wrappers are present, but [Advice][advice] and [Aspect][aspect] still participate in typed advice preparation. Distinguish operator-invocation advice from algorithm/other typed advice before generalizing the historical ownership consequences.                                                                                                                                                                                                     |
| R22 / 253        | Typed operator invocation                               | **Rewrite the current-baseline paragraph; retain historical failure.** `IOperator<TExecutor>` is not the current consuming contract; [IOperator<TOperation, TCandidate>][operator-contract] and generic invocation plumbing exist while executors retain named methods. Clearly distinguish rejection of a universal public role method from accepted generic wrapping.                                                                                                                                          |
| R23 / 269        | Candidate-work capability interface                     | **Keep.** [Invocation/adapter contracts][invocation] retain defaults and candidate-work checks. No new requirement was found for the extra interface.                                                                                                                                                                                                                                                                                                                                                            |
| R24 / 273        | Cast-free ProceedWith                                   | **Refreshed 2026-10-24; retain tradeoff.** Private per-operation invocations such as [Mutation][mutation-operation] implement `IInvocation`; the shared `OperationExecutorAdapter` is gone. Preserve the historical cast/dispatch tradeoff rather than naming a removed current owner.                                                                                                                                                                                                                           |
| R25 / 277        | Builders, witnesses and value-dependent recommendations | **Keep.** [Guidelines § 8.6][guidelines] and [recommendations][permutation-space] match the selected construction distinction. Preserve value-dependent recommendations as possible, not mandatory.                                                                                                                                                                                                                                                                                                              |
| R26 / 289        | Generated operator families                             | **Keep historical rejection.** Ordinary role source remains; B67 should remeasure maintenance cost before any explicit reopening. Do not recreate generator experiments as migration work.                                                                                                                                                                                                                                                                                                                       |
| R27 / 301        | Parallel observation systems                            | **Refreshed 2026-10-24; retain rejection.** [ObservationSource][observations] and [Analyzer][analyzer] use typed observation records and functions. Preserve rejection of a parallel attachment system and an overload-only capability hierarchy; readings-based descriptions are intermediate history.                                                                                                                                                                                                          |
| R28 / 309        | Type-erased operator observations                       | **Keep, update current selection terminology if needed.** [MutationObservation][mutation-observation] exposes typed data. Historical measurements and interface names must remain clearly dated.                                                                                                                                                                                                                                                                                                                 |
| R29 / 315        | Per-operation trace overloads                           | **Keep.** [Analyzer][analyzer] consumes generic handles and [MutationObservations][mutation-observation] exposes `Calls`; the old overload count is evidence from that proposal, not a current API count.                                                                                                                                                                                                                                                                                                        |
| R30 / 319        | Input/result types on descriptors                       | **Keep reasoning; reconcile selected adapter description with R34.** [Mutation][mutation-operation] is candidate-generic only in its internal execution machinery. The historical language limitation is not permission to add descriptor generics now.                                                                                                                                                                                                                                                          |
| R31 / 323        | IObservable / Rx operators                              | **Refreshed 2026-10-24; retain rejection.** Sources deliver records and traces read them through functions; `Readings<T>` and `Observations.Read` are no longer current source APIs. Preserve attachment/lifecycle objections to Rx; the old source-model conflict with R33 no longer holds.                                                                                                                                                                                                                     |
| R32 / 329        | Measurements as strategies                              | **Refreshed 2026-10-24; retain rejection.** Standalone measurement strategies and readings source types are absent from current production source. [Analyzer][analyzer], [Ranking][aggregations] and ordinary functions replace them. Keep the historical removal rationale without describing its intermediate readings design as current.                                                                                                                                                                      |
| R33 / 333        | Readings/projections on sources                         | **Reconciled 2026-10-24.** The original review saw the intermediate readings implementation; current production source has no `Readings<T>` or `IProblemObservation`. [ObservationSource][observations] delivers records and [Analyzer][analyzer] reads them through functions; [Ranking][aggregations] takes explicit objective directions. The removal matches the accepted fifth/sixth reviews. Corrected only the backlog record’s present-tense ranking sentence; rejection and reopening condition remain. |
| R34 / 339        | Operation arguments behind shared invocation            | **Reconciled 2026-10-24.** [Mutation][mutation-operation] owns a private `Invocation` implementing `IInvocation`; operation-argument interfaces and `OperationExecutorAdapter` are absent from current production source. This matches the accepted invocation changes; the original checkout mismatch no longer holds.                                                                                                                                                                                          |
| R35 / 343        | Untyped execution state on configurations               | **Reconciled 2026-10-24 with staged scope.** Operators build through [CreateExecutor][operator-contract] and request state via [scope.GetExecutionState(this, create)][scope]. `OperatorExecutorFactory` is absent. Algorithm/own-role factories remain as specified by the accepted plan; their presence does not contradict the operator change or the rejection of untyped configuration state.                                                                                                               |
| R36 / 347        | Cached neighborhood operators                           | **Keep.** [Neighborhood][neighborhood] returns itself for move creation/application/evaluation, matching the chosen alternative. Preserve the record-equality/copy rationale.                                                                                                                                                                                                                                                                                                                                    |
| R37 / 355        | Trace storage structures                                | **Keep historical evidence.** [TraceAnalyzer][trace] uses locked list storage and snapshot copying. Its reported timing/allocation numbers are historical measurements, not validation of this revision.                                                                                                                                                                                                                                                                                                         |
| R38 / 363        | Detecting never-resolved observation sources            | **Keep, modernize terminology.** Reference matching and deferred activation remain; an opt-in expectation is still needed before empty selection becomes an error. Do not reintroduce per-module flags incidentally.                                                                                                                                                                                                                                                                                             |
| R39 / 371        | Analyzer cleanup contracts                              | **Keep historical.** Current trace/analyzer attachment does not establish a new external-resource lifetime requirement. The review found no concrete reason to reopen it.                                                                                                                                                                                                                                                                                                                                        |
| R40 / 379        | Layer namespaces/folders/assemblies                     | **Keep reasoning; recheck current dependency examples before publication.** [Layer tests][layer-tests] and [guidelines][guidelines] preserve concept-oriented organization. Later factory changes may alter individual dependency examples without creating a separately served audience or justifying a package split.                                                                                                                                                                                          |

The explanatory paragraphs at source lines 215–217 belong at the start of the rejected-decisions section; currently they appear after RunHost and can look like part of that decision. “Source material” remains navigation, not issue content. “Keep or delete?” should eventually defer to the accepted GitHub migration plan rather than retain the older suggestion that concrete spec files are an alternative destination. These are organization recommendations, not additional backlog issues.

## Recommended issue selection and relationships

Review dispositions before drafting more issues. Use these as proposed groupings, not a promise to create one issue per identifier:

- **Concrete maintenance/correctness candidates:** B19 (file casing), B22 (misleading specs), B25 (genealogy collisions), B36 (similarity edge cases). B25 needs identity semantics before a fix; B36 needs its numerical contract settled.
- **Focused design candidates:** B02 serialization, B06/B54 execution policy with B65 cancellation, B23 equivalence, B27/B28 pipeline transitions, B48/B50 lifecycle/results, B63 RNG sampling, B64 validation and B66 stream placement. Each still needs explicit acceptance of any new architecture.
- **Refresh existing programs rather than duplicate them:** B08 mating, B21 documentation validation, B30 objectives, B31 analysis catalog, B57 operator migration and B67 unification follow-up.
- **Do not create new open implementation issues for completed content:** B14 and B60; B45 only if a concrete residual gap is established. B17 and B42 have been merged into their owning entries.
- **Keep conditional ideas in relevant parent context:** B04, B07, B12, B13, B37, B38, B59 and B61. Creating an issue solely to restate “only if needed” would inflate the backlog without defining work.
- **Preserve rejected history in related owning issues:** group resolver/aspect decisions, operator-invocation decisions, observation/trace decisions and authoring decisions by context. Use separate closed issues only for decisions that need independent discovery. R33–R35 were reconciled in this pass; use their refreshed implementation scope.

Relationship recommendations:

| Relationship                                          | Kind                                           | Reason                                                                                                                                  |
| ----------------------------------------------------- | ---------------------------------------------- | --------------------------------------------------------------------------------------------------------------------------------------- |
| B48 + B50, with B01 context                           | One investigation                              | Result semantics and lifecycle names overlap; setup-type changes are an option, not a prerequisite.                                     |
| B06 + B54; B65 coordinated                            | Shared design context                          | Concurrency and cancellation both need explicit run-state ownership, but neither transport choice is accepted yet.                      |
| B27 + B28                                             | One outcome or parent/child                    | Typed pipeline transitions must include same-type transformations.                                                                      |
| B25 + B35; B23 consulted                              | Shared identity decision                       | Value equivalence and derivation occurrences differ. A large provenance framework must not automatically block a focused genealogy fix. |
| Benchmark infrastructure → B32/B33/B62 measurements   | Prerequisite when measurements use that runner | Share infrastructure and methodology; do not assume results transfer between workloads.                                                 |
| B63 ↔ B62/B66                                         | Design coordination                            | Sampling primitives, draw consumption and stream placement interact; no blanket implementation dependency is yet established.           |
| B02 ↔ B34 persisted aspects                           | Related requirement                            | Serialization must say whether aspects are included; runtime advice persistence is not automatically required.                          |
| B11 ↔ B60                                             | Completed work informs broader audit           | Ownership factories do not prove all candidate representations immutable.                                                               |
| B21 includes B14 validation; B56 includes B22 cleanup | Scope containment                              | Prevent duplicate documentation/test tasks while allowing independently reviewable fixes.                                               |
| R33–R35 refreshed status → related issue drafts       | Verified source context                        | Use the 2026-10-24 operator/algorithm distinction; recheck if source changes before publication.                                        |

## Suggested next review decisions

1. Select which vague and conditional entries represent work we actually want; identify a concrete scenario or reconsideration trigger before issue drafting. Their retention in the reorganized backlog is not acceptance.
2. Resolve the explicit requirements and acceptance questions, especially mating/count policy, lifecycle/results, configured controls and the proposed equivalence contract. Prioritize concrete correctness needs without making them depend on whole architecture programs.
3. Decide which selected concerns warrant independent issues versus a checklist or supporting context. Apply that threshold to small maintenance tasks as well as large investigations; splitting backlog descriptions does not mandate more issues.
4. Draft selected issue content locally before publication. Use the refreshed R33–R35 status and preserve accepted scope, historical evidence and remaining staged work. The GitHub migration pilot/handover requirements still apply.

The edited backlog remains authoritative. This report records applied cleanup, restructuring, remaining recommendations and evidence; it is not a replacement implementation plan.

[guidelines]: ../docs/contributing/developer-guidelines.md
[glossary]: ../docs/guide/glossary.md
[test-guide]: ../test/README.md
[ci]: ../.github/workflows/ci.yml
[core-project]: ../src/HeuristicLib/HeuristicLib.csproj
[run]: ../src/HeuristicLib/Execution/Runs/AlgorithmRun.cs
[run-tests]: ../test/HeuristicLib.Tests/Execution/AlgorithmRunTests.cs
[experiment-run]: ../src/HeuristicLib/Experiments/ExperimentRun.cs
[grid]: ../src/HeuristicLib/Experiments/Grid.cs
[grid-experiment]: ../src/HeuristicLib/Experiments/GridExperiment.cs
[materialization-tests]: ../test/HeuristicLib.Tests/Experiments/ExperimentMaterializationTests.cs
[experiment-specs]: ../test/HeuristicLib.Tests.ApiUsageSpecs/Experiments/ExperimentSpecs.cs
[experiments-doc]: ../docs/guide/execution/experiments.md
[scope]: ../src/HeuristicLib/Execution/ResolutionScope.cs
[scope-tests]: ../test/HeuristicLib.Tests/Execution/ResolutionScopeTests.cs
[factory-lifetimes]: ../test/HeuristicLib.Tests/Execution/ExecutorFactoryResolutionTests.Lifetimes.cs
[single-mutator]: ../src/HeuristicLib/Operators/Mutators/BaseClasses/SingleCandidateMutator.cs
[single-problem]: ../src/HeuristicLib/Problems/SingleSolutionProblem.cs
[batch]: ../src/HeuristicLib/Execution/Concurrency/BatchExecution.cs
[controls]: ../src/HeuristicLib/Execution/ExecutorControls.cs
[mutation-observation]: ../src/HeuristicLib/Operators/Mutators/MutationObservation.cs
[mutation-operation]: ../src/HeuristicLib/Operators/Mutators/Mutation.cs
[mutation-contract]: ../src/HeuristicLib/Operators/Mutators/IMutationExecutor.cs
[refinement-contract]: ../src/HeuristicLib/Operators/Refiners/IRefinementExecutor.cs
[named]: ../src/HeuristicLib/Operators/Composition/Named.cs
[advice-tests]: ../test/HeuristicLib.Tests/Execution/OperatorFoundation/OperatorAdviceTests.cs
[binding-tests]: ../test/HeuristicLib.Tests/Execution/OperatorFoundation/OperationBindingTests.cs
[composition-doc]: ../docs/guide/extending/operator-composition.md
[permutation-space]: ../src/HeuristicLib/Encodings/Permutations/PermutationSearchSpace.cs
[tsp]: ../src/HeuristicLib/Problems/TravelingSalesman/TravelingSalesmanProblem.cs
[legacy-nodes]: ../src/HeuristicLib.Experimental/Encodings/LegacySymbolicExpressions/Model/SymbolicExpressionTreeNode.cs
[assembly-tests]: ../test/HeuristicLib.Tests/Architecture/AssemblyDependencyTests.cs
[sr-specs]: ../test/HeuristicLib.Tests.ApiUsageSpecs/MachineLearning/Supervised/Regression/SymbolicRegressionRedesignSpecs.cs
[numeric-specs]: ../test/HeuristicLib.Tests.ApiUsageSpecs/MachineLearning/Supervised/Regression/NumericParameterFittingSpecs.cs
[fitting-refiner]: ../src/HeuristicLib/Problems/MachineLearning/Supervised/Regression/Operators/NumericParameterFittingRefiner.cs
[fitter]: ../src/HeuristicLib/MachineLearning/Supervised/Regression/Symbolic/NumericParameterFitter.cs
[duplicates]: ../src/HeuristicLib/Operators/Interceptors/RemoveDuplicatesInterceptor.cs
[problem-contract]: ../src/HeuristicLib/Problems/IProblem.cs
[space-contract]: ../src/HeuristicLib/SearchSpaces/ISearchSpace.cs
[analyzer]: ../src/HeuristicLib/Analysis/Tracing/Analyzer.cs
[run-traces]: ../src/HeuristicLib/Analysis/Tracing/RunTraceExtensions.cs
[genealogy]: ../src/HeuristicLib.Experimental/Analysis/GenealogyAnalysis/GenealogyGraph.cs
[objectives]: ../src/HeuristicLib/Objectives/Objective.cs
[multi-objective]: ../src/HeuristicLib/Objectives/MultiObjective.cs
[objective-doc]: ../docs/guide/fundamentals/objectives.md
[analysis-tests]: ../test/HeuristicLib.Tests/Analysis/AnalysisUsabilityTests.cs
[pipeline]: ../src/HeuristicLib/Algorithms/Composition/PipelineAlgorithm.cs
[proportional]: ../src/HeuristicLib/Operators/Selectors/ProportionalSelector.cs
[rank-selector]: ../src/HeuristicLib/Operators/Selectors/LinearRankSelector.cs
[observations]: ../src/HeuristicLib/Inspection/ObservationSource.cs
[aggregations]: ../src/HeuristicLib/Analysis/Tracing/Ranking.cs
[operator-plan]: operator-role-parameterization-rework.md
[benchmark-plan]: performance-regression-benchmarking.md
[pointcut]: ../src/HeuristicLib/Execution/Aspects/Pointcut.cs
[advice]: ../src/HeuristicLib/Execution/Aspects/Advice.cs
[aspect]: ../src/HeuristicLib/Execution/Aspects/Aspect.cs
[retry]: ../src/HeuristicLib/Operators/Composition/Retry.cs
[retry-tests]: ../test/HeuristicLib.Tests/Operators/RetryOperatorTests.cs
[iterative]: ../src/HeuristicLib/Algorithms/BaseClasses/IterativeAlgorithm.cs
[choose-call]: ../src/HeuristicLib/Operators/Composition/ChoosePerCall.cs
[choose-item]: ../src/HeuristicLib/Operators/Composition/ChoosePerItem.cs
[similarity]: ../src/HeuristicLib.Experimental/Analysis/PopulationSimilarity.cs
[observation-requirements]: ../test/HeuristicLib.Tests.Experimental/Analysis/ObservationSourceRequirementTests.cs
[epoch-work]: ../src/HeuristicLib.Experimental/Analysis/Dynamic/EpochWork.cs
[trace]: ../src/HeuristicLib/Analysis/Tracing/TraceAnalyzer.cs
[retention]: ../src/HeuristicLib/Analysis/Tracing/TraceRetention.cs
[islands]: ../src/HeuristicLib.Experimental/Algorithms/SearchStates/IslandPopulation.cs
[alps]: ../src/HeuristicLib.Experimental/Algorithms/Evolutionary/ALPSGeneticAlgorithm.cs
[sr-scenario]: ../test/HeuristicLib.Tests.Scenarios/Core/Problems/MachineLearning/SymbolicRegressionVerticalSliceScenario.cs
[dynamic-problem]: ../src/HeuristicLib.Experimental/Problems/Dynamic/DynamicProblem.cs
[dynamic-scenario]: ../test/HeuristicLib.Tests.Scenarios/Problems/Dynamic/AutoEcPaperScenarioTests.cs
[dynamic-tests]: ../test/HeuristicLib.Tests.Experimental/Problems/Dynamic/Analysis/DynamicAnalysisTests.cs
[meta-problem]: ../src/HeuristicLib.Experimental/Problems/MetaOptimization/MetaOptimizationProblem.cs
[meta-tests]: ../test/HeuristicLib.Tests.Scenarios/Problems/MetaOptimization/MetaOptimizationTests.cs
[arity-tests]: ../test/HeuristicLib.Tests/Architecture/RoleContractArityTests.cs
[layer-tests]: ../test/HeuristicLib.Tests/Architecture/LayerDependencyTests.cs
[authoring-analyzer]: ../analyzers/HeuristicLib.Analyzers/OperatorAuthoringAnalyzer.cs
[factory-analyzer]: ../analyzers/HeuristicLib.Analyzers/ExecutorFactoryAnalyzer.cs
[composite-space]: ../src/HeuristicLib.Experimental/Encodings/Composite/CompositeSearchSpace.cs
[composite-genotype]: ../src/HeuristicLib.Experimental/Encodings/Composite/CompositeGenotype.cs
[bool-creator]: ../src/HeuristicLib/Encodings/BoolVectors/Creators/RandomBoolVectorCreator.cs
[python-readme]: ../python-samples/README.md
[hypervolume]: ../src/HeuristicLib/Objectives/HyperVolumeCalculator.cs
[parser]: ../src/HeuristicLib/Encodings/SymbolicExpressions/Authoring/InfixExpressionParser.cs
[real-vector]: ../src/HeuristicLib/Encodings/RealVectors/RealVector.cs
[integer-vector]: ../src/HeuristicLib/Encodings/IntegerVectors/IntegerVector.cs
[bool-vector]: ../src/HeuristicLib/Encodings/BoolVectors/BoolVector.cs
[permutation]: ../src/HeuristicLib/Encodings/Permutations/Permutation.cs
[real-random]: ../src/HeuristicLib/Encodings/RealVectors/RealVectorRandomExtensions.cs
[rng-contract]: ../src/HeuristicLib/Random/IRandomNumberGenerator.cs
[rng-extensions]: ../src/HeuristicLib/Random/RandomExtensions.cs
[state-terminated]: ../src/HeuristicLib/Algorithms/Control/StateTerminatedAlgorithm.cs
[operator-contract]: ../src/HeuristicLib/Operators/IOperator.cs
[child-selector]: ../src/HeuristicLib/Execution/ChildScopeSelector.cs
[selection-tests]: ../test/HeuristicLib.Tests/Execution/MutatorSelectionTests.cs
[invocation]: ../src/HeuristicLib/Execution/IInvocationWrapper.cs
[neighborhood]: ../src/HeuristicLib.Experimental/Operators/Neighborhoods/Neighborhood.cs
