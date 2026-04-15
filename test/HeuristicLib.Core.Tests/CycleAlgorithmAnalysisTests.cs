using System.Data;
using System.Runtime.CompilerServices;
using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Algorithms.MetaAlgorithms;
using HEAL.HeuristicLib.Analysis;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Operators.Evaluators;
using HEAL.HeuristicLib.Operators.Interceptors;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.States;
using HEAL.HeuristicLib.Tests.Mocks;

namespace HEAL.HeuristicLib.Tests;

public class CycleAlgorithmAnalysisTests
{
  [Fact]
  public void CycleAlgorithm_WithFreshRegistries_PreservesAnalyzerStateAcrossCycles()
  {
    var result = RunCycleAlgorithm(newExecutionInstancesPerCycle: true);

    result.EvaluationTrace1.ObjectiveValues.ShouldBe([1.0, 1.0, 1.0, 1.0, 1.0, 1.0]);
    result.EvaluationTrace2.ObjectiveValues.ShouldBe([1.0, 1.0, 1.0, 1.0, 1.0, 1.0]);
    result.InterceptionTrace.ObjectiveValues.ShouldBe([1.0, 1.0, 1.0, 1.0, 1.0, 1.0]);
    result.FinalState.Population.Solutions.Single().Genotype.ShouldBe(2);
  }

  [Fact]
  public void CycleAlgorithm_WithReusedChildRegistries_PreservesAnalyzerStateAcrossCycles()
  {
    var result = RunCycleAlgorithm(newExecutionInstancesPerCycle: false);

    result.EvaluationTrace1.ObjectiveValues.ShouldBe([1.0, 1.0, 2.0, 2.0, 3.0, 3.0]);
    result.EvaluationTrace2.ObjectiveValues.ShouldBe([1.0, 1.0, 2.0, 2.0, 3.0, 3.0]);
    result.InterceptionTrace.ObjectiveValues.ShouldBe([1.0, 1.0, 2.0, 2.0, 3.0, 3.0]);
    result.FinalState.Population.Solutions.Single().Genotype.ShouldBe(2);
  }

  private static CycleRunResult RunCycleAlgorithm(bool newExecutionInstancesPerCycle)
  {
    var evaluator = new IncrementingEvaluator();
    var interceptor = new IdentityInterceptor<int, PopulationState<int>>();
    var algorithm1 = new SingleStepAlgorithm(1, evaluator, interceptor);
    var algorithm2 = new SingleStepAlgorithm(2, evaluator, interceptor);
    var cycleAlgorithm = new CycleAlgorithm<SingleStepAlgorithm, int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>, PopulationState<int>>(
      [algorithm1, algorithm2]) {
      MaximumCycles = 3,
      NewExecutionInstancesPerCycle = newExecutionInstancesPerCycle,
      Evaluator = evaluator
    };

    var evaluationTrace1 = new EvaluationTraceAnalysis(evaluator);
    var evaluationTrace2 = new EvaluationTraceAnalysis(evaluator);
    var interceptionTrace = new InterceptionTraceAnalysis(interceptor);
    var problem = FuncProblem.Create<int, DummySearchSpace<int>>(
      evaluateFunc: x => x,
      encoding: DummySearchSpace<int>.Instance,
      objective: SingleObjective.Minimize);

    var run = cycleAlgorithm.CreateRun(problem, evaluationTrace1, evaluationTrace2, interceptionTrace);
    var finalState = run.RunToCompletion(RandomNumberGenerator.Create(0));

    return new CycleRunResult(
      finalState,
      run.GetAnalyzerResult(evaluationTrace1),
      run.GetAnalyzerResult(evaluationTrace2),
      run.GetAnalyzerResult(interceptionTrace));
  }

  private sealed record CycleRunResult(
    PopulationState<int> FinalState,
    EvaluationTraceAnalysis.State EvaluationTrace1,
    EvaluationTraceAnalysis.State EvaluationTrace2,
    InterceptionTraceAnalysis.State InterceptionTrace);

  private sealed record IncrementingEvaluator
    : StatefulEvaluator<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>, IncrementingEvaluator.Counter>
  {
    public sealed class Counter
    {
      public int Value { get; set; }
    }

    protected override Counter CreateInitialState() => new();

    protected override IReadOnlyList<ObjectiveVector> Evaluate(
      IReadOnlyList<int> genotypes,
      Counter state,
      IRandomNumberGenerator random,
      DummySearchSpace<int> searchSpace,
      IProblem<int, DummySearchSpace<int>> problem)
    {
      return genotypes.Select(_ => new ObjectiveVector(++state.Value)).ToArray();
    }
  }

  private sealed record SingleStepAlgorithm : Algorithm<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>, PopulationState<int>, SingleStepAlgorithm.State>
  {
    public int Genotype { get; }

    public IInterceptor<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>, PopulationState<int>> Interceptor { get; }

    public SingleStepAlgorithm(
      int genotype,
      IEvaluator<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>> evaluator,
      IInterceptor<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>, PopulationState<int>> interceptor)
    {
      Genotype = genotype;
      Interceptor = interceptor;
      Evaluator = evaluator;
    }

    protected override State CreateInitialState(ExecutionInstanceRegistry instanceRegistry) => new(instanceRegistry, this);

    protected override async IAsyncEnumerable<PopulationState<int>> RunStreamingAsync(State state, IProblem<int, DummySearchSpace<int>> problem, IRandomNumberGenerator random, PopulationState<int>? initialState = null, [EnumeratorCancellation] CancellationToken ct = default)
    {
      ct.ThrowIfCancellationRequested();

      var objectiveVector = state.Evaluator.Evaluate([Genotype], random, problem.SearchSpace, problem).Single();
      var currentState = new PopulationState<int> {
        Population = Population.From([Solution.From(Genotype, objectiveVector)])
      };

      yield return state.Interceptor.Transform(currentState, initialState, problem.SearchSpace, problem);
      await Task.CompletedTask;
    }

    public sealed class State : AlgorithmState
    {
      public readonly IInterceptorInstance<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>, PopulationState<int>> Interceptor;

      public State(ExecutionInstanceRegistry instanceRegistry, SingleStepAlgorithm algorithm) :
        base(instanceRegistry, algorithm)
      {
        Interceptor = instanceRegistry.Resolve(algorithm.Interceptor);
      }
    }
  }

  private sealed class EvaluationTraceAnalysis(IEvaluator<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>> evaluator)
    : IAnalyzer<EvaluationTraceAnalysis.State>
  {
    public IEvaluator<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>> Evaluator { get; } = evaluator;

    public State CreateAnalyzerState() => new(this);

    public sealed class State(EvaluationTraceAnalysis analyzer)
      : AnalyzerRunState<EvaluationTraceAnalysis>(analyzer)
    {
      private readonly List<double> objectiveValues = [];

      public IReadOnlyList<double> ObjectiveValues => objectiveValues;

      public override void RegisterObservations(IObservationRegistry observationRegistry)
      {
        observationRegistry.Add(Analyzer.Evaluator, AfterEvaluation);
      }

      private void AfterEvaluation(IReadOnlyList<int> genotypes, IReadOnlyList<ObjectiveVector> objectiveVectors, DummySearchSpace<int> searchSpace, IProblem<int, DummySearchSpace<int>> problem)
      {
        objectiveValues.AddRange(objectiveVectors.Select(x => x[0]));
      }
    }
  }

  private sealed class InterceptionTraceAnalysis(IInterceptor<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>, PopulationState<int>> interceptor)
    : IAnalyzer<InterceptionTraceAnalysis.State>
  {
    public IInterceptor<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>, PopulationState<int>> Interceptor { get; } = interceptor;

    public State CreateAnalyzerState() => new(this);

    public sealed class State(InterceptionTraceAnalysis analyzer)
      : AnalyzerRunState<InterceptionTraceAnalysis>(analyzer)
    {
      private readonly List<double> objectiveValues = [];

      public IReadOnlyList<double> ObjectiveValues => objectiveValues;

      public override void RegisterObservations(IObservationRegistry observationRegistry)
      {
        observationRegistry.Add(Analyzer.Interceptor, AfterInterception);
      }

      private void AfterInterception(PopulationState<int> newState, PopulationState<int> currentState, PopulationState<int>? previousState, DummySearchSpace<int> searchSpace, IProblem<int, DummySearchSpace<int>> problem)
      {
        objectiveValues.Add(currentState.Population.Solutions.Single().ObjectiveVector[0]);
      }
    }
  }
}
