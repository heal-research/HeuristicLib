using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Algorithms.Evolutionary;
using HEAL.HeuristicLib.Genotypes.Vectors;
using HEAL.HeuristicLib.Operators.Creators.RealVectorCreators;
using HEAL.HeuristicLib.Operators.Crossovers.RealVectorCrossovers;
using HEAL.HeuristicLib.Operators.Mutators.RealVectorMutators;
using HEAL.HeuristicLib.Operators.Selectors;
using HEAL.HeuristicLib.Operators.Terminators;
using HEAL.HeuristicLib.Problems.TestFunctions;
using HEAL.HeuristicLib.Problems.TestFunctions.SingleObjectives;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces.Vectors;
using HEAL.HeuristicLib.States;

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

        result.Population.Solutions.Length.ShouldBe(5);
        result.Population.Solutions.All(solution => problem.SearchSpace.Contains(solution.Genotype)).ShouldBeTrue();
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
        results.All(result => result.Population.Solutions.Length == 5).ShouldBeTrue();
        results.SelectMany(result => result.Population.Solutions)
               .All(solution => problem.SearchSpace.Contains(solution.Genotype))
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
        results.Single().Population.Solutions.Length.ShouldBe(5);
        results.Single().Population.Solutions.All(solution => problem.SearchSpace.Contains(solution.Genotype)).ShouldBeTrue();
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
        results.All(result => result.Population.Solutions.Length == 5).ShouldBeTrue();
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
        results.All(result => result.Population.Solutions.Length == 5).ShouldBeTrue();
    }

    [Fact]
    public void Constructor_Throws_WhenMaximumGenerationsIsNotPositive()
    {
        var problem = CreateProblem();

        Should.Throw<ArgumentOutOfRangeException>(() => CreateUnwrappedAlgorithm(problem) with
        {
            MaximumGenerations = 0
        });
    }

    [Fact]
    public void RunStreaming_WithInternalTerminator_IncludesTriggeringState()
    {
        var problem = CreateProblem();
        var terminator = new RecordingPopulationTerminator(2);
        var algorithm = CreateUnwrappedAlgorithm(problem) with
        {
            Terminator = terminator
        };

        var results = algorithm.RunStreaming(
          problem,
          RandomNumberGenerator.Create(42),
          ct: TestContext.Current.CancellationToken).ToList();

        results.Count.ShouldBe(2);
        terminator.CheckedStates.Count.ShouldBe(2);
        terminator.CheckedStates.ShouldBe(results);
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
        var terminator = new RecordingPopulationTerminator(1);
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
        terminator.CheckedStates.Count.ShouldBe(1);
        ReferenceEquals(terminator.CheckedStates.Single(), initialState).ShouldBeFalse();
        terminator.CheckedStates.Single().ShouldBe(results.Single());
    }

    [Fact]
    public void RunStreaming_MaximumGenerationsAndInternalTerminator_ComposeWithStopIfAnySemantics()
    {
        var problem = CreateProblem();
        var terminator = new RecordingPopulationTerminator(2);
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
        terminator.CheckedStates.Count.ShouldBe(2);
    }

    [Fact]
    public void RunStreaming_WithInternalTerminator_InvokesTerminatorOncePerProducedState()
    {
        var problem = CreateProblem();
        var terminator = new RecordingPopulationTerminator(3);
        var algorithm = CreateUnwrappedAlgorithm(problem) with
        {
            Terminator = terminator
        };

        var results = algorithm.RunStreaming(
          problem,
          RandomNumberGenerator.Create(42),
          ct: TestContext.Current.CancellationToken).ToList();

        results.Count.ShouldBe(3);
        terminator.CheckedStates.ShouldBe(results);
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

        result.Population.Genotypes.ShouldBe(streamingResult.Population.Genotypes);
        result.Population.Solutions.Select(solution => solution.ObjectiveVector)
              .ShouldBe(streamingResult.Population.Solutions.Select(solution => solution.ObjectiveVector));
    }

    private static TestFunctionProblem CreateProblem()
    {
        return new TestFunctionProblem(new SphereFunction(dimension: 3));
    }

    private static GeneticAlgorithm<RealVector, RealVectorSearchSpace, TestFunctionProblem> CreateAlgorithm(
      TestFunctionProblem problem)
    {
        return CreateUnwrappedAlgorithm(problem) with
        {
            MaximumGenerations = 5
        };
    }

    private static GeneticAlgorithm<RealVector, RealVectorSearchSpace, TestFunctionProblem> CreateUnwrappedAlgorithm(
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

    private sealed record RecordingPopulationTerminator(int StopOnInvocation)
      : StatelessTerminator<RealVector, RealVectorSearchSpace, TestFunctionProblem, PopulationState<RealVector>>
    {
        public List<PopulationState<RealVector>> CheckedStates { get; } = [];

        public override bool IsTerminalState(
          PopulationState<RealVector> state,
          RealVectorSearchSpace searchSpace,
          TestFunctionProblem problem)
        {
            CheckedStates.Add(state);
            return CheckedStates.Count >= StopOnInvocation;
        }
    }
}
