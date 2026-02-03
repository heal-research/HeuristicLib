using HEAL.HeuristicLib.Encodings.RealVector;
using HEAL.HeuristicLib.Optimization;

namespace HEAL.HeuristicLib.Problems;

public abstract class RealVectorProblem(Objective objective, RealVectorSearchSpace searchSpace) : Problem<RealVector, RealVectorSearchSpace>(objective, searchSpace);
