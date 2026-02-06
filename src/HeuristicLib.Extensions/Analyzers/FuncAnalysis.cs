using HEAL.HeuristicLib.Observers;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Analyzers;

public class FuncAnalysis<T, TS, TP, TRes>(Action<TRes?, TRes> action) : IInterceptorObserver<T, TS, TP, TRes> where TS : class, ISearchSpace<T> where TP : class, IProblem<T, TS> where TRes : class, IAlgorithmState where T : class
{
  public void AfterInterception(TRes currentIterationState, TRes? previousIterationState, TS searchSpace, TP problem) => action.Invoke(previousIterationState, currentIterationState);
}

public static class FuncAnalysis
{
  public static IInterceptor<T, TRes, TS, TP> Attach<T, TS, TP, TRes>(IInterceptor<T, TRes, TS, TP> interceptor, Action<TRes?, TRes> action)
    where TS : class, ISearchSpace<T>
    where TP : class, IProblem<T, TS>
    where TRes : class, IAlgorithmState
    where T : class => new FuncAnalysis<T, TS, TP, TRes>(action).WrapInterceptor(interceptor);
}
