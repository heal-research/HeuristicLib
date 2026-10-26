using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.Tests.ExecutionInfrastructure.OperatorFoundation;
using HEAL.HeuristicLib.Tests.TestSupport.Mocks;

namespace HEAL.HeuristicLib.Tests.Analysis;

/// <summary>
/// Clocks over an operator's invocations or candidate work serve every operation, including ones defined outside the
/// library.
/// </summary>
public class OperatorCountClockTests
{
    [Fact]
    public void FromCandidates_CountsTheCandidateWorkOfAnExternalBatchOperation()
    {
        var scorer = new LengthScorer();
        var problem = CreateProblem();
        var candidates = Clock.FromCandidates(scorer);
        var invocations = Clock.FromInvocations(scorer);
        var executor = ResolutionScope.Create(candidates, invocations).For<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>().Resolve(scorer);

        executor.Score([1, 2, 3], RandomNumberGenerator.Create(1), problem.SearchSpace, problem);
        executor.Score([4, 5], RandomNumberGenerator.Create(1), problem.SearchSpace, problem);

        candidates.Read().ShouldBe(5L);
        invocations.Read().ShouldBe(2L);
    }

    [Fact]
    public void FromCandidates_RejectsAnOperationWithoutCandidateWorkBeforeBuildingIt()
    {
        var checksum = new ConstructionRecordingChecksum();
        var scope = ResolutionScope.Create(Clock.FromCandidates(checksum)).For<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>();

        var exception = Should.Throw<InvalidOperationException>(() => scope.Resolve(checksum));

        exception.Message.ShouldContain("reports none");
        checksum.Builds.ShouldBe(0);
    }

    [Fact]
    public void FromInvocations_CountsAnOperationWithoutCandidateWork()
    {
        var checksum = new ConstructionRecordingChecksum();
        var problem = CreateProblem();
        var invocations = Clock.FromInvocations(checksum);
        var executor = ResolutionScope.Create(invocations).For<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>().Resolve(checksum);

        executor.Read(RandomNumberGenerator.Create(1), problem.SearchSpace, problem);
        executor.Read(RandomNumberGenerator.Create(1), problem.SearchSpace, problem);

        invocations.Read().ShouldBe(2L);
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
