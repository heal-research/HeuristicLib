using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Algorithms.LocalSearch;
using HEAL.HeuristicLib.Algorithms.MetaAlgorithms;
using HEAL.HeuristicLib.Genotypes.Vectors;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Operators.Creators;
using HEAL.HeuristicLib.Operators.Mutators;
using HEAL.HeuristicLib.Problems.TestFunctions;
using HEAL.HeuristicLib.Problems.TestFunctions.SingleObjectives;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces.Vectors;
using System.Collections.Immutable;
using Xunit;

namespace HEAL.HeuristicLib.ApiSpecs;

public class OperatorAuthoringSpecs
{
  [Fact(Explicit = true)]
  public async Task WrappingCreator_AuthoringExample_RunsInsideHillClimber()
  {
    var problem = CreateRastriginProblem(dimension: 3);
    var algorithm = new HillClimber<RealVector, RealVectorSearchSpace, TestFunctionProblem> {
      Creator = new PrefixingWrappingCreator(new ConstantOriginCreator()),
      Mutator = new NoChangeMutator(),
      Direction = LocalSearchDirection.FirstImprovement,
      BatchSize = 3,
      MaxNeighbors = 3
    }.WithMaxIterations(1);

    var finalState = await algorithm.RunToCompletionAsync(
      problem,
      RandomNumberGenerator.Create(123),
      ct: TestContext.Current.CancellationToken);

    finalState.Solution.Genotype.ShouldBe(RealVector.Repeat(0.0, problem.TestFunction.Dimension));
  }

  [Fact(Explicit = true)]
  public async Task MultiMutator_AuthoringExample_CanBeUsedInsideHillClimber()
  {
    var problem = CreateRastriginProblem(dimension: 3);
    var algorithm = new HillClimber<RealVector, RealVectorSearchSpace, TestFunctionProblem> {
      Creator = new ConstantOneCreator(),
      Mutator = new PreferFirstMultiMutator(
        [
          new PullTowardZeroMutator(),
          new PushAwayFromZeroMutator()
        ]),
      Direction = LocalSearchDirection.FirstImprovement,
      BatchSize = 2,
      MaxNeighbors = 2
    }.WithMaxIterations(1);

    var finalState = await algorithm.RunToCompletionAsync(
      problem,
      RandomNumberGenerator.Create(456),
      ct: TestContext.Current.CancellationToken);

    problem.SearchSpace.Contains(finalState.Solution.Genotype).ShouldBeTrue();
  }

  private static TestFunctionProblem CreateRastriginProblem(int dimension)
  {
    return new TestFunctionProblem(new RastriginFunction(dimension));
  }

  private sealed record PrefixingWrappingCreator(ICreator<RealVector, RealVectorSearchSpace, TestFunctionProblem> Inner)
    : WrappingCreator<RealVector, RealVectorSearchSpace, TestFunctionProblem>(Inner)
  {
    protected override IReadOnlyList<RealVector> Create(
      int count,
      InnerCreate innerCreate,
      IRandomNumberGenerator random,
      RealVectorSearchSpace searchSpace,
      TestFunctionProblem problem)
    {
      var offspring = innerCreate(count, random, searchSpace, problem).ToArray();
      offspring[0] = RealVector.Repeat(0.0, problem.TestFunction.Dimension);
      return offspring;
    }
  }

  private sealed record ConstantOriginCreator
    : SingleSolutionCreator<RealVector, RealVectorSearchSpace, TestFunctionProblem>
  {
    public override RealVector Create(
      IRandomNumberGenerator random,
      RealVectorSearchSpace searchSpace,
      TestFunctionProblem problem)
    {
      return RealVector.Repeat(1.0, problem.TestFunction.Dimension);
    }
  }

  private sealed record ConstantOneCreator
    : SingleSolutionCreator<RealVector, RealVectorSearchSpace, TestFunctionProblem>
  {
    public override RealVector Create(
      IRandomNumberGenerator random,
      RealVectorSearchSpace searchSpace,
      TestFunctionProblem problem)
    {
      return RealVector.Repeat(1.0, problem.TestFunction.Dimension);
    }
  }

  private sealed record NoChangeMutator
    : SingleSolutionMutator<RealVector, RealVectorSearchSpace, TestFunctionProblem>
  {
    public override RealVector Mutate(
      RealVector parent,
      IRandomNumberGenerator random,
      RealVectorSearchSpace searchSpace,
      TestFunctionProblem problem)
    {
      return parent;
    }
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
      return new RealVector(parent.Select(x => x * 0.5));
    }
  }

  private sealed record PushAwayFromZeroMutator
    : SingleSolutionMutator<RealVector, RealVectorSearchSpace, TestFunctionProblem>
  {
    public override RealVector Mutate(
      RealVector parent,
      IRandomNumberGenerator random,
      RealVectorSearchSpace searchSpace,
      TestFunctionProblem problem)
    {
      return new RealVector(parent.Select(x => x * 2.0));
    }
  }

  private sealed record PreferFirstMultiMutator(
    ImmutableArray<IMutator<RealVector, RealVectorSearchSpace, TestFunctionProblem>> Inner)
    : MultiMutator<RealVector, RealVectorSearchSpace, TestFunctionProblem>(Inner)
  {
    protected override IReadOnlyList<RealVector> Mutate(
      IReadOnlyList<RealVector> parents,
      IReadOnlyList<InnerMutate> innerMutators,
      IRandomNumberGenerator random,
      RealVectorSearchSpace searchSpace,
      TestFunctionProblem problem)
    {
      return innerMutators[0](parents, random, searchSpace, problem);
    }
  }
}
