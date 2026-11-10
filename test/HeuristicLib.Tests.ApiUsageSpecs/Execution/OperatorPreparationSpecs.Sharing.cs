using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Analysis;
using HEAL.HeuristicLib.Encodings.RealVectors;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Experiments;
using HEAL.HeuristicLib.Inspection;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Operators.Evaluators;
using HEAL.HeuristicLib.Operators.Mutators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Problems.TestFunctions;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Tests.ApiUsageSpecs.Execution;

public sealed partial class OperatorPreparationSpecs
{
    [Fact]
    public void BothOperationsOfOneConfigurationShareItsExecutorWithinAScope()
    {
        var source = new CountingSource();
        var reads = ReadSource(source).Count();
        var counted = ((IOperator<ReadValue, RealVector>)source).AnalyzedBy(reads);
        var root = ResolutionScope.Create();
        var scope = root.For<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>();
        var problem = new TestFunctionProblem(new SphereFunction(1));
        var random = RandomNumberGenerator.Create(1);

        var read = Read(scope, counted);
        var mutation = scope.Resolve((IOperator<Mutation, RealVector>)source);
        read.Read(random, problem.SearchSpace, problem).ShouldBe(1);
        mutation.Mutate([new(0.0)], random, problem.SearchSpace, problem)[0][0].ShouldBe(2);
        Read(scope, counted).Read(random, problem.SearchSpace, problem).ShouldBe(3);

        var child = scope.CreateChildScope();
        child.Resolve((IOperator<Mutation, RealVector>)source).Mutate([new(0.0)], random, problem.SearchSpace, problem)[0][0].ShouldBe(1);
        Read(child, counted).Read(random, problem.SearchSpace, problem).ShouldBe(2);
        root.Results.Result(reads).CurrentCount.ShouldBe(3);
    }

    [Fact]
    public void ExplicitSharingKeepsBothOperationsStateAcrossChildScopes()
    {
        var source = new CountingSource();
        var root = ResolutionScope.Create();
        root.Share(source);
        var scope = root.For<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>();
        var first = scope.CreateChildScope();
        var second = scope.CreateChildScope();
        var problem = new TestFunctionProblem(new SphereFunction(1));
        var random = RandomNumberGenerator.Create(1);

        Read(first, source).Read(random, problem.SearchSpace, problem).ShouldBe(1);
        second.Resolve((IOperator<Mutation, RealVector>)source).Mutate([new(0.0)], random, problem.SearchSpace, problem)[0][0].ShouldBe(2);
        Read(second, source).Read(random, problem.SearchSpace, problem).ShouldBe(3);
        Read(ResolutionScope.Create().For<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>(), source)
            .Read(random, problem.SearchSpace, problem).ShouldBe(1);
    }

    [Fact]
    public async Task CycleTurnsShareACacheWhileRepeatedTrialsCreateIndependentCaches()
    {
        var problem = new TestFunctionProblem(new SphereFunction(1));
        var evaluator = new ProblemEvaluator<RealVector>();
        var cached = evaluator.Cached(sizeLimit: 100);
        var first = new RepeatedEvaluation(cached, new RealVector(2.0));
        var second = first with { };
        var cycle = CycleAlgorithm.Create(first.TerminatedAfterIterations(2), second.TerminatedAfterIterations(2)) with { MaximumCycles = 3 };
        var calls = evaluator.Invocations.Count();
        var produced = cycle.Iterations.Count();
        var run = cycle.Sharing(cached).AnalyzedBy(calls, produced).Repeat(3).CreateRun(problem, RandomNumberGenerator.Create(7));
        var results = await run.CompleteAsync(ExecutionConcurrency.Concurrent(3), cancellationToken: TestContext.Current.CancellationToken);

        results.Length.ShouldBe(3);
        run.Trials.Sum(trial => trial.Run.Result(produced).CurrentCount).ShouldBe(36);
        run.Trials.ShouldAllBe(trial => trial.Run.Result(calls).CurrentCount == 1);
        run.Trials.Select(trial => trial.Run.Result(calls)).Distinct(ReferenceEqualityComparer.Instance).Count().ShouldBe(3);
    }

    private static IReadValueExecutor<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem> Read(
        ResolutionScope<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem> scope, IOperator<ReadValue, RealVector> source) =>
        (IReadValueExecutor<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>)scope.ResolveOperator(source);

    private static InvocationSource<ReadValue, RealVector, int> ReadSource(CountingSource source) =>
        ObservationSource.Create((IOperator<ReadValue, RealVector>)source, (Action<int> observe) => new ReadObservationAdvice(observe));

    private sealed class ReadObservationAdvice(Action<int> observe) : OperatorAdvice<ReadValue, RealVector>
    {
        public override IExecutor Wrap<TSearchSpace, TProblem, TSearchState>(IExecutor executor, OperationInfo operation,
            IOperator<ReadValue, RealVector> source) => operation.Wrap(executor, new Reporter(observe));

        private readonly struct Reporter(Action<int> observe) : IInvocationWrapper
        {
            public TResult Invoke<TInvocation, TResult>(TInvocation invocation) where TInvocation : struct, IInvocation<TResult>
            {
                var result = invocation.Proceed();
                observe(1);
                return result;
            }
        }
    }

    // Performs two operations with one construction hook, implemented once as a public method.
    private sealed record CountingSource : IOperator<ReadValue, RealVector>, IOperator<Mutation, RealVector>
    {
        public IExecutor CreateExecutor<TSearchSpace, TProblem, TSearchState>(ResolutionScope<RealVector, TSearchSpace, TProblem, TSearchState> scope)
            where TSearchSpace : class, ISearchSpace<RealVector>
            where TProblem : class, IProblem<RealVector, TSearchSpace>
            where TSearchState : class, ISearchState =>
            new Executor<TSearchSpace, TProblem>();

        private sealed class Executor<TSearchSpace, TProblem>
            : IReadValueExecutor<RealVector, TSearchSpace, TProblem>, IMutationExecutor<RealVector, TSearchSpace, TProblem>
            where TSearchSpace : class, ISearchSpace<RealVector>
            where TProblem : class, IProblem<RealVector, TSearchSpace>
        {
            private int count;
            public int Read(IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem) => ++count;
            public IReadOnlyList<RealVector> Mutate(IReadOnlyList<RealVector> parents, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem) =>
                [new(++count)];
        }
    }

    private sealed record RepeatedEvaluation(CachingEvaluator<RealVector> Evaluator, RealVector Candidate)
        : Algorithm<RepeatedEvaluation, RealVector, SingleSolutionState<RealVector>>
    {
        public static Parameter<RepeatedEvaluation, CachingEvaluator<RealVector>> EvaluatorParameter { get; } =
            new(nameof(Evaluator), static algorithm => algorithm.Evaluator, static (algorithm, value) => algorithm with { Evaluator = value });

        public override IReadOnlyList<IParameter> GetParameters() => [.. base.GetParameters(), EvaluatorParameter];

        protected override IAlgorithmExecutor<RealVector, TSearchSpace, TProblem, SingleSolutionState<RealVector>> CreateExecutor<TSearchSpace, TProblem>(
            ResolutionScope<RealVector, TSearchSpace, TProblem, SingleSolutionState<RealVector>> scope)
        {
            var evaluator = scope.Resolve(Evaluator);
            return new Executor<TSearchSpace, TProblem>(evaluator, Candidate);
        }

        private sealed class Executor<TSearchSpace, TProblem>(IEvaluationExecutor<RealVector, TSearchSpace, TProblem> evaluator, RealVector candidate)
            : AlgorithmExecutor<RealVector, TSearchSpace, TProblem, SingleSolutionState<RealVector>>
            where TSearchSpace : class, ISearchSpace<RealVector>
            where TProblem : class, IProblem<RealVector, TSearchSpace>
        {
            public override async IAsyncEnumerable<SingleSolutionState<RealVector>> RunStreamingAsync(TProblem problem, IRandomNumberGenerator random,
                SingleSolutionState<RealVector>? initialState = null, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
            {
                for (var i = 0; i < 3; i++)
                    yield return SingleSolutionState.From(candidate, evaluator.Evaluate([candidate], random, problem.SearchSpace, problem)[0]);
                await Task.CompletedTask;
            }
        }
    }
}
