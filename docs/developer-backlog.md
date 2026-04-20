# Developer Backlog

This file is the short living tracker for doc, API-spec, and refactoring follow-up.

It is intentionally narrow: it should track only partial and unfinished follow-up.

## Partly addressed, but still open

These items have meaningful progress behind them, but should still stay on the backlog until the remaining work is done:

- desired-state experiment API, beside the current-state experiment specs
- desired-state analysis API, beside the current-state analysis specs
- runtime-graph invariants within one top-level execution
- no-publish observation model
- role-aware mating and parent selection pressure differences
- keep analyzers observational and read-only from the algorithm perspective, while further simplifying the surrounding observation architecture
- decide and document one common rule for record configuration style: primary constructor parameters versus explicit init-only properties, including naming and when each form is allowed
- audit all solution candidate types used as `TGenotype` and refactor the model so solution candidates are immutable everywhere
- simplify the analyzer/observation system to remove framework-heavy leakage from the authoring and usage model
- improve project and assembly layering so the solution structure better matches responsibility boundaries
- reduce namespace sprawl so common usage requires fewer `using` statements, while keeping a clear folder-level code organization
- move to a more standard C# formatting baseline, including a more conventional editor configuration and CI enforcement that fails pull requests on formatting violations

Why these are only partial today:

- the experiment and analysis specs cover the current API well, but there is not yet a separate desired-state API story
- the analyzer docs are much clearer now, but the observation architecture is still an active design area
- immutability is now an explicit design rule, but that is stronger as policy than as a completed whole-repo audit

## Still not taken care of

These are still real open items:

- multi-objective short-path usage
- island-style population workflow
- ALPS-style layered workflow
- richer experiment scenarios
- symbolic-regression validation scenarios
- dynamic-problem extension workflow
- meta-optimization by running an inner algorithm inside `Problem.Evaluate(...)`
- clear separation between population topology and per-member side metadata as an executable example story
- add explicit ownership-taking factory methods for genotype containers such as `RealVector`, `IntegerVector`, and similar types, following the explicit static-factory style rather than constructor overloads; the goal is to let callers that already own the backing storage transfer it without another allocation or copy, and random generation should be one of the first places to adopt this once the API shape is decided

## Still failing the current design rules

These are not just theoretical backlog items; the repository still contains active mismatches with current design guidance:

- remove all `Debug.Assert` usage and any other debug-versus-release behavioral divergence from library code, and replace correctness checks with runtime invariants, type constraints, or tests

At the moment there are still `Debug.Assert` calls in symbolic tree operators, so this item is not done.

## Source material

The active guiding docs remain:

- [design-goals.md](design-goals.md)
- [requirements.md](requirements.md)

## Keep or delete?

Keep this file only while it helps maintainers decide what still needs examples, refactoring, or cleanup.

Delete it once most remaining items have either:

- become concrete spec files, or
- moved into normal issue tracking or another active planning system

If it starts reading like a second architecture document again, cut it down.
