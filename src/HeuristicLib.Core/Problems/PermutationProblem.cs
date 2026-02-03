using HEAL.HeuristicLib.Encodings.Permutation;
using HEAL.HeuristicLib.Optimization;

namespace HEAL.HeuristicLib.Problems;

public abstract class PermutationProblem(Objective objective, PermutationSearchSpace searchSpace) :
  Problem<Permutation, PermutationSearchSpace>(objective, searchSpace);
