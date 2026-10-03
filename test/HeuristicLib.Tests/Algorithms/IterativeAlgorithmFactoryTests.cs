using HEAL.HeuristicLib.Operators.Interceptors;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.Tests.TestSupport.Mocks;

namespace HEAL.HeuristicLib.Tests.Algorithms;

public sealed class IterativeAlgorithmFactoryTests
{
    private static readonly FuncProblem<int, DummySearchSpace<int>> Problem =
        FuncProblem.Create(static (int candidate) => candidate, DummySearchSpace<int>.Instance, SingleObjective.Minimize);

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Preparation_PrecedesInterceptorResolutionAndRunsOncePerFactory(bool bound)
    {
        var events = new List<string>();
        var interceptor = new AdvancingInterceptor(() => events.Add("prepare interceptor"));
        var source = CreateAlgorithm(bound, () => events.Add("prepare algorithm"), () => events.Add("bind algorithm"), interceptor);

        var factory = source.CreateExecutionFactory<DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>();
        events.ShouldBe(["prepare algorithm"]);

        var parent = ResolutionScope.Create();
        var first = factory(parent);
        var second = factory(parent.CreateChildScope());
        first.ShouldNotBeSameAs(second);
        events.ShouldBe(["prepare algorithm", "prepare interceptor", "bind algorithm", "bind algorithm"]);
        (await Next(first)).Value.ShouldBe(101);
        (await Next(second)).Value.ShouldBe(202);

        var independentFactory = source.CreateExecutionFactory<DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>();
        (await Next(independentFactory(ResolutionScope.Create()))).Value.ShouldBe(101);
        events.Count(value => value == "prepare algorithm").ShouldBe(2);
        events.Count(value => value == "prepare interceptor").ShouldBe(2);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DescendantObservation_RebindsTheInterceptorAndPreservesBothStates(bool bound)
    {
        var preparations = 0;
        var interceptorPreparations = 0;
        var observedCalls = new CountAccumulator();
        var interceptor = new AdvancingInterceptor(() => interceptorPreparations++);
        var source = CreateAlgorithm(bound, () => preparations++, interceptor: interceptor);
        var parent = ResolutionScope.Create();
        var outer = Resolve(parent, source);
        (await Next(outer)).Value.ShouldBe(101);

        var child = parent.CreateChildScope(builder => builder.Wrap<IInterceptor<int>>(interceptor, original => original.CountCalls(observedCalls)));
        var inner = Resolve(child, source);
        inner.ShouldNotBeSameAs(outer);
        (await Next(inner)).Value.ShouldBe(202);
        (await Next(outer)).Value.ShouldBe(303);
        (await Next(inner)).Value.ShouldBe(404);
        observedCalls.CurrentCount.ShouldBe(2);
        preparations.ShouldBe(1);
        interceptorPreparations.ShouldBe(1);

        (await Next(Resolve(ResolutionScope.Create(), source))).Value.ShouldBe(101);
        observedCalls.CurrentCount.ShouldBe(2);
        preparations.ShouldBe(2);
        interceptorPreparations.ShouldBe(2);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task PausedIterator_KeepsItsInterceptorPreviousStateAndRandomForkSequence(bool bound)
    {
        var observedCalls = new CountAccumulator();
        var interceptor = new AdvancingInterceptor(static () => { });
        var source = CreateAlgorithm(bound, static () => { }, interceptor: interceptor);
        var parent = ResolutionScope.Create();
        var outer = Resolve(parent, source);
        var outerRandom = new RecordingRandom();
        await using var paused = outer.RunStreamingAsync(Problem, outerRandom, ct: TestContext.Current.CancellationToken).GetAsyncEnumerator();
        (await paused.MoveNextAsync()).ShouldBeTrue();
        paused.Current.Value.ShouldBe(101);
        paused.Current.PreviousValue.ShouldBeNull();

        var child = parent.CreateChildScope(builder => builder.Wrap<IInterceptor<int>>(interceptor, original => original.CountCalls(observedCalls)));
        var inner = Resolve(child, source);
        var innerRandom = new RecordingRandom();
        await using var observed = inner.RunStreamingAsync(Problem, innerRandom, ct: TestContext.Current.CancellationToken).GetAsyncEnumerator();
        (await observed.MoveNextAsync()).ShouldBeTrue();
        observed.Current.Value.ShouldBe(202);

        (await paused.MoveNextAsync()).ShouldBeTrue();
        paused.Current.Value.ShouldBe(303);
        paused.Current.PreviousValue.ShouldBe(101);
        observedCalls.CurrentCount.ShouldBe(1);
        outerRandom.ForkKeys.ShouldBe([0UL, 1UL]);

        (await observed.MoveNextAsync()).ShouldBeTrue();
        observed.Current.Value.ShouldBe(404);
        observed.Current.PreviousValue.ShouldBe(202);
        observedCalls.CurrentCount.ShouldBe(2);
        innerRandom.ForkKeys.ShouldBe([0UL, 1UL]);
        (await paused.MoveNextAsync()).ShouldBeFalse();
        (await observed.MoveNextAsync()).ShouldBeFalse();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Binding_AllowsAnAbsentInterceptor(bool bound)
    {
        var source = CreateAlgorithm(bound, static () => { });

        (await Next(Resolve(ResolutionScope.Create(), source))).Value.ShouldBe(1);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void PreparationFailure_DoesNotPrepareTheInterceptorOrBindAnExecution(bool bound)
    {
        var failure = new InvalidOperationException("preparation failed");
        var interceptorPreparations = 0;
        var bindings = 0;
        var interceptor = new AdvancingInterceptor(() => interceptorPreparations++);
        var source = CreateAlgorithm(bound, () => throw failure, () => bindings++, interceptor);

        Should.Throw<InvalidOperationException>(() => source.CreateExecutionFactory<DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>()).ShouldBeSameAs(failure);

        interceptorPreparations.ShouldBe(0);
        bindings.ShouldBe(0);
    }

    [Fact]
    public void BoundFactory_RejectsAnIncompatibleSearchSpaceBeforePreparation()
    {
        var preparations = 0;
        var source = new BoundAlgorithm(() => preparations++, static () => { });

        Should.Throw<InvalidOperationException>(() => source.CreateExecutionFactory<ISearchSpace<int>, IProblem<int, ISearchSpace<int>>>());

        preparations.ShouldBe(0);
    }

    [Fact]
    public void BoundFactory_RejectsAnIncompatibleProblemBeforePreparation()
    {
        var preparations = 0;
        var source = new BoundAlgorithm(() => preparations++, static () => { });

        Should.Throw<InvalidOperationException>(() => source.CreateExecutionFactory<DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>>());

        preparations.ShouldBe(0);
    }

    [Fact]
    public void Factory_RejectsAnIncompatibleSearchStateBeforePreparation()
    {
        var preparations = 0;
        IAlgorithm<int> source = new BoundAlgorithm(() => preparations++, static () => { });

        Should.Throw<InvalidOperationException>(() => source.CreateExecutionFactory<DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>, OtherState>());

        preparations.ShouldBe(0);
    }

    [Fact]
    public async Task BoundFactory_AcceptsASupportedConcreteProblemAndPreservesItsPreparedState()
    {
        var preparations = 0;
        var source = new WideProblemAlgorithm(() => preparations++);
        var factory = source.CreateExecutionFactory<DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>();

        (await Next(factory(ResolutionScope.Create()))).Value.ShouldBe(1);
        (await Next(factory(ResolutionScope.Create()))).Value.ShouldBe(2);
        preparations.ShouldBe(1);
    }

    private static IAlgorithm<int, ProbeState> CreateAlgorithm(bool bound, Action prepare, Action? bind = null, IInterceptor<int>? interceptor = null) =>
        bound
            ? new BoundAlgorithm(prepare, bind ?? (static () => { })) { Interceptor = interceptor }
            : new AgnosticAlgorithm(prepare, bind ?? (static () => { })) { Interceptor = interceptor };

    private static IAlgorithmExecution<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>, ProbeState> Resolve(ResolutionScope scope, IAlgorithm<int, ProbeState> source) =>
        scope.Resolve<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>, ProbeState>(source);

    private static async Task<ProbeState> Next(IAlgorithmExecution<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>, ProbeState> execution)
    {
        await using var iterator = execution.RunStreamingAsync(Problem, RandomNumberGenerator.Create(1), ct: TestContext.Current.CancellationToken).GetAsyncEnumerator();
        (await iterator.MoveNextAsync()).ShouldBeTrue();
        return iterator.Current;
    }

    private sealed record ProbeState(int Value, int? PreviousValue = null) : ISearchState;

    private sealed record OtherState : ISearchState;

    private sealed class CounterState
    {
        public int Calls { get; set; }
    }

    private sealed record AgnosticAlgorithm(Action Prepare, Action Bind) : IterativeAlgorithm<AgnosticAlgorithm, int, ProbeState>
    {
        protected override ExecutionFactory<IterativeAlgorithmExecution<int, TRunSearchSpace, TRunProblem, ProbeState>> CreateIterationFactory<TRunSearchSpace, TRunProblem>()
        {
            Prepare();
            var state = new CounterState();
            return scope =>
            {
                var interceptor = scope.ResolveOptional<int, TRunSearchSpace, TRunProblem, ProbeState>(Interceptor);
                Bind();
                return new CounterExecution<TRunSearchSpace, TRunProblem>(interceptor, state);
            };
        }
    }

    private sealed record BoundAlgorithm(Action Prepare, Action Bind) : IterativeAlgorithm<BoundAlgorithm, int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>, ProbeState>
    {
        protected override ExecutionFactory<IterativeAlgorithmExecution<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>, ProbeState>> CreateIterationFactory()
        {
            Prepare();
            var state = new CounterState();
            return scope =>
            {
                var interceptor = scope.ResolveOptional<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>, ProbeState>(Interceptor);
                Bind();
                return new CounterExecution<DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>(interceptor, state);
            };
        }
    }

    private sealed record WideProblemAlgorithm(Action Prepare) : IterativeAlgorithm<WideProblemAlgorithm, int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>, ProbeState>
    {
        protected override ExecutionFactory<IterativeAlgorithmExecution<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>, ProbeState>> CreateIterationFactory()
        {
            Prepare();
            var state = new CounterState();
            return scope => new CounterExecution<DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>>(scope.ResolveOptional<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>, ProbeState>(Interceptor), state);
        }
    }

    private sealed class CounterExecution<TSearchSpace, TProblem>(IInterceptorExecution<int, TSearchSpace, TProblem, ProbeState>? interceptor, CounterState state)
        : IterativeAlgorithmExecution<int, TSearchSpace, TProblem, ProbeState>(interceptor)
        where TSearchSpace : class, ISearchSpace<int>
        where TProblem : class, IProblem<int, TSearchSpace>
    {
        protected override ProbeState ExecuteStep(ProbeState? previousState, TProblem problem, IRandomNumberGenerator random) =>
            new(++state.Calls, previousState?.Value);

        protected override bool HasCompleted(int yieldedStateCount, ProbeState? previousState, TProblem problem) => yieldedStateCount == 2;
    }

    private sealed record AdvancingInterceptor(Action Prepare) : StatefulInterceptor<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>, ProbeState, CounterState>
    {
        protected override CounterState CreateInitialState()
        {
            Prepare();
            return new CounterState();
        }

        protected override ProbeState Transform(ProbeState currentState, ProbeState? previousState, CounterState state, IRandomNumberGenerator random, DummySearchSpace<int> searchSpace, FuncProblem<int, DummySearchSpace<int>> problem) =>
            currentState with { Value = currentState.Value + 100 * ++state.Calls };
    }

    private sealed class RecordingRandom : IRandomNumberGenerator
    {
        public List<ulong> ForkKeys { get; } = [];

        public double NextDouble() => 0;

        public int NextInt() => 0;

        public IRandomNumberGenerator Fork(ulong forkKey)
        {
            ForkKeys.Add(forkKey);
            return this;
        }
    }
}
