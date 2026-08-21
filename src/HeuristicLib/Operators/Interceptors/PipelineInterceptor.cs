using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Operators.Interceptors;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators;

public sealed record PipelineInterceptor<TCandidate, TSearchSpace, TProblem, TSearchState>
    : MultiInterceptor<TCandidate, TSearchSpace, TProblem, TSearchState>
    where TSearchState : class, ISearchState
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    public PipelineInterceptor(IReadOnlyList<IInterceptor<TCandidate, TSearchSpace, TProblem, TSearchState>> childInterceptors)
        : base(childInterceptors)
    {
    }

    protected override MultiInterceptorInstance<TCandidate, TSearchSpace, TProblem, TSearchState> CreateExecutionInstance(ImmutableArray<IInterceptorInstance<TCandidate, TSearchSpace, TProblem, TSearchState>> childInterceptors) =>
        new Instance(childInterceptors);

    private sealed class Instance(ImmutableArray<IInterceptorInstance<TCandidate, TSearchSpace, TProblem, TSearchState>> childInterceptors)
        : MultiInterceptorInstance<TCandidate, TSearchSpace, TProblem, TSearchState>(childInterceptors)
    {
        public override TSearchState Transform(TSearchState currentState, TSearchState? previousState, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem)
        {
            var transformedState = currentState;
            foreach (var interceptor in ChildInterceptors)
            {
                transformedState = interceptor.Transform(transformedState, previousState, random, searchSpace, problem);
            }

            return transformedState;
        }
    }
}

public static class PipelineInterceptor
{
    public static PipelineInterceptor<TCandidate, TSearchSpace, TProblem, TSearchState> Create<TCandidate, TSearchSpace, TProblem, TSearchState>(params IReadOnlyList<IInterceptor<TCandidate, TSearchSpace, TProblem, TSearchState>> childInterceptors)
        where TSearchState : class, ISearchState
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace> => new(childInterceptors);
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
