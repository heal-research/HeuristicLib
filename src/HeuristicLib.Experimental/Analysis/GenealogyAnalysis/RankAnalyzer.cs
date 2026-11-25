using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Inspection;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Operators;

namespace HEAL.HeuristicLib.Analysis.GenealogyAnalysis;

/// <summary>
/// Records, per generation, the average rank of the descendants of each candidate in the generation before it.
/// </summary>
/// <remarks>
/// Each run creates its own graph and rank history. Genealogy observations update that graph before rank recording.
/// Both observations share the same objective ordering. Read the live graph and history after execution has stopped.
/// </remarks>
public sealed class RankAnalyzer<TCandidate, TSearchState> : Analyzer<RankState<TCandidate>>,
    IObservationReceiver<AlgorithmObservation<TCandidate, TSearchState>>
    where TSearchState : PopulationState<TCandidate>
    where TCandidate : notnull
{
    /// <param name="crossovers">Crossovers whose offspring become graph edges from two parents.</param>
    /// <param name="mutators">Mutators whose offspring become graph edges from one parent.</param>
    /// <param name="algorithms">
    /// Algorithms whose yielded populations close a generation, which is also where ranks are read. At least one is required.
    /// </param>
    /// <param name="equality">
    /// Decides which candidates are the same graph node. Defaults to <see cref="EqualityComparer{T}.Default"/>.
    /// </param>
    /// <param name="objectiveComparer">
    /// Ranks the candidates of a generation. Without one, the problem's objective must define a total order.
    /// </param>
    /// <exception cref="InvalidOperationException"><paramref name="algorithms"/> is empty.</exception>
    public RankAnalyzer(IReadOnlyList<IAlgorithm<TCandidate, TSearchState>> algorithms,
                        IReadOnlyList<IOperator<Crossover, TCandidate>>? crossovers = null,
                        IReadOnlyList<IOperator<Mutation, TCandidate>>? mutators = null,
                        IEqualityComparer<TCandidate>? equality = null,
                        IComparer<ObjectiveVector>? objectiveComparer = null)
    {
        if (algorithms.Count == 0)
            throw new InvalidOperationException("A rank analysis needs at least one algorithm to observe.");
        this.algorithms = algorithms.ToValueArray();
        ObjectiveComparer = objectiveComparer;
        Genealogy = new GenealogyAnalyzer<TCandidate, TSearchState>(crossovers, mutators, algorithms, equality)
        { ObjectiveComparer = objectiveComparer };
    }

    private readonly ValueArray<IAlgorithm<TCandidate, TSearchState>> algorithms;

    /// <summary>The genealogy analyzer whose graph this analysis ranks.</summary>
    /// <remarks>
    /// Placing the rank analyzer with <c>CreateRun</c> or <c>Substitute</c> also places it. To place the rank analyzer explicitly,
    /// place this analyzer as well, or no graph is recorded and every rank row stays empty.
    /// </remarks>
    public GenealogyAnalyzer<TCandidate, TSearchState> Genealogy { get; }

    /// <summary>Orders each generation; when omitted, the observation's problem must supply a total order.</summary>
    public IComparer<ObjectiveVector>? ObjectiveComparer { get; }

    protected override IReadOnlyList<ISubstitution> CreateSubstitutions() =>
    [
        .. algorithms.Select(algorithm => algorithm.Iterations.RecordingInto(this)),
        Genealogy
    ];

    // Genealogy's inner wrapper has closed this generation before ranks are read.
    /// <summary>Records descendant ranks from this run's graph after genealogy closes the generation.</summary>
    public Action<AlgorithmObservation<TCandidate, TSearchState>> CreateRecorder(RunResults results)
    {
        var result = results.Result(this);
        return _ => result.RecordRanks();
    }

    /// <inheritdoc/>
    public override RankState<TCandidate> CreateExecutionNode(ResolutionScope scope) => new(scope.Results.Result(Genealogy));
}

/// <summary>
/// The graph a rank analysis builds and the ranks it read from it, one row per closed generation.
/// </summary>
/// <remarks>Both are accumulators, so read them once the run has finished.</remarks>
public class RankState<TCandidate> : IRunResult where TCandidate : notnull
{
    private readonly List<ImmutableArray<double>> ranks = [];

    public RankState(GenealogyGraph<TCandidate> graph)
    {
        Graph = graph;
    }

    public GenealogyGraph<TCandidate> Graph { get; }

    /// <summary>Live rows of average descendant ranks, oldest first; generations without a rank row are omitted.</summary>
    public IReadOnlyList<ImmutableArray<double>> Ranks => ranks;

    internal void RecordRanks()
    {
        var row = Graph.AverageDescendantRanksOfPreviousGeneration();
        if (row.IsEmpty)
            return;

        lock (ranks)
            ranks.Add(row);
    }
}

public static class RankAnalysisTraces
{
    extension(Analyzer)
    {
        /// <summary>
        /// Creates a rank analyzer over the given observation sources.
        /// </summary>
        /// <param name="algorithm">The algorithm whose yielded populations close generations and trigger rank recording.</param>
        /// <param name="crossover">An optional crossover to observe, matched by reference.</param>
        /// <param name="mutator">An optional mutator to observe, matched by reference.</param>
        /// <param name="equality">Candidate identity in the graph; omitted means default equality.</param>
        /// <param name="objectiveComparer">Orders each generation; omitted means the problem must supply a total order.</param>
        public static RankAnalyzer<T, TR> Rank<T, TR>(
            IAlgorithm<T, TR> algorithm,
            IOperator<Crossover, T>? crossover = null,
            IOperator<Mutation, T>? mutator = null,
            IEqualityComparer<T>? equality = null,
            IComparer<ObjectiveVector>? objectiveComparer = null)
            where T : notnull
            where TR : PopulationState<T> =>
            new(
                [algorithm],
                crossover is null ? null : (IReadOnlyList<IOperator<Crossover, T>>)[crossover],
                mutator is null ? null : (IReadOnlyList<IOperator<Mutation, T>>)[mutator],
                equality, objectiveComparer);
    }
}
