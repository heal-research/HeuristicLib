using HEAL.HeuristicLib.Genotypes.Vectors;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.SearchSpaces.Vectors;

namespace HEAL.HeuristicLib.Problems;

public abstract class RealVectorProblem(ObjectiveDirections objective, RealVectorSearchSpace searchSpace) : SingleSolutionProblem<RealVector, RealVectorSearchSpace>(objective, searchSpace);
