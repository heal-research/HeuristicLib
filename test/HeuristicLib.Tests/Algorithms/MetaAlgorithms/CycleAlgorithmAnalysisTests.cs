using System.Runtime.CompilerServices;
using HEAL.HeuristicLib.Operators.Evaluators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
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
        var problem = FuncProblem.Create(
            evaluateFunc: (int x) => x,
            encoding: DummySearchSpace<int>.Instance,
            objective: SingleObjective.Minimize);

        var run = algorithm.CreateRun(problem, RandomNumberGenerator.Create(0)).WithAnalyzers(analysis1, analysis2);

        run.Complete(cancellationToken: TestContext.Current.CancellationToken);

        run.GetResult(analysis1).ObjectiveValues.ShouldBe([1.0]);
        run.GetResult(analysis2).ObjectiveValues.ShouldBe([1.0]);
    }

    [Fact]
    public void GetResult_ReturnsSameResultOnRepeatedCalls()
    {
        var evaluator = new IncrementingEvaluator();
        var analysis = new EvaluationTraceAnalysis(evaluator);
        var run = CreateRun(analysis);
        run.Complete(cancellationToken: TestContext.Current.CancellationToken);

        var result = run.GetResult(analysis);

        result.ShouldBeSameAs(run.GetResult(analysis));
    }

    [Fact]
    public void TryGetResult_ReturnsDirectResultWhenPresent()
    {
        var evaluator = new IncrementingEvaluator();
        var analysis = new EvaluationTraceAnalysis(evaluator);
        var run = CreateRun(analysis);
        run.Complete(cancellationToken: TestContext.Current.CancellationToken);

        run.TryGetResult(analysis, out var result).ShouldBeTrue();

        result.ShouldNotBeNull();
        result.ShouldBeSameAs(run.GetResult(analysis));
    }

    [Fact]
    public void TryGetResult_ReturnsFalseWhenAnalyzerWasNotAttached()
    {
        var attached = new EvaluationTraceAnalysis(new IncrementingEvaluator());
        var missing = new EvaluationTraceAnalysis(new IncrementingEvaluator());
        var run = CreateRun(attached);
        run.Complete(cancellationToken: TestContext.Current.CancellationToken);

        run.TryGetResult(missing, out var result).ShouldBeFalse();

        result.ShouldBeNull();
    }

    [Fact]
    public void ResultRetrieval_ThrowsInvalidOperationExceptionForMismatchedRunState()
    {
        var analyzer = new MalformedAnalyzer();
        var run = CreateRun(analyzer);
        run.Complete(cancellationToken: TestContext.Current.CancellationToken);

        Should.Throw<InvalidOperationException>(() => run.GetResult(analyzer));
        Should.Throw<InvalidOperationException>(() => run.TryGetResult<MalformedAnalyzer.Result>(analyzer, out _));
    }

    private static AlgorithmRun<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>, PopulationState<int>> CreateRun(IAnalyzer analyzer)
    {
        var evaluator = new IncrementingEvaluator();
        var problem = FuncProblem.Create(evaluateFunc: (int x) => x, encoding: DummySearchSpace<int>.Instance, objective: SingleObjective.Minimize);
        var algorithm = new SingleStepAlgorithm(1, evaluator, new IdentityInterceptor<int, PopulationState<int>>());
        return algorithm.CreateRun(problem, RandomNumberGenerator.Create(0)).WithAnalyzer(analyzer);
    }

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
            return candidates.Select(candidate => new ObjectiveVector(++executionState.Value)).ToArray();
        }
    }

    private sealed record SingleStepAlgorithm : Algorithm<SingleStepAlgorithm, int, PopulationState<int>>
    {
        public int Candidate { get; }

        public IEvaluator<int> Evaluator { get; }

        public IInterceptor<int> Interceptor { get; }

        public SingleStepAlgorithm(int candidate, IEvaluator<int> evaluator, IInterceptor<int> interceptor)
        {
            Candidate = candidate;
            Interceptor = interceptor;
            Evaluator = evaluator;
        }

        public override IAlgorithmInstance<int, TRunSearchSpace, TRunProblem, PopulationState<int>> CreateExecutionInstance<TRunSearchSpace, TRunProblem>(ExecutionInstanceRegistry instanceRegistry)
        {
            var resolver = instanceRegistry.For<int, TRunSearchSpace, TRunProblem, PopulationState<int>>();
            return new Instance<TRunSearchSpace, TRunProblem>(resolver.Resolve(Evaluator), resolver.Resolve(Interceptor), Candidate);
        }

        private sealed class Instance<TSearchSpace, TProblem>(IEvaluatorInstance<int, TSearchSpace, TProblem> evaluator, IInterceptorInstance<int, TSearchSpace, TProblem, PopulationState<int>> interceptor, int candidate)
            : AlgorithmInstance<int, TSearchSpace, TProblem, PopulationState<int>>
            where TSearchSpace : class, ISearchSpace<int>
            where TProblem : class, IProblem<int, TSearchSpace>
        {
            private readonly IEvaluatorInstance<int, TSearchSpace, TProblem> evaluator = evaluator;

            private readonly IInterceptorInstance<int, TSearchSpace, TProblem, PopulationState<int>> interceptor = interceptor;

            private readonly int candidate = candidate;

            public override async IAsyncEnumerable<PopulationState<int>> RunStreamingAsync(TProblem problem, IRandomNumberGenerator random, PopulationState<int>? initialState = null, [EnumeratorCancellation] CancellationToken ct = default)
            {
                ct.ThrowIfCancellationRequested();

                var objectiveVector = evaluator.Evaluate([candidate], random, problem.SearchSpace, problem).Single();
                var currentState = Population.From([candidate.ToEvaluated(objectiveVector)]).ToPopulationState();

                yield return interceptor.Transform(currentState, initialState, random, problem.SearchSpace, problem);
                await Task.CompletedTask;
            }
        }
    }

    private sealed record EvaluationTraceAnalysis(IEvaluator<int> Evaluator)
        : Analyzer<EvaluationTraceAnalysis.ExecutionState>
    {
        public override ExecutionState CreateInitialResult() => new();

        public override void RegisterObservations(ObservationPlan observations, ExecutionState result)
        {
            observations.Observe<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>>(Evaluator, (objectiveVectors, _, _, _) => result.RecordObjectiveValues(objectiveVectors));
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
