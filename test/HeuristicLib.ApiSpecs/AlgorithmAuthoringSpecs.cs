using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Algorithms.MetaAlgorithms;
using HEAL.HeuristicLib.Execution;
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
  public async Task StatefulIterativeAlgorithm_AuthoringExample_UsesResolvedExecutionState()
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
  public async Task StatefulIterativeAlgorithm_AuthoringExample_HidesPublicExecutionStateClass()
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

  [Fact]
  public void StatefulIterativeAlgorithm_CreateExecutionInstance_EagerlyResolvesRuntimeDependencies()
  {
    var problem = new TestFunctionProblem(new SphereFunction(dimension: 3));
    var creator = new InstancingCreator();
    var evaluator = new InstancingEvaluator();
    var algorithm = new SingleCreateAlgorithm {
      Creator = creator,
      Evaluator = evaluator
    };

    var run = algorithm.CreateRun(problem);
    var registry = run.CreateNewRegistry();

    _ = registry.Resolve(algorithm);

    creator.ExecutionInstancesCreated.ShouldBe(1);
    evaluator.ExecutionInstancesCreated.ShouldBe(1);
    creator.CreateCalls.ShouldBe(0);
    evaluator.EvaluateCalls.ShouldBe(0);
  }

  private sealed record SingleCreateAlgorithm
    : IterativeAlgorithm<RealVector, RealVectorSearchSpace, TestFunctionProblem, SingleSolutionState<RealVector>, SingleCreateAlgorithm.ExecutionState>
  {
    public new sealed class ExecutionState
      : IterativeAlgorithm<RealVector, RealVectorSearchSpace, TestFunctionProblem, SingleSolutionState<RealVector>, ExecutionState>.ExecutionState
    {
      public required ICreatorInstance<RealVector, RealVectorSearchSpace, TestFunctionProblem> Creator { get; init; }
    }

    public required ICreator<RealVector, RealVectorSearchSpace, TestFunctionProblem> Creator { get; init; }

    protected override ExecutionState CreateInitialExecutionState(IExecutionInstanceResolver resolver)
    {
      return new ExecutionState {
        Evaluator = resolver.Resolve(Evaluator),
        Interceptor = Interceptor is not null ? resolver.Resolve(Interceptor) : null,
        Creator = resolver.Resolve(Creator)
      };
    }

    protected override SingleSolutionState<RealVector> ExecuteStep(
      SingleSolutionState<RealVector>? previousState,
      ExecutionState executionState,
      TestFunctionProblem problem,
      IRandomNumberGenerator random)
    {
      var candidate = executionState.Creator.Create(1, random, problem.SearchSpace, problem)[0];
      var objective = executionState.Evaluator.Evaluate([candidate], random, problem.SearchSpace, problem)[0];

      return new SingleSolutionState<RealVector> {
        Population = Population.From([candidate], [objective])
      };
    }
  }

  private sealed record DoubleCreateAlgorithm
    : IterativeAlgorithm<RealVector, RealVectorSearchSpace, TestFunctionProblem, SingleSolutionState<RealVector>, DoubleCreateAlgorithm.ExecutionState>
  {
    public new sealed class ExecutionState
      : IterativeAlgorithm<RealVector, RealVectorSearchSpace, TestFunctionProblem, SingleSolutionState<RealVector>, ExecutionState>.ExecutionState
    {
      public int Steps { get; set; }
      public required ICreatorInstance<RealVector, RealVectorSearchSpace, TestFunctionProblem> Creator { get; init; }
    }

    public required ICreator<RealVector, RealVectorSearchSpace, TestFunctionProblem> Creator { get; init; }

    protected override ExecutionState CreateInitialExecutionState(IExecutionInstanceResolver resolver)
    {
      return new ExecutionState {
        Evaluator = resolver.Resolve(Evaluator),
        Interceptor = Interceptor is not null ? resolver.Resolve(Interceptor) : null,
        Creator = resolver.Resolve(Creator)
      };
    }

    protected override SingleSolutionState<RealVector> ExecuteStep(
      SingleSolutionState<RealVector>? previousState,
      ExecutionState executionState,
      TestFunctionProblem problem,
      IRandomNumberGenerator random)
    {
      executionState.Steps++;

      var first = executionState.Creator.Create(1, random, problem.SearchSpace, problem)[0];
      var second = executionState.Creator.Create(1, random, problem.SearchSpace, problem)[0];
      var candidate = new RealVector([first[0], second[0], executionState.Steps]);
      var objective = executionState.Evaluator.Evaluate([candidate], random, problem.SearchSpace, problem)[0];

      return new SingleSolutionState<RealVector> {
        Population = Population.From([candidate], [objective])
      };
    }
  }

  private sealed record CountingCreator
    : Creator<RealVector, RealVectorSearchSpace, TestFunctionProblem, CountingCreator.ExecutionState>
  {
    public sealed class ExecutionState
    {
      public int Calls { get; set; }
    }

    protected override ExecutionState CreateInitialState()
    {
      return new ExecutionState();
    }

    protected override IReadOnlyList<RealVector> Create(
      int count,
      ExecutionState executionState,
      IRandomNumberGenerator random,
      RealVectorSearchSpace searchSpace,
      TestFunctionProblem problem)
    {
      return Enumerable.Range(0, count)
        .Select(_ => {
          executionState.Calls++;
          return new RealVector([executionState.Calls, 0.0, 0.0]);
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

  private sealed class InstancingCreator : ICreator<RealVector, RealVectorSearchSpace, TestFunctionProblem>
  {
    public int ExecutionInstancesCreated { get; private set; }
    public int CreateCalls { get; private set; }

    public ICreatorInstance<RealVector, RealVectorSearchSpace, TestFunctionProblem> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry)
    {
      ExecutionInstancesCreated++;
      return new Instance(this);
    }

    private sealed class Instance(InstancingCreator owner) : ICreatorInstance<RealVector, RealVectorSearchSpace, TestFunctionProblem>
    {
      public IReadOnlyList<RealVector> Create(int count, IRandomNumberGenerator random, RealVectorSearchSpace searchSpace, TestFunctionProblem problem)
      {
        owner.CreateCalls++;
        return Enumerable.Range(0, count)
          .Select(_ => RealVector.Repeat(0.0, problem.TestFunction.Dimension))
          .ToArray();
      }
    }
  }

  private sealed class InstancingEvaluator : IEvaluator<RealVector, RealVectorSearchSpace, TestFunctionProblem>
  {
    public int ExecutionInstancesCreated { get; private set; }
    public int EvaluateCalls { get; private set; }

    public IEvaluatorInstance<RealVector, RealVectorSearchSpace, TestFunctionProblem> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry)
    {
      ExecutionInstancesCreated++;
      return new Instance(this);
    }

    private sealed class Instance(InstancingEvaluator owner) : IEvaluatorInstance<RealVector, RealVectorSearchSpace, TestFunctionProblem>
    {
      public IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<RealVector> genotypes, IRandomNumberGenerator random, RealVectorSearchSpace searchSpace, TestFunctionProblem problem)
      {
        owner.EvaluateCalls++;
        return Enumerable.Range(0, genotypes.Count)
          .Select(_ => new ObjectiveVector(0.0))
          .ToArray();
      }
    }
  }
}
