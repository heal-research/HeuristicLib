using System.Runtime.CompilerServices;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Operators.Evaluators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.Tests.TestSupport.Mocks;

namespace HEAL.HeuristicLib.Tests.Operators.Evaluators;

public sealed class EvaluationResolutionTests
{
    private static readonly FuncProblem<int, DummySearchSpace<int>> Problem = FuncProblem.Create(static (int x) => x, DummySearchSpace<int>.Instance, SingleObjective.Minimize);
    [Fact]
    public void Cache_NegativeLimitsDistinguishArgumentsFromConfigurationAndFailBeforeChildConstruction()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => new EvaluationCache<int>(-1)).ParamName.ShouldBe("sizeLimit");
        var preparations = 0;
        var leaf = new CountingEvaluator(() => preparations++, () => { });
        Should.Throw<InvalidOperationException>(() => Resolve(ResolutionScope.Create(), leaf.Cached(-1)));
        preparations.ShouldBe(0);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void Cache_EntryLimitBoundsStorageAndClearInvalidatesAllEntries(int capacity)
    {
        var cache = new EvaluationCache<int>(capacity);
        for (var key = 0; key < 10; key++)
            cache.Set(key, new ObjectiveVector(key));
        Enumerable.Range(0, 10).Count(key => cache.TryGet(key, out _)).ShouldBeLessThanOrEqualTo(capacity);
        cache.Clear();
        Enumerable.Range(0, 10).Select(key => cache.TryGet(key, out _)).ShouldAllBe(found => !found);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(2L)]
    public void Cache_DiscardedExecutionReleasesCachedCandidatesAndResults(long? sizeLimit)
    {
        var source = new ProblemEvaluator<object>().Cached(sizeLimit);
        var problem = FuncProblem.Create(static (object _) => 1.0, DummySearchSpace<object>.Instance, SingleObjective.Minimize);
        var (candidate, result) = CreateCachedReferences(source, problem);

        // Collection of discarded cache entries is the behavior under test.
        for (var attempt = 0; attempt < 4 && (candidate.IsAlive || result.IsAlive); attempt++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }

        candidate.IsAlive.ShouldBeFalse();
        result.IsAlive.ShouldBeFalse();
        GC.KeepAlive(source);
        GC.KeepAlive(problem);

        [MethodImpl(MethodImplOptions.NoInlining)]
        static (WeakReference Candidate, WeakReference Result) CreateCachedReferences(IOperator<Evaluation, object> source, FuncProblem<object, DummySearchSpace<object>> problem)
        {
            var scope = ResolutionScope.Create();
            var executor = scope.Resolve<object, DummySearchSpace<object>, FuncProblem<object, DummySearchSpace<object>>>(source, problem);
            var candidate = new object();
            var result = executor.Evaluate([candidate], RandomNumberGenerator.Create(1)).Single();
            executor.Evaluate([candidate], RandomNumberGenerator.Create(2)).Single().ShouldBeSameAs(result);
            return (new WeakReference(candidate), new WeakReference(result));
        }
    }

    [Fact]
    public void Cache_ZeroSizeLimitReturnsResultsWithoutCachingThem()
    {
        var calls = 0;
        var source = new CachingEvaluator<int>(new CountingEvaluator(() => { }, () => calls++)) { SizeLimit = 0 };
        var executor = Resolve(ResolutionScope.Create(), source);

        Evaluate(executor, 1).ShouldBe([new ObjectiveVector(101)]);
        Evaluate(executor, 1).ShouldBe([new ObjectiveVector(201)]);
        calls.ShouldBe(2);
    }

    [Fact]
    public void Cache_SharedEvaluatorPreservesResultsAndObservesOnlyUncachedChildCalls()
    {
        var preparations = 0;
        var calls = 0;
        var reports = new List<int>();
        var leaf = new CountingEvaluator(() => preparations++, () => calls++);
        var source = new CachingEvaluator<int>(leaf);
        var reporting = source.Substitute(leaf, evaluator => new ReportingEvaluator(evaluator, reports.AddRange));
        var scope = ResolutionScope.Create();
        var first = Resolve(scope, reporting);
        var second = Resolve(scope, reporting);

        Evaluate(first, 1).ShouldBe([new ObjectiveVector(101)]);
        Evaluate(second, 1).ShouldBe([new ObjectiveVector(101)]);
        Evaluate(second, 2).ShouldBe([new ObjectiveVector(202)]);
        Evaluate(first, 2).ShouldBe([new ObjectiveVector(202)]);
        reports.ShouldBe([1, 2]);
        preparations.ShouldBe(1);
        calls.ShouldBe(2);

        Evaluate(Resolve(ResolutionScope.Create(), source), 1).ShouldBe([new ObjectiveVector(101)]);
        preparations.ShouldBe(2);
        calls.ShouldBe(3);
    }

    [Fact]
    public void Limit_SharedEvaluatorSharesTheRemainingBudget()
    {
        var preparations = 0;
        var calls = 0;
        var leaf = new CountingEvaluator(() => preparations++, () => calls++);
        var source = new LimitEvaluator<int>(leaf, 2) { FallbackObjectiveVector = new ObjectiveVector(-1), EnforceLimitWithinBatch = true };
        var scope = ResolutionScope.Create();
        var first = Resolve(scope, source);
        var second = Resolve(scope, source);

        Evaluate(first, 1).ShouldBe([new ObjectiveVector(101)]);
        Evaluate(second, 2, 3).ShouldBe([new ObjectiveVector(202), new ObjectiveVector(-1)]);
        Evaluate(first, 4).ShouldBe([new ObjectiveVector(-1)]);
        preparations.ShouldBe(1);
        calls.ShouldBe(2);

        Evaluate(Resolve(ResolutionScope.Create(), source), 4).ShouldBe([new ObjectiveVector(104)]);
        preparations.ShouldBe(2);
        calls.ShouldBe(3);
    }

    private static IEvaluationExecutor<int> Resolve(ResolutionScope scope, IOperator<Evaluation, int> source) =>
        scope.Resolve<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>(source, Problem);

    private static IReadOnlyList<ObjectiveVector> Evaluate(IEvaluationExecutor<int> executor, params int[] candidates)
    {
        return executor.Evaluate(candidates, RandomNumberGenerator.Create(1));
    }

    private sealed record CountingEvaluator(Action Prepare, Action EvaluateCall) : Evaluator<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>
    {

        protected override IEvaluationExecutor<int> CreateExecutor<TRunSearchState>(ResolutionScope<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>, TRunSearchState> scope)
        {
            Prepare();
            return new Executor(this, scope.Problem);
        }

        private sealed class Executor(CountingEvaluator evaluator, FuncProblem<int, DummySearchSpace<int>> problem)
            : EvaluationExecutor<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>(problem)
        {
            private int invocations;

            public override IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<int> candidates, IRandomNumberGenerator random)
            {
                evaluator.EvaluateCall();
                var call = ++invocations;
                return candidates.Select(candidate => new ObjectiveVector(candidate + 100 * call)).ToArray();
            }
        }
    }

    // Reports the candidates of each successful call of the evaluator it wraps.
    private sealed record ReportingEvaluator : WrappingOperator<Evaluation, int>
    {
        public ReportingEvaluator(IOperator<Evaluation, int> childEvaluator, Action<IReadOnlyList<int>> report)
            : base(childEvaluator)
        {
            Report = report;
        }

        public Action<IReadOnlyList<int>> Report { get; init; }

        protected override IExecutor CreateExecutor<TRunSearchSpace, TRunProblem, TRunSearchState>(
            ResolutionScope<int, TRunSearchSpace, TRunProblem, TRunSearchState> scope) =>
            new Executor(scope.Resolve(ChildOperator), Report);

        private sealed class Executor(IEvaluationExecutor<int> child, Action<IReadOnlyList<int>> report)
            : IEvaluationExecutor<int>
        {
            public IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<int> candidates, IRandomNumberGenerator random)
            {
                var result = child.Evaluate(candidates, random);
                report(candidates);
                return result;
            }
        }
    }
}
