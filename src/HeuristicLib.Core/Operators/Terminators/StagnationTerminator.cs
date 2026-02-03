using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Operators.Terminators;

public class StagnationTerminator<TGenotype> 
  : Terminator<TGenotype, PopulationState<TGenotype>, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>>
  where TGenotype : class
{
  private readonly int window;
  
  public StagnationTerminator(int window = 20)
  {
    this.window = window;
  }

  public override Func<PopulationState<TGenotype>, bool> CreateShouldTerminatePredicate(ISearchSpace<TGenotype> searchSpace, IProblem<TGenotype, ISearchSpace<TGenotype>> problem)
  {
    var comparer = problem.Objective.TotalOrderComparer;
    var bestQualitySoFar = problem.Objective.Worst;
    int stagnationCounter = 0;

    return ShouldTerminatePredicate;

    bool ShouldTerminatePredicate(PopulationState<TGenotype> state)
    {
      var currentBestQuality = state.Population.Select(s => s.ObjectiveVector).OrderBy(i => i, comparer).First();
      if (comparer.Compare(currentBestQuality, bestQualitySoFar) < 0) {
        bestQualitySoFar = currentBestQuality;
        stagnationCounter = 0;
      } else {
        stagnationCounter++;
        if (stagnationCounter >= window) {
          return true;
        }
      }

      return false;
    }
  }
}

