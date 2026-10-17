using HEAL.HeuristicLib.Encodings.RealVectors;
using HEAL.HeuristicLib.Inspection;
using HEAL.HeuristicLib.Operators.Mutators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Problems.TestFunctions;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.Tests.ExecutionInfrastructure.OperatorFoundation;

namespace HEAL.HeuristicLib.Tests.Operators;

public sealed class RetryOperatorTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    public void Retry_UsesTheSameArgumentsAndRandomAndCountsSuccessfulCalls(int failures)
    {
        var attempts = 0;
        var random = RandomNumberGenerator.Create(7);
        var consumed = new List<int>();
        IReadOnlyList<int> parents = [4, 5];
        var leaf = new CallbackMutator((input, rng) =>
        {
            input.ShouldBeSameAs(parents);
            rng.ShouldBeSameAs(random);
            consumed.Add(rng.NextInt());
            if (attempts++ < failures)
                throw new RetryableOperationException("try again");
            return input;
        });
        var retry = leaf.Retry(3).Named("retry");
        var innerCalls = new CountAccumulator();
        var outerWork = new CountAccumulator();
        var innerTime = new DurationAccumulator();
        var outerTime = new DurationAccumulator();
        var clock = new TickClock();
        var scope = ResolutionScope.Create(
            Aspect.CountCalls(leaf, innerCalls), Aspect.CountCandidates(retry, outerWork),
            Aspect.MeasureDuration(leaf, innerTime, clock), Aspect.MeasureDuration(retry, outerTime, clock));
        var executor = scope.Resolve<int, ISearchSpace<int>, IProblem<int, ISearchSpace<int>>>(retry);
        executor.Mutate(parents, random, Space.Instance, Problem()).ShouldBeSameAs(parents);
        attempts.ShouldBe(failures + 1);
        innerCalls.CurrentCount.ShouldBe(1);
        outerWork.CurrentCount.ShouldBe(2);
        innerTime.CurrentDuration.ShouldBe(TimeSpan.FromMilliseconds(failures + 1));
        outerTime.CurrentDuration.ShouldBe(TimeSpan.FromMilliseconds(2 * (failures + 1) + 1));
        var expectedRandom = RandomNumberGenerator.Create(7);
        consumed.ShouldBe(Enumerable.Range(0, attempts).Select(_ => expectedRandom.NextInt()));
    }

    [Theory]
    [InlineData("retryable", 3)]
    [InlineData("ordinary", 1)]
    [InlineData("aggregate", 1)]
    [InlineData("canceled", 1)]
    public void ExhaustionAndOtherFailures_PreserveTheException(string kind, int expectedAttempts)
    {
        Exception failure = kind switch
        {
            "retryable" => new RetryableOperationException("exhausted"),
            "canceled" => new OperationCanceledException(),
            "aggregate" => new AggregateException(new RetryableOperationException("nested")),
            _ => new InvalidOperationException("stop")
        };
        var attempts = 0;
        var leaf = new CallbackMutator((_, _) => { attempts++; throw failure; });
        var calls = new CountAccumulator();
        var retry = leaf.Retry(3);
        var executor = ResolutionScope.Create(Aspect.CountCalls(retry, calls))
            .Resolve<int, ISearchSpace<int>, IProblem<int, ISearchSpace<int>>>(retry);
        Record.Exception(() => executor.Mutate([1], RandomNumberGenerator.Create(1), Space.Instance, Problem()))
            .ShouldBeSameAs(failure);
        attempts.ShouldBe(expectedAttempts);
        calls.CurrentCount.ShouldBe(0);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void InvalidAttemptLimit_FailsBeforePreparingTheChild(int attempts)
    {
        var preparations = 0;
        var source = new PreparationProbe(() => preparations++);
        Should.Throw<InvalidOperationException>(() => ResolutionScope.Create()
            .Resolve<int, ISearchSpace<int>, IProblem<int, ISearchSpace<int>>>(source.Retry(attempts)));
        preparations.ShouldBe(0);
    }

    [Fact]
    public void ExternalScalarDescriptor_SupportsTheSameRetryWrapper()
    {
        var attempts = 0;
        var source = new ScalarSource(() => ++attempts < 3 ? throw new RetryableOperationException("again") : 17);
        var executor = ResolutionScope.Create().For<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>()
            .Resolve(source.Retry(3).Named("scalar"));
        executor.Read().ShouldBe(17);
        attempts.ShouldBe(3);
    }

    [Fact]
    public void AConsumerDefinedPolicy_CanRepeatBothMutationAndExternalScalarCalls()
    {
        var mutations = 0;
        var leaf = new CallbackMutator((_, _) => [++mutations]);
        var twice = new Twice<Mutation, int>(leaf);
        var childCalls = new CountAccumulator();
        var outerCalls = new CountAccumulator();
        var scope = ResolutionScope.Create(Aspect.CountCalls(leaf, childCalls), Aspect.CountCalls(twice, outerCalls));
        scope.Resolve<int, ISearchSpace<int>, IProblem<int, ISearchSpace<int>>>(twice)
            .Mutate([0], RandomNumberGenerator.Create(1), Space.Instance, Problem()).ShouldBe([2]);
        childCalls.CurrentCount.ShouldBe(2);
        outerCalls.CurrentCount.ShouldBe(1);

        var reads = 0;
        var scalar = new Twice<Checksum, RealVector>(new ScalarSource(() => ++reads));
        ResolutionScope.Create().For<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>()
            .Resolve(scalar).Read().ShouldBe(2);
    }

    [Fact]
    public void SuccessfulRetryWithCountingAndDuration_AddsNoWarmedInvocationAllocations()
    {
        var leaf = new CallbackMutator(static (parents, _) => parents);
        var source = leaf.Retry(3).Named("inspected");
        var counter = new CountAccumulator();
        var duration = new DurationAccumulator();
        var executor = ResolutionScope.Create(
            Aspect.CountCalls(leaf, counter), Aspect.MeasureDuration(source, duration))
            .Resolve<int, ISearchSpace<int>, IProblem<int, ISearchSpace<int>>>(source);
        var random = RandomNumberGenerator.Create(1);
        var problem = Problem();
        IReadOnlyList<int> input = [1];
        IReadOnlyList<int>? result = null;
        for (var i = 0; i < 20_000; i++)
            result = executor.Mutate(input, random, Space.Instance, problem);
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 10_000; i++)
            result = executor.Mutate(input, random, Space.Instance, problem);
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        allocated.ShouldBe(0);
        result.ShouldBeSameAs(input);
        counter.CurrentCount.ShouldBe(30_000);
    }

    private sealed record Twice<TOperation, TCandidate>(IOperator<TOperation, TCandidate> Child)
        : WrappingOperator<TOperation, TCandidate>(Child) where TOperation : IOperationDescriptor<TOperation>
    {
        protected override OperatorExecutorFactory CreateOperatorFactory() => new Factory(Child);

        private sealed class Factory(IOperator<TOperation, TCandidate> child) : OperatorExecutorFactory<TOperation, TCandidate>
        {
            public override ExecutorFactory GetExecutorFactory<TSearchSpace, TProblem, TSearchState>(
                OperationExecutorAdapter<TOperation, TCandidate, TSearchSpace, TProblem, TSearchState> adapter) =>
                adapter.Wrap(child, adapter.PolicyWrapper(new TwicePolicy()));
        }
    }

    private sealed class TwicePolicy : SynchronousPolicy
    {
        public override OperationCallResult<TResult> Execute<TExecutor, TArguments, TResult>(
            ImmutableArray<TExecutor> children, TArguments arguments, IRandomNumberGenerator? random,
            Func<TExecutor, TArguments, OperationCallResult<TResult>> next)
        {
            _ = next(children[0], arguments);
            return next(children[0], arguments);
        }
    }

    private sealed record CallbackMutator(Func<IReadOnlyList<int>, IRandomNumberGenerator, IReadOnlyList<int>> Callback)
        : StatelessMutator<int>
    {
        public override IReadOnlyList<int> Mutate(IReadOnlyList<int> parents, IRandomNumberGenerator random) => Callback(parents, random);
    }

    private sealed record PreparationProbe(Action Prepare) : Mutator<int>
    {
        protected override ExecutorFactory<IMutationExecutor<int, ISearchSpace<int>, IProblem<int, ISearchSpace<int>>>> CreateFactory()
        {
            Prepare();
            return _ => throw new InvalidOperationException("must not construct");
        }
    }

    private sealed record ScalarSource(Func<int> Read) : IOperator<Checksum, RealVector>
    {
        public OperatorExecutorFactory CreateExecutorFactory() => OperatorExecutorFactory.FromFactory<IChecksumExecutor>(_ => new Executor(Read));
        private sealed class Executor(Func<int> read) : IChecksumExecutor
        {
            public int Read() => read();
        }
    }

    private sealed class TickClock : TimeProvider
    {
        private long timestamp;
        public override long TimestampFrequency => 1000;
        public override long GetTimestamp() => timestamp++;
    }

    private sealed class Space : ISearchSpace<int>
    {
        public static readonly Space Instance = new();
        public bool Contains(int candidate) => true;
    }

    private static IProblem<int, ISearchSpace<int>> Problem() =>
        FuncProblem.Create(static (int value) => value, (ISearchSpace<int>)Space.Instance, SingleObjective.Minimize);
}
