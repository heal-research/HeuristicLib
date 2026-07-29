using HEAL.HeuristicLib.Genotypes.Vectors;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces.Vectors;

namespace HEAL.HeuristicLib.Operators.Neighborhoods;

public abstract record RealVectorNeighborhood<TMove> : Neighborhood<RealVector, RealVectorSearchSpace, IProblem<RealVector, RealVectorSearchSpace>, TMove>;
