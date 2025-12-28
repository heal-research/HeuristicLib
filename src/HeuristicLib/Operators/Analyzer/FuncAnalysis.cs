using HEAL.HeuristicLib.Operators.Prototypes;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Operators.Analyzer;

public class FuncAnalysis<T, TS, TP, TRes>(Action<TRes?, TRes> action) : AttachedAnalysis<T, TS, TP, TRes> where TS : class, ISearchSpace<T> where TP : class, IProblem<T, TS> where TRes : IIterationResult where T : class {
  public override void AfterInterception(TRes currentIterationResult, TRes? previousIterationResult, TS searchSpace, TP problem) => action.Invoke(previousIterationResult, currentIterationResult);
}

public static class FuncAnalysis {
  public static FuncAnalysis<TGenotype, TS, TP, TRes> Create<TGenotype, TS, TP, TRes>(IAlgorithmBuilder<TGenotype, TS, TP, TRes> prototype, Action<TRes?, TRes> action)
    where TS : class, ISearchSpace<TGenotype>
    where TP : class, IProblem<TGenotype, TS>
    where TRes : PopulationIterationResult<TGenotype>
    where TGenotype : class {
    var t = new FuncAnalysis<TGenotype, TS, TP, TRes>(action);
    t.AttachTo(prototype);
    return t;
  }
}
