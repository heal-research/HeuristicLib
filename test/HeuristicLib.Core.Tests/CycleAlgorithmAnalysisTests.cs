using System.Collections;
using System.Reflection;
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
  [Fact(Explicit = true)]
  public void CycleAlgorithm_WithFreshRegistries_PreservesAnalyzerStateAcrossCycles()
  {
    var result = RunCycleAlgorithm(newExecutionInstancesPerCycle: true);

    result.EvaluationTrace1.ObjectiveValues.ShouldBe([1.0, 1.0, 1.0, 1.0, 1.0, 1.0]);
    result.EvaluationTrace2.ObjectiveValues.ShouldBe([1.0, 1.0, 1.0, 1.0, 1.0, 1.0]);
    result.InterceptionTrace.ObjectiveValues.ShouldBe([1.0, 1.0, 1.0, 1.0, 1.0, 1.0]);
    result.FinalState.Population.Solutions.Single().Genotype.ShouldBe(2);
  }

  [Fact(Explicit = true)]
  public void CycleAlgorithm_WithReusedChildRegistries_PreservesAnalyzerStateAcrossCycles()
  {
    var result = RunCycleAlgorithm(newExecutionInstancesPerCycle: false);

    result.EvaluationTrace1.ObjectiveValues.ShouldBe([1.0, 1.0, 2.0, 2.0, 3.0, 3.0]);
    result.EvaluationTrace2.ObjectiveValues.ShouldBe([1.0, 1.0, 2.0, 2.0, 3.0, 3.0]);
    result.InterceptionTrace.ObjectiveValues.ShouldBe([1.0, 1.0, 2.0, 2.0, 3.0, 3.0]);
    result.FinalState.Population.Solutions.Single().Genotype.ShouldBe(2);
  }

  [Fact]
  public void ObservationPlan_MergesMultipleAnalyzerCallbacksForSameOperator()
  {
    var evaluator = new IncrementingEvaluator();
    var interceptor = new IdentityInterceptor<int, PopulationState<int>>();
    var algorithm = new SingleStepAlgorithm(1, evaluator, interceptor);
    var analysis1 = new EvaluationTraceAnalysis(evaluator);
    var analysis2 = new EvaluationTraceAnalysis(evaluator);
    var problem = FuncProblem.Create<int, DummySearchSpace<int>>(
      evaluateFunc: x => x,
      encoding: DummySearchSpace<int>.Instance,
      objective: SingleObjective.Minimize);

    var run = algorithm.CreateRun(problem, analysis1, analysis2);

    GetReplacementCount(run).ShouldBe(1);

    run.RunToCompletion(RandomNumberGenerator.Create(0), cancellationToken: TestContext.Current.CancellationToken);

    run.GetAnalyzerResult(analysis1).ObjectiveValues.ShouldBe([1.0]);
    run.GetAnalyzerResult(analysis2).ObjectiveValues.ShouldBe([1.0]);
  }

  [Fact]
  public void GetAnalyzerResult_ReturnsDirectAnalyzerResult()
  {
    var evaluator = new IncrementingEvaluator();
    var analysis = new EvaluationTraceAnalysis(evaluator);
    var run = new AnalyzerTestRun(analysis);

    run.GetAnalyzerResult(analysis).ShouldBeSameAs(run.GetResult(analysis));
  }

  [Fact]
  public void TryGetAnalyzerResult_ReturnsDirectAnalyzerResultWhenPresent()
  {
    var evaluator = new IncrementingEvaluator();
    var analysis = new EvaluationTraceAnalysis(evaluator);
    var run = new AnalyzerTestRun(analysis);

    run.TryGetAnalyzerResult(analysis, out var result).ShouldBeTrue();

    result.ShouldNotBeNull();
    result.ShouldBeSameAs(run.GetAnalyzerResult(analysis));
  }

  [Fact]
  public void TryGetAnalyzerResult_ReturnsFalseWhenAnalyzerWasNotAttached()
  {
    var attached = new EvaluationTraceAnalysis(new IncrementingEvaluator());
    var missing = new EvaluationTraceAnalysis(new IncrementingEvaluator());
    var run = new AnalyzerTestRun(attached);

    run.TryGetAnalyzerResult(missing, out var result).ShouldBeFalse();

    result.ShouldBeNull();
  }

  [Fact]
  public void AnalyzerResultRetrieval_ThrowsInvalidOperationExceptionForMismatchedRunState()
  {
    var analyzer = new MalformedAnalyzer();
    var run = new AnalyzerTestRun(analyzer);

    Should.Throw<InvalidOperationException>(() => run.GetAnalyzerResult(analyzer));
    Should.Throw<InvalidOperationException>(() => run.TryGetAnalyzerResult<MalformedAnalyzer.Result>(analyzer, out _));
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

  private static int GetReplacementCount(Run run)
  {
    var rootRegistryField = typeof(Run).GetField("rootRegistry", BindingFlags.Instance | BindingFlags.NonPublic)!;
    var rootRegistry = ((Lazy<ExecutionInstanceRegistry>)rootRegistryField.GetValue(run)!).Value;
    var replacementExecutablesField = typeof(ExecutionInstanceRegistry).GetField("replacementExecutables", BindingFlags.Instance | BindingFlags.NonPublic)!;
    var replacementExecutables = (IDictionary)replacementExecutablesField.GetValue(rootRegistry)!;
    return replacementExecutables.Count;
  }

  private sealed class AnalyzerTestRun(params IAnalyzer[] analyzers) : Run(analyzers);

  private sealed record CycleRunResult(
    PopulationState<int> FinalState,
    EvaluationTraceAnalysis.ExecutionState EvaluationTrace1,
    EvaluationTraceAnalysis.ExecutionState EvaluationTrace2,
    InterceptionTraceAnalysis.ExecutionState InterceptionTrace);

  private sealed record IncrementingEvaluator
    : Evaluator<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>, IncrementingEvaluator.ExecutionState>
  {
    public sealed class ExecutionState
    {
      public int Value { get; set; }
    }

    protected override ExecutionState CreateInitialState() => new();

    protected override IReadOnlyList<ObjectiveVector> Evaluate(
      IReadOnlyList<int> genotypes,
      ExecutionState executionState,
      IRandomNumberGenerator random,
      DummySearchSpace<int> searchSpace,
      IProblem<int, DummySearchSpace<int>> problem)
    {
      return genotypes.Select(_ => new ObjectiveVector(++executionState.Value)).ToArray();
    }
  }

  private sealed record SingleStepAlgorithm : Algorithm<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>, PopulationState<int>, SingleStepAlgorithm.ExecutionState>
  {
    public new sealed class ExecutionState
      : Algorithm<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>, PopulationState<int>, ExecutionState>.ExecutionState
    {
      public required IInterceptorInstance<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>, PopulationState<int>> Interceptor { get; init; }
    }

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

    protected override ExecutionState CreateInitialExecutionState(IExecutionInstanceResolver resolver)
      => new() {
        Evaluator = resolver.Resolve(Evaluator),
        Interceptor = resolver.Resolve(Interceptor)
      };

    protected override IAlgorithmInstance<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>, PopulationState<int>> CreateAlgorithmInstance(Run run, ExecutionState executionState)
      => new Instance(run, executionState.Evaluator, executionState.Interceptor, Genotype);

    private sealed class Instance(
      Run run,
      IEvaluatorInstance<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>> evaluator,
      IInterceptorInstance<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>, PopulationState<int>> interceptor,
      int genotype)
      : AlgorithmInstance<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>, PopulationState<int>>(run, evaluator)
    {
      private readonly IInterceptorInstance<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>, PopulationState<int>> interceptor = interceptor;
      private readonly int genotype = genotype;

      public override async IAsyncEnumerable<PopulationState<int>> RunStreamingAsync(
        IProblem<int, DummySearchSpace<int>> problem,
        IRandomNumberGenerator random,
        PopulationState<int>? initialState = null,
        [EnumeratorCancellation]
        CancellationToken ct = default)
      {
        ct.ThrowIfCancellationRequested();

        var objectiveVector = Evaluator.Evaluate([genotype], random, problem.SearchSpace, problem).Single();
        var currentState = new PopulationState<int> {
          Population = Population.From([Solution.From(genotype, objectiveVector)])
        };

        yield return interceptor.Transform(currentState, initialState, problem.SearchSpace, problem);
        await Task.CompletedTask;
      }
    }
  }

  private sealed record EvaluationTraceAnalysis(IEvaluator<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>> Evaluator)
    : Analyzer<EvaluationTraceAnalysis.ExecutionState>
  {
    public override ExecutionState CreateInitialResult() => new();

    public override void RegisterObservations(ObservationPlan observations, ExecutionState result)
    {
      observations.Observe(Evaluator, (_, objectiveVectors, _, _) => result.AddObjectiveValues(objectiveVectors));
    }

    public sealed class ExecutionState
    {
      private readonly List<double> objectiveValues = [];

      public IReadOnlyList<double> ObjectiveValues => objectiveValues;

      public void AddObjectiveValues(IReadOnlyList<ObjectiveVector> objectiveVectors)
      {
        objectiveValues.AddRange(objectiveVectors.Select(x => x[0]));
      }
    }
  }

  private sealed record InterceptionTraceAnalysis(IInterceptor<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>, PopulationState<int>> Interceptor)
    : Analyzer<InterceptionTraceAnalysis.ExecutionState>
  {
    public override ExecutionState CreateInitialResult() => new();

    public override void RegisterObservations(ObservationPlan observations, ExecutionState result)
    {
      observations.Observe(Interceptor, (_, currentState, _, _, _) => result.AddObjectiveValue(currentState));
    }

    public sealed class ExecutionState
    {
      private readonly List<double> objectiveValues = [];

      public IReadOnlyList<double> ObjectiveValues => objectiveValues;

      public void AddObjectiveValue(PopulationState<int> currentState)
      {
        objectiveValues.Add(currentState.Population.Solutions.Single().ObjectiveVector[0]);
      }
    }
  }

  private sealed class MalformedAnalyzer : IAnalyzer<MalformedAnalyzer.Result>
  {
    public IAnalyzerRunState<Result> CreateAnalyzerState() => new CorrectRunState();

    IAnalyzerRunState IAnalyzer.CreateAnalyzerState() => new WrongRunState();

    public sealed class Result;

    private sealed class CorrectRunState : IAnalyzerRunState<Result>
    {
      public Result Result { get; } = new();

      public void RegisterObservations(ObservationPlan observations)
      {
      }
    }

    private sealed class WrongRunState : IAnalyzerRunState
    {
      public void RegisterObservations(ObservationPlan observations)
      {
      }
    }
  }
}

