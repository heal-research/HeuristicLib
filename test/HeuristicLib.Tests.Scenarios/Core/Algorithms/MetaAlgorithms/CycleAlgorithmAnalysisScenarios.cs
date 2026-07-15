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
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Tests.Scenarios.Core.Algorithms.MetaAlgorithms;

public class CycleAlgorithmAnalysisScenarios
{
    [Fact]
    public void CycleAlgorithm_WithFreshRegistries_PreservesAnalyzerStateAcrossCycles()
    {
        var result = RunCycleAlgorithm(newExecutionInstancesPerCycle: true);

        result.EvaluationTrace1.ObjectiveValues.ShouldBe([1.0, 1.0, 1.0, 1.0, 1.0, 1.0]);
        result.EvaluationTrace2.ObjectiveValues.ShouldBe([1.0, 1.0, 1.0, 1.0, 1.0, 1.0]);
        result.InterceptionTrace.ObjectiveValues.ShouldBe([1.0, 1.0, 1.0, 1.0, 1.0, 1.0]);
        result.FinalState.Population.EvaluatedCandidates.Single().Candidate.ShouldBe(2);
    }

    [Fact]
    public void CycleAlgorithm_WithReusedChildRegistries_PreservesAnalyzerStateAcrossCycles()
    {
        var result = RunCycleAlgorithm(newExecutionInstancesPerCycle: false);

        result.EvaluationTrace1.ObjectiveValues.ShouldBe([1.0, 1.0, 2.0, 2.0, 3.0, 3.0]);
        result.EvaluationTrace2.ObjectiveValues.ShouldBe([1.0, 1.0, 2.0, 2.0, 3.0, 3.0]);
        result.InterceptionTrace.ObjectiveValues.ShouldBe([1.0, 1.0, 2.0, 2.0, 3.0, 3.0]);
        result.FinalState.Population.EvaluatedCandidates.Single().Candidate.ShouldBe(2);
    }

    private static CycleRunResult RunCycleAlgorithm(bool newExecutionInstancesPerCycle)
    {
        var evaluator = new IncrementingEvaluator();
        var interceptor = new IdentityInterceptor<int, PopulationState<int>>();
        var algorithm1 = new SingleStepAlgorithm(1, evaluator, interceptor);
        var algorithm2 = new SingleStepAlgorithm(2, evaluator, interceptor);
        var cycleAlgorithm =
            new CycleAlgorithm<SingleStepAlgorithm, int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>,
                PopulationState<int>>(
                [algorithm1, algorithm2])
            {
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
        var finalState = run.Complete(RandomNumberGenerator.Create(0));

        return new CycleRunResult(
            finalState,
            run.GetAnalyzerResult(evaluationTrace1),
            run.GetAnalyzerResult(evaluationTrace2),
            run.GetAnalyzerResult(interceptionTrace));
    }

    private sealed record CycleRunResult(
        PopulationState<int> FinalState,
        EvaluationTraceAnalysis.ExecutionState EvaluationTrace1,
        EvaluationTraceAnalysis.ExecutionState EvaluationTrace2,
        InterceptionTraceAnalysis.ExecutionState InterceptionTrace);

    private sealed class DummySearchSpace<TCandidate> : ISearchSpace<TCandidate>
    {
        public static readonly DummySearchSpace<TCandidate> Instance = new();

        private DummySearchSpace()
        { }

        public bool Contains(TCandidate candidate) => true;
    }

    private sealed record IncrementingEvaluator
        : Evaluator<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>,
            IncrementingEvaluator.ExecutionState>
    {
        public sealed class ExecutionState
        {
            public int Value { get; set; }
        }

        protected override ExecutionState CreateInitialState() => new();

        protected override IReadOnlyList<ObjectiveVector> Evaluate(
            IReadOnlyList<int> candidates,
            ExecutionState executionState,
            IRandomNumberGenerator random,
            DummySearchSpace<int> searchSpace,
            IProblem<int, DummySearchSpace<int>> problem)
        {
            return candidates.Select(_ => new ObjectiveVector(++executionState.Value)).ToArray();
        }
    }

    private sealed record SingleStepAlgorithm : Algorithm<int, DummySearchSpace<int>,
        IProblem<int, DummySearchSpace<int>>, PopulationState<int>, SingleStepAlgorithm.ExecutionState>
    {
        public new sealed class ExecutionState
            : Algorithm<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>, PopulationState<int>,
                ExecutionState>.ExecutionState
        {
            public required
                IInterceptorInstance<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>,
                    PopulationState<int>> Interceptor { get; init; }
        }

        public int Candidate { get; }

        public IInterceptor<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>, PopulationState<int>>
            Interceptor { get; }

        public SingleStepAlgorithm(
            int candidate,
            IEvaluator<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>> evaluator,
            IInterceptor<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>, PopulationState<int>>
                interceptor)
        {
            Candidate = candidate;
            Interceptor = interceptor;
            Evaluator = evaluator;
        }

        protected override ExecutionState CreateInitialExecutionState(IExecutionInstanceResolver resolver)
            => new()
            {
                Evaluator = resolver.Resolve(Evaluator),
                Interceptor = resolver.Resolve(Interceptor)
            };

        protected override
            IAlgorithmInstance<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>, PopulationState<int>>
            CreateAlgorithmInstance(Run run, ExecutionState executionState)
            => new Instance(run, executionState.Evaluator, executionState.Interceptor, Candidate);

        private sealed class Instance(
            Run run,
            IEvaluatorInstance<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>> evaluator,
            IInterceptorInstance<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>, PopulationState<int>>
                interceptor,
            int candidate)
            : AlgorithmInstance<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>, PopulationState<int>>(
                run, evaluator)
        {
            private readonly
                IInterceptorInstance<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>,
                    PopulationState<int>> interceptor = interceptor;
            private readonly int candidate = candidate;

            public override async IAsyncEnumerable<PopulationState<int>> RunStreamingAsync(
                IProblem<int, DummySearchSpace<int>> problem,
                IRandomNumberGenerator random,
                PopulationState<int>? initialState = null,
                [EnumeratorCancellation] CancellationToken ct = default)
            {
                ct.ThrowIfCancellationRequested();

                var objectiveVector = Evaluator.Evaluate([candidate], random, problem.SearchSpace, problem).Single();
                var currentState = new PopulationState<int>
                {
                    Population = Population.From([EvaluatedCandidate.From(candidate, objectiveVector)])
                };

                yield return interceptor.Transform(currentState, initialState, problem.SearchSpace, problem);
                await Task.CompletedTask;
            }
        }
    }

    private sealed record EvaluationTraceAnalysis(
        IEvaluator<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>> Evaluator)
        : Analyzer<EvaluationTraceAnalysis.ExecutionState>
    {
        public override ExecutionState CreateInitialResult() => new();

        public override void RegisterObservations(ObservationPlan observations, ExecutionState result)
        {
            observations.Observe(Evaluator,
                (_, objectiveVectors, _, _) => result.RecordObjectiveValues(objectiveVectors));
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

    private sealed record InterceptionTraceAnalysis(
        IInterceptor<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>, PopulationState<int>>
            Interceptor)
        : Analyzer<InterceptionTraceAnalysis.ExecutionState>
    {
        public override ExecutionState CreateInitialResult() => new();

        public override void RegisterObservations(ObservationPlan observations, ExecutionState result)
        {
            observations.Observe(Interceptor, (_, currentState, _, _, _) => result.RecordObjectiveValue(currentState));
        }

        public sealed class ExecutionState
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
