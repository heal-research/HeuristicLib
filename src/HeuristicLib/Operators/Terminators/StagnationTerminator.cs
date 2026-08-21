using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Operators.Terminators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators;

/// <summary>
/// Terminates after a configured number of consecutive produced search states without strict objective improvement.
/// </summary>
public sealed record StagnationTerminator<TCandidate>
    : StatefulTerminator<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>, PopulationState<TCandidate>, StagnationTerminator<TCandidate>.ExecutionState>
{
    public sealed class ExecutionState
    {
        public ObjectiveVector? BestObjectiveVectorSoFar { get; set; }
        public int StagnationCounter { get; set; }
    }

    /// <summary>
    /// Gets the number of consecutive produced search states without strict objective improvement that triggers
    /// termination. The expected value is positive.
    /// </summary>
    /// <remarks>A zero or negative threshold terminates on the first checked produced search state.</remarks>
    public int StagnationThreshold { get; init; } = 20;

    protected override ExecutionState CreateInitialState() => new();

    protected override bool IsTerminalState(PopulationState<TCandidate> algorithmState, ExecutionState executionState, ISearchSpace<TCandidate> searchSpace, IProblem<TCandidate, ISearchSpace<TCandidate>> problem)
    {
        if (StagnationThreshold <= 0)
            return true;

        executionState.BestObjectiveVectorSoFar ??= problem.Objective.Worst;

        var comparer = problem.Objective.TotalOrderComparer;

        var currentBestObjectiveVector = algorithmState.Population.Select(s => s.ObjectiveVector).OrderBy(i => i, comparer).First();
        if (comparer.Compare(currentBestObjectiveVector, executionState.BestObjectiveVectorSoFar) < 0)
        {
            executionState.BestObjectiveVectorSoFar = currentBestObjectiveVector;
            executionState.StagnationCounter = 0;
        }
        else
        {
            executionState.StagnationCounter++;
            if (executionState.StagnationCounter >= StagnationThreshold)
            {
                return true;
            }
        }

        return false;
    }
}

public static class StagnationTerminator
{
    public static StagnationTerminator<TCandidate> For<TCandidate, TSearchSpace>(IProblem<TCandidate, TSearchSpace> problem, int stagnationThreshold = 20)
        where TSearchSpace : class, ISearchSpace<TCandidate> => new() { StagnationThreshold = stagnationThreshold };
}
