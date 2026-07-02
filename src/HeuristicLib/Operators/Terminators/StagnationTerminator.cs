using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Operators.Terminators;

public record StagnationTerminator<TCandidate>
  : Terminator<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>, PopulationState<TCandidate>, StagnationTerminator<TCandidate>.ExecutionState>
{
    public sealed class ExecutionState
    {
        public ObjectiveVector? BestQualitySoFar { get; set; }
        public int StagnationCounter { get; set; }
    }

    private readonly int window;

    public StagnationTerminator(int window = 20)
    {
        this.window = window;
    }

    protected override ExecutionState CreateInitialState() => new();

    protected override bool IsTerminalState(PopulationState<TCandidate> algorithmState, ExecutionState executionState, ISearchSpace<TCandidate> searchSpace, IProblem<TCandidate, ISearchSpace<TCandidate>> problem)
    {
        executionState.BestQualitySoFar ??= problem.Objective.Worst;

        var comparer = problem.Objective.TotalOrderComparer;

        var currentBestQuality = algorithmState.Population.Select(s => s.ObjectiveVector).OrderBy(i => i, comparer).First();
        if (comparer.Compare(currentBestQuality, executionState.BestQualitySoFar) < 0)
        {
            executionState.BestQualitySoFar = currentBestQuality;
            executionState.StagnationCounter = 0;
        }
        else
        {
            executionState.StagnationCounter++;
            if (executionState.StagnationCounter >= window)
            {
                return true;
            }
        }

        return false;
    }
}
