using HEAL.HeuristicLib.Encodings.RealVectors;
using HEAL.HeuristicLib.Inspection;
using HEAL.HeuristicLib.Operators.Evaluators;
using HEAL.HeuristicLib.Operators.Mutators;
using HEAL.HeuristicLib.Operators.Terminators;
using HEAL.HeuristicLib.Problems.TestFunctions;
using SinglePointCrossover = HEAL.HeuristicLib.Encodings.RealVectors.SinglePointCrossover;
using UniformDistributedCreator = HEAL.HeuristicLib.Encodings.RealVectors.UniformDistributedCreator;

namespace HEAL.HeuristicLib.Tests.Algorithms.MetaAlgorithms;

public class OperatorCountBudgetAlgorithmTests
{
    [Fact]
    public void CombinedInvocationBudgetCountsMutationAndCrossoverOncePerCallAcrossIndependentRuns()
    {
        var problem = CreateProblem();
        var algorithm = CreateAlgorithm(problem) with { MaximumGenerations = 6, MutationRate = 1.0 };
        var budget = algorithm.LimitedToInvocations([algorithm.Mutator, algorithm.Crossover, algorithm.Mutator], 3);
        var constructed = new OperatorCountBudgetAlgorithm<RealVector, PopulationState<RealVector>>(
            algorithm, [algorithm.Mutator, algorithm.Crossover, algorithm.Mutator], 3);
        constructed.ShouldBe(budget);

        for (var trial = 0; trial < 2; trial++)
            budget.Stream(problem, RandomNumberGenerator.Create(42), ct: TestContext.Current.CancellationToken).Count().ShouldBe(3);
        constructed.Stream(problem, RandomNumberGenerator.Create(42), ct: TestContext.Current.CancellationToken).Count().ShouldBe(3);
    }

    [Fact]
    public void CombinedCandidateBudgetSumsWorkFromDifferentOperations()
    {
        var problem = CreateProblem();
        var algorithm = CreateAlgorithm(problem) with { MaximumGenerations = 6, MutationRate = 1.0 };
        var budget = algorithm.LimitedToCandidates([algorithm.Mutator, algorithm.Crossover], 6);
        var constructed = new OperatorCountBudgetAlgorithm<RealVector, PopulationState<RealVector>>(
            algorithm, [algorithm.Mutator, algorithm.Crossover], 6)
        { Metric = OperatorCountMetric.Candidates };
        constructed.ShouldBe(budget);
        constructed.Stream(problem, RandomNumberGenerator.Create(42), ct: TestContext.Current.CancellationToken).Count().ShouldBe(2);
    }

    [Fact]
    public void CombinedDurationBudgetSumsMutationAndCrossoverWithConstructorParity()
    {
        var problem = CreateProblem();
        var algorithm = CreateAlgorithm(problem) with { MaximumGenerations = 6, MutationRate = 1.0 };
        var clock = new AdvancingTimeProvider(TimeSpan.FromSeconds(2));
        var budget = algorithm.LimitedToDuration([algorithm.Mutator, algorithm.Crossover, algorithm.Mutator], TimeSpan.FromSeconds(3), clock);
        var constructed = new OperatorDurationBudgetAlgorithm<RealVector, PopulationState<RealVector>>(
            algorithm, [algorithm.Mutator, algorithm.Crossover, algorithm.Mutator], TimeSpan.FromSeconds(3))
        { TimeProvider = clock };
        constructed.ShouldBe(budget);
        budget.Stream(problem, RandomNumberGenerator.Create(42), ct: TestContext.Current.CancellationToken).Count().ShouldBe(2);
        constructed.Stream(problem, RandomNumberGenerator.Create(42), ct: TestContext.Current.CancellationToken).Count().ShouldBe(2);
    }

    [Fact]
    public void WithMaxEvaluatorCalls_StopsAfterObservedEvaluatorCallUsage()
    {
        var problem = CreateProblem();
        var algorithm = CreateAlgorithm(problem) with
        {
            MaximumGenerations = 5
        };

        var results = algorithm.LimitedToInvocations(algorithm.Evaluator, 1).Stream(
            problem,
            RandomNumberGenerator.Create(42),
            ct: TestContext.Current.CancellationToken).ToList();

        results.Count.ShouldBe(1);
        results.Single().Population.EvaluatedCandidates.Count.ShouldBe(5);
    }

    [Fact]
    public void WithMaxEvaluatorCalls_StopsAfterObservedEvaluatorCallCount()
    {
        var problem = CreateProblem();
        var algorithm = CreateAlgorithm(problem) with
        {
            MaximumGenerations = 5
        };

        var results = algorithm.LimitedToInvocations(algorithm.Evaluator, 2).Stream(
            problem,
            RandomNumberGenerator.Create(42),
            ct: TestContext.Current.CancellationToken).ToList();

        results.Count.ShouldBe(2);
    }

    [Fact]
    public void WithMaxEvaluatedCandidates_StopsAfterObservedEvaluatedCandidateCount()
    {
        var problem = CreateProblem();
        var algorithm = CreateAlgorithm(problem) with
        {
            MaximumGenerations = 5
        };

        var results = algorithm.LimitedToCandidates(algorithm.Evaluator, 2).Stream(
            problem,
            RandomNumberGenerator.Create(42),
            ct: TestContext.Current.CancellationToken).ToList();

        results.Count.ShouldBe(1);
        results.Single().Population.EvaluatedCandidates.Count.ShouldBe(5);
    }

    [Fact]
    public void WithMaxEvaluatedCandidates_AllowsAnotherStateWhenBudgetIsNotReached()
    {
        var problem = CreateProblem();
        var algorithm = CreateAlgorithm(problem) with
        {
            MaximumGenerations = 5
        };

        var results = algorithm.LimitedToCandidates(algorithm.Evaluator, 6).Stream(
            problem,
            RandomNumberGenerator.Create(42),
            ct: TestContext.Current.CancellationToken).ToList();

        results.Count.ShouldBe(2);
    }

    [Fact]
    public void WithMaxEvaluatorDuration_StopsAfterObservedEvaluatorDuration()
    {
        var problem = CreateProblem();
        var algorithm = CreateAlgorithm(problem) with
        {
            MaximumGenerations = 5
        };

        var results = algorithm.LimitedToDuration(
            algorithm.Evaluator,
            TimeSpan.FromSeconds(3),
            new AdvancingTimeProvider(TimeSpan.FromSeconds(2)))
            .Stream(
                problem,
                RandomNumberGenerator.Create(42),
                ct: TestContext.Current.CancellationToken)
            .ToList();

        results.Count.ShouldBe(2);
    }

    [Fact]
    public void WithMaxEvaluatorDuration_YieldsCrossingStateBeforeStopping()
    {
        var problem = CreateProblem();
        var algorithm = CreateAlgorithm(problem) with
        {
            MaximumGenerations = 5
        };

        var results = algorithm.LimitedToDuration(
            algorithm.Evaluator,
            TimeSpan.FromSeconds(1),
            new AdvancingTimeProvider(TimeSpan.FromSeconds(2)))
            .Stream(
                problem,
                RandomNumberGenerator.Create(42),
                ct: TestContext.Current.CancellationToken)
            .ToList();

        results.Count.ShouldBe(1);
        results.Single().Population.EvaluatedCandidates.Count.ShouldBe(5);
    }

    [Fact]
    public void WithMaxEvaluatorDuration_DoesNotInternallyCompleteAlgorithm()
    {
        var problem = CreateProblem();
        var internalTerminator = new RecordingPopulationTerminator(5);
        var algorithm = CreateAlgorithm(problem) with
        {
            MaximumGenerations = 5,
            Terminator = internalTerminator
        };

        var results = algorithm.LimitedToDuration(
            algorithm.Evaluator,
            TimeSpan.FromSeconds(3),
            new AdvancingTimeProvider(TimeSpan.FromSeconds(2)))
            .Stream(
                problem,
                RandomNumberGenerator.Create(42),
                ct: TestContext.Current.CancellationToken)
            .ToList();

        results.Count.ShouldBe(2);
        internalTerminator.CheckedStateCount.ShouldBe(2);
        internalTerminator.HasTerminated.ShouldBeFalse();
    }

    [Fact]
    public void WithMaxEvaluatorDuration_CanObserveExplicitEvaluator()
    {
        var problem = CreateProblem();
        var algorithm = CreateAlgorithm(problem) with
        {
            MaximumGenerations = 5
        };

        var results = algorithm.LimitedToDuration(
            algorithm.Evaluator,
            TimeSpan.FromSeconds(1),
            new AdvancingTimeProvider(TimeSpan.FromSeconds(2)))
            .Stream(
                problem,
                RandomNumberGenerator.Create(42),
                ct: TestContext.Current.CancellationToken)
            .ToList();

        results.Count.ShouldBe(1);
    }

    [Fact]
    public void WithMaxAlgorithmDuration_StopsAfterObservedStateProductionDuration()
    {
        var problem = CreateProblem();
        var algorithm = CreateAlgorithm(problem) with
        {
            MaximumGenerations = 5
        };

        var results = algorithm.LimitedToDuration(
            TimeSpan.FromSeconds(3),
            new AdvancingTimeProvider(TimeSpan.FromSeconds(2)))
            .Stream(
                problem,
                RandomNumberGenerator.Create(42),
                ct: TestContext.Current.CancellationToken)
            .ToList();

        results.Count.ShouldBe(2);
    }

    [Fact]
    public void WithMaxAlgorithmDuration_YieldsCrossingStateBeforeStopping()
    {
        var problem = CreateProblem();
        var algorithm = CreateAlgorithm(problem) with
        {
            MaximumGenerations = 5
        };

        var results = algorithm.LimitedToDuration(
            TimeSpan.FromSeconds(1),
            new AdvancingTimeProvider(TimeSpan.FromSeconds(2)))
            .Stream(
                problem,
                RandomNumberGenerator.Create(42),
                ct: TestContext.Current.CancellationToken)
            .ToList();

        results.Count.ShouldBe(1);
        results.Single().Population.EvaluatedCandidates.Count.ShouldBe(5);
    }

    [Fact]
    public void WithMaxAlgorithmDuration_DoesNotInternallyCompleteAlgorithm()
    {
        var problem = CreateProblem();
        var internalTerminator = new RecordingPopulationTerminator(5);
        var algorithm = CreateAlgorithm(problem) with
        {
            MaximumGenerations = 5,
            Terminator = internalTerminator
        };

        var results = algorithm.LimitedToDuration(
            TimeSpan.FromSeconds(3),
            new AdvancingTimeProvider(TimeSpan.FromSeconds(2)))
            .Stream(
                problem,
                RandomNumberGenerator.Create(42),
                ct: TestContext.Current.CancellationToken)
            .ToList();

        results.Count.ShouldBe(2);
        internalTerminator.CheckedStateCount.ShouldBe(2);
        internalTerminator.HasTerminated.ShouldBeFalse();
    }

    [Fact]
    public void WithMaxAlgorithmDuration_CompletesNormally_WhenInnerAlgorithmCompletesFirst()
    {
        var problem = CreateProblem();
        var algorithm = CreateAlgorithm(problem) with
        {
            MaximumGenerations = 2
        };

        var results = algorithm.LimitedToDuration(
            TimeSpan.FromSeconds(10),
            new AdvancingTimeProvider(TimeSpan.FromSeconds(2)))
            .Stream(
                problem,
                RandomNumberGenerator.Create(42),
                ct: TestContext.Current.CancellationToken)
            .ToList();

        results.Count.ShouldBe(2);
    }

    [Fact]
    public void WithMaxCreatorCalls_CanObserveExplicitCreator()
    {
        var problem = CreateProblem();
        var algorithm = CreateAlgorithm(problem) with
        {
            MaximumGenerations = 5
        };

        var results = algorithm.LimitedToInvocations(
            algorithm.Creator,
            maximumInvocations: 1)
            .Stream(
                problem,
                RandomNumberGenerator.Create(42),
                ct: TestContext.Current.CancellationToken)
            .ToList();

        results.Count.ShouldBe(1);
    }

    [Fact]
    public void WithMaxCreatedCandidates_CanObserveExplicitCreator()
    {
        var problem = CreateProblem();
        var algorithm = CreateAlgorithm(problem) with
        {
            MaximumGenerations = 5
        };

        var results = algorithm.LimitedToCandidates(
            algorithm.Creator,
            maximumCandidates: 2)
            .Stream(
                problem,
                RandomNumberGenerator.Create(42),
                ct: TestContext.Current.CancellationToken)
            .ToList();

        results.Count.ShouldBe(1);
    }

    [Fact]
    public void WithMaxCreatorDuration_CanObserveExplicitCreator()
    {
        var problem = CreateProblem();
        var algorithm = CreateAlgorithm(problem) with
        {
            MaximumGenerations = 5
        };

        var results = algorithm.LimitedToDuration(
            algorithm.Creator,
            TimeSpan.FromSeconds(1),
            new AdvancingTimeProvider(TimeSpan.FromSeconds(2)))
            .Stream(
                problem,
                RandomNumberGenerator.Create(42),
                ct: TestContext.Current.CancellationToken)
            .ToList();

        results.Count.ShouldBe(1);
    }

    [Fact]
    public void WithMaxCrossoverCalls_CanObserveExplicitCrossover()
    {
        var problem = CreateProblem();
        var algorithm = CreateAlgorithm(problem) with
        {
            MaximumGenerations = 5,
            MutationRate = 0.0
        };

        var results = algorithm.LimitedToInvocations(
            algorithm.Crossover,
            maximumInvocations: 1)
            .Stream(
                problem,
                RandomNumberGenerator.Create(42),
                ct: TestContext.Current.CancellationToken)
            .ToList();

        results.Count.ShouldBe(2);
    }

    [Fact]
    public void WithMaxCrossedCandidates_CanObserveExplicitCrossover()
    {
        var problem = CreateProblem();
        var algorithm = CreateAlgorithm(problem) with
        {
            MaximumGenerations = 5,
            MutationRate = 0.0
        };

        var results = algorithm.LimitedToCandidates(
            algorithm.Crossover,
            maximumCandidates: 6)
            .Stream(
                problem,
                RandomNumberGenerator.Create(42),
                ct: TestContext.Current.CancellationToken)
            .ToList();

        results.Count.ShouldBe(3);
    }

    [Fact]
    public void WithMaxCrossoverDuration_CanObserveExplicitCrossover()
    {
        var problem = CreateProblem();
        var algorithm = CreateAlgorithm(problem) with
        {
            MaximumGenerations = 5,
            MutationRate = 0.0
        };

        var results = algorithm.LimitedToDuration(
            algorithm.Crossover,
            TimeSpan.FromSeconds(1),
            new AdvancingTimeProvider(TimeSpan.FromSeconds(2)))
            .Stream(
                problem,
                RandomNumberGenerator.Create(42),
                ct: TestContext.Current.CancellationToken)
            .ToList();

        results.Count.ShouldBe(2);
    }

    [Fact]
    public void WithMaxMutatorCalls_CanObserveExplicitMutator()
    {
        var problem = CreateProblem();
        var algorithm = CreateAlgorithm(problem) with
        {
            MaximumGenerations = 5,
            MutationRate = 1.0
        };

        var results = algorithm.LimitedToInvocations(
            algorithm.Mutator,
            maximumInvocations: 1)
            .Stream(
                problem,
                RandomNumberGenerator.Create(42),
                ct: TestContext.Current.CancellationToken)
            .ToList();

        results.Count.ShouldBe(2);
    }

    [Fact]
    public void WithMaxMutatedCandidates_CanObserveExplicitMutator()
    {
        var problem = CreateProblem();
        var algorithm = CreateAlgorithm(problem) with
        {
            MaximumGenerations = 5,
            MutationRate = 1.0
        };

        var results = algorithm.LimitedToCandidates(
            algorithm.Mutator,
            maximumCandidates: 6)
            .Stream(
                problem,
                RandomNumberGenerator.Create(42),
                ct: TestContext.Current.CancellationToken)
            .ToList();

        results.Count.ShouldBe(3);
    }

    [Fact]
    public void WithMaxMutatorDuration_CanObserveExplicitMutator()
    {
        var problem = CreateProblem();
        var algorithm = CreateAlgorithm(problem) with
        {
            MaximumGenerations = 5,
            MutationRate = 1.0
        };

        var results = algorithm.LimitedToDuration(
            algorithm.Mutator,
            TimeSpan.FromSeconds(1),
            new AdvancingTimeProvider(TimeSpan.FromSeconds(2)))
            .Stream(
                problem,
                RandomNumberGenerator.Create(42),
                ct: TestContext.Current.CancellationToken)
            .ToList();

        results.Count.ShouldBe(2);
    }

    [Fact]
    public void AfterOperatorCountTerminator_CanUseSharedCounterAcrossObservedOperators()
    {
        var problem = CreateProblem();
        var counter = new CountAccumulatorHandle();
        var baseAlgorithm = CreateAlgorithm(problem);
        var algorithm = baseAlgorithm with
        {
            MaximumGenerations = 5,
            MutationRate = 1.0
        };
        var externallyStoppedAlgorithm = algorithm.TerminatedBy(AfterOperatorCountTerminator.For(problem, counter, maximumCount: 3));

        var accumulator = new CountAccumulator();
        var run = externallyStoppedAlgorithm.CreateRun(problem, RandomNumberGenerator.Create(42), setup: setup => setup
            .AddResource(counter, accumulator)
            .AddAspect(algorithm.Evaluator.CountInvocations(accumulator))
            .AddAspect(algorithm.Mutator.CountInvocations(accumulator)));
        var results = run.Stream(cancellationToken: TestContext.Current.CancellationToken).ToList();

        results.Count.ShouldBe(2);
        run.Resource(counter).CurrentCount.ShouldBe(3);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void AfterOperatorCountTerminator_IsImmediatelyTerminal_WhenMaximumCountIsNotPositive(int maximumCount)
    {
        var counter = new CountAccumulatorHandle();
        var terminator = new AfterOperatorCountTerminator<RealVector>(counter, maximumCount);

        IsTerminal(terminator, ResolutionScope.Create()).ShouldBeTrue();
    }

    [Fact]
    public void AfterOperatorDurationTerminator_IsTerminalAtMaximumDuration()
    {
        var duration = new DurationAccumulatorHandle();
        var scope = ResolutionScope.Create();
        var terminator = new AfterOperatorDurationTerminator<RealVector>(
            duration,
            maximumDuration: TimeSpan.FromSeconds(2));

        scope.Resource(duration).AddDuration(TimeSpan.FromSeconds(1));
        IsTerminal(terminator, scope).ShouldBeFalse();

        scope.Resource(duration).AddDuration(TimeSpan.FromSeconds(1));
        IsTerminal(terminator, scope).ShouldBeTrue();
    }

    [Fact]
    public void AfterOperatorDurationTerminator_IsImmediatelyTerminal_WhenMaximumDurationIsNotPositive()
    {
        var duration = new DurationAccumulatorHandle();
        var scope = ResolutionScope.Create();

        IsTerminal(new AfterOperatorDurationTerminator<RealVector>(duration, TimeSpan.Zero), scope).ShouldBeTrue();
        IsTerminal(new AfterOperatorDurationTerminator<RealVector>(duration, TimeSpan.FromTicks(-1)), scope).ShouldBeTrue();
    }

    private static bool IsTerminal(IOperator<Termination, RealVector> terminator, ResolutionScope scope)
    {
        var problem = CreateProblem();
        var executor = scope.For<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem, SingleSolutionState<RealVector>>().Resolve(terminator);
        var state = SingleSolutionState.From(new RealVector(0.0), new ObjectiveVector(0));
        return executor.IsTerminalState(state, RandomNumberGenerator.Create(42), problem.SearchSpace, problem);
    }

    /// <summary>
    /// A nonpositive budget is a stable value rather than a rejected one. The budget is checked after each produced
    /// state, so the stream stops after the first one.
    /// </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Stream_WithNonpositiveMaximumCount_StopsAfterFirstState(int maximumCount)
    {
        var problem = CreateProblem();
        var algorithm = CreateAlgorithm(problem);

        var budgeted =
            new OperatorCountBudgetAlgorithm<Evaluation, RealVector, PopulationState<RealVector>>(
                algorithm, algorithm.Evaluator, maximumCount);

        budgeted.Stream(problem, RandomNumberGenerator.Create(42), ct: TestContext.Current.CancellationToken).Count().ShouldBe(1);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void OperatorDurationBudget_WithNonpositiveMaximumDuration_StopsAfterFirstState(int ticks)
    {
        var problem = CreateProblem();
        var algorithm = CreateAlgorithm(problem);

        var budgeted =
            new OperatorDurationBudgetAlgorithm<Evaluation, RealVector, PopulationState<RealVector>>(
                algorithm, algorithm.Evaluator, TimeSpan.FromTicks(ticks));

        budgeted.Stream(problem, RandomNumberGenerator.Create(42), ct: TestContext.Current.CancellationToken).Count().ShouldBe(1);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void AlgorithmDurationBudget_WithNonpositiveMaximumDuration_StopsAfterFirstState(int ticks)
    {
        var problem = CreateProblem();
        var algorithm = CreateAlgorithm(problem);

        var budgeted =
            new AlgorithmDurationBudgetAlgorithm<RealVector, PopulationState<RealVector>>
            {
                Algorithm = algorithm,
                MaximumDuration = TimeSpan.FromTicks(ticks)
            };

        budgeted.Stream(problem, RandomNumberGenerator.Create(42), ct: TestContext.Current.CancellationToken).Count().ShouldBe(1);
    }

    [Fact]
    public void OperatorBudget_KeepsAnalysisObservationsOnTheCountedOperator()
    {
        var problem = CreateProblem();
        var algorithm = CreateAlgorithm(problem) with
        {
            MaximumGenerations = 3
        };
        var analyzer = new EvaluationObservingAnalyzer(algorithm.Evaluator);

        algorithm.LimitedToCandidates(algorithm.Evaluator, maximumCandidates: 1000)
                 .CreateRun(problem, RandomNumberGenerator.Create(42), setup: setup => setup.AddAspect(analyzer))
                 .Complete(cancellationToken: TestContext.Current.CancellationToken);

        analyzer.ObservedCandidates.ShouldBeGreaterThan(0);
    }

    /// <summary>
    /// A budget measures the operator it limits, never the analyzer watching that operator. Were it the other way
    /// round, the recording work would be charged to the budget and attaching an analyzer would shorten the run. Here
    /// only the observation advances the clock, each time by more than the whole budget.
    /// </summary>
    [Fact]
    public void ADurationBudget_WrapsTheOperatorRatherThanTheAnalyzerObservingIt()
    {
        var problem = CreateProblem();
        var algorithm = CreateAlgorithm(problem) with { MaximumGenerations = 3 };
        var clock = new ManualTimeProvider();
        var slowObservation = Aspect.Observe(
            algorithm.Evaluator.Invocations, _ => clock.Advance(TimeSpan.FromMinutes(1)));
        var unbudgeted = algorithm.Stream(problem, RandomNumberGenerator.Create(42), ct: TestContext.Current.CancellationToken).Count();

        var budgeted = algorithm.LimitedToDuration(algorithm.Evaluator, TimeSpan.FromSeconds(30), clock)
            .CreateRun(problem, RandomNumberGenerator.Create(42), setup: setup => setup.AddAspect(slowObservation))
            .Stream(cancellationToken: TestContext.Current.CancellationToken).Count();

        budgeted.ShouldBe(unbudgeted);
    }

    /// <summary>
    /// The counterpart: the analyzer still observes everything the budget's wrapper passes through, so ordering them
    /// does not cost the observation.
    /// </summary>
    [Fact]
    public async Task ADurationBudget_DoesNotHideTheOperatorFromAnAnalyzer()
    {
        var problem = CreateProblem();
        var algorithm = CreateAlgorithm(problem) with { MaximumGenerations = 3 };
        var withoutBudget = algorithm.Evaluator.Invocations.TraceBestCandidateSoFar();
        var withBudget = algorithm.Evaluator.Invocations.TraceBestCandidateSoFar();

        await algorithm.CreateRun(problem, RandomNumberGenerator.Create(42), setup: setup => setup.AddAspect(withoutBudget))
                       .CompleteAsync(cancellationToken: TestContext.Current.CancellationToken);

        await algorithm
            .LimitedToDuration(algorithm.Evaluator, TimeSpan.FromSeconds(30), new AdvancingTimeProvider(TimeSpan.FromMilliseconds(1)))
            .CreateRun(problem, RandomNumberGenerator.Create(42), setup: setup => setup.AddAspect(withBudget))
            .CompleteAsync(cancellationToken: TestContext.Current.CancellationToken);

        withBudget.SampleCount.ShouldBe(withoutBudget.SampleCount);
    }

    private static TestFunctionProblem CreateProblem()
    {
        return new TestFunctionProblem(new SphereFunction(dimension: 3));
    }

    private static GeneticAlgorithm<RealVector> CreateAlgorithm(
        TestFunctionProblem problem)
    {
        return new GeneticAlgorithm<RealVector>
        {
            PopulationSize = 5,
            Creator = new UniformDistributedCreator(problem.SearchSpace),
            Crossover = new SinglePointCrossover(),
            Mutator = new GaussianMutator(0.1, 0.1),
            MutationRate = 0.5,
            Selector = RandomSelector.For(problem),
            Elites = 0
        };
    }

    /// <summary>
    /// Counts the candidates evaluated by one evaluator, so that a run can assert the observation was installed even
    /// when a budget algorithm wraps the same evaluator.
    /// </summary>
    private sealed class EvaluationObservingAnalyzer
        : Aspect
    {
        public int ObservedCandidates { get; private set; }

        public EvaluationObservingAnalyzer(IOperator<Evaluation, RealVector> evaluator)
        {
            Advices = [.. Aspect.Observe(evaluator.Invocations, Record)];
        }

        public void Record(EvaluationObservation<RealVector> observation) =>
            ObservedCandidates += observation.ObjectiveVectors.Count;
    }

    private sealed class ManualTimeProvider : TimeProvider
    {
        private long timestamp;

        public override long TimestampFrequency => TimeSpan.TicksPerSecond;

        public override long GetTimestamp() => timestamp;

        public void Advance(TimeSpan duration)
        {
            timestamp += duration.Ticks;
        }
    }

    private sealed class AdvancingTimeProvider(TimeSpan step) : TimeProvider
    {
        private long timestamp;

        public override long TimestampFrequency => TimeSpan.TicksPerSecond;

        public override long GetTimestamp()
        {
            var current = timestamp;
            timestamp += step.Ticks;
            return current;
        }
    }

    private sealed record RecordingPopulationTerminator(int StopOnCheckedStateCount)
        : StatelessTerminator<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem, PopulationState<RealVector>>
    {
        public int CheckedStateCount { get; private set; }
        public bool HasTerminated { get; private set; }

        public override bool IsTerminalState(
            PopulationState<RealVector> state,
            IRandomNumberGenerator random,
            BoundedRealVectorSearchSpace searchSpace,
            TestFunctionProblem problem)
        {
            CheckedStateCount++;
            HasTerminated = CheckedStateCount >= StopOnCheckedStateCount;
            return HasTerminated;
        }
    }
}
