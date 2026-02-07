namespace HEAL.HeuristicLib.Analyzers;

// public class FuncAnalysis<T, TRes, TS, TP>(Action<TRes?, TRes> action) 
//   : IInterceptorObserver<T, TRes, TS, TP>
//   where TS : class, ISearchSpace<T>
//   where TP : class, IProblem<T, TS> 
//   where TRes : class, IAlgorithmState
//   where T : class
// {
//   public void AfterInterception(TRes newState, TRes currentState, TRes? previousState, TS searchSpace, TP problem) => action.Invoke(previousState, currentState);
// }
//
// public static class FuncAnalysis
// {
//   public static IInterceptor<T, TRes, TS, TP> Attach<T, TS, TP, TRes>(IInterceptor<T, TRes, TS, TP> interceptor, Action<TRes?, TRes> action)
//     where TS : class, ISearchSpace<T>
//     where TP : class, IProblem<T, TS>
//     where TRes : class, IAlgorithmState
//     where T : class
//   {
//     return new FuncAnalysis<T, TRes, TS, TP>(action).WrapInterceptor(interceptor);
//   }
// }
