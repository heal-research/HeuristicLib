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
        var scorer = new LengthScorer();
        var problem = CreateProblem();
        var candidates = scorer.Invocations.CountCandidates();
        var invocations = scorer.Invocations.Count();
        var scope = ResolutionScope.Create();
        scope.AddAspect(scope.Results.Aspect(candidates));
        scope.AddAspect(scope.Results.Aspect(invocations));
        var executor = scope.For<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>().Resolve(scorer);

        executor.Score([1, 2, 3], RandomNumberGenerator.Create(1), problem.SearchSpace, problem);
        executor.Score([4, 5], RandomNumberGenerator.Create(1), problem.SearchSpace, problem);

        scope.Results.Result(candidates).CurrentCount.ShouldBe(5L);
        scope.Results.Result(invocations).CurrentCount.ShouldBe(2L);
    }

    [Fact]
    public void CountCandidates_RejectsAnOperationWithoutCandidateWorkWhenCreated()
    {
        var checksum = new ConstructionRecordingChecksum();

        var exception = Should.Throw<InvalidOperationException>(() => checksum.Invocations.CountCandidates());

        exception.Message.ShouldContain("reports none");
    }

    // A candidate counter of another operation, placed explicitly on an operation without candidate work, fails when
    // the wrapper is built and before its child is.
    [Fact]
    public void CountedWithACandidateCounter_RejectsAnOperationWithoutCandidateWorkBeforeBuildingIt()
    {
        var checksum = new ConstructionRecordingChecksum();
        var candidates = new LengthScorer().Invocations.CountCandidates();
        var scope = ResolutionScope.Create().For<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>();

        var exception = Should.Throw<InvalidOperationException>(() => scope.Resolve(checksum.Counted(candidates)));

        exception.Message.ShouldContain("reports none");
        checksum.Builds.ShouldBe(0);
    }

    [Fact]
    public void Count_CountsAnOperationWithoutCandidateWork()
    {
        var checksum = new ConstructionRecordingChecksum();
        var problem = CreateProblem();
        var invocations = checksum.Invocations.Count();
        var scope = ResolutionScope.Create();
        scope.AddAspect(scope.Results.Aspect(invocations));
        var executor = scope.For<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>().Resolve(checksum);

        executor.Read(RandomNumberGenerator.Create(1), problem.SearchSpace, problem);
        executor.Read(RandomNumberGenerator.Create(1), problem.SearchSpace, problem);

        scope.Results.Result(invocations).CurrentCount.ShouldBe(2L);
    }

    private static FuncProblem<int, DummySearchSpace<int>> CreateProblem() =>
        FuncProblem.Create(static (int candidate) => candidate, DummySearchSpace<int>.Instance, SingleObjective.Minimize);

    private sealed class ConstructionRecordingChecksum : IOperator<Checksum, int>
    {
        public int Builds { get; private set; }

        IExecutor IOperator<Checksum, int>.CreateExecutor<TSearchSpace, TProblem, TSearchState>(
            ResolutionScope<int, TSearchSpace, TProblem, TSearchState> scope)
        {
            Builds++;
            return new Executor();
        }

        private sealed class Executor : IChecksumExecutor<int, ISearchSpace<int>, IProblem<int, ISearchSpace<int>>>
        {
            public int Read(IRandomNumberGenerator random, ISearchSpace<int> searchSpace, IProblem<int, ISearchSpace<int>> problem) => 1;
        }
    }

    private sealed record LengthScorer : IOperator<Scoring, int>
    {
        IExecutor IOperator<Scoring, int>.CreateExecutor<TSearchSpace, TProblem, TSearchState>(
            ResolutionScope<int, TSearchSpace, TProblem, TSearchState> scope) => new Executor();

        private sealed class Executor : IScoringExecutor<int, ISearchSpace<int>, IProblem<int, ISearchSpace<int>>>
        {
            public IReadOnlyList<double> Score(IReadOnlyList<int> candidates, IRandomNumberGenerator random, ISearchSpace<int> searchSpace,
                IProblem<int, ISearchSpace<int>> problem) => [.. candidates.Select(candidate => (double)candidate)];
        }
    }
}

public interface IScoringExecutor<TCandidate, in TSearchSpace, in TProblem> : IExecutor
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    IReadOnlyList<double> Score(IReadOnlyList<TCandidate> candidates, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem);
}

// A batch operation defined outside the library that reports one unit of candidate work per scored candidate.
public sealed class Scoring : IOperation<Scoring>
{
    static bool IOperation<Scoring>.IsExecutor<TCandidate, TSearchSpace, TProblem, TSearchState>(IExecutor executor) =>
        executor is IScoringExecutor<TCandidate, TSearchSpace, TProblem>;

    static IExecutor IOperation<Scoring>.Wrap<TCandidate, TSearchSpace, TProblem, TSearchState, TWrapper>(IExecutor executor, TWrapper wrapper) =>
        new WrappedExecutor<TCandidate, TSearchSpace, TProblem, TWrapper>(executor, wrapper);

    static bool IOperation<Scoring>.SupportsCandidateWork => true;

    private readonly struct Invocation<TCandidate, TSearchSpace, TProblem>(IScoringExecutor<TCandidate, TSearchSpace, TProblem> executor,
        IReadOnlyList<TCandidate> candidates, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem) : IInvocation<IReadOnlyList<double>>
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        private readonly IScoringExecutor<TCandidate, TSearchSpace, TProblem> executor = executor;
        private readonly IRandomNumberGenerator random = random;

        public IRandomNumberGenerator Random => random;
        public ISearchSpace SearchSpace => searchSpace;
        public IProblem Problem => problem;

        public IReadOnlyList<double> Proceed(IExecutor? executor = null, IRandomNumberGenerator? random = null) =>
            ((IScoringExecutor<TCandidate, TSearchSpace, TProblem>?)executor ?? this.executor).Score(candidates, random ?? this.random, searchSpace, problem);

        public int CountCandidateWork(IReadOnlyList<double> result) => result.Count;
    }

    private sealed class WrappedExecutor<TCandidate, TSearchSpace, TProblem, TWrapper>(IExecutor executor, TWrapper wrapper)
        : WrappingExecutor<IScoringExecutor<TCandidate, TSearchSpace, TProblem>, TWrapper>(executor, wrapper), IScoringExecutor<TCandidate, TSearchSpace, TProblem>
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TWrapper : struct, IInvocationWrapper
    {
        public IReadOnlyList<double> Score(IReadOnlyList<TCandidate> candidates, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem) =>
            Wrapper.Invoke<Invocation<TCandidate, TSearchSpace, TProblem>, IReadOnlyList<double>>(new(Executor, candidates, random, searchSpace, problem));
    }
}

public static class ScoringResolutionExtensions
{
    extension<TCandidate, TSearchSpace, TProblem>(ResolutionScope<TCandidate, TSearchSpace, TProblem> scope)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        public IScoringExecutor<TCandidate, TSearchSpace, TProblem> Resolve(IOperator<Scoring, TCandidate> source) =>
            (IScoringExecutor<TCandidate, TSearchSpace, TProblem>)scope.ResolveOperator(source);
    }
}

// The external operation's source, which is what its counters come from, declared like a built-in operation's.
public sealed record ScoringObservation<TCandidate>(IReadOnlyList<TCandidate> Candidates, IReadOnlyList<double> Scores);

file sealed class ScoringObservationAdvice<TCandidate>(Action<ScoringObservation<TCandidate>> observe) : OperatorAdvice<Scoring, TCandidate>
{
    public override IExecutor Wrap<TSearchSpace, TProblem, TSearchState>(IExecutor executor, OperationInfo operation,
        IOperator<Scoring, TCandidate> source) =>
        new Executor<TSearchSpace, TProblem>((IScoringExecutor<TCandidate, TSearchSpace, TProblem>)executor, observe);

    private sealed class Executor<TSearchSpace, TProblem>(IScoringExecutor<TCandidate, TSearchSpace, TProblem> executor,
        Action<ScoringObservation<TCandidate>> observe) : IScoringExecutor<TCandidate, TSearchSpace, TProblem>
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        public IReadOnlyList<double> Score(IReadOnlyList<TCandidate> candidates, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem)
        {
            var scores = executor.Score(candidates, random, searchSpace, problem);
            observe(new ScoringObservation<TCandidate>(candidates, scores));
            return scores;
        }
    }
}

public static class ScoringObservations
{
    extension<TCandidate>(IOperator<Scoring, TCandidate> scorer)
    {
        public InvocationSource<Scoring, TCandidate, ScoringObservation<TCandidate>> Invocations =>
            ObservationSource.Create(scorer, (Action<ScoringObservation<TCandidate>> observe) => new ScoringObservationAdvice<TCandidate>(observe));
    }
}
