using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Operators;

public interface ITerminator<TGenotype, in TSearchSpace, in TProblem, in TAlgorithmState>
  : IOperator<ITerminatorInstance<TGenotype, TSearchSpace, TProblem, TAlgorithmState>>
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : IProblem<TGenotype, TSearchSpace>
  where TAlgorithmState : IAlgorithmState;

public interface ITerminatorInstance<TGenotype, in TSearchSpace, in TProblem, in TAlgorithmState>
  : IOperatorInstance
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : IProblem<TGenotype, TSearchSpace>
  where TAlgorithmState : IAlgorithmState
{
  bool ShouldTerminate(TAlgorithmState state, TSearchSpace searchSpace, TProblem problem);
}

public static class TerminatorExtension
{
  extension<TGenotype, TSearchSpace, TProblem, TAlgorithmState>(ITerminatorInstance<TGenotype, TSearchSpace, TProblem, TAlgorithmState> terminatorInstance)
    where TSearchSpace : class, ISearchSpace<TGenotype>
    where TProblem : IProblem<TGenotype, TSearchSpace>
    where TAlgorithmState : IAlgorithmState
  {
    public bool ShouldContinue(TSearchSpace searchSpace, TProblem problem, TAlgorithmState state)
    {
      return !terminatorInstance.ShouldTerminate(state, searchSpace, problem);
    }
  }
}
