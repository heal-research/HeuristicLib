using System.Runtime.CompilerServices;
using HEAL.HeuristicLib.Operators.Evaluators;
using HEAL.HeuristicLib.Problems;
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

        var run = algorithm.CreateRun(problem, RandomNumberGenerator.Create(0)).AddAnalyzer(analysis1).AddAnalyzer(analysis2);

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
            return candidates.Select(candidate => new ObjectiveVector(++executionState.Value)).ToArray();
        }
    }

    private sealed record SingleStepAlgorithm : Algorithm<SingleStepAlgorithm, int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>, PopulationState<int>>
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

        public override AlgorithmInstance<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>, PopulationState<int>> CreateExecutionInstance(ExecutionInstanceResolver resolver) =>
            new Instance(resolver.Resolve(Evaluator), resolver.Resolve(Interceptor), Candidate);

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
                var currentState = Population.From([candidate.ToEvaluated(objectiveVector)]).ToPopulationState();

                yield return interceptor.Transform(currentState, initialState, random, problem.SearchSpace, problem);
                await Task.CompletedTask;
            }
        }
    }

    private sealed class EvaluationTraceAnalysis(IEvaluator<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>> evaluator)
        : IAnalyzer
    {
        public ExecutionState Result { get; } = new();

        public void Install(ExecutionInstanceResolverBuilder builder) => builder.Observe(evaluator, Record);

        public void Record(EvaluatorObservation<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>> observation) =>
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
