using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Operators;

public static class TerminatorExtension
{
  extension<TGenotype, TSearchSpace, TProblem, TSearchState>(ITerminatorInstance<TGenotype, TSearchSpace, TProblem, TSearchState> terminatorInstance)
    where TSearchSpace : class, ISearchSpace<TGenotype>
    where TProblem : IProblem<TGenotype, TSearchSpace>
    where TSearchState : ISearchState
  {
    public bool ShouldContinue(TSearchSpace searchSpace, TProblem problem, TSearchState state)
    {
      return !terminatorInstance.ShouldTerminate(state, searchSpace, problem);
    }
  }
}

