using HEAL.HeuristicLib.Optimization;

namespace HEAL.HeuristicLib.States;

public record SingleSolutionState<T> : PopulationState<T>
{
    public EvaluatedCandidate<T> EvaluatedCandidate => Population.Single();
}
