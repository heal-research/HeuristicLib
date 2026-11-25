using HEAL.HeuristicLib.Encodings.RealVectors;
using HEAL.HeuristicLib.Objectives;

namespace HEAL.HeuristicLib.Problems;

public abstract class RealVectorProblem<TSelf>(ObjectiveDirections objective, BoundedRealVectorSearchSpace searchSpace)
    : SingleSolutionProblem<TSelf, RealVector, BoundedRealVectorSearchSpace>(objective, searchSpace)
    where TSelf : RealVectorProblem<TSelf>;
