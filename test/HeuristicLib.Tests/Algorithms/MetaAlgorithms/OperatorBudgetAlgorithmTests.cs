using HEAL.HeuristicLib.Analysis;
using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Algorithms.Evolutionary;
using HEAL.HeuristicLib.Algorithms.MetaAlgorithms;
using HEAL.HeuristicLib.Genotypes.Vectors;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Operators.Creators.RealVectorCreators;
using HEAL.HeuristicLib.Operators.Crossovers.RealVectorCrossovers;
using HEAL.HeuristicLib.Operators.Evaluators;
using HEAL.HeuristicLib.Operators.Mutators;
using HEAL.HeuristicLib.Operators.Mutators.RealVectorMutators;
using HEAL.HeuristicLib.Operators.Selectors;
using HEAL.HeuristicLib.Operators.Terminators;
using HEAL.HeuristicLib.Problems.TestFunctions;
using HEAL.HeuristicLib.Problems.TestFunctions.SingleObjectives;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces.Vectors;
using HEAL.HeuristicLib.States;

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

        var results = algorithm.WithMaxEvaluatorCalls(1).RunStreaming(
            problem,
            RandomNumberGenerator.Create(42),
            ct: TestContext.Current.CancellationToken).ToList();

        results.Count.ShouldBe(1);
        results.Single().Population.Solutions.Length.ShouldBe(5);
    }

    [Fact]
    public void WithMaxEvaluatorCalls_StopsAfterObservedEvaluatorCallCount()
    {
        var problem = CreateProblem();
        var algorithm = CreateAlgorithm(problem) with
        {
            MaximumGenerations = 5
        };

        var results = algorithm.WithMaxEvaluatorCalls(2).RunStreaming(
            problem,
            RandomNumberGenerator.Create(42),
            ct: TestContext.Current.CancellationToken).ToList();

        results.Count.ShouldBe(2);
    }

    [Fact]
    public void WithMaxEvaluatedGenotypes_StopsAfterObservedEvaluatedGenotypeCount()
    {
        var problem = CreateProblem();
        var algorithm = CreateAlgorithm(problem) with
        {
            MaximumGenerations = 5
        };

        var results = algorithm.WithMaxEvaluatedGenotypes(2).RunStreaming(
            problem,
            RandomNumberGenerator.Create(42),
            ct: TestContext.Current.CancellationToken).ToList();

        results.Count.ShouldBe(1);
        results.Single().Population.Solutions.Length.ShouldBe(5);
    }

    [Fact]
    public void WithMaxEvaluatedGenotypes_AllowsAnotherStateWhenBudgetIsNotReached()
    {
        var problem = CreateProblem();
        var algorithm = CreateAlgorithm(problem) with
        {
            MaximumGenerations = 5
        };

        var results = algorithm.WithMaxEvaluatedGenotypes(6).RunStreaming(
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
            TimeSpan.FromSeconds(3),
            new AdvancingTimeProvider(TimeSpan.FromSeconds(2)))
            .RunStreaming(
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
            TimeSpan.FromSeconds(1),
            new AdvancingTimeProvider(TimeSpan.FromSeconds(2)))
            .RunStreaming(
                problem,
                RandomNumberGenerator.Create(42),
                ct: TestContext.Current.CancellationToken)
            .ToList();

        results.Count.ShouldBe(1);
        results.Single().Population.Solutions.Length.ShouldBe(5);
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
            TimeSpan.FromSeconds(3),
            new AdvancingTimeProvider(TimeSpan.FromSeconds(2)))
            .RunStreaming(
                problem,
                RandomNumberGenerator.Create(42),
                ct: TestContext.Current.CancellationToken)
            .ToList();

        results.Count.ShouldBe(2);
        internalTerminator.CheckedStateCount.ShouldBe(2);
        internalTerminator.HasTerminated.ShouldBeFalse();
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
            .RunStreaming(
                problem,
                RandomNumberGenerator.Create(42),
                ct: TestContext.Current.CancellationToken)
            .ToList();

        results.Count.ShouldBe(2);
    }

    [Fact]
    public void WithMaxCount_CanObserveMutatedGenotypes()
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
                observedOperator.CountMutatedGenotypes(counter))
            .RunStreaming(
                problem,
                RandomNumberGenerator.Create(42),
                ct: TestContext.Current.CancellationToken)
            .ToList();

        results.Count.ShouldBe(3);
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
            .RunStreaming(
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
        var externallyStoppedAlgorithm = new StateTerminatedAlgorithm<
            RealVector,
            RealVectorSearchSpace,
            TestFunctionProblem,
            PopulationState<RealVector>>
        {
            Algorithm = algorithm,
            Terminator = new AfterOperatorCountTerminator<RealVector>(counter, maximumCount: 3)
        };

        var results = externallyStoppedAlgorithm.RunStreaming(
            problem,
            RandomNumberGenerator.Create(42),
            ct: TestContext.Current.CancellationToken).ToList();

        results.Count.ShouldBe(2);
        counter.CurrentCount.ShouldBe(3);
    }

    [Fact]
    public void AfterOperatorCountTerminator_Throws_WhenMaximumCountIsNotPositive()
    {
        var counter = new ObservationCounter();

        Should.Throw<ArgumentOutOfRangeException>(() =>
            new AfterOperatorCountTerminator<RealVector>(counter, maximumCount: 0));
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
    public void AfterOperatorDurationTerminator_Throws_WhenMaximumDurationIsNotPositive()
    {
        var duration = new ObservationDuration();

        Should.Throw<ArgumentOutOfRangeException>(() =>
            new AfterOperatorDurationTerminator<RealVector>(duration, maximumDuration: TimeSpan.Zero));
    }

    [Fact]
    public void Constructor_Throws_WhenMaximumCountIsNotPositive()
    {
        var problem = CreateProblem();
        var algorithm = CreateAlgorithm(problem);

        Should.Throw<ArgumentOutOfRangeException>(() =>
            new OperatorBudgetAlgorithm<
                RealVector,
                RealVectorSearchSpace,
                TestFunctionProblem,
                PopulationState<RealVector>,
                IEvaluator<RealVector, RealVectorSearchSpace, TestFunctionProblem>,
                IEvaluatorInstance<RealVector, RealVectorSearchSpace, TestFunctionProblem>>
            {
                Algorithm = algorithm,
                ObservedOperator = algorithm.Evaluator,
                MaximumCount = 0,
                CountedOperatorFactory = static (observedOperator, counter) =>
                    observedOperator.CountEvaluatorCalls(counter)
            });
    }

    [Fact]
    public void DurationBudgetConstructor_Throws_WhenMaximumDurationIsNotPositive()
    {
        var problem = CreateProblem();
        var algorithm = CreateAlgorithm(problem);

        Should.Throw<ArgumentOutOfRangeException>(() =>
            new OperatorDurationBudgetAlgorithm<
                RealVector,
                RealVectorSearchSpace,
                TestFunctionProblem,
                PopulationState<RealVector>,
                IEvaluator<RealVector, RealVectorSearchSpace, TestFunctionProblem>,
                IEvaluatorInstance<RealVector, RealVectorSearchSpace, TestFunctionProblem>>
            {
                Algorithm = algorithm,
                ObservedOperator = algorithm.Evaluator,
                MaximumDuration = TimeSpan.Zero,
                MeasuredOperatorFactory = static (observedOperator, duration, timeProvider) =>
                    observedOperator.MeasureEvaluatorDuration(duration, timeProvider)
            });
    }

    private static TestFunctionProblem CreateProblem()
    {
        return new TestFunctionProblem(new SphereFunction(dimension: 3));
    }

    private static GeneticAlgorithm<RealVector, RealVectorSearchSpace, TestFunctionProblem> CreateAlgorithm(
        TestFunctionProblem problem)
    {
        return new GeneticAlgorithm<RealVector, RealVectorSearchSpace, TestFunctionProblem>
        {
            PopulationSize = 5,
            Creator = new UniformDistributedCreator(problem.SearchSpace),
            Crossover = new SinglePointCrossover(),
            Mutator = new GaussianMutator(0.1, 0.1),
            MutationRate = 0.5,
            Selector = new RandomSelector<RealVector>(),
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
        : StatelessTerminator<RealVector, RealVectorSearchSpace, TestFunctionProblem, PopulationState<RealVector>>
    {
        public int CheckedStateCount { get; private set; }
        public bool HasTerminated { get; private set; }

        public override bool IsTerminalState(
            PopulationState<RealVector> state,
            RealVectorSearchSpace searchSpace,
            TestFunctionProblem problem)
        {
            CheckedStateCount++;
            HasTerminated = CheckedStateCount >= StopOnCheckedStateCount;
            return HasTerminated;
        }
    }
}
