using HEAL.HeuristicLib.Genotypes.Vectors;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces.Vectors;

namespace HEAL.HeuristicLib.Operators.Neighborhoods;

public abstract record PermutationNeighborhood<TMove> : Neighborhood<Permutation, PermutationSearchSpace, IProblem<Permutation, PermutationSearchSpace>, TMove>;
