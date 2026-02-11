using Generator.Equals;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Operators.Interceptors;

// ToDo: think about another name, maybe PipelineInceptor or SequentialInterceptor.
[Equatable]
public partial record MultiInterceptor<TGenotype, TAlgorithmState, TSearchSpace, TProblem>
  : Interceptor<TGenotype, TAlgorithmState, TSearchSpace, TProblem>
  where TAlgorithmState : class, IAlgorithmState
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
{
  [OrderedEquality] public ImmutableArray<IInterceptor<TGenotype, TAlgorithmState, TSearchSpace, TProblem>> Interceptors { get; }

  public MultiInterceptor(ImmutableArray<IInterceptor<TGenotype, TAlgorithmState, TSearchSpace, TProblem>> interceptors)
  {
    Interceptors = interceptors;
  }

  public override Instance CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry)
  {
    var interceptorInstances = Interceptors.Select(instanceRegistry.Resolve).ToList();
    return new Instance(interceptorInstances);
  }

  public new class Instance
    : Interceptor<TGenotype, TAlgorithmState, TSearchSpace, TProblem>.Instance
  {
    private readonly IReadOnlyList<IInterceptorInstance<TGenotype, TAlgorithmState, TSearchSpace, TProblem>> interceptors;

    public Instance(IReadOnlyList<IInterceptorInstance<TGenotype, TAlgorithmState, TSearchSpace, TProblem>> interceptors)
    {
      this.interceptors = interceptors;
    }

    public override TAlgorithmState Transform(TAlgorithmState currentState, TAlgorithmState? previousState, TSearchSpace searchSpace, TProblem problem)
    {
      return interceptors.Aggregate(currentState, (current, interceptor) => interceptor.Transform(current, previousState, searchSpace, problem));
    }
  }
}
