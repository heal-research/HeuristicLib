using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Operators.Terminators;

public record StagnationTerminator<TGenotype>
  : StatefulTerminator<TGenotype, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>, PopulationState<TGenotype>, StagnationTerminator<TGenotype>.State>
{
  public sealed class State
  {
    public ObjectiveVector? BestQualitySoFar { get; set; }
    public int StagnationCounter { get; set; }
  }

  private readonly int window;

  public StagnationTerminator(int window = 20)
  {
    this.window = window;
  }

  protected override State CreateInitialState() => new();

  protected override bool ShouldTerminate(PopulationState<TGenotype> algorithmState, State terminatorState, ISearchSpace<TGenotype> searchSpace, IProblem<TGenotype, ISearchSpace<TGenotype>> problem)
  {
    terminatorState.BestQualitySoFar ??= problem.Objective.Worst;

    var comparer = problem.Objective.TotalOrderComparer;

    var currentBestQuality = algorithmState.Population.Select(s => s.ObjectiveVector).OrderBy(i => i, comparer).First();
    if (comparer.Compare(currentBestQuality, terminatorState.BestQualitySoFar) < 0) {
      terminatorState.BestQualitySoFar = currentBestQuality;
      terminatorState.StagnationCounter = 0;
    } else {
      terminatorState.StagnationCounter++;
      if (terminatorState.StagnationCounter >= window) {
        return true;
      }
    }

    return false;
  }
}
