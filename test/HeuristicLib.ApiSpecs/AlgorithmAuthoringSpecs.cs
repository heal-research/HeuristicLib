using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Algorithms.MetaAlgorithms;
using HEAL.HeuristicLib.Genotypes.Vectors;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Operators.Creators;
using HEAL.HeuristicLib.Operators.Interceptors;
using HEAL.HeuristicLib.Problems.TestFunctions;
using HEAL.HeuristicLib.Problems.TestFunctions.SingleObjectives;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces.Vectors;
using HEAL.HeuristicLib.States;
using Xunit;

namespace HEAL.HeuristicLib.ApiSpecs;

public class AlgorithmAuthoringSpecs
{
  [Fact]
  public async Task IterativeAlgorithm_AuthoringExample_HidesRuntimeStateCompletely()
  {
    var problem = new TestFunctionProblem(new SphereFunction(dimension: 3));
    var algorithm = new SingleCreateAlgorithm {
      Creator = new CountingCreator()
    }.WithMaxIterations(1);

    var finalState = await algorithm.RunToCompletionAsync(
      problem,
      RandomNumberGenerator.Create(42),
      ct: TestContext.Current.CancellationToken);

    finalState.Solution.Genotype.ShouldBe(new RealVector([1.0, 0.0, 0.0]));
  }

  [Fact]
  public async Task StatefulIterativeAlgorithm_AuthoringExample_HidesPublicRuntimeClass()
  {
    var problem = new TestFunctionProblem(new SphereFunction(dimension: 3));
    var algorithm = new DoubleCreateAlgorithm {
      Creator = new CountingCreator()
    }.WithMaxIterations(1);

    var firstRunState = await algorithm.RunToCompletionAsync(
      problem,
      RandomNumberGenerator.Create(123),
      ct: TestContext.Current.CancellationToken);

    var secondRunState = await algorithm.RunToCompletionAsync(
      problem,
      RandomNumberGenerator.Create(456),
      ct: TestContext.Current.CancellationToken);

    firstRunState.Solution.Genotype.ShouldBe(new RealVector([1.0, 2.0, 1.0]));
    secondRunState.Solution.Genotype.ShouldBe(new RealVector([1.0, 2.0, 1.0]));
  }

  [Fact]
  public async Task StatefulIterativeAlgorithm_AuthoringExample_StillUsesInterceptorsNormally()
  {
    var problem = new TestFunctionProblem(new SphereFunction(dimension: 3));
    var algorithm = new DoubleCreateAlgorithm {
      Creator = new CountingCreator(),
      Interceptor = new ThirdCoordinateIncrementingInterceptor()
    }.WithMaxIterations(1);

    var finalState = await algorithm.RunToCompletionAsync(
      problem,
      RandomNumberGenerator.Create(789),
      ct: TestContext.Current.CancellationToken);

    finalState.Solution.Genotype.ShouldBe(new RealVector([1.0, 2.0, 2.0]));
  }

  private sealed record SingleCreateAlgorithm
    : IterativeAlgorithm<RealVector, RealVectorSearchSpace, TestFunctionProblem, SingleSolutionState<RealVector>>
  {
    public required ICreator<RealVector, RealVectorSearchSpace, TestFunctionProblem> Creator { get; init; }

    protected override SingleSolutionState<RealVector> ExecuteStep(
      SingleSolutionState<RealVector>? previousState,
      IOperatorExecutor executor,
      TestFunctionProblem problem,
      IRandomNumberGenerator random)
    {
      var candidate = executor.Create(Creator, 1, random, problem.SearchSpace, problem)[0];
      var objective = executor.Evaluate(Evaluator, [candidate], random, problem.SearchSpace, problem)[0];

      return new SingleSolutionState<RealVector> {
        Population = Population.From([candidate], [objective])
      };
    }
  }

  private sealed record DoubleCreateAlgorithm
    : StatefulIterativeAlgorithm<RealVector, RealVectorSearchSpace, TestFunctionProblem, SingleSolutionState<RealVector>, DoubleCreateAlgorithm.RuntimeState>
  {
    public sealed class RuntimeState
    {
      public int Steps { get; set; }
    }

    public required ICreator<RealVector, RealVectorSearchSpace, TestFunctionProblem> Creator { get; init; }

    protected override RuntimeState CreateInitialRuntimeState()
    {
      return new RuntimeState();
    }

    protected override SingleSolutionState<RealVector> ExecuteStep(
      SingleSolutionState<RealVector>? previousState,
      RuntimeState runtimeState,
      IOperatorExecutor executor,
      TestFunctionProblem problem,
      IRandomNumberGenerator random)
    {
      runtimeState.Steps++;

      var first = executor.Create(Creator, 1, random, problem.SearchSpace, problem)[0];
      var second = executor.Create(Creator, 1, random, problem.SearchSpace, problem)[0];
      var candidate = new RealVector([first[0], second[0], runtimeState.Steps]);
      var objective = executor.Evaluate(Evaluator, [candidate], random, problem.SearchSpace, problem)[0];

      return new SingleSolutionState<RealVector> {
        Population = Population.From([candidate], [objective])
      };
    }
  }

  private sealed record CountingCreator
    : StatefulCreator<RealVector, RealVectorSearchSpace, TestFunctionProblem, CountingCreator.CounterState>
  {
    public sealed class CounterState
    {
      public int Calls { get; set; }
    }

    protected override CounterState CreateInitialState()
    {
      return new CounterState();
    }

    protected override IReadOnlyList<RealVector> Create(
      int count,
      CounterState state,
      IRandomNumberGenerator random,
      RealVectorSearchSpace searchSpace,
      TestFunctionProblem problem)
    {
      return Enumerable.Range(0, count)
        .Select(_ => {
          state.Calls++;
          return new RealVector([state.Calls, 0.0, 0.0]);
        })
        .ToArray();
    }
  }

  private sealed record ThirdCoordinateIncrementingInterceptor
    : StatelessInterceptor<RealVector, RealVectorSearchSpace, TestFunctionProblem, SingleSolutionState<RealVector>>
  {
    public override SingleSolutionState<RealVector> Transform(
      SingleSolutionState<RealVector> currentState,
      SingleSolutionState<RealVector>? previousState,
      RealVectorSearchSpace searchSpace,
      TestFunctionProblem problem)
    {
      var current = currentState.Solution.Genotype;
      var transformed = new RealVector([current[0], current[1], current[2] + 1.0]);
      return new SingleSolutionState<RealVector> {
        Population = Population.From([transformed], [currentState.Solution.ObjectiveVector])
      };
    }
  }
}
