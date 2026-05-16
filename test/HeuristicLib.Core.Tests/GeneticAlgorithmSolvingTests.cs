using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Algorithms.Evolutionary;
using HEAL.HeuristicLib.Algorithms.MetaAlgorithms;
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

namespace HEAL.HeuristicLib.Tests;

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

  private static TerminatableAlgorithm<RealVector, RealVectorSearchSpace, TestFunctionProblem, PopulationState<RealVector>> CreateAlgorithm(
    TestFunctionProblem problem)
  {
    return new GeneticAlgorithm<RealVector, RealVectorSearchSpace, TestFunctionProblem> {
      PopulationSize = 5,
      Creator = new UniformDistributedCreator(problem.SearchSpace),
      Crossover = new SinglePointCrossover(),
      Mutator = new GaussianMutator(0.1, 0.1),
      MutationRate = 0.5,
      Selector = new RandomSelector<RealVector>(),
      Elites = 0
    }.WithMaxIterations(5);
  }
}
