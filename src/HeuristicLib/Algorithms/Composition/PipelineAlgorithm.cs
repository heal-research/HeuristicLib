using System.Runtime.CompilerServices;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Algorithms;

/// <remarks>
/// Every stage resolves its algorithm in a new child scope, so stages share no executors unless the pipeline shares
/// them through <c>Sharing</c>.
/// </remarks>
public record PipelineAlgorithm<TCandidate, TSearchState>
    : Algorithm<PipelineAlgorithm<TCandidate, TSearchState>, TCandidate, TSearchState>
    where TSearchState : class, ISearchState
{
    public ValueArray<IAlgorithm<TCandidate, TSearchState>> Algorithms { get; init; }

    public static ListParameter<PipelineAlgorithm<TCandidate, TSearchState>, IAlgorithm<TCandidate, TSearchState>> AlgorithmsParameter { get; } =
        new(nameof(Algorithms), static pipeline => pipeline.Algorithms, static (pipeline, value) => pipeline with { Algorithms = value.ToValueArray() });

    public static IReadOnlyList<IParameter> Parameters { get; } = [AlgorithmsParameter];

    public override IReadOnlyList<IParameter> GetParameters() => [.. base.GetParameters(), .. Parameters];

    public PipelineAlgorithm(IReadOnlyList<IAlgorithm<TCandidate, TSearchState>> algorithms)
    {
        if (algorithms.Count == 0)
            throw new ArgumentException("At least one algorithm must be provided.", nameof(algorithms));

        Algorithms = algorithms.ToValueArray();
    }

    protected override IAlgorithmExecutor<TCandidate, TSearchSpace, TProblem, TSearchState> CreateExecutor<TSearchSpace, TProblem>(
        ResolutionScope<TCandidate, TSearchSpace, TProblem, TSearchState> scope) =>
        new PipelineAlgorithmExecutor<TCandidate, TSearchSpace, TProblem, TSearchState>(scope, Algorithms);
}

public static class PipelineAlgorithm
{
    public static PipelineAlgorithm<TCandidate, TSearchState> Create<TCandidate, TSearchState>(
        params IReadOnlyList<IAlgorithm<TCandidate, TSearchState>> algorithms)
        where TSearchState : class, ISearchState => new([.. algorithms]);
}

public static class PipelineAlgorithmExtensions
{
    extension<TCandidate, TSearchState>(IAlgorithm<TCandidate, TSearchState> algorithm)
        where TSearchState : class, ISearchState
    {
        public PipelineAlgorithm<TCandidate, TSearchState> Then(params IReadOnlyList<IAlgorithm<TCandidate, TSearchState>> followingAlgorithms) =>
            PipelineAlgorithm.Create<TCandidate, TSearchState>([algorithm, .. followingAlgorithms]);
    }
}

public class PipelineAlgorithmExecutor<TCandidate, TSearchSpace, TProblem, TSearchState>
    : AlgorithmExecutor<TCandidate, TSearchSpace, TProblem, TSearchState>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
    where TSearchState : class, ISearchState
{
    private readonly ResolutionScope scope;
    protected readonly ImmutableArray<IAlgorithm<TCandidate, TSearchState>> Algorithms;

    public PipelineAlgorithmExecutor(ResolutionScope scope, IReadOnlyList<IAlgorithm<TCandidate, TSearchState>> algorithms)
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
            var algorithmExecutor = scope.CreateChildScope().Resolve<TCandidate, TSearchSpace, TProblem, TSearchState>(algorithm);

            await foreach (var newState in algorithmExecutor.RunStreamingAsync(problem, algRng, state, ct))
            {
                state = newState;
                yield return newState;
            }
        }
    }
}
