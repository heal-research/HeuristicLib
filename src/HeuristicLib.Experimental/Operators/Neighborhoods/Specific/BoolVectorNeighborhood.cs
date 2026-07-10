namespace HEAL.HeuristicLib.Operators.Neighborhoods;

using Genotypes.Vectors;
using Problems;
using SearchSpaces.Vectors;

public abstract record BoolVectorNeighborhood<TMove> : Neighborhood<BoolVector, BoolVectorSearchSpace, IProblem<BoolVector, BoolVectorSearchSpace>, TMove>;
