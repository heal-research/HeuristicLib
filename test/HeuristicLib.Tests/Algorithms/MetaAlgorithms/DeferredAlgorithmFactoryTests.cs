using System.Runtime.CompilerServices;
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
    [InlineData(true)]
    [InlineData(false)]
    public async Task Cycle_RepeatedChildrenRespectResetModeAcrossInvocations(bool reset)
    {
        var prepared = new List<ProbeData>();
        var algorithm = new ProbeAlgorithm { Prepared = prepared.Add };
        var execution = Resolve(ResolutionScope.Create(), Cycle(algorithm, reset));
        var forks = new List<string>();

        var first = await Run(execution, new RecordingRandom("first", forks));
        var second = await Run(execution, new RecordingRandom("second", forks));

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
    public async Task Cycle_RetainedSlotsUseReferenceIdentityAndBelongToTheirOwner()
    {
        var algorithm = new ProbeAlgorithm();
        var equal = algorithm with { };
        equal.ShouldBe(algorithm);
        var cycle = new CycleAlgorithm<IAlgorithm<int, ProbeState>, int, ProbeState>([algorithm, equal])
        {
            MaximumCycles = 2,
            NewExecutionInstancesPerCycle = false
        };
        var root = ResolutionScope.Create();
        var execution = Resolve(root, cycle);
        var first = await Run(execution);
        first.Select(state => state.Call).ShouldBe([1, 1, 2, 2]);
        first[0].Data.ShouldBeSameAs(first[2].Data);
        first[0].Data.ShouldNotBeSameAs(first[1].Data);

        var otherOwner = await Run(Resolve(root, cycle with { }));
        otherOwner[0].Data.ShouldNotBeSameAs(first[0].Data);
        otherOwner[0].Call.ShouldBe(1);
        var ancestor = (await Run(Resolve(root, algorithm))).Single();
        ancestor.Data.ShouldNotBeSameAs(first[0].Data);
        var next = await Run(execution);
        next.Select(state => state.Call).ShouldBe([3, 3, 4, 4]);
        next[0].Data.ShouldBeSameAs(first[0].Data);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Cycle_ActivationsInheritStateAlreadyOwnedByAnAncestor(bool reset)
    {
        var algorithm = new ProbeAlgorithm();
        var root = ResolutionScope.Create();
        var ancestor = (await Run(Resolve(root, algorithm))).Single();

        var states = await Run(Resolve(root, Cycle(algorithm, reset)));

        states.Select(state => state.Call).ShouldBe([2, 3, 4, 5]);
        states.ShouldAllBe(state => ReferenceEquals(state.Data, ancestor.Data));
    }

    [Fact]
    public async Task Pipeline_StagesAreFreshAndDoNotPublishStateToTheirParent()
    {
        var algorithm = new ProbeAlgorithm();
        var pipeline = new PipelineAlgorithm<IAlgorithm<int, ProbeState>, int, ProbeState>([algorithm, algorithm]);
        var root = ResolutionScope.Create();
        var execution = Resolve(root, pipeline);
        var forks = new List<string>();
        var first = await Run(execution, new RecordingRandom("first", forks));
        var second = await Run(execution, new RecordingRandom("second", forks));
        first.Select(state => state.Value).ShouldBe([1, 2]);
        first.Select(state => state.Call).ShouldBe([1, 1]);
        second.Select(state => state.Call).ShouldBe([1, 1]);
        first[0].Data.ShouldNotBeSameAs(first[1].Data);
        first[0].Data.ShouldNotBeSameAs(second[0].Data);
        forks.ShouldBe(["first/0", "first/1", "second/0", "second/1"]);

        var ancestor = (await Run(Resolve(root, algorithm))).Single();
        ancestor.Call.ShouldBe(1);
        var inherited = await Run(execution);
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
        var child = root.CreateChildScope(builder => builder.Wrap(algorithm.Mutator, originalMutator => originalMutator.CountCalls(observedCalls)));

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
            ? new OperatorDurationBudgetAlgorithm<int, ProbeState, IMutator<int>>
            {
                Algorithm = algorithm,
                ObservedOperator = algorithm.Mutator,
                MaximumDuration = TimeSpan.FromSeconds(3),
                TimeProvider = clock,
                MeasuredOperatorFactory = (source, duration, timeProvider) =>
                {
                    source.ShouldBeSameAs(algorithm.Mutator);
                    accumulators.Add(duration);
                    return source.MeasureDuration(duration, timeProvider);
                }
            }
            : new OperatorBudgetAlgorithm<int, ProbeState, IMutator<int>>
            {
                Algorithm = algorithm,
                ObservedOperator = algorithm.Mutator,
                MaximumCount = 3,
                CountedOperatorFactory = (source, count) =>
                {
                    source.ShouldBeSameAs(algorithm.Mutator);
                    accumulators.Add(count);
                    return source.CountCalls(count);
                }
            };
        var root = ResolutionScope.Create();
        var original = Resolve(root, budget);
        await using var paused = original.RunStreamingAsync(problem, RandomNumberGenerator.Create(42), ct: TestContext.Current.CancellationToken)
            .GetAsyncEnumerator(TestContext.Current.CancellationToken);
        (await paused.MoveNextAsync()).ShouldBeTrue();
        var observedCalls = new CountAccumulator();
        var child = root.CreateChildScope(builder => builder.Wrap(algorithm.Mutator, source => source.CountCalls(observedCalls)));

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
        var child = root.CreateChildScope(builder => builder.Wrap(algorithm.Mutator, source => source.CountCalls(observedCalls)));

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
            ? new OperatorDurationBudgetAlgorithm<int, ProbeState, IMutator<int>>
            {
                Algorithm = algorithm,
                ObservedOperator = algorithm.Mutator,
                MaximumDuration = TimeSpan.FromSeconds(3),
                TimeProvider = clock,
                MeasuredOperatorFactory = (source, accumulator, timeProvider) =>
                {
                    declarations++;
                    duration = accumulator;
                    return source.MeasureDuration(accumulator, timeProvider);
                }
            }
            : new OperatorBudgetAlgorithm<int, ProbeState, IMutator<int>>
            {
                Algorithm = algorithm,
                ObservedOperator = algorithm.Mutator,
                MaximumCount = 3,
                CountedOperatorFactory = (source, accumulator) =>
                {
                    declarations++;
                    count = accumulator;
                    return source.CountCalls(accumulator);
                }
            };
        var root = ResolutionScope.Create(builder => trace.Install(builder));
        await using var paused = Resolve(root, budget).RunStreamingAsync(problem, RandomNumberGenerator.Create(42), ct: TestContext.Current.CancellationToken)
            .GetAsyncEnumerator(TestContext.Current.CancellationToken);
        (await paused.MoveNextAsync()).ShouldBeTrue();
        var observed = 0;
        var child = root.CreateChildScope(builder => builder.Observe(algorithm.Mutator, _ =>
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
        var child = root.CreateChildScope(builder => builder.Wrap(terminator, original => original.CountCalls(observedCalls)));

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
        var child = root.CreateChildScope(builder => builder.Wrap(algorithm.Mutator, source => source.CountCalls(observedCalls)));

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
        new([algorithm, algorithm]) { MaximumCycles = 2, NewExecutionInstancesPerCycle = reset };

    private static IAlgorithmExecution<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>, ProbeState> Resolve(ResolutionScope scope, IAlgorithm<int, ProbeState> algorithm) =>
        scope.Resolve<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>, ProbeState>(algorithm);

    private static async Task<List<ProbeState>> Run(IAlgorithmExecution<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>, ProbeState> execution, IRandomNumberGenerator? random = null)
    {
        var states = new List<ProbeState>();
        await foreach (var state in execution.RunStreamingAsync(problem, random ?? RandomNumberGenerator.Create(42), ct: TestContext.Current.CancellationToken))
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
        public IMutator<int> Mutator { get; init; } = new PassThroughMutator();
        public int Steps { get; init; } = 1;
        public Action<ProbeData> Prepared { get; init; } = static _ => { };

        public override ExecutionFactory<IAlgorithmExecution<int, TRunSearchSpace, TRunProblem, ProbeState>> CreateExecutionFactory<TRunSearchSpace, TRunProblem>()
        {
            var data = new ProbeData();
            Prepared(data);
            return scope => new Execution<TRunSearchSpace, TRunProblem>(data, scope.Resolve<int, TRunSearchSpace, TRunProblem>(Mutator), Steps);
        }

        private sealed class Execution<TSearchSpace, TProblem>(ProbeData data, IMutatorExecution<int, TSearchSpace, TProblem> mutator, int steps)
            : AlgorithmExecution<int, TSearchSpace, TProblem, ProbeState>
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

    private sealed class MutationCallClock(IMutator<int> mutator) : Clock<long>
    {
        private long calls;
        protected override long ReadTime() => calls;
        public override void Install(ResolutionScopeBuilder builder) => builder.Observe(mutator, _ => calls++);
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
}
