# Example Spec Backlog

This file turns design work into concrete executable usage specs.

The intent is simple: examples come first, implementation changes follow.

## How to use this backlog

- Start with the smallest honest user example.
- Make the example compile and run against the current implementation family.
- Let the example expose friction directly.
- Refactor the existing implementation toward the example instead of inventing a second system.

## Current coverage

Already represented in executable specs:

- practitioner usage:
  [PractitionerUsageSpecs.cs](../test/HeuristicLib.ApiSpecs/PractitionerUsageSpecs.cs)
- researcher authoring:
  [ResearcherAuthoringSpecs.cs](../test/HeuristicLib.ApiSpecs/ResearcherAuthoringSpecs.cs)
- composition:
  [CompositionSpecs.cs](../test/HeuristicLib.ApiSpecs/CompositionSpecs.cs)
- experiments, current API:
  [ExperimentSpecs.cs](../test/HeuristicLib.ApiSpecs/ExperimentSpecs.cs)
- analysis, current API:
  [AnalysisSpecs.cs](../test/HeuristicLib.ApiSpecs/AnalysisSpecs.cs)

These already cover a meaningful part of the salvaged redesign ideas:

- custom operator authoring
- custom terminator authoring
- problem-specific operator authoring
- pipeline and cycle composition
- analyzer attachment and typed result querying

## Near-term backlog

These are the next most useful gaps to turn into specs or refactor targets:

- desired-state experiment API, beside the current-state experiment specs
- desired-state analysis API, beside the current-state analysis specs
- runtime-graph invariants within one top-level execution
- no-publish observation model
- multi-objective short-path usage
- role-aware mating and parent selection pressure differences

## Covered foundations

These foundational scenarios are already present in the current spec set:

1. simple genetic algorithm on a real-valued benchmark
2. simple hill climber / local search setup
3. simple repeated-execution / experiment setup

## Advanced backlog

Still worth adding:

1. island-style population workflow
2. ALPS-style layered workflow
3. richer experiment scenarios
4. symbolic-regression validation scenarios
5. dynamic-problem extension workflow
6. meta-optimization by running an inner algorithm inside `Problem.Evaluate(...)`

These backlog themes also come from the redesign salvage and should stay visible:

- explicit evaluator seams where they improve composition
- objective-focused comparison semantics
- batch-first operators where batch context is the honest semantic model
- clear separation between metaalgorithms and experiments
- clear separation between population topology and per-member side metadata

## Source material

The active guiding docs remain:

- [design-goals.md](design-goals.md)
- [requirements.md](requirements.md)
- [redesign-salvage.md](redesign-salvage.md)

## Keep or delete?

This backlog is still worth keeping, but only as a short living tracker.

Delete it once most remaining items have either:

- become concrete spec files, or
- moved into normal issue tracking or another active planning system

If it starts reading like a second architecture document again, it should be cut down or removed.
