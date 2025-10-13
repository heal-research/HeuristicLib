using HEAL.HeuristicLib.Encodings;
using HEAL.HeuristicLib.Genotypes;
using HEAL.HeuristicLib.Optimization;

namespace HEAL.HeuristicLib.Problems;

public abstract class RealVectorProblem(Objective objective, RealVectorEncoding searchSpace) : Problem<RealVector, RealVectorEncoding>(objective, searchSpace);
