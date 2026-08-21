using HEAL.HeuristicLib.Operators.Neighborhoods;
using HEAL.HeuristicLib.Problems;

namespace HEAL.HeuristicLib.Encodings.RealVectors;

public abstract record RealVectorNeighborhood<TMove> : Neighborhood<RealVector, RealVectorSearchSpace, IProblem<RealVector, RealVectorSearchSpace>, TMove>;
