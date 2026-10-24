using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Encodings.RealVectors;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Operators.Creators;
using HEAL.HeuristicLib.Operators.Interceptors;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Problems.TestFunctions;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Tests.ApiUsageSpecs.Algorithms;

public class AlgorithmAuthoringSpecs
{
    [Fact]
    public async Task IterativeAlgorithm_AuthoringExample_UsesResolvedExecutions()
    {
        var problem = new TestFunctionProblem(new SphereFunction(dimension: 3));
        var algorithm = new SingleCreateAlgorithm { Creator = new CountingCreator() }.TerminatedAfterIterations(1);

        var finalState = await algorithm.CompleteAsync(
            problem,
            RandomNumberGenerator.Create(42),
            ct: TestContext.Current.CancellationToken);

        finalState.EvaluatedCandidate.Candidate.ShouldBe(new RealVector(1.0, 0.0, 0.0));
    }

    [Fact]
    public async Task IterativeAlgorithm_AuthoringExample_KeepsMutableDataRunScoped()
    {
        var problem = new TestFunctionProblem(new SphereFunction(dimension: 3));
        var algorithm = new DoubleCreateAlgorithm { Creator = new CountingCreator() }.TerminatedAfterIterations(1);

        var firstRunState = await algorithm.CompleteAsync(
            problem,
            RandomNumberGenerator.Create(123),
            ct: TestContext.Current.CancellationToken);

        var secondRunState = await algorithm.CompleteAsync(
            problem,
            RandomNumberGenerator.Create(456),
            ct: TestContext.Current.CancellationToken);

        firstRunState.EvaluatedCandidate.Candidate.ShouldBe(new RealVector(1.0, 2.0, 1.0));
        secondRunState.EvaluatedCandidate.Candidate.ShouldBe(new RealVector(1.0, 2.0, 1.0));
    }

    [Fact]
    public async Task IterativeAlgorithm_AuthoringExample_KeepsConcurrentRunsIndependentAndConfigurationUnchanged()
    {
        var problem = new TestFunctionProblem(new SphereFunction(dimension: 3));
        var creator = new CountingCreator();
        var configuration = new DoubleCreateAlgorithm { Creator = creator };
        var algorithm = configuration.TerminatedAfterIterations(1);

        var runs = await Task.WhenAll(
            algorithm.CompleteAsync(problem, RandomNumberGenerator.Create(123), ct: TestContext.Current.CancellationToken),
            algorithm.CompleteAsync(problem, RandomNumberGenerator.Create(456), ct: TestContext.Current.CancellationToken));

        runs.Select(state => state.EvaluatedCandidate.Candidate).ShouldBe([new RealVector(1.0, 2.0, 1.0), new RealVector(1.0, 2.0, 1.0)]);
        configuration.Creator.ShouldBeSameAs(creator);
    }

    [Fact]
    public async Task IterativeAlgorithm_AuthoringExample_UsesInterceptorsNormally()
    {
        var problem = new TestFunctionProblem(new SphereFunction(dimension: 3));
        var algorithm = new DoubleCreateAlgorithm
        {
            Creator = new CountingCreator(),
            Interceptor = new ThirdCoordinateIncrementingInterceptor()
        }.TerminatedAfterIterations(1);

        var finalState = await algorithm.CompleteAsync(
            problem,
            RandomNumberGenerator.Create(789),
            ct: TestContext.Current.CancellationToken);

        finalState.EvaluatedCandidate.Candidate.ShouldBe(new RealVector(1.0, 2.0, 2.0));
    }

    [Fact]
    public void IterativeAlgorithm_FactoryBindsRuntimeDependenciesWithoutInvokingThem()
    {
        var creator = new InstancingCreator();
        var evaluator = new InstancingEvaluator();
        var interceptor = new InstancingInterceptor();
        var algorithm = new SingleCreateAlgorithm
        {
            Creator = creator,
            Evaluator = evaluator,
            Interceptor = interceptor
        };

        var registry = ResolutionScope.Create();

        _ = registry.Resolve<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem, SingleSolutionState<RealVector>>(algorithm);

        creator.ExecutionsCreated.ShouldBe(1);
        evaluator.ExecutionsCreated.ShouldBe(1);
        interceptor.ExecutionsCreated.ShouldBe(1);
        creator.CreateCalls.ShouldBe(0);
        evaluator.EvaluateCalls.ShouldBe(0);
        interceptor.TransformCalls.ShouldBe(0);
    }

    /// <summary>
    /// Algorithm configurations expose scope-free <c>CreateExecutorFactory</c> publicly. The protected
    /// <c>CreateIterationFactory</c> hook prepares the factory that resolves the interceptor and other children in its construction scope.
    /// </summary>
    [Fact]
    public void Algorithm_CreateExecutionFactory_IsCallableWithoutAnInterfaceCast()
    {
        var algorithm = new SingleCreateAlgorithm { Creator = new CountingCreator() };

        var factory = algorithm.CreateExecutorFactory();
        var execution = factory(ResolutionScope.Create());

        execution.ShouldBeAssignableTo<IterativeAlgorithmExecutor<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem, SingleSolutionState<RealVector>>>();
    }

    private sealed record SingleCreateAlgorithm
        : IterativeAlgorithm<SingleCreateAlgorithm, RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem, SingleSolutionState<RealVector>>
    {
        public required IOperator<Creation, RealVector> Creator { get; init; }
        public IOperator<Evaluation, RealVector> Evaluator { get; init; } = new ProblemEvaluator<RealVector>();

        protected override ExecutorFactory<IterativeAlgorithmExecutor<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem, SingleSolutionState<RealVector>>> CreateIterationFactory() =>
            scope =>
            {
                var typed = scope.For<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem, SingleSolutionState<RealVector>>();
                return new Executor(typed.ResolveOptional(Interceptor), typed.Resolve(Creator), typed.Resolve(Evaluator));
            };

        private sealed class Executor(
            IInterceptionExecutor<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem, SingleSolutionState<RealVector>>? interceptor,
            ICreationExecutor<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem> creator,
            IEvaluationExecutor<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem> evaluator)
            : IterativeAlgorithmExecutor<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem, SingleSolutionState<RealVector>>(interceptor)
        {
            protected override SingleSolutionState<RealVector> ExecuteStep(SingleSolutionState<RealVector>? previousState, TestFunctionProblem problem, IRandomNumberGenerator random)
            {
                var candidate = creator.Create(1, random, problem.SearchSpace, problem)[0];
                var objectiveVector = evaluator.Evaluate([candidate], random, problem.SearchSpace, problem)[0];

                return SingleSolutionState.From(candidate.ToEvaluated(objectiveVector));
            }
        }
    }

    private sealed record DoubleCreateAlgorithm
        : IterativeAlgorithm<DoubleCreateAlgorithm, RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem, SingleSolutionState<RealVector>>
    {
        public required IOperator<Creation, RealVector> Creator { get; init; }
        public IOperator<Evaluation, RealVector> Evaluator { get; init; } = new ProblemEvaluator<RealVector>();

        protected override ExecutorFactory<IterativeAlgorithmExecutor<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem, SingleSolutionState<RealVector>>> CreateIterationFactory()
        {
            var state = new ExecutionState();
            return scope =>
            {
                var typed = scope.For<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem, SingleSolutionState<RealVector>>();
                return new Executor(typed.ResolveOptional(Interceptor), typed.Resolve(Creator), typed.Resolve(Evaluator), state);
            };
        }

        private sealed class ExecutionState
        {
            public int Steps { get; set; }
        }

        private sealed class Executor(
            IInterceptionExecutor<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem, SingleSolutionState<RealVector>>? interceptor,
            ICreationExecutor<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem> creator,
            IEvaluationExecutor<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem> evaluator,
            ExecutionState state)
            : IterativeAlgorithmExecutor<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem, SingleSolutionState<RealVector>>(interceptor)
        {
            protected override SingleSolutionState<RealVector> ExecuteStep(SingleSolutionState<RealVector>? previousState, TestFunctionProblem problem, IRandomNumberGenerator random)
            {
                state.Steps++;

                var first = creator.Create(1, random, problem.SearchSpace, problem)[0];
                var second = creator.Create(1, random, problem.SearchSpace, problem)[0];
                RealVector candidate = [first[0], second[0], state.Steps];
                var objectiveVector = evaluator.Evaluate([candidate], random, problem.SearchSpace, problem)[0];

                return SingleSolutionState.From(candidate.ToEvaluated(objectiveVector));
            }
        }
    }

    private sealed record CountingCreator
        : StatefulCreator<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem, CountingCreator.ExecutionState>
    {
        public sealed class ExecutionState
        {
            public int Calls { get; set; }
        }

        protected override ExecutionState CreateExecutionState()
        {
            return new ExecutionState();
        }

        protected override IReadOnlyList<RealVector> Create(
            int count,
            ExecutionState executionState,
            IRandomNumberGenerator random,
            BoundedRealVectorSearchSpace searchSpace,
            TestFunctionProblem problem)
        {
            return Enumerable.Range(0, count)
                             .Select(_ =>
                             {
                                 executionState.Calls++;
                                 return new RealVector(executionState.Calls, 0.0, 0.0);
                             })
                             .ToArray();
        }
    }

    private sealed record ThirdCoordinateIncrementingInterceptor
        : StatelessInterceptor<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem, SingleSolutionState<RealVector>>
    {
        public override SingleSolutionState<RealVector> Transform(
            SingleSolutionState<RealVector> currentState,
            SingleSolutionState<RealVector>? previousState,
            IRandomNumberGenerator random,
            BoundedRealVectorSearchSpace searchSpace,
            TestFunctionProblem problem)
        {
            var current = currentState.EvaluatedCandidate.Candidate;
            RealVector transformed = [current[0], current[1], current[2] + 1.0];
            return SingleSolutionState.From(transformed, currentState.EvaluatedCandidate.ObjectiveVector);
        }
    }

    private sealed class InstancingCreator : IOperator<Creation, RealVector>
    {
        public int ExecutionsCreated { get; private set; }
        public int CreateCalls { get; private set; }

        IExecutor IOperator<Creation, RealVector>.CreateExecutor<TSearchSpace, TProblem, TSearchState>(
            ResolutionScope<RealVector, TSearchSpace, TProblem, TSearchState> scope)
        {
            ExecutionsCreated++;
            return new Executor(this);
        }

        private sealed class Executor(InstancingCreator owner)
            : ICreationExecutor<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>
        {
            public IReadOnlyList<RealVector> Create(int count, IRandomNumberGenerator random,
                                                    BoundedRealVectorSearchSpace searchSpace, TestFunctionProblem problem)
            {
                owner.CreateCalls++;
                return Enumerable.Range(0, count)
                                 .Select(_ => RealVector.Repeat(0.0, problem.TestFunction.Dimension))
                                 .ToArray();
            }
        }
    }

    private sealed class InstancingEvaluator : IOperator<Evaluation, RealVector>
    {
        public int ExecutionsCreated { get; private set; }
        public int EvaluateCalls { get; private set; }

        IExecutor IOperator<Evaluation, RealVector>.CreateExecutor<TSearchSpace, TProblem, TSearchState>(
            ResolutionScope<RealVector, TSearchSpace, TProblem, TSearchState> scope)
        {
            ExecutionsCreated++;
            return new Executor(this);
        }

        private sealed class Executor(InstancingEvaluator owner)
            : IEvaluationExecutor<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>
        {
            public IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<RealVector> candidates,
                                                           IRandomNumberGenerator random,
                                                           BoundedRealVectorSearchSpace searchSpace,
                                                           TestFunctionProblem problem)
            {
                owner.EvaluateCalls++;
                return candidates
                                 .Select(_ => new ObjectiveVector(0.0))
                                 .ToArray();
            }
        }
    }

    private sealed class InstancingInterceptor : IOperator<Interception<SingleSolutionState<RealVector>>, RealVector>
    {
        public int ExecutionsCreated { get; private set; }
        public int TransformCalls { get; private set; }

        IExecutor IOperator<Interception<SingleSolutionState<RealVector>>, RealVector>.CreateExecutor<TSearchSpace, TProblem, TSearchState>(
            ResolutionScope<RealVector, TSearchSpace, TProblem, TSearchState> scope)
        {
            ExecutionsCreated++;
            return new Executor(this);
        }

        private sealed class Executor(InstancingInterceptor owner)
            : IInterceptionExecutor<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem, SingleSolutionState<RealVector>>
        {
            public SingleSolutionState<RealVector> Transform(SingleSolutionState<RealVector> currentState, SingleSolutionState<RealVector>? previousState,
                IRandomNumberGenerator random, BoundedRealVectorSearchSpace searchSpace, TestFunctionProblem problem)
            {
                owner.TransformCalls++;
                return currentState;
            }
        }
    }
}
