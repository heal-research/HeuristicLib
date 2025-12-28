using HEAL.HeuristicLib.Encodings.Vectors;
using HEAL.HeuristicLib.Genotypes.Vectors;
using HEAL.HeuristicLib.Optimization;

namespace HEAL.HeuristicLib.Problems;

public abstract class PermutationProblem(Objective objective, PermutationEncoding searchSpace) :
  Problem<Permutation, PermutationEncoding>(objective, searchSpace);
