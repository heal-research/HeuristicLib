# Package Restructuring Plan

This document records the intended package shape for HeuristicLib. It is a planning document only: the projects, assemblies, namespaces, and NuGet packages have not yet been renamed or moved.

For durable design principles, see [design-goals.md](../docs/design-goals.md). For short-lived follow-up items, see [developer-backlog.md](../docs/developer-backlog.md).

## Goal

The current `Abstractions`, `Core`, and `Extensions` split mixes several concerns:

- foundational contracts versus implementation
- main supported functionality versus experimental functionality
- package dependency layering
- optional integration-oriented features

The target structure should make those concerns explicit. Users should be able to identify the main package, understand which APIs are foundational, and recognize experimental APIs before relying on them.

## Target Package Shape

The intended package names are:

- `HEAL.HeuristicLib.Contracts`
- `HEAL.HeuristicLib`
- `HEAL.HeuristicLib.Experimental`
- `HEAL.HeuristicLib.PythonInterop`

The current planning default is to implement these as separate projects and NuGet packages. The exact NuGet distribution strategy for experimental APIs remains open; see [developer-backlog.md](../docs/developer-backlog.md).

## Package Responsibilities

### `HEAL.HeuristicLib.Contracts`

Contains foundational public contracts and semantic primitives that other packages and user implementations can depend on.

This package is the successor to the current meaning of `HEAL.HeuristicLib.Abstractions`, but `Contracts` better communicates that these types define public library contracts rather than generic infrastructure.

Types belong here when they are broadly needed across packages or by user-authored implementations, and when they are expected to become part of the most stable public API surface. Examples include core interfaces and value primitives for problems, search spaces, operators, execution, states, random number generation, objectives, and solutions.

Changes to this package should be treated as high-impact API design changes, especially once the library approaches version 1.0.

### `HEAL.HeuristicLib`

Contains the main supported library functionality.

This package is the successor to the current practical role of `HEAL.HeuristicLib.Core`, but the package should use the root name because it is the primary package users install and use. The word `Core` suggests an inner layer with an outer shell, which is not the intended user-facing model.

Functionality belongs here when it is broadly useful, tested, documented enough for normal use, and intended to be part of the supported library surface. This includes polished algorithms, operators, search spaces, genotypes, problems, execution helpers, analysis tools, random engines, and optimization utilities.

### `HEAL.HeuristicLib.Experimental`

Contains APIs that are useful enough to share but not mature enough for normal compatibility expectations.

This package is the successor to the current maturity role of `HEAL.HeuristicLib.Extensions`. The name `Extensions` should be reserved for true add-on packages or extension helpers, not for communicating experimental status.

Experimental functionality may include research workflows, immature algorithms, unstable problem models, exploratory analysis tools, or designs that still need usage validation. Experimental code must still build, have appropriate tests, and have clear design intent. Experimental means unstable API contract, not lower code quality.

### `HEAL.HeuristicLib.PythonInterop`

Contains Python-specific integration code, scripts, adapters, and workflows.

Python interoperability should be separated from general experimental code because it has a distinct audience, dependency story, runtime assumptions, and documentation needs. Code belongs here when its purpose is specifically to support Python integration or Python-facing workflows.

## Experimental API Policy

Public experimental APIs must be marked with `System.Diagnostics.CodeAnalysis.ExperimentalAttribute`.

Experimental status should be communicated through several channels at once:

- package or namespace placement under `Experimental`
- `ExperimentalAttribute` diagnostics at call sites
- documentation that states the API may change without the normal compatibility expectations
- examples that make opt-in explicit

Promotion from `HEAL.HeuristicLib.Experimental` to `HEAL.HeuristicLib` requires:

- usage examples that demonstrate the intended user experience
- tests covering important behavior and invariants
- a short design justification explaining why the API is ready to become supported
- removal or replacement of experimental diagnostics for the promoted API

## Placement Rules

Put a type in `HEAL.HeuristicLib.Contracts` only when it is part of the small shared vocabulary that other packages or user implementations should depend on.

Put functionality in `HEAL.HeuristicLib` when it is intended for normal users and has reached the library's supported quality bar.

Put functionality in `HEAL.HeuristicLib.Experimental` when it is useful to expose but still unstable in API shape, semantics, or intended usage.

Put functionality in `HEAL.HeuristicLib.PythonInterop` when it is specifically about Python interoperability rather than general heuristic optimization behavior.

Avoid creating additional vertical packages until a feature has a distinct dependency profile, release cadence, or user installation need. Prefer clear namespaces and documentation first.

## Migration Path

1. Document the intended package model in this file.
2. Rename packages and projects only after the package model has been accepted.
3. Move Python-specific code into `HEAL.HeuristicLib.PythonInterop`.
4. Move unstable extension code into `HEAL.HeuristicLib.Experimental` and annotate public experimental APIs with `ExperimentalAttribute`.
5. Rename the main supported package from the current `Core` role to `HEAL.HeuristicLib`.
6. Promote mature APIs from experimental to the main package only after tests, examples, and API shape are reviewed.

Each migration step should update relevant docs, examples, and tests together with the code move.

## Open Follow-Up Decisions

The NuGet distribution model for experimental APIs is not final.

The current planning default keeps `HEAL.HeuristicLib` and `HEAL.HeuristicLib.Experimental` as separate projects and NuGet packages. This gives experimental APIs a clear install-time opt-in.

An alternative is to ship both stable and experimental assemblies through a single `HEAL.HeuristicLib` NuGet package, while relying on namespace usage and `ExperimentalAttribute` diagnostics for code-level opt-in.

A stronger alternative is to keep stable and experimental APIs in a single project and assembly, again relying on namespace placement and `ExperimentalAttribute` diagnostics to communicate experimental status.

This decision should be revisited before the package migration is implemented.
