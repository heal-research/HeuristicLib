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
        var reads = new Counter();
        var counted = ((IOperator<ReadValue, RealVector>)source).Substitute(ReadSource(source).Counting(reads));
        var root = ResolutionScope.Create();
        var problem = new TestFunctionProblem(new SphereFunction(1));
        var scope = root.For<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>(problem);
        var random = RandomNumberGenerator.Create(1);

        var read = Read(scope, counted);
        var mutation = scope.Resolve((IOperator<Mutation, RealVector>)source);
        read.Read(random).ShouldBe(1);
        mutation.Mutate([new(0.0)], random)[0][0].ShouldBe(2);
        Read(scope, counted).Read(random).ShouldBe(3);

        var child = scope.CreateChildScope();
        child.Resolve((IOperator<Mutation, RealVector>)source).Mutate([new(0.0)], random)[0][0].ShouldBe(1);
        Read(child, counted).Read(random).ShouldBe(2);
        root.Results.Result(reads).CurrentCount.ShouldBe(3);
    }

    [Fact]
    public void ExplicitSharingKeepsBothOperationsStateAcrossChildScopes()
    {
        var source = new CountingSource();
        var root = ResolutionScope.Create();
        root.Share(source);
        var problem = new TestFunctionProblem(new SphereFunction(1));
        var scope = root.For<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>(problem);
        var first = scope.CreateChildScope();
        var second = scope.CreateChildScope();
        var random = RandomNumberGenerator.Create(1);

        Read(first, source).Read(random).ShouldBe(1);
        second.Resolve((IOperator<Mutation, RealVector>)source).Mutate([new(0.0)], random)[0][0].ShouldBe(2);
        Read(second, source).Read(random).ShouldBe(3);
        Read(ResolutionScope.Create().For<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>(problem), source)
            .Read(random).ShouldBe(1);
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
        var calls = new Counter();
        var produced = new Counter();
        var run = cycle.Sharing(cached).Substitute(evaluator.Invocations.Counting(calls), cycle.Iterations.Counting(produced)).Repeat(3).CreateRun(problem, RandomNumberGenerator.Create(7));
        var results = await run.CompleteAsync(ExecutionConcurrency.Concurrent(3), cancellationToken: TestContext.Current.CancellationToken);

        results.Length.ShouldBe(3);
        run.Trials.Sum(trial => trial.Run.Result(produced).CurrentCount).ShouldBe(36);
        run.Trials.ShouldAllBe(trial => trial.Run.Result(calls).CurrentCount == 1);
        run.Trials.Select(trial => trial.Run.Result(calls)).Distinct(ReferenceEqualityComparer.Instance).Count().ShouldBe(3);
    }

    private static IReadValueExecutor Read(
        ResolutionScope<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem> scope, IOperator<ReadValue, RealVector> source) =>
        (IReadValueExecutor)scope.ResolveOperator(source);

    private static InvocationSource<ReadValue, RealVector, ReadObservation> ReadSource(CountingSource source) =>
        ObservationSource.Create<ReadValue, RealVector, ReadObservation>(source);

    private sealed record ReadObservation : IInvocationObservation<ReadValue, RealVector, ReadObservation>
    {
        static IExecutor IInvocationObservation<ReadValue, RealVector, ReadObservation>.Wrap<TSearchState>(
            IExecutor executor, IOperator<ReadValue, RealVector> source, Action<ReadObservation> record,
            IProblem<RealVector, ISearchSpace<RealVector>> problem) =>
            OperationInfo.For<ReadValue, RealVector, TSearchState>().Wrap(executor, new Reporter(record));

        private readonly struct Reporter(Action<ReadObservation> record) : IInvocationWrapper
        {
            public TResult Invoke<TInvocation, TResult>(TInvocation invocation) where TInvocation : struct, IInvocation<TResult>
            {
                var result = invocation.Proceed();
                record(new ReadObservation());
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
            new Executor();

        private sealed class Executor
            : IReadValueExecutor, IMutationExecutor<RealVector>
        {
            private int count;
            public int Read(IRandomNumberGenerator random) => ++count;
            public IReadOnlyList<RealVector> Mutate(IReadOnlyList<RealVector> parents, IRandomNumberGenerator random) =>
                [new(++count)];
        }
    }

    private sealed record RepeatedEvaluation(CachingEvaluator<RealVector> Evaluator, RealVector Candidate)
        : Algorithm<RepeatedEvaluation, RealVector, SingleSolutionState<RealVector>>
    {
        public static Parameter<RepeatedEvaluation, CachingEvaluator<RealVector>> EvaluatorParameter { get; } =
            new(nameof(Evaluator), static algorithm => algorithm.Evaluator, static (algorithm, value) => algorithm with { Evaluator = value });

        public override IReadOnlyList<IParameter> GetParameters() => [.. base.GetParameters(), EvaluatorParameter];

        protected override IAlgorithmExecutor<SingleSolutionState<RealVector>> CreateExecutor<TSearchSpace, TProblem>(
            ResolutionScope<RealVector, TSearchSpace, TProblem, SingleSolutionState<RealVector>> scope)
        {
            var evaluator = scope.Resolve(Evaluator);
            return new Executor(evaluator, Candidate);
        }

        private sealed class Executor(IEvaluationExecutor<RealVector> evaluator, RealVector candidate)
            : AlgorithmExecutor<SingleSolutionState<RealVector>>
        {
            public override async IAsyncEnumerable<SingleSolutionState<RealVector>> RunStreamingAsync(IRandomNumberGenerator random, SingleSolutionState<RealVector>? initialState = null, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
            {
                for (var i = 0; i < 3; i++)
                    yield return SingleSolutionState.From(candidate, evaluator.Evaluate([candidate], random)[0]);
                await Task.CompletedTask;
            }
        }
    }
}
