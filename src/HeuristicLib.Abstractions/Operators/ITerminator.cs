using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Operators;

public interface ITerminator<TGenotype, in TAlgorithmState, in TSearchSpace, in TProblem>
  : IOperator<ITerminatorInstance<TGenotype, TAlgorithmState, TSearchSpace, TProblem>>
  where TAlgorithmState : IAlgorithmState
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : IProblem<TGenotype, TSearchSpace>
{
}

public interface ITerminatorInstance<TGenotype, in TAlgorithmState, in TSearchSpace, in TProblem>
  : IOperatorInstance
  where TAlgorithmState : IAlgorithmState
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : IProblem<TGenotype, TSearchSpace>
{
  bool ShouldTerminate(TAlgorithmState state, TSearchSpace searchSpace, TProblem problem);
}

public static class TerminatorExtension
{
  extension<TGenotype, TAlgorithmState, TSearchSpace, TProblem>(ITerminatorInstance<TGenotype, TAlgorithmState, TSearchSpace, TProblem> terminatorInstance)
    where TAlgorithmState : IAlgorithmState
    where TSearchSpace : class, ISearchSpace<TGenotype>
    where TProblem : IProblem<TGenotype, TSearchSpace>
  {
    public bool ShouldContinue(TAlgorithmState state, TSearchSpace searchSpace, TProblem problem)
    {
      return !terminatorInstance.ShouldTerminate(state, searchSpace, problem);
    }
  }
}
