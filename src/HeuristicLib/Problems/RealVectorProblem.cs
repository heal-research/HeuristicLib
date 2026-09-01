using HEAL.HeuristicLib.Encodings.RealVectors;
using HEAL.HeuristicLib.Objectives;

namespace HEAL.HeuristicLib.Problems;

public abstract class RealVectorProblem(ObjectiveDirections objective, BoundedRealVectorSearchSpace searchSpace) : SingleSolutionProblem<RealVector, BoundedRealVectorSearchSpace>(objective, searchSpace);
