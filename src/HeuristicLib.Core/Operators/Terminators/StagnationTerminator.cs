using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Optimization;
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

  public override Instance CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry)
  {
    return new Instance(this.window);
  }

  public class Instance
    : TerminatorInstance<TGenotype, PopulationState<TGenotype>, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>>
  {
    private readonly int window;

    private ObjectiveVector? bestQualitySoFar;
    private int stagnationCounter = 0;

    public Instance(int window)
    {
      this.window = window;
    }
    
    public override bool ShouldTerminate(PopulationState<TGenotype> state, ISearchSpace<TGenotype> searchSpace, IProblem<TGenotype, ISearchSpace<TGenotype>> problem)
    {
      bestQualitySoFar ??= problem.Objective.Worst;
      
      var comparer = problem.Objective.TotalOrderComparer;

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

