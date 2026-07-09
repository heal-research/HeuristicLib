# Mating Contract

## Summary

This plan defines the intended contract between parent selection, mating arrangement, crossover, and replacement.

The long-term direction is to introduce a dedicated mating-arrangement concept, while keeping it mostly behind algorithm defaults so ordinary users can still configure a standard GA with familiar selector and crossover settings.

For the current standard `GeneticAlgorithm`, the intended model is an elitist generational GA: copied elites are not newly produced, so each generation should select parents for exactly `PopulationSize - Elites` offspring.

## Parent Ordering Options

Three designs were considered:

- Keep algorithm-owned pairing into `IParents<T>`.
  - Pros: minimal change, preserves explicit `Parent1`/`Parent2`, and works with existing crossovers and observers.
  - Cons: pairing remains hidden in each algorithm, selector output order has an implicit contract, and helper methods can be misused if counts are odd or role ordering is unclear.

- Remove `IParents<T>` and let crossovers pair a flat parent list.
  - Pros: smaller public surface and direct selector-to-crossover flow.
  - Cons: weakens type safety, makes parent roles convention-only, duplicates pairing logic in crossovers, and makes genealogy/analysis less explicit.

- Introduce a dedicated mating-arrangement concept.
  - Pros: makes role-aware mating explicit and supports gender-specific selection, best-parent-first ordering, no-same-mates, assortative mating, and future topology-aware mating without overloading `ISelector`.
  - Cons: adds another concept and needs defaults/adapters so simple GA setup does not become verbose.

Chosen direction: use a dedicated mating-arrangement concept long-term. The arranger should be an implementation-focused operator/concept that algorithms can use internally. Ordinary users should still be able to configure a standard GA with familiar selector/crossover settings, while advanced users can replace or compose the mating arrangement when parent roles matter.

## Implementation Direction

- Define the mating arrangement as the boundary that converts selected/evaluated candidates into ordered parent groups for crossover.
- Keep `IParents<T>` or an equivalent explicit pair type at the crossover boundary unless a later design proves n-ary/global crossover should become first-class.
- State the standard binary crossover convention: ordinary mating arrangements produce ordered neighboring pairs, and `Parent1`/`Parent2` may have semantic meaning for role-sensitive crossovers.
- Provide default arrangement behavior equivalent to today’s flow: select `2 * offspringCount` parents with the configured selector, then pair entries `0/1`, `2/3`, etc.
- Provide specialized arrangements or meta-selectors for role-specific/gender-specific selection, best-parent-first ordering, and pair-level constraints such as no-same-mates.

## Offspring Count Options

Three count policies were considered:

- Always produce `PopulationSize` offspring.
  - Pros: simple generation accounting and familiar “one population-size batch per generation.”
  - Cons: elitism overproduces candidates that replacement cannot use, increasing evaluations unnecessarily.

- Let replacement dictate required offspring count.
  - Pros: can be exact for every replacement strategy.
  - Cons: couples survivor selection backward into parent selection and variation, making `IReplacer` responsible for upstream planning.

- Use algorithm-owned offspring policy.
  - Pros: keeps generation semantics with the algorithm, avoids overproduction, and keeps replacement as survivor selection.
  - Cons: different algorithms may expose different count semantics.

Chosen direction for now: standard `GeneticAlgorithm` is an elitist generational GA. It should produce exactly `PopulationSize - Elites` offspring, select exactly `2 * (PopulationSize - Elites)` parents for binary crossover, evaluate only those new offspring, and copy elites without re-evaluation by default.

More flexible research workflows with plus/comma replacement or custom replacement should be handled by a separate broader evolutionary algorithm shape with explicit offspring count, such as `Lambda`/`OffspringCount`, rather than making the standard GA ambiguous.

## Test Plan

- Add API/spec tests documenting that parent order is meaningful and preserved through arrangement into crossover pairs.
- Add tests for gender-specific/role-specific arrangement: first selector fills `Parent1`, second selector fills `Parent2`.
- Add tests for best-parent-first arrangement using objective comparison before converting to candidate-only parents.
- Add GA tests showing `PopulationSize = 100` and `Elites = 1` produces/evaluates 99 offspring and requests 198 selected parents.
- Add guard tests for invalid counts, especially odd parent counts or impossible elite/population combinations.

## Assumptions

- Binary crossover remains the default/common case.
- Parent roles are meaningful enough to deserve explicit contract support.
- Copied elites are not re-evaluated in the default static deterministic GA path.
- Dynamic or noisy reevaluation should be handled by a separate explicit reevaluation policy/workflow, not by ordinary elitism behavior.
- The first implementation may keep existing selectors and `IParents<T>` while introducing the mating-arrangement concept incrementally.
