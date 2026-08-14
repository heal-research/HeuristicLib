using HEAL.HeuristicLib.Genotypes.Vectors;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces.Vectors;

namespace HEAL.HeuristicLib.Operators.Neighborhoods;

public abstract record IntegerVectorNeighborhood<TMove> : Neighborhood<IntegerVector, IntegerVectorSearchSpace, IProblem<IntegerVector, IntegerVectorSearchSpace>, TMove>;
