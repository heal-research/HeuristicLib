using HEAL.HeuristicLib.Genotypes.Vectors;
using HEAL.HeuristicLib.Operators.Creators;
using HEAL.HeuristicLib.Optimization;

namespace HEAL.HeuristicLib.Problems;

public abstract class PermutationProblem(Objective objective, PermutationSearchSpace searchSpace) :
  Problem<Permutation, PermutationSearchSpace>(objective, searchSpace);
