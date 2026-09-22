using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Analysis.GenealogyAnalysis;

/// <summary>
/// Reads what this analyzer collected, without naming its observation-source types.
/// </summary>
public interface IRankAnalyzer<TCandidate> : IAnalyzer
    where TCandidate : notnull
{
    RankState<TCandidate> State { get; }
}

/// <summary>
/// Records, per generation, the average rank of the descendants of each candidate in the generation before it.
/// </summary>
/// <remarks>
/// This analyzer composes a genealogy analysis: it installs that analysis into a graph it owns and adds one observation
/// of its own. Both observations share the same explicit objective ordering.
/// </remarks>
public sealed class RankAnalyzer<TCandidate, TSearchSpace, TProblem, TSearchState> : IRankAnalyzer<TCandidate>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
    where TSearchState : PopulationState<TCandidate>
    where TCandidate : notnull
{
    private readonly ImmutableArray<ICrossover<TCandidate>> crossovers;
    private readonly ImmutableArray<IMutator<TCandidate>> mutators;
    private readonly ImmutableArray<IAlgorithm<TCandidate, TSearchState>> algorithms;

    /// <param name="crossovers">Crossovers whose offspring become graph edges from two parents.</param>
    /// <param name="mutators">Mutators whose offspring become graph edges from one parent.</param>
    /// <param name="algorithms">
    /// Algorithms whose yielded populations close a generation, which is also where ranks are read. Without one the
    /// graph is still built, but no ranks are recorded.
    /// </param>
    /// <param name="equality">
    /// Decides which candidates are the same graph node. Defaults to <see cref="EqualityComparer{T}.Default"/>.
    /// </param>
    public RankAnalyzer(IReadOnlyList<ICrossover<TCandidate>>? crossovers = null,
                        IReadOnlyList<IMutator<TCandidate>>? mutators = null,
                        IReadOnlyList<IAlgorithm<TCandidate, TSearchState>>? algorithms = null,
                        IEqualityComparer<TCandidate>? equality = null)
    {
        if ((crossovers?.Count ?? 0) + (mutators?.Count ?? 0) + (algorithms?.Count ?? 0) == 0)
            throw new ArgumentException("A rank analysis needs at least one crossover, mutator or algorithm to observe.");
        State = new RankState<TCandidate>(new GenealogyGraph<TCandidate>(equality ?? EqualityComparer<TCandidate>.Default));
        this.crossovers = (crossovers ?? []).ToImmutableArray();
        this.mutators = (mutators ?? []).ToImmutableArray();
        this.algorithms = [.. algorithms ?? []];
    }

    public RankState<TCandidate> State { get; }

    public IComparer<ObjectiveVector>? ObjectiveComparer { get; init; }

    public void Install(ResolutionScopeBuilder builder)
    {
        // Installed first, so the graph already contains this generation when the ranks over it are read.
        var graphBuilder = new GenealogyAnalyzer<TCandidate, TSearchSpace, TProblem, TSearchState>(
            State.Graph, crossovers, mutators, algorithms, saveSpace: false)
        { ObjectiveComparer = ObjectiveComparer };
        graphBuilder.Install(builder);

        foreach (var algorithm in algorithms)
            builder.Observe(algorithm, _ => State.RecordRanks());
    }
}

/// <summary>
/// The graph a rank analysis builds and the ranks it read from it, one row per closed generation.
/// </summary>
/// <remarks>Both are accumulators, so read them once the run has finished.</remarks>
public class RankState<TCandidate> where TCandidate : notnull
{
    private readonly List<ImmutableArray<double>> ranks = [];

    public RankState(GenealogyGraph<TCandidate> graph)
    {
        Graph = graph;
    }

    public GenealogyGraph<TCandidate> Graph { get; }

    /// <summary>The average descendant ranks per closed generation, oldest first.</summary>
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
        public static RankAnalyzer<T, ISearchSpace<T>, IProblem<T, ISearchSpace<T>>, TR> Rank<T, TR>(
            ICrossover<T>? crossover = null,
            IMutator<T>? mutator = null,
            IAlgorithm<T, TR>? algorithm = null,
            IEqualityComparer<T>? equality = null,
            IComparer<ObjectiveVector>? objectiveComparer = null)
            where T : notnull
            where TR : PopulationState<T> =>
            new(
                crossover is null ? null : (IReadOnlyList<ICrossover<T>>)[crossover],
                mutator is null ? null : (IReadOnlyList<IMutator<T>>)[mutator],
                algorithm is null ? null : (IReadOnlyList<IAlgorithm<T, TR>>)[algorithm],
                equality)
            { ObjectiveComparer = objectiveComparer };
    }
}
