using System.Runtime.CompilerServices;
using HEAL.HeuristicLib.Encodings.RealVectors;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Operators.Interceptors;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Problems.TestFunctions;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.Tests.TestSupport.Mocks;
using SinglePointCrossover = HEAL.HeuristicLib.Encodings.RealVectors.SinglePointCrossover;
using UniformDistributedCreator = HEAL.HeuristicLib.Encodings.RealVectors.UniformDistributedCreator;

namespace HEAL.HeuristicLib.Tests.Algorithms.MetaAlgorithms;

public class CycleAlgorithmTests
{
    [Fact]
    public void CycleAlgorithm_RequiresAtLeastOneAlgorithm()
    {
        var exception = Should.Throw<ArgumentException>(() =>
            new CycleAlgorithm<int, PopulationState<int>>([]));

        exception.ParamName.ShouldBe("algorithms");
    }

    /// <summary>
    /// A nonpositive cycle limit is a stable value rather than a rejected one: it runs no cycles.
    /// </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void CycleAlgorithm_WithNonpositiveMaximumCycles_RunsNoCycles(int maximumCycles)
    {
        var problem = MetaAlgorithmTestHelpers.CreateIntegerProblem();
        var cycle = CycleAlgorithm.Create(new AdditiveStepAlgorithm(1)) with
        {
            MaximumCycles = maximumCycles
        };

        cycle.RunStreaming(problem, RandomNumberGenerator.Create(42), ct: TestContext.Current.CancellationToken).ShouldBeEmpty();
    }

    [Fact]
    public void CycleAlgorithm_WithoutAlgorithms_IsReportedWhenBuilt()
    {
        var problem = MetaAlgorithmTestHelpers.CreateIntegerProblem();
        var cycle = CycleAlgorithm.Create(new AdditiveStepAlgorithm(1)) with
        {
            Algorithms = []
        };

        Should.Throw<InvalidOperationException>(() =>
                cycle.RunStreaming(problem, RandomNumberGenerator.Create(42), ct: TestContext.Current.CancellationToken).ToList())
            .Message.ShouldContain("at least one algorithm");
    }

    [Fact]
    public void CycleAlgorithm_RetriesCyclesWithoutProgressUntilMaximumCycles()
    {
        var problem = MetaAlgorithmTestHelpers.CreateIntegerProblem();
        var algorithm = new NoProgressAlgorithm();
        var cycle = CycleAlgorithm.Create(algorithm) with
        {
            MaximumCycles = 3
        };

        var states = cycle.RunStreaming(problem, RandomNumberGenerator.Create(42), ct: TestContext.Current.CancellationToken).ToList();

        states.ShouldBeEmpty();
        algorithm.ExecutionCount.ShouldBe(3);
    }

    [Fact]
    public void CycleAlgorithm_ContinuesWhenALaterChildProducesProgress()
    {
        var problem = MetaAlgorithmTestHelpers.CreateIntegerProblem();
        var firstAlgorithm = new NoProgressAlgorithm();
        var cycle = firstAlgorithm.CycleWith(new AdditiveStepAlgorithm(1), maximumCycles: 2);

        var states = cycle.RunStreaming(problem, RandomNumberGenerator.Create(42), ct: TestContext.Current.CancellationToken).ToList();

        states.Select(MetaAlgorithmTestHelpers.StateCandidate).ShouldBe([1, 2]);
    }

    [Fact]
    public void CycleAlgorithm_ChecksCancellationBeforeCreatingAChildExecution()
    {
        var problem = MetaAlgorithmTestHelpers.CreateIntegerProblem();
        var evaluator = new CountingResolutionEvaluator();
        var algorithm = new CountingExecutionAlgorithm(1, evaluator);
        var cycle = CycleAlgorithm.Create(algorithm);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        Should.Throw<OperationCanceledException>(() => cycle.RunStreaming(problem, RandomNumberGenerator.Create(42), ct: cts.Token).ToList());

        algorithm.ExecutionCount.ShouldBe(0);
    }

    [Fact]
    public void CycleAlgorithm_RunStreaming_RepeatsStagesAndPassesStateAcrossCycles()
    {
        var problem = MetaAlgorithmTestHelpers.CreateIntegerProblem();
        var cycle = new AdditiveStepAlgorithm(1).CycleWith(new AdditiveStepAlgorithm(10), maximumCycles: 2);

        var states = cycle.RunStreaming(problem, RandomNumberGenerator.Create(42), ct: TestContext.Current.CancellationToken).ToList();

        states.Select(MetaAlgorithmTestHelpers.StateCandidate).ShouldBe([1, 11, 12, 22]);
        states.Select(MetaAlgorithmTestHelpers.StateObjective).ShouldBe([1.0, 11.0, 12.0, 22.0]);
    }

    [Fact]
    public void CycleAlgorithm_WithInnerGeneticAlgorithmBudgetAndExternalEarlyStopping_YieldsPartialThirdCycle()
    {
        var problem = new TestFunctionProblem(new SphereFunction(dimension: 2));
        var ga = CreateStampedGeneticAlgorithm(problem) with
        {
            MaximumGenerations = 3
        };
        var cycle = CycleAlgorithm.Create(ga) with
        {
            MaximumCycles = 5
        };

        var states = cycle.TerminatedAfterIterations(8)
            .RunStreaming(problem, RandomNumberGenerator.Create(42), ct: TestContext.Current.CancellationToken)
            .ToList();

        states.Count.ShouldBe(8);
        states.Select(GetStateStamp).ShouldBe([1.0, 2.0, 3.0, 1.0, 2.0, 3.0, 1.0, 2.0]);
    }

    [Theory]
    [InlineData(true, 2)]
    [InlineData(false, 1)]
    public void CycleAlgorithm_SharingItsAlgorithm_KeepsOneExecutorWithPrivateChildren(bool newExecutionInstancesPerCycle, int expectedAlgorithmExecutions)
    {
        var problem = MetaAlgorithmTestHelpers.CreateIntegerProblem();
        var evaluator = new CountingResolutionEvaluator();
        var algorithm = new CountingExecutionAlgorithm(1, evaluator);
        var fresh = CycleAlgorithm.Create(algorithm) with { MaximumCycles = 2 };
        IAlgorithm<int, PopulationState<int>> cycle = newExecutionInstancesPerCycle ? fresh : fresh.Sharing(algorithm);
        var scope = ResolutionScope.Create();
        _ = scope.Resolve<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>>(evaluator, problem);
        var cycleExecutor = scope.Resolve<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>, PopulationState<int>>(cycle, problem);

        var states = cycleExecutor.RunStreaming(RandomNumberGenerator.Create(42), ct: TestContext.Current.CancellationToken).ToList();

        states.Select(MetaAlgorithmTestHelpers.StateCandidate).ShouldBe([1, 2]);
        algorithm.ExecutionCount.ShouldBe(expectedAlgorithmExecutions);
        evaluator.ExecutionCount.ShouldBe(1 + expectedAlgorithmExecutions);
    }

    /// <summary>
    /// A request for a narrower problem type uses the scope's Cycle executor, which serves it, with its shared executors.
    /// </summary>
    [Theory]
    [InlineData(false, 4)]
    [InlineData(true, 1)]

    public void CycleAlgorithm_RequestedForANarrowerProblem_UsesTheExecutorOfItsScope(bool shared, int expectedAlgorithmExecutions)
    {
        var problem = (FuncProblem<int, DummySearchSpace<int>>)MetaAlgorithmTestHelpers.CreateIntegerProblem();
        var algorithm = new CountingExecutionAlgorithm(1, new CountingResolutionEvaluator());
        var fresh = CycleAlgorithm.Create(algorithm) with { MaximumCycles = 2 };
        IAlgorithm<int, PopulationState<int>> cycle = shared ? fresh.Sharing(algorithm) : fresh;
        var scope = ResolutionScope.Create();
        var general = scope.Resolve<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>, PopulationState<int>>(cycle, problem);
        var concrete = scope.Resolve<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>, PopulationState<int>>(cycle, problem);
        concrete.ShouldBeSameAs(general);

        general.RunStreaming(RandomNumberGenerator.Create(42), ct: TestContext.Current.CancellationToken).Count().ShouldBe(2);
        concrete.RunStreaming(RandomNumberGenerator.Create(42), ct: TestContext.Current.CancellationToken).Count().ShouldBe(2);

        algorithm.ExecutionCount.ShouldBe(expectedAlgorithmExecutions);
    }

    private static GeneticAlgorithm<RealVector> CreateStampedGeneticAlgorithm(
      TestFunctionProblem problem)
    {
        return new GeneticAlgorithm<RealVector>
        {
            PopulationSize = 4,
            Creator = new UniformDistributedCreator(problem.SearchSpace),
            Crossover = new SinglePointCrossover(),
            Mutator = new GaussianMutator(0.1, 0.1),
            MutationRate = 0.5,
            Selector = RandomSelector.For(problem),
            Elites = 0,
            Interceptor = new YieldedStateStampingInterceptor()
        };
    }

    private static double GetStateStamp(PopulationState<RealVector> state) =>
      state.Population.EvaluatedCandidates[0].ObjectiveVector[0];

    private sealed record NoProgressAlgorithm
        : Algorithm<NoProgressAlgorithm, int, PopulationState<int>>
    {
        public int ExecutionCount { get; private set; }

        // Counts the executors it builds, one per activation.
        protected override IAlgorithmExecutor<PopulationState<int>> CreateExecutor<TSearchSpace, TProblem>(
            ResolutionScope<int, TSearchSpace, TProblem, PopulationState<int>> scope)
        {
            ExecutionCount++;
            return new Executor();
        }

        private sealed class Executor : AlgorithmExecutor<PopulationState<int>>
        {

            public override async IAsyncEnumerable<PopulationState<int>> RunStreamingAsync(IRandomNumberGenerator random, PopulationState<int>? initialState = null, [EnumeratorCancellation] CancellationToken ct = default)
            {
                await Task.CompletedTask;
                yield break;
            }
        }
    }

    private sealed record YieldedStateStampingInterceptor
        : Interceptor<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem, PopulationState<RealVector>>
    {

        protected override IInterceptionExecutor<PopulationState<RealVector>> CreateExecutor(ResolutionScope<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem, PopulationState<RealVector>> scope) =>
        new Executor();

        private sealed class Executor
            : InterceptionExecutor<PopulationState<RealVector>>
        {
            private int yieldedStateCount;

            public override PopulationState<RealVector> Transform(PopulationState<RealVector> currentState, PopulationState<RealVector>? previousState, IRandomNumberGenerator random)
            {
                yieldedStateCount++;
                var objectiveVector = new ObjectiveVector(yieldedStateCount);
                var stampedCandidates = currentState.Population.EvaluatedCandidates
                                .Select(solution => EvaluatedCandidate.From(solution.Candidate, objectiveVector));

                return currentState with
                {
                    Population = Population.From(stampedCandidates)
                };
            }
        }
    }
}
