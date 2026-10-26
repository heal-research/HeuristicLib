using System.Runtime.CompilerServices;
using HEAL.HeuristicLib.Inspection;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Operators.Evaluators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Tests.Scenarios.Core.Algorithms.MetaAlgorithms;

public class CycleAlgorithmAnalysisScenarios
{
    [Fact]
    public void CycleAlgorithm_WithFreshScopes_PreservesAnalyzerStateAcrossCycles()
    {
        var result = RunCycleAlgorithm(ChildScopePolicy.Fresh);

        result.EvaluationTrace1.ObjectiveValues.ShouldBe([1.0, 1.0, 1.0, 1.0, 1.0, 1.0]);
        result.EvaluationTrace2.ObjectiveValues.ShouldBe([1.0, 1.0, 1.0, 1.0, 1.0, 1.0]);
        result.InterceptionTrace.ObjectiveValues.ShouldBe([1.0, 1.0, 1.0, 1.0, 1.0, 1.0]);
        result.FinalState.Population.EvaluatedCandidates.Single().Candidate.ShouldBe(2);
    }

    [Fact]
    public void CycleAlgorithm_WithSharedState_SharesSiblingDependenciesAndPreservesAnalyzerState()
    {
        var result = RunCycleAlgorithm(ChildScopePolicy.Shared);

        result.EvaluationTrace1.ObjectiveValues.ShouldBe([1.0, 2.0, 3.0, 4.0, 5.0, 6.0]);
        result.EvaluationTrace2.ObjectiveValues.ShouldBe([1.0, 2.0, 3.0, 4.0, 5.0, 6.0]);
        result.InterceptionTrace.ObjectiveValues.ShouldBe([1.0, 2.0, 3.0, 4.0, 5.0, 6.0]);
        result.FinalState.Population.EvaluatedCandidates.Single().Candidate.ShouldBe(2);
    }

    [Fact]
    public void CycleAlgorithm_WithRetainedState_IsolatesSiblingDependenciesAndPreservesAnalyzerState()
    {
        var result = RunCycleAlgorithm(ChildScopePolicy.Retained);

        result.EvaluationTrace1.ObjectiveValues.ShouldBe([1.0, 1.0, 2.0, 2.0, 3.0, 3.0]);
        result.EvaluationTrace2.ObjectiveValues.ShouldBe([1.0, 1.0, 2.0, 2.0, 3.0, 3.0]);
        result.InterceptionTrace.ObjectiveValues.ShouldBe([1.0, 1.0, 2.0, 2.0, 3.0, 3.0]);
        result.FinalState.Population.EvaluatedCandidates.Single().Candidate.ShouldBe(2);
    }

    private static CycleRunResult RunCycleAlgorithm(ChildScopePolicy mode)
    {
        var evaluator = new IncrementingEvaluator();
        var interceptor = new IdentityInterceptor<int, PopulationState<int>>();
        var algorithm1 = new SingleStepAlgorithm(1, evaluator, interceptor);
        var algorithm2 = new SingleStepAlgorithm(2, evaluator, interceptor);
        var cycleAlgorithm = algorithm1.CycleWith(algorithm2, maximumCycles: 3) with
        {
            ScopePolicy = mode
        };

        var evaluationTrace1 = new EvaluationTraceAnalysis(evaluator);
        var evaluationTrace2 = new EvaluationTraceAnalysis(evaluator);
        var interceptionTrace = new InterceptionTraceAnalysis(interceptor);
        var problem = FuncProblem.Create(
            evaluateFunc: (int x) => x,
            encoding: DummySearchSpace<int>.Instance,
            objective: SingleObjective.Minimize);

        var run = cycleAlgorithm.CreateRun(problem, RandomNumberGenerator.Create(0)).AddAspect(evaluationTrace1).AddAspect(evaluationTrace2).AddAspect(interceptionTrace);
        var finalState = run.Complete();

        return new CycleRunResult(
            finalState,
            evaluationTrace1.Result,
            evaluationTrace2.Result,
            interceptionTrace.Result);
    }

    private sealed record CycleRunResult(PopulationState<int> FinalState, EvaluationTraceAnalysis.State EvaluationTrace1, EvaluationTraceAnalysis.State EvaluationTrace2, InterceptionTraceAnalysis.State InterceptionTrace);

    private sealed class DummySearchSpace<TCandidate> : ISearchSpace<TCandidate>
    {
        public static readonly DummySearchSpace<TCandidate> Instance = new();

        private DummySearchSpace()
        { }

        public bool Contains(TCandidate candidate) => true;
    }

    private sealed record IncrementingEvaluator
        : StatefulEvaluator<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>, IncrementingEvaluator.State>
    {
        public sealed class State
        {
            public int Value { get; set; }
        }

        protected override State CreateScopedState() => new();

        protected override IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<int> candidates, State state, IRandomNumberGenerator random, DummySearchSpace<int> searchSpace, IProblem<int, DummySearchSpace<int>> problem)
        {
            return candidates.Select(candidate => new ObjectiveVector(++state.Value)).ToArray();
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

        protected override IAlgorithmExecutor<int, TSearchSpace, TProblem, PopulationState<int>> CreateExecutor<TSearchSpace, TProblem>(
            ResolutionScope<int, TSearchSpace, TProblem, PopulationState<int>> scope) =>
            new Executor<TSearchSpace, TProblem>(scope.Resolve(Evaluator), scope.Resolve(Interceptor), Candidate);

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
        public State Result { get; } = new();

        public EvaluationTraceAnalysis(IOperator<Evaluation, int> evaluator)
        {
            Advices = [.. Aspect.Observe(evaluator.Invocations, Record)];
        }

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

    private sealed class InterceptionTraceAnalysis
        : Aspect
    {
        public State Result { get; } = new();

        public InterceptionTraceAnalysis(IOperator<Interception<PopulationState<int>>, int> interceptor)
        {
            Advices = [.. Aspect.Observe(interceptor.Invocations, Record)];
        }

        public void Record(InterceptionObservation<int, PopulationState<int>> observation) =>
            Result.RecordObjectiveValue(observation.UntransformedState);

        public sealed class State
        {
            private readonly List<double> objectiveValues = [];

            public IReadOnlyList<double> ObjectiveValues => objectiveValues;

            public void RecordObjectiveValue(PopulationState<int> currentState)
            {
                objectiveValues.Add(currentState.Population.EvaluatedCandidates.Single().ObjectiveVector[0]);
            }
        }
    }
}
