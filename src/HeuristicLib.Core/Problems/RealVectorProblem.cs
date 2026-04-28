using HEAL.HeuristicLib.Genotypes.Vectors;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.SearchSpaces.Vectors;

namespace HEAL.HeuristicLib.Problems;

public abstract class RealVectorProblem(Objective objective, RealVectorSearchSpace searchSpace) : SingleSolutionProblem<RealVector, RealVectorSearchSpace>(objective, searchSpace);
