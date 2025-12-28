using HEAL.HeuristicLib.Encodings.Vectors;
using HEAL.HeuristicLib.Genotypes.Vectors;
using HEAL.HeuristicLib.Optimization;

namespace HEAL.HeuristicLib.Problems;

public abstract class RealVectorProblem(Objective objective, RealVectorEncoding searchSpace) : Problem<RealVector, RealVectorEncoding>(objective, searchSpace);
