using HEAL.HeuristicLib.Algorithms;
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
    public void PrivateFieldsAndConsumerOperationsShareOnlyAnExplicitResource()
    {
        var source = new CountingSource();
        var outer = new CountAccumulator();
        var inner = new CountAccumulator();
        var scope = ResolutionScope.Create(Aspect.CountInvocations((IOperator<ReadValue, RealVector>)source, outer))
            .For<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>();
        var problem = new TestFunctionProblem(new SphereFunction(1));
        var random = RandomNumberGenerator.Create(1);
        var first = Read(scope, source);
        var second = Read(scope, source);
        first.Read(random, problem.SearchSpace, problem).ShouldBe(1);
        first.Read(random, problem.SearchSpace, problem).ShouldBe(2);
        second.Read(random, problem.SearchSpace, problem).ShouldBe(1);

        var handle = new TallyHandle();
        var shared = source with { Tally = handle };
        var read = Read(scope, shared);
        var mutation = scope.Resolve((IOperator<Mutation, RealVector>)shared);
        var advised = Read(scope.With(Aspect.CountInvocations((IOperator<ReadValue, RealVector>)source, inner)), source);
        read.Read(random, problem.SearchSpace, problem).ShouldBe(1);
        mutation.Mutate([new(0.0)], random, problem.SearchSpace, problem)[0][0].ShouldBe(2);
        advised.Read(random, problem.SearchSpace, problem).ShouldBe(1);
        outer.CurrentCount.ShouldBe(4);
        inner.CurrentCount.ShouldBe(1);
        var sharedCalls = new CountAccumulator();
        var sharedContext = scope.With(Aspect.CountInvocations((IOperator<ReadValue, RealVector>)shared, sharedCalls));
        Read(sharedContext, shared).Read(random, problem.SearchSpace, problem).ShouldBe(3);
        mutation.Mutate([new(0.0)], random, problem.SearchSpace, problem)[0][0].ShouldBe(4);
        sharedCalls.CurrentCount.ShouldBe(1);
        scope.Resource(handle).ShouldBeSameAs(scope.With().Resource(handle));
        scope.Resource(new TallyHandle()).ShouldNotBeSameAs(scope.Resource(handle));
        (shared with { }).ShouldBe(shared);
        (shared with { Tally = new TallyHandle() }).ShouldNotBe(shared);
    }

    [Fact]
    public void DirectScopeSupplyUsesAReusableBuilder()
    {
        var handle = new SuppliedResourceHandle<Tally>();
        var supplied = new Tally();
        var builder = new ResolutionScopeBuilder().AddResource(handle, supplied);
        var scope = builder.Build();
        scope.Resource(handle).ShouldBeSameAs(supplied);
        var another = new SuppliedResourceHandle<Tally>();
        builder.AddResource(another, new Tally());
        builder.Build().Resource(handle).ShouldBeSameAs(supplied);
        Should.Throw<InvalidOperationException>(() => scope.Resource(another)).Message.ShouldContain("explicit supply");
        Should.Throw<InvalidOperationException>(() => ResolutionScope.Create().Resource(handle)).Message.ShouldContain("explicit supply");
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
        var calls = new CountAccumulator();
        var produced = 0;
        var trialCounts = new List<CountAccumulator>();
        var run = cycle.Sharing(cached).Repeat(3).CreateRun(problem, RandomNumberGenerator.Create(7), setup =>
        {
            setup.AddAspectPerTrial(_ =>
            {
                var count = new CountAccumulator();
                trialCounts.Add(count);
                return Aspect.CountInvocations(evaluator, count);
            });
            setup.AddAspectPerTrial(_ => Aspect.CountInvocations(evaluator, calls));
            setup.AddAspectPerTrial(_ => Aspect.Observe(cycle.Iterations, _ => Interlocked.Increment(ref produced)));
        });
        var results = await run.CompleteAsync(ExecutionConcurrency.Concurrent(3), cancellationToken: TestContext.Current.CancellationToken);
        results.Length.ShouldBe(3);
        produced.ShouldBe(36);
        calls.CurrentCount.ShouldBe(3);
        trialCounts.Count.ShouldBe(3);
        trialCounts.ShouldAllBe(count => count.CurrentCount == 1);
    }

    private static IReadValueExecutor<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem> Read(
        ResolutionScope<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem> scope, CountingSource source) =>
        (IReadValueExecutor<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>)scope.ResolveOperator((IOperator<ReadValue, RealVector>)source);

    private sealed class Tally
    {
        private int count;
        public int Next() => Interlocked.Increment(ref count);
    }

    private sealed class TallyHandle : ResourceHandle<Tally>
    {
        protected override Tally CreateResource() => new();
    }

    private sealed record CountingSource : IOperator<ReadValue, RealVector>, IOperator<Mutation, RealVector>
    {
        public TallyHandle? Tally { get; init; }

        IExecutor IOperator<ReadValue, RealVector>.CreateExecutor<TSearchSpace, TProblem, TSearchState>(ResolutionScope<RealVector, TSearchSpace, TProblem, TSearchState> scope) =>
            new Executor<TSearchSpace, TProblem>(Tally is null ? null : scope.Resource(Tally));

        IExecutor IOperator<Mutation, RealVector>.CreateExecutor<TSearchSpace, TProblem, TSearchState>(ResolutionScope<RealVector, TSearchSpace, TProblem, TSearchState> scope) =>
            new Executor<TSearchSpace, TProblem>(Tally is null ? null : scope.Resource(Tally));

        private sealed class Executor<TSearchSpace, TProblem>(Tally? tally)
            : IReadValueExecutor<RealVector, TSearchSpace, TProblem>, IMutationExecutor<RealVector, TSearchSpace, TProblem>
            where TSearchSpace : class, ISearchSpace<RealVector>
            where TProblem : class, IProblem<RealVector, TSearchSpace>
        {
            private int count;
            public int Read(IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem) => tally?.Next() ?? ++count;
            public IReadOnlyList<RealVector> Mutate(IReadOnlyList<RealVector> parents, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem) =>
                [new(tally?.Next() ?? ++count)];
        }
    }

    private sealed record RepeatedEvaluation(CachingEvaluator<RealVector> Evaluator, RealVector Candidate)
        : Algorithm<RepeatedEvaluation, RealVector, SingleSolutionState<RealVector>>
    {
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
