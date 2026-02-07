using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Operators.Interceptors;

// ToDo: think about another name, maybe PipelineInceptor or SequentialInterceptor.
public class MultiInterceptor<TGenotype, TAlgorithmState, TSearchSpace, TProblem>
  : Interceptor<TGenotype, TAlgorithmState, TSearchSpace, TProblem> 
  where TAlgorithmState : class, IAlgorithmState
  where TSearchSpace : class, ISearchSpace<TGenotype> 
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
{
  private readonly IReadOnlyList<IInterceptor<TGenotype, TAlgorithmState, TSearchSpace, TProblem>> interceptors;
  
  public MultiInterceptor(IReadOnlyList<IInterceptor<TGenotype, TAlgorithmState, TSearchSpace, TProblem>> interceptors) {
    this.interceptors = interceptors;
  }

  public override Instance CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) {
    var interceptorInstances = interceptors.Select(i => i.CreateExecutionInstance(instanceRegistry)).ToList();
    return new Instance(interceptorInstances);
  }

  public class Instance
    : InterceptorInstance<TGenotype, TAlgorithmState, TSearchSpace, TProblem>
  {
    private readonly IReadOnlyList<IInterceptorInstance<TGenotype, TAlgorithmState, TSearchSpace, TProblem>> interceptors;
    
    public Instance(IReadOnlyList<IInterceptorInstance<TGenotype, TAlgorithmState, TSearchSpace, TProblem>> interceptors) {
      this.interceptors = interceptors;
    }

    public override TAlgorithmState Transform(TAlgorithmState currentState, TAlgorithmState? previousState, TSearchSpace searchSpace, TProblem problem)
    {
      return interceptors.Aggregate(currentState, (current, interceptor) => interceptor.Transform(current, previousState, searchSpace, problem));
    }
  }
}

// public static class MultiInterceptor
// {
//   public static MultiInterceptor<TGenotype, TAlgorithmState, TSearchSpace, TProblem>? Create<TGenotype, TAlgorithmState, TSearchSpace, TProblem>(
//     IInterceptor<TGenotype, TAlgorithmState, TSearchSpace, TProblem>? interceptor)
//     where TSearchSpace : class, ISearchSpace<TGenotype>
//     where TProblem : class, IProblem<TGenotype, TSearchSpace>
//     where TAlgorithmState : class, IAlgorithmState
//   {
//     var list = new List<IInterceptor<TGenotype, TAlgorithmState, TSearchSpace, TProblem>>();
//     if (interceptor != null) {
//       list.Add(interceptor);
//     }
//
//     return list.Count == 0 ? null : new MultiInterceptor<TGenotype, TAlgorithmState, TSearchSpace, TProblem>(list);
//   }
// }
