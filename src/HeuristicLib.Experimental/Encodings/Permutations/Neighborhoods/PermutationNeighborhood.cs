using HEAL.HeuristicLib.Operators.Neighborhoods;
using HEAL.HeuristicLib.Problems;

namespace HEAL.HeuristicLib.Encodings.Permutations;

public abstract record PermutationNeighborhood<TMove> : Neighborhood<Permutation, PermutationSearchSpace, IProblem<Permutation, PermutationSearchSpace>, TMove>;
