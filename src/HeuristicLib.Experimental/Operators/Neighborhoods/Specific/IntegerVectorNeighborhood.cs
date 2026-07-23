namespace HEAL.HeuristicLib.Operators.Neighborhoods;

using Genotypes.Vectors;
using Problems;
using SearchSpaces.Vectors;

public abstract record IntegerVectorNeighborhood<TMove> : Neighborhood<IntegerVector, IntegerVectorSearchSpace, IProblem<IntegerVector, IntegerVectorSearchSpace>, TMove>;
