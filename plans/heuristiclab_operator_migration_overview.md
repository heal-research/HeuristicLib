# HeuristicLab Operator Migration Overview

This file tracks operator and operator-test migration from HeuristicLab to HeuristicLib. It is intentionally explicit so that migration work can happen systematically instead of by memory.

Current scope: encoding operators with HeuristicLab unit-test coverage under `HeuristicLab.Tests/HeuristicLab.Encodings.*`. Problem-specific move evaluators, algorithm operators, and non-operator infrastructure can be added in later sections when we start migrating those areas.

## Status Legend

| Status | Meaning |
|---|---|
| `migrated` | A HeuristicLib equivalent exists. |
| `not yet migrated` | No equivalent HeuristicLib operator exists yet. |
| `will not be migrated` | We manually decided not to carry this operator or test target forward into HeuristicLib. |
| `partial` | Some tests or behavior are covered, but not the full HeuristicLab test surface. |
| `complete` | The currently known HeuristicLab unit-test surface for this operator has been migrated or is covered by equivalent tests. |
| `not started` | No direct migrated tests exist yet. |
| `not applicable` | The item is not currently modeled as a HeuristicLib operator/test target. |

## Type Migration Overview

| HeuristicLab encoding/type | HeuristicLib equivalent | Type status | Operator migration state | Unit-test migration state | Notes |
|---|---|---|---|---|---|
| Binary vector | `BoolVector` | migrated | not yet migrated | not started | Genotype exists; encoding-specific operators are not present yet. |
| Integer vector | `IntegerVector` | migrated | partial | not started | Some crossover and mutator equivalents exist. |
| Linear linkage | not yet migrated | not yet migrated | not yet migrated | not started | Encoding is not currently present. |
| Permutation | `Permutation` | migrated | partial | partial | Core genotype and selected operators exist; several HeuristicLab operators are still undecided. |
| Real vector | `RealVector` | migrated | partial | partial | Core genotype and selected operators exist. |
| Schedule | not yet migrated | not yet migrated | not yet migrated | not started | Encoding is not currently present. |
| Symbolic expression tree | `SymbolicExpressionTree` | migrated | partial | partial | Core tree, grammar/search-space, creators, crossover, and selected mutators exist. |

## Binary Vector Operators

| HeuristicLab operator/test | HeuristicLib equivalent | Operator status | Unit-test migration | Notes |
|---|---|---|---|---|
| `NPointCrossover` / `NPointCrossoverTest.cs` | not yet migrated | not yet migrated | not started | Candidate if binary-vector crossovers are added. |
| `SinglePositionBitflipManipulator` / `SinglePositionBitflipManipulatorTest.cs` | not yet migrated | not yet migrated | not started | Candidate if bool-vector mutators are added. |
| `SomePositionsBitflipManipulator` / `SomePositionsBitflipManipulatorTest.cs` | not yet migrated | not yet migrated | not started | Candidate if bool-vector mutators are added. |
| `UniformCrossover` / `UniformCrossoverTest.cs` | not yet migrated | not yet migrated | not started | Candidate if binary-vector crossovers are added. |

## Integer Vector Operators

| HeuristicLab operator/test | HeuristicLib equivalent | Operator status | Unit-test migration | Notes |
|---|---|---|---|---|
| `DiscreteCrossover` / `DiscreteCrossoverTest.cs` | `IntegerVectorCrossovers.DiscreteCrossover` | migrated | not started | Add exact examples and boundary tests. |
| `SinglePointCrossover` / `SinglePointCrossoverTest.cs` | `IntegerVectorCrossovers.SinglePointCrossover` | migrated | not started | Add exact examples and boundary tests. |
| `UniformOnePositionManipulator` / `UniformOnePositionManipulatorTest.cs` | `IntegerVectorMutators.UniformOnePositionManipulator` | migrated | not started | Add deterministic index/value test. |

## Linear Linkage Operators

| HeuristicLab operator/test | HeuristicLib equivalent | Operator status | Unit-test migration | Notes |
|---|---|---|---|---|
| `Conversions` / `ConversionsTest.cs` | not yet migrated | not yet migrated | not applicable | Encoding utility, not an operator in current HeuristicLib. |
| `GroupCrossover` / `GroupCrossoverTest.cs` | not yet migrated | not yet migrated | not started | Candidate only if linear-linkage encoding is introduced. |

## Permutation Operators

| HeuristicLab operator/test | HeuristicLib equivalent | Operator status | Unit-test migration | Notes |
|---|---|---|---|---|
| `CosaCrossover` / `CosaCrossoverTest.cs` | not yet migrated | not yet migrated | not started | Candidate if the operator is still desired. |
| `CyclicCrossover` / `CyclicCrossoverTest.cs` | not yet migrated | not yet migrated | not started | Candidate if the operator is still desired. |
| `CyclicCrossover2` / `CyclicCrossover2Test.cs` | not yet migrated | not yet migrated | not started | Candidate if the variant is still desired. |
| `EdgeRecombinationCrossover` / `EdgeRecombinationCrossoverTest.cs` | `PermutationCrossovers.EdgeRecombinationCrossover` | migrated | not started | Exact tie-breaking examples need review before migration. |
| `InsertionManipulator` / `InsertionManipulatorTest.cs` | not yet migrated | not yet migrated | not started | Candidate permutation mutator. |
| `InversionManipulator` / `InversionManipulatorTest.cs` | `PermutationMutators.InversionMutator` | migrated | complete | Exact reference example exists. |
| `MaximalPreservativeCrossover` / `MaximalPreservativeCrossoverTest.cs` | not yet migrated | not yet migrated | not started | Candidate if the operator is still desired. |
| `OrderBasedCrossover` / `OrderBasedCrossoverTest.cs` | not yet migrated | not yet migrated | not started | Candidate if the operator is still desired. |
| `OrderCrossover` / `OrderCrossoverTest.cs` | `PermutationCrossovers.OrderCrossover` | migrated | complete | Exact examples and unequal-length exception exist. |
| `OrderCrossover2` / `OrderCrossover2Test.cs` | not yet migrated | not yet migrated | not started | Candidate if the variant is still desired. |
| `PartiallyMatchedCrossover` / `PartiallyMatchedCrossoverTest.cs` | not yet migrated | not yet migrated | not started | Candidate if PMX is still desired. |
| `PermutationEqualityComparer` / `PermutationEqualityComparerTest.cs` | not yet migrated | not yet migrated | not applicable | Comparer/encoding utility rather than an operator. Track separately if reintroduced. |
| `PermutationManipulation` / `PermutationManipulationTest.cs` | `Permutation` random/manipulation helpers | migrated | partial | Some genotype tests exist; direct migration coverage needs review. |
| `PositionBasedCrossover` / `PositionBasedCrossoverTest.cs` | not yet migrated | not yet migrated | not started | Candidate if the operator is still desired. |
| `ScrambleManipulator` / `ScrambleManipulatorTest.cs` | not yet migrated | not yet migrated | not started | Candidate permutation mutator. |
| `Swap2Manipulator` / `Swap2ManipulatorTest.cs` | `PermutationMutators.SwapSingleSolutionMutator` | migrated | complete | Exact reference example exists. |
| `Swap3Manipulator` / `Swap3ManipulatorTest.cs` | not yet migrated | not yet migrated | not started | Candidate if three-position swap is desired. |
| `TranslocationInversionManipulator` / `TranslocationInversionManipulatorTest.cs` | not yet migrated | not yet migrated | not started | Candidate permutation mutator. |
| `TranslocationManipulator` / `TranslocationManipulatorTest.cs` | not yet migrated | not yet migrated | not started | Candidate permutation mutator. |
| `UniformLikeCrossover` / `UniformLikeCrossoverTest.cs` | not yet migrated | not yet migrated | not started | Candidate if the operator is still desired. |

## Real Vector Operators

| HeuristicLab operator/test | HeuristicLib equivalent | Operator status | Unit-test migration | Notes |
|---|---|---|---|---|
| `BlendAlphaCrossover` / `BlendAlphaCrossoverTest.cs` | not yet migrated | not yet migrated | not started | HeuristicLib has `AlphaBetaBlendCrossover`, but no direct alpha-only equivalent. |
| `BlendAlphaBetaCrossover` / `BlendAlphaBetaCrossoverTest.cs` | `RealVectorCrossovers.AlphaBetaBlendCrossover` | migrated | not started | Add exact examples and bounds tests. |
| `DiscreteCrossover` / `DiscreteCrossoverTest.cs` | not yet migrated | not yet migrated | not started | Candidate real-vector crossover. |
| `HeuristicCrossover` / `HeuristicCrossoverTest.cs` | not yet migrated | not yet migrated | not started | Candidate real-vector crossover. |
| `LocalCrossover` / `LocalCrossoverTest.cs` | not yet migrated | not yet migrated | not started | Candidate real-vector crossover. |
| `MichalewiczNonUniformAllPositionsManipulator` / `MichalewiczNonUniformAllPositionsManipulatorTest.cs` | not yet migrated | not yet migrated | not started | Candidate if iteration-dependent mutation is desired. |
| `MichalewiczNonUniformOnePositionManipulator` / `MichalewiczNonUniformOnePositionManipulatorTest.cs` | not yet migrated | not yet migrated | not started | Candidate if iteration-dependent mutation is desired. |
| `PolynomialAllPositionManipulator` / `PolynomialAllPositionManipulatorTest.cs` | `RealVectorMutators.PolynomialMutator` | migrated | partial | Current mutator should be checked against both one/all-position expectations. |
| `PolynomialOnePositionManipulator` / `PolynomialOnePositionManipulatorTest.cs` | `RealVectorMutators.PolynomialMutator` | migrated | partial | Current mutator should be checked against both one/all-position expectations. |
| `RandomConvexCrossover` / `RandomConvexCrossoverTest.cs` | not yet migrated | not yet migrated | not started | Candidate real-vector crossover. |
| `SimulatedBinaryCrossover` / `SimulatedBinaryCrossoverTest.cs` | `RealVectorCrossovers.SimulatedBinaryCrossover` | migrated | not started | Add exact examples and bounds tests. |
| `SinglePointCrossover` / `SinglePointCrossoverTest.cs` | `RealVectorCrossovers.SinglePointCrossover` | migrated | not started | Add exact cut-point tests. |
| `UniformOnePositionManipulator` / `UniformOnePositionManipulatorTest.cs` | not yet migrated | not yet migrated | not started | `GaussianMutator` exists, but not a direct uniform-one-position equivalent. |

## Schedule Operators

| HeuristicLab operator/test | HeuristicLib equivalent | Operator status | Unit-test migration | Notes |
|---|---|---|---|---|
| `DirectScheduleGTCrossover` / `DirectScheduleGTCrossoverTest.cs` | not yet migrated | not yet migrated | not started | Schedule encoding is not currently present. |
| `JSMJOXCrossover` / `JSMJOXCrossoverTest.cs` | not yet migrated | not yet migrated | not started | Schedule encoding is not currently present. |
| `JSMShiftChangeManipulator` / `JSMShiftChangeManipulatorTest.cs` | not yet migrated | not yet migrated | not started | Schedule encoding is not currently present. |
| `JSMSXXCrossover` / `JSMSXXCrossoverTest.cs` | not yet migrated | not yet migrated | not started | Schedule encoding is not currently present. |
| `PWRGOXCrossover` / `PWRGOXCrossoverTest.cs` | not yet migrated | not yet migrated | not started | Schedule encoding is not currently present. |
| `PWRPPXCrossover` / `PWRPPXCrossoverTest.cs` | not yet migrated | not yet migrated | not started | Schedule encoding is not currently present. |

## Symbolic Expression Tree Operators

| HeuristicLab operator/test | HeuristicLib equivalent | Operator status | Unit-test migration | Notes |
|---|---|---|---|---|
| `AllArchitectureAlteringOperators` / `AllArchitectureAlteringOperatorsTest.cs` | not yet migrated | not yet migrated | not started | Architecture-altering operators are not currently present. |
| `ArgumentCreater` / `ArgumentCreaterTest.cs` | not yet migrated | not yet migrated | not started | Original spelling preserved for traceability. |
| `ArgumentDeleter` / `ArgumentDeleterTest.cs` | not yet migrated | not yet migrated | not started | Architecture-altering operator. |
| `ArgumentDuplicater` / `ArgumentDuplicaterTest.cs` | not yet migrated | not yet migrated | not started | Architecture-altering operator. |
| `ChangeNodeTypeManipulation` / `ChangeNodeTypeManipulationTest.cs` | `SymbolicExpressionTreeMutators.ChangeNodeTypeManipulation` | migrated | not started | Add grammar-aware deterministic mutation tests. |
| `FullTreeCreator` / `FullTreeCreatorTest.cs` | `SymbolicExpressionTreeCreators.FullTreeCreator` | migrated | not started | Add exact structural/depth tests. |
| `Grammars` / `GrammarsTest.cs` | symbolic-expression-tree grammar/search-space types | migrated | partial | Track outside operator table if grammar coverage grows. |
| `GrowTreeCreator` / `GrowTreeCreatorTest.cs` | `SymbolicExpressionTreeCreators.GrowTreeCreator` | migrated | not started | Add depth/length and grammar containment tests. |
| `ProbabilisticTreeCreator` / `ProbabilisticTreeCreatorTest.cs` | `SymbolicExpressionTreeCreators.ProbabilisticTreeCreator` | migrated | not started | Add deterministic target-length and grammar containment tests. |
| `ReplaceBranchManipulation` / `ReplaceBranchManipulationTest.cs` | `SymbolicExpressionTreeMutators.ReplaceBranchManipulation` | migrated | not started | Add grammar-aware deterministic mutation tests. |
| `SubroutineCreater` / `SubroutineCreaterTest.cs` | not yet migrated | not yet migrated | not started | Original spelling preserved for traceability. |
| `SubroutineDeleter` / `SubroutineDeleterTest.cs` | not yet migrated | not yet migrated | not started | Architecture-altering operator. |
| `SubroutineDuplicater` / `SubroutineDuplicaterTest.cs` | not yet migrated | not yet migrated | not started | Architecture-altering operator. |
| `SubtreeCrossover` / `SubtreeCrossoverTest.cs` | `SymbolicExpressionTreeCrossovers.SubtreeCrossover` | migrated | partial | Direct tests exist; compare remaining structural cases with the original suite. |

## Additional Fields Worth Tracking

- API shape changes: note when a HeuristicLab operator is intentionally split, merged, renamed, or generalized in HeuristicLib.
- Randomness contract: record whether exact examples require deterministic random sequences, explicit cut points, or only invariant checks.
- Search-space and problem dependencies: record whether the operator depends only on a genotype, on a search space, or on a problem instance.
- Behavior deviations: explicitly document intentional differences from HeuristicLab behavior.
- Test migration source: keep the original HeuristicLab test filename in the table so missing cases are easy to audit.
- Coverage depth: distinguish exact oracle examples, boundary/exception tests, direct helper validation tests, and integration-only coverage.
- Migration decision: for `not yet migrated` entries, track whether the operator is planned, undecided, or should move to `will not be migrated` once we decide not to carry it forward.
