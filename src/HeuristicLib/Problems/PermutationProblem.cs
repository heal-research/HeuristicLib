using HEAL.HeuristicLib.Encodings.Permutations;
using HEAL.HeuristicLib.Objectives;

namespace HEAL.HeuristicLib.Problems;

public abstract class PermutationProblem(ObjectiveDirections objective, PermutationSearchSpace searchSpace) :
  SingleSolutionProblem<Permutation, PermutationSearchSpace>(objective, searchSpace);
