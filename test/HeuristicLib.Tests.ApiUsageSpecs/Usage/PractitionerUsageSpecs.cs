using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Algorithms.Evolutionary;
using HEAL.HeuristicLib.Algorithms.LocalSearch;
using HEAL.HeuristicLib.Algorithms.MetaAlgorithms;
using HEAL.HeuristicLib.Analysis;
using HEAL.HeuristicLib.Experiments;
using HEAL.HeuristicLib.Genotypes.Vectors;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Operators.Creators.PermutationCreators;
using HEAL.HeuristicLib.Operators.Creators.RealVectorCreators;
using HEAL.HeuristicLib.Operators.Crossovers;
using HEAL.HeuristicLib.Operators.Crossovers.PermutationCrossovers;
using HEAL.HeuristicLib.Operators.Crossovers.RealVectorCrossovers;
using HEAL.HeuristicLib.Operators.Evaluators;
using HEAL.HeuristicLib.Operators.Interceptors;
using HEAL.HeuristicLib.Operators.Mutators;
using HEAL.HeuristicLib.Operators.Mutators.RealVectorMutators;
using HEAL.HeuristicLib.Operators.Replacers;
using HEAL.HeuristicLib.Operators.Selectors;
using HEAL.HeuristicLib.Operators.Terminators;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems.TestFunctions;
using HEAL.HeuristicLib.Problems.TestFunctions.SingleObjectives;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces.Vectors;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Tests.ApiUsageSpecs.Usage;

public class PractitionerUsageSpecs
{
    [Fact]
    public void StatelessOperators_CanBeInvokedDirectly_WithoutInstantiation()
    {
        var problem = CreateRastriginProblem(dimension: 3);
        var realSearchSpace = new RealVectorSearchSpace(3, [-1.0], [1.0]);
        var integerSearchSpace = new IntegerVectorSearchSpace(3, [-2], [2]);

        var randomIntegerVector = RandomNumberGenerator.Create(2025).NextIntegerVectorUniform([-2], [2], length: 4);
        randomIntegerVector.Count.ShouldBe(4);
        randomIntegerVector.All(x => x is >= -2 and <= 2).ShouldBeTrue();

        var randomIntegerVectorFromSearchSpace = RandomNumberGenerator.Create(2025).NextIntegerVectorUniform(integerSearchSpace);
        randomIntegerVectorFromSearchSpace.Count.ShouldBe(3);
        randomIntegerVectorFromSearchSpace.All(x => x is >= -2 and <= 2).ShouldBeTrue();

        var randomRealVector = RandomNumberGenerator.Create(2025).NextRealVectorUniform([-0.5], [0.5], length: 3);
        randomRealVector.Count.ShouldBe(3);
        randomRealVector.All(x => x is >= -0.5 and <= 0.5).ShouldBeTrue();

        var randomRealVectorFromSearchSpace = RandomNumberGenerator.Create(2025).NextRealVectorUniform(realSearchSpace);
        randomRealVectorFromSearchSpace.Count.ShouldBe(3);
        randomRealVectorFromSearchSpace.All(x => x is >= -1.0 and <= 1.0).ShouldBeTrue();

        RandomNumberGenerator.Create(2025).NextBools(3, probability: 0.0).ShouldBe([false, false, false]);
        RandomNumberGenerator.Create(2025).NextNormals(3, mu: 0.25, sigma: 0.0).ShouldBe([0.25, 0.25, 0.25]);
        var randomValues = new double[3];
        RandomNumberGenerator.Create(2025).NextDoubles(randomValues, low: -1, high: 1);
        randomValues.All(x => x is >= -1 and < 1).ShouldBeTrue();

        var roundedVector = new RealVector(1.6, -2.8, 0.2).RoundToIntegerVector(new IntegerVector(-2), new IntegerVector(2));
        roundedVector.ShouldBe(new IntegerVector(2, -2, 0));

        var floorVector = new RealVector(1.6, -2.8, 0.2).FloorToIntegerVector(new IntegerVector(-2), new IntegerVector(2));
        floorVector.ShouldBe(new IntegerVector(1, -2, 0));

        var ceilingVector = new RealVector(1.2, -1.8, 0.2).CeilToIntegerVector(integerSearchSpace.Minimum, integerSearchSpace.Maximum);
        ceilingVector.ShouldBe(new IntegerVector(2, -1, 1));

        var ceiledRealVector = new RealVector(1.2, -1.8, 0.2).Ceil();
        ceiledRealVector.ShouldBe(new RealVector(2.0, -1.0, 1.0));

        var permutation = RandomPermutationCreator.Create(RandomNumberGenerator.Create(2026), 5);
        permutation.Count.ShouldBe(5);
        permutation.Order().ToArray().ShouldBe([0, 1, 2, 3, 4]);

        var directPermutation = RandomNumberGenerator.Create(2026).NextPermutation(5);
        directPermutation.Order().ToArray().ShouldBe([0, 1, 2, 3, 4]);

        var directPermutationFromSearchSpace = RandomNumberGenerator.Create(2026).NextPermutation(new PermutationSearchSpace(5));
        directPermutationFromSearchSpace.Order().ToArray().ShouldBe([0, 1, 2, 3, 4]);

        var integerVector = HeuristicLib.Operators.Creators.IntegerVectorCreators.UniformDistributedCreator.Create(
          RandomNumberGenerator.Create(2027),
          length: 4,
          minimum: -2,
          maximum: 2);
        integerVector.Count.ShouldBe(4);
        integerVector.All(x => x is >= -2 and <= 2).ShouldBeTrue();

        var searchSpaceIntegerVector = HeuristicLib.Operators.Creators.IntegerVectorCreators.UniformDistributedCreator.Create(
          integerSearchSpace,
          RandomNumberGenerator.Create(2027));
        searchSpaceIntegerVector.Count.ShouldBe(3);
        searchSpaceIntegerVector.All(x => x is >= -2 and <= 2).ShouldBeTrue();

        var boundedRealVector = UniformDistributedCreator.Create(
          RandomNumberGenerator.Create(2027),
          length: 3,
          minimum: [-0.5],
          maximum: [0.5]);
        boundedRealVector.Count.ShouldBe(3);
        boundedRealVector.All(x => x is >= -0.5 and <= 0.5).ShouldBeTrue();

        var boundedRealVectorFromSearchSpace = UniformDistributedCreator.Create(
          RandomNumberGenerator.Create(2027),
          realSearchSpace,
          minimum: [-0.5],
          maximum: [0.5]);
        boundedRealVectorFromSearchSpace.Count.ShouldBe(3);
        boundedRealVectorFromSearchSpace.All(x => x is >= -0.5 and <= 0.5).ShouldBeTrue();

        var normalRealVector = NormalDistributedCreator.Create(
          RandomNumberGenerator.Create(2028),
          length: 3,
          means: [0.25],
          sigmas: [0.0],
          minimum: [-1.0],
          maximum: [1.0]);
        normalRealVector.ShouldBe(RealVector.Repeat(0.25, 3));

        var normalRealVectorFromSearchSpace = NormalDistributedCreator.Create(
          RandomNumberGenerator.Create(2028),
          realSearchSpace,
          means: [0.25],
          sigmas: [0.0]);
        normalRealVectorFromSearchSpace.ShouldBe(RealVector.Repeat(0.25, 3));

        var randomNormalRealVectorFromSearchSpace = RandomNumberGenerator.Create(2028).NextRealVectorNormal(
          realSearchSpace,
          means: [0.25],
          sigmas: [0.0]);
        randomNormalRealVectorFromSearchSpace.ShouldBe(RealVector.Repeat(0.25, 3));

        var normalIntegerVector = HeuristicLib.Operators.Creators.IntegerVectorCreators.NormalDistributedCreator.Create(
          RandomNumberGenerator.Create(2029),
          length: 3,
          means: [1.6],
          sigmas: [0.0],
          minimum: integerSearchSpace.Minimum,
          maximum: integerSearchSpace.Maximum);
        normalIntegerVector.ShouldBe(new IntegerVector(2, 2, 2));

        var normalIntegerVectorFromSearchSpace = HeuristicLib.Operators.Creators.IntegerVectorCreators.NormalDistributedCreator.Create(
          RandomNumberGenerator.Create(2029),
          integerSearchSpace,
          means: [1.6],
          sigmas: [0.0]);
        normalIntegerVectorFromSearchSpace.ShouldBe(new IntegerVector(2, 2, 2));

        var randomNormalIntegerVectorFromSearchSpace = RandomNumberGenerator.Create(2029).NextIntegerVectorNormal(
          integerSearchSpace,
          means: [1.6],
          sigmas: [0.0]);
        randomNormalIntegerVectorFromSearchSpace.ShouldBe(new IntegerVector(2, 2, 2));

        RealVector parent = [1.0, 2.0, 3.0];
        RealVector otherParent = [9.0, 9.0, 9.0];

        NoChangeMutator.Mutate(parent, RandomNumberGenerator.Create(2030)).ShouldBe(parent);
        var gaussianChild = GaussianMutator.Mutate(parent, RandomNumberGenerator.Create(2031), realSearchSpace, mutationRate: 1.0, mutationStrength: 100.0);
        realSearchSpace.Contains(gaussianChild).ShouldBeTrue();

        SelectFirstParentCrossover.Cross(Parents.From(parent, otherParent), RandomNumberGenerator.Create(2032)).ShouldBe(parent);
        SelectSecondParentCrossover.Cross(Parents.From(parent, otherParent), RandomNumberGenerator.Create(2032)).ShouldBe(otherParent);

        var edgeChild = EdgeRecombinationCrossover.Cross(
          [0, 1, 2, 3],
          [0, 2, 1, 3],
          RandomNumberGenerator.Create(2033));
        edgeChild.Order().ToArray().ShouldBe([0, 1, 2, 3]);

        var evaluations = DirectEvaluator.Evaluate([parent], RandomNumberGenerator.Create(2034), problem);
        evaluations.Count.ShouldBe(1);

        IReadOnlyList<EvaluatedCandidate<RealVector>> evaluatedCandidates =
        [
            EvaluatedCandidate.From(parent, new ObjectiveVector(2.0)),
            EvaluatedCandidate.From(otherParent, new ObjectiveVector(1.0))
        ];

        RandomSelector.Select(evaluatedCandidates, count: 2, RandomNumberGenerator.Create(2035)).Count.ShouldBe(2);
        ProportionalSelector.Select(evaluatedCandidates, problem.Objective, count: 2, RandomNumberGenerator.Create(2036), windowing: true).Count.ShouldBe(2);
        CommaSelectionReplacer.Replace(evaluatedCandidates, problem.Objective, count: 1).Single().ShouldBe(evaluatedCandidates[1]);

        IReadOnlyList<EvaluatedCandidate<RealVector>> offspring =
        [
            EvaluatedCandidate.From(RealVector.Create(5.0, 5.0, 5.0), new ObjectiveVector(0.5)),
            EvaluatedCandidate.From(RealVector.Create(7.0, 7.0, 7.0), new ObjectiveVector(3.0))
        ];
        var paretoReplacement = ParetoCrowdingReplacer.Replace(evaluatedCandidates, offspring, problem.Objective, count: 2, dominateOnEqualities: false);
        paretoReplacement.Select(solution => solution.ObjectiveVector[0]).Order().ToArray().ShouldBe([0.5, 1.0]);

        NeverTerminator.IsTerminalState().ShouldBeFalse();

        var state = SingleSolutionState.From(parent, evaluations[0]);
        IdentityInterceptor.Transform(state, previousState: null).ShouldBe(state);
    }

    [Fact]
    public async Task GeneticAlgorithm_BenchmarkExample_RunsToCompletion()
    {
        var problem = CreateRastriginProblem(dimension: 4);
        var algorithm = new GeneticAlgorithm<RealVector, RealVectorSearchSpace, TestFunctionProblem>
        {
            PopulationSize = 24,
            MaximumGenerations = 8,
            Creator = new UniformDistributedCreator(problem.SearchSpace),
            Crossover = new AlphaBetaBlendCrossover(alpha: 0.7),
            Mutator = new GaussianMutator(mutationRate: 0.2, mutationStrength: 0.15),
            Selector = TournamentSelector.For(problem, tournamentSize: 2),
            MutationRate = 0.2,
            Elites = 1
        };

        var finalState = await algorithm.CompleteAsync(
          problem,
          RandomNumberGenerator.Create(123),
          ct: TestContext.Current.CancellationToken);

        finalState.Population.EvaluatedCandidates.Length.ShouldBe(24);
        finalState.Population.EvaluatedCandidates.All(solution => problem.SearchSpace.Contains(solution.Candidate)).ShouldBeTrue();
    }

    [Fact]
    public void GeneticAlgorithm_InternalBudgetAndExternalEarlyStopping_AreDistinct()
    {
        var problem = CreateRastriginProblem(dimension: 4);
        var internallyCappedAlgorithm = CreateSimpleGeneticAlgorithm(problem) with
        {
            MaximumGenerations = 2
        };
        var externallyCappedAlgorithm = CreateSimpleGeneticAlgorithm(problem) with
        {
            MaximumGenerations = 5
        };

        var cannotExtendPastInternalCompletion = internallyCappedAlgorithm
          .WithMaxIterations(5)
          .Stream(
            problem,
            RandomNumberGenerator.Create(456),
            ct: TestContext.Current.CancellationToken)
          .ToList();
        var canStopEarlierThanInternalCompletion = externallyCappedAlgorithm
          .WithMaxIterations(2)
          .Stream(
            problem,
            RandomNumberGenerator.Create(456),
            ct: TestContext.Current.CancellationToken)
          .ToList();

        cannotExtendPastInternalCompletion.Count.ShouldBe(2);
        canStopEarlierThanInternalCompletion.Count.ShouldBe(2);
    }

    [Fact]
    public void GeneticAlgorithm_ExternalEarlyStopping_DoesNotInternallyCompleteAlgorithm()
    {
        var problem = CreateRastriginProblem(dimension: 4);
        var internalTerminator = new RecordingPopulationTerminator(5);
        var algorithm = CreateSimpleGeneticAlgorithm(problem) with
        {
            MaximumGenerations = 5,
            Terminator = internalTerminator
        };

        var earlyStoppedStates = algorithm
          .WithMaxIterations(2)
          .Stream(
            problem,
            RandomNumberGenerator.Create(789),
            ct: TestContext.Current.CancellationToken)
          .ToList();

        earlyStoppedStates.Count.ShouldBe(2);
        internalTerminator.CheckedStateCount.ShouldBe(2);
        internalTerminator.HasTerminated.ShouldBeFalse();
    }

    [Fact]
    public void GeneticAlgorithm_MaxEvaluatorCalls_IsExternalBudgetOverObservedEvaluatorUsage()
    {
        var problem = CreateRastriginProblem(dimension: 4);
        var internalTerminator = new RecordingPopulationTerminator(5);
        var algorithm = CreateSimpleGeneticAlgorithm(problem) with
        {
            MaximumGenerations = 5,
            Terminator = internalTerminator
        };

        var states = algorithm
            .WithMaxEvaluatorCalls(algorithm.Evaluator, 2)
            .Stream(
                problem,
                RandomNumberGenerator.Create(987),
                ct: TestContext.Current.CancellationToken)
            .ToList();

        states.Count.ShouldBe(2);
        states.All(state => state.Population.EvaluatedCandidates.Length == 16).ShouldBeTrue();
        internalTerminator.CheckedStateCount.ShouldBe(2);
        internalTerminator.HasTerminated.ShouldBeFalse();
    }

    [Fact]
    public void GeneticAlgorithm_EvaluatorCallAndEvaluatedGenotypeBudgets_CountDifferentUnits()
    {
        var problem = CreateRastriginProblem(dimension: 4);
        var algorithm = CreateSimpleGeneticAlgorithm(problem) with
        {
            MaximumGenerations = 5
        };

        var statesByEvaluatorCalls = algorithm
            .WithMaxEvaluatorCalls(algorithm.Evaluator, 2)
            .Stream(
                problem,
                RandomNumberGenerator.Create(987),
                ct: TestContext.Current.CancellationToken)
            .ToList();
        var statesByEvaluatedCandidates = algorithm
            .WithMaxEvaluatedCandidates(algorithm.Evaluator, 2)
            .Stream(
                problem,
                RandomNumberGenerator.Create(987),
                ct: TestContext.Current.CancellationToken)
            .ToList();

        statesByEvaluatorCalls.Count.ShouldBe(2);
        statesByEvaluatedCandidates.Count.ShouldBe(1);
        statesByEvaluatedCandidates.Single().Population.EvaluatedCandidates.Length.ShouldBe(16);
    }

    [Fact]
    public void GeneticAlgorithm_MaxEvaluatorDuration_IsExternalBudgetOverObservedEvaluatorWork()
    {
        var problem = CreateRastriginProblem(dimension: 4);
        var algorithm = CreateSimpleGeneticAlgorithm(problem) with
        {
            MaximumGenerations = 5
        };

        var states = algorithm
            .WithMaxEvaluatorDuration(
                algorithm.Evaluator,
                TimeSpan.FromSeconds(3),
                new AdvancingTimeProvider(TimeSpan.FromSeconds(2)))
            .Stream(
                problem,
                RandomNumberGenerator.Create(987),
                ct: TestContext.Current.CancellationToken)
            .ToList();

        states.Count.ShouldBe(2);
        states.All(state => state.Population.EvaluatedCandidates.Length == 16).ShouldBeTrue();
    }

    [Fact]
    public void GeneticAlgorithm_MaxAlgorithmDuration_IsExternalBudgetOverActiveStateProductionWork()
    {
        var problem = CreateRastriginProblem(dimension: 4);
        var algorithm = CreateSimpleGeneticAlgorithm(problem) with
        {
            MaximumGenerations = 5
        };

        var states = algorithm
            .WithMaxAlgorithmDuration(
                TimeSpan.FromSeconds(3),
                new AdvancingTimeProvider(TimeSpan.FromSeconds(2)))
            .Stream(
                problem,
                RandomNumberGenerator.Create(987),
                ct: TestContext.Current.CancellationToken)
            .ToList();

        states.Count.ShouldBe(2);
        states.All(state => state.Population.EvaluatedCandidates.Length == 16).ShouldBeTrue();
    }

    [Fact]
    public void GeneticAlgorithm_TypedOperatorBudgets_CanCountMutatorCallsOrMutatedCandidates()
    {
        var problem = CreateRastriginProblem(dimension: 4);
        var algorithm = CreateSimpleGeneticAlgorithm(problem) with
        {
            MaximumGenerations = 5,
            MutationRate = 1.0
        };

        var statesByMutatorCalls = algorithm
            .WithMaxMutatorCalls(
                algorithm.Mutator,
                maximumCalls: 1)
            .Stream(
                problem,
                RandomNumberGenerator.Create(987),
                ct: TestContext.Current.CancellationToken)
            .ToList();
        var statesByMutatedCandidates = algorithm
            .WithMaxMutatedCandidates(
                algorithm.Mutator,
                maximumCandidates: 20)
            .Stream(
                problem,
                RandomNumberGenerator.Create(987),
                ct: TestContext.Current.CancellationToken)
            .ToList();

        statesByMutatorCalls.Count.ShouldBe(2);
        statesByMutatorCalls.All(state => state.Population.EvaluatedCandidates.Length == 16).ShouldBeTrue();
        statesByMutatedCandidates.Count.ShouldBe(3);
        statesByMutatedCandidates.All(state => state.Population.EvaluatedCandidates.Length == 16).ShouldBeTrue();
    }

    [Fact]
    public void InstrumentationOperators_CanBeConstructedDirectlyOrThroughFluentMethods()
    {
        var problem = CreateRastriginProblem(dimension: 4);
        var mutator = CreateSimpleGeneticAlgorithm(problem).Mutator;
        var counter = new ObservationCounter();
        var duration = new ObservationDuration();

        var counted = new CountingMutator<RealVector, RealVectorSearchSpace, TestFunctionProblem>(mutator, counter, OperatorCountMetric.Candidates);
        var measured = mutator.MeasureMutatorDuration(duration);

        counted.ChildMutator.ShouldBeSameAs(mutator);
        counted.Counter.ShouldBeSameAs(counter);
        counted.Metric.ShouldBe(OperatorCountMetric.Candidates);
        measured.ChildMutator.ShouldBeSameAs(mutator);
        measured.Duration.ShouldBeSameAs(duration);
    }

    [Fact]
    public void GeneticAlgorithm_TypedOperatorDurationBudget_CanMeasureMutatorWork()
    {
        var problem = CreateRastriginProblem(dimension: 4);
        var algorithm = CreateSimpleGeneticAlgorithm(problem) with
        {
            MaximumGenerations = 5,
            MutationRate = 1.0
        };

        var states = algorithm
            .WithMaxMutatorDuration(
                algorithm.Mutator,
                TimeSpan.FromSeconds(3),
                new AdvancingTimeProvider(TimeSpan.FromSeconds(2)))
            .Stream(
                problem,
                RandomNumberGenerator.Create(987),
                ct: TestContext.Current.CancellationToken)
            .ToList();

        states.Count.ShouldBe(3);
        states.All(state => state.Population.EvaluatedCandidates.Length == 16).ShouldBeTrue();
    }

    [Fact]
    public void GeneticAlgorithm_GenericOperatorBudget_CanUseCustomCountedOperatorFactory()
    {
        var problem = CreateRastriginProblem(dimension: 4);
        var algorithm = CreateSimpleGeneticAlgorithm(problem) with
        {
            MaximumGenerations = 5,
            MutationRate = 1.0
        };

        var states = algorithm
            .WithMaxCount(
                algorithm.Mutator,
                maximumCount: 20,
                countedOperatorFactory: static (mutator, counter) =>
                    mutator.CountMutatedCandidates(counter))
            .Stream(
                problem,
                RandomNumberGenerator.Create(987),
                ct: TestContext.Current.CancellationToken)
            .ToList();

        states.Count.ShouldBe(3);
        states.All(state => state.Population.EvaluatedCandidates.Length == 16).ShouldBeTrue();
    }

    [Fact]
    public void GeneticAlgorithm_SharedOperatorCounter_CanDriveExternalEarlyStopping()
    {
        var problem = CreateRastriginProblem(dimension: 4);
        var counter = new ObservationCounter();
        var baseAlgorithm = CreateSimpleGeneticAlgorithm(problem) with
        {
            MaximumGenerations = 5,
            MutationRate = 1.0
        };
        var observedAlgorithm = baseAlgorithm with
        {
            Crossover = baseAlgorithm.Crossover.CountCrossoverCalls(counter),
            Mutator = baseAlgorithm.Mutator.CountMutatorCalls(counter)
        };
        var algorithm = observedAlgorithm.WithTerminator(AfterOperatorCountTerminator.For(problem, counter, maximumCount: 2));

        var states = algorithm.Stream(
            problem,
            RandomNumberGenerator.Create(987),
            ct: TestContext.Current.CancellationToken)
            .ToList();

        states.Count.ShouldBe(2);
        states.All(state => state.Population.EvaluatedCandidates.Length == 16).ShouldBeTrue();
        counter.CurrentCount.ShouldBeGreaterThanOrEqualTo(2);
    }

    [Fact]
    public async Task HillClimber_BenchmarkExample_RunsToCompletion()
    {
        var problem = CreateRastriginProblem(dimension: 4);
        IAlgorithm<RealVector, RealVectorSearchSpace, TestFunctionProblem, SingleSolutionState<RealVector>> algorithm = new HillClimber<RealVector, RealVectorSearchSpace, TestFunctionProblem>
        {
            Creator = new UniformDistributedCreator(problem.SearchSpace),
            Mutator = new GaussianMutator(mutationRate: 0.2, mutationStrength: 0.15),
            Direction = LocalSearchDirection.FirstImprovement,
            BatchSize = 4,
            MaxNeighbors = 12
        }.WithMaxIterations(10);

        var finalState = await algorithm.CompleteAsync(
          problem,
          RandomNumberGenerator.Create(321),
          ct: TestContext.Current.CancellationToken);

        problem.SearchSpace.Contains(finalState.EvaluatedCandidate.Candidate).ShouldBeTrue();
    }

    [Fact]
    public void HillClimber_StructuralCompletion_DoesNotRequireExternalIterationCap()
    {
        var problem = CreateRastriginProblem(dimension: 4);
        var algorithm = new HillClimber<RealVector, RealVectorSearchSpace, TestFunctionProblem>
        {
            Creator = new UniformDistributedCreator(problem.SearchSpace),
            Mutator = NoChangeMutator.For(problem),
            Direction = LocalSearchDirection.FirstImprovement,
            BatchSize = 4,
            MaxNeighbors = 12
        };

        var states = algorithm.Stream(
          problem,
          RandomNumberGenerator.Create(654),
          ct: TestContext.Current.CancellationToken).ToList();

        states.Count.ShouldBe(1);
        problem.SearchSpace.Contains(states.Single().EvaluatedCandidate.Candidate).ShouldBeTrue();
    }

    [Fact]
    public async Task RepeatedExecution_Example_RunsEachRepetition()
    {
        var problem = CreateRastriginProblem(dimension: 4);
        var algorithm = new HillClimber<RealVector, RealVectorSearchSpace, TestFunctionProblem>
        {
            Creator = new UniformDistributedCreator(problem.SearchSpace),
            Mutator = new GaussianMutator(mutationRate: 0.2, mutationStrength: 0.15),
            Direction = LocalSearchDirection.FirstImprovement,
            BatchSize = 4,
            MaxNeighbors = 12
        }.WithMaxIterations(6);
        var repeated = algorithm.Repeat(3);

        var results = await repeated.CompleteAsync(
          problem,
          RandomNumberGenerator.Create(999),
          cancellationToken: TestContext.Current.CancellationToken);

        results.Length.ShouldBe(3);
        results.Select(result => result.Trial.Key).ShouldBe([0, 1, 2]);
        results.All(result => problem.SearchSpace.Contains(result.State.EvaluatedCandidate.Candidate)).ShouldBeTrue();
    }

    [Fact]
    public async Task EvolutionStrategy_BenchmarkExample_RunsToCompletion()
    {
        var problem = CreateRastriginProblem(dimension: 4);
        var algorithm = new EvolutionStrategy<RealVector, RealVectorSearchSpace, TestFunctionProblem>
        {
            PopulationSize = 8,
            NumberOfChildren = 8,
            Strategy = EvolutionStrategyType.Plus,
            Creator = new UniformDistributedCreator(problem.SearchSpace),
            Mutator = new GaussianMutator(mutationRate: 0.2, mutationStrength: 0.15),
            Crossover = null,
            Selector = TournamentSelector.For(problem, tournamentSize: 2),
            MaximumGenerations = 5
        };

        var finalState = await algorithm.CompleteAsync(
          problem,
          RandomNumberGenerator.Create(555),
          ct: TestContext.Current.CancellationToken);

        finalState.Population.EvaluatedCandidates.Length.ShouldBe(8);
        finalState.Population.EvaluatedCandidates.All(solution => problem.SearchSpace.Contains(solution.Candidate)).ShouldBeTrue();
    }

    private static TestFunctionProblem CreateRastriginProblem(int dimension)
    {
        return new TestFunctionProblem(new RastriginFunction(dimension));
    }

    private static GeneticAlgorithm<RealVector, RealVectorSearchSpace, TestFunctionProblem> CreateSimpleGeneticAlgorithm(
      TestFunctionProblem problem)
    {
        return new GeneticAlgorithm<RealVector, RealVectorSearchSpace, TestFunctionProblem>
        {
            PopulationSize = 16,
            Creator = new UniformDistributedCreator(problem.SearchSpace),
            Crossover = new AlphaBetaBlendCrossover(alpha: 0.7),
            Mutator = new GaussianMutator(mutationRate: 0.2, mutationStrength: 0.15),
            Selector = TournamentSelector.For(problem, tournamentSize: 2),
            MutationRate = 0.2,
            Elites = 1
        };
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
}
