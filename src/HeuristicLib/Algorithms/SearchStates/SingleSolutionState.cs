using HEAL.HeuristicLib.Objectives;

namespace HEAL.HeuristicLib.Algorithms;

public record SingleSolutionState<T> : PopulationState<T>
{
    public EvaluatedCandidate<T> EvaluatedCandidate => Population.Single();
}

public static class SingleSolutionState
{
    public static SingleSolutionState<TCandidate> From<TCandidate>(EvaluatedCandidate<TCandidate> evaluatedCandidate) =>
        new() { Population = Population.From([evaluatedCandidate]) };

    public static SingleSolutionState<TCandidate> From<TCandidate>(TCandidate candidate, ObjectiveVector objectiveVector) =>
        EvaluatedCandidate.From(candidate, objectiveVector).ToSingleSolutionState();
}

public static class SingleSolutionStateExtensions
{
    extension<TCandidate>(EvaluatedCandidate<TCandidate> evaluatedCandidate)
    {
        public SingleSolutionState<TCandidate> ToSingleSolutionState() => SingleSolutionState.From(evaluatedCandidate);
    }
}
