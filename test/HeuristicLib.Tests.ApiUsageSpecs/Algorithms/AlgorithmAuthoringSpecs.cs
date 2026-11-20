using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Encodings.RealVectors;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Operators.Creators;
using HEAL.HeuristicLib.Operators.Evaluators;
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

        var finalState = await algorithm.RunToCompletionAsync(
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

        var firstRunState = await algorithm.RunToCompletionAsync(
            problem,
            RandomNumberGenerator.Create(123),
            ct: TestContext.Current.CancellationToken);

        var secondRunState = await algorithm.RunToCompletionAsync(
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
            algorithm.RunToCompletionAsync(problem, RandomNumberGenerator.Create(123), ct: TestContext.Current.CancellationToken),
            algorithm.RunToCompletionAsync(problem, RandomNumberGenerator.Create(456), ct: TestContext.Current.CancellationToken));

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

        var finalState = await algorithm.RunToCompletionAsync(
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

        _ = registry.Resolve<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem, SingleSolutionState<RealVector>>(algorithm, new TestFunctionProblem(new SphereFunction(3)));

        creator.ExecutionsCreated.ShouldBe(1);
        evaluator.ExecutionsCreated.ShouldBe(1);
        interceptor.ExecutionsCreated.ShouldBe(1);
        creator.CreateCalls.ShouldBe(0);
        evaluator.EvaluateCalls.ShouldBe(0);
        interceptor.TransformCalls.ShouldBe(0);
    }

    /// <summary>
    /// An algorithm builds its executor in the protected <c>CreateExecutor</c> hook, resolving the interceptor and other
    /// children through the scope it receives. Callers resolve the algorithm through a scope and receive the hook's
    /// executor itself.
    /// </summary>
    [Fact]
    public void Algorithm_ResolvesThroughTheScopeToTheExecutorItsHookBuilds()
    {
        var algorithm = new SingleCreateAlgorithm { Creator = new CountingCreator() };

        var executor = ResolutionScope.Create()
            .Resolve<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem, SingleSolutionState<RealVector>>(algorithm, new TestFunctionProblem(new SphereFunction(3)));

        executor.ShouldBeAssignableTo<IterativeAlgorithmExecutor<SingleSolutionState<RealVector>>>();
    }

    private sealed record SingleCreateAlgorithm
        : IterativeAlgorithm<SingleCreateAlgorithm, RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem, SingleSolutionState<RealVector>>
    {
        public required IOperator<Creation, RealVector> Creator { get; init; }
        public IOperator<Evaluation, RealVector> Evaluator { get; init; } = new ProblemEvaluator<RealVector>();

        public static Parameter<SingleCreateAlgorithm, IOperator<Creation, RealVector>> CreatorParameter { get; } =
            new(nameof(Creator), static algorithm => algorithm.Creator, static (algorithm, value) => algorithm with { Creator = value });

        public static Parameter<SingleCreateAlgorithm, IOperator<Evaluation, RealVector>> EvaluatorParameter { get; } =
            new(nameof(Evaluator), static algorithm => algorithm.Evaluator, static (algorithm, value) => algorithm with { Evaluator = value });

        public new static IReadOnlyList<IParameter> Parameters { get; } = [CreatorParameter, EvaluatorParameter];

        public override IReadOnlyList<IParameter> GetParameters() => [.. base.GetParameters(), .. Parameters];

        protected override IterativeAlgorithmExecutor<SingleSolutionState<RealVector>> CreateExecutor(
            ResolutionScope<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem, SingleSolutionState<RealVector>> scope)
        {
            var interceptor = scope.ResolveOptional(Interceptor);
            var creator = scope.Resolve(Creator);
            var evaluator = scope.Resolve(Evaluator);
            return new Executor(interceptor, creator, evaluator);
        }

        private sealed class Executor(
            IInterceptionExecutor<SingleSolutionState<RealVector>>? interceptor,
            ICreationExecutor<RealVector> creator,
            IEvaluationExecutor<RealVector> evaluator)
            : IterativeAlgorithmExecutor<SingleSolutionState<RealVector>>(interceptor)
        {
            protected override SingleSolutionState<RealVector> ExecuteStep(SingleSolutionState<RealVector>? previousState, IRandomNumberGenerator random)
            {
                var candidate = creator.Create(1, random)[0];
                var objectiveVector = evaluator.Evaluate([candidate], random)[0];

                return SingleSolutionState.From(candidate.ToEvaluated(objectiveVector));
            }
        }
    }

    private sealed record DoubleCreateAlgorithm
        : IterativeAlgorithm<DoubleCreateAlgorithm, RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem, SingleSolutionState<RealVector>>
    {
        public required IOperator<Creation, RealVector> Creator { get; init; }
        public IOperator<Evaluation, RealVector> Evaluator { get; init; } = new ProblemEvaluator<RealVector>();

        public static Parameter<DoubleCreateAlgorithm, IOperator<Creation, RealVector>> CreatorParameter { get; } =
            new(nameof(Creator), static algorithm => algorithm.Creator, static (algorithm, value) => algorithm with { Creator = value });

        public static Parameter<DoubleCreateAlgorithm, IOperator<Evaluation, RealVector>> EvaluatorParameter { get; } =
            new(nameof(Evaluator), static algorithm => algorithm.Evaluator, static (algorithm, value) => algorithm with { Evaluator = value });

        public new static IReadOnlyList<IParameter> Parameters { get; } = [CreatorParameter, EvaluatorParameter];

        public override IReadOnlyList<IParameter> GetParameters() => [.. base.GetParameters(), .. Parameters];

        protected override IterativeAlgorithmExecutor<SingleSolutionState<RealVector>> CreateExecutor(
            ResolutionScope<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem, SingleSolutionState<RealVector>> scope)
        {
            var interceptor = scope.ResolveOptional(Interceptor);
            var creator = scope.Resolve(Creator);
            var evaluator = scope.Resolve(Evaluator);
            return new Executor(interceptor, creator, evaluator);
        }

        private sealed class Executor(
            IInterceptionExecutor<SingleSolutionState<RealVector>>? interceptor,
            ICreationExecutor<RealVector> creator,
            IEvaluationExecutor<RealVector> evaluator)
            : IterativeAlgorithmExecutor<SingleSolutionState<RealVector>>(interceptor)
        {
            private int steps;

            protected override SingleSolutionState<RealVector> ExecuteStep(SingleSolutionState<RealVector>? previousState, IRandomNumberGenerator random)
            {
                steps++;

                var first = creator.Create(1, random)[0];
                var second = creator.Create(1, random)[0];
                RealVector candidate = [first[0], second[0], steps];
                var objectiveVector = evaluator.Evaluate([candidate], random)[0];

                return SingleSolutionState.From(candidate.ToEvaluated(objectiveVector));
            }
        }
    }

    private sealed record CountingCreator
        : Creator<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>
    {

        protected override ICreationExecutor<RealVector> CreateExecutor<TRunSearchState>(ResolutionScope<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem, TRunSearchState> scope) =>
        new Executor();

        private sealed class Executor
            : CreationExecutor<RealVector>
        {
            private int invocations;

            public override IReadOnlyList<RealVector> Create(int count, IRandomNumberGenerator random)
            {
                return Enumerable.Range(0, count)
                                 .Select(_ =>
                                 {
                                     invocations++;
                                     return new RealVector(invocations, 0.0, 0.0);
                                 })
                                 .ToArray();
            }
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

    private sealed record InstancingCreator : Creator<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>
    {
        public int ExecutionsCreated { get; private set; }
        public int CreateCalls { get; private set; }

        protected override ICreationExecutor<RealVector> CreateExecutor<TSearchState>(
            ResolutionScope<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem, TSearchState> scope)
        {
            ExecutionsCreated++;
            return new Executor(this, scope.Problem);
        }

        private sealed class Executor(InstancingCreator owner, TestFunctionProblem problem)
            : CreationExecutor<RealVector>
        {
            public override IReadOnlyList<RealVector> Create(int count, IRandomNumberGenerator random)
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

        IExecutor IOperator<RealVector>.CreateExecutor<TSearchSpace, TProblem, TSearchState>(
            ResolutionScope<RealVector, TSearchSpace, TProblem, TSearchState> scope)
        {
            ExecutionsCreated++;
            return new Executor(this);
        }

        private sealed class Executor(InstancingEvaluator owner)
            : EvaluationExecutor<RealVector>
        {
            public override IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<RealVector> candidates, IRandomNumberGenerator random)
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

        IExecutor IOperator<RealVector>.CreateExecutor<TSearchSpace, TProblem, TSearchState>(
            ResolutionScope<RealVector, TSearchSpace, TProblem, TSearchState> scope)
        {
            ExecutionsCreated++;
            return new Executor(this);
        }

        private sealed class Executor(InstancingInterceptor owner)
            : InterceptionExecutor<SingleSolutionState<RealVector>>
        {
            public override SingleSolutionState<RealVector> Transform(SingleSolutionState<RealVector> currentState, SingleSolutionState<RealVector>? previousState, IRandomNumberGenerator random)
            {
                owner.TransformCalls++;
                return currentState;
            }
        }
    }
}
