using HEAL.HeuristicLib.Operators.Neighborhoods;
using HEAL.HeuristicLib.Problems;

namespace HEAL.HeuristicLib.Encodings.IntegerVectors;

public abstract record IntegerVectorNeighborhood<TMove> : Neighborhood<IntegerVector, IntegerVectorSearchSpace, IProblem<IntegerVector, IntegerVectorSearchSpace>, TMove>;
