using HEAL.HeuristicLib.Encodings.RealVector;
using HEAL.HeuristicLib.Optimization;

namespace HEAL.HeuristicLib.Problems;

public abstract class RealVectorProblem(Objective objective, RealVectorEncoding searchSpace) : Problem<RealVector, RealVectorEncoding>(objective, searchSpace);
