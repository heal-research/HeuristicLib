using System.Runtime.CompilerServices;
using HEAL.HeuristicLib.Operators.Evaluators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.Tests.TestSupport.Mocks;

namespace HEAL.HeuristicLib.Tests.Operators.Evaluators;

public sealed class EvaluatorExecutionFactoryTests
{
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
        static (WeakReference Candidate, WeakReference Result) CreateCachedReferences(IEvaluator<object> source, FuncProblem<object, DummySearchSpace<object>> problem)
        {
            var scope = ResolutionScope.Create();
            var execution = scope.Resolve<object, DummySearchSpace<object>, FuncProblem<object, DummySearchSpace<object>>>(source);
            var candidate = new object();
            var result = execution.Evaluate([candidate], RandomNumberGenerator.Create(1), problem.SearchSpace, problem).Single();
            execution.Evaluate([candidate], RandomNumberGenerator.Create(2), problem.SearchSpace, problem).Single().ShouldBeSameAs(result);
            return (new WeakReference(candidate), new WeakReference(result));
        }
    }

    [Fact]
    public void Cache_ZeroSizeLimitReturnsResultsWithoutCachingThem()
    {
        var calls = 0;
        var source = new CachingEvaluator<int>(new CountingEvaluator(() => { }, () => calls++)) { SizeLimit = 0 };
        var execution = Resolve(ResolutionScope.Create(), source);

        Evaluate(execution, 1).ShouldBe([new ObjectiveVector(101)]);
        Evaluate(execution, 1).ShouldBe([new ObjectiveVector(201)]);
        calls.ShouldBe(2);
    }

    [Fact]
    public void Cache_RebindingPreservesResultsAndObservesOnlyUncachedChildCalls()
    {
        var preparations = 0;
        var calls = 0;
        var reports = new List<int>();
        var leaf = new CountingEvaluator(() => preparations++, () => calls++);
        var source = new CachingEvaluator<int>(leaf);
        var parent = ResolutionScope.Create();
        var outer = Resolve(parent, source);
        Evaluate(outer, 1).ShouldBe([new ObjectiveVector(101)]);

        var child = parent.CreateChildScope(builder => builder.Wrap<IEvaluator<int>>(leaf, original => new ObservingEvaluator(original, reports.AddRange)));
        var inner = Resolve(child, source);
        inner.ShouldNotBeSameAs(outer);
        Evaluate(inner, 1).ShouldBe([new ObjectiveVector(101)]);
        reports.ShouldBeEmpty();
        Evaluate(inner, 2).ShouldBe([new ObjectiveVector(202)]);
        Evaluate(outer, 2).ShouldBe([new ObjectiveVector(202)]);
        Evaluate(outer, 3).ShouldBe([new ObjectiveVector(303)]);
        Evaluate(inner, 3).ShouldBe([new ObjectiveVector(303)]);
        reports.ShouldBe([2]);
        preparations.ShouldBe(1);
        calls.ShouldBe(3);

        Evaluate(Resolve(ResolutionScope.Create(), source), 1).ShouldBe([new ObjectiveVector(101)]);
        preparations.ShouldBe(2);
        calls.ShouldBe(4);
    }

    [Fact]
    public void Limit_RebindingSharesTheRemainingBudgetAndBindsTheObservedChild()
    {
        var preparations = 0;
        var calls = 0;
        var reports = new List<int>();
        var leaf = new CountingEvaluator(() => preparations++, () => calls++);
        var source = new LimitEvaluator<int>(leaf, 2) { FallbackObjectiveVector = new ObjectiveVector(-1), EnforceLimitWithinBatch = true };
        var parent = ResolutionScope.Create();
        var outer = Resolve(parent, source);
        Evaluate(outer, 1).ShouldBe([new ObjectiveVector(101)]);

        var child = parent.CreateChildScope(builder => builder.Wrap<IEvaluator<int>>(leaf, original => new ObservingEvaluator(original, reports.AddRange)));
        var inner = Resolve(child, source);
        inner.ShouldNotBeSameAs(outer);
        Evaluate(inner, 2, 3).ShouldBe([new ObjectiveVector(202), new ObjectiveVector(-1)]);
        Evaluate(outer, 4).ShouldBe([new ObjectiveVector(-1)]);
        reports.ShouldBe([2]);
        preparations.ShouldBe(1);
        calls.ShouldBe(2);

        Evaluate(Resolve(ResolutionScope.Create(), source), 4).ShouldBe([new ObjectiveVector(104)]);
        preparations.ShouldBe(2);
        calls.ShouldBe(3);
    }

    private static IEvaluatorExecution<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>> Resolve(ResolutionScope scope, IEvaluator<int> source) =>
        scope.Resolve<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>(source);

    private static IReadOnlyList<ObjectiveVector> Evaluate(IEvaluatorExecution<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>> execution, params int[] candidates)
    {
        var problem = FuncProblem.Create(static (int candidate) => candidate, DummySearchSpace<int>.Instance, SingleObjective.Minimize);
        return execution.Evaluate(candidates, RandomNumberGenerator.Create(1), problem.SearchSpace, problem);
    }

    private sealed class CounterState
    {
        public int Calls { get; set; }
    }

    private sealed record CountingEvaluator(Action Prepare, Action EvaluateCall) : StatefulEvaluator<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>, CounterState>
    {
        protected override CounterState CreateInitialState()
        {
            Prepare();
            return new CounterState();
        }

        protected override IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<int> candidates, CounterState state, IRandomNumberGenerator random, DummySearchSpace<int> searchSpace, FuncProblem<int, DummySearchSpace<int>> problem)
        {
            EvaluateCall();
            var call = ++state.Calls;
            return candidates.Select(candidate => new ObjectiveVector(candidate + 100 * call)).ToArray();
        }
    }

    private sealed record ObservingEvaluator : WrappingEvaluator<int>
    {
        private readonly Action<IReadOnlyList<int>> report;

        public ObservingEvaluator(IEvaluator<int> childEvaluator, Action<IReadOnlyList<int>> report) : base(childEvaluator)
        {
            this.report = report;
        }

        protected override WrapperExecutionFactory<IEvaluatorExecution<int, TRunSearchSpace, TRunProblem>> CreateWrapperFactory<TRunSearchSpace, TRunProblem>() =>
            child => new Execution<TRunSearchSpace, TRunProblem>(child, report);

        private sealed class Execution<TSearchSpace, TProblem>(IEvaluatorExecution<int, TSearchSpace, TProblem> child, Action<IReadOnlyList<int>> report)
            : WrappingEvaluatorExecution<int, TSearchSpace, TProblem>(child)
            where TSearchSpace : class, ISearchSpace<int>
            where TProblem : class, IProblem<int, TSearchSpace>
        {
            public override IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<int> candidates, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem)
            {
                var result = ChildEvaluator.Evaluate(candidates, random, searchSpace, problem);
                report(candidates);
                return result;
            }
        }
    }
}
