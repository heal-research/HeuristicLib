using Generator.Equals;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Operators.Interceptors;

// ToDo: think about another name, maybe PipelineInceptor or SequentialInterceptor.
[Equatable]
public partial record MultiInterceptor<TGenotype, TAlgorithmState, TSearchSpace, TProblem>
  : Interceptor<TGenotype, TSearchSpace, TProblem, TAlgorithmState>
  where TAlgorithmState : class, IAlgorithmState
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
{
  [OrderedEquality]
  public ImmutableArray<IInterceptor<TGenotype, TSearchSpace, TProblem, TAlgorithmState>> Interceptors { get; }

  public MultiInterceptor(ImmutableArray<IInterceptor<TGenotype, TSearchSpace, TProblem, TAlgorithmState>> interceptors)
  {
    Interceptors = interceptors;
  }

  public override Instance CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry)
  {
    var interceptorInstances = Interceptors.Select(instanceRegistry.Resolve).ToList();
    return new Instance(interceptorInstances);
  }

  public new class Instance
    : Interceptor<TGenotype, TSearchSpace, TProblem, TAlgorithmState>.Instance
  {
    private readonly IReadOnlyList<IInterceptorInstance<TGenotype, TSearchSpace, TProblem, TAlgorithmState>> interceptors;

    public Instance(IReadOnlyList<IInterceptorInstance<TGenotype, TSearchSpace, TProblem, TAlgorithmState>> interceptors)
    {
      this.interceptors = interceptors;
    }

    public override TAlgorithmState Transform(TAlgorithmState currentState, TAlgorithmState? previousState, TSearchSpace searchSpace, TProblem problem)
    {
      return interceptors.Aggregate(currentState, (current, interceptor) => interceptor.Transform(current, previousState, searchSpace, problem));
    }
  }
}
