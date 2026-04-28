using HEAL.HeuristicLib.Genotypes.Vectors;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.SearchSpaces.Vectors;

namespace HEAL.HeuristicLib.Problems;

public abstract class PermutationProblem(Objective objective, PermutationSearchSpace searchSpace) :
  SingleSolutionProblem<Permutation, PermutationSearchSpace>(objective, searchSpace);
