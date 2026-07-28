using System.Runtime.CompilerServices;
using Generator.Equals;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Algorithms.MetaAlgorithms;

// ToDo: Add Pipeline with different State types
// ToDo: Add support for Transformation between different (or the same typed) states.

[Equatable]
public partial record PipelineAlgorithm<TAlgorithm, TCandidate, TSearchSpace, TProblem, TSearchState>
    : Algorithm<PipelineAlgorithm<TAlgorithm, TCandidate, TSearchSpace, TProblem, TSearchState>, TCandidate, TSearchSpace, TProblem, TSearchState>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
    where TSearchState : class, ISearchState
    where TAlgorithm : IAlgorithm<TCandidate, TSearchSpace, TProblem, TSearchState>
{
    [OrderedEquality] public ImmutableArray<TAlgorithm> Algorithms { get; }

    public PipelineAlgorithm(ImmutableArray<TAlgorithm> algorithms)
    {
        if (algorithms.Length == 0)
        {
            throw new ArgumentException("At least one algorithm must be provided.", nameof(algorithms));
        }

        Algorithms = algorithms;
    }

    protected override PipelineAlgorithmInstance<TAlgorithm, TCandidate, TSearchSpace, TProblem, TSearchState> CreateAlgorithmInstance(ExecutionInstanceRegistry registry) =>
        new(registry, Algorithms);
}

public static class PipelineAlgorithm
{
    public static PipelineAlgorithm<TAlgorithm, TCandidate, TSearchSpace, TProblem, TSearchState> Create<TAlgorithm, TCandidate, TSearchSpace, TProblem, TSearchState>(
        Algorithm<TAlgorithm, TCandidate, TSearchSpace, TProblem, TSearchState> firstAlgorithm, params IEnumerable<TAlgorithm> followingAlgorithms)
        where TAlgorithm : Algorithm<TAlgorithm, TCandidate, TSearchSpace, TProblem, TSearchState>
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TSearchState : class, ISearchState => new([firstAlgorithm.Self, .. followingAlgorithms]);

    public static PipelineAlgorithm<IAlgorithm<TCandidate, TSearchSpace, TProblem, TSearchState>, TCandidate, TSearchSpace, TProblem, TSearchState> Create<TCandidate, TSearchSpace, TProblem, TSearchState>(
        params IEnumerable<IAlgorithm<TCandidate, TSearchSpace, TProblem, TSearchState>> algorithms)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TSearchState : class, ISearchState => new([.. algorithms]);
}

public static class PipelineAlgorithmExtensions
{
    extension<TAlgorithm, TCandidate, TSearchSpace, TProblem, TSearchState>(Algorithm<TAlgorithm, TCandidate, TSearchSpace, TProblem, TSearchState> algorithm)
        where TAlgorithm : Algorithm<TAlgorithm, TCandidate, TSearchSpace, TProblem, TSearchState>
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TSearchState : class, ISearchState
    {
        public PipelineAlgorithm<TAlgorithm, TCandidate, TSearchSpace, TProblem, TSearchState> Then(params IEnumerable<TAlgorithm> followingAlgorithms) =>
            PipelineAlgorithm.Create(algorithm, followingAlgorithms);
    }

    extension<TCandidate, TSearchSpace, TProblem, TSearchState>(IAlgorithm<TCandidate, TSearchSpace, TProblem, TSearchState> algorithm)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TSearchState : class, ISearchState
    {
        public PipelineAlgorithm<IAlgorithm<TCandidate, TSearchSpace, TProblem, TSearchState>, TCandidate, TSearchSpace, TProblem, TSearchState> Then(params IEnumerable<IAlgorithm<TCandidate, TSearchSpace, TProblem, TSearchState>> followingAlgorithms) =>
            PipelineAlgorithm.Create([algorithm, .. followingAlgorithms]);
    }
}

public class PipelineAlgorithmInstance<TAlgorithm, TCandidate, TSearchSpace, TProblem, TSearchState>
    : AlgorithmInstance<TCandidate, TSearchSpace, TProblem, TSearchState>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
    where TSearchState : class, ISearchState
    where TAlgorithm : IAlgorithm<TCandidate, TSearchSpace, TProblem, TSearchState>
{
    private readonly ExecutionInstanceRegistry registry;
    protected readonly IReadOnlyList<TAlgorithm> Algorithms;

    public PipelineAlgorithmInstance(ExecutionInstanceRegistry registry, IReadOnlyList<TAlgorithm> algorithms)
    {
        this.registry = registry;
        Algorithms = algorithms;
    }

    public override async IAsyncEnumerable<TSearchState> RunStreamingAsync(TProblem problem, IRandomNumberGenerator random, TSearchState? initialState = null, [EnumeratorCancellation] CancellationToken ct = default)
    {
        var state = initialState;

        foreach (var (algorithm, index) in Algorithms.Select((a, i) => (a, i)))
        {
            ct.ThrowIfCancellationRequested();
            var algRng = random.Fork(index);
            var childRegistry = registry.CreateChildRegistry();
            var algorithmInstance = algorithm.CreateExecutionInstance(childRegistry);

            await foreach (var newState in algorithmInstance.RunStreamingAsync(problem, algRng, state, ct))
            {
                state = newState;
                yield return newState;
            }
        }
    }
}
