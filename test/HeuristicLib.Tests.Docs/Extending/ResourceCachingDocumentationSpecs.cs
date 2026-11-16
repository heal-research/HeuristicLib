#region resource-cache-imports
using HEAL.HeuristicLib.Analysis;
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
    public long? SizeLimit { get; init; }

    public MemoizingEvaluator(IOperator<Evaluation, int> child) : base(child)
    {
    }

    protected override IExecutor CreateExecutor<TSearchSpace, TProblem, TSearchState>(
        ResolutionScope<int, TSearchSpace, TProblem, TSearchState> scope)
    {
        var child = scope.Resolve(ChildOperator);
        var cache = new EvaluationCache<int>(SizeLimit);
        return new Executor(child, cache);
    }

    private sealed class Executor(
        IEvaluationExecutor<int> child, EvaluationCache<int> cache)
        : IEvaluationExecutor<int>
    {
        public IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<int> candidates, IRandomNumberGenerator random)
        {
            var results = new ObjectiveVector[candidates.Count];
            for (var i = 0; i < candidates.Count; i++)
            {
                var candidate = candidates[i];
                if (!cache.TryGet(candidate, out var result))
                {
                    // MemoryCache synchronizes storage. Expensive child calls run outside storage operations.
                    // Concurrent misses may evaluate the same candidate more than once.
                    result = child.Evaluate([candidate], random)[0];
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
    public async Task SharedExecutorServesConcurrentCallsFromSafeStorage()
    {
        #region resource-cache-usage
        var evaluator = new ProblemEvaluator<int>();
        var memoized = new MemoizingEvaluator(evaluator) { SizeLimit = 100 };
        var space = new UnrestrictedIntegerSpace();
        var problem = FuncProblem.Create((int candidate) => candidate * candidate, space, SingleObjective.Minimize);
        var evaluations = evaluator.Invocations.CountCandidates();
        var analyzed = memoized.AnalyzedBy(evaluations);
        var root = ResolutionScope.Create();
        var scope = root
            .For<int, UnrestrictedIntegerSpace, IProblem<int, UnrestrictedIntegerSpace>>(problem);
        var first = scope.Resolve(analyzed);
        var second = scope.Resolve(analyzed);
        first.Evaluate([1, 2], RandomNumberGenerator.Create(1));
        var results = await Task.WhenAll(
            Task.Run(() => first.Evaluate([1, 2, 3], RandomNumberGenerator.Create(2))),
            Task.Run(() => second.Evaluate([1, 2, 3], RandomNumberGenerator.Create(3))));
        #endregion resource-cache-usage

        results.ShouldAllBe(result => result.Select(vector => vector[0]).SequenceEqual(new[] { 1.0, 4.0, 9.0 }));
        root.Results.Result(evaluations).CurrentCount.ShouldBeInRange(3, 4);
        var before = root.Results.Result(evaluations).CurrentCount;
        second.Evaluate([3], RandomNumberGenerator.Create(4));
        root.Results.Result(evaluations).CurrentCount.ShouldBe(before);
    }

}
