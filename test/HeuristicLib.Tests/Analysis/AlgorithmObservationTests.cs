using HEAL.HeuristicLib.Analysis;
using HEAL.HeuristicLib.Encodings.RealVectors;
using HEAL.HeuristicLib.Inspection;
using HEAL.HeuristicLib.Operators.Interceptors;
using HEAL.HeuristicLib.Problems.TestFunctions;
using UniformDistributedCreator = HEAL.HeuristicLib.Encodings.RealVectors.UniformDistributedCreator;

namespace HEAL.HeuristicLib.Tests.Analysis;

public class AlgorithmObservationTests
{
    private const int PopulationSize = 16;

    [Fact]
    public async Task AlgorithmObservation_ObservesOncePerYieldedIteration()
    {
        var problem = CreateProblem();
        var algorithm = CreateAlgorithm(problem, maximumGenerations: 4);
        var observed = algorithm.Iterations.Trace(observation => observation.State.Population.EvaluatedCandidates.Count);

        var run = algorithm.CreateRun(problem, RandomNumberGenerator.Create(seed: 42), [observed]);
        await run.CompleteAsync(cancellationToken: TestContext.Current.CancellationToken);

        run.Result(observed).SampleCount.ShouldBe(4);
        run.Result(observed).Snapshot().ShouldAllBe(sample => sample.Value == PopulationSize);
    }

    [Fact]
    public async Task AlgorithmObservation_ObservesStateAfterInterception()
    {
        var problem = CreateProblem();
        var algorithm = CreateAlgorithm(problem, maximumGenerations: 4) with
        {
            Interceptor = new TruncatingInterceptor(KeptCandidates: 5)
        };
        var observed = algorithm.Iterations.Trace(observation => observation.State.Population.EvaluatedCandidates.Count);

        var run = algorithm.CreateRun(problem, RandomNumberGenerator.Create(seed: 42), [observed]);
        await run.CompleteAsync(cancellationToken: TestContext.Current.CancellationToken);

        run.Result(observed).SampleCount.ShouldBe(4);
        run.Result(observed).Snapshot().ShouldAllBe(sample => sample.Value == 5);
    }

    [Fact]
    public async Task AlgorithmObservation_CarriesThePreviousStateAndTheProblem()
    {
        var problem = CreateProblem();
        var algorithm = CreateAlgorithm(problem, maximumGenerations: 3);
        var previousStates = new List<PopulationState<RealVector>?>();
        var observed = algorithm.Iterations.Trace(
            observation =>
            {
                previousStates.Add(observation.PreviousState);
                observation.SearchSpace.ShouldBeSameAs(problem.SearchSpace);
                observation.Problem.ShouldBeSameAs(problem);
                return observation.Iteration;
            });

        var run = algorithm.CreateRun(problem, RandomNumberGenerator.Create(seed: 42), [observed]);
        await run.CompleteAsync(cancellationToken: TestContext.Current.CancellationToken);

        previousStates.Count.ShouldBe(3);
        previousStates[0].ShouldBeNull();
        previousStates[1].ShouldNotBeNull();
        previousStates[2].ShouldNotBeNull();
    }

    [Fact]
    public async Task AlgorithmAnalyzer_RecordsOneEntryPerGeneration()
    {
        var problem = CreateProblem();
        var algorithm = CreateAlgorithm(problem, maximumGenerations: 4);
        var analysis = algorithm.Iterations.TracePopulationCandidates();

        var run = algorithm.CreateRun(problem, RandomNumberGenerator.Create(seed: 42), [analysis]);
        await run.CompleteAsync(cancellationToken: TestContext.Current.CancellationToken);

        run.Result(analysis).SampleCount.ShouldBe(4);
    }

    [Fact]
    public async Task AlgorithmAnalyzers_ShareOneSourceWithoutConflict()
    {
        var problem = CreateProblem();
        var algorithm = CreateAlgorithm(problem, maximumGenerations: 3);
        var first = algorithm.Iterations.TracePopulationCandidates();
        var second = algorithm.Iterations.TracePopulationCandidates();

        var run = algorithm.CreateRun(problem, RandomNumberGenerator.Create(seed: 42), [first, second]);
        await run.CompleteAsync(cancellationToken: TestContext.Current.CancellationToken);

        run.Result(first).SampleCount.ShouldBe(3);
        run.Result(second).SampleCount.ShouldBe(3);
    }

    [Fact]
    public async Task AlgorithmAndInterceptorSources_CanBeUsedInTheSameRun()
    {
        var problem = CreateProblem();
        var interceptor = new IdentityInterceptor<RealVector, PopulationState<RealVector>>();
        var algorithm = CreateAlgorithm(problem, maximumGenerations: 3) with { Interceptor = interceptor };
        var atIterationEnd = algorithm.Iterations.TracePopulationCandidates();
        var atInterceptor = interceptor.Invocations.Trace(
            static observation => observation.State.Population.EvaluatedCandidates.BestMedianWorst(observation.Problem.Objective));

        var run = algorithm.CreateRun(problem, RandomNumberGenerator.Create(seed: 42), [atIterationEnd, atInterceptor]);
        await run.CompleteAsync(cancellationToken: TestContext.Current.CancellationToken);

        run.Result(atIterationEnd).SampleCount.ShouldBe(3);
        run.Result(atInterceptor).SampleCount.ShouldBe(3);
    }

    /// <summary>
    /// The analyzer observes the exact algorithm configuration supplied as its source, so a copy is not it.
    /// </summary>
    [Fact]
    public void AlgorithmObservation_UsesTheConfiguredSourceInstance()
    {
        var problem = CreateProblem();
        var algorithm = CreateAlgorithm(problem, maximumGenerations: 3);
        var analysis = algorithm.Iterations.TracePopulationCandidates();

        var copy = algorithm with { PopulationSize = PopulationSize * 2 };
        Should.Throw<InvalidOperationException>(() => copy.Substitute(analysis)).Message.ShouldContain("not reachable");
    }

    [Fact]
    public async Task TraceAnalyzer_IsReadableWhileTheRunStreams()
    {
        var problem = CreateProblem();
        var algorithm = CreateAlgorithm(problem, maximumGenerations: 4);
        var iterations = new Counter();
        var quality = algorithm.Iterations.TracePopulationCandidates([iterations.Clock]);
        var run = algorithm.CreateRun(problem, RandomNumberGenerator.Create(seed: 42), [quality, algorithm.Iterations.Counting(iterations)]);
        var observedIterations = 0;

        await foreach (var state in run.Stream(cancellationToken: TestContext.Current.CancellationToken))
        {
            observedIterations++;
            run.Result(quality).SampleCount.ShouldBe(observedIterations);
            var latest = run.Result(quality).Latest;
            latest.ShouldNotBeNull();
            latest.Value.At(iterations.Clock).ShouldBe(observedIterations);
            state.Population.EvaluatedCandidates.ShouldContain(latest.Value.Value.Best);
        }

    }

    [Fact]
    public async Task TraceAnalyzer_SnapshotRemainsStableWhileTheRunContinues()
    {
        var problem = CreateProblem();
        var algorithm = CreateAlgorithm(problem, maximumGenerations: 3);
        var iterations = new Counter();
        var quality = algorithm.Iterations.Trace(
            static observation => observation.State.Population.EvaluatedCandidates.Select(candidate => candidate.ObjectiveVector).BestMedianWorst(observation.Problem.Objective),
            [iterations.Clock]);
        var run = algorithm.CreateRun(problem, RandomNumberGenerator.Create(seed: 42), [quality, algorithm.Iterations.Counting(iterations)]);

        await using var enumerator = run.Stream(cancellationToken: TestContext.Current.CancellationToken)
                                        .GetAsyncEnumerator(TestContext.Current.CancellationToken);

        (await enumerator.MoveNextAsync()).ShouldBeTrue();
        var firstIteration = run.Result(quality).Snapshot();

        (await enumerator.MoveNextAsync()).ShouldBeTrue();

        firstIteration.Count.ShouldBe(1);
        firstIteration[0].At(iterations.Clock).ShouldBe(1);
        run.Result(quality).SampleCount.ShouldBe(2);
        run.Result(quality).Snapshot().Count.ShouldBe(2);
    }

    [Fact]
    public async Task TraceAnalyzer_ByIterationPublishesAStableProjection()
    {
        var problem = CreateProblem();
        var algorithm = CreateAlgorithm(problem, maximumGenerations: 3);
        var iterations = new Counter();
        var quality = algorithm.Iterations.TracePopulationCandidates([iterations.Clock]);
        var run = algorithm.CreateRun(problem, RandomNumberGenerator.Create(seed: 42), [quality, algorithm.Iterations.Counting(iterations)]);

        await run.CompleteAsync(cancellationToken: TestContext.Current.CancellationToken);

        var byIteration = run.Result(quality).By(iterations.Clock);
        byIteration.Select(point => point.Time).ShouldBe([1L, 2L, 3L]);
    }

    [Fact]
    public async Task TraceAnalyzer_CombinesContextualRetainedAndPulledCoordinates()
    {
        var problem = CreateProblem();
        var algorithm = CreateAlgorithm(problem, maximumGenerations: 3);
        var iterations = new Counter();
        var evaluations = new Counter();
        var elapsed = Clock.Elapsed;
        var quality = algorithm.Iterations.TracePopulationCandidates([iterations.Clock, evaluations.Clock, elapsed]);
        var run = algorithm.CreateRun(problem, RandomNumberGenerator.Create(seed: 42), [quality, algorithm.Iterations.Counting(iterations), algorithm.Evaluator.Invocations.CountingCandidates(evaluations)]);

        await run.CompleteAsync(cancellationToken: TestContext.Current.CancellationToken);

        run.Result(quality).By(iterations.Clock).Select(point => point.Time).ShouldBe([1L, 2L, 3L]);

        var evaluationCounts = run.Result(quality).By(evaluations.Clock).Select(point => point.Time).ToArray();
        evaluationCounts.ShouldAllBe(count => count > 0);
        evaluationCounts.ShouldBe(evaluationCounts.Order());

        var elapsedTimes = run.Result(quality).By(elapsed).Select(point => point.Time).ToArray();
        elapsedTimes.ShouldAllBe(time => time >= TimeSpan.Zero);
        elapsedTimes.ShouldBe(elapsedTimes.Order());
    }

    [Fact]
    public void TracePopulationCandidates_AtOneSource_StaysTwoIndependentAnalyzers()
    {
        var algorithm = CreateAlgorithm(CreateProblem(), maximumGenerations: 3);

        var first = algorithm.Iterations.TracePopulationCandidates();
        var second = algorithm.Iterations.TracePopulationCandidates();

        // Each analyzer has its own result in a run, so two of them are never interchangeable.
        var results = ResolutionScope.Create().Results;
        first.ShouldNotBeSameAs(second);
        results.Result(first).ShouldNotBeSameAs(results.Result(second));
        results.Result(first).SampleCount.ShouldBe(0);
        results.Result(second).SampleCount.ShouldBe(0);
    }

    /// <summary>
    /// The clock keeps its own count, so it continues across every turn of the observed algorithm, while each turn
    /// restarts the algorithm's iteration count, whether Cycle constructs or shares its executor.
    /// </summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]

    public async Task IterationClock_ContinuesAcrossTheCyclesTurnsWhileEachTurnRestartsTheIteration(bool shared)
    {
        var problem = CreateProblem();
        var inner = CreateAlgorithm(problem, maximumGenerations: 3);
        var fresh = CycleAlgorithm.Create(inner) with { MaximumCycles = 2 };
        IAlgorithm<RealVector, PopulationState<RealVector>> cycle = shared ? fresh.Sharing(inner) : fresh;

        var iterations = new Counter();
        var iteration = inner.Iterations.Trace(observation => observation.Iteration, [iterations.Clock]);
        var run = cycle.CreateRun(problem, RandomNumberGenerator.Create(seed: 42), [iteration, inner.Iterations.Counting(iterations)]);

        await run.CompleteAsync(cancellationToken: TestContext.Current.CancellationToken);

        run.Result(iteration).By(iterations.Clock).ShouldBe([new(1, 1), new(2, 2), new(3, 3), new(4, 1), new(5, 2), new(6, 3)]);
    }

    private static TestFunctionProblem CreateProblem() => new(new RastriginFunction(dimension: 4));

    private static GeneticAlgorithm<RealVector> CreateAlgorithm(
        TestFunctionProblem problem, int maximumGenerations) =>
        new()
        {
            PopulationSize = PopulationSize,
            MaximumGenerations = maximumGenerations,
            Creator = new UniformDistributedCreator(problem.SearchSpace),
            Crossover = new AlphaBetaBlendCrossover { Alpha = 0.7 },
            Mutator = new GaussianMutator(mutationRate: 0.2, mutationStrength: 0.15),
            Selector = TournamentSelector.For(problem, tournamentSize: 2),
            MutationRate = 0.2,
            Elites = 1
        };

    private sealed record TruncatingInterceptor(int KeptCandidates)
        : StatelessInterceptor<RealVector, PopulationState<RealVector>>
    {
        public override PopulationState<RealVector> Transform(PopulationState<RealVector> currentState,
                                                              PopulationState<RealVector>? previousState,
                                                              IRandomNumberGenerator random) =>
            currentState with { Population = Population.From(currentState.Population.EvaluatedCandidates.Take(KeptCandidates)) };
    }
}
