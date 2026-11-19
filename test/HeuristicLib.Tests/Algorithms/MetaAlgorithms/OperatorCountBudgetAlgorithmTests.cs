using HEAL.HeuristicLib.Analysis;
using HEAL.HeuristicLib.Encodings.RealVectors;
using HEAL.HeuristicLib.Inspection;
using HEAL.HeuristicLib.Operators.Evaluators;
using HEAL.HeuristicLib.Operators.Mutators;
using HEAL.HeuristicLib.Operators.Terminators;
using HEAL.HeuristicLib.Problems.TestFunctions;
using HEAL.HeuristicLib.Tests.TestSupport;
using SinglePointCrossover = HEAL.HeuristicLib.Encodings.RealVectors.SinglePointCrossover;
using UniformDistributedCreator = HEAL.HeuristicLib.Encodings.RealVectors.UniformDistributedCreator;

namespace HEAL.HeuristicLib.Tests.Algorithms.MetaAlgorithms;

public class OperatorCountBudgetAlgorithmTests
{
    [Fact]
    public void TimeBasedConfigurations_DeclareOnlyTheirDataAndCompareEqual()
    {
        var problem = CreateProblem();
        var firstTerminator = AfterElapsedTimeTerminator.For(problem, TimeSpan.FromSeconds(3));
        var secondTerminator = AfterElapsedTimeTerminator.For(problem, TimeSpan.FromSeconds(3));
        AfterElapsedTimeTerminator<RealVector>.Parameters.Select(parameter => parameter.Name)
            .ShouldBe([nameof(firstTerminator.MaximumElapsedTime)]);
        firstTerminator.ShouldBe(secondTerminator);
        firstTerminator.ShouldNotBeSameAs(secondTerminator);

        var algorithm = CreateAlgorithm(problem);
        var firstBudget = algorithm.LimitedToDuration(TimeSpan.FromSeconds(3));
        var secondBudget = algorithm.LimitedToDuration(TimeSpan.FromSeconds(3));
        AlgorithmDurationBudgetAlgorithm<RealVector, PopulationState<RealVector>>.Parameters.Select(parameter => parameter.Name)
            .ShouldBe([nameof(firstBudget.Algorithm), nameof(firstBudget.MaximumDuration)]);
        firstBudget.ShouldBe(secondBudget);
        firstBudget.ShouldNotBeSameAs(secondBudget);
    }

    [Fact]
    public void CombinedInvocationBudgetCountsMutationAndCrossoverOncePerCallAcrossIndependentRuns()
    {
        var problem = CreateProblem();
        var algorithm = CreateAlgorithm(problem) with { MaximumGenerations = 6, MutationRate = 1.0 };
        var budget = algorithm.LimitedToCount(3, (g, calls) => g.Substitute(g.Mutator.Invocations.Counting(calls), g.Crossover.Invocations.Counting(calls)));
        var calls = budget.Counter;
        var constructed = new OperatorCountBudgetAlgorithm<RealVector, PopulationState<RealVector>>(
            algorithm with { Mutator = algorithm.Mutator.Counted(calls), Crossover = algorithm.Crossover.Counted(calls) }, calls, 3);
        constructed.ShouldBe(budget);

        for (var trial = 0; trial < 2; trial++)
            budget.RunStreaming(problem, RandomNumberGenerator.Create(42), ct: TestContext.Current.CancellationToken).Count().ShouldBe(3);
        constructed.RunStreaming(problem, RandomNumberGenerator.Create(42), ct: TestContext.Current.CancellationToken).Count().ShouldBe(3);
    }

    [Fact]
    public void CombinedCandidateBudgetSumsWorkFromDifferentOperations()
    {
        var problem = CreateProblem();
        var algorithm = CreateAlgorithm(problem) with { MaximumGenerations = 6, MutationRate = 1.0 };
        var budget = algorithm.LimitedToCount(6, (g, candidates) => g.Substitute(
            g.Mutator.Invocations.CountingCandidates(candidates),
            g.Crossover.Invocations.CountingCandidates(candidates)));
        var counter = budget.Counter;
        var constructed = new OperatorCountBudgetAlgorithm<RealVector, PopulationState<RealVector>>(
            algorithm with
            {
                Mutator = algorithm.Mutator.Counted(counter, OperatorCountMetric.Candidates),
                Crossover = algorithm.Crossover.Counted(counter, OperatorCountMetric.Candidates)
            }, counter, 6);
        constructed.ShouldBe(budget);
        constructed.RunStreaming(problem, RandomNumberGenerator.Create(42), ct: TestContext.Current.CancellationToken).Count().ShouldBe(2);
    }

    [Fact]
    public void CombinedDurationBudgetSumsMutationAndCrossoverWithConstructorParity()
    {
        var problem = CreateProblem();
        var algorithm = CreateAlgorithm(problem) with { MaximumGenerations = 6, MutationRate = 1.0 };
        var clock = new AdvancingTimeProvider(TimeSpan.FromSeconds(2));
        var budget = algorithm.LimitedToDuration(TimeSpan.FromSeconds(3), (g, time) => g.Substitute(g.Mutator.Invocations.Timing(time), g.Crossover.Invocations.Timing(time)));
        var time = budget.Duration;
        var constructed = new OperatorDurationBudgetAlgorithm<RealVector, PopulationState<RealVector>>(
            algorithm with { Mutator = algorithm.Mutator.Timed(time), Crossover = algorithm.Crossover.Timed(time) }, time, TimeSpan.FromSeconds(3));
        constructed.ShouldBe(budget);
        budget.RunStreamingWithTime(problem, RandomNumberGenerator.Create(42), clock, ct: TestContext.Current.CancellationToken).Count().ShouldBe(2);
        constructed.RunStreamingWithTime(problem, RandomNumberGenerator.Create(42), clock, ct: TestContext.Current.CancellationToken).Count().ShouldBe(2);
    }

    [Fact]
    public void GeneralDurationBudgetAndSingleSelection_StopAtTheirOwnMeasuredWork()
    {
        var problem = CreateProblem();
        var algorithm = CreateAlgorithm(problem) with { MaximumGenerations = 5, MutationRate = 1.0 };
        var maximumDuration = TimeSpan.FromSeconds(6);
        var combined = algorithm.LimitedToDuration(maximumDuration,
            (g, time) => g.Substitute(g.Evaluator.Invocations.Timing(time), g.Mutator.Invocations.Timing(time)));
        var single = algorithm.LimitedToDuration(algorithm.Evaluator, maximumDuration);
        var constructed = new OperatorDurationBudgetAlgorithm<RealVector, PopulationState<RealVector>>(
            algorithm.Timing(algorithm.Evaluator, out var duration), duration, maximumDuration);
        var clock = new AdvancingTimeProvider(TimeSpan.FromSeconds(2));

        combined.RunStreamingWithTime(problem, RandomNumberGenerator.Create(42), clock, ct: TestContext.Current.CancellationToken).Count().ShouldBe(2);
        single.RunStreamingWithTime(problem, RandomNumberGenerator.Create(42), clock, ct: TestContext.Current.CancellationToken).Count().ShouldBe(3);
        constructed.RunStreamingWithTime(problem, RandomNumberGenerator.Create(42), clock, ct: TestContext.Current.CancellationToken).Count().ShouldBe(3);
    }

    [Fact]
    public void DurationAccountingAndTheTerminator_UseOneExactlyMeasuredResultPerRun()
    {
        var problem = CreateProblem();
        var algorithm = CreateAlgorithm(problem) with { MaximumGenerations = 5 };
        var duration = new Duration();
        var configured = algorithm.TerminatedBy(AfterOperatorDurationTerminator.For(problem, duration, TimeSpan.FromTicks(2)))
            .Substitute(algorithm.Evaluator.Invocations.Timing(duration));
        var clock = new AdvancingTimeProvider(TimeSpan.FromTicks(1));
        var first = new AlgorithmRun<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem, PopulationState<RealVector>>(
            configured, problem, RandomNumberGenerator.Create(42), clock);
        var second = new AlgorithmRun<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem, PopulationState<RealVector>>(
            configured, problem, RandomNumberGenerator.Create(42), clock);

        first.Stream(cancellationToken: TestContext.Current.CancellationToken).Count().ShouldBe(2);
        second.Stream(cancellationToken: TestContext.Current.CancellationToken).Count().ShouldBe(2);
        first.Result(duration).CurrentDuration.ShouldBe(TimeSpan.FromTicks(2));
        second.Result(duration).CurrentDuration.ShouldBe(TimeSpan.FromTicks(2));
        first.Result(duration).ShouldNotBeSameAs(second.Result(duration));
    }

    [Fact]
    public void WithMaxEvaluatorCalls_StopsAfterObservedEvaluatorCallUsage()
    {
        var problem = CreateProblem();
        var algorithm = CreateAlgorithm(problem) with
        {
            MaximumGenerations = 5
        };

        var results = algorithm.LimitedToInvocations(algorithm.Evaluator, 1).RunStreaming(
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

        var results = algorithm.LimitedToInvocations(algorithm.Evaluator, 2).RunStreaming(
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

        var results = algorithm.LimitedToCandidates(algorithm.Evaluator, 2).RunStreaming(
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

        var results = algorithm.LimitedToCandidates(algorithm.Evaluator, 6).RunStreaming(
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
            TimeSpan.FromSeconds(3))
            .RunStreamingWithTime(
                problem,
                RandomNumberGenerator.Create(42),
                new AdvancingTimeProvider(TimeSpan.FromSeconds(2)),
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
            TimeSpan.FromSeconds(1))
            .RunStreamingWithTime(
                problem,
                RandomNumberGenerator.Create(42),
                new AdvancingTimeProvider(TimeSpan.FromSeconds(2)),
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
            TimeSpan.FromSeconds(3))
            .RunStreamingWithTime(
                problem,
                RandomNumberGenerator.Create(42),
                new AdvancingTimeProvider(TimeSpan.FromSeconds(2)),
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
            TimeSpan.FromSeconds(1))
            .RunStreamingWithTime(
                problem,
                RandomNumberGenerator.Create(42),
                new AdvancingTimeProvider(TimeSpan.FromSeconds(2)),
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
            TimeSpan.FromSeconds(3))
            .RunStreamingWithTime(
                problem,
                RandomNumberGenerator.Create(42),
                new AdvancingTimeProvider(TimeSpan.FromSeconds(2)),
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
            TimeSpan.FromSeconds(1))
            .RunStreamingWithTime(
                problem,
                RandomNumberGenerator.Create(42),
                new AdvancingTimeProvider(TimeSpan.FromSeconds(2)),
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
            TimeSpan.FromSeconds(3))
            .RunStreamingWithTime(
                problem,
                RandomNumberGenerator.Create(42),
                new AdvancingTimeProvider(TimeSpan.FromSeconds(2)),
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
            TimeSpan.FromSeconds(10))
            .RunStreamingWithTime(
                problem,
                RandomNumberGenerator.Create(42),
                new AdvancingTimeProvider(TimeSpan.FromSeconds(2)),
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
            .RunStreaming(
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
            .RunStreaming(
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
            TimeSpan.FromSeconds(1))
            .RunStreamingWithTime(
                problem,
                RandomNumberGenerator.Create(42),
                new AdvancingTimeProvider(TimeSpan.FromSeconds(2)),
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
            .RunStreaming(
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
            .RunStreaming(
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
            TimeSpan.FromSeconds(1))
            .RunStreamingWithTime(
                problem,
                RandomNumberGenerator.Create(42),
                new AdvancingTimeProvider(TimeSpan.FromSeconds(2)),
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
            .RunStreaming(
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
            .RunStreaming(
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
            TimeSpan.FromSeconds(1))
            .RunStreamingWithTime(
                problem,
                RandomNumberGenerator.Create(42),
                new AdvancingTimeProvider(TimeSpan.FromSeconds(2)),
                ct: TestContext.Current.CancellationToken)
            .ToList();

        results.Count.ShouldBe(2);
    }

    // One counter counts the evaluator where it is attached and the mutator where it is placed explicitly, and the
    // terminator reads that one run result.
    [Fact]
    public void AfterOperatorCountTerminator_CanUseSharedCounterAcrossObservedOperators()
    {
        var problem = CreateProblem();
        var baseAlgorithm = CreateAlgorithm(problem);
        var counter = new Counter();
        var algorithm = baseAlgorithm with
        {
            MaximumGenerations = 5,
            MutationRate = 1.0,
            Mutator = baseAlgorithm.Mutator.Counted(counter)
        };
        var externallyStoppedAlgorithm = algorithm.TerminatedBy(AfterOperatorCountTerminator.For(problem, counter, maximumCount: 3));

        var run = externallyStoppedAlgorithm.CreateRun(problem, RandomNumberGenerator.Create(42), [baseAlgorithm.Evaluator.Invocations.Counting(counter)]);
        var results = run.Stream(cancellationToken: TestContext.Current.CancellationToken).ToList();

        results.Count.ShouldBe(2);
        run.Result(counter).CurrentCount.ShouldBe(3);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void AfterOperatorCountTerminator_IsImmediatelyTerminal_WhenMaximumCountIsNotPositive(int maximumCount)
    {
        var counter = new Counter();
        var terminator = new AfterOperatorCountTerminator<RealVector>(counter, maximumCount);

        IsTerminal(terminator, ResolutionScope.Create()).ShouldBeTrue();
    }

    [Fact]
    public void AfterOperatorDurationTerminator_IsTerminalAtMaximumDuration()
    {
        var duration = new Duration();
        var scope = ResolutionScope.Create();
        var terminator = new AfterOperatorDurationTerminator<RealVector>(
            duration,
            maximumDuration: TimeSpan.FromSeconds(2));

        scope.Results.Result(duration).AddDuration(TimeSpan.FromSeconds(1));
        IsTerminal(terminator, scope).ShouldBeFalse();

        scope.Results.Result(duration).AddDuration(TimeSpan.FromSeconds(1));
        IsTerminal(terminator, scope).ShouldBeTrue();
    }

    [Fact]
    public void AfterOperatorDurationTerminator_IsImmediatelyTerminal_WhenMaximumDurationIsNotPositive()
    {
        var duration = new Duration();
        var scope = ResolutionScope.Create();

        IsTerminal(new AfterOperatorDurationTerminator<RealVector>(duration, TimeSpan.Zero), scope).ShouldBeTrue();
        IsTerminal(new AfterOperatorDurationTerminator<RealVector>(duration, TimeSpan.FromTicks(-1)), scope).ShouldBeTrue();
    }

    private static bool IsTerminal(IOperator<Termination, RealVector> terminator, ResolutionScope scope)
    {
        var problem = CreateProblem();
        var executor = scope.For<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem, SingleSolutionState<RealVector>>(problem).Resolve(terminator);
        var state = SingleSolutionState.From(new RealVector(0.0), new ObjectiveVector(0));
        return executor.IsTerminalState(state, RandomNumberGenerator.Create(42));
    }

    /// <summary>
    /// A nonpositive budget is a stable value rather than a rejected one. The budget is checked after each produced
    /// state, so the stream stops after the first one.
    /// </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void RunStreaming_WithNonpositiveMaximumCount_StopsAfterFirstState(int maximumCount)
    {
        var problem = CreateProblem();
        var algorithm = CreateAlgorithm(problem);

        var budgeted =
            new OperatorCountBudgetAlgorithm<RealVector, PopulationState<RealVector>>(
                algorithm.Counting(algorithm.Evaluator, out var calls), calls, maximumCount);

        budgeted.RunStreaming(problem, RandomNumberGenerator.Create(42), ct: TestContext.Current.CancellationToken).Count().ShouldBe(1);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void OperatorDurationBudget_WithNonpositiveMaximumDuration_StopsAfterFirstState(int ticks)
    {
        var problem = CreateProblem();
        var algorithm = CreateAlgorithm(problem);

        var budgeted =
            new OperatorDurationBudgetAlgorithm<RealVector, PopulationState<RealVector>>(
                algorithm.Timing(algorithm.Evaluator, out var time), time, TimeSpan.FromTicks(ticks));

        budgeted.RunStreaming(problem, RandomNumberGenerator.Create(42), ct: TestContext.Current.CancellationToken).Count().ShouldBe(1);
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

        budgeted.RunStreaming(problem, RandomNumberGenerator.Create(42), ct: TestContext.Current.CancellationToken).Count().ShouldBe(1);
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
                 .CreateRun(problem, RandomNumberGenerator.Create(42), [analyzer])
                 .Complete(cancellationToken: TestContext.Current.CancellationToken);

        analyzer.ObservedCandidates.ShouldBeGreaterThan(0);
    }

    /// <summary>
    /// Analyzed first, then limited, a budget measures the operator it limits and not the analyzer watching that
    /// operator, because the budget's wrapper sits closer to the operator. Were it the other way round, the recording
    /// work would be charged to the budget and attaching an analyzer would shorten the run. Here only the observation
    /// advances the clock, each time by more than the whole budget.
    /// </summary>
    [Fact]
    public void ADurationBudget_WrapsTheOperatorRatherThanTheAnalyzerObservingIt()
    {
        var problem = CreateProblem();
        var algorithm = CreateAlgorithm(problem) with { MaximumGenerations = 3 };
        var clock = new ManualTimeProvider();
        var slowObservation = algorithm.Evaluator.Invocations.Observing(_ => clock.Advance(TimeSpan.FromMinutes(1)));
        var unbudgeted = algorithm.RunStreaming(problem, RandomNumberGenerator.Create(42), ct: TestContext.Current.CancellationToken).Count();

        var budgeted = algorithm.Substitute(slowObservation)
            .LimitedToDuration(algorithm.Evaluator, TimeSpan.FromSeconds(30))
            .RunStreamingWithTime(problem, RandomNumberGenerator.Create(42), clock, ct: TestContext.Current.CancellationToken).Count();

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
        var withoutBudget = Analyzer.BestSoFar(algorithm.Evaluator);
        var withBudget = Analyzer.BestSoFar(algorithm.Evaluator);

        var unbudgeted = algorithm.CreateRun(problem, RandomNumberGenerator.Create(42), [withoutBudget]);
        await unbudgeted.CompleteAsync(cancellationToken: TestContext.Current.CancellationToken);

        var budgeted = new AlgorithmRun<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem, PopulationState<RealVector>>(
            algorithm.LimitedToDuration(algorithm.Evaluator, TimeSpan.FromSeconds(30)).Substitute(withBudget),
            problem, RandomNumberGenerator.Create(42), new AdvancingTimeProvider(TimeSpan.FromMilliseconds(1)));
        await budgeted.CompleteAsync(cancellationToken: TestContext.Current.CancellationToken);

        budgeted.Result(withBudget).Count.ShouldBe(unbudgeted.Result(withoutBudget).Count);
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
        : ISubstitution
    {
        private readonly ISubstitution observing;

        public int ObservedCandidates { get; private set; }

        public EvaluationObservingAnalyzer(IOperator<Evaluation, RealVector> evaluator)
        {
            observing = evaluator.Invocations.Observing(Record);
        }

        public IReadOnlyList<Substitution> Expand() => observing.Expand();

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
