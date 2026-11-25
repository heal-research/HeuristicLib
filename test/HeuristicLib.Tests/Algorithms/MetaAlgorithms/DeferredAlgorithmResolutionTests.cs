using System.Runtime.CompilerServices;
using HEAL.HeuristicLib.Analysis;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Inspection;
using HEAL.HeuristicLib.Operators.Mutators;
using HEAL.HeuristicLib.Operators.Terminators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.Tests.TestSupport;
using HEAL.HeuristicLib.Tests.TestSupport.Mocks;

namespace HEAL.HeuristicLib.Tests.Algorithms.MetaAlgorithms;

public sealed class DeferredAlgorithmResolutionTests
{
    private static readonly IProblem<int, DummySearchSpace<int>> problem = MetaAlgorithmTestHelpers.CreateIntegerProblem();

    [Theory]
    [InlineData(false)]
    [InlineData(true)]

    public async Task Cycle_RepeatedChildrenRespectSharingAcrossInvocations(bool mode)
    {
        var reset = mode == false;
        var prepared = new List<ProbeData>();
        var algorithm = new ProbeAlgorithm { Prepared = prepared.Add };
        var executor = Resolve(ResolutionScope.Create(), Cycle(algorithm, reset));
        var forks = new List<string>();

        var first = await Run(executor, new RecordingRandom("first", forks.Add));
        var second = await Run(executor, new RecordingRandom("second", forks.Add));

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
    public async Task Cycle_SharingBelongsToTheScopeAndUsesReferenceIdentity()
    {
        var algorithm = new ProbeAlgorithm();
        var equal = algorithm with { };
        equal.ShouldBe(algorithm);
        var cycle = new CycleAlgorithm<int, ProbeState>([algorithm, equal]) { MaximumCycles = 2 }
            .Sharing(algorithm, equal);
        var root = ResolutionScope.Create();
        var executor = Resolve(root, cycle);
        var first = await Run(executor);
        first.Select(state => state.Call).ShouldBe([1, 1, 2, 2]);
        first[0].Data.ShouldBeSameAs(first[2].Data);
        first[0].Data.ShouldNotBeSameAs(first[1].Data);

        var otherOwner = await Run(Resolve(root.CreateChildScope(), cycle));
        otherOwner[0].Data.ShouldNotBeSameAs(first[0].Data);
        otherOwner.Select(state => state.Call).ShouldBe([1, 1, 2, 2]);
        // The cycle shares the algorithm in the scope it is resolved in, so a direct use there gets the same executor.
        var direct = (await Run(Resolve(root, algorithm))).Single();
        direct.Data.ShouldBeSameAs(first[0].Data);
        direct.Call.ShouldBe(3);
        var next = await Run(executor);
        next.Select(state => state.Call).ShouldBe([4, 3, 5, 4]);
        next[0].Data.ShouldBeSameAs(first[0].Data);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]

    public async Task CycleSiblingsKeepSeparateChildrenWhenTheyAreShared(bool mode)
    {
        var shared = new CountingMutations();
        var first = new ProbeAlgorithm { Mutator = shared };
        var second = first with { };
        var fresh = new CycleAlgorithm<int, ProbeState>([first, second]) { MaximumCycles = 2 };
        IAlgorithm<int, ProbeState> cycle = mode ? fresh.Sharing(first, second) : fresh;
        var root = ResolutionScope.Create();
        var states = await Run(Resolve(root, cycle));
        states.Select(state => state.Value).ShouldBe(mode switch
        {
            false => [1, 1, 1, 1],
            true => [1, 1, 2, 2]
        });
        states[0].Data.ShouldNotBeSameAs(states[1].Data);
        var direct = root.Resolve<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>>(shared, problem);
        direct.Mutate([0], RandomNumberGenerator.Create(1)).ShouldBe([1]);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Cycle_SharedActivationsUseTheScopesExecutorAndOtherActivationsTheirOwn(bool shared)
    {
        var algorithm = new ProbeAlgorithm();
        var root = ResolutionScope.Create();

        var states = await Run(Resolve(root, Cycle(algorithm, reset: !shared)));
        var direct = (await Run(Resolve(root, algorithm))).Single();

        states.Select(state => state.Call).ShouldBe(shared ? [1, 2, 3, 4] : [1, 1, 1, 1]);
        // A direct use in the sharing scope receives the shared executor; without sharing, the scope builds its own.
        direct.Call.ShouldBe(shared ? 5 : 1);
        states.ShouldAllBe(state => ReferenceEquals(state.Data, direct.Data) == shared);
    }

    // A scope fixes how it resolves an algorithm when the resolution starts, so the algorithm's own construction cannot
    // share it.
    [Fact]
    public void Algorithm_SharingItselfWhileBuilding_IsAnError()
    {
        ProbeAlgorithm? algorithm = null;
        algorithm = new ProbeAlgorithm { Building = scope => scope.Share(algorithm!) };

        Should.Throw<InvalidOperationException>(() => Resolve(ResolutionScope.Create(), algorithm)).Message.ShouldContain("has already resolved it");
    }

    // Sharing must not depend on the order of resolution, so an algorithm the scope already built cannot be shared
    // there afterwards.
    [Fact]
    public void Cycle_SharingAnAlgorithmTheScopeHasAlreadyBuilt_IsAnError()
    {
        var algorithm = new ProbeAlgorithm();
        var root = ResolutionScope.Create();
        _ = Resolve(root, algorithm);

        Should.Throw<InvalidOperationException>(() => Resolve(root, Cycle(algorithm, reset: false))).Message.ShouldContain("has already resolved it");
    }

    [Fact]
    public async Task Cycle_SharedExecutorsKeepTheirChildrenAndTheObservationOfTheirGraph()
    {
        var mutator = new CountingMutations();
        var first = new ProbeAlgorithm { Mutator = mutator };
        var equal = first with { };
        equal.ShouldBe(first);
        var cycle = new CycleAlgorithm<int, ProbeState>([first, first, equal]) { MaximumCycles = 2 }
            .Sharing(first, equal);
        var root = ResolutionScope.Create();
        var observations = new List<string>();
        var originalScope = root.CreateChildScope();
        var original = Resolve(originalScope, cycle.Substitute(mutator.Invocations.Observing(_ => observations.Add("original"))));
        var before = await Run(original);
        before.Select(state => state.Call).ShouldBe([1, 2, 1, 3, 4, 2]);
        before[0].Data.ShouldBeSameAs(before[1].Data);
        before[0].Data.ShouldNotBeSameAs(before[2].Data);

        var rebuiltScope = root.CreateChildScope();
        var rebuilt = Resolve(rebuiltScope, cycle.Substitute(mutator.Invocations.Observing(_ => observations.Add("rebuilt"))));
        var after = await Run(rebuilt);
        after.Select(state => state.Call).ShouldBe([1, 2, 1, 3, 4, 2]);
        after[0].Data.ShouldNotBeSameAs(before[0].Data);
        observations.ShouldBe([.. Enumerable.Repeat("original", 6), .. Enumerable.Repeat("rebuilt", 6)]);

        var otherOwner = await Run(Resolve(root.CreateChildScope(), cycle));
        otherOwner.Select(state => state.Call).ShouldBe([1, 2, 1, 3, 4, 2]);
        otherOwner[0].Data.ShouldNotBeSameAs(before[0].Data);
        var direct = (await Run(Resolve(root, first))).Single();
        direct.Call.ShouldBe(1);
        direct.Data.ShouldNotBeSameAs(before[0].Data);

        await Run(original);
        observations.ShouldBe([.. Enumerable.Repeat("original", 6), .. Enumerable.Repeat("rebuilt", 6), .. Enumerable.Repeat("original", 6)]);
    }

    [Fact]
    public void Cycle_DiscardedExecutorReleasesItsObserver()
    {
        var algorithm = new ProbeAlgorithm { Mutator = new CountingMutations() };
        var cycle = Cycle(algorithm, false);
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
        IAlgorithm<int, ProbeState> cycle, IOperator<Mutation, int> mutator)
    {
        var observer = mutator.Invocations.Observing(_ => { });
        var scope = root.CreateChildScope();
        Run(Resolve(scope, cycle.Substitute(observer))).GetAwaiter().GetResult();
        return new WeakReference(observer);
    }

    [Fact]
    public async Task Pipeline_StagesAreFreshAndDoNotPublishStateToTheirParent()
    {
        var algorithm = new ProbeAlgorithm();
        var pipeline = new PipelineAlgorithm<int, ProbeState>([algorithm, algorithm]);
        var root = ResolutionScope.Create();
        var executor = Resolve(root, pipeline);
        var forks = new List<string>();
        var first = await Run(executor, new RecordingRandom("first", forks.Add));
        var second = await Run(executor, new RecordingRandom("second", forks.Add));
        first.Select(state => state.Value).ShouldBe([1, 2]);
        first.Select(state => state.Call).ShouldBe([1, 1]);
        second.Select(state => state.Call).ShouldBe([1, 1]);
        first[0].Data.ShouldNotBeSameAs(first[1].Data);
        first[0].Data.ShouldNotBeSameAs(second[0].Data);
        forks.ShouldBe(["first/0", "first/1", "second/0", "second/1"]);

        var ancestor = (await Run(Resolve(root, algorithm))).Single();
        ancestor.Call.ShouldBe(1);
        var inherited = await Run(executor);
        inherited.Select(state => state.Call).ShouldBe([1, 1]);
        inherited.ShouldAllBe(state => !ReferenceEquals(state.Data, ancestor.Data));
    }

    [Theory]
    [InlineData(true, true)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public async Task PausedDeferredStream_KeepsItsObservationContextForLaterActivations(bool cycle, bool reset)
    {
        var algorithm = new ProbeAlgorithm();
        IAlgorithm<int, ProbeState> source = cycle ? Cycle(algorithm, reset)
            : new PipelineAlgorithm<int, ProbeState>([algorithm, algorithm]);
        var root = ResolutionScope.Create();
        var original = Resolve(root, source);
        await using var paused = original.RunStreamingAsync(RandomNumberGenerator.Create(42), ct: TestContext.Current.CancellationToken)
            .GetAsyncEnumerator(TestContext.Current.CancellationToken);
        (await paused.MoveNextAsync()).ShouldBeTrue();
        var values = new List<int> { paused.Current.Value };
        var observing = source.Counting(algorithm.Mutator, out var calls);
        var observedCalls = root.Results.Result(calls);
        var child = root.CreateChildScope();

        var observed = await Run(Resolve(child, observing));
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
    public async Task OperatorBudget_PrivateUsageSurvivesPauseButNotResolutionInASiblingScope(bool measureDuration)
    {
        var clock = new ManualClock();
        var algorithm = new ProbeAlgorithm { Mutator = new AdvancingMutator(clock), Steps = 4 };
        IAlgorithm<int, ProbeState> budget = measureDuration
            ? algorithm.LimitedToDuration(algorithm.Mutator, TimeSpan.FromSeconds(3))
            : algorithm.LimitedToInvocations(algorithm.Mutator, 3);
        var root = ResolutionScope.Create(clock);
        var original = Resolve(root.CreateChildScope(), budget);
        await using var paused = original.RunStreamingAsync(RandomNumberGenerator.Create(42), ct: TestContext.Current.CancellationToken)
            .GetAsyncEnumerator(TestContext.Current.CancellationToken);
        (await paused.MoveNextAsync()).ShouldBeTrue();
        var observing = budget.Counting(algorithm.Mutator, out var calls);
        var observedCalls = root.Results.Result(calls);
        // A sibling scope builds its own budget. A child of the original budget's scope would count against that budget too.
        var sibling = root.CreateChildScope();

        var observed = await Run(Resolve(sibling, observing));
        observed.Select(state => state.Call).ShouldBe([1, 2, 3]);
        observedCalls.CurrentCount.ShouldBe(3);
        (await paused.MoveNextAsync()).ShouldBeTrue();
        (await paused.MoveNextAsync()).ShouldBeTrue();
        (await paused.MoveNextAsync()).ShouldBeFalse();
        (await Run(original)).Count.ShouldBe(1);
        observedCalls.CurrentCount.ShouldBe(3);

        var independent = await Run(Resolve(ResolutionScope.Create(clock), budget));
        independent.Select(state => state.Call).ShouldBe([1, 2, 3]);
        independent[0].Data.ShouldNotBeSameAs(observed[0].Data);
        observedCalls.CurrentCount.ShouldBe(3);
    }

    /// <summary>
    /// The budget allows two calls and holds a copy of the algorithm with its wrapper in place. A direct run of the
    /// original algorithm, in the budget's scope or a child scope, uses an executor of its own, and its four calls do
    /// not count against the budget. Building the original first changes nothing either, because the budget does not
    /// change how the original is built.
    /// </summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Budget_CountsOnlyTheCallsOfTheAlgorithmItHolds(bool measureDuration)
    {
        var clock = new ManualClock();
        var algorithm = new ProbeAlgorithm { Mutator = new AdvancingMutator(clock), Steps = 4 };
        IAlgorithm<int, ProbeState> budget = measureDuration
            ? algorithm.LimitedToDuration(algorithm.Mutator, TimeSpan.FromSeconds(2))
            : algorithm.LimitedToInvocations(algorithm.Mutator, 2);
        var root = ResolutionScope.Create(clock);
        var limited = Resolve(root, budget);
        var direct = await Run(Resolve(root, algorithm));
        direct.Select(state => state.Call).ShouldBe([1, 2, 3, 4]);
        var throughBudget = await Run(limited);
        throughBudget.Select(state => state.Call).ShouldBe([1, 2]);
        throughBudget[0].Data.ShouldNotBeSameAs(direct[0].Data);
        (await Run(Resolve(ResolutionScope.Create(clock), budget))).Select(state => state.Call).ShouldBe([1, 2]);

        var parent = ResolutionScope.Create(clock);
        var limitedInParent = Resolve(parent, budget);
        var inChild = await Run(Resolve(parent.CreateChildScope(), algorithm));
        inChild.Select(state => state.Call).ShouldBe([1, 2, 3, 4]);
        var afterChild = await Run(limitedInParent);
        afterChild.Select(state => state.Call).ShouldBe([1, 2]);
        afterChild[0].Data.ShouldNotBeSameAs(inChild[0].Data);

        var builtFirst = ResolutionScope.Create(clock);
        _ = Resolve(builtFirst, algorithm);
        (await Run(Resolve(builtFirst, budget))).Select(state => state.Call).ShouldBe([1, 2]);
    }

    [Fact]
    public async Task NestedCountAndDurationBudgets_KeepBothDeclarationsAcrossRebinding()
    {
        var clock = new ManualClock();
        var algorithm = new ProbeAlgorithm { Mutator = new AdvancingMutator(clock), Steps = 4 };
        var durationBudget = algorithm.LimitedToDuration(algorithm.Mutator, TimeSpan.FromSeconds(3));
        var budget = durationBudget.LimitedToInvocations(algorithm.Mutator, 2);
        var root = ResolutionScope.Create(clock);
        var first = await Run(Resolve(root, budget));
        first.Select(state => state.Call).ShouldBe([1, 2]);
        var observing = budget.Counting(algorithm.Mutator, out var calls);
        var observedCalls = root.Results.Result(calls);
        var child = root.CreateChildScope();

        var observed = await Run(Resolve(child, observing));
        observed.Select(state => state.Call).ShouldBe([1, 2]);
        observed[0].Data.ShouldNotBeSameAs(first[0].Data);
        observedCalls.CurrentCount.ShouldBe(2);
        (await Run(Resolve(ResolutionScope.Create(clock), budget))).Count.ShouldBe(2);
        observedCalls.CurrentCount.ShouldBe(2);
    }

    /// <summary>
    /// The trace advances the clock by 10 seconds per call. The budgeted runs still reaching their third call show that
    /// the budget, placed after the analysis, does not measure the observation.
    /// </summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task OperatorBudget_RebindingKeepsClockBeforeTraceAndExcludesObservationDuration(bool measureDuration)
    {
        var clock = new ManualClock();
        var algorithm = new ProbeAlgorithm { Mutator = new AdvancingMutator(clock), Steps = 4 };
        var calls = new MutationCallClock(algorithm.Mutator);
        var trace = algorithm.Mutator.Invocations.Trace(observation =>
        {
            clock.Advance(10);
            return observation.Offspring[0];
        }, clocks: [calls]);
        var analyzed = ((IAlgorithm<int, ProbeState>)algorithm).Substitute(trace, calls);
        IAlgorithm<int, ProbeState> budget = measureDuration
            ? analyzed.LimitedToDuration(algorithm.Mutator, TimeSpan.FromSeconds(3))
            : analyzed.LimitedToInvocations(algorithm.Mutator, 3);
        var root = ResolutionScope.Create(clock);
        await using var paused = Resolve(root.CreateChildScope(), budget).RunStreamingAsync(RandomNumberGenerator.Create(42), ct: TestContext.Current.CancellationToken)
            .GetAsyncEnumerator(TestContext.Current.CancellationToken);
        (await paused.MoveNextAsync()).ShouldBeTrue();
        var observed = 0;
        var sibling = root.CreateChildScope();

        (await Run(Resolve(sibling, budget.Substitute(algorithm.Mutator.Invocations.Observing(_ => observed++))))).Count.ShouldBe(3);
        (await paused.MoveNextAsync()).ShouldBeTrue();
        (await paused.MoveNextAsync()).ShouldBeTrue();
        (await paused.MoveNextAsync()).ShouldBeFalse();

        root.Results.Result(trace).By(calls).Select(point => point.Time).ShouldBe([1L, 2L, 3L, 4L, 5L, 6L]);
        root.Results.Result(trace).By(calls).Select(point => point.Value).ShouldBe([1, 1, 2, 3, 2, 3]);
        observed.ShouldBe(3);
    }

    [Fact]
    public async Task StateTermination_EachScopeHasItsOwnLimitAndContextualChecks()
    {
        var algorithm = new ProbeAlgorithm { Steps = 4 };
        IOperator<Termination, int> terminator = new AfterIterationsTerminator<int>(2);
        var source = algorithm.TerminatedBy(terminator);
        var root = ResolutionScope.Create();
        (await Run(Resolve(root, source))).Count.ShouldBe(2);
        var observing = source.Counting(terminator, out var calls);
        var observedCalls = root.Results.Result(calls);
        var child = root.CreateChildScope();

        (await Run(Resolve(child, observing))).Count.ShouldBe(2);
        observedCalls.CurrentCount.ShouldBe(2);
        (await Run(Resolve(root, source))).Count.ShouldBe(1); // the root's terminator continues its count
        observedCalls.CurrentCount.ShouldBe(2);
        (await Run(Resolve(ResolutionScope.Create(), source))).Count.ShouldBe(2);
    }

    [Fact]
    public async Task AlgorithmDuration_PausedInvocationKeepsItsOwnDurationAcrossRebinding()
    {
        var clock = new ManualClock();
        var algorithm = new ProbeAlgorithm { Mutator = new AdvancingMutator(clock), Steps = 4 };
        var budget = algorithm.LimitedToDuration(TimeSpan.FromSeconds(2));
        var root = ResolutionScope.Create(clock);
        var original = Resolve(root, budget);
        await using var paused = original.RunStreamingAsync(RandomNumberGenerator.Create(42), ct: TestContext.Current.CancellationToken)
            .GetAsyncEnumerator(TestContext.Current.CancellationToken);
        (await paused.MoveNextAsync()).ShouldBeTrue();
        paused.Current.Value.ShouldBe(1);
        var observing = budget.Counting(algorithm.Mutator, out var calls);
        var observedCalls = root.Results.Result(calls);
        var child = root.CreateChildScope();

        (await Run(Resolve(child, observing))).Count.ShouldBe(2);
        observedCalls.CurrentCount.ShouldBe(2);
        (await paused.MoveNextAsync()).ShouldBeTrue();
        paused.Current.Value.ShouldBe(2);
        (await paused.MoveNextAsync()).ShouldBeFalse();
        observedCalls.CurrentCount.ShouldBe(2);
        (await Run(original)).Count.ShouldBe(2);
        observedCalls.CurrentCount.ShouldBe(2);
    }

    private static IAlgorithm<int, ProbeState> Cycle(ProbeAlgorithm algorithm, bool reset)
    {
        var cycle = new CycleAlgorithm<int, ProbeState>([algorithm, algorithm]) { MaximumCycles = 2 };
        return reset ? cycle : cycle.Sharing(algorithm);
    }

    private static IAlgorithmExecutor<ProbeState> Resolve(ResolutionScope scope, IAlgorithm<int, ProbeState> algorithm) =>
        scope.Resolve<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>, ProbeState>(algorithm, problem);

    private static async Task<List<ProbeState>> Run(IAlgorithmExecutor<ProbeState> executor, IRandomNumberGenerator? random = null)
    {
        var states = new List<ProbeState>();
        await foreach (var state in executor.RunStreamingAsync(random ?? RandomNumberGenerator.Create(42), ct: TestContext.Current.CancellationToken))
            states.Add(state);
        return states;
    }

    private sealed record ProbeState(int Value, int Call, ProbeData Data) : ISearchState;

    private sealed class ProbeData
    {
        public int Invocations;
        public int Starts;
        public int Disposals;
    }

    private sealed record ProbeAlgorithm : Algorithm<ProbeAlgorithm, int, ProbeState>
    {
        public IOperator<Mutation, int> Mutator { get; init; } = new PassThroughMutator();
        public int Steps { get; init; } = 1;
        public Action<ProbeData> Prepared { get; init; } = static _ => { };
        public Action<ResolutionScope> Building { get; init; } = static _ => { };

        public static Parameter<ProbeAlgorithm, IOperator<Mutation, int>> MutatorParameter { get; } =
            new(nameof(Mutator), static algorithm => algorithm.Mutator, static (algorithm, value) => algorithm with { Mutator = value });

        public override IReadOnlyList<IParameter> GetParameters() => [.. base.GetParameters(), MutatorParameter];

        protected override IAlgorithmExecutor<ProbeState> CreateExecutor<TSearchSpace, TProblem>(
            ResolutionScope<int, TSearchSpace, TProblem, ProbeState> scope)
        {
            Building(scope);
            var data = new ProbeData();
            Prepared(data);
            return new Executor(data, scope.Resolve(Mutator), Steps);
        }

        private sealed class Executor(ProbeData data, IMutationExecutor<int> mutator, int steps)
            : AlgorithmExecutor<ProbeState>
        {
            public override async IAsyncEnumerable<ProbeState> RunStreamingAsync(IRandomNumberGenerator random, ProbeState? initialState = null, [EnumeratorCancellation] CancellationToken ct = default)
            {
                data.Starts++;
                var value = initialState?.Value ?? 0;
                try
                {
                    for (var index = 0; index < steps; index++)
                    {
                        ct.ThrowIfCancellationRequested();
                        value = mutator.Mutate([value + 1], random).Single();
                        yield return new ProbeState(value, ++data.Invocations, data);
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

    private sealed class MutationCallClock(IOperator<Mutation, int> mutator) : SubstitutionGroup, IClock<long>, IObservationReceiver<MutationObservation<int>>
    {
        protected override IReadOnlyList<ISubstitution> CreateSubstitutions() => [mutator.Invocations.RecordingInto(this)];

        private readonly Counter counter = new();

        public Action<MutationObservation<int>> CreateRecorder(RunResults results)
        {
            var count = results.Result(counter);
            return _ => count.IncrementBy(1);
        }

        public IClockReading<long> CreateExecutionNode(ResolutionScope scope) => new CallCount(scope.Results.Result(counter));
    }

    private sealed record CountingMutations : Mutator<int>
    {

        protected override IMutationExecutor<int> CreateExecutor<TRunSearchState>(ResolutionScope<int, ISearchSpace<int>, IProblem<int, ISearchSpace<int>>, TRunSearchState> scope) =>
        new Executor();

        private sealed class Executor
            : MutationExecutor<int>
        {
            private int invocations;

            public override IReadOnlyList<int> Mutate(IReadOnlyList<int> parents, IRandomNumberGenerator random) => [++invocations];
        }
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

    private sealed record RecordingRandom(string Path, Action<string> RecordFork) : IRandomNumberGenerator
    {
        public double NextDouble() => throw new InvalidOperationException("No random draws are expected.");
        public int NextInt() => throw new InvalidOperationException("No random draws are expected.");
        public IRandomNumberGenerator Fork(ulong forkKey)
        {
            var childPath = $"{Path}/{forkKey}";
            RecordFork(childPath);
            return new RecordingRandom(childPath, RecordFork);
        }
    }
}
