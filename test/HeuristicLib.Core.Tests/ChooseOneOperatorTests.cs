using HEAL.HeuristicLib.Operators.Crossovers;
using HEAL.HeuristicLib.Operators.Interceptors;
using HEAL.HeuristicLib.Operators.Mutators;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.States;
using HEAL.HeuristicLib.Tests.Mocks;

namespace HEAL.HeuristicLib.Tests;

public class ChooseOneOperatorTests
{
  [Fact]
  public void ChooseOneMutator_ShouldPreserveOriginalOrder()
  {
    var mutator = ChooseOneMutator.Create(
      [new AddOffsetMutator(100), new AddOffsetMutator(200)],
      [1.0, 1.0]);

    var problem = FuncProblem.Create<int, DummySearchSpace<int>>(
      evaluateFunc: x => x,
      encoding: DummySearchSpace<int>.Instance,
      objective: SingleObjective.Minimize);
    var rng = new SequenceRandomNumberGenerator(0.2, 0.8, 0.3);
    var instance = mutator.CreateExecutionInstance(new Execution.ExecutionInstanceRegistry(TestRun.Instance));

    var result = instance.Mutate([1, 2, 3], rng, DummySearchSpace<int>.Instance, problem);

    result.ShouldBe([101, 202, 103]);
  }

  [Fact]
  public void ChooseOneCrossover_CreateWithParams_ShouldAssignUniformWeights()
  {
    var crossover = ChooseOneCrossover.Create(
      new FirstParentCrossover(100),
      new SecondParentCrossover(200));

    crossover.Weights.ShouldBe([0.5, 0.5]);
  }

  [Fact]
  public void ChooseOneOperators_ShouldRejectInvalidWeights()
  {
    Should.Throw<ArgumentException>(() => ChooseOneMutator.Create(
      [new AddOffsetMutator(100)],
      []));
    Should.Throw<ArgumentException>(() => ChooseOneMutator.Create(
      [new AddOffsetMutator(100), new AddOffsetMutator(200)],
      [1.0, -1.0]));
    Should.Throw<ArgumentException>(() => ChooseOneCrossover.Create(
      [new FirstParentCrossover(100), new SecondParentCrossover(200)],
      [0.0, 0.0]));
  }

  [Fact]
  public void ChooseOneCrossover_ShouldPreserveOriginalOrder()
  {
    var crossover = ChooseOneCrossover.Create(
      [new FirstParentCrossover(100), new SecondParentCrossover(200)],
      [1.0, 1.0]);

    var problem = FuncProblem.Create<int, DummySearchSpace<int>>(
      evaluateFunc: x => x,
      encoding: DummySearchSpace<int>.Instance,
      objective: SingleObjective.Minimize);
    var rng = new SequenceRandomNumberGenerator(0.2, 0.8, 0.3);
    var instance = crossover.CreateExecutionInstance(new Execution.ExecutionInstanceRegistry(TestRun.Instance));

    var result = instance.Cross(
      [new Parents<int>(1, 10), new Parents<int>(2, 20), new Parents<int>(3, 30)],
      rng,
      DummySearchSpace<int>.Instance,
      problem);

    result.ShouldBe([101, 220, 103]);
  }

  [Fact]
  public void PipelineMutator_ShouldApplyMutatorsInSequence()
  {
    var mutator = new PipelineMutator<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>>([
      new AddOffsetMutator(10),
      new AddOffsetMutator(100)
    ]);

    var problem = FuncProblem.Create<int, DummySearchSpace<int>>(
      evaluateFunc: x => x,
      encoding: DummySearchSpace<int>.Instance,
      objective: SingleObjective.Minimize);
    var instance = mutator.CreateExecutionInstance(new Execution.ExecutionInstanceRegistry(TestRun.Instance));

    var result = instance.Mutate([1, 2, 3], RandomNumberGenerator.Create(0), DummySearchSpace<int>.Instance, problem);

    result.ShouldBe([111, 112, 113]);
  }

  [Fact]
  public void PipelineMutator_ShouldResolveInnerMutatorsOncePerExecutionInstance()
  {
    var countingMutator = new CountingInstanceMutator();
    var mutator = new PipelineMutator<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>>([
      countingMutator
    ]);

    var problem = FuncProblem.Create<int, DummySearchSpace<int>>(
      evaluateFunc: x => x,
      encoding: DummySearchSpace<int>.Instance,
      objective: SingleObjective.Minimize);
    var instance = mutator.CreateExecutionInstance(new Execution.ExecutionInstanceRegistry(TestRun.Instance));

    var first = instance.Mutate([1], RandomNumberGenerator.Create(0), DummySearchSpace<int>.Instance, problem);
    var second = instance.Mutate([1], RandomNumberGenerator.Create(1), DummySearchSpace<int>.Instance, problem);

    countingMutator.ExecutionInstancesCreated.ShouldBe(1);
    first.ShouldBe([2]);
    second.ShouldBe([3]);
  }

  [Fact]
  public void PipelineMutator_ShouldRejectEmptyPipelines()
  {
    Should.Throw<ArgumentException>(() => new PipelineMutator<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>>([]))
      .ParamName.ShouldBe("mutators");
  }

  [Fact]
  public void PipelineInterceptor_ShouldApplyInterceptorsInSequence()
  {
    var interceptor = new PipelineInterceptor<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>, TestAlgorithmState>([
      new AddToStateInterceptor(10),
      new AddToStateInterceptor(100)
    ]);

    var problem = FuncProblem.Create<int, DummySearchSpace<int>>(
      evaluateFunc: x => x,
      encoding: DummySearchSpace<int>.Instance,
      objective: SingleObjective.Minimize);
    var instance = interceptor.CreateExecutionInstance(new Execution.ExecutionInstanceRegistry(TestRun.Instance));

    var result = instance.Transform(new TestAlgorithmState { Value = 1 }, previousState: null, DummySearchSpace<int>.Instance, problem);

    result.Value.ShouldBe(111);
  }

  private sealed record AddOffsetMutator(int Offset) : SingleSolutionMutator<int, DummySearchSpace<int>>
  {
    public override int Mutate(int parent, IRandomNumberGenerator random, DummySearchSpace<int> searchSpace) => parent + Offset;
  }

  private sealed record FirstParentCrossover(int Offset) : SingleSolutionCrossover<int, DummySearchSpace<int>>
  {
    public override int Cross(IParents<int> parents, IRandomNumberGenerator random, DummySearchSpace<int> searchSpace) => parents.Parent1 + Offset;
  }

  private sealed record SecondParentCrossover(int Offset) : SingleSolutionCrossover<int, DummySearchSpace<int>>
  {
    public override int Cross(IParents<int> parents, IRandomNumberGenerator random, DummySearchSpace<int> searchSpace) => parents.Parent2 + Offset;
  }

  private sealed record AddToStateInterceptor(int Offset)
    : StatelessInterceptor<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>, TestAlgorithmState>
  {
    public override TestAlgorithmState Transform(TestAlgorithmState currentState, TestAlgorithmState? previousState, DummySearchSpace<int> searchSpace, IProblem<int, DummySearchSpace<int>> problem)
      => currentState with { Value = currentState.Value + Offset };
  }

  private sealed record TestAlgorithmState : SearchState
  {
    public required int Value { get; init; }
  }

  private sealed class CountingInstanceMutator : IMutator<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>>
  {
    public int ExecutionInstancesCreated { get; private set; }

    public IMutatorInstance<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>> CreateExecutionInstance(Execution.ExecutionInstanceRegistry instanceRegistry)
    {
      ExecutionInstancesCreated++;
      return new Instance();
    }

    private sealed class Instance : IMutatorInstance<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>>
    {
      private int calls;

      public IReadOnlyList<int> Mutate(IReadOnlyList<int> parents, IRandomNumberGenerator random, DummySearchSpace<int> searchSpace, IProblem<int, DummySearchSpace<int>> problem)
      {
        calls++;
        return parents.Select(parent => parent + calls).ToArray();
      }
    }
  }

  private sealed class SequenceRandomNumberGenerator(params double[] nextDoubles) : IRandomNumberGenerator
  {
    private readonly Queue<double> doubles = new(nextDoubles);

    public double NextDouble()
    {
      if (doubles.Count == 0)
        throw new InvalidOperationException("No more predefined doubles are available.");
      return doubles.Dequeue();
    }

    public int NextInt() => 0;

    public IRandomNumberGenerator Fork(ulong forkKey) => this;
  }
}




