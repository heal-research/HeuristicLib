# Core And Experimental Package Boundary

## Summary

This plan defines the intended boundary between `HEAL.HeuristicLib` and `HEAL.HeuristicLib.Experimental`.

The main package should contain the coherent conceptual spine of HeuristicLib and components that already follow the intended architecture. The experimental package should contain useful components whose public model, semantics or integration remain unresolved.

The experimental package is an incubation boundary. It is not a place to preserve broken public APIs or unfinished stubs. Experimental code may change substantially, but any publicly shipped behavior should still have a meaningful use case, explicit limitations and proportionate tests.

Mature optional domain functionality may eventually move from the experimental package into a dedicated package instead of the main package.

No source placement changes are part of this planning change.

## Motivation

HeuristicLib is in early alpha. Existing code and APIs are evidence rather than permanent commitments and current package boundaries are provisional.

The main package currently contains several feature families that do not yet satisfy its intended promise:

1. Some APIs still encode unresolved or legacy design choices.
2. Some components conflict with current ownership and immutability principles.
3. Some specialized domains add substantial public surface and package dependencies to ordinary core use.
4. Some research algorithms advertise semantics that their implementations do not yet provide.
5. Some analysis types are incomplete or are already exposed through experimental factories despite living in the main assembly.

Correcting the boundary during alpha lets users opt into these components intentionally and gives redesign work room to replace weak APIs.

## Goals

1. Define a repeatable admission policy for the main package.
2. Identify current feature families that should move to the experimental package.
3. Preserve a small coherent core that supports ordinary heuristic optimization.
4. Keep optional and unstable functionality usable while it is redesigned.
5. Avoid namespace churn when types move between the main and experimental assemblies.
6. Remove unnecessary dependencies from the main package where feature moves make that possible.
7. Define promotion criteria for moving components back into the main package.

## Non Goals

1. This plan does not redesign analysis, objective comparison, DataAnalysis or any algorithm.
2. This plan does not require every provisional API to move to the experimental package.
3. This plan does not make backward compatibility a current goal.
4. This plan does not use the experimental package as a substitute for deleting dead code.
5. This plan does not decide the final package structure for mature domain extensions.

## Placement Policy

### Main Package Admission

A component belongs in `HEAL.HeuristicLib` when all of the following are true:

1. Its responsibility belongs in the common HeuristicLib model or the standard toolkit expected by ordinary users.
2. Its API follows the current configuration, execution ownership and immutable candidate principles.
3. Its advertised behavior is meaningfully complete.
4. Its important invariants and representative usage are tested.
5. Its public shape is documented when the usage or lifecycle is not obvious.
6. The team does not already expect to replace its central abstraction.
7. Its dependencies are appropriate for ordinary users of the main package.

Missing future extensions do not by themselves make a component experimental. A narrow component can remain in the main package when its current contract is honest, coherent and complete.

### Experimental Package Admission

A component belongs in `HEAL.HeuristicLib.Experimental` when it has a real use case but one or more of the following apply:

1. Its public model is still being explored.
2. Its semantics are useful but not yet complete enough for the main package promise.
3. It is a research workflow or specialized integration with limited validation.
4. It is a legacy migration that has not yet been adapted to the modern HeuristicLib architecture.
5. It is an optional domain vertical that may later become a dedicated package.
6. It depends on another experimental component.

Experimental public APIs should state important limitations and should not silently claim semantics they do not implement.

### Code That Should Not Ship

The following cases should be removed, made internal or completed before publication:

1. Entirely commented out implementations.
2. Public analyzers or algorithms whose main operation is empty.
3. Types that expose a result which cannot be observed.
4. Known correctness defects without a bounded documented behavior.
5. Duplicate or wrongly named source files that obscure ownership.

Moving such code to the experimental assembly is not sufficient by itself.

### Future Dedicated Packages

A component may be mature and still not belong in the main package. A dedicated package is preferable when a feature family is cohesive, optional and dependency heavy.

DataAnalysis and symbolic regression are the clearest current candidates for this outcome. Benchmark suites and external system adapters may follow the same pattern later.

### Durable Policy Deliverable

The placement rules in this plan are proposed repository policy, not plan specific background. Before this plan is considered complete, the settled main package admission criteria, experimental package criteria and promotion criteria must be added to durable contributor guidance.

`docs/developer-guidelines.md` should become the canonical source for these criteria. Package facing documentation should summarize the user visible distinction and link to the canonical guidance where appropriate.

This plan must not be removed or archived until that documentation exists. Future placement and promotion decisions should cite the durable guidance rather than this plan.

## Proposed Main Package Boundary

The main package should retain the following areas:

1. Problem, search space, algorithm and operator contracts.
2. Configuration and execution instance infrastructure.
3. Runs, execution streams, cancellation and explicit randomness.
4. Core candidate representations such as bool, integer, permutation and real vectors.
5. Core search spaces for those candidate representations.
6. Population and single solution state types.
7. Objective value, objective vector and objective direction storage primitives.
8. Single objective comparison.
9. Standard algorithms whose current semantics are complete, including the genetic algorithm, evolution strategy and hill climber.
10. General operator roles and mature reusable operators.
11. Observable operator contracts and wrappers.
12. Basic instrumentation sinks such as `ObservationCounter` and `ObservationDuration`.
13. Counting and duration wrappers used by execution budgets.
14. Representative mature problems such as single objective test functions and Traveling Salesman.
15. Experiment and pipeline APIs whose current contracts remain honest and well tested.

This list is a target boundary rather than an exhaustive permanent inventory.

## Proposed Moves To Experimental

### DataAnalysis And Symbolic Regression

Move the complete `Problems/DataAnalysis` feature family into `HEAL.HeuristicLib.Experimental`.

Move related source that directly depends on DataAnalysis types, including the symbolic expression tree linear scaling extension.

Reasons:

1. DataAnalysis is a large optional domain vertical rather than a core optimization concept.
2. Its current problem data exposes mutable datasets, lists and partition dictionaries.
3. Several problem configurations expose mutable evaluator collections.
4. Symbolic regression parameter training during evaluation raises unresolved candidate ownership questions.
5. The feature family accounts for the AutoDiff dependency and much of the MathNet usage in the main package.
6. Existing Python workflows already depend on the experimental assembly.

Move the corresponding unit tests to `HeuristicLib.Tests.Experimental`. Keep broader Python and symbolic regression workflows in the scenario project.

Long term, redesign and promote this feature family into a dedicated DataAnalysis package if it becomes a coherent supported domain extension.

### Quadratic Assignment

Move the static Quadratic Assignment problem and its data contracts into `HEAL.HeuristicLib.Experimental`.

Reasons:

1. `QuadraticAssignmentProblemData` retains caller supplied matrices.
2. It exposes the matrices through public mutable array fields.
3. This conflicts with the snapshot semantics required for durable core values.
4. Existing dynamic Quadratic Assignment problems already live in the experimental assembly and depend on these types.

Promotion requires immutable problem data ownership, a representative usage story and a deliberate loading or construction API.

### Multi Objective Feature Family

Keep the minimal objective storage model in the main package:

1. `ObjectiveValue`
2. `ObjectiveVector`
3. `ObjectiveDirection`
4. The minimal direction collection needed by problem contracts
5. `EvaluatedCandidate`

Move the current multi objective policy and algorithm layer into `HEAL.HeuristicLib.Experimental`:

1. `NSGA2` and its builder
2. Pareto crowding selectors and replacers
3. Dominance sorting utilities
4. Pareto front utilities
5. Crowding distance
6. Hypervolume calculation
7. Hypervolume and Pareto analyzers
8. Multi objective test problem interfaces and implementations
9. ZDT functions
10. Multi objective factories and comparison strategies that depend on the unresolved objective model

Before implementation, audit methods such as dominance comparison that currently live directly on `ObjectiveVector`. Keep core objective types focused on storage and validated shape unless a comparison policy is sufficiently general and settled.

Promotion requires:

1. A settled distinction between objective directions, partial comparison and total ordering.
2. A short representative multi objective setup path.
3. Explicit algorithm requirements for objective count and comparison semantics.
4. Strong tests for equal objective vectors, invalid dimensions, mixed directions and ordering behavior.
5. API usage specs for at least one standard multi objective algorithm.

### Run Owned Analysis

Keep direct observability and instrumentation in the main package. Move the current run owned analyzer layer and concrete analyzers into `HEAL.HeuristicLib.Experimental`.

The target experimental area includes:

1. `IAnalyzer`
2. `IAnalyzerRunState`
3. `Analyzer<TResult>`
4. `ObservationPlan`
5. Analyzer attachment and result lookup
6. Trial analyzer integration
7. Quality history analyzers
8. Diversity analyzers
9. Pareto and hypervolume analyzers
10. Genealogy and rank analyzers

This move requires a design checkpoint because `AlgorithmRun` and `ExperimentRun` currently own analyzer attachment and observation installation. Do not implement it as a mechanical file move.

The design checkpoint must choose a small core boundary that lets the experimental analysis package arrange execution without exposing registry replacement machinery to ordinary users. Acceptable directions include an experimental analyzed run wrapper or a general core execution customization hook with no analysis specific semantics.

The current incomplete allele frequency analyzer should not be preserved as public experimental API until it has an observable result and implemented analysis behavior.

Promotion requires:

1. A desired state API usage story.
2. A settled lifecycle for attaching analyzers and retrieving results.
3. A public API that does not require ordinary users to understand execution registry replacement.
4. Correct behavior across nested algorithms and recreated child execution graphs.
5. Typed immutable result views where historical data is exposed.
6. Representative analysis API usage specs.

### Research And Incomplete Algorithms

Move the following into `HEAL.HeuristicLib.Experimental`:

1. `AlpsGeneticAlgorithm` and `AlpsState`
2. `OpenEndedRelevantAllelesPreservingGeneticAlgorithm` and its builder
3. `IslandPopulation`

The current ALPS implementation does not yet provide an age layered population structure. Its public name promises more than its behavior.

The open ended relevant alleles preserving algorithm is a specialized research algorithm with limited validation and documentation.

`IslandPopulation` should remain experimental until it participates in a complete island workflow. General population abstractions and any solution layout contract still required by mature core states may remain in the main package.

The entirely commented out parameterless population pyramid implementation should be deleted rather than moved.

## Areas That Remain In Core For Now

Do not move the following solely because their backlogs contain future improvements:

1. Experiment configurations and experiment runs.
2. Pipeline, cycle and budget meta algorithms.
3. Standard algorithm builders.
4. Traveling Salesman.
5. Single objective test functions.

These areas should remain under ordinary architecture review. A missing future capability is different from a dishonest or unresolved current contract.

The builder API may later be redesigned or removed. That decision should be made as one public authoring style discussion rather than by moving individual builders between packages.

## Implementation Sequence

### Stage 1: Record And Enforce The Boundary

1. Settle the main package admission, experimental package and promotion criteria proposed by this plan.
2. Add the settled criteria to `docs/developer-guidelines.md` as durable repository policy.
3. Update package documentation to state the main and experimental package promises.
4. Add an architecture test that prevents references from the main package or contracts package to the experimental package.
5. Add a lightweight inventory test or reviewed list for feature families intentionally owned by the experimental package.
6. Keep public namespaces stable where possible because both assemblies use the `HEAL.HeuristicLib` root namespace.

### Stage 2: Remove Code That Should Not Ship

1. Delete the commented parameterless population pyramid source.
2. Remove or make internal the incomplete allele frequency analyzer.
3. Correct wrongly named analysis source files while preserving intentional public namespaces.
4. Audit moved areas for other empty public implementations.

### Stage 3: Move Low Coupling Problem Families

1. Move Quadratic Assignment source and tests.
2. Move DataAnalysis and symbolic regression source and tests.
3. Move the linear scaling extension and any other direct DataAnalysis dependents.
4. Update experimental dynamic problems and Python workflows without changing their public namespaces unnecessarily.
5. Move AutoDiff and affected MathNet package references out of the main project when no main package usage remains.

### Stage 4: Move The Multi Objective Feature Family

1. Define the minimal objective primitives that must remain in contracts.
2. Move algorithms, operators, analyses, calculators and benchmark problems as one coherent cluster.
3. Move their unit tests to the experimental test project.
4. Update API usage specs so experimental examples clearly require the experimental package.
5. Remove any main package dependency that was required only by the moved feature family.

### Stage 5: Separate Run Owned Analysis

1. Complete the analysis design checkpoint.
2. Introduce the chosen core execution boundary if one is needed.
3. Remove analysis specific dependencies from core run contracts.
4. Move analyzer contracts, implementations and experiment analysis integration.
5. Preserve direct observable operators and instrumentation in core.
6. Update analysis documentation to identify the required package and current stability.

### Stage 6: Move Research Algorithms And Island Types

1. Move ALPS related types and tests.
2. Move the open ended relevant alleles preserving algorithm and tests.
3. Move island specific state types that have no mature core consumer.
4. Preserve general core contracts only where current mature types need them.

### Stage 7: Package And Documentation Cleanup

1. Update package descriptions and README package guidance.
2. Document experimental limitations for each retained feature family.
3. Audit NuGet dependencies after all moves.
4. Confirm that examples install the package that owns the APIs they use.
5. Add promotion checklists to the relevant feature backlog entries.
6. Confirm that durable contributor guidance contains the final placement and promotion criteria before closing this plan.

## Test And Validation Plan

During each implementation stage:

1. Run focused tests for the moved feature family.
2. Run the complete `HeuristicLib.Tests` project after meaningful core changes.
3. Run `HeuristicLib.Tests.Experimental` after each placement move.
4. Run `HeuristicLib.Tests.ApiUsageSpecs` when public package usage changes.
5. Run scenario tests once after DataAnalysis, analysis or Python workflow moves.
6. Build the complete solution in release configuration.
7. Run whitespace, style and analyzer verification.
8. Pack both main and experimental projects and inspect their dependency graphs.

Final acceptance criteria:

1. `HEAL.HeuristicLib.Contracts` and `HEAL.HeuristicLib` have no dependency on `HEAL.HeuristicLib.Experimental`.
2. The main package no longer exposes the feature families listed for movement.
3. The experimental package builds and exposes the moved types under intentional namespaces.
4. Core only users do not receive AutoDiff or another dependency used only by moved features.
5. All moved unit tests live in the experimental test project.
6. API usage specs make experimental package usage visible.
7. No public moved component is an empty implementation or inaccessible result container.
8. Main package documentation describes a coherent standard optimization toolkit.
9. Experimental package documentation describes instability and known limitations without implying low quality is acceptable.
10. `docs/developer-guidelines.md` contains the canonical criteria for admitting a component to the main package, placing it in the experimental package and promoting it later.
11. The documented criteria remain sufficient for future placement decisions without referring to this plan.

## Promotion Checklist

A feature family may move from experimental into the main package when:

1. Its public responsibility is clear and belongs in the common toolkit.
2. Its central API no longer has an unresolved redesign item.
3. Its ownership and immutability rules match the developer guidelines.
4. Its advertised semantics are complete.
5. Its normal setup path has an API usage spec.
6. Its important invariants and failure behavior have unit tests.
7. Its documentation states the intended extension points and limitations.
8. Its dependencies are appropriate for the main package.
9. Promotion does not force unrelated experimental concepts into core.

Mature optional domains should move to dedicated packages when that produces a clearer dependency and product boundary.

## Assumptions

1. Backward compatibility remains outside the current alpha goals.
2. Assembly moves may change binary identity even when namespaces remain stable.
3. The experimental package continues to reference the main package and never the reverse.
4. Python interop may depend on experimental features.
5. Test namespaces remain stable across promotion or demotion where practical.
6. Feature moves should be coherent clusters rather than isolated types that create circular concepts.
7. This plan may be implemented in several focused changes rather than one large change.
