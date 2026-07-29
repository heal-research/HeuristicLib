using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Algorithms.MetaAlgorithms;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Genotypes.Vectors;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Operators.Creators;
using HEAL.HeuristicLib.Operators.Evaluators;
using HEAL.HeuristicLib.Operators.Interceptors;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems.TestFunctions;
using HEAL.HeuristicLib.Problems.TestFunctions.SingleObjectives;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces.Vectors;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Tests.ApiUsageSpecs.Algorithms;

public class AlgorithmAuthoringSpecs
{
    [Fact]
    public async Task IterativeAlgorithm_AuthoringExample_UsesResolvedExecutionInstances()
    {
        var problem = new TestFunctionProblem(new SphereFunction(dimension: 3));
        var algorithm = new SingleCreateAlgorithm { Creator = new CountingCreator() }.WithMaxIterations(1);

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
        var algorithm = new DoubleCreateAlgorithm { Creator = new CountingCreator() }.WithMaxIterations(1);

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
        var algorithm = configuration.WithMaxIterations(1);

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
        }.WithMaxIterations(1);

        var finalState = await algorithm.CompleteAsync(
            problem,
            RandomNumberGenerator.Create(789),
            ct: TestContext.Current.CancellationToken);

        finalState.EvaluatedCandidate.Candidate.ShouldBe(new RealVector(1.0, 2.0, 2.0));
    }

    [Fact]
    public void IterativeAlgorithm_CreateExecutionInstance_EagerlyResolvesRuntimeDependenciesOnce()
    {
        var problem = new TestFunctionProblem(new SphereFunction(dimension: 3));
        var creator = new InstancingCreator();
        var evaluator = new InstancingEvaluator();
        var interceptor = new InstancingInterceptor();
        var algorithm = new SingleCreateAlgorithm
        {
            Creator = creator,
            Evaluator = evaluator,
            Interceptor = interceptor
        };

        var registry = new ExecutionInstanceRegistry();

        _ = registry.Resolve(algorithm);

        creator.ExecutionInstancesCreated.ShouldBe(1);
        evaluator.ExecutionInstancesCreated.ShouldBe(1);
        interceptor.ExecutionInstancesCreated.ShouldBe(1);
        creator.CreateCalls.ShouldBe(0);
        evaluator.EvaluateCalls.ShouldBe(0);
        interceptor.TransformCalls.ShouldBe(0);
    }

    [Fact]
    public void Algorithm_CreateExecutionInstance_IsHiddenFromTheConcreteConfigurationSurface()
    {
        typeof(SingleCreateAlgorithm).GetMethod("CreateExecutionInstance").ShouldBeNull();
    }

    private sealed record SingleCreateAlgorithm
        : IterativeAlgorithm<SingleCreateAlgorithm, RealVector, RealVectorSearchSpace, TestFunctionProblem, SingleSolutionState<RealVector>>
    {
        public required ICreator<RealVector, RealVectorSearchSpace, TestFunctionProblem> Creator { get; init; }
        public IEvaluator<RealVector, RealVectorSearchSpace, TestFunctionProblem> Evaluator { get; init; } = new DirectEvaluator<RealVector>();

        protected override IterativeAlgorithmInstance<RealVector, RealVectorSearchSpace, TestFunctionProblem, SingleSolutionState<RealVector>> CreateIterativeAlgorithmInstance(
            ExecutionInstanceRegistry registry,
            IInterceptorInstance<RealVector, RealVectorSearchSpace, TestFunctionProblem, SingleSolutionState<RealVector>>? resolvedInterceptor)
        {
            return new Instance(resolvedInterceptor, registry.Resolve(Creator), registry.Resolve(Evaluator));
        }

        private sealed class Instance(
            IInterceptorInstance<RealVector, RealVectorSearchSpace, TestFunctionProblem, SingleSolutionState<RealVector>>? interceptor,
            ICreatorInstance<RealVector, RealVectorSearchSpace, TestFunctionProblem> creator,
            IEvaluatorInstance<RealVector, RealVectorSearchSpace, TestFunctionProblem> evaluator)
            : IterativeAlgorithmInstance<RealVector, RealVectorSearchSpace, TestFunctionProblem, SingleSolutionState<RealVector>>(interceptor)
        {
            protected override SingleSolutionState<RealVector> ExecuteStep(SingleSolutionState<RealVector>? previousState, TestFunctionProblem problem, IRandomNumberGenerator random)
            {
                var candidate = creator.Create(1, random, problem.SearchSpace, problem)[0];
                var objective = evaluator.Evaluate([candidate], random, problem.SearchSpace, problem)[0];

                return SingleSolutionState.From(candidate, objective);
            }
        }
    }

    private sealed record DoubleCreateAlgorithm
        : IterativeAlgorithm<DoubleCreateAlgorithm, RealVector, RealVectorSearchSpace, TestFunctionProblem, SingleSolutionState<RealVector>>
    {
        public required ICreator<RealVector, RealVectorSearchSpace, TestFunctionProblem> Creator { get; init; }
        public IEvaluator<RealVector, RealVectorSearchSpace, TestFunctionProblem> Evaluator { get; init; } = new DirectEvaluator<RealVector>();

        protected override IterativeAlgorithmInstance<RealVector, RealVectorSearchSpace, TestFunctionProblem, SingleSolutionState<RealVector>> CreateIterativeAlgorithmInstance(
            ExecutionInstanceRegistry registry,
            IInterceptorInstance<RealVector, RealVectorSearchSpace, TestFunctionProblem, SingleSolutionState<RealVector>>? resolvedInterceptor)
        {
            return new Instance(resolvedInterceptor, registry.Resolve(Creator), registry.Resolve(Evaluator));
        }

        private sealed class Instance(
            IInterceptorInstance<RealVector, RealVectorSearchSpace, TestFunctionProblem, SingleSolutionState<RealVector>>? interceptor,
            ICreatorInstance<RealVector, RealVectorSearchSpace, TestFunctionProblem> creator,
            IEvaluatorInstance<RealVector, RealVectorSearchSpace, TestFunctionProblem> evaluator)
            : IterativeAlgorithmInstance<RealVector, RealVectorSearchSpace, TestFunctionProblem, SingleSolutionState<RealVector>>(interceptor)
        {
            private int steps;

            protected override SingleSolutionState<RealVector> ExecuteStep(SingleSolutionState<RealVector>? previousState, TestFunctionProblem problem, IRandomNumberGenerator random)
            {
                steps++;

                var first = creator.Create(1, random, problem.SearchSpace, problem)[0];
                var second = creator.Create(1, random, problem.SearchSpace, problem)[0];
                RealVector candidate = [first[0], second[0], steps];
                var objective = evaluator.Evaluate([candidate], random, problem.SearchSpace, problem)[0];

                return SingleSolutionState.From(candidate, objective);
            }
        }
    }

    private sealed record CountingCreator
        : StatefulCreator<RealVector, RealVectorSearchSpace, TestFunctionProblem, CountingCreator.ExecutionState>
    {
        public sealed class ExecutionState
        {
            public int Calls { get; set; }
        }

        protected override ExecutionState CreateInitialState()
        {
            return new ExecutionState();
        }

        protected override IReadOnlyList<RealVector> Create(
            int count,
            ExecutionState executionState,
            IRandomNumberGenerator random,
            RealVectorSearchSpace searchSpace,
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
        : StatelessInterceptor<RealVector, RealVectorSearchSpace, TestFunctionProblem, SingleSolutionState<RealVector>>
    {
        public override SingleSolutionState<RealVector> Transform(
            SingleSolutionState<RealVector> currentState,
            SingleSolutionState<RealVector>? previousState,
            RealVectorSearchSpace searchSpace,
            TestFunctionProblem problem)
        {
            var current = currentState.EvaluatedCandidate.Candidate;
            RealVector transformed = [current[0], current[1], current[2] + 1.0];
            return SingleSolutionState.From(transformed, currentState.EvaluatedCandidate.ObjectiveVector);
        }
    }

    private sealed class InstancingCreator : ICreator<RealVector, RealVectorSearchSpace, TestFunctionProblem>
    {
        public int ExecutionInstancesCreated { get; private set; }
        public int CreateCalls { get; private set; }

        public ICreatorInstance<RealVector, RealVectorSearchSpace, TestFunctionProblem> CreateExecutionInstance(
            ExecutionInstanceRegistry instanceRegistry)
        {
            ExecutionInstancesCreated++;
            return new Instance(this);
        }

        private sealed class Instance(InstancingCreator owner)
            : ICreatorInstance<RealVector, RealVectorSearchSpace, TestFunctionProblem>
        {
            public IReadOnlyList<RealVector> Create(int count, IRandomNumberGenerator random,
                                                    RealVectorSearchSpace searchSpace, TestFunctionProblem problem)
            {
                owner.CreateCalls++;
                return Enumerable.Range(0, count)
                                 .Select(_ => RealVector.Repeat(0.0, problem.TestFunction.Dimension))
                                 .ToArray();
            }
        }
    }

    private sealed class InstancingEvaluator : IEvaluator<RealVector, RealVectorSearchSpace, TestFunctionProblem>
    {
        public int ExecutionInstancesCreated { get; private set; }
        public int EvaluateCalls { get; private set; }

        public IEvaluatorInstance<RealVector, RealVectorSearchSpace, TestFunctionProblem> CreateExecutionInstance(
            ExecutionInstanceRegistry instanceRegistry)
        {
            ExecutionInstancesCreated++;
            return new Instance(this);
        }

        private sealed class Instance(InstancingEvaluator owner)
            : IEvaluatorInstance<RealVector, RealVectorSearchSpace, TestFunctionProblem>
        {
            public IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<RealVector> candidates,
                                                           IRandomNumberGenerator random,
                                                           RealVectorSearchSpace searchSpace,
                                                           TestFunctionProblem problem)
            {
                owner.EvaluateCalls++;
                return Enumerable.Range(0, candidates.Count)
                                 .Select(_ => new ObjectiveVector(0.0))
                                 .ToArray();
            }
        }
    }

    private sealed class InstancingInterceptor : IInterceptor<RealVector, RealVectorSearchSpace, TestFunctionProblem, SingleSolutionState<RealVector>>
    {
        public int ExecutionInstancesCreated { get; private set; }
        public int TransformCalls { get; private set; }

        public IInterceptorInstance<RealVector, RealVectorSearchSpace, TestFunctionProblem, SingleSolutionState<RealVector>> CreateExecutionInstance(
            ExecutionInstanceRegistry instanceRegistry)
        {
            ExecutionInstancesCreated++;
            return new Instance(this);
        }

        private sealed class Instance(InstancingInterceptor owner)
            : IInterceptorInstance<RealVector, RealVectorSearchSpace, TestFunctionProblem, SingleSolutionState<RealVector>>
        {
            public SingleSolutionState<RealVector> Transform(SingleSolutionState<RealVector> currentState, SingleSolutionState<RealVector>? previousState,
                RealVectorSearchSpace searchSpace, TestFunctionProblem problem)
            {
                owner.TransformCalls++;
                return currentState;
            }
        }
    }
}
