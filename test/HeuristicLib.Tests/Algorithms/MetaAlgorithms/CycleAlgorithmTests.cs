using System.Runtime.CompilerServices;
using HEAL.HeuristicLib.Encodings.RealVectors;
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
            new CycleAlgorithm<AdditiveStepAlgorithm, int, PopulationState<int>>([]));

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

        cycle.Stream(problem, RandomNumberGenerator.Create(42), ct: TestContext.Current.CancellationToken).ShouldBeEmpty();
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

        var states = cycle.Stream(problem, RandomNumberGenerator.Create(42), ct: TestContext.Current.CancellationToken).ToList();

        states.ShouldBeEmpty();
        algorithm.ExecutionCount.ShouldBe(3);
    }

    [Fact]
    public void CycleAlgorithm_ContinuesWhenALaterChildProducesProgress()
    {
        var problem = MetaAlgorithmTestHelpers.CreateIntegerProblem();
        var firstAlgorithm = new NoProgressAlgorithm();
        var cycle = firstAlgorithm.CycleWith(new AdditiveStepAlgorithm(1), maximumCycles: 2);

        var states = cycle.Stream(problem, RandomNumberGenerator.Create(42), ct: TestContext.Current.CancellationToken).ToList();

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

        Should.Throw<OperationCanceledException>(() => cycle.Stream(problem, RandomNumberGenerator.Create(42), ct: cts.Token).ToList());

        algorithm.ExecutionCount.ShouldBe(0);
    }

    [Fact]
    public void CycleAlgorithm_Stream_RepeatsStagesAndPassesStateAcrossCycles()
    {
        var problem = MetaAlgorithmTestHelpers.CreateIntegerProblem();
        var cycle = new AdditiveStepAlgorithm(1).CycleWith(new AdditiveStepAlgorithm(10), maximumCycles: 2);

        var states = cycle.Stream(problem, RandomNumberGenerator.Create(42), ct: TestContext.Current.CancellationToken).ToList();

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
            .Stream(problem, RandomNumberGenerator.Create(42), ct: TestContext.Current.CancellationToken)
            .ToList();

        states.Count.ShouldBe(8);
        states.Select(GetStateStamp).ShouldBe([1.0, 2.0, 3.0, 1.0, 2.0, 3.0, 1.0, 2.0]);
    }

    [Theory]
    [InlineData(true, 2)]
    [InlineData(false, 1)]
    public void CycleAlgorithm_UsesConfiguredAlgorithmExecutionLifecycleWhileReusingResolvedParentDependencies(bool newExecutionInstancesPerCycle, int expectedAlgorithmExecutions)
    {
        var problem = MetaAlgorithmTestHelpers.CreateIntegerProblem();
        var evaluator = new CountingResolutionEvaluator();
        var algorithm = new CountingExecutionAlgorithm(1, evaluator);
        var cycle = CycleAlgorithm.Create(algorithm) with
        {
            MaximumCycles = 2,
            ScopePolicy = newExecutionInstancesPerCycle ? ChildScopePolicy.Fresh : ChildScopePolicy.Shared
        };
        var scope = ResolutionScope.Create();
        _ = scope.Resolve<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>>(evaluator);
        var cycleExecutor = scope.Resolve<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>, PopulationState<int>>(cycle);

        var states = cycleExecutor.Stream(problem, RandomNumberGenerator.Create(42), ct: TestContext.Current.CancellationToken).ToList();

        states.Select(MetaAlgorithmTestHelpers.StateCandidate).ShouldBe([1, 2]);
        algorithm.ExecutionCount.ShouldBe(expectedAlgorithmExecutions);
        evaluator.ExecutionCount.ShouldBe(1);
    }

    /// <summary>
    /// The cycle keeps its child-scope selector as scoped state, so builds for different run types in one owning
    /// state select the same child states.
    /// </summary>
    [Theory]
    [InlineData(ChildScopePolicy.Fresh, 4)]
    [InlineData(ChildScopePolicy.Retained, 1)]
    [InlineData(ChildScopePolicy.Shared, 1)]
    public void CycleAlgorithm_BuiltForTwoRunTypesInOneScope_SelectsChildStatesThroughOneSelector(ChildScopePolicy policy, int expectedAlgorithmExecutions)
    {
        var problem = (FuncProblem<int, DummySearchSpace<int>>)MetaAlgorithmTestHelpers.CreateIntegerProblem();
        var algorithm = new CountingExecutionAlgorithm(1, new CountingResolutionEvaluator());
        var cycle = CycleAlgorithm.Create(algorithm) with
        {
            MaximumCycles = 2,
            ScopePolicy = policy
        };
        var scope = ResolutionScope.Create();
        var general = scope.Resolve<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>, PopulationState<int>>(cycle);
        var concrete = scope.Resolve<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>, PopulationState<int>>(cycle);

        general.Stream(problem, RandomNumberGenerator.Create(42), ct: TestContext.Current.CancellationToken).Count().ShouldBe(2);
        concrete.Stream(problem, RandomNumberGenerator.Create(42), ct: TestContext.Current.CancellationToken).Count().ShouldBe(2);

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
        protected override IAlgorithmExecutor<int, TSearchSpace, TProblem, PopulationState<int>> CreateExecutor<TSearchSpace, TProblem>(
            ResolutionScope<int, TSearchSpace, TProblem, PopulationState<int>> scope)
        {
            ExecutionCount++;
            return new Executor<TSearchSpace, TProblem>();
        }

        private sealed class Executor<TSearchSpace, TProblem> : AlgorithmExecutor<int, TSearchSpace, TProblem, PopulationState<int>>
            where TSearchSpace : class, ISearchSpace<int>
            where TProblem : class, IProblem<int, TSearchSpace>
        {
            public override async IAsyncEnumerable<PopulationState<int>> RunStreamingAsync(TProblem problem, IRandomNumberGenerator random, PopulationState<int>? initialState = null, [EnumeratorCancellation] CancellationToken ct = default)
            {
                await Task.CompletedTask;
                yield break;
            }
        }
    }

    private sealed record YieldedStateStampingInterceptor
        : StatefulInterceptor<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem, PopulationState<RealVector>, YieldedStateStampingInterceptor.State>
    {
        protected override State CreateScopedState() => new();

        protected override PopulationState<RealVector> Transform(
          PopulationState<RealVector> currentState,
          PopulationState<RealVector>? previousState,
          State operatorState,
          IRandomNumberGenerator random,
          BoundedRealVectorSearchSpace searchSpace,
          TestFunctionProblem problem)
        {
            operatorState.YieldedStateCount++;
            var objectiveVector = new ObjectiveVector(operatorState.YieldedStateCount);
            var stampedCandidates = currentState.Population.EvaluatedCandidates
                            .Select(solution => EvaluatedCandidate.From(solution.Candidate, objectiveVector));

            return currentState with
            {
                Population = Population.From(stampedCandidates)
            };
        }

        public sealed class State
        {
            public int YieldedStateCount { get; set; }
        }
    }
}
