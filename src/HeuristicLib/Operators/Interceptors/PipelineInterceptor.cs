using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Operators.Interceptors;

public record PipelineInterceptor<TCandidate, TSearchSpace, TProblem, TSearchState>
  : MultiInterceptor<TCandidate, TSearchSpace, TProblem, TSearchState>
  where TSearchState : class, ISearchState
  where TSearchSpace : class, ISearchSpace<TCandidate>
  where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    public PipelineInterceptor(IReadOnlyList<IInterceptor<TCandidate, TSearchSpace, TProblem, TSearchState>> interceptors)
      : base(interceptors)
    {
    }

    protected override MultiInterceptorInstance<TCandidate, TSearchSpace, TProblem, TSearchState> CreateInterceptorInstance(ImmutableArray<IInterceptorInstance<TCandidate, TSearchSpace, TProblem, TSearchState>> innerInterceptors) =>
        new Instance(innerInterceptors);

    private sealed class Instance(ImmutableArray<IInterceptorInstance<TCandidate, TSearchSpace, TProblem, TSearchState>> innerInterceptors)
        : MultiInterceptorInstance<TCandidate, TSearchSpace, TProblem, TSearchState>(innerInterceptors)
    {
        public override TSearchState Transform(TSearchState currentState, TSearchState? previousState, TSearchSpace searchSpace, TProblem problem) =>
            InnerInterceptors.Aggregate(currentState, (current, interceptor) => interceptor.Transform(current, previousState, searchSpace, problem));
    }
}

public static class PipelineInterceptor
{
    public static PipelineInterceptor<TCandidate, TSearchSpace, TProblem, TSearchState> Create<TCandidate, TSearchSpace, TProblem, TSearchState>(params IReadOnlyList<IInterceptor<TCandidate, TSearchSpace, TProblem, TSearchState>> interceptors)
        where TSearchState : class, ISearchState
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace> => new([.. interceptors]);
}

public static class PipelineInterceptorExtensions
{
    extension<TCandidate, TSearchSpace, TProblem, TSearchState>(IInterceptor<TCandidate, TSearchSpace, TProblem, TSearchState> interceptor)
        where TSearchState : class, ISearchState
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        public PipelineInterceptor<TCandidate, TSearchSpace, TProblem, TSearchState> Then(params IReadOnlyList<IInterceptor<TCandidate, TSearchSpace, TProblem, TSearchState>> followingInterceptors) =>
            PipelineInterceptor.Create([interceptor, .. followingInterceptors]);
    }
}
