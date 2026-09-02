using HEAL.HeuristicLib.Encodings.Permutations;
using HEAL.HeuristicLib.Objectives;

namespace HEAL.HeuristicLib.Problems;

public abstract class PermutationProblem<TSelf>(ObjectiveDirections objective, PermutationSearchSpace searchSpace) :
  SingleSolutionProblem<TSelf, Permutation, PermutationSearchSpace>(objective, searchSpace)
  where TSelf : PermutationProblem<TSelf>;
