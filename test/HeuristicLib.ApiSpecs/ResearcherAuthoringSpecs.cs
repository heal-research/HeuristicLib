using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Algorithms.LocalSearch;
using HEAL.HeuristicLib.Algorithms.MetaAlgorithms;
using HEAL.HeuristicLib.Genotypes.Vectors;
using HEAL.HeuristicLib.Operators.Creators;
using HEAL.HeuristicLib.Operators.Creators.RealVectorCreators;
using HEAL.HeuristicLib.Operators.Mutators;
using HEAL.HeuristicLib.Operators.Mutators.RealVectorMutators;
using HEAL.HeuristicLib.Operators.Terminators;
using HEAL.HeuristicLib.Problems.TestFunctions;
using HEAL.HeuristicLib.Problems.TestFunctions.SingleObjectives;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces.Vectors;
using HEAL.HeuristicLib.States;
using Xunit;

namespace HEAL.HeuristicLib.ApiSpecs;

public class ResearcherAuthoringSpecs
{
  [Fact(Explicit = true)]
  public async Task CustomMutator_AuthoringExample_RunsInHillClimber()
  {
    var problem = CreateRastriginProblem(dimension: 4);
    var algorithm = new HillClimber<RealVector, RealVectorSearchSpace, TestFunctionProblem> {
      Creator = new UniformDistributedCreator(problem.SearchSpace),
      Mutator = new PullTowardZeroMutator(),
      Direction = LocalSearchDirection.FirstImprovement,
      BatchSize = 4,
      MaxNeighbors = 12
    }.WithMaxIterations(5);

    var finalState = await algorithm.RunToCompletionAsync(
      problem,
      RandomNumberGenerator.Create(1234),
      ct: TestContext.Current.CancellationToken);

    problem.SearchSpace.Contains(finalState.Solution.Genotype).ShouldBeTrue();
  }

  [Fact(Explicit = true)]
  public async Task CustomTerminator_AuthoringExample_CanStopAlgorithm()
  {
    var problem = CreateRastriginProblem(dimension: 4);
    var innerAlgorithm = new HillClimber<RealVector, RealVectorSearchSpace, TestFunctionProblem> {
      Creator = new UniformDistributedCreator(problem.SearchSpace),
      Mutator = new GaussianMutator(mutationRate: 0.2, mutationStrength: 0.15),
      Direction = LocalSearchDirection.FirstImprovement,
      BatchSize = 4,
      MaxNeighbors = 12
    };

    var algorithm = new TerminatableAlgorithm<RealVector, RealVectorSearchSpace, TestFunctionProblem, SingleSolutionState<RealVector>> {
      Algorithm = innerAlgorithm,
      Terminator = new FirstEvaluatedStateTerminator()
    };

    var finalState = await algorithm.RunToCompletionAsync(
      problem,
      RandomNumberGenerator.Create(4321),
      ct: TestContext.Current.CancellationToken);

    problem.SearchSpace.Contains(finalState.Solution.Genotype).ShouldBeTrue();
  }

  [Fact(Explicit = true)]
  public async Task ProblemSpecificOperator_AuthoringExample_CanUseProblemType()
  {
    var problem = CreateRastriginProblem(dimension: 4);
    var algorithm = new HillClimber<RealVector, RealVectorSearchSpace, TestFunctionProblem> {
      Creator = new TestFunctionOriginCreator(),
      Mutator = new GaussianMutator(mutationRate: 0.2, mutationStrength: 0.15),
      Direction = LocalSearchDirection.FirstImprovement,
      BatchSize = 4,
      MaxNeighbors = 12
    }.WithMaxIterations(1);

    var finalState = await algorithm.RunToCompletionAsync(
      problem,
      RandomNumberGenerator.Create(9876),
      ct: TestContext.Current.CancellationToken);

    finalState.Solution.Genotype.ShouldBe(RealVector.Repeat(0.0, problem.TestFunction.Dimension));
  }

  private static TestFunctionProblem CreateRastriginProblem(int dimension)
  {
    return new TestFunctionProblem(new RastriginFunction(dimension));
  }

  private sealed record PullTowardZeroMutator
    : SingleSolutionMutator<RealVector, RealVectorSearchSpace, TestFunctionProblem>
  {
    public override RealVector Mutate(
      RealVector parent,
      IRandomNumberGenerator random,
      RealVectorSearchSpace searchSpace,
      TestFunctionProblem problem)
    {
      var moved = new RealVector(parent.Select(x => x * 0.5));
      return RealVector.Clamp(moved, searchSpace.Minimum, searchSpace.Maximum);
    }
  }

  private sealed record FirstEvaluatedStateTerminator
    : StatelessTerminator<RealVector, RealVectorSearchSpace, TestFunctionProblem, SingleSolutionState<RealVector>>
  {
    public override bool ShouldTerminate(
      SingleSolutionState<RealVector> state,
      RealVectorSearchSpace searchSpace,
      TestFunctionProblem problem)
    {
      return state.Solution.ObjectiveVector[0] >= 0.0;
    }
  }

  private sealed record TestFunctionOriginCreator
    : SingleSolutionCreator<RealVector, RealVectorSearchSpace, TestFunctionProblem>
  {
    public override RealVector Create(
      IRandomNumberGenerator random,
      RealVectorSearchSpace searchSpace,
      TestFunctionProblem problem)
    {
      return RealVector.Repeat(0.0, problem.TestFunction.Dimension);
    }
  }
}
