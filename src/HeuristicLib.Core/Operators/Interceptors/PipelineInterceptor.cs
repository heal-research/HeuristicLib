using Generator.Equals;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Operators.Interceptors;

[Equatable]
public partial record PipelineInterceptor<TGenotype, TSearchSpace, TProblem, TAlgorithmState>
  : CompositeInterceptor<TGenotype, TSearchSpace, TProblem, TAlgorithmState>
  where TAlgorithmState : class, IAlgorithmState
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
{
  public PipelineInterceptor(ImmutableArray<IInterceptor<TGenotype, TSearchSpace, TProblem, TAlgorithmState>> interceptors)
	  : base(interceptors)
  {
  }

  protected override TAlgorithmState Transform(
	  TAlgorithmState currentState,
	  TAlgorithmState? previousState,
	  IReadOnlyList<IInterceptorInstance<TGenotype, TSearchSpace, TProblem, TAlgorithmState>> innerInterceptors,
	  TSearchSpace searchSpace,
	  TProblem problem)
  {
	  return innerInterceptors.Aggregate(currentState, (current, interceptor) => interceptor.Transform(current, previousState, searchSpace, problem));
  }
}

