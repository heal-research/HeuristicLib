using System.Runtime.CompilerServices;
using HEAL.HeuristicLib.Execution;
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

        var run = algorithm.AnalyzedBy(analysis1, analysis2).CreateRun(problem, RandomNumberGenerator.Create(0));

        run.Complete(cancellationToken: TestContext.Current.CancellationToken);

        analysis1.Result.ObjectiveValues.ShouldBe([1.0]);
        analysis2.Result.ObjectiveValues.ShouldBe([1.0]);
    }

    private sealed record IncrementingEvaluator
        : Evaluator<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>>
    {

        protected override IEvaluationExecutor<int> CreateExecutor<TRunSearchState>(ResolutionScope<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>, TRunSearchState> scope) =>
        new Executor(scope.Problem);

        private sealed class Executor
            : EvaluationExecutor<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>>
        {
            public Executor(IProblem<int, DummySearchSpace<int>> problem) : base(problem) { }

            private int value;

            public override IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<int> candidates, IRandomNumberGenerator random)
            {
                return candidates.Select(_ => new ObjectiveVector(++value)).ToArray();
            }
        }
    }

    private sealed record SingleStepAlgorithm : Algorithm<SingleStepAlgorithm, int, PopulationState<int>>
    {
        public int Candidate { get; }

        public IOperator<Evaluation, int> Evaluator { get; init; }

        public IOperator<Interception<PopulationState<int>>, int> Interceptor { get; init; }

        public static Parameter<SingleStepAlgorithm, IOperator<Evaluation, int>> EvaluatorParameter { get; } =
            new(nameof(Evaluator), static algorithm => algorithm.Evaluator, static (algorithm, value) => algorithm with { Evaluator = value });

        public static Parameter<SingleStepAlgorithm, IOperator<Interception<PopulationState<int>>, int>> InterceptorParameter { get; } =
            new(nameof(Interceptor), static algorithm => algorithm.Interceptor, static (algorithm, value) => algorithm with { Interceptor = value });

        public override IReadOnlyList<IParameter> GetParameters() => [.. base.GetParameters(), EvaluatorParameter, InterceptorParameter];

        public SingleStepAlgorithm(int candidate, IOperator<Evaluation, int> evaluator, IOperator<Interception<PopulationState<int>>, int> interceptor)
        {
            Candidate = candidate;
            Interceptor = interceptor;
            Evaluator = evaluator;
        }

        protected override IAlgorithmExecutor<PopulationState<int>> CreateExecutor<TSearchSpace, TProblem>(
            ResolutionScope<int, TSearchSpace, TProblem, PopulationState<int>> scope) =>
            new Executor(scope.Resolve(Evaluator), scope.Resolve(Interceptor), Candidate);

        private sealed class Executor(IEvaluationExecutor<int> evaluator, IInterceptionExecutor<PopulationState<int>> interceptor, int candidate)
            : AlgorithmExecutor<PopulationState<int>>
        {
            private readonly IEvaluationExecutor<int> evaluator = evaluator;

            private readonly IInterceptionExecutor<PopulationState<int>> interceptor = interceptor;

            private readonly int candidate = candidate;

            public override async IAsyncEnumerable<PopulationState<int>> RunStreamingAsync(IRandomNumberGenerator random, PopulationState<int>? initialState = null, [EnumeratorCancellation] CancellationToken ct = default)
            {
                ct.ThrowIfCancellationRequested();

                var objectiveVector = evaluator.Evaluate([candidate], random).Single();
                var currentState = Population.From([candidate.ToEvaluated(objectiveVector)]).ToPopulationState();

                yield return interceptor.Transform(currentState, initialState, random);
                await Task.CompletedTask;
            }
        }
    }

    // An analyzer of its own, built on Observing.
    private sealed class EvaluationTraceAnalysis
        : ISubstitutionBundle
    {
        private readonly Instrument observing;

        public State Result { get; } = new();

        public EvaluationTraceAnalysis(IOperator<Evaluation, int> evaluator)
        {
            observing = evaluator.Invocations.Observing(Record);
        }

        public IReadOnlyList<Substitution> Substitutions => observing.Substitutions;

        public void Record(EvaluationObservation<int> observation) =>
            Result.RecordObjectiveValues(observation.ObjectiveVectors);

        public sealed class State
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
