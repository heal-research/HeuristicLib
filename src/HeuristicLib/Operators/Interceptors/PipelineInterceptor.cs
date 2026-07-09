using Generator.Equals;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Operators.Interceptors;

[Equatable]
public partial record PipelineInterceptor<TCandidate, TSearchSpace, TProblem, TSearchState>
  : MultiInterceptor<TCandidate, TSearchSpace, TProblem, TSearchState>
  where TSearchState : class, ISearchState
  where TSearchSpace : class, ISearchSpace<TCandidate>
  where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    public PipelineInterceptor(ImmutableArray<IInterceptor<TCandidate, TSearchSpace, TProblem, TSearchState>> interceptors)
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

