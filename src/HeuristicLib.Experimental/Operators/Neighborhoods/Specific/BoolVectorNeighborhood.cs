using HEAL.HeuristicLib.Genotypes.Vectors;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces.Vectors;

namespace HEAL.HeuristicLib.Operators.Neighborhoods;

public abstract record BoolVectorNeighborhood<TMove> : Neighborhood<BoolVector, BoolVectorSearchSpace, IProblem<BoolVector, BoolVectorSearchSpace>, TMove>;
