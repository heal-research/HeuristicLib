# Analysis usability follow-up

Status: complete. The approved foundation and ownership revision was validated on 2026-09-09.

The approved [first-class analyzers and run lifecycle](first-class-analyzers-and-run-lifecycle.md) plan owns the next
rework and supersedes this document where the designs differ. This document remains the record of the completed
foundation revision.

This plan supersedes conflicting recommendations in [analysis-api-simplification.md](analysis-api-simplification.md). The first implementation passed its checks, then the user requested the revisions below. Production APIs, usage specs and current guides define the resulting design.

## Working agreement

The user reviews by staging files. Do not stage, unstage, reset the index or commit. Preserve their working edits and treat index changes as review activity.

## Approved design

- Use the existing execution-instance foundation for aggregation and trace retention. Configurations are value records; mutable execution state belongs to instances. Expose `CreateExecutionInstance(resolver)` and role-specific runtime operations.
- Use one aggregation role for both summaries of individual observations and accumulation across observations. Offer `Aggregate.Best()` and `Aggregate.BestSoFar()` as named strategies. Remove the separate trace reducer/reduction hierarchy.
- Resolve independent trace state by default. An explicitly supplied resolver can share aggregation and retention instances across traces using the existing reference-identity and scope rules.
- Keep named measurements as the normal strategy path. Runtime-only delegate adapters are classes with identity semantics. The typed trace recorder stores its dependencies directly rather than retaining an installation callback.
- Move comparer strategies and `RequireTotalOrder` into Objectives. Pass an optional comparer to aggregation; null uses the observed problem's objective. Do not clone or cache objective objects on the trace.
- Implement `Install` directly. Remove the single-install base, `InstallCore`, hook-disposal tracking and general dependency registry. Reusing mutable analyzers or clocks is allowed and intentionally shares state.
- Deduplicate the same recorder at the same observation boundary within one builder scope. Different boundaries and general decorations remain composable. Preserve clocks-before-trace observation order.
- Remove the dynamic relative evaluator's event subscription and disposable execution instance. Refresh its best-known reference from the observed epoch, accounting for updates deferred until batch evaluation starts.
- Keep direct typed results, immutable snapshots and selected-axis validation. TraceRetention filters stored entries after aggregation; it does not skip computation or evict history.
- Keep experiment factories as a single algorithm-to-analyzer function. Independent trial factories create fresh analyzers and clocks.

## Implementation checklist

- [x] Unify aggregation and migrate core and experimental implementations.
- [x] Resolve retention instances through the common foundation.
- [x] Add explicit resolver sharing and preserve independent defaults.
- [x] Move objective comparer and eliminate trace objective caches.
- [x] Use named measurements and a typed observation recorder.
- [x] Remove hook binding and resource ownership machinery.
- [x] Deduplicate observations by boundary and recorder identity.
- [x] Remove dynamic evaluator event subscription.
- [x] Update guides, glossary, API usage specs and regression tests.
- [x] Complete final build, solution tests and formatting verification.

## Validation

Regressions cover independent counters, shared accumulators across anchors and algorithms, resolver identity, shared clock counts, analyzer reuse, cancellation and early stopping, immutable snapshots, explicit objective comparer and dynamic batch updates.

Final validation passed on 2026-09-09: restore, Release build, 2,019 core tests, 146 API usage specs,
167 experimental tests and 24 scenarios. Solution-wide whitespace, style at warning severity and analyzer checks at
error severity passed. `git diff --check` passed. Existing unrelated build warnings remain.

## Existing separate limitation

Genealogy still keys generation connections by candidate value. Repeated equal children with distinct parents can conflict in `GenealogyGraph.AddConnection`; the developer backlog records that separate identity-design issue. This revision does not change genealogy identity semantics.
