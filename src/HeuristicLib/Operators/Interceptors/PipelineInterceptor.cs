using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Operators.Interceptors;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators;

public sealed record PipelineInterceptor<TCandidate>
    : MultiInterceptor<TCandidate>
{
    public PipelineInterceptor(IReadOnlyList<IInterceptor<TCandidate>> childInterceptors)
        : base(childInterceptors)
    {
    }

    protected override CompositeExecutionFactory<IInterceptorExecution<TCandidate, TRunSearchSpace, TRunProblem, TRunSearchState>> CreateCompositeFactory<TRunSearchSpace, TRunProblem, TRunSearchState>() =>
        childInterceptors => new Execution<TRunSearchSpace, TRunProblem, TRunSearchState>(childInterceptors);

    private sealed class Execution<TSearchSpace, TProblem, TSearchState>(ImmutableArray<IInterceptorExecution<TCandidate, TSearchSpace, TProblem, TSearchState>> childInterceptors)
        : MultiInterceptorExecution<TCandidate, TSearchSpace, TProblem, TSearchState>(childInterceptors)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TSearchState : class, ISearchState
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
    public static PipelineInterceptor<TCandidate> Create<TCandidate>(params IReadOnlyList<IInterceptor<TCandidate>> childInterceptors) =>
        new(childInterceptors);
}

public static class PipelineInterceptorExtensions
{
    extension<TCandidate>(IInterceptor<TCandidate> interceptor)
    {
        public PipelineInterceptor<TCandidate> Then(params IReadOnlyList<IInterceptor<TCandidate>> followingInterceptors) =>
            PipelineInterceptor.Create([interceptor, .. followingInterceptors]);
    }
}
