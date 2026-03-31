using Generator.Equals;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Operators.Interceptors;

// ToDo: think about another name, maybe PipelineInceptor or SequentialInterceptor.
[Equatable]
public partial record MultiInterceptor<TGenotype, TAlgorithmState, TSearchSpace, TProblem>
  : CompositeInterceptor<TGenotype, TSearchSpace, TProblem, TAlgorithmState, NoState>
  where TAlgorithmState : class, IAlgorithmState
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
{
  public MultiInterceptor(ImmutableArray<IInterceptor<TGenotype, TSearchSpace, TProblem, TAlgorithmState>> interceptors)
    : base(interceptors)
  {
  }

  protected override NoState CreateInitialState() => NoState.Instance;

  protected override TAlgorithmState Transform(TAlgorithmState currentState, TAlgorithmState? previousState, NoState _,
    IReadOnlyList<IInterceptorInstance<TGenotype, TSearchSpace, TProblem, TAlgorithmState>> innerInterceptors,
    TSearchSpace searchSpace, TProblem problem)
  {
    return innerInterceptors.Aggregate(currentState, (current, interceptor) => interceptor.Transform(current, previousState, searchSpace, problem));
  }
}
