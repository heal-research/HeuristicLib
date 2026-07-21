using System.Collections;
using System.Reflection;
using System.Runtime.CompilerServices;
using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Analysis;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Operators.Evaluators;
using HEAL.HeuristicLib.Operators.Interceptors;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.States;
using HEAL.HeuristicLib.Tests.TestSupport.Mocks;

namespace HEAL.HeuristicLib.Tests.Algorithms.MetaAlgorithms;

public class CycleAlgorithmAnalysisTests
{
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

        run.Complete(RandomNumberGenerator.Create(0), cancellationToken: TestContext.Current.CancellationToken);

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
        Should.Throw<InvalidOperationException>(() =>
            run.TryGetAnalyzerResult<MalformedAnalyzer.Result>(analyzer, out _));
    }

    private static int GetReplacementCount(Run run)
    {
        var rootRegistryField = typeof(Run).GetField("rootRegistry", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var rootRegistry = ((Lazy<ExecutionInstanceRegistry>)rootRegistryField.GetValue(run)!).Value;
        var replacementResolvablesField = typeof(ExecutionInstanceRegistry).GetField("replacementResolvables",
            BindingFlags.Instance | BindingFlags.NonPublic)!;
        var replacementResolvables = (IDictionary)replacementResolvablesField.GetValue(rootRegistry)!;
        return replacementResolvables.Count;
    }

    private sealed class AnalyzerTestRun(params IAnalyzer[] analyzers) : Run(analyzers);

    private sealed record IncrementingEvaluator
        : StatefulEvaluator<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>,
            IncrementingEvaluator.ExecutionState>
    {
        public sealed class ExecutionState
        {
            public int Value { get; set; }
        }

        protected override ExecutionState CreateInitialState() => new();

        protected override IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<int> candidates, ExecutionState executionState, IRandomNumberGenerator random, DummySearchSpace<int> searchSpace, IProblem<int, DummySearchSpace<int>> problem)
        {
            return candidates.Select(_ => new ObjectiveVector(++executionState.Value)).ToArray();
        }
    }

    private sealed record SingleStepAlgorithm : Algorithm<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>, PopulationState<int>>
    {
        public int Candidate { get; }

        public IEvaluator<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>> Evaluator { get; }

        public IInterceptor<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>, PopulationState<int>> Interceptor { get; }

        public SingleStepAlgorithm(int candidate, IEvaluator<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>> evaluator, IInterceptor<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>, PopulationState<int>> interceptor)
        {
            Candidate = candidate;
            Interceptor = interceptor;
            Evaluator = evaluator;
        }

        protected override AlgorithmInstance<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>, PopulationState<int>> CreateAlgorithmInstance(ExecutionInstanceRegistry registry) =>
            new Instance(registry.Resolve(Evaluator), registry.Resolve(Interceptor), Candidate);

        private sealed class Instance(IEvaluatorInstance<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>> evaluator, IInterceptorInstance<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>, PopulationState<int>> interceptor, int candidate)
            : AlgorithmInstance<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>, PopulationState<int>>
        {
            private readonly IEvaluatorInstance<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>> evaluator = evaluator;

            private readonly IInterceptorInstance<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>, PopulationState<int>> interceptor = interceptor;

            private readonly int candidate = candidate;

            public override async IAsyncEnumerable<PopulationState<int>> RunStreamingAsync(IProblem<int, DummySearchSpace<int>> problem, IRandomNumberGenerator random, PopulationState<int>? initialState = null, [EnumeratorCancellation] CancellationToken ct = default)
            {
                ct.ThrowIfCancellationRequested();

                var objectiveVector = evaluator.Evaluate([candidate], random, problem.SearchSpace, problem).Single();
                var currentState = new PopulationState<int>
                {
                    Population = Population.From([EvaluatedCandidate.From(candidate, objectiveVector)])
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
            observations.Observe(Evaluator, (_, objectiveVectors, _, _) => result.RecordObjectiveValues(objectiveVectors));
        }

        public sealed class ExecutionState
        {
            private readonly List<double> objectiveValues = [];

            public IReadOnlyList<double> ObjectiveValues => objectiveValues;

            public void RecordObjectiveValues(IReadOnlyList<ObjectiveVector> objectiveVectors)
            {
                objectiveValues.AddRange(objectiveVectors.Select(x => x[0]));
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
            { }
        }

        private sealed class WrongRunState : IAnalyzerRunState
        {
            public void RegisterObservations(ObservationPlan observations)
            { }
        }
    }
}
