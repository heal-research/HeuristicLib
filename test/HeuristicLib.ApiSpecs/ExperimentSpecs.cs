using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Algorithms.LocalSearch;
using HEAL.HeuristicLib.Algorithms.MetaAlgorithms;
using HEAL.HeuristicLib.Experiments;
using HEAL.HeuristicLib.Genotypes.Vectors;
using HEAL.HeuristicLib.Operators.Creators.RealVectorCreators;
using HEAL.HeuristicLib.Operators.Mutators.RealVectorMutators;
using HEAL.HeuristicLib.Problems.TestFunctions;
using HEAL.HeuristicLib.Problems.TestFunctions.SingleObjectives;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces.Vectors;
using HEAL.HeuristicLib.States;
using Xunit;

namespace HEAL.HeuristicLib.ApiSpecs;

public class ExperimentSpecs
{
  [Fact]
  public async Task RepeatedExecution_CurrentApi_UsesExplicitRepeatAlgorithmAndGenericRunToCompletion()
  {
    var problem = CreateRastriginProblem(dimension: 4);
    var algorithm = CreateSimpleHillClimber(problem)
      .WithMaxIterations(6);

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

  [Fact]
  public async Task RepeatedExecution_CurrentApi_SupportsInterleavedStreamingThroughInstance()
  {
    var problem = CreateRastriginProblem(dimension: 4);
    var algorithm = CreateSimpleHillClimber(problem)
      .WithMaxIterations(2);

    var repeated = new RepeatAlgorithm<
      RealVector,
      RealVectorSearchSpace,
      TestFunctionProblem,
      SingleSolutionState<RealVector>,
      TerminatableAlgorithm<RealVector, RealVectorSearchSpace, TestFunctionProblem, SingleSolutionState<RealVector>>> {
      Algorithm = algorithm,
      Repetitions = 3
    };
    var repeatedInstance = repeated.CreateExecutionInstance(null!);

    var stream = new List<KeyValuePair<(int Repetition, int Iteration), SingleSolutionState<RealVector>>>();

    await foreach (var entry in repeatedInstance.RunInterleavedStreamingAsync(
                     problem,
                     RandomNumberGenerator.Create(123),
                     cancellationToken: TestContext.Current.CancellationToken)) {
      stream.Add(entry);
    }

    stream.Count.ShouldBe(6);
    stream.Select(entry => entry.Key.Repetition).Distinct().Order().ShouldBe([0, 1, 2]);
    stream.All(entry => problem.SearchSpace.Contains(entry.Value.Solution.Genotype)).ShouldBeTrue();
  }

  private static TestFunctionProblem CreateRastriginProblem(int dimension)
  {
    return new TestFunctionProblem(new RastriginFunction(dimension));
  }

  private static HillClimber<RealVector, RealVectorSearchSpace, TestFunctionProblem> CreateSimpleHillClimber(
    TestFunctionProblem problem)
  {
    return new HillClimber<RealVector, RealVectorSearchSpace, TestFunctionProblem> {
      Creator = new UniformDistributedCreator(problem.SearchSpace),
      Mutator = new GaussianMutator(mutationRate: 0.2, mutationStrength: 0.15),
      Direction = LocalSearchDirection.FirstImprovement,
      BatchSize = 4,
      MaxNeighbors = 12
    };
  }
}
