# Permutation operator port

Status: implemented and validated.

## Scope and decisions

Port the creators, crossovers and manipulators supplied in `for_Agent/PermutationEncoding` to the core permutation encoding. Use immutable candidates, explicit RNGs, single-candidate operator bases for independent variation, and execution-owned state for cyclic creation. Keep the source artifact repository unchanged.

The existing `RandomPermutationCreator`, `OrderCrossover`, `EdgeRecombinationCrossover`, `SwapMutator` and `InversionMutator` cover the corresponding source algorithms. `Swap2Manipulator` maps to `SwapMutator`. Preserve existing swap and inversion sampling behavior rather than adopting the source inversion's exclusion of the final position and nontermination on short permutations.

Add `PredefinedPermutationCreator`, retaining cyclic creation and optional uniform sampling with replacement. `PredefinedCandidatesCreator` is not equivalent: it consumes its seeds once and then delegates to a fallback. The predefined list is a value-array snapshot; each logical execution owns a cursor that survives rebinding and starts fresh in an independent scope.

Add the nine missing crossovers with their source names: `CosaCrossover`, `CyclicCrossover`, `CyclicCrossover2`, `MaximalPreservativeCrossover`, `OrderBasedCrossover`, `OrderCrossover2`, `PartiallyMatchedCrossover`, `PositionBasedCrossover` and `UniformLikeCrossover`. The numbered variants have distinct algorithms and remain separate types. Preserve the source license notices on adapted crossover implementations.

The supplied crossover implementations carry GPL-3.0-or-later notices, retained in the adapted files. Repository package metadata declares MIT; this port does not resolve that discrepancy.

Add `InsertionMutator`, `ScrambleMutator`, `Swap3Mutator`, `TranslocationMutator` and `TranslocationInversionMutator`. The source `MultiPermutationCrossover` and `MultiPermutationManipulator` map to generic `ChooseOneCrossover<Permutation>` and `ChooseOneMutator<Permutation>`. Do not copy their reflection-based discovery of all available operators; callers explicitly choose children and optional weights.

## Intentional differences from the source

- All operators preserve parent values; mutation returns a candidate rather than modifying the input.
- Empty and singleton permutations are unchanged without random draws, except where an operator requires a larger minimum length. Keep the source minimum of four for partially matched and maximal preservative crossover, and three for three-position swap.
- Three-position swap samples distinct positions without rejection. Partially matched crossover samples valid segment endpoints without rejection, keeping at least two positions outside the segment. Both terminate for constant RNGs. Random draw sequences are not intended to match the legacy framework.
- Scramble uses Fisher–Yates instead of allocating successively smaller arrays of indices.
- Translocation samples all insertion points in the remaining permutation, including its end. The source excludes end positions. Deterministic overloads use inclusive segment endpoints and remaining-permutation insertion coordinates.
- Rely on `Permutation` and the algorithm's validity contract for valid values and matching parent dimensions. Validate independent segment coordinates and minimum lengths before random draws. Predefined creation checks space membership before changing its cursor or sampling.

## Validation

Core tests cover reference outputs, value preservation, parent immutability, reproducibility, small permutations, segment bounds, creation state ownership, sampling and configuration snapshots. A published executable documentation example exercises operator resolution and generic crossover/mutator composition. Run the core, API usage and documentation suites, documentation build, relevant formatting checks and final solution tests.

The unfiltered solution run was stopped after entering the research experiment matrix in `AutoEcPaperScenarioTests`. That class includes hundreds of optimization runs per experiment and is excluded from the bounded final solution run with `--filter-not-class '*AutoEcPaperScenarioTests'`. The final run passed all 3,107 tests across the core, Experimental, scenario, API usage and documentation suites. Release build, documentation build, scoped whitespace/style/analyzer verification and changed-Markdown formatting checks passed. Builds and restore use one MSBuild worker in this environment; the formatter and test platform require named-pipe access outside the sandbox.
