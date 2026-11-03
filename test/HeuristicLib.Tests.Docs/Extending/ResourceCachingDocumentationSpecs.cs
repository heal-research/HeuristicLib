#region resource-cache-imports
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Inspection;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Operators.Evaluators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

#endregion resource-cache-imports

namespace HEAL.HeuristicLib.Tests.Docs.Extending;

#region resource-cache-authoring
public sealed record MemoizingEvaluator : WrappingOperator<Evaluation, int>
{
    public ResourceHandle<EvaluationCache<int>> Cache { get; init; }

    public MemoizingEvaluator(IOperator<Evaluation, int> child, ResourceHandle<EvaluationCache<int>> cache) : base(child)
    {
        Cache = cache;
    }

    protected override IExecutor CreateExecutor<TSearchSpace, TProblem, TSearchState>(
        ResolutionScope<int, TSearchSpace, TProblem, TSearchState> scope)
    {
        var child = scope.Resolve(ChildOperator);
        var cache = scope.Resource(Cache);
        return new Executor<TSearchSpace, TProblem>(child, cache);
    }

    private sealed class Executor<TSearchSpace, TProblem>(
        IEvaluationExecutor<int, TSearchSpace, TProblem> child, EvaluationCache<int> cache)
        : IEvaluationExecutor<int, TSearchSpace, TProblem>
        where TSearchSpace : class, ISearchSpace<int>
        where TProblem : class, IProblem<int, TSearchSpace>
    {
        public IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<int> candidates,
            IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem)
        {
            var results = new ObjectiveVector[candidates.Count];
            for (var i = 0; i < candidates.Count; i++)
            {
                var candidate = candidates[i];
                if (!cache.TryGet(candidate, out var result))
                {
                    // MemoryCache synchronizes storage. Expensive child calls run outside storage operations.
                    // Concurrent misses may evaluate the same candidate more than once.
                    result = child.Evaluate([candidate], random, searchSpace, problem)[0];
                    cache.Set(candidate, result);
                }
                results[i] = result;
            }
            return results;
        }
    }
}
public sealed class UnrestrictedIntegerSpace : ISearchSpace<int>
{
    public bool Contains(int candidate) => true;
}
#endregion resource-cache-authoring

public sealed class ResourceCachingDocumentationSpecs
{
    [Fact]
    public async Task ContextualExecutorsShareSafeStorageWithIndependentCalls()
    {
        #region resource-cache-usage
        var cache = new EvaluationCacheHandle<int> { SizeLimit = 100 };
        var evaluator = new ProblemEvaluator<int>();
        var memoized = new MemoizingEvaluator(evaluator, cache);
        var space = new UnrestrictedIntegerSpace();
        var problem = FuncProblem.Create((int candidate) => candidate * candidate, space, SingleObjective.Minimize);
        var evaluations = new CountAccumulator();
        var scope = ResolutionScope.Create(Aspect.CountCandidates(evaluator, evaluations))
            .For<int, UnrestrictedIntegerSpace, IProblem<int, UnrestrictedIntegerSpace>>();
        var first = scope.Resolve(memoized);
        var second = scope.With().Resolve(memoized);
        first.Evaluate([1, 2], RandomNumberGenerator.Create(1), space, problem);
        var results = await Task.WhenAll(
            Task.Run(() => first.Evaluate([1, 2, 3], RandomNumberGenerator.Create(2), space, problem)),
            Task.Run(() => second.Evaluate([1, 2, 3], RandomNumberGenerator.Create(3), space, problem)));
        #endregion resource-cache-usage

        results.ShouldAllBe(result => result.Select(vector => vector[0]).SequenceEqual(new[] { 1.0, 4.0, 9.0 }));
        evaluations.CurrentCount.ShouldBeInRange(3, 4);
        var before = evaluations.CurrentCount;
        second.Evaluate([3], RandomNumberGenerator.Create(4), space, problem);
        evaluations.CurrentCount.ShouldBe(before);
    }

}
