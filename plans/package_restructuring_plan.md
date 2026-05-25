# Package Restructuring Plan

This document records the intended package shape for HeuristicLib. It is both a planning document and a migration checklist while the projects, assemblies, namespaces, and NuGet packages are being renamed and moved.

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

The planning default is to implement these as separate projects and NuGet packages. Experimental APIs use package-level opt-in rather than an additional `.Experimental` namespace so promotion from experimental to core can be source-compatible where the API shape itself has not changed.

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

This NuGet package is the .NET-side interop surface. A later Python package may wrap it for Python users, handling bootstrap, assembly loading, and distribution ergonomics without making generated .NET binaries part of the source tree.

## Experimental API Policy

Experimental APIs currently use package-level opt-in through `HEAL.HeuristicLib.Experimental`.

While HeuristicLib is still early alpha and all public APIs may change, do not require `System.Diagnostics.CodeAnalysis.ExperimentalAttribute` on every experimental API. Reconsider this once `HEAL.HeuristicLib` has a meaningful stable API baseline. At that point, experimental status can be communicated through several channels at once:

- package placement in `HEAL.HeuristicLib.Experimental`
- `ExperimentalAttribute` diagnostics at call sites for selected unstable APIs
- documentation that states the API may change without the normal compatibility expectations
- examples that make opt-in explicit

Avoid adding `.Experimental` to normal API namespaces only to communicate maturity. Package opt-in should be enough for users to understand that the API is unstable, and keeping the domain namespace stable avoids unnecessary source changes when an API is promoted into `HEAL.HeuristicLib`.

Promotion from `HEAL.HeuristicLib.Experimental` to `HEAL.HeuristicLib` requires:

- usage examples that demonstrate the intended user experience
- tests covering important behavior and invariants
- a short design justification explaining why the API is ready to become supported
- removal or replacement of experimental diagnostics for the promoted API, if such diagnostics exist

## Placement Rules

Put a type in `HEAL.HeuristicLib.Contracts` only when it is part of the small shared vocabulary that other packages or user implementations should depend on.

Put functionality in `HEAL.HeuristicLib` when it is intended for normal users and has reached the library's supported quality bar.

Put functionality in `HEAL.HeuristicLib.Experimental` when it is useful to expose but still unstable in API shape, semantics, or intended usage.

Put functionality in `HEAL.HeuristicLib.PythonInterop` when it is specifically about Python interoperability rather than general heuristic optimization behavior.

Avoid creating additional vertical packages until a feature has a distinct dependency profile, release cadence, or user installation need. Prefer clear namespaces and documentation first.

## Migration Path

Completed steps:

1. Document the intended package model in this file.
2. Adopt the agreed repository coding style before the broad package move, so the mechanical formatting churn is paid once rather than spread across later semantic changes.
3. Rename packages and projects to the target package identities.
4. Move projects into the target repository layout.
5. Normalize namespaces so experimental APIs use normal domain namespaces and package-level opt-in rather than `.Experimental` or `.Extensions` namespace placement.
6. Move Python-specific code into `HEAL.HeuristicLib.PythonInterop`.

Remaining steps:

1. Review the resulting package boundaries and project references.
2. Move or promote APIs between `HEAL.HeuristicLib`, `HEAL.HeuristicLib.Experimental`, and `HEAL.HeuristicLib.Contracts` only after tests, examples, and API shape are reviewed.
3. Revisit `ExperimentalAttribute` once the main package has a meaningful stable API baseline.

Each migration step should update relevant docs, examples, and tests together with the code move.

## Coding Style Migration

The package restructuring will touch a large portion of the repository through project renames, namespace changes, and file moves. That makes it a good time to also move to a more conventional C# formatting baseline, such as four-space indentation and standard C# brace/newline layout.

The style migration should still be treated as mechanical work. Prefer an isolated formatting commit or a clearly separated phase in the restructuring work, with no behavioral or API changes mixed into the formatting diff. Add the formatting commit to `.git-blame-ignore-revs` so blame views can skip the style-only churn where supported.

The updated `.editorconfig` should start from a mainstream .NET/Visual Studio-style baseline, then deliberately preserve HeuristicLib-specific rules such as verified snapshot-file handling, intentional analyzer suppressions, nullable/code-style preferences, and line-ending policy.

Target indentation by file family:

- C# (`*.cs`): 4 spaces.
- MSBuild/XML (`*.csproj`, `*.props`, `*.targets`, `*.xml`): 4 spaces.
- YAML (`*.yml`, `*.yaml`): 2 spaces, matching common YAML and GitHub Actions style.
- JSON (`*.json`): 2 spaces, matching common Prettier, npm, and tooling defaults.
- Markdown (`*.md`): Prettier defaults; indentation is 2 spaces, while prose wrapping should stay controlled by Prettier rather than by C# rules.
- Verified snapshots (`*.received.*`, `*.verified.*`): keep formatting exceptions so generated approval artifacts are not churned.

The migration tooling can be staged through npm scripts so contributors have one repeatable entry point:

```powershell
npm install
npm run format
npm run format:check
```

Use separate script targets when reviewing or troubleshooting a specific file family:

```powershell
npm run format:code       # C# whitespace through dotnet format
npm run format:text       # JSON and YAML through Prettier
npm run format:docs       # Markdown through Prettier when explicitly desired
npm run format:xml        # XML, .csproj, .props, and .targets through Prettier XML
```

The XML formatter needs `@prettier/plugin-xml` and should run with whitespace normalization enabled for MSBuild files, for example `--xml-whitespace-sensitivity ignore --tab-width 4`. Without that option the plugin preserves existing XML whitespace and may leave old two-space `.csproj` and `.props` indentation unchanged.

## Open Follow-Up Decisions

The current planning default keeps `HEAL.HeuristicLib`, `HEAL.HeuristicLib.Experimental`, and `HEAL.HeuristicLib.PythonInterop` as separate projects and NuGet packages. This gives experimental and Python-specific APIs clear install-time opt-in while keeping ordinary domain namespaces stable.

Revisit this only if packaging friction becomes larger than the value of the explicit install-time boundary.

The Python user-facing distribution story is related but separate: `HEAL.HeuristicLib.PythonInterop` remains the NuGet package boundary, while a Python package can later provide a more idiomatic `pip`/`uv` install experience.
