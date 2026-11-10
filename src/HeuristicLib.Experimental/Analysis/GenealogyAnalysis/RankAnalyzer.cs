using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Inspection;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Operators;

namespace HEAL.HeuristicLib.Analysis.GenealogyAnalysis;

/// <summary>
/// Defines a rank result without naming its observation-source types.
/// </summary>
public abstract class RankAnalyzer<TCandidate> : Instrument<RankState<TCandidate>>
    where TCandidate : notnull;

/// <summary>
/// Records, per generation, the average rank of the descendants of each candidate in the generation before it.
/// </summary>
/// <remarks>
/// Each run creates its own graph and rank history. Genealogy observations update that graph before rank recording.
/// Both observations share the same explicit objective ordering.
/// </remarks>
public sealed class RankAnalyzer<TCandidate, TSearchState> : RankAnalyzer<TCandidate>,
    IObservationInstrument<CrossoverObservation<TCandidate>>,
    IObservationInstrument<MutationObservation<TCandidate>>,
    IObservationInstrument<AlgorithmObservation<TCandidate, TSearchState>>
    where TSearchState : PopulationState<TCandidate>
    where TCandidate : notnull
{
    /// <param name="crossovers">Crossovers whose offspring become graph edges from two parents.</param>
    /// <param name="mutators">Mutators whose offspring become graph edges from one parent.</param>
    /// <param name="algorithms">
    /// Algorithms whose yielded populations close a generation, which is also where ranks are read. Without one the
    /// graph is still built, but no ranks are recorded.
    /// </param>
    /// <param name="equality">
    /// Decides which candidates are the same graph node. Defaults to <see cref="EqualityComparer{T}.Default"/>.
    /// </param>
    /// <param name="objectiveComparer">
    /// Ranks the candidates of a generation. Without one, the problem's objective must define a total order.
    /// </param>
    public RankAnalyzer(IReadOnlyList<IOperator<Crossover, TCandidate>>? crossovers = null,
                        IReadOnlyList<IOperator<Mutation, TCandidate>>? mutators = null,
                        IReadOnlyList<IAlgorithm<TCandidate, TSearchState>>? algorithms = null,
                        IEqualityComparer<TCandidate>? equality = null,
                        IComparer<ObjectiveVector>? objectiveComparer = null)
    {
        if ((crossovers?.Count ?? 0) + (mutators?.Count ?? 0) + (algorithms?.Count ?? 0) == 0)
            throw new ArgumentException("A rank analysis needs at least one crossover, mutator or algorithm to observe.");
        this.equality = equality ?? EqualityComparer<TCandidate>.Default;
        ObjectiveComparer = objectiveComparer;
        genealogy = new GenealogyAnalyzer<TCandidate, TSearchState>(crossovers, mutators, algorithms, equality)
        { ObjectiveComparer = objectiveComparer };
    }

    private readonly GenealogyAnalyzer<TCandidate, TSearchState> genealogy;
    private readonly IEqualityComparer<TCandidate> equality;

    public IComparer<ObjectiveVector>? ObjectiveComparer { get; }

    protected override IReadOnlyList<Substitution> CreateSubstitutions() => genealogy.Recording(this, this, this);

    Action<CrossoverObservation<TCandidate>> IObservationInstrument<CrossoverObservation<TCandidate>>.CreateRecorder(RunResults results) =>
        GenealogyAnalyzer<TCandidate, TSearchState>.RecordCrossovers(results.Result(this).Graph);

    Action<MutationObservation<TCandidate>> IObservationInstrument<MutationObservation<TCandidate>>.CreateRecorder(RunResults results) =>
        GenealogyAnalyzer<TCandidate, TSearchState>.RecordMutations(results.Result(this).Graph);

    // The graph contains this generation before its ranks are read.
    Action<AlgorithmObservation<TCandidate, TSearchState>> IObservationInstrument<AlgorithmObservation<TCandidate, TSearchState>>.CreateRecorder(RunResults results)
    {
        var result = results.Result(this);
        var closeGeneration = genealogy.RecordGenerations(result.Graph);
        return observation =>
        {
            closeGeneration(observation);
            result.RecordRanks();
        };
    }

    protected override RankState<TCandidate> CreateResult(RunResults results) => new(new GenealogyGraph<TCandidate>(equality));
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
        public static RankAnalyzer<T, TR> Rank<T, TR>(
            IOperator<Crossover, T>? crossover = null,
            IOperator<Mutation, T>? mutator = null,
            IAlgorithm<T, TR>? algorithm = null,
            IEqualityComparer<T>? equality = null,
            IComparer<ObjectiveVector>? objectiveComparer = null)
            where T : notnull
            where TR : PopulationState<T> =>
            new(
                crossover is null ? null : (IReadOnlyList<IOperator<Crossover, T>>)[crossover],
                mutator is null ? null : (IReadOnlyList<IOperator<Mutation, T>>)[mutator],
                algorithm is null ? null : (IReadOnlyList<IAlgorithm<T, TR>>)[algorithm],
                equality, objectiveComparer);
    }
}
