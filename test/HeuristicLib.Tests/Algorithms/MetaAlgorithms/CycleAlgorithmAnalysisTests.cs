using System.Runtime.CompilerServices;
using HEAL.HeuristicLib.Inspection;
using HEAL.HeuristicLib.Operators.Evaluators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.Tests.TestSupport.Mocks;

namespace HEAL.HeuristicLib.Tests.Algorithms.MetaAlgorithms;

public class CycleAlgorithmAnalysisTests
{
    [Fact]
    public void SeveralAnalyzers_ObserveTheSameOperatorIndependently()
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

        var run = algorithm.CreateRun(problem, RandomNumberGenerator.Create(0)).AddAspect(analysis1).AddAspect(analysis2);

        run.Complete(cancellationToken: TestContext.Current.CancellationToken);

        analysis1.Result.ObjectiveValues.ShouldBe([1.0]);
        analysis2.Result.ObjectiveValues.ShouldBe([1.0]);
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
            return candidates.Select(_ => new ObjectiveVector(++executionState.Value)).ToArray();
        }
    }

    private sealed record SingleStepAlgorithm : Algorithm<SingleStepAlgorithm, int, PopulationState<int>>
    {
        public int Candidate { get; }

        public IOperator<Evaluation, int> Evaluator { get; }

        public IOperator<Interception<PopulationState<int>>, int> Interceptor { get; }

        public SingleStepAlgorithm(int candidate, IOperator<Evaluation, int> evaluator, IOperator<Interception<PopulationState<int>>, int> interceptor)
        {
            Candidate = candidate;
            Interceptor = interceptor;
            Evaluator = evaluator;
        }

        public override ExecutorFactory<IAlgorithmExecutor<int, TRunSearchSpace, TRunProblem, PopulationState<int>>> CreateExecutorFactory<TRunSearchSpace, TRunProblem>() => scope =>
        {
            var typed = scope.For<int, TRunSearchSpace, TRunProblem, PopulationState<int>>();
            return new Executor<TRunSearchSpace, TRunProblem>(typed.Resolve(Evaluator), typed.Resolve(Interceptor), Candidate);
        };

        private sealed class Executor<TSearchSpace, TProblem>(IEvaluationExecutor<int, TSearchSpace, TProblem> evaluator, IInterceptionExecutor<int, TSearchSpace, TProblem, PopulationState<int>> interceptor, int candidate)
            : AlgorithmExecutor<int, TSearchSpace, TProblem, PopulationState<int>>
            where TSearchSpace : class, ISearchSpace<int>
            where TProblem : class, IProblem<int, TSearchSpace>
        {
            private readonly IEvaluationExecutor<int, TSearchSpace, TProblem> evaluator = evaluator;

            private readonly IInterceptionExecutor<int, TSearchSpace, TProblem, PopulationState<int>> interceptor = interceptor;

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

    private sealed class EvaluationTraceAnalysis
        : Aspect
    {
        public ExecutionState Result { get; } = new();

        public EvaluationTraceAnalysis(IOperator<Evaluation, int> evaluator)
        {
            Advices = Aspect.Observe(evaluator, Record).Advices;
        }

        public void Record(OperatorObservation<Evaluation, int> observation) =>
            Result.RecordObjectiveValues(observation.ObjectiveVectors);

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
}
