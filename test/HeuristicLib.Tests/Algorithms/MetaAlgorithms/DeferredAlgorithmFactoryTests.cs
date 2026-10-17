using System.Runtime.CompilerServices;
using HEAL.HeuristicLib.Inspection;
using HEAL.HeuristicLib.Operators.Mutators;
using HEAL.HeuristicLib.Operators.Terminators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.Tests.TestSupport.Mocks;

namespace HEAL.HeuristicLib.Tests.Algorithms.MetaAlgorithms;

public sealed class DeferredAlgorithmFactoryTests
{
    private static readonly IProblem<int, DummySearchSpace<int>> problem = MetaAlgorithmTestHelpers.CreateIntegerProblem();

    [Theory]
    [InlineData(ChildScopePolicy.Fresh)]
    [InlineData(ChildScopePolicy.Retained)]
    [InlineData(ChildScopePolicy.Shared)]
    public async Task Cycle_RepeatedChildrenRespectScopePolicyAcrossInvocations(ChildScopePolicy mode)
    {
        var reset = mode == ChildScopePolicy.Fresh;
        var prepared = new List<ProbeData>();
        var algorithm = new ProbeAlgorithm { Prepared = prepared.Add };
        var executor = Resolve(ResolutionScope.Create(), algorithm.CycleWith(algorithm, maximumCycles: 2, scopePolicy: mode));
        var forks = new List<string>();

        var first = await Run(executor, new RecordingRandom("first", forks));
        var second = await Run(executor, new RecordingRandom("second", forks));

        first.Select(state => state.Value).ShouldBe([1, 2, 3, 4]);
        second.Select(state => state.Value).ShouldBe([1, 2, 3, 4]);
        first.Select(state => state.Call).ShouldBe(reset ? [1, 1, 1, 1] : [1, 2, 3, 4]);
        second.Select(state => state.Call).ShouldBe(reset ? [1, 1, 1, 1] : [5, 6, 7, 8]);
        prepared.Count.ShouldBe(reset ? 8 : 1);
        prepared.Sum(data => data.Starts).ShouldBe(8);
        prepared.Sum(data => data.Disposals).ShouldBe(8);
        forks.ShouldBe(["first/0", "first/0/0", "first/0/1", "first/1", "first/1/0", "first/1/1",
            "second/0", "second/0/0", "second/0/1", "second/1", "second/1/0", "second/1/1"]);
    }

    [Fact]
    public async Task Cycle_ReuseSharesReferencesAcrossOwnersAndDirectResolution()
    {
        var algorithm = new ProbeAlgorithm();
        var equal = algorithm with { };
        equal.ShouldBe(algorithm);
        var cycle = new CycleAlgorithm<IAlgorithm<int, ProbeState>, int, ProbeState>([algorithm, equal])
        {
            MaximumCycles = 2,
            ScopePolicy = ChildScopePolicy.Shared
        };
        var root = ResolutionScope.Create();
        var executor = Resolve(root, cycle);
        var first = await Run(executor);
        first.Select(state => state.Call).ShouldBe([1, 1, 2, 2]);
        first[0].Data.ShouldBeSameAs(first[2].Data);
        first[0].Data.ShouldNotBeSameAs(first[1].Data);

        var otherOwner = await Run(Resolve(root, cycle with { }));
        otherOwner[0].Data.ShouldBeSameAs(first[0].Data);
        otherOwner.Select(state => state.Call).ShouldBe([3, 3, 4, 4]);
        var ancestor = (await Run(Resolve(root, algorithm))).Single();
        ancestor.Data.ShouldBeSameAs(first[0].Data);
        ancestor.Call.ShouldBe(5);
        var next = await Run(executor);
        next.Select(state => state.Call).ShouldBe([6, 5, 7, 6]);
        next[0].Data.ShouldBeSameAs(first[0].Data);
    }

    [Theory]
    [InlineData(ChildScopePolicy.Fresh)]
    [InlineData(ChildScopePolicy.Retained)]
    [InlineData(ChildScopePolicy.Shared)]
    public async Task CycleSiblingsShareCommonDependencyStateOnlyInSharedMode(ChildScopePolicy mode)
    {
        var shared = new CountingMutations();
        var first = new ProbeAlgorithm { Mutator = shared };
        var second = first with { };
        var cycle = new CycleAlgorithm<IAlgorithm<int, ProbeState>, int, ProbeState>([first, second])
        {
            MaximumCycles = 2,
            ScopePolicy = mode
        };
        var root = ResolutionScope.Create();
        var states = await Run(Resolve(root, cycle));
        states.Select(state => state.Value).ShouldBe(mode switch
        {
            ChildScopePolicy.Fresh => [1, 1, 1, 1],
            ChildScopePolicy.Retained => [1, 1, 2, 2],
            _ => new[] { 1, 2, 3, 4 }
        });
        states[0].Data.ShouldNotBeSameAs(states[1].Data);
        var direct = root.Resolve<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>>(shared);
        direct.Mutate([0], RandomNumberGenerator.Create(1), problem.SearchSpace, problem).ShouldBe(mode == ChildScopePolicy.Shared ? [5] : [1]);
    }

    [Theory]
    [InlineData(ChildScopePolicy.Fresh)]
    [InlineData(ChildScopePolicy.Retained)]
    [InlineData(ChildScopePolicy.Shared)]
    public async Task Cycle_ActivationsInheritStateAlreadyOwnedByAnAncestor(ChildScopePolicy mode)
    {
        var algorithm = new ProbeAlgorithm();
        var root = ResolutionScope.Create();
        var ancestor = (await Run(Resolve(root, algorithm))).Single();

        var states = await Run(Resolve(root, Cycle(algorithm, false) with { ScopePolicy = mode }));

        states.Select(state => state.Call).ShouldBe([2, 3, 4, 5]);
        states.ShouldAllBe(state => ReferenceEquals(state.Data, ancestor.Data));
    }

    [Fact]
    public async Task Cycle_RetainedStateSurvivesRebindingWithoutSharingWithOtherOwnersOrDirectResolution()
    {
        var mutator = new CountingMutations();
        var first = new ProbeAlgorithm { Mutator = mutator };
        var equal = first with { };
        equal.ShouldBe(first);
        var cycle = new CycleAlgorithm<IAlgorithm<int, ProbeState>, int, ProbeState>([first, first, equal])
        {
            MaximumCycles = 2,
            ScopePolicy = ChildScopePolicy.Retained
        };
        var root = ResolutionScope.Create();
        var observations = new List<string>();
        var original = Resolve(root.With(Aspect.Observe(mutator, _ => observations.Add("original"))), cycle);
        var before = await Run(original);
        before.Select(state => state.Call).ShouldBe([1, 2, 1, 3, 4, 2]);
        before[0].Data.ShouldBeSameAs(before[1].Data);
        before[0].Data.ShouldNotBeSameAs(before[2].Data);

        var rebuilt = Resolve(root.With(Aspect.Observe(mutator, _ => observations.Add("rebuilt"))), cycle);
        var after = await Run(rebuilt);
        after.Select(state => state.Call).ShouldBe([5, 6, 3, 7, 8, 4]);
        after[0].Data.ShouldBeSameAs(before[0].Data);
        observations.ShouldBe([.. Enumerable.Repeat("original", 6), .. Enumerable.Repeat("rebuilt", 6)]);

        var otherOwner = await Run(Resolve(root, cycle with { }));
        otherOwner.Select(state => state.Call).ShouldBe([1, 2, 1, 3, 4, 2]);
        otherOwner[0].Data.ShouldNotBeSameAs(before[0].Data);
        var direct = (await Run(Resolve(root, first))).Single();
        direct.Call.ShouldBe(1);
        direct.Data.ShouldNotBeSameAs(before[0].Data);

        await Run(original);
        observations.ShouldBe([.. Enumerable.Repeat("original", 6), .. Enumerable.Repeat("rebuilt", 6), .. Enumerable.Repeat("original", 6)]);
    }

    [Fact]
    public void Cycle_RetainedStateDoesNotRetainTheFirstObserver()
    {
        var algorithm = new ProbeAlgorithm { Mutator = new CountingMutations() };
        var cycle = Cycle(algorithm, false) with { ScopePolicy = ChildScopePolicy.Retained };
        var root = ResolutionScope.Create();
        var retired = RunUnderTemporaryObserver(root, cycle, algorithm.Mutator);
        for (var attempt = 0; attempt < 3 && retired.IsAlive; attempt++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }
        retired.IsAlive.ShouldBeFalse();
        GC.KeepAlive(root);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference RunUnderTemporaryObserver(ResolutionScope root,
        CycleAlgorithm<IAlgorithm<int, ProbeState>, int, ProbeState> cycle, IOperator<Mutation, int> mutator)
    {
        var observer = Aspect.Observe(mutator, _ => { });
        Run(Resolve(root.With(observer), cycle)).GetAwaiter().GetResult();
        return new WeakReference(observer);
    }

    [Fact]
    public async Task Pipeline_StagesAreFreshAndDoNotPublishStateToTheirParent()
    {
        var algorithm = new ProbeAlgorithm();
        var pipeline = new PipelineAlgorithm<IAlgorithm<int, ProbeState>, int, ProbeState>([algorithm, algorithm]);
        var root = ResolutionScope.Create();
        var executor = Resolve(root, pipeline);
        var forks = new List<string>();
        var first = await Run(executor, new RecordingRandom("first", forks));
        var second = await Run(executor, new RecordingRandom("second", forks));
        first.Select(state => state.Value).ShouldBe([1, 2]);
        first.Select(state => state.Call).ShouldBe([1, 1]);
        second.Select(state => state.Call).ShouldBe([1, 1]);
        first[0].Data.ShouldNotBeSameAs(first[1].Data);
        first[0].Data.ShouldNotBeSameAs(second[0].Data);
        forks.ShouldBe(["first/0", "first/1", "second/0", "second/1"]);

        var ancestor = (await Run(Resolve(root, algorithm))).Single();
        ancestor.Call.ShouldBe(1);
        var inherited = await Run(executor);
        inherited.Select(state => state.Call).ShouldBe([2, 3]);
        inherited.ShouldAllBe(state => ReferenceEquals(state.Data, ancestor.Data));
    }

    [Theory]
    [InlineData(true, true)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public async Task PausedDeferredStream_KeepsItsObservationContextForLaterActivations(bool cycle, bool reset)
    {
        var algorithm = new ProbeAlgorithm();
        IAlgorithm<int, ProbeState> source = cycle ? Cycle(algorithm, reset)
            : new PipelineAlgorithm<IAlgorithm<int, ProbeState>, int, ProbeState>([algorithm, algorithm]);
        var root = ResolutionScope.Create();
        var original = Resolve(root, source);
        await using var paused = original.RunStreamingAsync(problem, RandomNumberGenerator.Create(42), ct: TestContext.Current.CancellationToken)
            .GetAsyncEnumerator(TestContext.Current.CancellationToken);
        (await paused.MoveNextAsync()).ShouldBeTrue();
        var values = new List<int> { paused.Current.Value };
        var observedCalls = new CountAccumulator();
        var child = root.CreateChildScope(Aspect.CountCalls(algorithm.Mutator, observedCalls));

        var observed = await Run(Resolve(child, source));
        observedCalls.CurrentCount.ShouldBe(cycle ? 4 : 2);
        while (await paused.MoveNextAsync())
            values.Add(paused.Current.Value);

        values.ShouldBe(cycle ? [1, 2, 3, 4] : [1, 2]);
        observed.Select(state => state.Value).ShouldBe(values);
        observedCalls.CurrentCount.ShouldBe(cycle ? 4 : 2);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task OperatorBudget_RebindingPreservesUsageAndOneMeasurementDeclaration(bool measureDuration)
    {
        var clock = new ManualClock();
        var algorithm = new ProbeAlgorithm { Mutator = new AdvancingMutator(clock), Steps = 4 };
        var accumulators = new List<object>();
        IAlgorithm<int, ProbeState> budget = measureDuration
            ? new OperatorDurationBudgetAlgorithm<int, ProbeState, IOperator<Mutation, int>>
            {
                Algorithm = algorithm,
                ObservedOperator = algorithm.Mutator,
                MaximumDuration = TimeSpan.FromSeconds(3),
                TimeProvider = clock,
                DurationMeasuring = new RecordingMutatorDurationMeasuring<int>((source, duration, timeProvider) =>
                {
                    source.ShouldBeSameAs(algorithm.Mutator);
                    accumulators.Add(duration);
                    return new DurationMeasuringOperatorAdvice<Mutation, int>(duration, timeProvider);
                })
            }
            : new OperatorBudgetAlgorithm<int, ProbeState, IOperator<Mutation, int>>
            {
                Algorithm = algorithm,
                ObservedOperator = algorithm.Mutator,
                MaximumCount = 3,
                Counting = new RecordingMutatorCounting<int>((source, count) =>
                {
                    source.ShouldBeSameAs(algorithm.Mutator);
                    accumulators.Add(count);
                    return new CountingOperatorAdvice<Mutation, int>(count, OperatorCountMetric.Calls);
                })
            };
        var root = ResolutionScope.Create();
        var original = Resolve(root, budget);
        await using var paused = original.RunStreamingAsync(problem, RandomNumberGenerator.Create(42), ct: TestContext.Current.CancellationToken)
            .GetAsyncEnumerator(TestContext.Current.CancellationToken);
        (await paused.MoveNextAsync()).ShouldBeTrue();
        var observedCalls = new CountAccumulator();
        var child = root.CreateChildScope(Aspect.CountCalls(algorithm.Mutator, observedCalls));

        var observed = await Run(Resolve(child, budget));
        observed.Select(state => state.Call).ShouldBe([2, 3]);
        observedCalls.CurrentCount.ShouldBe(2);
        (await paused.MoveNextAsync()).ShouldBeFalse();
        (await Run(original)).Count.ShouldBe(1);
        observedCalls.CurrentCount.ShouldBe(2);
        accumulators.Count.ShouldBe(1);
        if (measureDuration)
            ((DurationAccumulator)accumulators[0]).CurrentDuration.ShouldBe(TimeSpan.FromSeconds(4));
        else
            ((CountAccumulator)accumulators[0]).CurrentCount.ShouldBe(4);

        var independent = await Run(Resolve(ResolutionScope.Create(), budget));
        independent.Select(state => state.Call).ShouldBe([1, 2, 3]);
        independent[0].Data.ShouldNotBeSameAs(observed[0].Data);
        accumulators.Count.ShouldBe(2);
        accumulators[0].ShouldNotBeSameAs(accumulators[1]);
        observedCalls.CurrentCount.ShouldBe(2);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task BudgetSharesItsInnerAlgorithmStateAndCountsOnlyCallsThroughTheBudget(bool measureDuration)
    {
        var clock = new ManualClock();
        var algorithm = new ProbeAlgorithm { Mutator = new AdvancingMutator(clock), Steps = 4 };
        CountAccumulator? count = null;
        DurationAccumulator? duration = null;
        IAlgorithm<int, ProbeState> budget = measureDuration
            ? new OperatorDurationBudgetAlgorithm<int, ProbeState, IOperator<Mutation, int>>
            {
                Algorithm = algorithm,
                ObservedOperator = algorithm.Mutator,
                MaximumDuration = TimeSpan.FromSeconds(2),
                TimeProvider = clock,
                DurationMeasuring = new RecordingMutatorDurationMeasuring<int>((source, accumulator, provider) =>
                {
                    duration = accumulator;
                    return new DurationMeasuringOperatorAdvice<Mutation, int>(accumulator, provider);
                })
            }
            : new OperatorBudgetAlgorithm<int, ProbeState, IOperator<Mutation, int>>
            {
                Algorithm = algorithm,
                ObservedOperator = algorithm.Mutator,
                MaximumCount = 2,
                Counting = new RecordingMutatorCounting<int>((source, accumulator) =>
                {
                    count = accumulator;
                    return new CountingOperatorAdvice<Mutation, int>(accumulator, OperatorCountMetric.Calls);
                })
            };
        var root = ResolutionScope.Create();
        var limited = Resolve(root, budget);
        var direct = await Run(Resolve(root, algorithm));
        var throughBudget = await Run(limited);
        throughBudget.Select(state => state.Call).ShouldBe([5, 6]);
        throughBudget[0].Data.ShouldBeSameAs(direct[0].Data);
        var laterDirect = await Run(Resolve(root, algorithm));
        laterDirect.Select(state => state.Call).ShouldBe([7, 8, 9, 10]);
        if (measureDuration)
            duration.ShouldNotBeNull().CurrentDuration.ShouldBe(TimeSpan.FromSeconds(2));
        else
            count.ShouldNotBeNull().CurrentCount.ShouldBe(2);
    }

    [Fact]
    public async Task NestedCountAndDurationBudgets_KeepBothDeclarationsAcrossRebinding()
    {
        var clock = new ManualClock();
        var algorithm = new ProbeAlgorithm { Mutator = new AdvancingMutator(clock), Steps = 4 };
        var durationBudget = algorithm.LimitedToMutatorDuration(algorithm.Mutator, TimeSpan.FromSeconds(3), clock);
        var budget = durationBudget.LimitedToMutatorCalls(algorithm.Mutator, 2);
        var root = ResolutionScope.Create();
        var first = await Run(Resolve(root, budget));
        first.Select(state => state.Call).ShouldBe([1, 2]);
        var observedCalls = new CountAccumulator();
        var child = root.CreateChildScope(Aspect.CountCalls(algorithm.Mutator, observedCalls));

        var observed = await Run(Resolve(child, budget));
        observed.Select(state => state.Call).ShouldBe([3]);
        observed[0].Data.ShouldBeSameAs(first[0].Data);
        observedCalls.CurrentCount.ShouldBe(1);
        (await Run(Resolve(root, budget))).Count.ShouldBe(1);
        observedCalls.CurrentCount.ShouldBe(1);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task OperatorBudget_RebindingKeepsClockBeforeTraceAndExcludesObservationDuration(bool measureDuration)
    {
        var clock = new ManualClock();
        var algorithm = new ProbeAlgorithm { Mutator = new AdvancingMutator(clock), Steps = 4 };
        var calls = new MutationCallClock(algorithm.Mutator);
        var trace = Analyzer.Trace(algorithm.Mutator, observation =>
        {
            clock.Advance(10);
            return observation.Offspring[0];
        }, clocks: [calls]);
        var count = new CountAccumulator();
        var duration = new DurationAccumulator();
        var declarations = 0;
        IAlgorithm<int, ProbeState> budget = measureDuration
            ? new OperatorDurationBudgetAlgorithm<int, ProbeState, IOperator<Mutation, int>>
            {
                Algorithm = algorithm,
                ObservedOperator = algorithm.Mutator,
                MaximumDuration = TimeSpan.FromSeconds(3),
                TimeProvider = clock,
                DurationMeasuring = new RecordingMutatorDurationMeasuring<int>((source, accumulator, timeProvider) =>
                {
                    declarations++;
                    duration = accumulator;
                    return new DurationMeasuringOperatorAdvice<Mutation, int>(accumulator, timeProvider);
                })
            }
            : new OperatorBudgetAlgorithm<int, ProbeState, IOperator<Mutation, int>>
            {
                Algorithm = algorithm,
                ObservedOperator = algorithm.Mutator,
                MaximumCount = 3,
                Counting = new RecordingMutatorCounting<int>((source, accumulator) =>
                {
                    declarations++;
                    count = accumulator;
                    return new CountingOperatorAdvice<Mutation, int>(accumulator, OperatorCountMetric.Calls);
                })
            };
        var root = ResolutionScope.Create(trace);
        await using var paused = Resolve(root, budget).RunStreamingAsync(problem, RandomNumberGenerator.Create(42), ct: TestContext.Current.CancellationToken)
            .GetAsyncEnumerator(TestContext.Current.CancellationToken);
        (await paused.MoveNextAsync()).ShouldBeTrue();
        var observed = 0;
        var child = root.CreateChildScope(Aspect.Observe(algorithm.Mutator, _ =>
        {
            observed++;
            clock.Advance(20);
        }));

        (await Run(Resolve(child, budget))).Count.ShouldBe(2);
        (await paused.MoveNextAsync()).ShouldBeFalse();

        trace.By(calls).Select(point => point.Time).ShouldBe([1L, 2L, 3L]);
        trace.By(calls).Select(point => point.Value).ShouldBe([1, 1, 2]);
        observed.ShouldBe(2);
        declarations.ShouldBe(1);
        if (measureDuration)
            duration.CurrentDuration.ShouldBe(TimeSpan.FromSeconds(3));
        else
            count.CurrentCount.ShouldBe(3);
    }

    [Fact]
    public async Task StateTermination_RebindingSharesTheLimitAndObservesContextualChecks()
    {
        var algorithm = new ProbeAlgorithm { Steps = 4 };
        ITerminator<int> terminator = new AfterIterationsTerminator<int>(2);
        var source = algorithm.TerminatedBy(terminator);
        var root = ResolutionScope.Create();
        (await Run(Resolve(root, source))).Count.ShouldBe(2);
        var observedCalls = new CountAccumulator();
        var child = root.CreateChildScope(Aspect.CountCalls(terminator, observedCalls));

        (await Run(Resolve(child, source))).Count.ShouldBe(1);
        observedCalls.CurrentCount.ShouldBe(1);
        (await Run(Resolve(root, source))).Count.ShouldBe(1);
        observedCalls.CurrentCount.ShouldBe(1);
        (await Run(Resolve(ResolutionScope.Create(), source))).Count.ShouldBe(2);
    }

    [Fact]
    public async Task AlgorithmDuration_PausedInvocationKeepsItsOwnDurationAcrossRebinding()
    {
        var clock = new ManualClock();
        var algorithm = new ProbeAlgorithm { Mutator = new AdvancingMutator(clock), Steps = 4 };
        var budget = algorithm.LimitedToDuration(TimeSpan.FromSeconds(2), clock);
        var root = ResolutionScope.Create();
        var original = Resolve(root, budget);
        await using var paused = original.RunStreamingAsync(problem, RandomNumberGenerator.Create(42), ct: TestContext.Current.CancellationToken)
            .GetAsyncEnumerator(TestContext.Current.CancellationToken);
        (await paused.MoveNextAsync()).ShouldBeTrue();
        paused.Current.Value.ShouldBe(1);
        var observedCalls = new CountAccumulator();
        var child = root.CreateChildScope(Aspect.CountCalls(algorithm.Mutator, observedCalls));

        (await Run(Resolve(child, budget))).Count.ShouldBe(2);
        observedCalls.CurrentCount.ShouldBe(2);
        (await paused.MoveNextAsync()).ShouldBeTrue();
        paused.Current.Value.ShouldBe(2);
        (await paused.MoveNextAsync()).ShouldBeFalse();
        observedCalls.CurrentCount.ShouldBe(2);
        (await Run(original)).Count.ShouldBe(2);
        observedCalls.CurrentCount.ShouldBe(2);
    }

    private static CycleAlgorithm<IAlgorithm<int, ProbeState>, int, ProbeState> Cycle(ProbeAlgorithm algorithm, bool reset) =>
        new([algorithm, algorithm]) { MaximumCycles = 2, ScopePolicy = reset ? ChildScopePolicy.Fresh : ChildScopePolicy.Shared };

    private static IAlgorithmExecutor<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>, ProbeState> Resolve(ResolutionScope scope, IAlgorithm<int, ProbeState> algorithm) =>
        scope.Resolve<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>, ProbeState>(algorithm);

    private static async Task<List<ProbeState>> Run(IAlgorithmExecutor<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>, ProbeState> executor, IRandomNumberGenerator? random = null)
    {
        var states = new List<ProbeState>();
        await foreach (var state in executor.RunStreamingAsync(problem, random ?? RandomNumberGenerator.Create(42), ct: TestContext.Current.CancellationToken))
            states.Add(state);
        return states;
    }

    private sealed record ProbeState(int Value, int Call, ProbeData Data) : ISearchState;

    private sealed class ProbeData
    {
        public int Calls;
        public int Starts;
        public int Disposals;
    }

    private sealed record ProbeAlgorithm : Algorithm<ProbeAlgorithm, int, ProbeState>
    {
        public IOperator<Mutation, int> Mutator { get; init; } = new PassThroughMutator();
        public int Steps { get; init; } = 1;
        public Action<ProbeData> Prepared { get; init; } = static _ => { };

        public override ExecutorFactory<IAlgorithmExecutor<int, TRunSearchSpace, TRunProblem, ProbeState>> CreateExecutorFactory<TRunSearchSpace, TRunProblem>()
        {
            var data = new ProbeData();
            Prepared(data);
            return scope => new Executor<TRunSearchSpace, TRunProblem>(data, scope.Resolve<int, TRunSearchSpace, TRunProblem>(Mutator), Steps);
        }

        private sealed class Executor<TSearchSpace, TProblem>(ProbeData data, IMutationExecutor<int, TSearchSpace, TProblem> mutator, int steps)
            : AlgorithmExecutor<int, TSearchSpace, TProblem, ProbeState>
            where TSearchSpace : class, ISearchSpace<int>
            where TProblem : class, IProblem<int, TSearchSpace>
        {
            public override async IAsyncEnumerable<ProbeState> RunStreamingAsync(TProblem problem, IRandomNumberGenerator random, ProbeState? initialState = null, [EnumeratorCancellation] CancellationToken ct = default)
            {
                data.Starts++;
                var value = initialState?.Value ?? 0;
                try
                {
                    for (var index = 0; index < steps; index++)
                    {
                        ct.ThrowIfCancellationRequested();
                        value = mutator.Mutate([value + 1], random, problem.SearchSpace, problem).Single();
                        yield return new ProbeState(value, ++data.Calls, data);
                        await Task.CompletedTask;
                    }
                }
                finally
                {
                    data.Disposals++;
                }
            }
        }
    }

    private sealed class ManualClock : TimeProvider
    {
        private long timestamp;
        public override long TimestampFrequency => TimeSpan.TicksPerSecond;
        public override long GetTimestamp() => timestamp;
        public void Advance(int seconds = 1)
        {
            timestamp += seconds * TimeSpan.TicksPerSecond;
        }
    }

    private sealed class MutationCallClock : Clock<long>
    {
        private long calls;
        protected override long ReadTime() => calls;
        public MutationCallClock(IOperator<Mutation, int> mutator)
        {
            Advices = Aspect.Observe(mutator, _ => calls++).Advices;
        }
    }

    private sealed class MutationData { public int Calls; }

    private sealed record CountingMutations : StatefulMutator<int, MutationData>
    {
        protected override MutationData CreateInitialState() => new();
        protected override IReadOnlyList<int> Mutate(IReadOnlyList<int> parents, MutationData state, IRandomNumberGenerator random) => [++state.Calls];
    }

    private sealed record PassThroughMutator : StatelessMutator<int>
    {
        public override IReadOnlyList<int> Mutate(IReadOnlyList<int> parents, IRandomNumberGenerator random) => parents;
    }

    private sealed record AdvancingMutator(ManualClock Clock) : StatelessMutator<int>
    {
        public override IReadOnlyList<int> Mutate(IReadOnlyList<int> parents, IRandomNumberGenerator random)
        {
            Clock.Advance();
            return parents;
        }
    }

    private sealed record RecordingRandom(string Path, List<string> Forks) : IRandomNumberGenerator
    {
        public double NextDouble() => throw new InvalidOperationException("No random draws are expected.");
        public int NextInt() => throw new InvalidOperationException("No random draws are expected.");
        public IRandomNumberGenerator Fork(ulong forkKey)
        {
            var childPath = $"{Path}/{forkKey}";
            Forks.Add(childPath);
            return new RecordingRandom(childPath, Forks);
        }
    }
    private sealed record RecordingMutatorCounting<TCandidate>(Func<IOperator<Mutation, TCandidate>, CountAccumulator, OperatorAdvice<Mutation, TCandidate>> Create) : IOperatorCounting<IOperator<Mutation, TCandidate>>
    {
        public Advice<IOperator<Mutation, TCandidate>> CreateAdvice(CountAccumulator counter) => new SelectedAdvice(source => Create(source, counter));

        private sealed class SelectedAdvice(Func<IOperator<Mutation, TCandidate>, OperatorAdvice<Mutation, TCandidate>> create) : OperatorAdvice<Mutation, TCandidate>
        {
            public override OperatorAdviceWrapperFactory<Mutation, TCandidate> CreateWrapperFactory(IOperator<Mutation, TCandidate> configuration) =>
                create(configuration).CreateWrapperFactory(configuration);
        }
    }

    private sealed record RecordingMutatorDurationMeasuring<TCandidate>(Func<IOperator<Mutation, TCandidate>, DurationAccumulator, TimeProvider, OperatorAdvice<Mutation, TCandidate>> Create) : IOperatorDurationMeasuring<IOperator<Mutation, TCandidate>>
    {
        public Advice<IOperator<Mutation, TCandidate>> CreateAdvice(DurationAccumulator duration, TimeProvider timeProvider) => new SelectedAdvice(source => Create(source, duration, timeProvider));

        private sealed class SelectedAdvice(Func<IOperator<Mutation, TCandidate>, OperatorAdvice<Mutation, TCandidate>> create) : OperatorAdvice<Mutation, TCandidate>
        {
            public override OperatorAdviceWrapperFactory<Mutation, TCandidate> CreateWrapperFactory(IOperator<Mutation, TCandidate> configuration) =>
                create(configuration).CreateWrapperFactory(configuration);
        }
    }

}
