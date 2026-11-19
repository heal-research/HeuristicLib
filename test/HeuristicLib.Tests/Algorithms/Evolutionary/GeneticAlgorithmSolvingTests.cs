using HEAL.HeuristicLib.Encodings.RealVectors;
using HEAL.HeuristicLib.Operators.Terminators;
using HEAL.HeuristicLib.Problems.TestFunctions;
using SinglePointCrossover = HEAL.HeuristicLib.Encodings.RealVectors.SinglePointCrossover;
using UniformDistributedCreator = HEAL.HeuristicLib.Encodings.RealVectors.UniformDistributedCreator;

namespace HEAL.HeuristicLib.Tests.Algorithms.Evolutionary;

public class GeneticAlgorithmSolvingTests
{
    [Fact]
    public void RunToCompletion_ReturnsPopulationWithinProblemSearchSpace()
    {
        var problem = CreateProblem();
        var algorithm = CreateAlgorithm(problem);

        var result = algorithm.RunToCompletion(
          problem,
          RandomNumberGenerator.Create(42),
          ct: TestContext.Current.CancellationToken);

        result.Population.EvaluatedCandidates.Count.ShouldBe(5);
        result.Population.EvaluatedCandidates.All(solution => problem.SearchSpace.Contains(solution.Candidate)).ShouldBeTrue();
    }

    [Fact]
    public void RunStreaming_YieldsConfiguredNumberOfPopulationStates()
    {
        var problem = CreateProblem();
        var algorithm = CreateAlgorithm(problem);

        var results = algorithm.RunStreaming(
          problem,
          RandomNumberGenerator.Create(42),
          ct: TestContext.Current.CancellationToken).ToList();

        results.Count.ShouldBe(5);
        results.All(result => result.Population.EvaluatedCandidates.Count == 5).ShouldBeTrue();
        results.SelectMany(result => result.Population.EvaluatedCandidates)
               .All(solution => problem.SearchSpace.Contains(solution.Candidate))
               .ShouldBeTrue();
    }

    [Fact]
    public void RunStreaming_WithMaximumGenerationsOne_YieldsOnlyGeneratedInitialPopulation()
    {
        var problem = CreateProblem();
        var algorithm = CreateUnwrappedAlgorithm(problem) with
        {
            MaximumGenerations = 1
        };

        var results = algorithm.RunStreaming(
          problem,
          RandomNumberGenerator.Create(42),
          ct: TestContext.Current.CancellationToken).ToList();

        results.Count.ShouldBe(1);
        results.Single().Population.EvaluatedCandidates.Count.ShouldBe(5);
        results.Single().Population.EvaluatedCandidates.All(solution => problem.SearchSpace.Contains(solution.Candidate)).ShouldBeTrue();
    }

    [Fact]
    public void RunStreaming_WithMaximumGenerations_YieldsConfiguredNumberOfGenerationStates()
    {
        var problem = CreateProblem();
        var algorithm = CreateUnwrappedAlgorithm(problem) with
        {
            MaximumGenerations = 3
        };

        var results = algorithm.RunStreaming(
          problem,
          RandomNumberGenerator.Create(42),
          ct: TestContext.Current.CancellationToken).ToList();

        results.Count.ShouldBe(3);
        results.All(result => result.Population.EvaluatedCandidates.Count == 5).ShouldBeTrue();
    }

    [Fact]
    public void RunStreaming_WithMaximumGenerationsAndInitialState_CountsOnlyNewlyProducedStates()
    {
        var problem = CreateProblem();
        var initialState = (CreateUnwrappedAlgorithm(problem) with
        {
            MaximumGenerations = 1
        }).RunStreaming(
          problem,
          RandomNumberGenerator.Create(42),
          ct: TestContext.Current.CancellationToken).Single();
        var algorithm = CreateUnwrappedAlgorithm(problem) with
        {
            MaximumGenerations = 2
        };

        var results = algorithm.RunStreaming(
          problem,
          RandomNumberGenerator.Create(43),
          initialState,
          TestContext.Current.CancellationToken).ToList();

        results.Count.ShouldBe(2);
        results.All(result => result.Population.EvaluatedCandidates.Count == 5).ShouldBeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void RunStreaming_WithNonpositiveMaximumGenerations_YieldsNoStates(int maximumGenerations)
    {
        var problem = CreateProblem();
        var algorithm = CreateUnwrappedAlgorithm(problem) with
        {
            MaximumGenerations = maximumGenerations
        };

        algorithm.RunStreaming(problem, RandomNumberGenerator.Create(43), ct: TestContext.Current.CancellationToken).ShouldBeEmpty();
    }

    /// <summary>
    /// The mutation rate is a threshold against a random value in <c>[0, 1)</c>, so values outside <c>[0, 1]</c> and
    /// <c>NaN</c> have stable meanings instead of being rejected.
    /// </summary>
    [Theory]
    [InlineData(-1.0)]
    [InlineData(2.0)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void UnusualMutationRate_IsAccepted(double mutationRate)
    {
        var problem = CreateProblem();
        var algorithm = CreateUnwrappedAlgorithm(problem) with
        {
            MutationRate = mutationRate,
            MaximumGenerations = 2
        };

        algorithm.MutationRate.ShouldBe(mutationRate);
        algorithm.RunStreaming(problem, RandomNumberGenerator.Create(43), ct: TestContext.Current.CancellationToken).Count().ShouldBe(2);
    }

    [Fact]
    public void RunStreaming_WithInternalTerminator_IncludesTriggeringState()
    {
        var problem = CreateProblem();
        var checkedStates = new List<PopulationState<RealVector>>();
        var terminator = new RecordingPopulationTerminator(checkedStates.Add, () => checkedStates.Count >= 2);
        var algorithm = CreateUnwrappedAlgorithm(problem) with
        {
            Terminator = terminator
        };

        var results = algorithm.RunStreaming(
          problem,
          RandomNumberGenerator.Create(42),
          ct: TestContext.Current.CancellationToken).ToList();

        results.Count.ShouldBe(2);
        checkedStates.Count.ShouldBe(2);
        checkedStates.ShouldBe(results);
    }

    [Fact]
    public void RunStreaming_WithInternalTerminatorAndInitialState_DoesNotCheckSuppliedInitialState()
    {
        var problem = CreateProblem();
        var initialState = (CreateUnwrappedAlgorithm(problem) with
        {
            MaximumGenerations = 1
        }).RunStreaming(
          problem,
          RandomNumberGenerator.Create(42),
          ct: TestContext.Current.CancellationToken).Single();
        var checkedStates = new List<PopulationState<RealVector>>();
        var terminator = new RecordingPopulationTerminator(checkedStates.Add, () => checkedStates.Count >= 1);
        var algorithm = CreateUnwrappedAlgorithm(problem) with
        {
            Terminator = terminator
        };

        var results = algorithm.RunStreaming(
          problem,
          RandomNumberGenerator.Create(43),
          initialState,
          TestContext.Current.CancellationToken).ToList();

        results.Count.ShouldBe(1);
        checkedStates.Count.ShouldBe(1);
        ReferenceEquals(checkedStates.Single(), initialState).ShouldBeFalse();
        checkedStates.Single().ShouldBe(results.Single());
    }

    [Fact]
    public void RunStreaming_MaximumGenerationsAndInternalTerminator_ComposeWithStopIfAnySemantics()
    {
        var problem = CreateProblem();
        var checkedStates = new List<PopulationState<RealVector>>();
        var terminator = new RecordingPopulationTerminator(checkedStates.Add, () => checkedStates.Count >= 2);
        var algorithm = CreateUnwrappedAlgorithm(problem) with
        {
            MaximumGenerations = 10,
            Terminator = terminator
        };

        var results = algorithm.RunStreaming(
          problem,
          RandomNumberGenerator.Create(42),
          ct: TestContext.Current.CancellationToken).ToList();

        results.Count.ShouldBe(2);
        checkedStates.Count.ShouldBe(2);
    }

    [Fact]
    public void RunStreaming_WithInternalTerminator_InvokesTerminatorOncePerProducedState()
    {
        var problem = CreateProblem();
        var checkedStates = new List<PopulationState<RealVector>>();
        var terminator = new RecordingPopulationTerminator(checkedStates.Add, () => checkedStates.Count >= 3);
        var algorithm = CreateUnwrappedAlgorithm(problem) with
        {
            Terminator = terminator
        };

        var results = algorithm.RunStreaming(
          problem,
          RandomNumberGenerator.Create(42),
          ct: TestContext.Current.CancellationToken).ToList();

        results.Count.ShouldBe(3);
        checkedStates.ShouldBe(results);
    }

    [Fact]
    public void RunToCompletion_ReturnsSameFinalStateAsRunStreamingLastState()
    {
        var problem = CreateProblem();
        var algorithm = CreateAlgorithm(problem);

        var result = algorithm.RunToCompletion(
          problem,
          RandomNumberGenerator.Create(42),
          ct: TestContext.Current.CancellationToken);
        var streamingResult = algorithm.RunStreaming(
          problem,
          RandomNumberGenerator.Create(42),
          ct: TestContext.Current.CancellationToken).Last();

        result.Population.Candidates.ShouldBe(streamingResult.Population.Candidates);
        result.Population.EvaluatedCandidates.Select(solution => solution.ObjectiveVector)
            .ShouldBe(streamingResult.Population.EvaluatedCandidates.Select(solution => solution.ObjectiveVector));
    }

    private static TestFunctionProblem CreateProblem()
    {
        return new TestFunctionProblem(new SphereFunction(dimension: 3));
    }

    private static GeneticAlgorithm<RealVector> CreateAlgorithm(
      TestFunctionProblem problem)
    {
        return CreateUnwrappedAlgorithm(problem) with
        {
            MaximumGenerations = 5
        };
    }

    private static GeneticAlgorithm<RealVector> CreateUnwrappedAlgorithm(
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

    private sealed record RecordingPopulationTerminator(Action<PopulationState<RealVector>> RecordState, Func<bool> ShouldStop)
        : StatelessTerminator<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem, PopulationState<RealVector>>
    {
        public override bool IsTerminalState(
            PopulationState<RealVector> state,
            IRandomNumberGenerator random,
            BoundedRealVectorSearchSpace searchSpace,
            TestFunctionProblem problem)
        {
            RecordState(state);
            return ShouldStop();
        }
    }
}
