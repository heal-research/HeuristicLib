using HEAL.HeuristicLib.Analysis;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.Tests.ExecutionInfrastructure.OperatorFoundation;
using HEAL.HeuristicLib.Tests.TestSupport.Mocks;

namespace HEAL.HeuristicLib.Tests.Analysis;

/// <summary>
/// Counters of an operator's invocations or candidate work serve every operation, including ones defined outside the
/// library.
/// </summary>
public class OperatorCountClockTests
{
    [Fact]
    public void CountCandidates_CountsTheCandidateWorkOfAnExternalBatchOperation()
    {
        IOperator<Scoring, int> scorer = new LengthScorer();
        var problem = CreateProblem();
        var candidates = new Counter();
        var invocations = new Counter();
        var scope = ResolutionScope.Create();
        var executor = scope.For<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>(problem).Resolve(scorer.Substitute(scorer.Invocations.CountingCandidates(candidates), scorer.Invocations.Counting(invocations)));

        executor.Score([1, 2, 3], RandomNumberGenerator.Create(1));
        executor.Score([4, 5], RandomNumberGenerator.Create(1));

        scope.Results.Result(candidates).CurrentCount.ShouldBe(5L);
        scope.Results.Result(invocations).CurrentCount.ShouldBe(2L);
    }

    [Fact]
    public void CountingCandidates_RejectsAnOperationWithoutCandidateWorkWhenCreated()
    {
        var checksum = new ConstructionRecordingChecksum();

        var exception = Should.Throw<InvalidOperationException>(() => checksum.Invocations.CountingCandidates(new Counter()));

        exception.Message.ShouldBe("Counting candidates cannot measure candidate work for ConstructionRecordingChecksum: Checksum reports none.");
    }

    // Counting candidates, placed explicitly on an operation without candidate work, fails when the wrapper is built and
    // before its child is.
    [Fact]
    public void CountedCandidates_RejectsAnOperationWithoutCandidateWorkBeforeBuildingIt()
    {
        var checksum = new ConstructionRecordingChecksum();
        var scope = ResolutionScope.Create().For<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>(CreateProblem());

        var exception = Should.Throw<InvalidOperationException>(() => scope.Resolve(checksum.Counted(new Counter(), OperatorCountMetric.Candidates)));

        exception.Message.ShouldContain("reports none");
        checksum.Builds.ShouldBe(0);
    }

    [Fact]
    public void Count_CountsAnOperationWithoutCandidateWork()
    {
        var checksum = new ConstructionRecordingChecksum();
        var problem = CreateProblem();
        var invocations = new Counter();
        var scope = ResolutionScope.Create();
        IOperator<Checksum, int> receiver = checksum;
        var executor = scope.For<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>(problem).Resolve(receiver.Substitute(checksum.Invocations.Counting(invocations)));

        executor.Read(RandomNumberGenerator.Create(1));
        executor.Read(RandomNumberGenerator.Create(1));

        scope.Results.Result(invocations).CurrentCount.ShouldBe(2L);
    }

    private static FuncProblem<int, DummySearchSpace<int>> CreateProblem() =>
        FuncProblem.Create(static (int candidate) => candidate, DummySearchSpace<int>.Instance, SingleObjective.Minimize);

    private sealed class ConstructionRecordingChecksum : IOperator<Checksum, int>
    {
        public int Builds { get; private set; }

        IExecutor IOperator<int>.CreateExecutor<TSearchSpace, TProblem, TSearchState>(
            ResolutionScope<int, TSearchSpace, TProblem, TSearchState> scope)
        {
            Builds++;
            return new Executor();
        }

        private sealed class Executor : IChecksumExecutor
        {
            public int Read(IRandomNumberGenerator random) => 1;
        }
    }

    private sealed record LengthScorer : IOperator<Scoring, int>
    {
        IExecutor IOperator<int>.CreateExecutor<TSearchSpace, TProblem, TSearchState>(
            ResolutionScope<int, TSearchSpace, TProblem, TSearchState> scope) => new Executor();

        private sealed class Executor : IScoringExecutor<int>
        {
            public IReadOnlyList<double> Score(IReadOnlyList<int> candidates, IRandomNumberGenerator random) => [.. candidates.Select(candidate => (double)candidate)];
        }
    }
}

public interface IScoringExecutor<TCandidate> : IExecutor
{
    IReadOnlyList<double> Score(IReadOnlyList<TCandidate> candidates, IRandomNumberGenerator random);
}

// A batch operation defined outside the library that reports one unit of candidate work per scored candidate.
public sealed class Scoring : IOperation<Scoring>
{
    static bool IOperation<Scoring>.IsExecutor<TCandidate, TSearchState>(IExecutor executor) =>
        executor is IScoringExecutor<TCandidate>;

    static IExecutor IOperation<Scoring>.Wrap<TCandidate, TSearchState, TWrapper>(IExecutor executor, TWrapper wrapper) =>
        new WrappedExecutor<TCandidate, TWrapper>(executor, wrapper);

    static bool IOperation<Scoring>.SupportsCandidateWork => true;

    private readonly struct Invocation<TCandidate>(IScoringExecutor<TCandidate> executor,
        IReadOnlyList<TCandidate> candidates, IRandomNumberGenerator random) : IInvocation<IReadOnlyList<double>>
    {
        private readonly IScoringExecutor<TCandidate> executor = executor;
        private readonly IRandomNumberGenerator random = random;

        public IRandomNumberGenerator Random => random;

        public IReadOnlyList<double> Proceed(IExecutor? executor = null, IRandomNumberGenerator? random = null) =>
            ((IScoringExecutor<TCandidate>?)executor ?? this.executor).Score(candidates, random ?? this.random);

        public int CountCandidateWork(IReadOnlyList<double> result) => result.Count;
    }

    private sealed class WrappedExecutor<TCandidate, TWrapper>(IExecutor executor, TWrapper wrapper)
        : WrappingExecutor<IScoringExecutor<TCandidate>, TWrapper>(executor, wrapper), IScoringExecutor<TCandidate>
        where TWrapper : struct, IInvocationWrapper
    {
        public IReadOnlyList<double> Score(IReadOnlyList<TCandidate> candidates, IRandomNumberGenerator random) =>
            Wrapper.Invoke<Invocation<TCandidate>, IReadOnlyList<double>>(new(Executor, candidates, random));
    }
}

public static class ScoringResolutionExtensions
{
    extension<TCandidate, TSearchSpace, TProblem>(ResolutionScope<TCandidate, TSearchSpace, TProblem> scope)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        public IScoringExecutor<TCandidate> Resolve(IOperator<Scoring, TCandidate> source) =>
            (IScoringExecutor<TCandidate>)scope.ResolveOperator(source);
    }
}

// The external operation's source, which is what its counters come from, declared like a built-in operation's.
public sealed record ScoringObservation<TCandidate>(ValueArray<TCandidate> Candidates, ValueArray<double> Scores)
    : IInvocationObservation<Scoring, TCandidate, ScoringObservation<TCandidate>>
{
    static IExecutor IInvocationObservation<Scoring, TCandidate, ScoringObservation<TCandidate>>.Wrap<TSearchState>(
        IExecutor executor, IOperator<Scoring, TCandidate> source, Action<ScoringObservation<TCandidate>> record,
        IProblem<TCandidate, ISearchSpace<TCandidate>> problem) =>
        new Executor((IScoringExecutor<TCandidate>)executor, record);

    private sealed class Executor(IScoringExecutor<TCandidate> executor,
        Action<ScoringObservation<TCandidate>> observe) : IScoringExecutor<TCandidate>
    {
        public IReadOnlyList<double> Score(IReadOnlyList<TCandidate> candidates, IRandomNumberGenerator random)
        {
            var scores = executor.Score(candidates, random);
            observe(new ScoringObservation<TCandidate>(candidates.ToValueArray(), scores.ToValueArray()));
            return scores;
        }
    }
}

public static class ScoringObservations
{
    extension<TCandidate>(IOperator<Scoring, TCandidate> scorer)
    {
        public InvocationSource<Scoring, TCandidate, ScoringObservation<TCandidate>> Invocations =>
            ObservationSource.Create<Scoring, TCandidate, ScoringObservation<TCandidate>>(scorer);
    }
}
