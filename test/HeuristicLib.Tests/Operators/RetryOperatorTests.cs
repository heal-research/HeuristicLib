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
        var clock = new TickClock();
        var innerTime = new Duration(clock);
        var outerTime = new Duration(clock);
        var retry = leaf.Timed(innerTime).Retry(3).Named("retry");
        Counter innerCalls = new(), outerWork = new();
        var scope = ResolutionScope.Create();
        var executor = scope.Resolve<int, ISearchSpace<int>, IProblem<int, ISearchSpace<int>>>(retry.Timed(outerTime).Substitute(
            Substitution.Counting(leaf, innerCalls), Substitution.Counting(retry, outerWork, OperatorCountMetric.Candidates)), Problem());
        executor.Mutate(parents, random).ShouldBeSameAs(parents);
        attempts.ShouldBe(failures + 1);
        scope.Results.Result(innerCalls).CurrentCount.ShouldBe(1);
        scope.Results.Result(outerWork).CurrentCount.ShouldBe(2);
        scope.Results.Result(innerTime).CurrentDuration.ShouldBe(TimeSpan.FromMilliseconds(failures + 1));
        scope.Results.Result(outerTime).CurrentDuration.ShouldBe(TimeSpan.FromMilliseconds(2 * (failures + 1) + 1));
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
        var calls = new Counter();
        var retry = leaf.Retry(3);
        var scope = ResolutionScope.Create();
        var executor = scope
            .Resolve<int, ISearchSpace<int>, IProblem<int, ISearchSpace<int>>>(retry.Counted(calls), Problem());
        Record.Exception(() => executor.Mutate([1], RandomNumberGenerator.Create(1)))
            .ShouldBeSameAs(failure);
        attempts.ShouldBe(expectedAttempts);
        scope.Results.Result(calls).CurrentCount.ShouldBe(0);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void InvalidAttemptLimit_FailsBeforeResolvingTheChild(int attempts)
    {
        var preparations = 0;
        var source = new PreparationProbe(() => preparations++);
        Should.Throw<InvalidOperationException>(() => ResolutionScope.Create()
            .Resolve<int, ISearchSpace<int>, IProblem<int, ISearchSpace<int>>>(source.Retry(attempts), Problem()));
        preparations.ShouldBe(0);
    }

    [Fact]
    public void ExternalScalarOperation_SupportsTheSameRetryWrapper()
    {
        var attempts = 0;
        var source = new ScalarSource(() => ++attempts < 3 ? throw new RetryableOperationException("again") : 17);
        var problem = new TestFunctionProblem(new SphereFunction(1));
        var executor = ResolutionScope.Create().For<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>(problem)
            .Resolve(source.Retry(3).Named("scalar"));
        executor.Read(RandomNumberGenerator.Create(1)).ShouldBe(17);
        attempts.ShouldBe(3);
    }

    [Fact]
    public void NestedRetries_RespectBothAttemptLimitsAndCountingBoundaries()
    {
        var attempts = 0;
        var leaf = new CallbackMutator((_, _) => ++attempts < 4 ? throw new RetryableOperationException("again") : [attempts]);
        var inner = leaf.Retry(2);
        var outer = inner.Retry(2);
        Counter childCalls = new(), innerCalls = new(), outerCalls = new();
        var counted = outer.Substitute(Substitution.Counting(leaf, childCalls), Substitution.Counting(inner, innerCalls)).Counted(outerCalls);
        var scope = ResolutionScope.Create();
        scope.Resolve<int, ISearchSpace<int>, IProblem<int, ISearchSpace<int>>>(counted, Problem())
            .Mutate([0], RandomNumberGenerator.Create(1)).ShouldBe([4]);
        attempts.ShouldBe(4);
        scope.Results.Result(childCalls).CurrentCount.ShouldBe(1);
        scope.Results.Result(innerCalls).CurrentCount.ShouldBe(1);
        scope.Results.Result(outerCalls).CurrentCount.ShouldBe(1);
    }

    [Fact]
    public void Retry_ReturnsTheChildResultWithoutReadingItsCount()
    {
        var result = new UncountableList();
        var attempts = 0;
        var source = new CallbackMutator((_, _) => { attempts++; return result; }).Retry(3);
        var executor = ResolutionScope.Create().Resolve<int, ISearchSpace<int>, IProblem<int, ISearchSpace<int>>>(source, Problem());

        executor.Mutate([1], RandomNumberGenerator.Create(1)).ShouldBeSameAs(result);
        attempts.ShouldBe(1);
    }

    [Fact]
    public void CallCountingAndDuration_DoNotReadTheCandidateCount()
    {
        var result = new UncountableList();
        var source = new CallbackMutator((_, _) => result);
        var calls = new Counter();
        var scope = ResolutionScope.Create();
        var executor = scope
            .Resolve<int, ISearchSpace<int>, IProblem<int, ISearchSpace<int>>>(source.Counted(calls).Timed(new Duration()), Problem());

        executor.Mutate([1], RandomNumberGenerator.Create(1)).ShouldBeSameAs(result);
        scope.Results.Result(calls).CurrentCount.ShouldBe(1);
    }

    [Fact]
    public void SuccessfulRetryWithCountingAndDuration_AddsNoWarmedInvocationAllocations()
    {
        var leaf = new CallbackMutator(static (parents, _) => parents);
        var source = leaf.Retry(3).Named("inspected");
        var scope = ResolutionScope.Create();
        var problem = Problem();
        var executor = scope
            .Resolve<int, ISearchSpace<int>, IProblem<int, ISearchSpace<int>>>(source.Timed(new Duration()).Counting(leaf, out var counted), problem);
        var counter = scope.Results.Result(counted);
        var random = RandomNumberGenerator.Create(1);
        IReadOnlyList<int> input = [1];
        IReadOnlyList<int>? result = null;
        for (var i = 0; i < 20_000; i++)
            result = executor.Mutate(input, random);
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 10_000; i++)
            result = executor.Mutate(input, random);
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        allocated.ShouldBe(0);
        result.ShouldBeSameAs(input);
        counter.CurrentCount.ShouldBe(30_000);
    }

    private sealed class UncountableList : IReadOnlyList<int>
    {
        public int Count => throw new InvalidOperationException("Retry must not inspect the result.");
        public int this[int index] => throw new InvalidOperationException("Retry must not inspect the result.");
        public IEnumerator<int> GetEnumerator() => throw new InvalidOperationException("Retry must not inspect the result.");
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
    }

    private sealed record CallbackMutator(Func<IReadOnlyList<int>, IRandomNumberGenerator, IReadOnlyList<int>> Callback)
        : StatelessMutator<int>
    {
        public override IReadOnlyList<int> Mutate(IReadOnlyList<int> parents, IRandomNumberGenerator random) => Callback(parents, random);
    }

    private sealed record PreparationProbe(Action Prepare) : Mutator<int>
    {
        protected override IMutationExecutor<int> CreateExecutor<TRunSearchState>(ResolutionScope<int, ISearchSpace<int>, IProblem<int, ISearchSpace<int>>, TRunSearchState> scope)
        {
            Prepare();
            throw new InvalidOperationException("must not construct");
        }
    }

    private sealed record ScalarSource(Func<int> Read) : IOperator<Checksum, RealVector>
    {
        IExecutor IOperator<RealVector>.CreateExecutor<TSearchSpace, TProblem, TSearchState>(
            ResolutionScope<RealVector, TSearchSpace, TProblem, TSearchState> scope) => new Executor(Read);
        private sealed class Executor(Func<int> read) : IChecksumExecutor
        {
            public int Read(IRandomNumberGenerator random) => read();
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
