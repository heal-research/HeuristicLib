# Serializable parameter grids for experiments

Status: design proposal. The intended direction is runtime parameter metadata over ordinary typed configuration properties. The concrete API, serialization format and graph-update semantics need to be settled before implementation.

Update on 2026-11-05: the developer selected declared parameters without reflection as a core contract of the [core simplification plan](core-simplification-plan.md), which also uses them for walks and substitution in the configuration graph. That plan builds the parameter system and in-process experiment setup. String-based configuration, including the JSON recipes below, is a [later separate task](developer-backlog.md#runs-execution-and-experiments).

## Problem

Experiments need to be definable as data received through a REST API, loaded from a file or supplied by another integration. A caller must be able to describe which algorithm parameters to vary and which values to try without providing executable C# callbacks.

The existing [`Grid<T>.VaryBy`](../src/HeuristicLib/Experiments/Grid.cs) supports Cartesian expansion through typed update lambdas. It applies each callback immediately and retains the resulting configurations. This supports constructing cases in C#, but does not retain the grid recipe: the parameter paths, candidate values and relationships between alternatives. Serialization of the expanded configurations is also not a verified round-trip contract.

Nested parameters depend on the selected component. `Selector__TournamentSize` is meaningful for a tournament selector, but not for roulette-wheel selection. An experiment must be able to vary both the concrete selector and parameters specific to each selector alternative.

## Requirements

- Define parameter grids as serializable data, including scalar values, alternative child configurations and nested parameter grids.
- Support explicit finite value lists and finite numeric sequences, including multiplicative steps.
- Keep ordinary C# constructors, typed properties, `init` accessors and `with` expressions.
- Provide runtime parameter discovery, reading and immutable replacement without reflection over properties.
- Keep metadata access delegates out of persisted instance state and configuration value equality.
- Give consumer-defined configurations the same capabilities as built-in configurations.
- Preserve the separation between configuration and execution. Executors continue to use typed settings and dependencies without parameter-path or string lookups during execution.

## Proposed approach

### Runtime parameter metadata

Declare metadata for each configuration type that identifies its configurable properties and provides typed read and immutable replacement operations. For example:

```csharp
// Illustrative API.
Parameter.Define<TournamentSelector<TCandidate>, int>(
    nameof(TournamentSize),
    read: static selector => selector.TournamentSize,
    replace: static (selector, size) => selector with { TournamentSize = size });
```

Share this metadata per configuration type. Accessors operate on the supplied instance rather than capturing one configuration. Ordinary properties remain the source of configuration values; no dictionary backing is needed. Stateless operators that serve as their own executors continue to read their ordinary typed properties.

Metadata must distinguish settings from child configurations, nullable children and child collections sufficiently to support traversal. A nested path uses the actual child configuration's type. Required and inherited settings also need to be discoverable and replaceable. Missing declarations must be detectable rather than silently making a setting unavailable to experiments.

Compare handwritten declarations with focused completeness checks before deciding whether a source generator is justified. The exact metadata contract and its exposure through `IConfigurationNode` or a separate service remain open.

### Experiment recipes and alternatives

A recipe describes a base algorithm configuration, parameter dimensions, alternatives, repetitions and reproducibility inputs. Expansion produces ordinary immutable configuration graphs for experiment cases.

Each child alternative supplies a configuration prototype and an optional local parameter grid. Expand that grid against its prototype, concatenate the alternative results, and combine the resulting child configurations with independent algorithm dimensions through a Cartesian product.

For example, the following grid fragment varies mutation rate and selection. The JSON syntax and type identifiers are illustrative:

```json
{
  "MutationRate": {
    "values": [0.01, 0.05, 0.1]
  },
  "Selector": {
    "alternatives": [
      {
        "prototype": {
          "type": "TournamentSelector",
          "TournamentSize": 2
        },
        "vary": {
          "TournamentSize": {
            "values": [2, 3, 4]
          }
        }
      },
      {
        "prototype": {
          "type": "ProportionalSelector"
        }
      },
      {
        "prototype": {
          "type": "GeneralizedRankSelector",
          "Pressure": 0.1
        },
        "vary": {
          "Pressure": {
            "geometric": {
              "start": 0.1,
              "stop": 100,
              "factor": 10
            }
          }
        }
      }
    ]
  }
}
```

This produces three tournament selectors, one roulette-wheel selector (`ProportionalSelector`) and four generalized rank selectors with pressures 0.1, 1, 10 and 100. The eight selector configurations combine with three mutation rates to produce 24 algorithm configurations before repetition.

Each local path is resolved against its own prototype. The base algorithm's initial selector does not determine which alternatives can be used. An unavailable parameter is an error, not an update silently ignored for unrelated alternatives. Expansion must not depend on JSON object property order.

Numeric sequence descriptions belong to the recipe. Expanding a geometric sequence and supplying its values explicitly must have the same meaning; operators still receive ordinary numeric settings.

### Construction and serialization

Read and replacement metadata operate on existing objects. Turning a serialized prototype into a configuration additionally needs a construction contract, including required constructor values, concrete type identification and conversion of complex settings.

Evaluate two approaches: a serializer that constructs configurations independently of parameter metadata, or a shared schema that also describes construction. Either must support consumer-defined types and check compatibility with the destination slot. Hosts must control which constructors and converters they expose. Explicit typed factories can support construction without runtime generic reflection.

Recipes may carry complete configurations or reference application-registered prototypes. Registered references reduce the initial scope, but require version or snapshot semantics for reproducibility. Even with a registered base algorithm, selector alternatives must be constructible from data or supplied by reference.

Define the boundary for problem and dataset references, termination, random seeds and analysis. The existing experiment contract receives its problem separately from algorithm cases. Delegates stored inside analysis configurations remain a separate persistence obstacle; runtime parameter accessors do not solve it.

### Graph identity and immutable updates

Configurations form a graph and may share children. Their reference identity affects execution sharing, pointcut selection, observation sources and run-bound results. Serialization must preserve intentional shared references.

Specify whether updating a path replaces only that occurrence or every reference to the selected node. Branch-local replacement is the proposed default; replacement everywhere would be explicit. Both need a defined treatment of pointcuts, sharing declarations and other references to replaced nodes. References outside the rewritten graph need an attachment or rebinding contract.

Configuration `Name` labels are not unique and cannot automatically serve as unambiguous collection addresses. Parameter updates configure future execution; they do not replace adaptive executor controls.

### Diagnostics and reproducibility

Resolve recipe paths and check value types, alternative compatibility and finite sequence descriptions before trial execution. Coordinate addressing with the property and collection paths reported by [`SearchConfigurationValidation`](../src/HeuristicLib/Execution/SearchConfigurationValidation.cs). The declared child metadata could also replace that validator's reflection-based graph walk.

Semantic configuration validation remains in executor construction under developer guidelines section 4.7. Suggested UI choices and experiment ranges must not silently become new validity constraints.

Define deterministic ordering of dimensions, alternatives and generated values so expansion preserves case keys and random fork paths. Consider retaining the recipe together with each trial's resolved parameter assignments. A flattened result table must identify the concrete alternative and distinguish inactive parameters from explicitly null values.

## Open decisions

1. What is the metadata API, how is it discovered, and how are missing or inherited settings handled?
2. Does configuration construction share the parameter schema or remain a serializer responsibility? Must the first implementation accept complete configurations, registered prototypes or both?
3. What are the path syntax, collection addressing rules and conflict rules for overlapping updates?
4. How do branch-local and node-wide replacement preserve or rebind graph references?
5. What are the external type identifiers, value conversions and property-rename semantics?
6. What are numeric sequence endpoint, precision and ordering rules?
7. What round-trip and reproducibility guarantees cover recipes, expanded cases and resolved trial parameters?
8. Which problem, dataset and analysis descriptions belong in a persisted experiment, and how are runtime-only callbacks handled?

## Acceptance criteria

- [ ] Demonstrate a serialize-deserialize-expand round trip for the selector example, producing the expected 24 configurations without caller-supplied C# update lambdas.
- [ ] Run repeated trials from the expanded configurations through the experiment execution model.
- [ ] Discover, read and replace scalar, required, inherited and nested settings, including those on a consumer-defined configuration.
- [ ] Verify that typed property updates and metadata-based updates produce equivalent configurations without changing the originals or configuration equality.
- [ ] Verify the chosen graph-reference behavior through serialization and updates, including sharing and reference-based observation or pointcut selection.
- [ ] Report unknown paths, incompatible values and malformed sequences with actionable locations before trial execution.
- [ ] Verify deterministic expansion, case keys and random fork paths across round trips, including an explicit value list equivalent to a generated numeric sequence.
- [ ] Demonstrate the chosen persistence boundary for problem inputs and analysis, and report unsupported runtime-only values explicitly.
- [ ] Keep metadata delegates out of serialized data and parameter lookups out of operator execution.
- [ ] Add API usage specs and published examples for the supported workflow, with focused behavioral coverage following the [test suite guide](../test/README.md).

## Repository guidance

Use the [glossary](../docs/guide/glossary.md#configuration-node) for configuration graph identity and the [developer guidelines](../docs/contributing/developer-guidelines.md) for configuration immutability, execution ownership and validation responsibilities. Runtime metadata accessors are construction machinery under section 4.8; configured behavior stored in persisted instances must remain representable as data.
