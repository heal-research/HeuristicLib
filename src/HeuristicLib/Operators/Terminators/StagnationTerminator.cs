using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Execution;
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
    : Terminator<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>, PopulationState<TCandidate>>
{
    /// <summary>
    /// Gets the number of consecutive produced search states without strict objective improvement that triggers
    /// termination. The expected value is positive.
    /// </summary>
    /// <remarks>A zero or negative threshold terminates on the first checked produced search state.</remarks>
    public int StagnationThreshold { get; init; } = 20;

    public static Parameter<StagnationTerminator<TCandidate>, int> StagnationThresholdParameter { get; } =
        new(nameof(StagnationThreshold), static terminator => terminator.StagnationThreshold, static (terminator, value) => terminator with { StagnationThreshold = value });

    public static IReadOnlyList<IParameter> Parameters { get; } = [StagnationThresholdParameter];

    public override IReadOnlyList<IParameter> GetParameters() => [.. base.GetParameters(), .. Parameters];

    protected override TerminationExecutor<PopulationState<TCandidate>> CreateExecutor(ResolutionScope<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>, PopulationState<TCandidate>> scope) => new Executor(StagnationThreshold, scope.Problem);

    private sealed class Executor(int threshold, IProblem problem)
        : TerminationExecutor<PopulationState<TCandidate>>
    {
        private ObjectiveVector? bestObjectiveVectorSoFar;
        private int stagnationCounter;

        public override bool IsTerminalState(PopulationState<TCandidate> searchState, IRandomNumberGenerator random)
        {
            if (threshold <= 0)
                return true;

            bestObjectiveVectorSoFar ??= problem.Objective.Worst;

            var comparer = problem.Objective.TotalOrderComparer;

            var currentBestObjectiveVector = searchState.Population.EvaluatedCandidates.Select(s => s.ObjectiveVector).OrderBy(i => i, comparer).First();
            if (comparer.Compare(currentBestObjectiveVector, bestObjectiveVectorSoFar) < 0)
            {
                bestObjectiveVectorSoFar = currentBestObjectiveVector;
                stagnationCounter = 0;
            }
            else
            {
                stagnationCounter++;
                if (stagnationCounter >= threshold)
                {
                    return true;
                }
            }

            return false;
        }
    }
}

public static class StagnationTerminator
{
    public static StagnationTerminator<TCandidate> For<TCandidate>(IProblem<TCandidate, ISearchSpace<TCandidate>> problem, int stagnationThreshold = 20) => new() { StagnationThreshold = stagnationThreshold };
}
