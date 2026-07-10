namespace HEAL.HeuristicLib.Operators.Neighborhoods;

using Genotypes.Vectors;
using Problems;
using SearchSpaces.Vectors;

public abstract record RealVectorNeighborhood<TMove> : Neighborhood<RealVector, RealVectorSearchSpace, IProblem<RealVector, RealVectorSearchSpace>, TMove>;
