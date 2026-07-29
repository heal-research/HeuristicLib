using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Algorithms.Evolutionary;
using HEAL.HeuristicLib.Genotypes.Vectors;
using HEAL.HeuristicLib.Operators.Creators.RealVectorCreators;
using HEAL.HeuristicLib.Operators.Crossovers.RealVectorCrossovers;
using HEAL.HeuristicLib.Operators.Mutators.RealVectorMutators;
using HEAL.HeuristicLib.Operators.Replacers;
using HEAL.HeuristicLib.Operators.Selectors;
using HEAL.HeuristicLib.Problems.TestFunctions;
using HEAL.HeuristicLib.Problems.TestFunctions.SingleObjectives;
using HEAL.HeuristicLib.Problems.TestFunctions.ZDT;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces.Vectors;

namespace HEAL.HeuristicLib.Tests.Algorithms.Evolutionary;

public class EvolutionaryGenerationBudgetTests
{
    [Fact]
    public void EvolutionStrategy_Stream_WithMaximumGenerations_YieldsConfiguredNumberOfStates()
    {
        var problem = CreateSingleObjectiveProblem();
        var algorithm = CreateEvolutionStrategy(problem) with
        {
            MaximumGenerations = 3
        };

        var states = algorithm.Stream(
          problem,
          RandomNumberGenerator.Create(42),
          ct: TestContext.Current.CancellationToken).ToList();

        states.Count.ShouldBe(3);
        states.All(state => state.Population.EvaluatedCandidates.Length == 6).ShouldBeTrue();
    }

    [Fact]
    public void NSGA2_Stream_WithMaximumGenerations_YieldsConfiguredNumberOfStates()
    {
        var problem = CreateMultiObjectiveProblem();
        var algorithm = CreateNSGA2(problem) with
        {
            MaximumGenerations = 3
        };

        var states = algorithm.Stream(
          problem,
          RandomNumberGenerator.Create(42),
          ct: TestContext.Current.CancellationToken).ToList();

        states.Count.ShouldBe(3);
        states.All(state => state.Population.EvaluatedCandidates.Length == 6).ShouldBeTrue();
        states.SelectMany(state => state.Population.EvaluatedCandidates)
              .All(solution => solution.ObjectiveVector.Count == 2)
              .ShouldBeTrue();
    }

    [Fact]
    public void AlpsGeneticAlgorithm_Stream_WithMaximumGenerations_YieldsConfiguredNumberOfStates()
    {
        var problem = CreateSingleObjectiveProblem();
        var algorithm = CreateAlpsGeneticAlgorithm(problem) with
        {
            MaximumGenerations = 3
        };

        var states = algorithm.Stream(
          problem,
          RandomNumberGenerator.Create(42),
          ct: TestContext.Current.CancellationToken).ToList();

        states.Count.ShouldBe(3);
        states.All(state => state.Population.Single().EvaluatedCandidates.Length == 6).ShouldBeTrue();
        states.Select(state => state.Ages.Single().Distinct().Single()).ShouldBe([0, 1, 2]);
    }

    [Fact]
    public void OpenEndedRelevantAllelesPreservingGeneticAlgorithm_Stream_WithMaximumGenerations_YieldsConfiguredNumberOfStates()
    {
        var problem = CreateSingleObjectiveProblem();
        var algorithm = CreateOpenEndedRelevantAllelesPreservingGeneticAlgorithm(problem) with
        {
            MaximumGenerations = 3
        };

        var states = algorithm.Stream(
          problem,
          RandomNumberGenerator.Create(42),
          ct: TestContext.Current.CancellationToken).ToList();

        states.Count.ShouldBe(3);
        states.All(state => state.Population.EvaluatedCandidates.Length > 0).ShouldBeTrue();
    }

    [Fact]
    public void Constructors_Throw_WhenMaximumGenerationsIsNotPositive()
    {
        var singleObjectiveProblem = CreateSingleObjectiveProblem();
        var multiObjectiveProblem = CreateMultiObjectiveProblem();

        Should.Throw<ArgumentOutOfRangeException>(() => CreateEvolutionStrategy(singleObjectiveProblem) with
        {
            MaximumGenerations = 0
        });
        Should.Throw<ArgumentOutOfRangeException>(() => CreateNSGA2(multiObjectiveProblem) with
        {
            MaximumGenerations = 0
        });
        Should.Throw<ArgumentOutOfRangeException>(() => CreateAlpsGeneticAlgorithm(singleObjectiveProblem) with
        {
            MaximumGenerations = 0
        });
        Should.Throw<ArgumentOutOfRangeException>(() => CreateOpenEndedRelevantAllelesPreservingGeneticAlgorithm(singleObjectiveProblem) with
        {
            MaximumGenerations = 0
        });
    }

    private static TestFunctionProblem CreateSingleObjectiveProblem()
    {
        return new TestFunctionProblem(new SphereFunction(dimension: 3));
    }

    private static MultiObjectiveTestFunctionProblem CreateMultiObjectiveProblem()
    {
        return new MultiObjectiveTestFunctionProblem(new Zdt1(dimension: 3));
    }

    private static EvolutionStrategy<RealVector, RealVectorSearchSpace, TestFunctionProblem> CreateEvolutionStrategy(
      TestFunctionProblem problem)
    {
        return new EvolutionStrategy<RealVector, RealVectorSearchSpace, TestFunctionProblem>
        {
            PopulationSize = 6,
            NumberOfChildren = 6,
            Strategy = EvolutionStrategyType.Plus,
            Creator = new UniformDistributedCreator(problem.SearchSpace),
            Mutator = new GaussianMutator(0.1, 0.1),
            Crossover = null,
            Selector = RandomSelector.For(problem)
        };
    }

    private static NSGA2<RealVector, RealVectorSearchSpace, MultiObjectiveTestFunctionProblem> CreateNSGA2(
      MultiObjectiveTestFunctionProblem problem)
    {
        return new NSGA2<RealVector, RealVectorSearchSpace, MultiObjectiveTestFunctionProblem>
        {
            PopulationSize = 6,
            Creator = new UniformDistributedCreator(problem.SearchSpace),
            Crossover = new SinglePointCrossover(),
            Mutator = new GaussianMutator(0.1, 0.1),
            Selector = ParetoCrowdingTournamentSelector.For(problem, dominateOnEqualities: false, tournamentSize: 2),
            Replacer = ParetoCrowdingReplacer.For(problem, dominateOnEqualities: true)
        };
    }

    private static AlpsGeneticAlgorithm<RealVector, RealVectorSearchSpace, TestFunctionProblem> CreateAlpsGeneticAlgorithm(
      TestFunctionProblem problem)
    {
        return new AlpsGeneticAlgorithm<RealVector, RealVectorSearchSpace, TestFunctionProblem>
        {
            PopulationSize = 6,
            Creator = new UniformDistributedCreator(problem.SearchSpace),
            Crossover = new SinglePointCrossover(),
            Mutator = new GaussianMutator(0.1, 0.1),
            MutationRate = 0.5,
            Selector = RandomSelector.For(problem),
            Elites = 0
        };
    }

    private static OpenEndedRelevantAllelesPreservingGeneticAlgorithm<RealVector, RealVectorSearchSpace, TestFunctionProblem> CreateOpenEndedRelevantAllelesPreservingGeneticAlgorithm(
      TestFunctionProblem problem)
    {
        return new OpenEndedRelevantAllelesPreservingGeneticAlgorithm<RealVector, RealVectorSearchSpace, TestFunctionProblem>
        {
            PopulationSize = 6,
            Creator = new UniformDistributedCreator(problem.SearchSpace),
            Crossover = new SinglePointCrossover(),
            Mutator = new GaussianMutator(0.1, 0.1),
            Selector = RandomSelector.For(problem),
            Elites = 1,
            MaxEffort = 6
        };
    }
}
