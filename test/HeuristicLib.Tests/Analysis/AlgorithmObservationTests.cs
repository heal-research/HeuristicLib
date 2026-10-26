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
        var observed = Analyzer.Trace(algorithm.Iterations, observation => observation.State.Population.EvaluatedCandidates.Count);

        await algorithm.CreateRun(problem, RandomNumberGenerator.Create(seed: 42)).AddAspect(observed)
                       .CompleteAsync(cancellationToken: TestContext.Current.CancellationToken);

        observed.SampleCount.ShouldBe(4);
        observed.Snapshot().ShouldAllBe(sample => sample.Value == PopulationSize);
    }

    [Fact]
    public async Task AlgorithmObservation_ObservesStateAfterInterception()
    {
        var problem = CreateProblem();
        var algorithm = CreateAlgorithm(problem, maximumGenerations: 4) with
        {
            Interceptor = new TruncatingInterceptor(KeptCandidates: 5)
        };
        var observed = Analyzer.Trace(algorithm.Iterations, observation => observation.State.Population.EvaluatedCandidates.Count);

        await algorithm.CreateRun(problem, RandomNumberGenerator.Create(seed: 42)).AddAspect(observed)
                       .CompleteAsync(cancellationToken: TestContext.Current.CancellationToken);

        observed.SampleCount.ShouldBe(4);
        observed.Snapshot().ShouldAllBe(sample => sample.Value == 5);
    }

    [Fact]
    public async Task AlgorithmObservation_CarriesThePreviousStateAndTheProblem()
    {
        var problem = CreateProblem();
        var algorithm = CreateAlgorithm(problem, maximumGenerations: 3);
        var previousStates = new List<PopulationState<RealVector>?>();
        var observed = Analyzer.Trace(algorithm.Iterations,
            observation =>
            {
                previousStates.Add(observation.PreviousState);
                observation.SearchSpace.ShouldBeSameAs(problem.SearchSpace);
                observation.Problem.ShouldBeSameAs(problem);
                return observation.Iteration;
            });

        await algorithm.CreateRun(problem, RandomNumberGenerator.Create(seed: 42)).AddAspect(observed)
                       .CompleteAsync(cancellationToken: TestContext.Current.CancellationToken);

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

        var run = algorithm.CreateRun(problem, RandomNumberGenerator.Create(seed: 42)).AddAspect(analysis);
        await run.CompleteAsync(cancellationToken: TestContext.Current.CancellationToken);

        analysis.SampleCount.ShouldBe(4);
    }

    [Fact]
    public async Task AlgorithmAnalyzers_ShareOneSourceWithoutConflict()
    {
        var problem = CreateProblem();
        var algorithm = CreateAlgorithm(problem, maximumGenerations: 3);
        var first = algorithm.Iterations.TracePopulationCandidates();
        var second = algorithm.Iterations.TracePopulationCandidates();

        var run = algorithm.CreateRun(problem, RandomNumberGenerator.Create(seed: 42)).AddAspect(first).AddAspect(second);
        await run.CompleteAsync(cancellationToken: TestContext.Current.CancellationToken);

        first.SampleCount.ShouldBe(3);
        second.SampleCount.ShouldBe(3);
    }

    [Fact]
    public async Task AlgorithmAndInterceptorSources_CanBeUsedInTheSameRun()
    {
        var problem = CreateProblem();
        var interceptor = new IdentityInterceptor<RealVector, PopulationState<RealVector>>();
        var algorithm = CreateAlgorithm(problem, maximumGenerations: 3) with { Interceptor = interceptor };
        var atIterationEnd = algorithm.Iterations.TracePopulationCandidates();
        var atInterceptor = Analyzer.Trace(interceptor.Invocations,
            static observation => observation.State.Population.BestMedianWorst(observation.Problem.Objective));

        var run = algorithm.CreateRun(problem, RandomNumberGenerator.Create(seed: 42)).AddAspect(atIterationEnd).AddAspect(atInterceptor);
        await run.CompleteAsync(cancellationToken: TestContext.Current.CancellationToken);

        atIterationEnd.SampleCount.ShouldBe(3);
        atInterceptor.SampleCount.ShouldBe(3);
    }

    /// <summary>
    /// The analyzer observes the exact algorithm configuration supplied as its source.
    /// </summary>
    [Fact]
    public async Task AlgorithmObservation_UsesTheConfiguredSourceInstance()
    {
        var problem = CreateProblem();
        var algorithm = CreateAlgorithm(problem, maximumGenerations: 3);
        var analysis = algorithm.Iterations.TracePopulationCandidates();

        var copy = algorithm with { PopulationSize = PopulationSize * 2 };
        var run = copy.CreateRun(problem, RandomNumberGenerator.Create(seed: 42)).AddAspect(analysis);
        await run.CompleteAsync(cancellationToken: TestContext.Current.CancellationToken);

        analysis.SampleCount.ShouldBe(0);
    }

    [Fact]
    public async Task TraceAnalyzer_IsReadableWhileTheRunStreams()
    {
        var problem = CreateProblem();
        var algorithm = CreateAlgorithm(problem, maximumGenerations: 4);
        var iterations = Clock.FromIterations(algorithm);
        var quality = algorithm.Iterations.TracePopulationCandidates([iterations]);
        var run = algorithm.CreateRun(problem, RandomNumberGenerator.Create(seed: 42)).AddAspect(quality);
        var observedIterations = 0;

        await foreach (var state in run.Stream(cancellationToken: TestContext.Current.CancellationToken))
        {
            observedIterations++;
            quality.SampleCount.ShouldBe(observedIterations);
            quality.Latest.ShouldNotBeNull();
            quality.Latest.Value.At(iterations).ShouldBe(observedIterations);
            state.Population.EvaluatedCandidates.ShouldContain(quality.Latest.Value.Value.Best);
        }

    }

    [Fact]
    public async Task TraceAnalyzer_SnapshotRemainsStableWhileTheRunContinues()
    {
        var problem = CreateProblem();
        var algorithm = CreateAlgorithm(problem, maximumGenerations: 3);
        var iterations = Clock.FromIterations(algorithm);
        var quality = Analyzer.Trace(algorithm.Iterations,
            static observation => observation.State.Population.Select(candidate => candidate.ObjectiveVector).BestMedianWorst(observation.Problem.Objective),
            [iterations]);
        var run = algorithm.CreateRun(problem, RandomNumberGenerator.Create(seed: 42)).AddAspect(quality);

        await using var enumerator = run.Stream(cancellationToken: TestContext.Current.CancellationToken)
                                        .GetAsyncEnumerator(TestContext.Current.CancellationToken);

        (await enumerator.MoveNextAsync()).ShouldBeTrue();
        var firstIteration = quality.Snapshot();

        (await enumerator.MoveNextAsync()).ShouldBeTrue();

        firstIteration.Count.ShouldBe(1);
        firstIteration[0].At(iterations).ShouldBe(1);
        quality.SampleCount.ShouldBe(2);
        quality.Snapshot().Count.ShouldBe(2);
    }

    [Fact]
    public async Task TraceAnalyzer_ByIterationPublishesAStableProjection()
    {
        var problem = CreateProblem();
        var algorithm = CreateAlgorithm(problem, maximumGenerations: 3);
        var iterations = Clock.FromIterations(algorithm);
        var quality = algorithm.Iterations.TracePopulationCandidates([iterations]);
        var run = algorithm.CreateRun(problem, RandomNumberGenerator.Create(seed: 42)).AddAspect(quality);

        await run.CompleteAsync(cancellationToken: TestContext.Current.CancellationToken);

        var byIteration = quality.By(iterations);
        byIteration.Select(point => point.Time).ShouldBe([1L, 2L, 3L]);
    }

    [Fact]
    public async Task TraceAnalyzer_CombinesContextualRetainedAndPulledCoordinates()
    {
        var problem = CreateProblem();
        var algorithm = CreateAlgorithm(problem, maximumGenerations: 3);
        var iterations = Clock.FromIterations(algorithm);
        var evaluations = Clock.FromCandidates(algorithm.Evaluator);
        var elapsed = Clock.FromElapsedTime(TimeProvider.System);
        var quality = algorithm.Iterations.TracePopulationCandidates([iterations, evaluations, elapsed]);
        var run = algorithm.CreateRun(problem, RandomNumberGenerator.Create(seed: 42)).AddAspect(quality);

        await run.CompleteAsync(cancellationToken: TestContext.Current.CancellationToken);

        quality.By(iterations).Select(point => point.Time).ShouldBe([1L, 2L, 3L]);

        var evaluationCounts = quality.By(evaluations).Select(point => point.Time).ToArray();
        evaluationCounts.ShouldAllBe(count => count > 0);
        evaluationCounts.ShouldBe(evaluationCounts.Order());

        var elapsedTimes = quality.By(elapsed).Select(point => point.Time).ToArray();
        elapsedTimes.ShouldAllBe(time => time >= TimeSpan.Zero);
        elapsedTimes.ShouldBe(elapsedTimes.Order());
    }

    [Fact]
    public void TracePopulationCandidates_AtOneSource_StaysTwoIndependentAnalyzers()
    {
        var algorithm = CreateAlgorithm(CreateProblem(), maximumGenerations: 3);

        var first = algorithm.Iterations.TracePopulationCandidates();
        var second = algorithm.Iterations.TracePopulationCandidates();

        // A stateful analyzer holds one run's data, so two of them are never interchangeable.
        first.ShouldNotBeSameAs(second);
        first.SampleCount.ShouldBe(0);
        second.SampleCount.ShouldBe(0);
    }

    /// <summary>
    /// The clock keeps its own count, so it continues across every turn of the observed algorithm, while each turn
    /// restarts the algorithm's own iteration count, whether the cycle recreates its scoped state or keeps it.
    /// </summary>
    [Theory]
    [InlineData(ChildScopePolicy.Fresh)]
    [InlineData(ChildScopePolicy.Retained)]
    [InlineData(ChildScopePolicy.Shared)]
    public async Task IterationClock_ContinuesAcrossTheCyclesTurnsWhileEachTurnRestartsTheIteration(ChildScopePolicy policy)
    {
        var problem = CreateProblem();
        var inner = CreateAlgorithm(problem, maximumGenerations: 3);
        var cycle = CycleAlgorithm.Create(inner) with
        {
            MaximumCycles = 2,
            ScopePolicy = policy
        };

        var iterations = Clock.FromIterations(inner);
        var iteration = Analyzer.Trace(inner.Iterations, observation => observation.Iteration, [iterations]);
        var run = cycle.CreateRun(problem, RandomNumberGenerator.Create(seed: 42)).AddAspect(iteration);

        await run.CompleteAsync(cancellationToken: TestContext.Current.CancellationToken);

        iteration.By(iterations).ShouldBe([new(1, 1), new(2, 2), new(3, 3), new(4, 1), new(5, 2), new(6, 3)]);
    }

    /// <summary>
    /// The cycle numbers its cycles and turns per call, whatever its scope policy, and reports the state each starts from.
    /// </summary>
    [Theory]
    [InlineData(ChildScopePolicy.Fresh)]
    [InlineData(ChildScopePolicy.Retained)]
    [InlineData(ChildScopePolicy.Shared)]
    public async Task CycleObservations_NumberCyclesAndTurnsAndCarryTheStateTheyStartFrom(ChildScopePolicy policy)
    {
        var problem = CreateProblem();
        var first = CreateAlgorithm(problem, maximumGenerations: 3);
        var second = CreateAlgorithm(problem, maximumGenerations: 2);
        var cycle = CycleAlgorithm.Create(first, second) with
        {
            MaximumCycles = 2,
            ScopePolicy = policy
        };
        var cycles = new List<CycleStart<RealVector, PopulationState<RealVector>>>();
        var turns = new List<CycleTurn<RealVector, PopulationState<RealVector>>>();
        var states = new List<PopulationState<RealVector>>();

        await foreach (var state in cycle.CreateRun(problem, RandomNumberGenerator.Create(seed: 42))
            .AddAspect(Aspect.Observe(cycle.Cycles, cycles.Add))
            .AddAspect(Aspect.Observe(cycle.Turns, turns.Add))
            .Stream(cancellationToken: TestContext.Current.CancellationToken))
        {
            states.Add(state);
        }

        cycles.Select(start => start.Cycle).ShouldBe([1L, 2L]);
        cycles.Select(start => start.State).ShouldBe([null, states[4]]);
        turns.Select(turn => (turn.Cycle, turn.Turn, turn.Index)).ShouldBe([(1L, 1L, 0), (1L, 2L, 1), (2L, 3L, 0), (2L, 4L, 1)]);
        turns.Select(turn => turn.Algorithm).ShouldBe([first, second, first, second]);
        turns.Select(turn => turn.State).ShouldBe([null, states[2], states[4], states[7]]);
        turns.ShouldAllBe(turn => ReferenceEquals(turn.Problem, problem) && ReferenceEquals(turn.SearchSpace, problem.SearchSpace));
    }

    /// <summary>
    /// A turn is published before the inner algorithm runs, so its clock has counted the turn when the inner algorithm's
    /// iterations are recorded; a clock shared by two traces counts each turn once.
    /// </summary>
    [Fact]
    public async Task CycleClocks_TickBeforeTheInnerIterationsAndCountOnceWhenShared()
    {
        var problem = CreateProblem();
        var first = CreateAlgorithm(problem, maximumGenerations: 3);
        var second = CreateAlgorithm(problem, maximumGenerations: 2);
        var cycle = CycleAlgorithm.Create(first, second) with { MaximumCycles = 2 };
        var cycleClock = Clock.FromCycles(cycle);
        var turnClock = Clock.FromTurns(cycle);
        var firstIterations = Analyzer.Trace(first.Iterations, observation => observation.Iteration, [cycleClock, turnClock]);
        var secondIterations = Analyzer.Trace(second.Iterations, observation => observation.Iteration, [cycleClock, turnClock]);

        await cycle.CreateRun(problem, RandomNumberGenerator.Create(seed: 42))
            .AddAspect(firstIterations)
            .AddAspect(secondIterations)
            .CompleteAsync(cancellationToken: TestContext.Current.CancellationToken);

        firstIterations.By(cycleClock).Select(point => point.Time).ShouldBe([1L, 1L, 1L, 2L, 2L, 2L]);
        firstIterations.By(turnClock).Select(point => point.Time).ShouldBe([1L, 1L, 1L, 3L, 3L, 3L]);
        secondIterations.By(cycleClock).Select(point => point.Time).ShouldBe([1L, 1L, 2L, 2L]);
        secondIterations.By(turnClock).Select(point => point.Time).ShouldBe([2L, 2L, 4L, 4L]);
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
            currentState with { Population = Population.From(currentState.Population.Take(KeptCandidates)) };
    }
}
