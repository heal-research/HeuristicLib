using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Algorithms.Evolutionary;
using HEAL.HeuristicLib.Algorithms.LocalSearch;
using HEAL.HeuristicLib.Algorithms.MetaAlgorithms;
using HEAL.HeuristicLib.Experiments;
using HEAL.HeuristicLib.Genotypes.Vectors;
using HEAL.HeuristicLib.Operators.Creators.RealVectorCreators;
using HEAL.HeuristicLib.Operators.Crossovers.RealVectorCrossovers;
using HEAL.HeuristicLib.Operators.Mutators.RealVectorMutators;
using HEAL.HeuristicLib.Operators.Selectors;
using HEAL.HeuristicLib.Problems.TestFunctions;
using HEAL.HeuristicLib.Problems.TestFunctions.SingleObjectives;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces.Vectors;
using HEAL.HeuristicLib.States;
using Xunit;

namespace HEAL.HeuristicLib.ApiSpecs;

public class PractitionerUsageSpecs
{
  [Fact]
  public async Task GeneticAlgorithm_BenchmarkExample_RunsToCompletion()
  {
    var problem = CreateRastriginProblem(dimension: 4);
    var algorithm = new GeneticAlgorithm<RealVector, RealVectorSearchSpace, TestFunctionProblem> {
      PopulationSize = 24,
      Creator = new UniformDistributedCreator(problem.SearchSpace),
      Crossover = new AlphaBetaBlendCrossover(alpha: 0.7),
      Mutator = new GaussianMutator(mutationRate: 0.2, mutationStrength: 0.15),
      Selector = new TournamentSelector<RealVector>(tournamentSize: 2),
      MutationRate = 0.2,
      Elites = 1
    }.WithMaxIterations(8);

    var finalState = await algorithm.RunToCompletionAsync(
      problem,
      RandomNumberGenerator.Create(123),
      ct: TestContext.Current.CancellationToken);

    finalState.Population.Solutions.Length.ShouldBe(24);
    finalState.Population.Solutions.All(solution => problem.SearchSpace.Contains(solution.Genotype)).ShouldBeTrue();
  }

  [Fact]
  public async Task HillClimber_BenchmarkExample_RunsToCompletion()
  {
    var problem = CreateRastriginProblem(dimension: 4);
    var algorithm = new HillClimber<RealVector, RealVectorSearchSpace, TestFunctionProblem> {
      Creator = new UniformDistributedCreator(problem.SearchSpace),
      Mutator = new GaussianMutator(mutationRate: 0.2, mutationStrength: 0.15),
      Direction = LocalSearchDirection.FirstImprovement,
      BatchSize = 4,
      MaxNeighbors = 12
    }.WithMaxIterations(10);

    var finalState = await algorithm.RunToCompletionAsync(
      problem,
      RandomNumberGenerator.Create(321),
      ct: TestContext.Current.CancellationToken);

    problem.SearchSpace.Contains(finalState.Solution.Genotype).ShouldBeTrue();
  }

  [Fact]
  public async Task RepeatedExecution_Example_RunsEachRepetition()
  {
    var problem = CreateRastriginProblem(dimension: 4);
    var algorithm = new HillClimber<RealVector, RealVectorSearchSpace, TestFunctionProblem> {
      Creator = new UniformDistributedCreator(problem.SearchSpace),
      Mutator = new GaussianMutator(mutationRate: 0.2, mutationStrength: 0.15),
      Direction = LocalSearchDirection.FirstImprovement,
      BatchSize = 4,
      MaxNeighbors = 12
    }.WithMaxIterations(6);
    var repeated = new RepeatAlgorithm<
      RealVector,
      RealVectorSearchSpace,
      TestFunctionProblem,
      SingleSolutionState<RealVector>,
      TerminatableAlgorithm<RealVector, RealVectorSearchSpace, TestFunctionProblem, SingleSolutionState<RealVector>>> {
      Algorithm = algorithm,
      Repetitions = 3
    };

    var results = await MultiStreamAlgorithmExtensions.RunToCompletionAsync<
      RealVector,
      RealVectorSearchSpace,
      TestFunctionProblem,
      SingleSolutionState<RealVector>,
      TerminatableAlgorithm<RealVector, RealVectorSearchSpace, TestFunctionProblem, SingleSolutionState<RealVector>>,
      int>(
      repeated,
      problem,
      RandomNumberGenerator.Create(999),
      cancellationToken: TestContext.Current.CancellationToken);

    results.Count.ShouldBe(3);
    results.Select(result => result.Key).ShouldBe([0, 1, 2]);
    results.All(result => problem.SearchSpace.Contains(result.Value.Solution.Genotype)).ShouldBeTrue();
  }

  private static TestFunctionProblem CreateRastriginProblem(int dimension)
  {
    return new TestFunctionProblem(new RastriginFunction(dimension));
  }
}
