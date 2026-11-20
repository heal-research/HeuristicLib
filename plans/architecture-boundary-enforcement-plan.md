# Architecture boundary enforcement plan

Status: planned on 2026-11-05. Start after the core simplification branch is merged. No implementation has started under this plan. The goal and sequencing are agreed; the detailed dependency policy, test tooling and any assembly or package changes remain proposals requiring acceptance before implementation.

## Problem and intended outcome

The main HeuristicLib assembly contains both the optimization foundation and the machinery and content built on it. Being in the main package means a component belongs in the standard toolkit and meets the repository's readiness criteria. It does not establish an architectural boundary between domain concepts, execution, hosting, experimentation, inspection, analysis and concrete library content.

The intended outcome is stronger, executable constraints on these dependencies. A new component or reference should not silently weaken the architecture. Ordinary users should retain a simple NuGet entry point, and component authors should retain the same capabilities as built-in components.

This work strengthens the simplified design. It must not introduce new runtime abstractions merely to make a layering diagram or an assembly split work.

## Discussion and decision status

The discussion compared technical separation through assemblies, as used in the former HeuristicLab, with architecture tests such as ArchUnitNET. It also considered shipping several assemblies through one core NuGet package.

| Topic                          | Status and direction                                                                                                                                                                       |
| ------------------------------ | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| Stronger dependency boundaries | Agreed goal.                                                                                                                                                                               |
| Timing                         | Agreed: undertake this after the simplification rework, against the resulting code and contracts.                                                                                          |
| Consumer setup                 | Preserve a simple entry point for the standard toolkit. Whether that requires literally one `.nupkg` or one consumer `PackageReference` remains open if packaging changes become relevant. |
| Primary enforcement            | Recommended: strengthen architecture tests first, using the existing checks as the starting point.                                                                                         |
| Assembly boundaries            | Recommended: consider a few coherent boundaries later where they provide a demonstrated benefit. No extraction is approved.                                                                |
| ArchUnitNET                    | Candidate tool to compare with the existing Roslyn checks. Adoption is not decided.                                                                                                        |
| Namespace and folder layout    | Preserve concept-oriented organization. No layer-first namespace migration is proposed.                                                                                                    |

Keep these distinctions explicit:

- A responsibility boundary says what code may depend on.
- An assembly boundary provides compiler and accessibility constraints between projects.
- A package boundary controls distribution, dependency selection and versioning.
- Main versus Experimental describes readiness and package suitability. Experimental content still has architectural responsibilities and dependency rules.

These are separate decisions. One assembly can contain several enforced responsibility boundaries, and one package can contain several assemblies.

## Findings from the current checkout

These findings describe the code inspected before this plan was written. They are an investigation baseline, not a claim that the architecture tests were run or that their current rules are complete. Refresh them after simplification.

### Existing guidance and enforcement

The [layering guide](../docs/contributing/architecture/layering.md) already describes six responsibilities: domain vocabulary, composition, resolution, library content, hosting and analysis. It assigns types by responsibility rather than by namespace or assembly.

The existing checks include:

- [LayerDependencyTests](../test/HeuristicLib.Tests/Architecture/LayerDependencyTests.cs): Roslyn-based source dependency checks for main and Experimental, agreed public type locations, and deliberate forbidden-reference probes.
- [AssemblyDependencyTests](../test/HeuristicLib.Tests/Architecture/AssemblyDependencyTests.cs): main-to-Experimental isolation, selected namespace and placement rules, and ownership of selected standard-toolkit types.
- [ExperimentalBoundaryTests](../test/HeuristicLib.Tests.Experimental/Architecture/ExperimentalBoundaryTests.cs): ownership and namespace checks for selected experimental features.

The source checks already protect a domain floor, prohibit dependencies on Analysis from several responsibilities, prevent hosting from naming concrete library algorithms, and restrict access to resolution and run internals. Existing probes include qualified references, global usings, internal access and legitimate uses that must remain allowed.

The implementation is a collection of selected rules rather than a complete module dependency policy. In particular, `SourceKind` can return `Other`, there is no assertion that every library type has a reviewed responsibility, and there is no general cycle check between responsibility groups. Reaching a passing result therefore establishes the implemented checks, not every relationship described in the guide.

The Roslyn checks reconstruct a compilation from source and dependency assets, including explicit parse options and generated usings. Their correspondence with the real build configuration needs verification when extending them, especially after simplification changes the projects or generated code.

### Boundaries cut across concept namespaces

`Algorithms` and `Operators` contain authoring contracts as well as concrete behavior. `Execution` includes concurrency helpers, composition, resolution and hosting. `Inspection` includes two domain accumulators as well as execution observation and measurement facilities. Encodings are vertical slices containing representation values, search spaces, operators and recommendations.

A blanket rule based on a namespace prefix would therefore misclassify some types. A concrete representation does not become foundational domain vocabulary simply because its candidate type is a value.

### A split into six assemblies would not follow the current graph

Configuration construction accepts `ResolutionScope`, while resolution depends on configuration contracts. Composition and resolution currently have mutual dependencies. They can be treated as one execution module for cycle enforcement without pretending they are independently separable assemblies.

The [earlier layering investigation](developer-backlog.md#namespaces-folders-or-assemblies-by-layer) also records the previous Contracts assembly consolidation and the consequences of moving hosting. For example, `AlgorithmExtensions` and `RunTraceExtensions` reference `AlgorithmRun` despite living beside algorithm and analysis concepts. Those references need ownership decisions if hosting is extracted.

That investigation rejected reorganizing namespaces, folders or assemblies mechanically around the six layers. Its recorded reconsideration trigger is a layer gaining an audience of its own. This plan does not silently overturn that decision: any extraction proposal must explain how its evidence meets the trigger, or explicitly ask to reconsider the tradeoff.

### Simplification changes the baseline

The core simplification changed configuration introspection, placement, resolution responsibilities, executor construction and analysis. It also retired `Instrument` while retaining `Analyzer`. Current names and current placements are evidence for the investigation, not a specification to preserve after those changes.

## Proposed dependency model

Use a dependency graph with branches rather than assuming one linear stack. The following is the proposed starting policy to test against the simplified implementation; it is not yet the complete allowed-dependency matrix.

| Responsibility           | Proposed constraint                                                                                                                                                                                                                                                                                                    |
| ------------------------ | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Optimization domain      | Foundational, representation-independent concepts remain independent of configurations, resolution, hosting, inspection machinery and analysis. Shared domain values used for measurement may remain here.                                                                                                             |
| Execution model          | Configuration and executor contracts, construction and resolution use domain concepts. They remain independent of concrete algorithms, concrete representations, experiment coordination and concrete analysis implementations. Assess composition and resolution together where their authoring contract requires it. |
| Concrete library content | Algorithms, operators, representations and problems use the relevant domain and execution contracts. Representation-independent components do not acquire dependencies on particular encodings or problems without a concrete responsibility that justifies them.                                                      |
| Inspection               | Observation and measurement preserve the observed computation. Content may use counts and durations for explicit policies without depending on analysis implementations. Resolve observation contract and wrapper ownership against the simplified design.                                                             |
| Run hosting              | Owns run lifecycle and invokes algorithm/executor contracts. It does not select concrete library algorithms or depend on concrete analyzers.                                                                                                                                                                           |
| Experimentation          | Coordinates independent runs through hosting and configuration contracts. It must not make experimentation a prerequisite for running or analyzing one algorithm. Audit any analysis-related conveniences separately from scheduling.                                                                                  |
| Analysis                 | Derives results from observations. Its single-run workflows do not require experimentation. Existing hosting conveniences can remain higher-level conveniences; do not impose a blanket ban on Analysis referencing hosting.                                                                                           |
| Integration clients      | PythonInterop may use the libraries it integrates. Main and Experimental do not acquire a dependency on PythonInterop. Main remains independent of Experimental.                                                                                                                                                       |

Do not require arbitrary concrete features to be independent of one another. A problem-specific operator can depend on its problem, and an algorithm can compose operators. Review feature dependencies according to responsibility. Cycle checks apply between agreed modules, not indiscriminately between every namespace or type.

## Implementation sequence

### Phase 1: refresh the inventory and accept the dependency policy

1. Record the commit at which simplification is complete. Read its final glossary and architecture guidance before classifying the resulting code.
2. Inventory types and dependencies in main, Experimental and PythonInterop, including internal types, nested types, generic constraints, public signatures and implementation references. Identify generated or conditional code and how the actual build supplies it. Keep test and analyzer tooling outside the runtime module graph.
3. Map types to responsibilities, with explicit treatment of mixed namespaces and higher-level convenience extensions. Record project references and external package ownership separately from internal type dependencies.
4. Find cycles and dependencies that contradict the proposed direction. Distinguish a coherent module with internal collaboration from a dependency that crosses an intended boundary.
5. Produce the allowed-dependency matrix, module classification rules, relevant access restrictions and any narrowly justified exceptions. Include concrete examples for the difficult placements.

Exit criterion: the developer accepts the policy and any required ownership changes. Every runtime library type has an unambiguous classification; a broad `Other` category cannot exempt new code from enforcement. Existing dependencies are not automatically approved by being present in the inventory.

### Phase 2: choose the enforcement mechanism with a bounded comparison

Compare extending the existing Roslyn checks with [TNG ArchUnitNET](https://github.com/TNG/ArchUnitNET), whose [guide](https://archunitnet.readthedocs.io/en/latest/guide/) covers dependency rules and cycle checks. The project also documents an xUnit v3 integration. Recheck compatibility with the repository's SDK and test setup when this phase starts.

Use representative rules from the accepted matrix: the domain floor, hosting independence from concrete algorithms and analyzers, a mixed-namespace classification, an internal-access restriction and a module cycle. Include both legal references and deliberate violations.

Compare coverage, diagnostic usefulness, classification effort, test runtime, build fidelity and maintenance cost. Roslyn can retain source ownership rules that compiled-code analysis does not express. Prefer one implementation per rule; complementary tools need distinct responsibilities.

Exit criterion: record the selected approach and the evidence. Adopting another library must simplify maintenance or close a demonstrated enforcement gap. A fluent syntax alone does not establish that benefit.

### Phase 3: enforce the accepted boundaries

1. Implement classification coverage and the accepted dependency matrix in the existing architecture test ownership, following [the test suite guide](../test/README.md).
2. Check module cycles at the agreed granularity. Keep intentionally mutually dependent execution contracts and resolution in one module unless an accepted redesign separates them.
3. Cover main and Experimental consistently, and check the direction of integration dependencies. Experimental status does not bypass dependency policy.
4. Preserve access restrictions that are finer than public type dependencies, such as which responsibility may construct root resolution storage or manipulate run internals, where those restrictions still apply after simplification.
5. Add deliberate violation probes for each rule family and legal-reference probes for ambiguous boundaries. Failures should identify the referencing type/member, target, applicable rule and source location where available.
6. Fix violations within accepted ownership decisions. Return to design discussion if a fix needs a new interface, adapter, exposed internal API or changed execution semantics. Do not accumulate blanket exceptions or a permanent grandfathered baseline.
7. Update contributor architecture guidance to describe the implemented rules. Ensure the normal CI test path runs the checks as blocking tests.

Exit criterion: all inventoried code is classified, accepted dependencies pass, representative forbidden dependencies and cycles fail, and new code cannot escape classification. There is one clear source of enforcement for each rule.

### Phase 4: decide whether an assembly extraction adds value

This is an optional decision checkpoint, not a promised extraction phase. Architecture tests can be the completed solution.

A domain foundation is the first candidate to investigate because it has the clearest independence requirement. A coarse execution assembly could be considered later if its graph and audience support it. Do not start with one assembly per documented layer, encoding, operator family or problem suite.

For each candidate, record:

- The concrete benefit over the passing architecture tests: for example an independent consumer, unsuitable dependencies for that consumer, or recurring boundary violations for which compiler enforcement would materially help.
- The dependency graph, accessibility consequences and changes needed by extension methods and authoring contracts.
- Whether responsibilities separate naturally without making internal machinery public or inventing contracts solely to cross the boundary.
- The project, build, packaging and maintenance costs, including Experimental and PythonInterop consumption.
- The proposed consumer experience and whether a literal single package or a single package reference is required.

Multiple assemblies can ship in one package, but standard MSBuild pack does not automatically bundle referenced projects' outputs; it normally represents them as package dependencies. An umbrella package is another option when one consumer reference is sufficient. [Microsoft's packaging guidance](https://learn.microsoft.com/en-us/nuget/create-packages/select-assemblies-referenced-by-projects) recommends one assembly per package and explains the explicit work involved in bundling several.

If extraction is accepted, write its concrete migration and packaging steps before changing projects. Preserve architecture tests within the resulting assemblies. Validate a packed-package consumer as well as project references; a successful solution build alone does not establish correct package assets or transitive dependencies.

Exit criterion: an explicit decision to keep the assembly structure or an accepted extraction proposal. Deferring extraction is a valid outcome and does not leave the enforcement work incomplete.

## Representative acceptance cases

| Scenario                                                                                                  | Expected result                                                                                            |
| --------------------------------------------------------------------------------------------------------- | ---------------------------------------------------------------------------------------------------------- |
| A foundational objective or RNG helper references `IConfigurationNode`, a concrete encoding or a run host | Dependency test fails.                                                                                     |
| A configuration's construction hook uses resolution, and resolution invokes configuration contracts       | Allowed within the agreed execution module. No artificial cross-assembly cycle is introduced.              |
| Run hosting instantiates a particular genetic algorithm or concrete analyzer                              | Dependency test fails.                                                                                     |
| An algorithm stops according to an inspection counter or duration                                         | Allowed through the accepted measurement contracts, without depending on trace or ranking implementations. |
| A caller records and reads analysis for one algorithm run                                                 | Works without experiment setup; existing public usage specs remain valid.                                  |
| Experimental content violates the same responsibility boundary enforced for main content                  | Dependency test fails even though the code is experimental.                                                |
| A new library type is placed outside every classification rule                                            | Classification test fails instead of silently treating it as unrestricted.                                 |
| A forbidden reference uses a qualified name, alias, generic argument, signature or internal member        | The relevant dependency or access check detects it. Source-only placement rules remain covered separately. |
| Two modules intended to be acyclic acquire reciprocal dependencies                                        | Cycle check fails and reports the path.                                                                    |
| A consumer defines its own operation, configuration or analyzer through the supported public contracts    | Existing authoring/API usage specs continue to compile and pass. No built-in privilege is introduced.      |

## Validation

For this plan document, check Markdown formatting and local links. It changes no code or current architecture policy.

For implementation, follow `AGENTS.md` and the test suite guide, using the SDK selected by `global.json` and sequential build/test commands. Start with the affected architecture classes, for example:

```powershell
dotnet test --project test/HeuristicLib.Tests/HeuristicLib.Tests.csproj --configuration Release --no-restore --filter-class '*LayerDependencyTests'
```

Run all affected architecture checks, including assembly and Experimental boundaries, and the full core suite after meaningful changes. Validate tooling changes through their affected test and CI paths. Public API or authoring changes require API usage validation; published examples require the documentation suite and documentation build. Substantial cross-project changes require the final whole-solution validation and applicable formatting checks. An accepted packaging change additionally requires `dotnet pack --configuration Release --no-restore` and consumption of its produced packages.

## Open decisions and recommended defaults

| Decision                                                                   | Recommended default                                                                                                                                                             |
| -------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| How many responsibility groups should be enforced after simplification?    | Derive the smallest coherent graph from the resulting responsibilities. Preserve useful distinctions inside modules without forcing each into an assembly or cycle-check slice. |
| Extend Roslyn or adopt ArchUnitNET?                                        | Retain Roslyn unless the bounded comparison shows a concrete coverage or maintenance benefit. Avoid duplicate rules.                                                            |
| Are assembly boundaries necessary for completion?                          | No. Complete dependency enforcement first; extract only through the explicit decision checkpoint.                                                                               |
| Should existing namespace layout determine classification?                 | Use namespaces where they are reliable, with explicit responsibility rules for mixed concepts. Do not reorganize user imports to simplify a test predicate.                     |
| Should lighter external dependencies drive a package split?                | Require a concrete consumer need, consistent with the [optional dependency extraction backlog item](developer-backlog.md#optional-capabilities-and-packaging).                  |
| What if simplification leaves a dependency that contradicts this proposal? | Reconcile the policy and ownership openly before enforcement. Do not reverse accepted simplification decisions or conceal the dependency behind an adapter.                     |

No assembly count, package layout, new public contract or replacement test framework is selected by this document.
