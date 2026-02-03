using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Operators.Prototypes;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Operators.Analyzers;

public class FuncAnalysis<T, TE, TP, TRes>(Action<TRes?, TRes> action) : AttachedAnalysis<T, TE, TP, TRes> where TE : class, ISearchSpace<T> where TP : class, IProblem<T, TE> where TRes : IAlgorithmState where T : class
{
  public override void AfterInterception(TRes currentIterationResult, TRes? previousIterationResult, TE encoding, TP problem) => action.Invoke(previousIterationResult, currentIterationResult);
}

public static class FuncAnalysis
{
  public static FuncAnalysis<TGenotype, TE, TP, TRes> Create<TGenotype, TE, TP, TRes>(IAlgorithmBuilder<TGenotype, TE, TP, TRes> prototype, Action<TRes?, TRes> action)
    where TE : class, ISearchSpace<TGenotype>
    where TP : class, IProblem<TGenotype, TE>
    where TRes : PopulationAlgorithmState<TGenotype>
    where TGenotype : class
  {
    var t = new FuncAnalysis<TGenotype, TE, TP, TRes>(action);
    t.AttachTo(prototype);

    return t;
  }
}
