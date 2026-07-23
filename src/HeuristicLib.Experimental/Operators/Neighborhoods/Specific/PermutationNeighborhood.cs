namespace HEAL.HeuristicLib.Operators.Neighborhoods;

using Genotypes.Vectors;
using Problems;
using SearchSpaces.Vectors;

public abstract record PermutationNeighborhood<TMove> : Neighborhood<Permutation, PermutationSearchSpace, IProblem<Permutation, PermutationSearchSpace>, TMove>;
