using HEAL.HeuristicLib.Encodings.RealVectors;
using HEAL.HeuristicLib.Objectives;

namespace HEAL.HeuristicLib.Problems;

public abstract class RealVectorProblem(ObjectiveDirections objective, RealVectorSearchSpace searchSpace) : SingleSolutionProblem<RealVector, RealVectorSearchSpace>(objective, searchSpace);
