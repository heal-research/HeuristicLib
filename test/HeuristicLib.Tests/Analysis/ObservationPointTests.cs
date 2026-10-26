using System.Runtime.CompilerServices;
using HEAL.HeuristicLib.Operators.Mutators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.Tests.TestSupport.Mocks;

namespace HEAL.HeuristicLib.Tests.Analysis;

public class ObservationPointTests
{
    [Fact]
    public void Points_KeepIdentityPerConfigurationAndName()
    {
        var ticker = new Ticker();
        var copy = ticker with { };

        ticker.First.ShouldBeSameAs(ticker.First);
        ticker.First.ShouldNotBeSameAs(ticker.Second);
        ticker.First.ShouldNotBeSameAs(copy.First);
        ticker.ShouldBe(copy);
        ticker.First.Name.ShouldBe(nameof(Ticker.First));
        ObservationPoint.Of<int>(ticker, "first").ShouldNotBeSameAs(ticker.First);
    }

    [Fact]
    public void Points_KeepIdentityDuringConcurrentAccess()
    {
        var ticker = new Ticker();
        var points = new ObservationPoint<int>[32];

        Parallel.For(0, points.Length, index => points[index] = ticker.First);

        points.ShouldAllBe(point => ReferenceEquals(point, ticker.First));
    }

    [Fact]
    public void UnobservedPoints_LeaveTheComputationUnchanged()
    {
        var executor = ResolutionScope.Create().Resolve(new Ticker());

        executor.Tick().ShouldBe(1);
        executor.Tick().ShouldBe(2);
    }

    [Fact]
    public void Reports_CallEachAdviceOnceInAspectOrder()
    {
        var ticker = new Ticker();
        var events = new List<string>();
        var first = Aspect.Observe(ticker.First, value => events.Add($"first {value}"));
        var second = Aspect.Observe(ticker.First, value => events.Add($"second {value}"));

        ResolutionScope.Create(first, second, first).Resolve(ticker).Tick();

        events.ShouldBe(["first 1", "second 1"]);
    }

    [Fact]
    public void Trace_DistinguishesSameTypePointsAndDeduplicatesOnePoint()
    {
        var ticker = new Ticker();
        var trace = Analyzer.Trace([ticker.First, ticker.Second, ticker.First], value => value);
        var other = new Ticker();
        var copy = ticker with { };
        var scope = ResolutionScope.Create(trace);

        scope.Resolve(other).Tick();
        scope.Resolve(copy).Tick();
        scope.Resolve(ticker).Tick();

        trace.Snapshot().Values.ShouldBe([1, -1]);
    }

    [Fact]
    public void SharedClock_CountsOnceBeforeEachTrace()
    {
        var ticker = new Ticker();
        var clock = new ObservationClock<int>(ticker.First);
        var first = Analyzer.Trace(ticker.First, value => value, clocks: [clock]);
        var second = Analyzer.Trace(ticker.First, value => value, clocks: [clock]);
        var executor = ResolutionScope.Create(first, second, clock).Resolve(ticker);

        executor.Tick();
        executor.Tick();

        first.Snapshot().Select(entry => entry.At(clock)).ShouldBe([1L, 2L]);
        second.Snapshot().Select(entry => entry.At(clock)).ShouldBe([1L, 2L]);
    }

    [Fact]
    public void Rebinding_AppliesTheRequestingAspectsToTheSamePoints()
    {
        var ticker = new Ticker();
        var outer = Analyzer.Trace(ticker.First, value => value);
        var inner = Analyzer.Trace(ticker.First, value => value);
        var scope = ResolutionScope.Create(outer);
        var original = scope.Resolve(ticker);
        var rebound = scope.With(inner).Resolve(ticker);

        original.Tick();
        rebound.Tick();
        original.Tick();

        outer.Snapshot().Values.ShouldBe([1, 1, 2]);
        inner.Snapshot().Values.ShouldBe([1]);
    }

    [Fact]
    public void Operator_ResolvesItsOwnPointWithoutAnOperatorWrapper()
    {
        var mutator = new ReportingMutator();
        var trace = Analyzer.Trace(mutator.Counts, count => count);
        var problem = FuncProblem.Create(static (int candidate) => candidate, DummySearchSpace<int>.Instance, SingleObjective.Minimize);
        var observed = ResolutionScope.Create(trace).Resolve<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>(mutator);
        var unobserved = ResolutionScope.Create().Resolve<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>(mutator);

        observed.Mutate([1, 2], RandomNumberGenerator.Create(1), problem.SearchSpace, problem).ShouldBe([1, 2]);

        observed.GetType().ShouldBe(unobserved.GetType());
        trace.Snapshot().Values.ShouldBe([2]);
    }

    [Fact]
    public void PointCache_DoesNotRetainConfigurationsOrTheirPoints()
    {
        var (configuration, point) = CreateWeakReferences();

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        configuration.IsAlive.ShouldBeFalse();
        point.IsAlive.ShouldBeFalse();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static (WeakReference Configuration, WeakReference Point) CreateWeakReferences()
    {
        var ticker = new Ticker();
        return (new(ticker), new(ticker.First));
    }

    private interface ITickExecutor : IExecutor
    {
        int Tick();
    }

    private sealed record Ticker : IConfigurationNode<ITickExecutor>
    {
        public ObservationPoint<int> First => ObservationPoint.Of<int>(this, nameof(First));
        public ObservationPoint<int> Second => ObservationPoint.Of<int>(this, nameof(Second));

        public ITickExecutor CreateExecutor(ResolutionScope scope)
        {
            var first = scope.Resolve(First);
            var second = scope.Resolve(Second);
            return new Executor(first, second);
        }

        private sealed class Executor(IObservationPointExecutor<int> first, IObservationPointExecutor<int> second) : ITickExecutor
        {
            private int count;

            public int Tick()
            {
                first.Report(++count);
                second.Report(-count);
                return count;
            }
        }
    }

    private sealed record ReportingMutator : Mutator<int>
    {
        public ObservationPoint<int> Counts => ObservationPoint.Of<int>(this, nameof(Counts));

        protected override IMutationExecutor<int, ISearchSpace<int>, IProblem<int, ISearchSpace<int>>> CreateExecutor(ResolutionScope scope)
        {
            var counts = scope.Resolve(Counts);
            return new Executor(counts);
        }

        private sealed class Executor(IObservationPointExecutor<int> counts) : IMutationExecutor<int, ISearchSpace<int>, IProblem<int, ISearchSpace<int>>>
        {
            public IReadOnlyList<int> Mutate(IReadOnlyList<int> parents, IRandomNumberGenerator random, ISearchSpace<int> searchSpace,
                IProblem<int, ISearchSpace<int>> problem)
            {
                counts.Report(parents.Count);
                return parents;
            }
        }
    }
}
