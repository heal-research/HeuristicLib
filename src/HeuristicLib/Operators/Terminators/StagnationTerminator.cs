using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Operators.Terminators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators;

/// <summary>
/// Terminates after a configured number of consecutive produced search states without strict objective improvement.
/// </summary>
public sealed record StagnationTerminator<TCandidate>
    : StatefulTerminator<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>, PopulationState<TCandidate>, StagnationTerminator<TCandidate>.State>
{
    public sealed class State
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

    protected override State CreateScopedState() => new();

    protected override bool IsTerminalState(PopulationState<TCandidate> searchState, State operatorState, IRandomNumberGenerator random, ISearchSpace<TCandidate> searchSpace, IProblem<TCandidate, ISearchSpace<TCandidate>> problem)
    {
        if (StagnationThreshold <= 0)
            return true;

        operatorState.BestObjectiveVectorSoFar ??= problem.Objective.Worst;

        var comparer = problem.Objective.TotalOrderComparer;

        var currentBestObjectiveVector = searchState.Population.Select(s => s.ObjectiveVector).OrderBy(i => i, comparer).First();
        if (comparer.Compare(currentBestObjectiveVector, operatorState.BestObjectiveVectorSoFar) < 0)
        {
            operatorState.BestObjectiveVectorSoFar = currentBestObjectiveVector;
            operatorState.StagnationCounter = 0;
        }
        else
        {
            operatorState.StagnationCounter++;
            if (operatorState.StagnationCounter >= StagnationThreshold)
            {
                return true;
            }
        }

        return false;
    }
}

public static class StagnationTerminator
{
    public static StagnationTerminator<TCandidate> For<TCandidate>(IProblem<TCandidate, ISearchSpace<TCandidate>> problem, int stagnationThreshold = 20) => new() { StagnationThreshold = stagnationThreshold };
}
