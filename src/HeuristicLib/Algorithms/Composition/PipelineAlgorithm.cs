using System.Runtime.CompilerServices;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Algorithms;

public record PipelineAlgorithm<TAlgorithm, TCandidate, TSearchState>
    : Algorithm<PipelineAlgorithm<TAlgorithm, TCandidate, TSearchState>, TCandidate, TSearchState>
    where TSearchState : class, ISearchState
    where TAlgorithm : IAlgorithm<TCandidate, TSearchState>
{
    public ValueArray<TAlgorithm> Algorithms { get; }

    public override bool Fits(ExecutionSignature execution) => base.Fits(execution) && execution.Fits([.. Algorithms]);

    public PipelineAlgorithm(IReadOnlyList<TAlgorithm> algorithms)
    {
        if (algorithms.Count == 0)
            throw new ArgumentException("At least one algorithm must be provided.", nameof(algorithms));

        Algorithms = algorithms.ToValueArray();
    }

    protected override IAlgorithmExecutor<TCandidate, TSearchSpace, TProblem, TSearchState> CreateExecutor<TSearchSpace, TProblem>(
        ResolutionScope<TCandidate, TSearchSpace, TProblem, TSearchState> scope) =>
        new PipelineAlgorithmExecutor<TAlgorithm, TCandidate, TSearchSpace, TProblem, TSearchState>(scope, Algorithms);
}

public static class PipelineAlgorithm
{
    public static PipelineAlgorithm<TAlgorithm, TCandidate, TSearchState> Create<TAlgorithm, TCandidate, TSearchState>(
        Algorithm<TAlgorithm, TCandidate, TSearchState> firstAlgorithm, params IReadOnlyList<TAlgorithm> followingAlgorithms)
        where TAlgorithm : Algorithm<TAlgorithm, TCandidate, TSearchState>
        where TSearchState : class, ISearchState => new([firstAlgorithm.Self, .. followingAlgorithms]);

    public static PipelineAlgorithm<IAlgorithm<TCandidate, TSearchState>, TCandidate, TSearchState> Create<TCandidate, TSearchState>(
        params IReadOnlyList<IAlgorithm<TCandidate, TSearchState>> algorithms)
        where TSearchState : class, ISearchState => new([.. algorithms]);
}

public static class PipelineAlgorithmExtensions
{
    extension<TAlgorithm, TCandidate, TSearchState>(Algorithm<TAlgorithm, TCandidate, TSearchState> algorithm)
        where TAlgorithm : Algorithm<TAlgorithm, TCandidate, TSearchState>
        where TSearchState : class, ISearchState
    {
        public PipelineAlgorithm<TAlgorithm, TCandidate, TSearchState> Then(params IReadOnlyList<TAlgorithm> followingAlgorithms) =>
            PipelineAlgorithm.Create(algorithm, followingAlgorithms);
    }

    extension<TCandidate, TSearchState>(IAlgorithm<TCandidate, TSearchState> algorithm)
        where TSearchState : class, ISearchState
    {
        public PipelineAlgorithm<IAlgorithm<TCandidate, TSearchState>, TCandidate, TSearchState> Then(params IReadOnlyList<IAlgorithm<TCandidate, TSearchState>> followingAlgorithms) =>
            PipelineAlgorithm.Create<TCandidate, TSearchState>([algorithm, .. followingAlgorithms]);
    }
}

public class PipelineAlgorithmExecutor<TAlgorithm, TCandidate, TSearchSpace, TProblem, TSearchState>
    : AlgorithmExecutor<TCandidate, TSearchSpace, TProblem, TSearchState>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
    where TSearchState : class, ISearchState
    where TAlgorithm : IAlgorithm<TCandidate, TSearchState>
{
    private readonly ResolutionScope scope;
    protected readonly ImmutableArray<TAlgorithm> Algorithms;

    public PipelineAlgorithmExecutor(ResolutionScope scope, IReadOnlyList<TAlgorithm> algorithms)
    {
        this.scope = scope;
        Algorithms = algorithms.ToImmutableArray();
    }

    public override async IAsyncEnumerable<TSearchState> RunStreamingAsync(TProblem problem, IRandomNumberGenerator random, TSearchState? initialState = null, [EnumeratorCancellation] CancellationToken ct = default)
    {
        var state = initialState;

        foreach (var (algorithm, index) in Algorithms.Select((a, i) => (a, i)))
        {
            ct.ThrowIfCancellationRequested();
            var algRng = random.Fork(index);
            var childScope = scope.CreateChildScope();
            var algorithmExecutor = childScope.Resolve<TCandidate, TSearchSpace, TProblem, TSearchState>(algorithm);

            await foreach (var newState in algorithmExecutor.RunStreamingAsync(problem, algRng, state, ct))
            {
                state = newState;
                yield return newState;
            }
        }
    }
}
