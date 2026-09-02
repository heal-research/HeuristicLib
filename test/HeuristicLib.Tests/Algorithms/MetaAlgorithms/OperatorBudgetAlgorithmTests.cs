using HEAL.HeuristicLib.Encodings.RealVectors;
using HEAL.HeuristicLib.Operators.Evaluators;
using HEAL.HeuristicLib.Operators.Mutators;
using HEAL.HeuristicLib.Operators.Terminators;
using HEAL.HeuristicLib.Problems.TestFunctions;
using HEAL.HeuristicLib.Problems.TestFunctions.SingleObjectives;
using SinglePointCrossover = HEAL.HeuristicLib.Encodings.RealVectors.SinglePointCrossover;
using UniformDistributedCreator = HEAL.HeuristicLib.Encodings.RealVectors.UniformDistributedCreator;

namespace HEAL.HeuristicLib.Tests.Algorithms.MetaAlgorithms;

public class OperatorBudgetAlgorithmTests
{
    [Fact]
    public void WithMaxEvaluatorCalls_StopsAfterObservedEvaluatorCallUsage()
    {
        var problem = CreateProblem();
        var algorithm = CreateAlgorithm(problem) with
        {
            MaximumGenerations = 5
        };

        var results = algorithm.WithMaxEvaluatorCalls(algorithm.Evaluator, 1).Stream(
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

        var results = algorithm.WithMaxEvaluatorCalls(algorithm.Evaluator, 2).Stream(
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

        var results = algorithm.WithMaxEvaluatedCandidates(algorithm.Evaluator, 2).Stream(
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

        var results = algorithm.WithMaxEvaluatedCandidates(algorithm.Evaluator, 6).Stream(
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

        var results = algorithm.WithMaxEvaluatorDuration(
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

        var results = algorithm.WithMaxEvaluatorDuration(
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

        var results = algorithm.WithMaxEvaluatorDuration(
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

        var results = algorithm.WithMaxEvaluatorDuration(
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

        var results = algorithm.WithMaxAlgorithmDuration(
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

        var results = algorithm.WithMaxAlgorithmDuration(
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

        var results = algorithm.WithMaxAlgorithmDuration(
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

        var results = algorithm.WithMaxAlgorithmDuration(
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
    public void WithMaxOperatorDuration_CanObserveExplicitOperator()
    {
        var problem = CreateProblem();
        var algorithm = CreateAlgorithm(problem) with
        {
            MaximumGenerations = 5
        };

        var results = algorithm.WithMaxOperatorDuration(
            algorithm.Evaluator,
            TimeSpan.FromSeconds(1),
            new AdvancingTimeProvider(TimeSpan.FromSeconds(2)),
            static (observedOperator, duration, timeProvider) =>
                observedOperator.MeasureEvaluatorDuration(duration, timeProvider))
            .Stream(
                problem,
                RandomNumberGenerator.Create(42),
                ct: TestContext.Current.CancellationToken)
            .ToList();

        results.Count.ShouldBe(1);
    }

    [Fact]
    public void WithMaxCreatorCalls_CanObserveExplicitCreator()
    {
        var problem = CreateProblem();
        var algorithm = CreateAlgorithm(problem) with
        {
            MaximumGenerations = 5
        };

        var results = algorithm.WithMaxCreatorCalls(
            algorithm.Creator,
            maximumCalls: 1)
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

        var results = algorithm.WithMaxCreatedCandidates(
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

        var results = algorithm.WithMaxCreatorDuration(
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

        var results = algorithm.WithMaxCrossoverCalls(
            algorithm.Crossover,
            maximumCalls: 1)
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

        var results = algorithm.WithMaxCrossedCandidates(
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

        var results = algorithm.WithMaxCrossoverDuration(
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
    public void WithMaxCount_CanObserveMutatorCalls()
    {
        var problem = CreateProblem();
        var algorithm = CreateAlgorithm(problem) with
        {
            MaximumGenerations = 5,
            MutationRate = 1.0
        };

        var results = algorithm.WithMaxCount(
            algorithm.Mutator,
            maximumCount: 1,
            countedOperatorFactory: static (observedOperator, counter) =>
                observedOperator.CountMutatorCalls(counter))
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

        var results = algorithm.WithMaxMutatorCalls(
            algorithm.Mutator,
            maximumCalls: 1)
            .Stream(
                problem,
                RandomNumberGenerator.Create(42),
                ct: TestContext.Current.CancellationToken)
            .ToList();

        results.Count.ShouldBe(2);
    }

    [Fact]
    public void WithMaxCount_CanObserveMutatedCandidates()
    {
        var problem = CreateProblem();
        var algorithm = CreateAlgorithm(problem) with
        {
            MaximumGenerations = 5,
            MutationRate = 1.0
        };

        var results = algorithm.WithMaxCount(
            algorithm.Mutator,
            maximumCount: 6,
            countedOperatorFactory: static (observedOperator, counter) =>
                observedOperator.CountMutatedCandidates(counter))
            .Stream(
                problem,
                RandomNumberGenerator.Create(42),
                ct: TestContext.Current.CancellationToken)
            .ToList();

        results.Count.ShouldBe(3);
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

        var results = algorithm.WithMaxMutatedCandidates(
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

        var results = algorithm.WithMaxMutatorDuration(
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
    public void WithMaxCount_CanObserveExplicitOperator()
    {
        var problem = CreateProblem();
        var algorithm = CreateAlgorithm(problem) with
        {
            MaximumGenerations = 5
        };

        var results = algorithm.WithMaxCount(
            algorithm.Evaluator,
            maximumCount: 1,
            countedOperatorFactory: static (observedOperator, counter) =>
                observedOperator.CountEvaluatorCalls(counter))
            .Stream(
                problem,
                RandomNumberGenerator.Create(42),
                ct: TestContext.Current.CancellationToken)
            .ToList();

        results.Count.ShouldBe(1);
    }

    [Fact]
    public void AfterOperatorCountTerminator_CanUseSharedCounterAcrossObservedOperators()
    {
        var problem = CreateProblem();
        var counter = new ObservationCounter();
        var baseAlgorithm = CreateAlgorithm(problem);
        var algorithm = baseAlgorithm with
        {
            MaximumGenerations = 5,
            MutationRate = 1.0,
            Evaluator = baseAlgorithm.Evaluator.CountEvaluatorCalls(counter),
            Mutator = baseAlgorithm.Mutator.CountMutatorCalls(counter)
        };
        var externallyStoppedAlgorithm = algorithm.WithTerminator(AfterOperatorCountTerminator.For(problem, counter, maximumCount: 3));

        var results = externallyStoppedAlgorithm.Stream(
            problem,
            RandomNumberGenerator.Create(42),
            ct: TestContext.Current.CancellationToken).ToList();

        results.Count.ShouldBe(2);
        counter.CurrentCount.ShouldBe(3);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void AfterOperatorCountTerminator_IsImmediatelyTerminal_WhenMaximumCountIsNotPositive(int maximumCount)
    {
        var counter = new ObservationCounter();
        var terminator = new AfterOperatorCountTerminator<RealVector>(counter, maximumCount);

        terminator.IsTerminalState().ShouldBeTrue();
    }

    [Fact]
    public void AfterOperatorDurationTerminator_IsTerminalAtMaximumDuration()
    {
        var duration = new ObservationDuration();
        var terminator = new AfterOperatorDurationTerminator<RealVector>(
            duration,
            maximumDuration: TimeSpan.FromSeconds(2));

        duration.AddDuration(TimeSpan.FromSeconds(1));
        terminator.IsTerminalState().ShouldBeFalse();

        duration.AddDuration(TimeSpan.FromSeconds(1));
        terminator.IsTerminalState().ShouldBeTrue();
    }

    [Fact]
    public void AfterOperatorDurationTerminator_IsImmediatelyTerminal_WhenMaximumDurationIsNotPositive()
    {
        var duration = new ObservationDuration();

        new AfterOperatorDurationTerminator<RealVector>(duration, TimeSpan.Zero).IsTerminalState().ShouldBeTrue();
        new AfterOperatorDurationTerminator<RealVector>(duration, TimeSpan.FromTicks(-1)).IsTerminalState().ShouldBeTrue();
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
            new OperatorBudgetAlgorithm<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem, PopulationState<RealVector>, IEvaluator<RealVector>>
            {
                Algorithm = algorithm,
                ObservedOperator = algorithm.Evaluator,
                MaximumCount = maximumCount,
                CountedOperatorFactory = static (observedOperator, counter) =>
                    observedOperator.CountEvaluatorCalls(counter)
            };

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
            new OperatorDurationBudgetAlgorithm<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem, PopulationState<RealVector>, IEvaluator<RealVector>>
            {
                Algorithm = algorithm,
                ObservedOperator = algorithm.Evaluator,
                MaximumDuration = TimeSpan.FromTicks(ticks),
                MeasuredOperatorFactory = static (observedOperator, duration, timeProvider) =>
                    observedOperator.MeasureEvaluatorDuration(duration, timeProvider)
            };

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
            new AlgorithmDurationBudgetAlgorithm<
                RealVector,
                BoundedRealVectorSearchSpace,
                TestFunctionProblem,
                PopulationState<RealVector>>
            {
                Algorithm = algorithm,
                MaximumDuration = TimeSpan.FromTicks(ticks)
            };

        budgeted.Stream(problem, RandomNumberGenerator.Create(42), ct: TestContext.Current.CancellationToken).Count().ShouldBe(1);
    }

    private static TestFunctionProblem CreateProblem()
    {
        return new TestFunctionProblem(new SphereFunction(dimension: 3));
    }

    private static GeneticAlgorithm<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem> CreateAlgorithm(
        TestFunctionProblem problem)
    {
        return new GeneticAlgorithm<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>
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
            BoundedRealVectorSearchSpace searchSpace,
            TestFunctionProblem problem)
        {
            CheckedStateCount++;
            HasTerminated = CheckedStateCount >= StopOnCheckedStateCount;
            return HasTerminated;
        }
    }
}
