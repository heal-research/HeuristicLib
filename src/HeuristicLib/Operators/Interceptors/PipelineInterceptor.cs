using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators;

public sealed record PipelineInterceptor<TCandidate, TSearchState>
    : MultiOperator<Interception<TSearchState>, TCandidate>
    where TSearchState : class, ISearchState
{
    public PipelineInterceptor(IReadOnlyList<IOperator<Interception<TSearchState>, TCandidate>> childInterceptors)
        : base(childInterceptors)
    {
    }

    protected override IExecutor CreateExecutor<TRunSearchSpace, TRunProblem, TRunSearchState>(
        ResolutionScope<TCandidate, TRunSearchSpace, TRunProblem, TRunSearchState> scope)
    {
        ImmutableArray<IInterceptionExecutor<TCandidate, TRunSearchSpace, TRunProblem, TSearchState>> childInterceptors = [.. ChildOperators.Select(child => scope.Resolve(child))];
        return new Executor<TRunSearchSpace, TRunProblem>(childInterceptors);
    }

    private sealed class Executor<TSearchSpace, TProblem>(ImmutableArray<IInterceptionExecutor<TCandidate, TSearchSpace, TProblem, TSearchState>> childInterceptors)
        : IInterceptionExecutor<TCandidate, TSearchSpace, TProblem, TSearchState>
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        public TSearchState Transform(TSearchState currentState, TSearchState? previousState, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem)
        {
            var transformedState = currentState;
            foreach (var interceptor in childInterceptors)
            {
                transformedState = interceptor.Transform(transformedState, previousState, random, searchSpace, problem);
            }

            return transformedState;
        }
    }
}

public static class PipelineInterceptor
{
    public static PipelineInterceptor<TCandidate, TSearchState> Create<TCandidate, TSearchState>(params IReadOnlyList<IOperator<Interception<TSearchState>, TCandidate>> childInterceptors)
        where TSearchState : class, ISearchState =>
        new(childInterceptors);
}

public static class PipelineInterceptorExtensions
{
    extension<TCandidate, TSearchState>(IOperator<Interception<TSearchState>, TCandidate> interceptor)
        where TSearchState : class, ISearchState
    {
        public PipelineInterceptor<TCandidate, TSearchState> Then(params IReadOnlyList<IOperator<Interception<TSearchState>, TCandidate>> followingInterceptors) =>
            PipelineInterceptor.Create([interceptor, .. followingInterceptors]);
    }
}
