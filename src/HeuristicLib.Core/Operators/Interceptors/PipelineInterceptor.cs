using Generator.Equals;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Operators.Interceptors;

[Equatable]
public partial record PipelineInterceptor<TGenotype, TSearchSpace, TProblem, TSearchState>
  : MultiInterceptor<TGenotype, TSearchSpace, TProblem, TSearchState>
  where TSearchState : class, ISearchState
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
{
  public PipelineInterceptor(ImmutableArray<IInterceptor<TGenotype, TSearchSpace, TProblem, TSearchState>> interceptors)
    : base(interceptors)
  {
  }

  protected override TSearchState Transform(
    TSearchState currentState,
    TSearchState? previousState,
    IReadOnlyList<InnerTransform> innerInterceptors,
    TSearchSpace searchSpace,
    TProblem problem)
  {
    return innerInterceptors.Aggregate(currentState, (current, interceptor) => interceptor(current, previousState, searchSpace, problem));
  }
}

