using HEAL.HeuristicLib.Operators.Neighborhoods;
using HEAL.HeuristicLib.Problems;

namespace HEAL.HeuristicLib.Encodings.BoolVectors;

public abstract record BoolVectorNeighborhood<TMove> : Neighborhood<BoolVector, BoolVectorSearchSpace, IProblem<BoolVector, BoolVectorSearchSpace>, TMove>;
