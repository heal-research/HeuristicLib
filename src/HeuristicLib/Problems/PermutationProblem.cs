using HEAL.HeuristicLib.Encodings;
using HEAL.HeuristicLib.Genotypes;
using HEAL.HeuristicLib.Optimization;

namespace HEAL.HeuristicLib.Problems;

public abstract class PermutationProblem(Objective objective, PermutationEncoding searchSpace) : Problem<Permutation, PermutationEncoding>(objective, searchSpace);
